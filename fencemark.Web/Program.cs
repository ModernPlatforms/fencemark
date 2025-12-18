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

var builder = WebApplication.CreateBuilder(args);

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

// Configure DUAL authentication: Entra External ID (for future) + API Cookie (for current login)
// The default scheme is Cookie auth since that's what the Login page uses with the API
var authBuilder = builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Identity.Application"; // Same as API service
        options.Cookie.HttpOnly = true;
        
        // In development, allow cookies to work across localhost ports
        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in dev
            options.Cookie.SameSite = SameSiteMode.Lax; // Allow cross-site in dev
        }
        else
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        }
        
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        
        // Don't redirect to login page - return 401 instead
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// Configure Azure Entra External ID (CIAM) authentication as an additional scheme
var azureAdSection = builder.Configuration.GetSection("AzureAd");
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
var certificateName = builder.Configuration["KeyVault:CertificateName"];

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

                // Configure certificate from Key Vault
                options.ClientCertificates = new[]
                {
                    CertificateDescription.FromKeyVault(keyVaultUrl, certificateName)
                };
            },
            options =>
            {
                builder.Configuration.Bind("AzureAd", options);
            },
            openIdConnectScheme: "EntraExternalId", // Named scheme, not default
            cookieScheme: null, // Don't use a separate cookie for OIDC
            displayName: "Entra External ID")
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to configure certificate from Key Vault. Error: {ex.Message}");
        Console.Error.WriteLine("Entra External ID authentication will be unavailable.");
        Console.Error.WriteLine("Application will continue with cookie-based authentication only (API login will still work).");
    }
}
else if (azureAdSection.Exists())
{
    // Client secret based authentication (for development) - also as a non-default scheme
    authBuilder.AddMicrosoftIdentityWebApp(
        azureAdSection,
        openIdConnectScheme: "EntraExternalId", // Named scheme, not default
        cookieScheme: null,
        displayName: "Entra External ID")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
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
forwardedHeadersOptions.KnownNetworks.Clear();
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
