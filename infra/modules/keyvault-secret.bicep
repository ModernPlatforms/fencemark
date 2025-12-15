// ============================================================================
// Key Vault Secret Module
// ============================================================================
// This module creates a secret in an existing Key Vault.
// ============================================================================

@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the secret')
param secretName string

@description('The value of the secret')
@secure()
param secretValue string

@description('Content type for the secret')
param contentType string = 'text/plain'

@description('Tags to apply to the secret')
param tags object = {}

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

// Create the secret
resource secret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: secretName
  tags: tags
  properties: {
    value: secretValue
    contentType: contentType
  }
}

@description('The URI of the secret')
output secretUri string = secret.properties.secretUri

@description('The URI of the secret with version')
output secretUriWithVersion string = secret.properties.secretUriWithVersion

@description('The name of the secret')
output secretName string = secret.name
