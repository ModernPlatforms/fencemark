using fencemark.Client.Components;
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
var azureAdAuthority = builder.Configuration["AzureAd:Authority"];
var isValidAzureAdConfig = !string.IsNullOrEmpty(azureAdClientId) &&
                           azureAdClientId != "00000000-0000-0000-0000-000000000000" &&
                           !azureAdClientId.StartsWith("{") &&
                           !string.IsNullOrEmpty(azureAdAuthority);


Console.WriteLine($"[Client] Azure AD Config - ClientId: {azureAdClientId}, Authority: {azureAdAuthority}, Valid: {isValidAzureAdConfig}");



// Configure HTTP client with automatic JWT token attachment (if authentication is configured)
// When running under Aspire, use service discovery to find the API service
// Otherwise, fall back to appsettings.json configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                ?? "https://localhost:62010";


Console.WriteLine($"[Client] Using API Base URL: {apiBaseUrl}");


var apiScope = builder.Configuration["ApiScope"]; // Read from root level, not AzureAd section

// Register MSAL authentication first so AuthorizationMessageHandler is available
if (!string.IsNullOrEmpty(apiScope))
{
    Console.WriteLine($"[Client] Using API Scope: {apiScope}");
    builder.Services.AddMsalAuthentication(options =>
    {
        // Manually configure authentication options instead of using Bind()
        // This ensures all properties are properly initialized for JavaScript interop
        options.ProviderOptions.Authentication.Authority = azureAdAuthority;
        options.ProviderOptions.Authentication.ClientId = azureAdClientId;
        options.ProviderOptions.Authentication.ValidateAuthority = builder.Configuration.GetValue<bool>("AzureAd:ValidateAuthority", true);
        
        // Configure redirect URIs
        var redirectUri = builder.Configuration["AzureAd:RedirectUri"];
        if (!string.IsNullOrEmpty(redirectUri))
        {
            options.ProviderOptions.Authentication.RedirectUri = redirectUri;
        }
        
        var postLogoutRedirectUri = builder.Configuration["AzureAd:PostLogoutRedirectUri"];
        if (!string.IsNullOrEmpty(postLogoutRedirectUri))
        {
            options.ProviderOptions.Authentication.PostLogoutRedirectUri = postLogoutRedirectUri;
        }
        
        var navigateToLoginRequestUrl = builder.Configuration.GetValue<bool>("AzureAd:NavigateToLoginRequestUrl", true);
        options.ProviderOptions.Authentication.NavigateToLoginRequestUrl = navigateToLoginRequestUrl;
        
        // Ensure KnownAuthorities is initialized as an empty list to prevent JavaScript errors
        // The MSAL.js library expects this to be an array, not null/undefined
        if (options.ProviderOptions.Authentication.KnownAuthorities == null)
        {
            options.ProviderOptions.Authentication.KnownAuthorities = new List<string>();
        }
        
        // Add known authorities from configuration if present
        var knownAuthorities = builder.Configuration.GetSection("AzureAd:KnownAuthorities").Get<string[]>();
        if (knownAuthorities != null && knownAuthorities.Length > 0)
        {
            foreach (var authority in knownAuthorities)
            {
                options.ProviderOptions.Authentication.KnownAuthorities.Add(authority);
            }
        }
        
        options.ProviderOptions.LoginMode = "redirect";
        
        // Ensure DefaultAccessTokenScopes is initialized
        if (options.ProviderOptions.DefaultAccessTokenScopes == null)
        {
            options.ProviderOptions.DefaultAccessTokenScopes = new List<string>();
        }
        
        // Add required scopes
        options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
        
        Console.WriteLine($"[Client] MSAL scopes configured: {string.Join(", ", options.ProviderOptions.DefaultAccessTokenScopes)}");
        Console.WriteLine($"[Client] Known authorities configured: {(options.ProviderOptions.Authentication.KnownAuthorities.Count > 0 ? string.Join(", ", options.ProviderOptions.Authentication.KnownAuthorities) : "none")}");
    });
}
else
{
    Console.WriteLine("[Client] No API Scope configured; skipping MSAL authentication setup.");
}

// Register HttpClient for API calls
builder.Services.AddHttpClient("fencemark.ApiService", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    
    if (!string.IsNullOrEmpty(apiScope))
    {
        handler.ConfigureHandler(
            authorizedUrls: new[] { apiBaseUrl },
            scopes: new[] { apiScope });
    }
    else
    {
        handler.ConfigureHandler(authorizedUrls: new[] { apiBaseUrl });
    }
    
    return handler;
});

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


await builder.Build().RunAsync();

