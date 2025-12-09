// ============================================================================
// Key Vault Access - Grant RBAC permissions to an existing Key Vault
// ============================================================================
// Usage:
//   az deployment group create -g <resource-group> --template-file keyvault-access.bicep --parameters keyVaultName=<name> principalId=<id>
// ============================================================================

targetScope = 'resourceGroup'

@description('Name of the existing Key Vault')
param keyVaultName string

@description('Principal ID to grant access to')
param principalId string

@description('Principal type (ServicePrincipal, User, Group)')
@allowed([
  'ServicePrincipal'
  'User'
  'Group'
])
param principalType string = 'ServicePrincipal'

@description('Role to assign')
@allowed([
  'Key Vault Secrets User'
  'Key Vault Secrets Officer'
  'Key Vault Administrator'
  'Key Vault Certificates Officer'
  'Key Vault Certificate User'
  'Key Vault Crypto Officer'
  'Key Vault Reader'
])
param roleName string = 'Key Vault Secrets User'

// Role definition IDs
var roleDefinitionIds = {
  'Key Vault Secrets User': '4633458b-17de-408a-b874-0445c86b69e6'
  'Key Vault Secrets Officer': 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
  'Key Vault Administrator': '00482a5a-887f-4fb3-b363-3b7fe8e74483'
  'Key Vault Certificates Officer': 'a4417e6f-fecd-4de8-b567-7b0420556985'
  'Key Vault Certificate User' : 'db79e9a7-68ee-4b58-9aeb-b90e7c24fcba'
  'Key Vault Crypto Officer': '14b46e9e-c2b7-41b4-b07b-48a6ebf60603'
  'Key Vault Reader': '21090545-7ca7-4776-b22c-e363652d74d2'
}

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// Assign role to principal
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, roleDefinitionIds[roleName])
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionIds[roleName])
    principalId: principalId
    principalType: principalType
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The role assignment ID')
output roleAssignmentId string = keyVaultRoleAssignment.id

@description('The Key Vault resource ID')
output keyVaultId string = keyVault.id
