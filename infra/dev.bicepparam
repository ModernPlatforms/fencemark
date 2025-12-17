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
param externalidRg = 'rg-fencemark-identity-dev' 

// ============================================================================
// Azure Entra External ID Authentication
// ============================================================================
// NOTE: These values are stored in Azure App Configuration during deployment.
// Applications use managed identity to read from App Config at runtime.
// CIAM tenant configuration
// NOTE: Run ./infra/get-tenant-id.sh rg-fencemark-identity-dev to retrieve the tenant ID
param entraExternalIdTenantId = '153c1433-2dfc-4a35-9aab-52219c3ca071' 
param entraExternalIdClientId = '5b204301-0113-4b40-bd2e-e0ef8be99f48'
param entraExternalIdInstance = 'https://devfencemark.ciamlogin.com/'
param entraExternalIdDomain = 'devfencemark.onmicrosoft.com'

// Key Vault and Certificate
param keyVaultUrl = 'https://kv-ciambfwyw65gna5lu.vault.azure.net/'
param certificateName = 'dev-external-id-cert'
param customDomain = 'devfencemark.modernplatforms.dev'

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'dev'
}
