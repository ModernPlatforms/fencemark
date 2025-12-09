param keyVaultId string
param principalId string

resource keyVaultAccess 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(keyVaultId, principalId, 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
  scope: keyVaultId
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}