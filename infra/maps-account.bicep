// ============================================================================
// Azure Maps Account Module
// ============================================================================

@description('Name of the Azure Maps account')
param name string

@description('Tags to apply to the resource')
param tags object = {}

resource mapsAccount 'Microsoft.Maps/accounts@2024-01-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  sku: {
    name: 'G2'
  }
  kind: 'Gen2'
  properties: {
    disableLocalAuth: false
    cors: {
      corsRules: []
    }
  }
}

@description('The name of the Azure Maps Account')
output name string = mapsAccount.name

@description('The resource ID of the Azure Maps Account')
output resourceId string = mapsAccount.id
