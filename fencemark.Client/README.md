# Fencemark.Client - Blazor WebAssembly Standalone Application

This is a standalone Blazor WebAssembly client application for Fencemark that authenticates users via Azure Entra External ID (CIAM) and communicates with the fencemark.ApiService backend API.

## Prerequisites

- .NET 10.0 SDK
- fencemark.ApiService running and accessible (optional for local UI development)
- Azure Entra External ID (CIAM) tenant configured (optional - authentication can be disabled for local development)

## Quick Start for Local Development

The application comes with `appsettings.Development.json` that includes real Azure AD development credentials. You have two options:

### Option 1: Run with Authentication (Real Azure AD)
```bash
dotnet run
```
The application will start at https://localhost:7173 with Azure AD authentication enabled using the development tenant.

### Option 2: Run without Authentication
To disable authentication for local UI development without requiring Azure AD sign-in:

1. Edit `appsettings.Development.json` and change the ClientId to the placeholder:
   ```json
   "ClientId": "00000000-0000-0000-0000-000000000000"
   ```
2. Run the application:
   ```bash
   dotnet run
   ```

**Note**: When authentication is disabled (placeholder ClientId), you can view the UI but authentication-protected features won't work.

## Configuration

### For Production or Full Authentication Testing

The application requires configuration for Azure AD authentication and API access. Create or update the `wwwroot/appsettings.json` file with your environment-specific values:

```json
{
  "AzureAd": {
    "Authority": "https://{AZURE_AD_TENANT_ID}.ciamlogin.com/{AZURE_AD_TENANT_ID}.onmicrosoft.com",
    "ClientId": "{AZURE_AD_CLIENT_ID}",
    "ValidateAuthority": true,
    "ApiScope": "api://{AZURE_AD_CLIENT_ID}/access_as_user"
  },
  "ApiBaseUrl": "{API_BASE_URL}",
  "ApiScope": "api://{AZURE_AD_CLIENT_ID}/access_as_user"
}
```

### Configuration Values

- **AZURE_AD_TENANT_ID**: Your Azure Entra External ID tenant ID (e.g., `yourtenant`)
- **AZURE_AD_CLIENT_ID**: The Client ID of the Azure AD App Registration for this WASM application
- **API_BASE_URL**: The base URL of the fencemark.ApiService API (e.g., `https://api.fencemark.com` or `https://localhost:62010`)

**Important**: The `appsettings.json` file is gitignored. Use `appsettings.template.json` as a reference for required settings.

### Azure AD App Registration Setup

1. Create an App Registration in Azure Entra External ID for the client application
2. Set the redirect URI to your application URL (e.g., `https://localhost:7173/authentication/login-callback` for development)
3. Enable implicit flow and ID tokens
4. Configure API permissions:
   - Add the `api://{API_CLIENT_ID}/access_as_user` scope (where API_CLIENT_ID is the client ID of your API's App Registration)
5. Update the `appsettings.json` with the Client ID

## Development

### Running Locally

```bash
dotnet run
```

The application will start at:
- HTTPS: https://localhost:7173
- HTTP: http://localhost:5173

### Building

```bash
dotnet build
```

### Publishing

```bash
dotnet publish -c Release -o ./publish
```

The published files will be in the `./publish/wwwroot` directory and can be deployed to any static web host.

## Architecture

### Authentication Flow

1. User clicks "Sign In" and is redirected to Azure Entra External ID
2. After successful authentication, Azure AD redirects back with an authorization code
3. MSAL.js exchanges the code for an access token with the `api://{clientId}/access_as_user` scope
4. The access token is automatically attached to all HTTP requests to the API via `AuthorizationMessageHandler`

### HTTP Client Configuration

The application uses a configured `HttpClient` that:
- Points to the API base URL specified in configuration
- Automatically attaches JWT access tokens to requests
- Uses the correct scope for token acquisition

### Components Structure

```
Components/
├── Layout/
│   └── MainLayout.razor          # Main layout with navigation
├── Pages/
│   ├── Home.razor                # Landing/home page
│   └── Authentication.razor      # Authentication flow handler
├── Routes.razor                  # Router configuration
└── _Imports.razor                # Global using statements
```

## Technologies

- **.NET 10.0**: Latest .NET framework
- **Blazor WebAssembly**: Client-side SPA framework
- **MSAL (Microsoft Authentication Library)**: Azure AD authentication
- **MudBlazor 8.15.0**: Material Design component library

## Security Considerations

- Access tokens are stored securely in browser storage by MSAL
- Tokens are automatically refreshed before expiration
- All API requests include the JWT bearer token
- The application validates tokens and enforces authorization

## Deployment

This standalone WASM application can be deployed to Azure Static Web Apps or other static hosting services.

### Azure Deployment with Authentication

The CI/CD pipeline (`.github/workflows/deploy-static-web-app.yml`) handles authentication configuration automatically:

1. **During Build**: The workflow creates `appsettings.json` with environment-specific values:
   - **Dev**: Uses real Azure AD credentials (hardcoded in workflow)
   - **Staging/Prod**: Uses credentials from GitHub secrets

2. **Build Step Example** (from workflow):
   ```bash
   cat > fencemark.Client/wwwroot/appsettings.json <<EOF
   {
     "AzureAd": {
       "Authority": "https://devfencemark.ciamlogin.com/",
       "ClientId": "5b204301-0113-4b40-bd2e-e0ef8be99f48",
       "ValidateAuthority": true
     },
     "ApiBaseUrl": "https://ca-fencemark-api-dev.azurecontainerapps.io"
   }
   EOF
   ```

3. **Published Output**: The `appsettings.json` file is included in the published wwwroot directory

**Important**: The `appsettings.json` file is gitignored and never committed. It's only created during the CI/CD build process for deployment.

### Deployment Targets

This standalone WASM application can be deployed to:
- Azure Static Web Apps
- Azure Blob Storage with Static Website hosting
- Any static file hosting service (Netlify, Vercel, etc.)
- IIS, Nginx, Apache with proper routing configuration

### Static Web Apps Configuration

For Azure Static Web Apps, ensure your `staticwebapp.config.json` includes proper routing rules for SPA:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html"
  }
}
```

## Troubleshooting

### Authentication Issues

- Verify the Azure AD app registration redirect URIs match your application URLs
- Check that the API scope is correctly configured in both the client and API app registrations
- Ensure the API is configured to accept tokens with the correct audience

### API Communication Issues

- Verify the `ApiBaseUrl` in `appsettings.json` is correct
- Check CORS configuration on the API to allow requests from your client domain
- Verify the API is running and accessible

### Build Issues

- Ensure .NET 10.0 SDK is installed
- Run `dotnet restore` to restore NuGet packages
- Check for any package version conflicts
