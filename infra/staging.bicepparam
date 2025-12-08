using './main.bicep'

// ============================================================================
// STAGING Environment Parameters
// ============================================================================

param environmentName = 'staging'
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')
param resourceGroupName = 'rg-fencemark-staging'

// ============================================================================
// Container Images
// ============================================================================

param apiServiceImage = ''
param webFrontendImage = ''

// ============================================================================
// Resource Scaling (Staging - moderate resources)
// ============================================================================

param apiServiceCpu = '0.5'
param apiServiceMemory = '1Gi'
param apiServiceMinReplicas = 1
param apiServiceMaxReplicas = 3

param webFrontendCpu = '0.5'
param webFrontendMemory = '1Gi'
param webFrontendMinReplicas = 1
param webFrontendMaxReplicas = 3

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'staging'
}

// ============================================================================
// Azure Entra External ID Configuration
// ============================================================================
// TODO: Create a separate Entra External ID tenant for staging

param enableEntraExternalId = true
param externalIdTenantId = '' // TODO: Set staging tenant ID
param externalIdPrimaryDomain = '' // TODO: Set staging domain
param customDomain = ''
param companyName = 'Fencemark Staging'
param privacyPolicyUrl = ''
param termsOfUseUrl = ''
param enableCustomBranding = true
param brandingBackgroundColor = '#0078D4'
param brandingBannerLogoUrl = ''
param brandingSquareLogoUrl = ''
param signInAudience = 'AzureADMyOrg'
