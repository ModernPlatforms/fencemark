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

// ============================================================================
// Azure Entra External ID Configuration
// ============================================================================

param enableEntraExternalId = true
param externalIdTenantId = '153c1433-2dfc-4a35-9aab-52219c3ca071'
param externalIdPrimaryDomain = 'devfencemark.onmicrosoft.com'
param customDomain = ''
param companyName = 'Fencemark Dev'
param privacyPolicyUrl = ''
param termsOfUseUrl = ''
param enableCustomBranding = true
param brandingBackgroundColor = '#0078D4'
param brandingBannerLogoUrl = ''
param brandingSquareLogoUrl = ''
param signInAudience = 'AzureADMyOrg'
