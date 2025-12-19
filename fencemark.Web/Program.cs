using fencemark.Web;
using fencemark.Web.Components;
using fencemark.Web.Features.Auth;
using fencemark.Web.Features.Organization;
using fencemark.Web.Features.Fences;
using fencemark.Web.Features.FenceSegments;
using fencemark.Web.Features.Gates;
using fencemark.Web.Features.GatePositions;
using fencemark.Web.Features.Components;
using fencemark.Web.Features.Jobs;
using fencemark.Web.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Helper method for handling external authentication sync
static async Task HandleExternalAuthenticationAsync(TokenValidatedContext context)
{
    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Extract user information from Entra External ID claims
        var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? context.Principal?.FindFirst("preferred_username")?.Value
                    ?? context.Principal?.FindFirst("upn")?.Value;
        var externalId = context.Principal?.FindFirst("oid")?.Value
                        ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var givenName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value;
        var familyName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(externalId))
        {
            logger.LogError("Email or External ID claim not found in token. Available claims: {Claims}",
                string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
            context.Fail("Email or External ID claim not found");
            return;
        }

        logger.LogInformation("Syncing user {Email} with API", email);

        // Extract organization name from email domain
        var organizationName = "Default Organization";
        if (email.Contains('@'))
        {
            var parts = email.Split('@');
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                organizationName = parts[1];
            }
        }

        // Call API to sync user and establish session
        var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("API");

        var externalLoginRequest = new
        {
            email = email,
            externalId = externalId,
            provider = "AzureAD",
            givenName = givenName,
            familyName = familyName,
            organizationName = organizationName
        };

        logger.LogInformation("Calling API external-login for {Email}", email);
        var response = await httpClient.PostAsJsonAsync("/api/auth/external-login", externalLoginRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to sync user with API. Status: {Status}, Error: {Error}",
                response.StatusCode, errorContent);
            context.Fail("Failed to sync user with API");
            return;
        }

        logger.LogInformation("Successfully synced user {Email} with API", email);
        
        // Cookie should now be set by API, authentication is complete
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Exception during external authentication sync");
        context.Fail($"Exception during authentication: {ex.Message}");
    }
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddOutputCache();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Configure Data Protection for shared cookies with API service
builder.Services.AddDataProtection()
    .SetApplicationName("fencemark");

// Configure authentication - Entra External ID only
// The API will set a session cookie after successful Entra authentication
var authBuilder = builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);

// Configure Azure Entra External ID (CIAM) authentication
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
var certificateName = builder.Configuration["KeyVault:CertificateName"];

bool authConfigured = false;

if (!string.IsNullOrWhiteSpace(keyVaultUrl) && !string.IsNullOrWhiteSpace(certificateName))
{
    // Certificate-based authentication with Azure Key Vault
    try
    {
        var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = true
        };
        var credential = new DefaultAzureCredential(defaultAzureCredentialOptions);
        var client = new CertificateClient(new Uri(keyVaultUrl), credential);
        var certificate = await client.GetCertificateAsync(certificateName);

        authBuilder.AddMicrosoftIdentityWebApp(
            options =>
            {
                builder.Configuration.Bind("AzureAd", options);

                // Save tokens so we can forward them to API
                options.SaveTokens = true;
                
                // Configure certificate from Key Vault
                options.ClientCertificates = new[]
                {
                    CertificateDescription.FromKeyVault(keyVaultUrl, certificateName)
                };
                
                // Configure event to sync user with API after Entra authentication
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = HandleExternalAuthenticationAsync
                };
            },
            options =>
            {
                builder.Configuration.Bind("AzureAd", options);
                // Configure cookie to match API's cookie name for session sharing
                options.Cookie.Name = ".AspNetCore.Identity.Application";
            },
            displayName: "Entra External ID")
        .EnableTokenAcquisitionToCallDownstreamApi()  // No scopes here - will be requested on-demand
        .AddInMemoryTokenCaches();
        
        Console.WriteLine($"[Web] Configured for on-demand token acquisition. Scopes: {builder.Configuration.GetValue<string>("AzureAd:Scopes")}");
        
        authConfigured = true;
        Console.WriteLine("✓ Configured Entra External ID with Key Vault certificate");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to configure certificate from Key Vault. Error: {ex.Message}");
        Console.Error.WriteLine("Falling back to client secret authentication...");
    }
}

if (!authConfigured && azureAdSection.Exists())
{
    // Client secret based authentication (for development or fallback)
    try
    {
        Console.WriteLine("Attempting to configure Entra External ID with client secret...");
        authBuilder.AddMicrosoftIdentityWebApp(
            options =>
            {
                builder.Configuration.Bind("AzureAd", options);
                
                Console.WriteLine($"AzureAd Configuration - Instance: {options.Instance}, ClientId: {options.ClientId}, TenantId: {options.TenantId}");
                
                // Save tokens so we can forward them to API
                options.SaveTokens = true;
                
                // Configure event to sync user with API after Entra authentication
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = HandleExternalAuthenticationAsync
                };
            },
            cookieOptions =>
            {
                builder.Configuration.Bind("AzureAd", cookieOptions);
                // Configure cookie to match API's cookie name for session sharing
                cookieOptions.Cookie.Name = ".AspNetCore.Identity.Application";
            },
            displayName: "Entra External ID")
        .EnableTokenAcquisitionToCallDownstreamApi()  // No scopes here - will be requested on-demand
        .AddInMemoryTokenCaches();
        
        Console.WriteLine($"[Web] Configured for on-demand token acquisition. Scopes: {builder.Configuration.GetValue<string>("AzureAd:Scopes")}");
        
        authConfigured = true;
        Console.WriteLine("✓ Configured Entra External ID with client secret");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to configure authentication with client secret: {ex.Message}");
        Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
        throw;
    }
}

if (!authConfigured)
{
    throw new InvalidOperationException(
        "No authentication configured. Please configure AzureAd section in appsettings.json or set up Key Vault.");
}

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();

// Add HttpContextAccessor for authentication forwarding
builder.Services.AddHttpContextAccessor();

// Register the authentication delegating handler
builder.Services.AddScoped<AuthenticationDelegatingHandler>();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    })
    .AddHttpMessageHandler<AuthenticationDelegatingHandler>();

// Add HttpClient for API calls with authentication
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new("https+http://apiservice");
})
.AddHttpMessageHandler<AuthenticationDelegatingHandler>();

// Register API clients
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<OrganizationApiClient>();
builder.Services.AddScoped<FenceApiClient>();
builder.Services.AddScoped<GateApiClient>();
builder.Services.AddScoped<ComponentApiClient>();
builder.Services.AddScoped<JobApiClient>();
builder.Services.AddScoped<FenceSegmentApiClient>();
builder.Services.AddScoped<GatePositionApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Honor X-Forwarded-* headers from Azure Container Apps ingress
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
