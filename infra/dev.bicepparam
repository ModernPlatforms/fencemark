using './main.bicep'

// ============================================================================
// DEV Environment Parameters
// ============================================================================

param environmentName = 'dev'
param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')

param resourceGroupName = 'rg-fencemark-dev'

// ============================================================================
// Container Images
// ============================================================================

param apiServiceImage = ''
param webFrontendImage = ''

// ============================================================================
// Resource Scaling (Dev - minimal resources)
// ============================================================================

param apiServiceCpu = '0.25'
param apiServiceMemory = '0.5Gi'
param apiServiceMinReplicas = 0
param apiServiceMaxReplicas = 2

param webFrontendCpu = '0.25'
param webFrontendMemory = '0.5Gi'
param webFrontendMinReplicas = 0
param webFrontendMaxReplicas = 2

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'dev'
}
