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
