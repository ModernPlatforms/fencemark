// ============================================================================
// RBAC Role Assignment Module for App Configuration
// ============================================================================
// This module assigns an Azure RBAC role to a principal on an App Config store.
// ============================================================================

targetScope = 'resourceGroup'

@description('Name of the App Configuration store')
param appConfigName string

@description('Principal ID to grant access to')
param principalId string

@description('Role definition ID (GUID)')
param roleDefinitionId string

@description('Principal type (ServicePrincipal, User, Group)')
@allowed([
  'ServicePrincipal'
  'User'
  'Group'
])
param principalType string = 'ServicePrincipal'

// Reference the existing App Configuration store
resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' existing = {
  name: appConfigName
}

// Create role assignment
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appConfig.id, principalId, roleDefinitionId)
  scope: appConfig
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalId: principalId
    principalType: principalType
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The role assignment ID')
output roleAssignmentId string = roleAssignment.id
