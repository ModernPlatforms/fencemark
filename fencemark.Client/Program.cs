using fencemark.Client.Components;
using fencemark.Client.Features.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root components
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

// Check if we have valid Azure AD configuration (not the placeholder values)
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var isValidAzureAdConfig = !string.IsNullOrEmpty(azureAdClientId) && 
                           azureAdClientId != "00000000-0000-0000-0000-000000000000" &&
                           !azureAdClientId.StartsWith("{");

// Configure HTTP client with automatic JWT token attachment (if authentication is configured)
// When running under Aspire, use service discovery to find the API service
// Otherwise, fall back to appsettings.json configuration
var apiBaseUrl = builder.Configuration["services:apiservice:https:0"] 
                ?? builder.Configuration["services:apiservice:http:0"]
                ?? builder.Configuration["ApiBaseUrl"] 
                ?? "https://localhost:62010";

Console.WriteLine($"[Client] Using API Base URL: {apiBaseUrl}");

var apiScope = builder.Configuration["ApiScope"]; // Read from root level, not AzureAd section

if (isValidAzureAdConfig)
{
    // With authentication - add authorization message handler
    builder.Services.AddHttpClient("fencemark.ApiService", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    })
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
        
        var authorizedUrls = new[] { apiBaseUrl };
        
        if (!string.IsNullOrEmpty(apiScope))
        {
            handler.ConfigureHandler(
                authorizedUrls: authorizedUrls,
                scopes: new[] { apiScope });
        }
        else
        {
            handler.ConfigureHandler(authorizedUrls: authorizedUrls);
        }
        
        return handler;
    });
}
else
{
    // Without authentication - simple HTTP client
    builder.Services.AddHttpClient("fencemark.ApiService", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    });
}

// Register a scoped HttpClient for API calls
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("fencemark.ApiService"));

// Register API clients
builder.Services.AddScoped<fencemark.Client.Features.Auth.AuthApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.Fences.FenceApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.Gates.GateApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.Components.ComponentApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.Jobs.JobApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.Organization.OrganizationApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.FenceSegments.FenceSegmentApiClient>();
builder.Services.AddScoped<fencemark.Client.Features.GatePositions.GatePositionApiClient>();

// Configure MSAL authentication for Azure Entra External ID (CIAM)
if (isValidAzureAdConfig)
{
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        
        // Ensure DefaultAccessTokenScopes is initialized
        if (options.ProviderOptions.DefaultAccessTokenScopes == null)
        {
            options.ProviderOptions.DefaultAccessTokenScopes = new List<string>();
        }
        
        if (!string.IsNullOrEmpty(apiScope))
        {
            if (!options.ProviderOptions.DefaultAccessTokenScopes.Contains(apiScope))
            {
                options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
            }
        }
        
        // Always add the openid and profile scopes for CIAM
        if (!options.ProviderOptions.DefaultAccessTokenScopes.Contains("openid"))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
        }
        if (!options.ProviderOptions.DefaultAccessTokenScopes.Contains("profile"))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
        }
        
        options.ProviderOptions.LoginMode = "redirect";
        
        Console.WriteLine($"[Client] MSAL configured with scopes: {string.Join(", ", options.ProviderOptions.DefaultAccessTokenScopes)}");
    });
}
else
{
    // For local development without authentication, provide a no-op authentication state provider
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, NoAuthenticationStateProvider>();
}

await builder.Build().RunAsync();
