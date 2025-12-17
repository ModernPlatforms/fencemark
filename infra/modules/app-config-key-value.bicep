// ============================================================================
// App Configuration Key-Value Module
// ============================================================================
// This module creates a key-value pair in Azure App Configuration
// with support for labels and Key Vault references.
// ============================================================================

@description('Name of the App Configuration store')
param appConfigName string

@description('The key name')
param key string

@description('The value')
param value string

@description('Label for the key-value (e.g., dev, staging, prod)')
param label string = ''

@description('Content type for the value')
param contentType string = 'text/plain'

@description('Tags for the key-value')
param tags object = {}

// Reference existing App Configuration store
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' existing = {
  name: appConfigName
}

// Create the key-value pair
// Note: Key Vault references use a special format:
// {"uri":"https://<keyvault-name>.vault.azure.net/secrets/<secret-name>"}
// with contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
resource keyValue 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  // The resource name format is: <key>$<label>
  name: empty(label) ? key : '${key}$${label}'
  properties: {
    value: value
    contentType: contentType
    tags: tags
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The name of the key-value resource')
output name string = keyValue.name

@description('The key')
output key string = key

@description('The label')
output label string = label
