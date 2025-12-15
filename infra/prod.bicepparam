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
param customDomain = 'fencemark.com.au'
// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'prod'
}
