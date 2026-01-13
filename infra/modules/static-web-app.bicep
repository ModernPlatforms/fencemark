// ============================================================================
// Static Web App Module
// ============================================================================
// This module creates an Azure Static Web App using Azure Verified Modules
// for hosting the Blazor WASM application frontend.
// ============================================================================

@description('The name of the Static Web App')
param name string

@description('The location for the Static Web App')
param location string

@description('Tags to apply to the Static Web App')
param tags object = {}

@description('The SKU of the Static Web App (Free, Standard)')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Standard'

@description('The backend API URL for the Static Web App to link to')
param apiUrl string = ''

@description('Enable managed identity for the Static Web App')
param enableManagedIdentity bool = true

@description('Azure AD tenant ID for authentication')
param aadTenantId string = ''

@description('Azure AD client ID for authentication')
param aadClientId string = ''

@description('Azure AD instance URL')
param aadInstance string = ''

@description('Application Insights connection string for monitoring')
param appInsightsConnectionString string = ''

// ============================================================================
// Deploy Static Web App using AVM
// ============================================================================

module staticWebApp 'br/public:avm/res/web/static-site:0.9.3' = {
  name: 'staticWebApp-${name}'
  params: {
    name: name
    location: location
    tags: tags
    sku: sku
    
    // Enable managed identity
    managedIdentities: enableManagedIdentity ? {
      systemAssigned: true
    } : null
    
    // Configure build properties for Blazor WASM
    buildProperties: {
      apiLocation: ''
      appLocation: '/'
      outputLocation: 'wwwroot'
    }
    
    // Configure app settings for the Static Web App
    appSettings: union(
      // Always include these base settings
      {},
      // Add API backend URL if provided
      !empty(apiUrl) ? {
        BACKEND_API_URL: apiUrl
      } : {},
      // Add Azure AD settings if provided
      !empty(aadTenantId) && !empty(aadClientId) && !empty(aadInstance) ? {
        'AzureAd__TenantId': aadTenantId
        'AzureAd__ClientId': aadClientId
        'AzureAd__Instance': aadInstance
      } : {},
      // Add Application Insights if provided
      !empty(appInsightsConnectionString) ? {
        APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
      } : {}
    )
    
    // Enterprise features configuration (available in Standard SKU)
    enterpriseGradeCdnStatus: sku == 'Standard' ? 'Enabled' : 'Disabled'
    
    // Allow only production branch (deployment will be via GitHub Actions)
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
  }
}

// ============================================================================
// Custom Domain Configuration
// ============================================================================

// Note: Custom domain binding for Static Web Apps is typically done via Azure CLI
// or Portal after initial deployment. The domain validation must be completed first.
// This module creates the SWA, and custom domains can be added post-deployment.

// ============================================================================
// Outputs
// ============================================================================

@description('The resource ID of the Static Web App')
output resourceId string = staticWebApp.outputs.resourceId

@description('The name of the Static Web App')
output name string = staticWebApp.outputs.name

@description('The default hostname of the Static Web App')
output defaultHostname string = staticWebApp.outputs.defaultHostname

@description('The system-assigned managed identity principal ID')
output systemAssignedMIPrincipalId string = enableManagedIdentity ? (staticWebApp.outputs.?systemAssignedMIPrincipalId ?? '') : ''

@description('The Static Web App URL')
output url string = 'https://${staticWebApp.outputs.defaultHostname}'
