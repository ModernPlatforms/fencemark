// ============================================================================
// Central Azure App Configuration
// ============================================================================
// This template deploys a centralized Azure App Configuration store that
// serves all environments (dev, staging, prod) using labels.
// This is deployed once to a dedicated resource group.
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// Parameters
// ============================================================================

@minLength(1)
@description('Primary location for resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

@description('Name of the central configuration resource group')
param centralConfigResourceGroupName string = 'rg-fencemark-central-config'

@description('Name prefix for the App Configuration store')
param appConfigNamePrefix string = 'appcs-fencemark'

// ============================================================================
// Variables
// ============================================================================

var defaultTags = union(tags, {
  purpose: 'central-configuration'
  'managed-by': 'bicep'
})

// ============================================================================
// Central Configuration Resource Group
// ============================================================================

resource centralConfigRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: centralConfigResourceGroupName
  location: location
  tags: defaultTags
}

// ============================================================================
// Central App Configuration
// ============================================================================

module appConfig './modules/app-config.bicep' = {
  name: 'centralAppConfig'
  scope: centralConfigRg
  params: {
    name: appConfigNamePrefix
    location: location
    tags: defaultTags
    sku: 'standard'
    publicNetworkAccess: true
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The resource ID of the central App Configuration store')
output appConfigResourceId string = appConfig.outputs.resourceId

@description('The name of the central App Configuration store')
output appConfigName string = appConfig.outputs.name

@description('The endpoint of the central App Configuration store')
output appConfigEndpoint string = appConfig.outputs.endpoint

@description('The name of the central configuration resource group')
output centralConfigResourceGroupName string = centralConfigRg.name
