# Azure Static Web Apps Setup for Blazor WASM

This document describes the Azure Static Web Apps infrastructure for hosting the Fencemark Blazor WASM client application.

## Overview

The Static Web App (SWA) infrastructure is deployed using Azure Verified Modules (AVM) and provides:

- **Standard SKU**: Enterprise-grade features including custom domains, enhanced performance, and CDN
- **Managed Identity**: System-assigned managed identity for secure access to Azure resources
- **API Backend Integration**: Linked to the existing API Container App for backend services
- **Azure AD Authentication**: Configured with Entra External ID (CIAM) settings
- **Application Insights**: Integrated monitoring and telemetry
- **GitHub Actions Deployment**: Deployment token stored securely in Key Vault

## Infrastructure Components

### 1. Static Web App Resource
- **Naming Convention**: `stapp-fencemark-{environment}` (following Azure naming best practices)
- **SKU**: Standard (configurable via parameter)
- **Location**: Australia East (follows main infrastructure)
- **Managed Identity**: System-assigned enabled by default

### 2. Networking
- **Default Hostname**: Automatically provisioned by Azure (e.g., `{name}.azurestaticapps.net`)
- **Custom Domains**: Configured via parameters:
  - Dev: `app.dev.fencemark.com.au`
  - Staging: `app.stgfencemark.modernplatforms.dev`
  - Prod: `app.fencemark.com.au`

### 3. Backend Integration
- **API Backend URL**: Links to API Container App (`https://{apiService.fqdn}`)
- **Configuration**: Passed as `BACKEND_API_URL` app setting

### 4. Security & Access
- **RBAC Role Assignments**:
  - App Configuration Data Reader (for central App Config)
  - Key Vault Secrets User (for accessing secrets)
- **Deployment Token**: Stored in Key Vault as `swa-deployment-token`

## Deployment

### Prerequisites
1. Azure subscription with appropriate permissions
2. Existing API Container App infrastructure deployed
3. Central App Configuration store deployed
4. Azure Entra External ID configured

### Deployment Steps

1. **Deploy the infrastructure** (one-time):
   ```bash
   cd /home/runner/work/fencemark/fencemark/infra
   
   # For dev environment
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters dev.bicepparam
   
   # For staging environment
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters staging.bicepparam
   
   # For prod environment
   az deployment sub create \
     --location australiaeast \
     --template-file main.bicep \
     --parameters prod.bicepparam
   ```

2. **Retrieve the deployment token**:
   ```bash
   # Get deployment token from Key Vault
   az keyvault secret show \
     --vault-name {keyVaultName} \
     --name swa-deployment-token \
     --query value -o tsv
   ```

3. **Configure GitHub Actions**:
   - Add the deployment token as a GitHub secret (e.g., `AZURE_STATIC_WEB_APPS_API_TOKEN_DEV`)
   - Configure the GitHub Actions workflow to deploy the Blazor WASM app

### Deployment Outputs

The deployment provides the following outputs:
- `staticWebAppName`: Name of the Static Web App resource
- `staticWebAppUrl`: Public URL of the Static Web App
- `staticWebAppHostname`: Default hostname
- `staticWebAppIdentityPrincipalId`: Managed identity principal ID
- `staticWebAppResourceId`: Azure resource ID

## Configuration

### App Settings

The Static Web App includes the following app settings:

| Setting | Description | Source |
|---------|-------------|--------|
| `BACKEND_API_URL` | API backend endpoint | Container App FQDN |
| `AzureAd__TenantId` | Azure AD tenant ID | Entra External ID config |
| `AzureAd__ClientId` | Azure AD client ID | Entra External ID config |
| `AzureAd__Instance` | Azure AD instance URL | Entra External ID config |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection | Application Insights |

### Build Configuration

The Static Web App is configured for Blazor WASM:
- **App Location**: `/` (root of repository)
- **Output Location**: `wwwroot` (Blazor WASM output)
- **API Location**: Empty (API is separate Container App)

## Custom Domain Setup

Custom domains are configured via the parameters but need to be validated:

1. **Add domain validation** (automatically created by Bicep):
   - DNS TXT record for domain verification
   - DNS CNAME record pointing to the Static Web App

2. **Bind the custom domain** (manual step via Azure Portal or CLI):
   ```bash
   az staticwebapp hostname set \
     --name stapp-fencemark-{environment} \
     --resource-group rg-fencemark-{environment} \
     --hostname app.{environment}.fencemark.com.au
   ```

## Monitoring & Diagnostics

- **Application Insights**: Integrated via connection string
- **Metrics**: Available in Azure Portal under Static Web App resource
- **Logs**: Accessible via Application Insights

## Cost Management

- **Standard SKU**: ~$9/month base + bandwidth costs
- **Enterprise CDN**: Included with Standard SKU
- **Bandwidth**: Pay-as-you-go based on usage

To optimize costs:
- Use Free SKU for development (set `staticWebAppSku = 'Free'` in dev.bicepparam)
- Monitor bandwidth usage via Azure Cost Management
- Implement CDN caching strategies

## Troubleshooting

### Common Issues

1. **Deployment fails with network error**:
   - Ensure network connectivity to Azure
   - Check firewall rules and proxy settings

2. **Managed identity access denied**:
   - Verify RBAC role assignments are deployed
   - Check role assignment propagation (can take up to 15 minutes)

3. **Custom domain not working**:
   - Verify DNS records are correctly configured
   - Check domain validation status in Azure Portal

## Next Steps

1. **Configure GitHub Actions workflow** for automated deployments
2. **Set up custom domain** and SSL certificate
3. **Configure routing rules** for SPA behavior
4. **Set up staging environments** using Static Web App environments feature
5. **Monitor performance** and optimize CDN caching

## References

- [Azure Static Web Apps Documentation](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure Verified Modules - Static Site](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/web/static-site)
- [Blazor WebAssembly Deployment](https://docs.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly)
