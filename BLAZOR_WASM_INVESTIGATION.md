# Blazor WebAssembly Client-Only Static App Investigation

**Date:** January 2026  
**Status:** Investigation Complete  
**Recommendation:** Feasible with Moderate Effort

---

## Executive Summary

This document investigates migrating the Fencemark application from its current **Blazor Server** architecture to a **Blazor WebAssembly (WASM) client-only** application hosted as a static web app. The investigation finds that this migration is **feasible** but requires **moderate effort** across multiple areas including authentication, deployment infrastructure, and API client patterns.

### Key Findings

✅ **Feasibility:** Migration is technically feasible  
⚠️ **Effort:** Moderate - approximately 2-3 weeks of development  
✅ **Benefits:** Improved scalability, reduced server costs, better offline capabilities  
⚠️ **Trade-offs:** Larger initial download, loss of server-side rendering benefits  
✅ **Security:** Can maintain current security posture with proper implementation

---

## Current Architecture Analysis

### Current State: Blazor Server

The Fencemark application currently uses:

- **Frontend:** Blazor Server with Interactive Server Components
- **Backend:** ASP.NET Core Minimal API (fencemark.ApiService)
- **Hosting:** Azure Container Apps via .NET Aspire orchestration
- **Authentication:** Azure Entra External ID (CIAM) with OpenID Connect
- **Session Management:** Server-side with cookie-based authentication
- **Database:** SQL Server (via Aspire SQL resource)
- **UI Framework:** MudBlazor components

**Architecture Diagram:**
```
┌─────────────────────────────────────────────────────────┐
│                   Current Architecture                  │
│                                                         │
│  User Browser                                           │
│       ↓                                                 │
│  ┌─────────────────────┐                                │
│  │   Blazor Server     │←──── SignalR WebSocket         │
│  │   (Interactive)     │                                │
│  └─────────┬───────────┘                                │
│            │                                            │
│            ↓ HTTP Calls                                 │
│  ┌─────────────────────┐                                │
│  │    API Service      │                                │
│  │  (Minimal API)      │                                │
│  └─────────┬───────────┘                                │
│            │                                            │
│            ↓                                            │
│  ┌─────────────────────┐                                │
│  │   SQL Server DB     │                                │
│  └─────────────────────┘                                │
└─────────────────────────────────────────────────────────┘
```

**Key Dependencies:**
- `Microsoft.AspNetCore.Components.Server` - Blazor Server hosting
- `Microsoft.Identity.Web` - Authentication integration
- `Microsoft.Identity.Web.UI` - Auth UI components
- `Microsoft.AspNetCore.DataProtection` - Cookie/token protection
- `MudBlazor` - UI component library
- SignalR connection for server-client communication

### Communication Pattern

**Current:** Server-side rendering with SignalR for UI updates
- User interacts with browser
- Events sent to server via SignalR
- Server processes and renders HTML diff
- Diff sent back to browser for DOM update

---

## Proposed Architecture: Blazor WebAssembly

### Target State

- **Frontend:** Blazor WebAssembly standalone app
- **Backend:** Same ASP.NET Core Minimal API (no changes needed)
- **Hosting:** Azure Static Web Apps OR Azure Blob Storage + Azure CDN
- **Authentication:** Azure Entra External ID with MSAL.js / Microsoft.Authentication.WebAssembly.Msal
- **Session Management:** Client-side tokens (JWT) in browser storage
- **Database:** SQL Server (unchanged)
- **UI Framework:** MudBlazor (compatible with WASM)

**Architecture Diagram:**
```
┌─────────────────────────────────────────────────────────┐
│                  Proposed Architecture                  │
│                                                         │
│  User Browser                                           │
│  ┌─────────────────────┐                                │
│  │  Blazor WASM App    │←──── Downloaded once           │
│  │  (Static Files)     │      (HTML/CSS/JS/WASM)       │
│  │  - Runs in browser  │                                │
│  │  - All UI logic     │                                │
│  └─────────┬───────────┘                                │
│            │                                            │
│            ↓ HTTP API Calls (with JWT)                  │
│  ┌─────────────────────┐                                │
│  │    API Service      │←──── Authentication via        │
│  │  (Minimal API)      │      Bearer token (JWT)       │
│  └─────────┬───────────┘                                │
│            │                                            │
│            ↓                                            │
│  ┌─────────────────────┐                                │
│  │   SQL Server DB     │                                │
│  └─────────────────────┘                                │
└─────────────────────────────────────────────────────────┘
```

