using './main.bicep'

// ============================================================================
// PROD Environment Parameters
// ============================================================================

param environmentName = 'prod'
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')
param resourceGroupName = 'rg-fencemark-prod'

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
// Azure Entra External ID (CIAM) Configuration
// ============================================================================

param enableEntraExternalId = true
param ciamTenantName = 'fencemarkprod'
param ciamLocation = 'United States'
param ciamDisplayName = 'Fencemark Identity'
param ciamCountryCode = 'US'
param ciamSkuName = 'Standard'
param ciamSkuTier = 'A0'
param customDomain = '' // TODO: Set custom domain for production (e.g., login.fencemark.com)
