using fencemark.Client.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Authentication.WebAssembly.Msal;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root components
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

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
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    
    // Configure the default access token scope for the API
    // This scope should match what's configured in Azure AD and expected by the API
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        builder.Configuration["AzureAd:ApiScope"] ?? "api://{clientId}/access_as_user");
    
    // Configure login mode - redirect is recommended for WASM standalone apps
    options.ProviderOptions.LoginMode = "redirect";
});

// Configure HTTP client with automatic JWT token attachment
builder.Services.AddHttpClient("fencemark.ApiService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "{API_BASE_URL}");
})
.AddHttpMessageHandler(sp =>
{
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: new[] { builder.Configuration["ApiBaseUrl"] ?? "{API_BASE_URL}" },
            scopes: new[] { builder.Configuration["AzureAd:ApiScope"] ?? "api://{clientId}/access_as_user" });
    
    return handler;
});

// Register the default HttpClient for the API
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("fencemark.ApiService"));

await builder.Build().RunAsync();
