# Azure Entra External ID Setup Guide

This guide provides step-by-step instructions for setting up Azure Entra External ID (Customer Identity Access Management) for the Fencemark application.

## Overview

Azure Entra External ID (formerly Azure AD B2C for external identities) is Microsoft's customer identity and access management (CIAM) solution. It provides:

- **Customer Sign-up and Sign-in**: Self-service registration and authentication
- **Custom Branding**: Branded sign-in experiences with company logos and colors
- **Social Identity Providers**: Sign-in with Microsoft, Google, Facebook, etc.
- **Custom Domains**: Use your own domain for authentication pages
- **Security**: Enterprise-grade security with MFA, conditional access, and threat protection

## Prerequisites

1. **Azure Subscription**: Active Azure subscription with appropriate permissions
2. **Global Administrator**: Azure AD Global Administrator role (for tenant creation)
3. **Custom Domain** (optional): A registered domain name if using custom domains
4. **DNS Access** (optional): Ability to modify DNS records for custom domain verification

## Step 1: Create Azure Entra External ID Tenant

Azure Entra External ID tenants must be created manually through the Azure Portal or Azure CLI. Infrastructure as Code (Bicep) handles the configuration, not creation.

### Option A: Azure Portal

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID** > **Overview**
3. Click **Manage tenants** > **Create**
4. Select **External ID for customers**
5. Configure:
   - **Organization name**: `Fencemark`
   - **Initial domain name**: `fencemark` (results in `fencemark.onmicrosoft.com`)
   - **Country/Region**: Select your primary region
   - **Subscription**: Choose your Azure subscription
   - **Resource group**: Select or create a resource group
   - **Location**: Choose a location close to your users
6. Click **Review + create** > **Create**
7. Wait for the tenant to be created (2-5 minutes)
8. Note the **Tenant ID** and **Primary Domain** for later use

### Option B: Azure CLI

```bash
# Install Azure CLI if needed
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli

# Login to Azure
az login

# Create a new Entra External ID tenant
az rest \
  --method POST \
  --uri https://management.azure.com/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.AzureActiveDirectory/ciamDirectories/{directory-name}?api-version=2023-05-17-preview \
  --body '{
    "location": "United States",
    "sku": {
      "name": "Standard",
      "tier": "A0"
    },
    "properties": {
      "createTenantProperties": {
        "displayName": "Fencemark",
        "countryCode": "US"
      }
    }
  }'
```

## Step 2: Configure Bicep Parameters

After creating the tenant, update the parameters in `main.bicepparam`:

```bicep
// Enable Azure Entra External ID
param enableEntraExternalId = true

// Your tenant ID (from Step 1)
param externalIdTenantId = '12345678-1234-1234-1234-123456789012'

// Your primary domain (from Step 1)
param externalIdPrimaryDomain = 'fencemark.onmicrosoft.com'

// Optional: Custom domain
param customDomain = 'login.fencemark.com'

// Branding configuration
param companyName = 'Fencemark'
param privacyPolicyUrl = 'https://fencemark.com/privacy'
param termsOfUseUrl = 'https://fencemark.com/terms'
param enableCustomBranding = true
param brandingBackgroundColor = '#0078D4'

// Optional: Logo URLs (must be publicly accessible or base64 encoded)
param brandingBannerLogoUrl = 'https://yourstorage.blob.core.windows.net/logos/banner.png'
param brandingSquareLogoUrl = 'https://yourstorage.blob.core.windows.net/logos/square.png'
```

## Step 3: Grant Permissions to Managed Identity

The Bicep deployment creates a managed identity that needs permissions to configure Microsoft Graph resources.

### Assign Required Permissions

1. Go to **Azure Portal** > **Microsoft Entra ID**
2. Switch to your **External ID tenant** (use the directory switcher)
3. Navigate to **Enterprise applications**
4. Find the managed identity created by the deployment: `id-entra-{resourceToken}`
5. Assign the following API permissions:
   - `Application.ReadWrite.All` (to manage app registrations)
   - `Directory.ReadWrite.All` (to configure branding)
   - `Domain.ReadWrite.All` (to manage custom domains)

### PowerShell Script to Assign Permissions

