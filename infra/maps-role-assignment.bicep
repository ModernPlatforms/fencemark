// ============================================================================
// Azure Maps Role Assignment - Grant RBAC permissions to an existing Azure Maps account
// ============================================================================

targetScope = 'resourceGroup'

@description('Name of the existing Azure Maps account')
param mapsAccountName string

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
  'Azure Maps Data Reader'
  'Azure Maps Data Contributor'
  'Azure Maps Search and Render Data Reader'
])
param roleName string = 'Azure Maps Data Reader'

// Azure Maps role definition IDs
// See: https://learn.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#azure-maps-data-reader
var roleDefinitionIds = {
  'Azure Maps Data Reader': '423170ca-a8f6-4b0f-8487-9e4eb8f49bfa'
  'Azure Maps Data Contributor': '8f5e0ce6-4f7b-4dcf-bddf-e6f48634a204'
  'Azure Maps Search and Render Data Reader': '6be48352-4f82-47c9-ad5e-0acacefdb005'
}

// Reference existing Azure Maps account
resource mapsAccount 'Microsoft.Maps/accounts@2024-01-01-preview' existing = {
  name: mapsAccountName
}

// Assign role to principal
resource mapsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(mapsAccount.id, principalId, roleDefinitionIds[roleName])
  scope: mapsAccount
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
output roleAssignmentId string = mapsRoleAssignment.id

@description('The Azure Maps account resource ID')
output mapsAccountId string = mapsAccount.id
