using './main.bicep'

// ============================================================================
// Environment Parameters
// ============================================================================

param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
param location = readEnvironmentVariable('AZURE_LOCATION', 'eastus')

// ============================================================================
// Container Images
// ============================================================================
// These values should be set after building and pushing container images
// to the Azure Container Registry. Example:
// param apiServiceImage = 'crXXXXXX.azurecr.io/fencemark-apiservice:latest'
// param webFrontendImage = 'crXXXXXX.azurecr.io/fencemark-webfrontend:latest'

param apiServiceImage = ''
param webFrontendImage = ''

// ============================================================================
// Resource Scaling
// ============================================================================

param apiServiceCpu = '0.25'
param apiServiceMemory = '0.5Gi'
param apiServiceMinReplicas = 0
param apiServiceMaxReplicas = 3

param webFrontendCpu = '0.25'
param webFrontendMemory = '0.5Gi'
param webFrontendMinReplicas = 0
param webFrontendMaxReplicas = 3

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: readEnvironmentVariable('AZURE_ENV_NAME', 'dev')
}

// ============================================================================
// Azure Entra External ID Configuration
// ============================================================================
// IMPORTANT: Before enabling, you must first create an Azure Entra External ID
// tenant manually through the Azure Portal. Then update the values below.
// To create a tenant:
// 1. Go to Azure Portal > Microsoft Entra External ID
// 2. Create a new External ID tenant
// 3. Note the Tenant ID and Primary Domain
// 4. Set enableEntraExternalId to true and update the parameters below

param enableEntraExternalId = true

// Replace with your actual Entra External ID tenant ID
// Example: '12345678-1234-1234-1234-123456789012'
param externalIdTenantId = '153c1433-2dfc-4a35-9aab-52219c3ca071'

// Replace with your Entra External ID primary domain
// Example: 'fencemark.onmicrosoft.com'
param externalIdPrimaryDomain = 'devfencemark.onmicrosoft.com'

// Optional: Custom domain for sign-in experience
// Example: 'login.fencemark.com'
// NOTE: Requires DNS configuration and domain verification
param customDomain = ''

// Branding Configuration
param companyName = 'Fencemark'
// TODO: Set to your actual privacy policy URL before deployment
param privacyPolicyUrl = ''
// TODO: Set to your actual terms of use URL before deployment
param termsOfUseUrl = ''

// Custom Branding Settings
param enableCustomBranding = true
param brandingBackgroundColor = '#0078D4' // Microsoft blue
param brandingBannerLogoUrl = '' // URL to banner logo (recommended: 1920x1080px PNG)
param brandingSquareLogoUrl = '' // URL to square logo (recommended: 240x240px PNG)

// Sign-in audience
// - AzureADMyOrg: Single tenant (recommended for External ID)
// - AzureADMultipleOrgs: Multi-tenant
// - AzureADandPersonalMicrosoftAccount: Multi-tenant + personal Microsoft accounts
param signInAudience = 'AzureADMyOrg'

