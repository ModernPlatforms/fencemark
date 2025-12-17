// ============================================================================
// Azure Key Vault Module
// ============================================================================
// This module creates an Azure Key Vault for storing secrets, keys, and
// certificates with RBAC-based access control.
// ============================================================================

@description('Name of the Key Vault')
param name string

@description('Location for the Key Vault')
param location string

@description('Tags to apply to the resource')
param tags object = {}

@description('SKU for Key Vault')
@allowed([
  'standard'
  'premium'
])
param sku string = 'standard'

@description('Enable public network access')
param publicNetworkAccess bool = true

@description('Enable soft delete')
param enableSoftDelete bool = true

@description('Soft delete retention in days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 7

@description('Enable purge protection')
param enablePurgeProtection bool = false

@description('Enable RBAC authorization')
param enableRbacAuthorization bool = true

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: sku
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: enableRbacAuthorization
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : null
    publicNetworkAccess: publicNetworkAccess ? 'Enabled' : 'Disabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The resource ID of the Key Vault')
output resourceId string = keyVault.id

@description('The name of the Key Vault')
output name string = keyVault.name

@description('The URI of the Key Vault')
output vaultUri string = keyVault.properties.vaultUri
