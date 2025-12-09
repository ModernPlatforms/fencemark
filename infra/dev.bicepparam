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
// Azure Entra External ID Authentication
// ============================================================================

// CIAM tenant configuration
// NOTE: Run ./infra/get-tenant-id.sh rg-fencemark-identity-dev to retrieve the tenant ID
param entraExternalIdTenantId = '' // TODO: Set this to the tenant ID from the CIAM deployment
param entraExternalIdClientId = '5b204301-0113-4b40-bd2e-e0ef8be99f48'
param entraExternalIdInstance = 'https://devfencemark.ciamlogin.com/'
param entraExternalIdDomain = 'devfencemark.onmicrosoft.com'

// Key Vault and Certificate
param keyVaultUrl = 'https://kv-ciambfwyw65gna5lu.vault.azure.net/'
param certificateName = 'dev-external-id-cert'

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'dev'
}
