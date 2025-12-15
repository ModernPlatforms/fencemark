// ============================================================================
// Azure SQL Database Module - Serverless tier for cost optimization
// ============================================================================

@description('The name of the SQL Server')
param sqlServerName string

@description('The location for the SQL Server')
param location string = resourceGroup().location

@description('Tags to apply to resources')
param tags object = {}

@description('The name of the database')
param databaseName string = 'fencemark'

@description('The administrator login username')
@secure()
param administratorLogin string

@description('The administrator login password')
@secure()
param administratorLoginPassword string

@description('The Azure AD admin object ID')
param azureAdAdminObjectId string = ''

@description('The Azure AD admin user principal name')
param azureAdAdminLogin string = ''

@description('Whether to enable Azure AD authentication')
param enableAzureAdAuth bool = false

// ============================================================================
// SQL Server
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// ============================================================================
// Azure AD Admin (if enabled)
// ============================================================================

resource sqlServerAzureAdAdmin 'Microsoft.Sql/servers/administrators@2023-05-01-preview' = if (enableAzureAdAuth && !empty(azureAdAdminObjectId)) {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: azureAdAdminLogin
    sid: azureAdAdminObjectId
    tenantId: subscription().tenantId
  }
}

// ============================================================================
// Firewall Rules
// ============================================================================

// Allow Azure services to access the server
resource firewallRuleAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================================
// SQL Database - Serverless tier for cost optimization
// ============================================================================

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1 // 1 vCore min
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    autoPauseDelay: 60 // Auto-pause after 60 minutes of inactivity
    minCapacity: json('0.5') // Minimum 0.5 vCore
    isLedgerOn: false
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The resource ID of the SQL Server')
output sqlServerResourceId string = sqlServer.id

@description('The name of the SQL Server')
output sqlServerName string = sqlServer.name

@description('The fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The resource ID of the SQL Database')
output sqlDatabaseResourceId string = sqlDatabase.id

@description('The name of the SQL Database')
output sqlDatabaseName string = sqlDatabase.name

@description('The connection string for the SQL Database (without password)')
output connectionStringTemplate string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${administratorLogin};Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

@description('The principal ID of the SQL Server managed identity')
output sqlServerIdentityPrincipalId string = sqlServer.identity.principalId