### Communication Pattern

**Proposed:** Client-side rendering with direct HTTP API calls
- User interacts with browser
- Blazor WASM processes event locally
- Updates DOM directly (instant feedback)
- Makes HTTP calls to API when data needed
- API returns JSON (not HTML)

---

## Required Changes

### 1. Project Structure Changes

#### Create New Blazor WASM Project

**Option A: New Standalone Project (Recommended)**
```bash
dotnet new blazorwasm -o fencemark.Client
```

**Option B: Convert Existing Project**
- Change SDK to `Microsoft.NET.Sdk.BlazorWebAssembly`
- Update dependencies
- Remove server-specific packages

#### Project File Changes

**Current (fencemark.Web.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Server" />
    <PackageReference Include="Microsoft.Identity.Web" />
    <PackageReference Include="Microsoft.Identity.Web.UI" />
  </ItemGroup>
</Project>
```

**Proposed (fencemark.Client.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.*" />
    <PackageReference Include="Microsoft.Authentication.WebAssembly.Msal" Version="10.0.*" />
    <PackageReference Include="MudBlazor" Version="8.15.0" />
  </ItemGroup>
  <ServiceWorker Include="wwwroot\service-worker.js" />
</Project>
```

**Key Differences:**
- SDK changes from `Microsoft.NET.Sdk.Web` to `Microsoft.NET.Sdk.BlazorWebAssembly`
- Remove server-side packages (`Microsoft.Identity.Web`, `Microsoft.AspNetCore.Components.Server`)
- Add WASM-specific packages (`Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.Authentication.WebAssembly.Msal`)
- Optionally add PWA support with service worker

### 2. Authentication Changes

This is the **most significant change** required.

#### Current: Server-Side OpenID Connect

**Current Flow:**
1. User clicks "Sign In"
2. Redirected to Azure Entra External ID
3. After auth, redirected back to server
4. Server validates token, creates session
5. Cookie set for subsequent requests
6. Server maintains session state

**Current Code (Program.cs):**
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options => {
        builder.Configuration.Bind("AzureAd", options);
        options.SaveTokens = true;
    });
```

#### Proposed: Client-Side MSAL

**Proposed Flow:**
1. User clicks "Sign In"
2. MSAL.js redirects to Azure Entra External ID
3. After auth, redirected back to client app
4. MSAL.js receives token, stores in browser
5. Token attached to API requests as Bearer header
6. No server-side session needed

**Proposed Code (Program.cs in Client project):**
```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Authentication.WebAssembly.Msal;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) 
});

// Configure MSAL authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        builder.Configuration["AzureAd:Scopes"] ?? "api://default/access_as_user"
    );
});

// Add MudBlazor
builder.Services.AddMudServices();

// Register API clients
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<OrganizationApiClient>();
builder.Services.AddScoped<FenceApiClient>();
builder.Services.AddScoped<GateApiClient>();
builder.Services.AddScoped<ComponentApiClient>();
builder.Services.AddScoped<JobApiClient>();
builder.Services.AddScoped<FenceSegmentApiClient>();
builder.Services.AddScoped<GatePositionApiClient>();

await builder.Build().RunAsync();
```

**Configuration (wwwroot/appsettings.json):**
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/{tenant-id}",
    "ClientId": "{client-id}",
    "ValidateAuthority": true,
    "Scopes": "api://fencemark-api/access_as_user"
  },
  "ApiBaseUrl": "https://api.fencemark.com"
}
```

#### API Changes Required

The API service needs to accept **Bearer tokens** instead of cookies:

**Current API Authentication:**
```csharp
// Uses shared cookie authentication
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);
```

