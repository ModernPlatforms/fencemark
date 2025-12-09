using './entra-external-id.bicep'

// ============================================================================
// PROD - Azure Entra External ID (CIAM) Deployment
// ============================================================================
// Deploy manually:
//   az deployment sub create --location australiaeast --template-file entra-external-id.bicep --parameters entra-external-id.prod.bicepparam
// ============================================================================

param environmentName = 'prod'
param location = 'australiaeast'
param resourceGroupName = 'rg-fencemark-identity-prod'

// ============================================================================
// CIAM Tenant Configuration
// ============================================================================

param ciamTenantName = 'fencemark'
param ciamLocation = 'Australia'
param ciamDisplayName = 'Fencemark Identity'
param ciamCountryCode = 'AU'
param ciamSkuName = 'Base'
param ciamSkuTier = 'A0'

// ============================================================================
// Optional Configuration
// ============================================================================

param customDomain = '' // TODO: Set custom domain (e.g., login.fencemark.com)
param webFrontendRedirectUri = '' // Set after main deployment

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'prod'
  component: 'identity'
}
