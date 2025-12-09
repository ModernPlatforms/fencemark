using fencemark.Web;
using fencemark.Web.Components;
using fencemark.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Configure Azure Entra External ID (CIAM) authentication
var azureAdSection = builder.Configuration.GetSection("AzureAd");

// Check if KeyVault configuration is present for certificate-based authentication
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
var certificateName = builder.Configuration["KeyVault:CertificateName"];

if (!string.IsNullOrWhiteSpace(keyVaultUrl) && !string.IsNullOrWhiteSpace(certificateName))
{
    // Certificate-based authentication with Azure Key Vault
    try
    {
        builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(
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
                displayName: "AzureAd")
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to configure certificate from Key Vault. Error: {ex.Message}");
        throw;
    }
}
else
{
    // Client secret based authentication (for development)
    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(azureAdSection)
        .EnableTokenAcquisitionToCallDownstreamApi()
        .AddInMemoryTokenCaches();
}

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });

// Add HttpClient for API calls
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Register API clients
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<OrganizationApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