```powershell
# Connect to the External ID tenant
Connect-AzureAD -TenantId "your-external-id-tenant-id"

# Get the managed identity service principal
$managedIdentityDisplayName = "id-entra-{resourceToken}"
$sp = Get-AzureADServicePrincipal -Filter "displayName eq '$managedIdentityDisplayName'"

# Get Microsoft Graph service principal
$graphSp = Get-AzureADServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"

# Required permissions
$permissions = @(
    "Application.ReadWrite.All",    # 1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9
    "Directory.ReadWrite.All",      # 19dbc75e-c2e2-444c-a770-ec69d8559fc7
    "Domain.ReadWrite.All"          # 7e05723c-0bb0-42da-be95-ae9f08a6e53c
)

# Assign each permission
foreach ($permission in $permissions) {
    $appRole = $graphSp.AppRoles | Where-Object { $_.Value -eq $permission }
    
    New-AzureADServiceAppRoleAssignment `
        -ObjectId $sp.ObjectId `
        -PrincipalId $sp.ObjectId `
        -ResourceId $graphSp.ObjectId `
        -Id $appRole.Id
}
```

## Step 4: Deploy Infrastructure

Deploy the Bicep template with Azure CLI or Azure Developer CLI:

### Using Azure CLI

```bash
# Login to Azure
az login

# Set your subscription
az account set --subscription "your-subscription-id"

# Create resource group (if not exists)
az group create \
  --name rg-fencemark-dev \
  --location eastus

# Deploy the template
az deployment group create \
  --resource-group rg-fencemark-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam

# IMPORTANT: After deployment completes, update Container App settings
# This step is required because of deployment dependencies
./infra/update-entra-settings.sh rg-fencemark-dev main
```

### Using Azure Developer CLI (azd)

```bash
# Login to Azure
azd auth login

# Deploy infrastructure
azd provision

# IMPORTANT: After deployment completes, update Container App settings
# Get the resource group name from azd
RESOURCE_GROUP=$(azd env get-values | grep AZURE_RESOURCE_GROUP | cut -d'=' -f2)
./infra/update-entra-settings.sh "$RESOURCE_GROUP" main
```

### Why Two Steps?

The deployment process requires two steps due to circular dependencies:

1. **First deployment**: Creates all infrastructure including Web Frontend and Entra External ID configuration
2. **Update step**: Sets the Application (Client) ID in the Web Frontend environment variables

This happens because:
- Entra External ID needs the Web Frontend URL for redirect URI configuration
- Web Frontend needs the Application ID from Entra External ID for authentication

The update script automatically retrieves the Application ID from deployment outputs and updates the Container App.

## Step 5: Custom Domain Configuration (Optional)

If you specified a custom domain, you need to verify domain ownership:

### Verify Custom Domain

1. Deploy the infrastructure first (it creates a DNS zone)
2. Get the DNS name servers from deployment outputs:
   ```bash
   az deployment group show \
     --resource-group rg-fencemark-dev \
     --name main \
     --query properties.outputs.entraExternalIdDnsNameServers.value
   ```
3. Update your domain registrar to use these name servers
4. Wait for DNS propagation (up to 48 hours, typically faster)
5. In Azure Portal, go to your External ID tenant > **Custom domain names**
6. Add your custom domain and verify using DNS TXT record
7. Set as primary domain for sign-in

### DNS TXT Record for Verification

Azure will provide a TXT record like:
```
Name: @
Type: TXT
Value: MS=ms12345678
```

Add this to your DNS zone or domain registrar.

## Step 6: Configure Custom Branding

The deployment script configures basic branding. For advanced branding:

### Upload Custom Logos

1. Go to **Azure Portal** > Your External ID tenant
2. Navigate to **Company branding**
3. Click **Configure**
4. Upload logos:
   - **Banner logo**: 1920x1080px PNG (recommended)
   - **Square logo**: 240x240px PNG (recommended)
   - **Favicon**: 32x32px PNG or ICO
5. Set background color, button colors, and customize text
6. Save changes

### Brand Templates

Create consistent branding with these specifications:

