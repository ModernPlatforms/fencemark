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

// Configure HTTP client with automatic JWT token attachment
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:62010";
var apiScope = builder.Configuration["ApiScope"]; // Read from root level, not AzureAd section

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
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    if (!string.IsNullOrEmpty(apiScope))
    {
        options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
    }
    options.ProviderOptions.LoginMode = "redirect";
});

await builder.Build().RunAsync();
