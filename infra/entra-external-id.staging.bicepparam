using './entra-external-id.bicep'

// ============================================================================
// STAGING - Azure Entra External ID (CIAM) Deployment
// ============================================================================
// Deploy manually:
//   az deployment sub create --location australiaeast --template-file entra-external-id.bicep --parameters entra-external-id.staging.bicepparam
// ============================================================================

param environmentName = 'staging'
param location = 'australiaeast'
param resourceGroupName = 'rg-fencemark-identity-staging'

// ============================================================================
// CIAM Tenant Configuration
// ============================================================================

param ciamTenantName = 'stgfencemark'
param ciamLocation = 'Australia'
param ciamDisplayName = 'Fencemark Staging'
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
  environment: 'staging'
  component: 'identity'
}
