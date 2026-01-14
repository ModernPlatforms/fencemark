// ============================================================================
// Azure Static Web Apps Module
// ============================================================================

@description('Name of the Static Web App')
param name string

@description('Location for the Static Web App')
param location string

@description('Tags to apply to resources')
param tags object = {}

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

@description('Custom domain name (optional)')
param customDomainName string = ''

// ============================================================================
// Static Web App
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'Custom'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// ============================================================================
// Custom Domain (Standard SKU only)
// ============================================================================

resource customDomain 'Microsoft.Web/staticSites/customDomains@2023-12-01' = if (!empty(customDomainName)) {
  parent: staticWebApp
  name: customDomainName
  properties: {}
}

// ============================================================================
// Outputs
// ============================================================================

@description('Static Web App name')
output name string = staticWebApp.name

@description('Static Web App resource ID')
output resourceId string = staticWebApp.id

@description('Static Web App default hostname')
output defaultHostname string = staticWebApp.properties.defaultHostname

@description('Static Web App deployment token')
@secure()
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
