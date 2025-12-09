using './entra-external-id.bicep'

// ============================================================================
// DEV - Azure Entra External ID (CIAM) Deployment
// ============================================================================
// Deploy manually:
//   az deployment sub create --location australiaeast --template-file entra-external-id.bicep --parameters entra-external-id.dev.bicepparam
// ============================================================================

param environmentName = 'dev'
param location = 'australiaeast'
param resourceGroupName = 'rg-fencemark-identity-dev'

// ============================================================================
// CIAM Tenant Configuration
// ============================================================================

param ciamTenantName = 'devfencemark'
param ciamLocation = 'Australia'
param ciamDisplayName = 'FenceMark Dev'
param ciamCountryCode = 'AU'
param ciamSkuName = 'Base'
param ciamSkuTier = 'A0'

// ============================================================================
// Optional Configuration
// ============================================================================

param customDomain = ''
param webFrontendRedirectUri = '' // Set after main deployment

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'dev'
  component: 'identity'
}
