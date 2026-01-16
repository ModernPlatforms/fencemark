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
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        options.ProviderOptions.LoginMode = "redirect";
        
        // Initialize DefaultAccessTokenScopes AFTER binding to ensure it exists
        if (options.ProviderOptions.DefaultAccessTokenScopes == null)
        {
            options.ProviderOptions.DefaultAccessTokenScopes = new List<string>();
        }
        
        // Ensure list has required scopes
        if (!options.ProviderOptions.DefaultAccessTokenScopes.Contains("https://graph.microsoft.com/User.Read"))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/User.Read");
        }
        
        if (!options.ProviderOptions.DefaultAccessTokenScopes.Contains(apiScope))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
        }
        
        Console.WriteLine($"[Client] MSAL scopes configured: {string.Join(", ", options.ProviderOptions.DefaultAccessTokenScopes)}");
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

// Register Graph HttpClient for separate Graph API calls
builder.Services.AddScoped(sp =>
{
    var authorizationMessageHandler =
        sp.GetRequiredService<AuthorizationMessageHandler>();
    authorizationMessageHandler.InnerHandler = new HttpClientHandler();
    authorizationMessageHandler.ConfigureHandler(
        authorizedUrls: new[] { "https://graph.microsoft.com/v1.0" },
        scopes: new[] { "User.Read" });

    return new HttpClient(authorizationMessageHandler);
});

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

