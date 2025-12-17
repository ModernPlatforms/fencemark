// ============================================================================
// Azure App Configuration Module
// ============================================================================
// This module creates an Azure App Configuration store for centralized
// configuration management across environments using labels.
// ============================================================================

@description('Name of the App Configuration store')
param name string

@description('Location for the App Configuration store')
param location string

@description('Tags to apply to the resource')
param tags object = {}

@description('SKU for App Configuration')
@allowed([
  'free'
  'standard'
])
param sku string = 'standard'

@description('Enable public network access')
param publicNetworkAccess bool = true

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    publicNetworkAccess: publicNetworkAccess ? 'Enabled' : 'Disabled'
    disableLocalAuth: false // Allow key-based access for initial setup, enable RBAC for production
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The resource ID of the App Configuration store')
output resourceId string = appConfig.id

@description('The name of the App Configuration store')
output name string = appConfig.name

@description('The endpoint of the App Configuration store')
output endpoint string = appConfig.properties.endpoint
