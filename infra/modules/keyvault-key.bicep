// ============================================================================
// Key Vault Key Module
// ============================================================================
// This module creates an RSA key in an existing Key Vault for use with
// ASP.NET Core Data Protection.
// ============================================================================

@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the key')
param keyName string

@description('Key type (RSA, EC, RSA-HSM, EC-HSM)')
@allowed([
  'RSA'
  'EC'
  'RSA-HSM'
  'EC-HSM'
])
param keyType string = 'RSA'

@description('Key size in bits (2048, 3072, 4096 for RSA)')
@allowed([
  2048
  3072
  4096
])
param keySize int = 2048

@description('Tags to apply to the key')
param tags object = {}

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Create the key
resource key 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: keyName
  tags: tags
  properties: {
    kty: keyType
    keySize: keySize
    keyOps: [
      'encrypt'
      'decrypt'
      'sign'
      'verify'
      'wrapKey'
      'unwrapKey'
    ]
    attributes: {
      enabled: true
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The resource ID of the key')
output keyId string = key.id

@description('The name of the key')
output keyName string = key.name

@description('The key URI')
output keyUri string = key.properties.keyUri

@description('The key URI with version')
output keyUriWithVersion string = key.properties.keyUriWithVersion
