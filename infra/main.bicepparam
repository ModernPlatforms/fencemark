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
