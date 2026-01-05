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

@description('The domain name for the DNS zone')
param domainName string = 'fencemark.com.au'

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
    sku: 'free'
    publicNetworkAccess: true
  }
}

// ============================================================================
// DNS Zone
// ============================================================================

resource dnsZone 'Microsoft.Network/dnsZones@2023-07-01-preview' = {
  name: domainName
  location: 'global'
  tags: defaultTags
  properties: {
    zoneType: 'Public'
  }
  scope: centralConfigRg
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

@description('The name of the DNS zone')
output dnsZoneName string = dnsZone.name

@description('The resource ID of the DNS zone')
output dnsZoneId string = dnsZone.id

@description('The name servers for the DNS zone - Configure these at your domain registrar')
output nameServers array = dnsZone.properties.nameServers