- **Banner Logo**: Horizontal logo, transparent background, PNG format, max 500KB
- **Square Logo**: Company icon, transparent background, PNG format, max 500KB
- **Background Color**: Use hex color codes (e.g., `#0078D4`)
- **Sign-in Page URL**: Custom domain (e.g., `https://login.fencemark.com`)

## Step 7: Test Authentication

After deployment, test the authentication flow:

### Test Application Registration

1. Get the application (client) ID from deployment outputs:
   ```bash
   az deployment group show \
     --resource-group rg-fencemark-dev \
     --name main \
     --query properties.outputs.entraExternalIdApplicationId.value
   ```

2. Navigate to the web frontend URL:
   ```bash
   az deployment group show \
     --resource-group rg-fencemark-dev \
     --name main \
     --query properties.outputs.webFrontendUrl.value
   ```

3. Click **Sign In** and test the authentication flow
4. Verify:
   - Custom branding appears
   - Sign-in works correctly
   - Token is received and validated
   - User profile is accessible

### Test User Sign-up

1. Click **Sign up** on the sign-in page
2. Create a test user account
3. Verify email confirmation (if enabled)
4. Complete profile information
5. Sign in with the new account

## Step 8: Production Considerations

Before going to production:

### Security

- [ ] Enable **Purge Protection** on Key Vault (set in Bicep)
- [ ] Enable **Multi-Factor Authentication** (MFA) in Entra External ID
- [ ] Configure **Conditional Access** policies
- [ ] Set up **Identity Protection** and risk-based policies
- [ ] Enable **Audit Logging** and integrate with SIEM
- [ ] Review and restrict **API Permissions** to minimum required

### Scalability

- [ ] Test with production load (MAU - Monthly Active Users)
- [ ] Configure **Token Lifetime** policies
- [ ] Set up **Custom Attributes** for user profiles
- [ ] Plan for **Session Management**
- [ ] Configure **CORS** policies for your domains

### Compliance

- [ ] Review **Data Residency** requirements
- [ ] Configure **Privacy Statement** and **Terms of Use**
- [ ] Enable **Age Gating** if required
- [ ] Configure **Consent Framework**
- [ ] Document **Data Processing** agreements

### Monitoring

- [ ] Configure **Azure Monitor** alerts for authentication failures
- [ ] Set up **Sign-in Logs** retention
- [ ] Create **Dashboard** for user activity
- [ ] Monitor **Token Usage** and quotas
- [ ] Set up **Alerts** for suspicious activity

## Troubleshooting

### Common Issues

#### Deployment Script Fails

**Issue**: Deployment script times out or fails with permissions error

**Solution**:
1. Verify managed identity has required Graph API permissions
2. Check if tenant ID is correct
3. Ensure you're connected to the right tenant
4. Review deployment script logs in Azure Portal

#### Custom Domain Verification Fails

**Issue**: Domain verification TXT record not found

**Solution**:
1. Wait for DNS propagation (up to 48 hours)
2. Use `nslookup` or `dig` to verify DNS records
3. Ensure TXT record is correctly formatted
4. Check if name servers are properly configured

#### Application Registration Not Found

**Issue**: Application not created or not visible

**Solution**:
1. Check deployment logs for errors
2. Verify managed identity permissions
3. Manually create app registration if needed
4. Ensure correct tenant is selected

#### Sign-in Redirect Fails

**Issue**: After authentication, redirect to app fails

**Solution**:
1. Verify redirect URI matches exactly (including protocol and port)
2. Check application configuration in Azure Portal
3. Ensure Web Frontend FQDN is correct
4. Review CORS settings

## Additional Resources

- [Azure Entra External ID Documentation](https://learn.microsoft.com/en-us/entra/external-id/)
- [Custom Branding Guide](https://learn.microsoft.com/en-us/entra/external-id/how-to-customize-branding)
- [Custom Domains](https://learn.microsoft.com/en-us/entra/external-id/how-to-custom-domain)
- [Authentication Flows](https://learn.microsoft.com/en-us/entra/external-id/authentication-flows)
- [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview)
- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)

## Support

For issues or questions:
- Review this guide and Azure documentation
- Check deployment logs in Azure Portal
- Review Microsoft Entra External ID diagnostic logs
- Contact Azure Support for tenant-specific issues
