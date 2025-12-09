// ============================================================================
// Fencemark - Azure Entra External ID Tenant
// ============================================================================
// This is a STANDALONE deployment for creating an Azure Entra External ID
// (CIAM) tenant. Run this MANUALLY before or after main infrastructure.
//
// Usage:
//   az deployment sub create --location australiaeast --template-file entra-external-id.bicep --parameters entra-external-id.bicepparam
//
// Reference: https://cloudtips.nl/deploy-microsoft-entra-external-id-tenant-using-azure-bicep
// ============================================================================
// NOTES:
// - The tenant name must be globally unique (will become {name}.onmicrosoft.com)
// - Location must be one of: 'United States', 'Europe', 'Asia Pacific', 'Australia'
// - App registration must be done manually via Azure Portal after deployment
// - This deployment creates its own resource group for CIAM resources
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// Parameters
// ============================================================================

@description('Name of the environment (e.g., dev, staging, prod)')
param environmentName string

@description('Primary location for resources like Key Vault and Resource Group')
param location string

@description('Name of the resource group for CIAM resources')
param resourceGroupName string

@description('Tags to apply to all resources')
param tags object = {}

@description('The name of the CIAM tenant (will become {name}.onmicrosoft.com). Max 26 chars, alphanumeric only.')
@maxLength(26)
param ciamTenantName string

@description('The data residency location for the CIAM tenant. Can be one of: United States, Europe, Asia Pacific, Australia.')
@allowed([
  'United States'
  'Europe'
  'Asia Pacific'
  'Australia'
])
param ciamLocation string = 'Australia'

@description('The display name for the CIAM tenant')
param ciamDisplayName string = 'Fencemark Identity'

@description('Country code for data residency (e.g., US, GB, DE, AU)')
param ciamCountryCode string = 'AU'

@description('The SKU name for the CIAM tenant')
@allowed([
  'Base'
  'PremiumP1'
  'PremiumP2'
])
param ciamSkuName string = 'Base'

@description('The SKU tier for the CIAM tenant')
param ciamSkuTier string = 'A0'

@description('Custom domain name for sign-in experience (e.g., login.fencemark.com)')
param customDomain string = ''

@description('Redirect URI for the web frontend application (get this from main deployment output)')
param webFrontendRedirectUri string = ''

// ============================================================================
// Variables
// ============================================================================

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, resourceGroupName, environmentName))
var defaultTags = union(tags, {
  'azd-env-name': environmentName
  component: 'identity'
})

var keyVaultName = '${abbrs.keyVaultVaults}${take('ciam${resourceToken}', 17)}'
var ciamDomainName = '${ciamTenantName}.onmicrosoft.com'

// ============================================================================
// Resource Group
// ============================================================================

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: defaultTags
}

// ============================================================================
// Azure Entra External ID (CIAM) Tenant
// ============================================================================
// Deploys the CIAM directory using a module (resource requires resourceGroup scope)

module ciamDirectory './modules/ciam-directory.bicep' = {
  name: 'ciamDirectory'
  scope: rg
  params: {
    ciamDomainName: ciamDomainName
    ciamLocation: ciamLocation
    tags: defaultTags
    ciamSkuName: ciamSkuName
    ciamSkuTier: ciamSkuTier
    ciamCountryCode: ciamCountryCode
    ciamDisplayName: ciamDisplayName
  }
}

// ============================================================================
// Key Vault for Secrets
// ============================================================================
// Stores sensitive values like client secrets after manual app registration

module keyVault 'br/public:avm/res/key-vault/vault:0.11.0' = {
  name: 'entraKeyVault'
  scope: rg
  params: {
    name: keyVaultName
    location: location
    tags: defaultTags
    sku: 'standard'
    enablePurgeProtection: false // Set to true for production
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: true
  }
}

// ============================================================================
// DNS Zone for Custom Domain (Optional)
// ============================================================================
// Creates a DNS zone if custom domain is specified for domain verification

module dnsZone 'br/public:avm/res/network/dns-zone:0.5.4' = if (!empty(customDomain)) {
  name: 'entraDnsZone'
  scope: rg
  params: {
    name: customDomain
    location: 'global'
    tags: defaultTags
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The resource group name')
output resourceGroupName string = rg.name

@description('The tenant ID of the CIAM directory')
output tenantId string = ciamDirectory.outputs.tenantId

@description('The domain name of the CIAM tenant')
output domainName string = ciamDirectory.outputs.domainName

@description('The CIAM tenant name')
output ciamTenantName string = ciamTenantName

@description('The provisioning state of the CIAM directory')
output provisioningState string = ciamDirectory.outputs.provisioningState

@description('The resource ID of the CIAM directory')
output ciamDirectoryResourceId string = ciamDirectory.outputs.resourceId

@description('The custom domain (if configured)')
output customDomain string = customDomain

@description('The CIAM login URL')
output ciamLoginUrl string = 'https://${ciamTenantName}.ciamlogin.com'

@description('The authority URL for authentication')
output authorityUrl string = 'https://${ciamTenantName}.ciamlogin.com/${ciamDirectory.outputs.tenantId}'

@description('The name of the Key Vault storing secrets')
output keyVaultName string = keyVault.outputs.name

@description('The resource ID of the Key Vault')
output keyVaultResourceId string = keyVault.outputs.resourceId

@description('DNS zone name servers (if custom domain is configured)')
output dnsZoneNameServers array = dnsZone.?outputs.?nameServers ?? []

@description('OIDC configuration endpoint')
output oidcConfigurationEndpoint string = 'https://${ciamTenantName}.ciamlogin.com/${ciamDirectory.outputs.tenantId}/v2.0/.well-known/openid-configuration'

@description('Redirect URI to configure in app registration')
output redirectUri string = webFrontendRedirectUri

// ============================================================================
// Post-Deployment Steps (Manual)
// ============================================================================
// After deployment, create the app registration manually:
//
// 1. Go to Azure Portal > Entra External ID tenant ({ciamTenantName}.onmicrosoft.com)
// 2. App registrations > New registration
// 3. Name: fencemark-{environmentName}
// 4. Supported account types: Accounts in this organizational directory only
// 5. Redirect URI (Web): {webFrontendRedirectUri}
// 6. After creation, note the Application (client) ID
// 7. Create a client secret and store in Key Vault: {keyVaultName}
// 8. Update the Container App environment variables with the client ID and tenant ID
