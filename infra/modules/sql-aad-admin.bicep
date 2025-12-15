// ============================================================================
// SQL Database User Assignment Module
// ============================================================================
// This module configures Azure AD authentication for SQL Server by setting
// the SQL Server's managed identity as the Azure AD admin. This allows
// other managed identities to authenticate to the database.
//
// NOTE: Creating database users for managed identities requires a deployment
// script that runs T-SQL. This module enables Azure AD auth on the server.
// ============================================================================

@description('The name of the SQL Server')
param sqlServerName string

@description('The object ID of the Azure AD admin (typically a group or user that can manage the database)')
param azureAdAdminObjectId string

@description('The login name for the Azure AD admin')
param azureAdAdminLogin string

@description('Enable Azure AD-only authentication (disable SQL auth)')
param azureAdOnlyAuthentication bool = false

// Reference existing SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' existing = {
  name: sqlServerName
}

// Set Azure AD administrator
resource azureAdAdmin 'Microsoft.Sql/servers/administrators@2023-05-01-preview' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: azureAdAdminLogin
    sid: azureAdAdminObjectId
    tenantId: subscription().tenantId
  }
}

// Optionally enable Azure AD-only authentication
resource azureAdOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-05-01-preview' = if (azureAdOnlyAuthentication) {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
  dependsOn: [azureAdAdmin]
}

@description('The Azure AD admin login')
output adminLogin string = azureAdAdminLogin

@description('The Azure AD admin object ID')
output adminObjectId string = azureAdAdminObjectId
