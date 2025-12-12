// ============================================================================
// ACR Role Assignment Module
// ============================================================================
// Assigns AcrPull role to a principal for pulling images from Azure Container Registry
// ============================================================================

@description('The name of the Azure Container Registry')
param acrName string

@description('The principal ID to assign the role to')
param principalId string

@description('The type of principal (ServicePrincipal, User, Group)')
param principalType string = 'ServicePrincipal'

// AcrPull role definition ID
var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

// Reference to existing ACR
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// Role assignment
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, principalId, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: acrPullRoleDefinitionId
    principalId: principalId
    principalType: principalType
  }
}
