# Deployment Guide for Authentication Setup

This guide provides step-by-step instructions for deploying the Fencemark application with Azure Entra External ID (CIAM) authentication.

## Overview

The application uses Azure Entra External ID for customer identity and access management (CIAM). The infrastructure is already configured with:

- **Client ID**: `5b204301-0113-4b40-bd2e-e0ef8be99f48`
- **Certificate**: Stored in Key Vault at `https://kv-ciambfwyw65gna5lu.vault.azure.net/`
- **Certificate Name**: `dev-external-id-cert`
- **CIAM Tenant**: `devfencemark.onmicrosoft.com`

## Prerequisites

1. **Azure Subscription**: Active Azure subscription with appropriate permissions
2. **Azure CLI**: Install from [https://docs.microsoft.com/cli/azure/install-azure-cli](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. **Authentication**: Logged in to Azure CLI (`az login`)
4. **CIAM Tenant**: Already deployed (confirmed by the presence of the certificate in Key Vault)

## Step 1: Retrieve the Tenant ID

The tenant ID is required for the deployment but was not provided in the issue. You can retrieve it using the helper script:

```bash
# Run the script to get the tenant ID
./infra/get-tenant-id.sh rg-fencemark-identity-dev
```

The script will output the tenant ID. Update `infra/dev.bicepparam` with this value:

```bicep
param entraExternalIdTenantId = '<TENANT_ID_FROM_SCRIPT>'
```

### Manual Method (Alternative)

If the script doesn't work, you can manually retrieve the tenant ID:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID**
3. Switch to your CIAM tenant directory: `devfencemark.onmicrosoft.com`
4. On the Overview page, copy the **Tenant ID**
5. Update `infra/dev.bicepparam` with this value

## Step 2: Verify Configuration

Ensure all authentication parameters are correctly set in `infra/dev.bicepparam`:

```bicep
// Azure Entra External ID Authentication
param entraExternalIdTenantId = '<TENANT_ID>'  // Update this
param entraExternalIdClientId = '5b204301-0113-4b40-bd2e-e0ef8be99f48'
param entraExternalIdInstance = 'https://devfencemark.ciamlogin.com/'
param entraExternalIdDomain = 'devfencemark.onmicrosoft.com'

// Key Vault and Certificate
param keyVaultUrl = 'https://kv-ciambfwyw65gna5lu.vault.azure.net/'
param certificateName = 'dev-external-id-cert'
```

## Step 3: Grant Managed Identity Access to Key Vault

The Web Frontend needs access to the Key Vault to retrieve the certificate for authentication.

### Option A: Using Azure Portal

1. Go to [Azure Portal](https://portal.azure.com) > Key Vaults
2. Select the Key Vault: `kv-ciambfwyw65gna5lu`
3. Navigate to **Access policies** or **Access control (IAM)**
4. Add a new access policy/role assignment:
   - **Principal**: The managed identity of the Web Frontend Container App
   - **Certificate permissions**: Get, List
   - **Secret permissions**: Get, List (for certificate private key access)

### Option B: Using Azure CLI

After deployment, run these commands:

```bash
# Get the Web Frontend managed identity principal ID
WEB_IDENTITY=$(az containerapp show \
    --name ca-webfrontend-<resourceToken> \
    --resource-group rg-fencemark-dev \
    --query "identity.principalId" \
    -o tsv)

# Grant access to Key Vault
az keyvault set-policy \
    --name kv-ciambfwyw65gna5lu \
    --object-id "$WEB_IDENTITY" \
    --certificate-permissions get list \
    --secret-permissions get list
```

## Step 4: Deploy Infrastructure

Deploy using the GitHub Actions workflow or Azure CLI:

### Option A: GitHub Actions (Recommended)

1. Push your changes to the `main` branch or trigger manually:
   ```bash
   git push origin <your-branch>
   ```

2. The deployment workflow will automatically:
   - Build the solution
   - Run tests
   - Deploy to Azure Container Apps

### Option B: Azure CLI

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "<your-subscription-id>"

# Deploy infrastructure
az deployment sub create \
    --location australiaeast \
    --template-file ./infra/main.bicep \
    --parameters ./infra/dev.bicepparam
```

## Step 5: Configure Redirect URIs in CIAM

After deployment, you need to configure the redirect URIs in the CIAM app registration:

1. Get the Web Frontend URL from deployment outputs:
   ```bash
   az deployment sub show \
       --name main \
       --query "properties.outputs.webFrontendUrl.value" \
       -o tsv
   ```

2. Go to [Azure Portal](https://portal.azure.com)
3. Switch to the CIAM tenant: `devfencemark.onmicrosoft.com`
4. Navigate to **App registrations**
5. Find the app with Client ID: `5b204301-0113-4b40-bd2e-e0ef8be99f48`
6. Go to **Authentication**
7. Add the following redirect URIs:
   - `https://<web-frontend-fqdn>/signin-oidc`
   - `https://<web-frontend-fqdn>/signout-callback-oidc`
8. Save changes

## Step 6: Verify Deployment

1. Navigate to the Web Frontend URL (from Step 5)
2. You should see the application home page
3. Click on any authenticated page or login link
4. You should be redirected to the Entra External ID login page
5. Test authentication with a CIAM user account

### Check Application Logs

If authentication is not working:

```bash
# View Web Frontend logs
az containerapp logs show \
    --name ca-webfrontend-<resourceToken> \
    --resource-group rg-fencemark-dev \
    --follow

# Check for authentication errors
az containerapp logs show \
    --name ca-webfrontend-<resourceToken> \
    --resource-group rg-fencemark-dev \
    --follow \
    | grep -i "auth\|certificate\|keyvault"
```

## Troubleshooting

### Issue: "Certificate not found in Key Vault"

**Solution:**
1. Verify the certificate exists in Key Vault:
   ```bash
   az keyvault certificate show \
       --vault-name kv-ciambfwyw65gna5lu \
       --name dev-external-id-cert
   ```
2. Check that the managed identity has access to the Key Vault (Step 3)

### Issue: "Unable to authenticate"

**Solution:**
1. Verify all environment variables are correctly set in the Container App
2. Check the redirect URIs are configured in CIAM (Step 5)
3. Ensure the CIAM tenant ID is correct
4. Verify the client ID matches the app registration

### Issue: "Access denied to Key Vault"

**Solution:**
1. Verify the managed identity has been granted access (Step 3)
2. Ensure the Key Vault firewall allows access from Container Apps
3. Check that the managed identity is enabled on the Container App

## Next Steps

After successful deployment:

1. **Configure Custom Branding**: Update the CIAM sign-in experience with your branding
2. **Enable MFA**: Configure multi-factor authentication for enhanced security
3. **Set up Monitoring**: Configure Application Insights and alerts
4. **Production Deployment**: Repeat these steps for staging and production environments

## Additional Resources

- [Azure Entra External ID Documentation](https://learn.microsoft.com/entra/external-id/)
- [Microsoft.Identity.Web Documentation](https://learn.microsoft.com/azure/active-directory/develop/microsoft-identity-web)
- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [ENTRA-EXTERNAL-ID-SETUP.md](./ENTRA-EXTERNAL-ID-SETUP.md) - Detailed setup guide