**Proposed API Authentication (add JWT Bearer):**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.Audience = builder.Configuration["AzureAd:ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
```

**Note:** The API can support **both** cookie and JWT authentication for backward compatibility:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ })
    .AddCookie(IdentityConstants.ApplicationScheme, options => { /* Cookie config */ });
```

### 3. Component Changes

Most Blazor components will require **minimal changes** because Blazor WASM uses the same component model.

#### Changes Needed:

**Render Mode Removal:**
```razor
<!-- Current (Server with Interactive mode) -->
@rendermode InteractiveServer

<!-- Proposed (WASM - no render mode needed) -->
<!-- Remove @rendermode entirely -->
```

**Authentication State:**
```razor
<!-- Current -->
<AuthorizeView>
    <Authorized>
        <p>Hello, @context.User.Identity?.Name!</p>
    </Authorized>
    <NotAuthorized>
        <a href="MicrosoftIdentity/Account/SignIn">Sign In</a>
    </NotAuthorized>
</AuthorizeView>

<!-- Proposed -->
<AuthorizeView>
    <Authorized>
        <p>Hello, @context.User.Identity?.Name!</p>
    </Authorized>
    <NotAuthorized>
        <a href="authentication/login">Sign In</a>
    </NotAuthorized>
</AuthorizeView>
```

**App.razor Changes:**
```razor
<!-- Current -->
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- head content -->
</head>
<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>

<!-- Proposed -->
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

**Add Authentication Component:**
```razor
@page "/authentication/{action}"
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication

<RemoteAuthenticatorView Action="@Action" />

@code {
    [Parameter] public string? Action { get; set; }
}
```

### 4. API Client Changes

#### Current: Server-Side with Cookie Forwarding

```csharp
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Forwards cookie from browser to API
        var cookies = httpContext.Request.Cookies;
        // ... forward cookies
        return await base.SendAsync(request, cancellationToken);
    }
}
```

#### Proposed: Client-Side with Token Attachment

```csharp
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;
    
    public AuthorizationMessageHandler(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestRequest, CancellationToken cancellationToken)
    {
        var tokenResult = await _tokenProvider.RequestAccessToken();
        
        if (tokenResult.TryGetToken(out var token))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token.Value);
        }
        
        return await base.SendAsync(request, cancellationToken);
    }
}
```

**Register Handler:**
```csharp
builder.Services.AddHttpClient("API", client => 
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
```

**Built-in Alternative:**
```csharp
// Blazor provides a built-in handler
builder.Services.AddHttpClient("API", client => 
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped<BaseAddressAuthorizationMessageHandler>();
```

### 5. Infrastructure and Deployment Changes

This is a **major architectural change**.

#### Option A: Azure Static Web Apps (Recommended)

**Advantages:**
- ✅ Built specifically for static sites with APIs
- ✅ Integrated authentication support
- ✅ Automatic SSL/TLS certificates
- ✅ Global CDN distribution
- ✅ Staging environments with every PR
- ✅ GitHub Actions integration
- ✅ Can link to existing Azure Functions or Container Apps API

**Architecture:**
```
Azure Static Web App
├── Static Web App Resource
│   ├── Blazor WASM files (wwwroot/*)
│   ├── _framework/*.wasm
│   ├── _framework/*.dll.br (compressed)
│   └── index.html
└── Linked API
    └── Azure Container App (existing fencemark.ApiService)
```

**Bicep Configuration:**
```bicep
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: 'swa-fencemark-${environment}'
  location: location
  sku: {
    name: 'Standard'  // Free tier available, Standard for custom domains
    tier: 'Standard'
  }
  properties: {
    repositoryUrl: 'https://github.com/ModernPlatforms/fencemark'
    branch: 'main'
    buildProperties: {
      appLocation: 'fencemark.Client'
      apiLocation: ''  // Empty if using external API
      outputLocation: 'wwwroot'
    }
    enterpriseGradeCdnStatus: 'Enabled'
  }
}

// Link to existing Container App API
resource apiLink 'Microsoft.Web/staticSites/linkedBackends@2023-01-01' = {
  parent: staticWebApp
  name: 'api-link'
  properties: {
    backendResourceId: containerApp.id
    region: location
  }
}
```

**GitHub Actions Workflow:**
```yaml
name: Deploy Blazor WASM to Azure Static Web Apps

on:
  push:
    branches: [main]
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches: [main]

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: true
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Build Blazor WASM
        run: |
          dotnet publish fencemark.Client/fencemark.Client.csproj \
            -c Release \
            -o publish
      
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "publish/wwwroot"
          api_location: ""
          output_location: ""
```

#### Option B: Azure Blob Storage + CDN

**Advantages:**
- ✅ Lower cost for simple static hosting
- ✅ Full control over CDN configuration
- ✅ Can use existing infrastructure patterns

**Disadvantages:**
- ⚠️ More complex setup
- ⚠️ Manual SSL certificate management
- ⚠️ No built-in staging environments

**Bicep Configuration:**
```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stfencemark${environment}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    staticWebsite: {
      enabled: true
      indexDocument: 'index.html'
      errorDocument404Path: 'index.html'  // SPA routing
    }
  }
}

resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: 'cdn-fencemark-${environment}'
  location: 'global'
  sku: {
    name: 'Standard_Microsoft'
  }
}

resource cdnEndpoint 'Microsoft.Cdn/profiles/endpoints@2023-05-01' = {
  parent: cdnProfile
  name: 'fencemark-${environment}'
  location: 'global'
  properties: {
    originHostHeader: storageAccount.properties.primaryEndpoints.web
    origins: [
      {
        name: 'storage'
        properties: {
          hostName: replace(replace(storageAccount.properties.primaryEndpoints.web, 'https://', ''), '/', '')
        }
      }
    ]
    isHttpAllowed: false
    isHttpsAllowed: true
    queryStringCachingBehavior: 'IgnoreQueryString'
    isCompressionEnabled: true
    contentTypesToCompress: [
      'application/wasm'
      'application/javascript'
      'text/css'
      'text/html'
      'application/json'
    ]
  }
}
```

**Deployment Script:**
```bash
#!/bin/bash
# Deploy Blazor WASM to Blob Storage

# Build the app
dotnet publish fencemark.Client/fencemark.Client.csproj -c Release -o ./publish

# Upload to blob storage
az storage blob upload-batch \
  --account-name stfencemarkprod \
  --destination '$web' \
  --source ./publish/wwwroot \
  --overwrite \
  --content-cache-control "public, max-age=31536000, immutable" \
  --pattern "*.wasm" \
  --pattern "*.dll" \
  --pattern "*.js"

# Purge CDN cache
az cdn endpoint purge \
  --resource-group rg-fencemark-prod \
  --profile-name cdn-fencemark-prod \
  --name fencemark-prod \
  --content-paths "/*"
```

#### Option C: Keep Container Apps (Not Recommended)

You *could* keep using Azure Container Apps, but this loses most benefits of the WASM migration:
- ❌ Still paying for always-on containers
- ❌ Not leveraging CDN for static assets
- ❌ Higher latency (no edge distribution)
- ❌ More complex than necessary

**Only use if:** You need server-side features that aren't available in static hosting.

### 6. AppHost / Aspire Changes

The Aspire AppHost configuration needs significant changes:

**Current:**
```csharp
var webFrontend = builder.AddProject<Projects.fencemark_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WaitFor(apiService)
    .WithReference(apiService);
```

**Proposed Option 1 (Static Web App - remove from Aspire):**
```csharp
// Remove web frontend from Aspire entirely
// Static Web App is deployed separately via GitHub Actions

var apiService = builder.AddProject<Projects.fencemark_ApiService>("apiservice")
    .WaitFor(sqldb)
    .WithReference(sqldb)
    .WithExternalHttpEndpoints();  // Expose publicly for WASM app
```

**Proposed Option 2 (Keep for local dev only):**
```csharp
// For local development, serve WASM files
var webFrontend = builder.AddProject<Projects.fencemark_Client>("blazorwasm")
    .WithExternalHttpEndpoints();

// Or use npm/dotnet watch for hot reload during dev
var webFrontend = builder.AddNpmApp("blazorwasm", "../fencemark.Client")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();
```

---

## Security Implications

### Current Security Model

- **Authentication:** Server-side OpenID Connect
- **Session:** HttpOnly cookies (not accessible to JS)
- **CSRF Protection:** Anti-forgery tokens
- **XSS Protection:** Server-side rendering limits XSS surface
- **Token Storage:** Tokens stored server-side
- **Data Protection:** Azure Key Vault for key storage

### WASM Security Model

- **Authentication:** Client-side MSAL.js
- **Session:** Tokens in browser storage (sessionStorage/localStorage)
- **CSRF Protection:** Not needed (no cookies)
- **XSS Protection:** Higher risk - tokens in JS-accessible storage
- **Token Storage:** Browser storage (encrypted by browser)
- **Data Protection:** Not applicable (no server-side encryption keys)

### Security Recommendations

1. **Use Short-Lived Tokens**
   - Access tokens: 1 hour maximum
   - Refresh tokens: Implement token refresh flow
   
2. **Implement Token Refresh**
   ```csharp
   // MSAL handles this automatically
   var tokenResult = await tokenProvider.RequestAccessToken(
       new AccessTokenRequestOptions 
       { 
           Scopes = new[] { "api://fencemark/access_as_user" } 
       });
   ```

3. **Content Security Policy (CSP)**
   ```html
   <meta http-equiv="Content-Security-Policy" 
         content="default-src 'self'; 
                  script-src 'self' 'wasm-unsafe-eval'; 
                  style-src 'self' 'unsafe-inline';">
   ```

4. **Subresource Integrity (SRI)**
   - Azure Static Web Apps handles this automatically
   - For manual deployment, generate SRI hashes:
   ```bash
   openssl dgst -sha384 -binary file.js | openssl base64 -A
   ```

5. **API Security**
   - **Always validate tokens** on the API side
   - **Never trust client-side validation**
   - Implement proper CORS policies:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowBlazorWasm", policy =>
       {
           policy.WithOrigins("https://fencemark.azurestaticapps.net")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
       });
   });
   ```

6. **Sensitive Data**
   - **Never** include secrets in WASM app
   - Configuration in `appsettings.json` is **publicly accessible**
   - Only include non-sensitive config (API URLs, client IDs)

### Security Comparison

| Aspect | Blazor Server | Blazor WASM |
|--------|---------------|-------------|
| Token Storage | Server-side (more secure) | Browser storage (less secure) |
| XSS Risk | Low (tokens not in JS) | Higher (tokens in JS) |
| CSRF Risk | Requires mitigation | Not applicable |
| Source Code Visibility | Hidden on server | Visible in browser |
| API Calls | Server-to-server | Client-to-server |
| Network Inspection | Harder to inspect | Easy to inspect in DevTools |

**Mitigation:** Despite higher XSS risk, WASM can be **equally secure** with proper implementation:
- Use MSAL.js security features
- Implement CSP headers
- Short-lived tokens with refresh
- Always validate on server
- Use HTTPS everywhere

---

## Performance Implications

### Initial Load Time

**Current (Blazor Server):**
- Initial page: ~50-100 KB (HTML + minimal JS)
- SignalR connection: ~10 KB
- Total: **~100 KB for first load**
- Interactive after: ~1-2 seconds

**Proposed (Blazor WASM):**
- Blazor framework: ~2-3 MB (compressed to ~800 KB with Brotli)
- Application DLLs: ~500 KB - 1 MB (compressed to ~200-400 KB)
- MudBlazor: ~500 KB (compressed to ~150 KB)
- Total: **~3-4 MB uncompressed, ~1.5 MB compressed**
- Interactive after: ~3-5 seconds (first load), instant thereafter

**Optimization Techniques:**
1. **Lazy Loading**
   ```csharp
   @code {
       [Inject] private IComponentActivator ComponentActivator { get; set; }
       
       private Type? _componentType;
       
       protected override async Task OnInitializedAsync()
       {
           var assembly = await AssemblyLoader.LoadAsync("Heavy.Module.dll");
           _componentType = assembly.GetType("Heavy.Module.Component");
       }
   }
   ```

2. **AOT Compilation** (Ahead-of-Time)
   ```xml
   <PropertyGroup>
     <RunAOTCompilation>true</RunAOTCompilation>
   </PropertyGroup>
   ```
   - Reduces download size by ~30%
   - Increases build time significantly
   - Improves runtime performance

3. **Trimming**
   ```xml
   <PropertyGroup>
     <PublishTrimmed>true</PublishTrimmed>
     <TrimMode>link</TrimMode>
   </PropertyGroup>
   ```
   - Automatically enabled in .NET 10
   - Removes unused code
   - Can reduce size by 40-50%

4. **Compression**
   - Brotli compression (automatic in Azure Static Web Apps)
   - Reduces WASM/DLL size by 60-70%

### Runtime Performance

**Blazor Server:**
- Every interaction: Network round-trip to server
- Latency: ~50-200ms per interaction
- Bandwidth: Lower (only diffs sent)
- CPU: Server processes all logic

**Blazor WASM:**
- Interactions: Instant (runs in browser)
- Latency: 0ms for UI updates, only API calls have latency
- Bandwidth: Higher initial load, lower ongoing
- CPU: Client processes all logic

### Scalability

**Blazor Server:**
- Connections: One SignalR connection per user
- Memory: ~1-2 MB per connection
- 1000 concurrent users = ~2 GB RAM + CPU
- Vertical scaling needed (more powerful servers)

**Blazor WASM:**
- Connections: None (except API calls)
- Memory: Static files cached on CDN
- 1000 concurrent users = negligible server resources
- Horizontal scaling via CDN (automatic)
- API only scales based on API call volume

### Cost Comparison (Estimated for 10,000 active users)

**Current (Blazor Server on Container Apps):**
- Container Apps: ~$200-400/month (2-4 instances)
- Database: ~$50-100/month
- **Total: ~$250-500/month**

**Proposed (Blazor WASM on Static Web Apps):**
- Static Web Apps Standard: ~$9/month (or Free tier)
- Container Apps (API only): ~$50-100/month (smaller instances)
- Database: ~$50-100/month
- **Total: ~$100-200/month**

**Savings: ~$150-300/month (60% reduction)**

---

## Offline Capabilities

### Progressive Web App (PWA) Support

Blazor WASM can easily become a PWA:

**Add Service Worker:**
```javascript
// service-worker.js
const CACHE_NAME = 'fencemark-v1';
const urlsToCache = [
  '/',
  '/index.html',
  '/_framework/blazor.webassembly.js',
  // ... other resources
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

self.addEventListener('fetch', event => {
  event.respondWith(
    caches.match(event.request)
      .then(response => response || fetch(event.request))
  );
});
```

**Manifest File:**
```json
{
  "name": "Fencemark",
  "short_name": "Fencemark",
  "start_url": "/",
  "display": "standalone",
  "theme_color": "#0078d4",
  "background_color": "#ffffff",
  "icons": [
    {
      "src": "icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

**Enable in Project:**
```xml
<PropertyGroup>
  <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
</PropertyGroup>
<ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />
```

**Benefits:**
- ✅ Install on mobile/desktop like native app
- ✅ Offline access to cached content
- ✅ Background sync when online
- ✅ Push notifications (future feature)

**Not Possible with Blazor Server** (requires active connection)

---

## Development Experience

### Local Development

**Current (Blazor Server):**
```bash
# Run entire app via Aspire
dotnet run --project fencemark.AppHost

# Hot reload works
# Server-side debugging works
# Full .NET debugging experience
```

**Proposed (Blazor WASM):**
```bash
# Option 1: Run WASM with API via Aspire
dotnet run --project fencemark.AppHost

# Option 2: Run WASM standalone with hot reload
cd fencemark.Client
dotnet watch run

# Option 3: Use SWA CLI for local development
swa start ./publish/wwwroot --api-devserver-url https://localhost:7001
```

**Debugging:**
- Browser DevTools debugger works
- Source maps for C# code
- Set breakpoints in .cs files
- Step through code in browser
- Slightly different experience than server debugging

### CI/CD Changes

**Current Pipeline:**
```yaml
- Build Blazor Server
- Build API
- Deploy both to Container Apps
```

**Proposed Pipeline:**
```yaml
- Build Blazor WASM
- Publish to wwwroot
- Deploy to Azure Static Web Apps (or Blob + CDN)
- Build API (unchanged)
- Deploy API to Container Apps (unchanged)
```

**New Workflow File:**
```yaml
name: Deploy Static Web App

on:
  push:
    branches: [main]
    paths:
      - 'fencemark.Client/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Publish Blazor WASM
        run: |
          dotnet publish fencemark.Client -c Release -o publish
      
      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: "upload"
          app_location: "publish/wwwroot"
```

---

## Migration Effort Estimation

### Detailed Task Breakdown

| Task | Effort | Complexity | Risk |
|------|--------|------------|------|
| **1. Create new Blazor WASM project** | 2 hours | Low | Low |
| **2. Migrate components (no render mode)** | 4 hours | Low | Low |
| **3. Setup MSAL authentication** | 8 hours | Medium | Medium |
| **4. Update API to accept JWT tokens** | 6 hours | Medium | Medium |
| **5. Migrate API clients to use tokens** | 4 hours | Medium | Low |
| **6. Update App.razor and routing** | 3 hours | Low | Low |
| **7. Configure Azure Static Web Apps** | 4 hours | Medium | Medium |
| **8. Update Bicep infrastructure** | 6 hours | Medium | Medium |
| **9. Update GitHub Actions workflows** | 4 hours | Low | Low |
| **10. Configure CORS on API** | 2 hours | Low | Low |
| **11. Test authentication flow** | 8 hours | Medium | High |
| **12. Test all features** | 16 hours | Low | High |
| **13. Performance testing** | 4 hours | Low | Low |
| **14. Security review** | 4 hours | Medium | High |
| **15. Documentation updates** | 4 hours | Low | Low |
| **16. Deployment and monitoring** | 4 hours | Low | Medium |
| **Buffer (20%)** | 16 hours | - | - |

**Total Estimated Effort: 99 hours (~2.5 weeks for 1 developer)**

### Phased Approach

**Phase 1: Proof of Concept (1 week)**
- Create new WASM project
- Migrate 2-3 key components
- Setup basic authentication
- Deploy to test environment
- **Goal:** Validate technical feasibility

**Phase 2: Full Migration (1 week)**
- Migrate all components
- Complete authentication
- Update all API clients
- Full testing

**Phase 3: Production Deployment (3-5 days)**
- Infrastructure setup
- Security hardening
- Performance optimization
- Production deployment
- Monitoring setup

---

## Risks and Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Authentication complexity** | High | Medium | Use Microsoft's MSAL library, thorough testing |
| **Larger initial download** | Medium | High | AOT compilation, lazy loading, compression |
| **Browser compatibility** | Medium | Low | WASM supported in all modern browsers |
| **Debugging difficulties** | Low | Medium | Use browser DevTools, source maps |
| **Security vulnerabilities** | High | Low | Follow MSAL best practices, CSP headers |
| **Lost SEO benefits** | Medium | Low | Not applicable (private B2B app) |
| **API CORS issues** | Low | Medium | Proper CORS configuration, testing |
| **Token management bugs** | Medium | Medium | Use MSAL refresh flow, monitoring |

---

## Decision Matrix

### When to Choose Blazor WASM

✅ **Choose WASM if:**
- You want better scalability (10,000+ users)
- You want lower hosting costs
- Offline capabilities are valuable
- Initial load time is acceptable (3-5 seconds)
- Users have modern browsers
- Mobile/PWA support is desired
- You want to reduce server load

❌ **Avoid WASM if:**
- You need instant first load (<1 second)
- SEO is critical (public-facing site)
- Users on very slow networks
- You need server-side rendering for accessibility
- Application is extremely large (>10 MB even compressed)
- Team lacks JavaScript debugging skills

### For Fencemark Specifically

**Recommendation: ✅ PROCEED with migration**

**Reasons:**
1. **B2B Application:** No SEO concerns
2. **Authenticated Users:** All users will tolerate 3-5 second load once
3. **Cost Savings:** Significant reduction in infrastructure costs
4. **Scalability:** Better prepared for growth
5. **Modern Architecture:** Aligns with industry trends
6. **Offline Potential:** Contractors could work offline

**Concerns Addressed:**
- Initial load: Mitigated by compression, AOT, lazy loading
- Authentication: MSAL.js is mature and well-documented
- Security: Proper implementation maintains security
- Migration effort: Moderate (2-3 weeks) is acceptable

---

## Recommendations

### Immediate Actions

1. **Approve Migration:** Greenlight the project for next sprint
2. **Spike Work:** Dedicate 1-2 days for POC with authentication
3. **Azure Resources:** Provision Static Web App in development environment
4. **Team Training:** 2-hour session on WASM and MSAL.js

### Implementation Plan

**Week 1: Foundation**
- Create new fencemark.Client project
- Setup MSAL authentication
- Migrate core components
- API token support
- Local development environment

**Week 2: Complete Migration**
- Migrate all remaining components
- Full feature testing
- Performance optimization
- Security review

**Week 3: Production Deployment**
- Infrastructure as Code updates
- Production deployment
- Monitoring setup
- Documentation
- Team training on new architecture

### Success Criteria

- [ ] All features work identically to current version
- [ ] Authentication flow is smooth and secure
- [ ] Initial load time < 5 seconds on 10 Mbps connection
- [ ] Subsequent page loads < 100ms
- [ ] No security vulnerabilities in penetration testing
- [ ] API responds to JWT tokens correctly
- [ ] CORS configured correctly
- [ ] Monitoring shows healthy metrics
- [ ] Documentation updated
- [ ] Team comfortable with new architecture

---

## Alternative Considerations

### Hybrid Approach: Blazor United (.NET 8+)

.NET 10 supports **Blazor Web App** which can mix Server and WASM rendering:

```razor
@page "/dashboard"
@rendermode InteractiveAuto  <!-- Auto-switches between Server and WASM -->

<!-- OR specify per component -->
<AdminPanel @rendermode="InteractiveServer" />  <!-- Heavy computation on server -->
<Charts @rendermode="InteractiveWebAssembly" />  <!-- Fast client-side rendering -->
```

**Pros:**
- Best of both worlds
- Server-side for initial load
- WASM for interactive components
- Seamless transition

**Cons:**
- More complex architecture
- Still requires SignalR infrastructure
- Higher hosting costs than pure WASM

**Recommendation:** Consider for future if pure WASM has issues

---

## Conclusion

### Summary of Findings

The migration from Blazor Server to Blazor WebAssembly client-only static app is **technically feasible** and **financially beneficial** for the Fencemark application. The primary challenges are in authentication migration and infrastructure changes, both of which have well-established patterns and tooling.

### Key Takeaways

1. **Feasibility: ✅ High** - All technical blockers have known solutions
2. **Effort: ⚠️ Moderate** - ~2-3 weeks of focused development
3. **Cost Savings: ✅ Significant** - ~60% reduction in hosting costs
4. **Performance: ✅ Better** - After initial load, all interactions are instant
5. **Scalability: ✅ Excellent** - Horizontal scaling via CDN
6. **Security: ✅ Maintained** - With proper MSAL implementation
7. **Offline: ✅ Bonus** - PWA capabilities unlock new features

### Final Recommendation

**✅ PROCEED with migration to Blazor WebAssembly**

The benefits significantly outweigh the costs, and the moderate effort (2-3 weeks) is justified by long-term cost savings, better scalability, and improved user experience.

**Recommended Hosting:** Azure Static Web Apps (easiest path, best DX)

**Recommended Timeline:**
- **Week 1:** POC and foundation
- **Week 2:** Full migration
- **Week 3:** Production deployment
- **Week 4:** Monitoring and optimization

### Next Steps

1. **Approve Project:** Get stakeholder buy-in
2. **Allocate Resources:** Assign 1-2 developers for 3 weeks
3. **Create Spike:** 2-day POC with authentication
4. **Proceed or Pivot:** Based on POC results, commit to full migration

---

## Appendices

### A. Reference Links

- [Blazor WebAssembly Documentation](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-webassembly)
- [Azure Static Web Apps Documentation](https://learn.microsoft.com/azure/static-web-apps/)
- [MSAL.js for Blazor](https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-microsoft-entra-id)
- [Blazor WASM Performance Best Practices](https://learn.microsoft.com/aspnet/core/blazor/performance)
- [Progressive Web Apps with Blazor](https://learn.microsoft.com/aspnet/core/blazor/progressive-web-app)

### B. Sample Projects

- [Blazor WASM with Authentication](https://github.com/Azure-Samples/blazor-cosmos-wasm)
- [Azure Static Web Apps Samples](https://github.com/Azure-Samples/awesome-azure-static-web-apps)
- [Blazor WASM Hosted Template](https://github.com/dotnet/aspnetcore/tree/main/src/ProjectTemplates/Web.ProjectTemplates/content/BlazorWebAssembly-CSharp)

### C. Configuration Templates

See inline code samples throughout document for:
- Project file configurations
- Program.cs setup
- Authentication configuration
- Bicep templates
- GitHub Actions workflows
- CORS setup

---

**Document Version:** 1.0  
**Last Updated:** January 2026  
**Author:** GitHub Copilot  
**Reviewers:** [Pending]
