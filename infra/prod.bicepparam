using './main.bicep'

// ============================================================================
// PROD Environment Parameters
// ============================================================================

param environmentName = 'prod'
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')

// ============================================================================
// Container Images
// ============================================================================

param apiServiceImage = ''
param webFrontendImage = ''

// ============================================================================
// Resource Scaling (Prod - production-ready resources)
// ============================================================================

param apiServiceCpu = '1'
param apiServiceMemory = '2Gi'
param apiServiceMinReplicas = 2
param apiServiceMaxReplicas = 10

param webFrontendCpu = '1'
param webFrontendMemory = '2Gi'
param webFrontendMinReplicas = 2
param webFrontendMaxReplicas = 10

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'prod'
}

// ============================================================================
// Azure Entra External ID Configuration
// ============================================================================
// TODO: Create a separate Entra External ID tenant for production

param enableEntraExternalId = true
param externalIdTenantId = '' // TODO: Set production tenant ID
param externalIdPrimaryDomain = '' // TODO: Set production domain
param customDomain = '' // TODO: Set custom domain for production (e.g., login.fencemark.com)
param companyName = 'Fencemark'
param privacyPolicyUrl = '' // TODO: Set production privacy policy URL
param termsOfUseUrl = '' // TODO: Set production terms of use URL
param enableCustomBranding = true
param brandingBackgroundColor = '#0078D4'
param brandingBannerLogoUrl = ''
param brandingSquareLogoUrl = ''
param signInAudience = 'AzureADMyOrg'
