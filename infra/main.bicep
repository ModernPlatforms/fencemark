// ============================================================================
// Fencemark Infrastructure - Azure Container Apps for .NET Aspire
// ============================================================================
// This template deploys the infrastructure required to host the Fencemark
// .NET Aspire application on Azure Container Apps.
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// Parameters
// ============================================================================

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g., dev, staging, prod)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Tags to apply to all resources')
param tags object = {}

@description('Container image for the API service')
param apiServiceImage string = ''

@description('Container image for the Web frontend')
param webFrontendImage string = ''

@description('The CPU allocation for the API service container')
param apiServiceCpu string = '0.25'

@description('The memory allocation for the API service container')
param apiServiceMemory string = '0.5Gi'

@description('The CPU allocation for the Web frontend container')
param webFrontendCpu string = '0.25'

@description('The memory allocation for the Web frontend container')
param webFrontendMemory string = '0.5Gi'

@description('Minimum number of replicas for the API service')
param apiServiceMinReplicas int = 0

@description('Maximum number of replicas for the API service')
param apiServiceMaxReplicas int = 3

@description('Minimum number of replicas for the Web frontend')
param webFrontendMinReplicas int = 0

@description('Maximum number of replicas for the Web frontend')
param webFrontendMaxReplicas int = 3

@description('The name of the resource group')
param resourceGroupName string

// ============================================================================
// Azure Entra External ID (CIAM) Authentication Parameters
// ============================================================================

@description('The Azure Entra External ID (CIAM) tenant ID')
param entraExternalIdTenantId string = ''

@description('The Azure Entra External ID application (client) ID')
param entraExternalIdClientId string = ''

@description('The Azure Entra External ID instance URL')
param entraExternalIdInstance string = ''

@description('The Azure Entra External ID domain')
param entraExternalIdDomain string = ''

@description('The Key Vault URL containing the certificate for authentication')
param keyVaultUrl string = ''

@description('The name of the certificate in Key Vault')
param certificateName string = ''

@description('Entra External ID Resource Group')
param externalidRg string = ''

@description('Custom domain to bind to the Web frontend')
param customDomain string = ''

// ============================================================================
// Central App Configuration Parameters
// ============================================================================

@description('The name of the central App Configuration store (deployed separately)')
param centralAppConfigName string = 'appcs-fencemark'

@description('The resource group containing the central App Configuration')
param centralAppConfigResourceGroup string = 'rg-fencemark-central-config'

// ============================================================================
// Database Parameters
// ============================================================================

@description('The administrator login username for SQL Server')
param sqlAdminLogin string = 'sqladmin'

@description('Whether to provision Azure SQL Database (true for prod, false for dev with SQLite)')
param provisionSqlDatabase bool = true

@description('Use managed identity for SQL authentication (recommended for staging/prod)')
param useManagedIdentityForSql bool = false

@description('Azure AD admin object ID for SQL Server (required when useManagedIdentityForSql is true)')
param sqlAzureAdAdminObjectId string = ''

@description('Azure AD admin login name for SQL Server (e.g., user@domain.com or group name)')
param sqlAzureAdAdminLogin string = ''

// Generate a random password for SQL Server admin
// Uses uniqueString with multiple inputs for entropy, plus special chars for complexity
var generatedSqlPassword = '${uniqueString(subscription().id, resourceGroupName, 'sql-admin')}!Aa1${uniqueString(resourceGroupName, environmentName, 'sql-pwd')}'

// ============================================================================
// Variables
// ============================================================================

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, resourceGroupName, environmentName))
var defaultTags = union(tags, {
  'azd-env-name': environmentName
})

// SQL Server connection string components (computed inline since module is conditional)
var sqlServerNameValue = '${abbrs.sqlServers}${resourceToken}'
var sqlServerFqdnValue = '${sqlServerNameValue}${environment().suffixes.sqlServerHostname}'
var sqlDatabaseNameValue = 'fencemark'

// Connection string: Use managed identity auth if enabled, otherwise SQL auth
var sqlConnectionStringManagedIdentity = 'Server=tcp:${sqlServerFqdnValue},1433;Initial Catalog=${sqlDatabaseNameValue};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var sqlConnectionStringSqlAuth = 'Server=tcp:${sqlServerFqdnValue},1433;Initial Catalog=${sqlDatabaseNameValue};User ID=${sqlAdminLogin};Password=${generatedSqlPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var sqlConnectionString = provisionSqlDatabase 
  ? (useManagedIdentityForSql ? sqlConnectionStringManagedIdentity : sqlConnectionStringSqlAuth)
  : 'Data Source=fencemark.db'

// ============================================================================
// Resource Group
// ============================================================================

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
}

// ============================================================================
// Log Analytics Workspace
// ============================================================================

module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.11.1' = {
  name: 'logAnalytics'
  scope: rg
  params: {
    name: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    location: location
    tags: defaultTags
    dataRetention: 30
  }
}

// Reference to get the shared key for Log Analytics
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
  scope: rg
}

// ============================================================================
// Application Insights
// ============================================================================

module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'applicationInsights'
  scope: rg
  params: {
    name: '${abbrs.insightsComponents}${resourceToken}'
    location: location
    tags: defaultTags
    workspaceResourceId: logAnalytics.outputs.resourceId
    applicationType: 'web'
  }
}

// ============================================================================
// Container Registry
// ============================================================================

module containerRegistry 'br/public:avm/res/container-registry/registry:0.9.1' = {
  name: 'containerRegistry'
  scope: rg
  params: {
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: defaultTags
    acrSku: 'Basic'
    acrAdminUserEnabled: true
  }
}

// Reference to ACR for getting admin credentials
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: '${abbrs.containerRegistryRegistries}${resourceToken}'
  scope: rg
}

// ============================================================================
// Azure Maps Account
// ============================================================================

module mapsAccount './maps-account.bicep' = {
  name: 'mapsAccount'
  scope: rg
  params: {
    name: '${abbrs.mapsAccounts}${resourceToken}'
    tags: defaultTags
  }
}

// Reference to get Maps subscription key
resource mapsAccountResource 'Microsoft.Maps/accounts@2024-01-01-preview' existing = {
  name: '${abbrs.mapsAccounts}${resourceToken}'
  scope: rg
}

// ============================================================================
// Key Vault (per-environment in fencemark resource group)
// ============================================================================

module keyVault './modules/keyvault.bicep' = {
  name: 'keyVault'
  scope: rg
  params: {
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    location: location
    tags: defaultTags
    sku: 'standard'
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
  }
}

// ============================================================================
// Data Protection Key in Key Vault
// ============================================================================
// Create RSA key for ASP.NET Core Data Protection
// This key is used to protect authentication state across container instances

module dataProtectionKey './modules/keyvault-key.bicep' = {
  name: 'dataProtectionKey'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    keyName: 'dataprotection-key'
    keyType: 'RSA'
    keySize: 2048
    tags: defaultTags
  }
}

// ============================================================================
// Reference to Central App Configuration
// ============================================================================
// The central App Config is deployed separately via central-appconfig.bicep
// It serves all environments using labels (dev, staging, prod)
// Note: No need for explicit resource reference - using parameters directly

// ============================================================================
// Container Apps Environment
// ============================================================================

module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.11.0' = {
  name: 'containerAppsEnvironment'
  scope: rg
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: defaultTags
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.outputs.logAnalyticsWorkspaceId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    publicNetworkAccess: 'Enabled'
    appInsightsConnectionString: applicationInsights.outputs.connectionString
    openTelemetryConfiguration: {
      tracesDestination: 'appInsights'
      logsDestination: 'appInsights'
    }
  }
}

// ============================================================================
// Azure SQL Database (Production)
// ============================================================================

module sqlDatabase './modules/sql-database.bicep' = if (provisionSqlDatabase) {
  name: 'sqlDatabase'
  scope: rg
  params: {
    sqlServerName: sqlServerNameValue
    location: location
    tags: defaultTags
    databaseName: sqlDatabaseNameValue
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: generatedSqlPassword
  }
}

// ============================================================================
// SQL Server Azure AD Admin (for Managed Identity auth)
// ============================================================================

module sqlAadAdmin './modules/sql-aad-admin.bicep' = if (provisionSqlDatabase && useManagedIdentityForSql && !empty(sqlAzureAdAdminObjectId)) {
  name: 'sqlAadAdmin'
  scope: rg
  params: {
    sqlServerName: sqlServerNameValue
    azureAdAdminObjectId: sqlAzureAdAdminObjectId
    azureAdAdminLogin: sqlAzureAdAdminLogin
    azureAdOnlyAuthentication: false // Keep SQL auth available as fallback
  }
  dependsOn: [sqlDatabase]
}

// ============================================================================
// Managed Certificate for Custom Domain
// ============================================================================

module managedCertificate './modules/managed-certificate.bicep' = if (!empty(customDomain)) {
  name: 'managedCertificate'
  scope: rg
  params: {
    name: 'cert-${replace(customDomain, '.', '-')}'
    location: location
    environmentId: containerAppsEnvironment.outputs.resourceId
    subjectName: customDomain
    tags: defaultTags
  }
}

// ============================================================================
// .NET Aspire Dashboard Container App
// ============================================================================

module aspireDashboard 'br/public:avm/res/app/container-app:0.16.0' = {
  name: 'aspireDashboard'
  scope: rg
  params: {
    name: '${abbrs.appContainerApps}aspiredash-${resourceToken}'
    location: location
    tags: union(defaultTags, {
      'azd-service-name': 'aspire-dashboard'
    })
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    workloadProfileName: 'Consumption'
    containers: [
      {
        name: 'aspiredashboard'
        image: 'mcr.microsoft.com/dotnet/aspire-dashboard:9.0'
        resources: {
          cpu: json('0.25')
          memory: '0.5Gi'
        }
        env: [
          {
            name: 'DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS'
            value: 'true'
          }
          {
            name: 'DASHBOARD__OTLP__AUTHMODE'
            value: 'Unsecured'
          }
          {
            name: 'DASHBOARD__OTLP__ENDPOINT_URL'
            value: 'http://+:18889'
          }
        ]
      }
    ]
    scaleSettings: {
      minReplicas: 1
      maxReplicas: 1
    }
    ingressExternal: true
    ingressTargetPort: 18888
    ingressTransport: 'http'
    additionalPortMappings: [
      {
        external: false
        targetPort: 18889
      }
    ]
  }
}

// ============================================================================
// API Service Container App
// ============================================================================

module apiService 'br/public:avm/res/app/container-app:0.19.0' = {
  name: 'apiService'
  scope: rg
  params: {
    name: '${abbrs.appContainerApps}apiservice-${resourceToken}'
    location: location
    tags: union(defaultTags, {
      'azd-service-name': 'apiservice'
    })
    
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    managedIdentities: {
      systemAssigned: true
    }
    workloadProfileName: 'Consumption'
    containers: [
      {
        name: 'apiservice'
        image: !empty(apiServiceImage) ? apiServiceImage : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        
        resources: {
          cpu: json(apiServiceCpu)
          memory: apiServiceMemory
        }
        env: [
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: applicationInsights.outputs.connectionString
          }
          {
            name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
            value: 'http://${abbrs.appContainerApps}aspiredash-${resourceToken}:18889'
          }
          {
            name: 'OTEL_SERVICE_NAME'
            value: 'apiservice'
          }
          {
            name: 'ConnectionStrings__DefaultConnection'
            secretRef: 'sql-connection-string'
          }
          {
            name: 'AppConfig__Endpoint'
            value: 'https://${centralAppConfigName}.azconfig.io'
          }
          {
            name: 'AppConfig__Label'
            value: environmentName
          }
          {
            name: 'ASPNETCORE_ENVIRONMENT'
            value: environmentName == 'dev' ? 'Development' : 'Production'
          }
          // Configure Data Protection to use Azure Key Vault for persistent key storage
          // This ensures encrypted data survives container restarts and works across instances
          {
            name: 'ASPNETCORE_DATAPROTECTION_KEYSTORE'
            value: 'AzureKeyVault'
          }
          {
            name: 'ASPNETCORE_DATAPROTECTION_KEYVAULT_URI'
            value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/'
          }
          {
            name: 'ASPNETCORE_DATAPROTECTION_KEYVAULT_KEYIDENTIFIER'
            value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/Keys/dataprotection-key'
          }
        ]
        probes: [
          {
            type: 'Liveness'
            httpGet: {
              path: '/alive'
              port: 8080
            }
            initialDelaySeconds: 10
            periodSeconds: 30
          }
          {
            type: 'Readiness'
            httpGet: {
              path: '/health'
              port: 8080
            }
            initialDelaySeconds: 5
            periodSeconds: 10
          }
        ]
       
      }
       
    ]
    scaleSettings: {
      minReplicas: apiServiceMinReplicas
      maxReplicas: apiServiceMaxReplicas
    }
    ingressExternal: false
    ingressTargetPort: 8080
    ingressTransport: 'http'
    registries: [
      {
        server: containerRegistry.outputs.loginServer
        username: containerRegistry.outputs.name
        passwordSecretRef: 'acr-password'
      }
    ]
    runtime: {
          dotnet: {
            autoConfigureDataProtection: false
          }
        }
    secrets: [
      {
        name: 'acr-password'
        value: acr.listCredentials().passwords[0].value
      }
      {
        name: 'sql-connection-string'
        // NOTE: For production, consider using Azure AD authentication with managed identity
        // instead of SQL authentication. This connection string is only for initial setup.
        // See: https://docs.microsoft.com/en-us/azure/app-service/tutorial-connect-msi-sql-database
        value: sqlConnectionString
      }
    ]
  }
}

// ============================================================================
// Web Frontend Container App
// ============================================================================

module webFrontend 'br/public:avm/res/app/container-app:0.19.0' = {
  name: 'webFrontend'
  scope: rg
  params: {
    name: '${abbrs.appContainerApps}webfrontend-${resourceToken}'
    location: location
    tags: union(defaultTags, {
      'azd-service-name': 'webfrontend'
    })
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    workloadProfileName: 'Consumption'
    managedIdentities: {
      systemAssigned: true
    }
    containers: [
      {
        name: 'webfrontend'
        image: !empty(webFrontendImage) ? webFrontendImage : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        resources: {
          cpu: json(webFrontendCpu)
          memory: webFrontendMemory
        }
        env: [
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: applicationInsights.outputs.connectionString
          }
          {
            name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
            value: 'http://${abbrs.appContainerApps}aspiredash-${resourceToken}:18889'
          }
          {
            name: 'OTEL_SERVICE_NAME'
            value: 'webfrontend'
          }
          {
            name: 'services__apiservice__http__0'
            value: 'http://${apiService.outputs.fqdn}'
          }
          {
            name: 'services__apiservice__https__0'
            value: 'https://${apiService.outputs.fqdn}'
          }
          {
            name: 'AppConfig__Endpoint'
            value: 'https://${centralAppConfigName}.azconfig.io'
          }
          {
            name: 'AppConfig__Label'
            value: environmentName
          }
          // Keep essential environment variables for backwards compatibility
          {
            name: 'AzureMaps__ClientId'
            value: mapsAccount.outputs.resourceId
          }
          {
            name: 'AzureAd__CallbackPath'
            value: '/signin-oidc'
          }
          {
            name: 'AzureAd__SignedOutCallbackPath'
            value: '/signout-callback-oidc'
          }
          {
            name: 'ASPNETCORE_ENVIRONMENT'
            value: environmentName == 'dev' ? 'Development' : 'Production'
          }
          // Configure Data Protection to use Azure Key Vault for persistent key storage
          // This ensures CSRF tokens and other encrypted data survive container restarts
          {
            name: 'ASPNETCORE_DATAPROTECTION_KEYSTORE'
            value: 'AzureKeyVault'
          }
          {
            name: 'ASPNETCORE_DATAPROTECTION_KEYVAULT_URI'
            value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/'
          }
          {
            name: 'ASPNETCORE_DATAPROTECTION_KEYVAULT_KEYIDENTIFIER'
            value: 'https://${keyVault.outputs.name}${environment().suffixes.keyvaultDns}/Keys/dataprotection-key'
          }
        ]
        probes: [
          {
            type: 'Liveness'
            httpGet: {
              path: '/alive'
              port: 8080
            }
            initialDelaySeconds: 10
            periodSeconds: 30
          }
          {
            type: 'Readiness'
            httpGet: {
              path: '/health'
              port: 8080
            }
            initialDelaySeconds: 5
            periodSeconds: 10
          }
        ]
      }
    ]
    scaleSettings: {
      minReplicas: webFrontendMinReplicas
      maxReplicas: webFrontendMaxReplicas
    }
    ingressExternal: true
    ingressTargetPort: 8080
    ingressTransport: 'http'
    customDomains: !empty(customDomain) ? [
      {
        name: customDomain
        bindingType: 'SniEnabled'
        certificateId: managedCertificate.?outputs.resourceId ?? ''
      }
    ] : null
    registries: [
      {
        server: containerRegistry.outputs.loginServer
        username: containerRegistry.outputs.name
        passwordSecretRef: 'acr-password'
      }
    ]
    runtime: {
          dotnet: {
            autoConfigureDataProtection: false
          }
        }
    secrets: [
      {
        name: 'acr-password'
        value: acr.listCredentials().passwords[0].value
      }
    ]
  }
}

// ============================================================================
// Reference to External ID Key Vault resource
// ============================================================================

resource externalIdKeyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = if (!empty(externalidRg)) {
  name: 'kv-ciambfwyw65gna5lu'
  scope: resourceGroup(externalidRg)
}

// ============================================================================
// Store SQL Admin Password in External ID Key Vault
// ============================================================================

module sqlPasswordSecret './modules/keyvault-secret.bicep' = if (provisionSqlDatabase && !empty(externalidRg)) {
  name: 'sqlPasswordSecret'
  scope: resourceGroup(externalidRg)
  params: {
    keyVaultName: externalIdKeyVault.name
    secretName: 'sql-admin-password-${environmentName}'
    secretValue: generatedSqlPassword
    contentType: 'SQL Server admin password'
    tags: defaultTags
  }
}

// ============================================================================
// Store Secrets in Fencemark Key Vault
// ============================================================================

// Store SQL admin password
module sqlPasswordInKeyVault './modules/keyvault-secret.bicep' = if (provisionSqlDatabase) {
  name: 'sqlPasswordInKeyVault'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    secretName: 'sql-admin-password'
    secretValue: generatedSqlPassword
    contentType: 'SQL Server admin password'
    tags: defaultTags
  }
}

// Store Maps primary key
module mapsPrimaryKeyInKeyVault './modules/keyvault-secret.bicep' = {
  name: 'mapsPrimaryKeyInKeyVault'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    secretName: 'azure-maps-primary-key'
    secretValue: mapsAccountResource.listKeys().primaryKey
    contentType: 'Azure Maps Primary Key'
    tags: defaultTags
  }
  dependsOn: [mapsAccount]
}

// ============================================================================
// RBAC Role Assignments for Central App Configuration
// ============================================================================
// Grant managed identities from this environment access to the central App Config
// Use modules to handle cross-resource group role assignments

// Role definition IDs
var appConfigDataReaderRoleId = '516239f1-63e1-4d78-a4de-a74fb236a071' // App Configuration Data Reader

// Grant API Service access to Central App Configuration (cross-resource group)
module apiServiceAppConfigRoleAssignment './modules/rbac-assignment.bicep' = {
  name: 'apiSvcAppCfgRole'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    principalId: apiService.outputs.?systemAssignedMIPrincipalId ?? ''
    roleDefinitionId: appConfigDataReaderRoleId
    principalType: 'ServicePrincipal'
  }
}

// Grant Web Frontend access to Central App Configuration (cross-resource group)
module webFrontendAppConfigRoleAssignment './modules/rbac-assignment.bicep' = {
  name: 'webAppCfgRole'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    principalId: webFrontend.outputs.?systemAssignedMIPrincipalId ?? ''
    roleDefinitionId: appConfigDataReaderRoleId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// RBAC Role Assignments for Key Vault
// ============================================================================

// Grant API Service access to Key Vault secrets
module apiServiceKeyVaultRoleAssignment './keyvault-access.bicep' = {
  name: 'apiServiceKeyVaultRoleAssignment'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: apiService.outputs.?systemAssignedMIPrincipalId ?? ''
    principalType: 'ServicePrincipal'
    roleName: 'Key Vault Secrets User'
  }
}

// Grant API Service access to Key Vault key (Crypto User)
module apiServiceKeyVaultCryptoRoleAssignment './keyvault-access.bicep' = {
  name: 'apiServiceKeyVaultCryptoRoleAssignment'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: apiService.outputs.?systemAssignedMIPrincipalId ?? ''
    principalType: 'ServicePrincipal'
    roleName: 'Key Vault Crypto Officer'
  }
}

// Grant Web Frontend access to Key Vault secrets
module webFrontendKeyVaultRoleAssignment './keyvault-access.bicep' = {
  name: 'webFrontendKeyVaultRoleAssignment'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: webFrontend.outputs.?systemAssignedMIPrincipalId ?? ''
    principalType: 'ServicePrincipal'
    roleName: 'Key Vault Secrets User'
  }
}

// Grant Web Frontend access to Key Vault key (Crypto User)
module webFrontendKeyVaultCryptoRoleAssignment './keyvault-access.bicep' = {
  name: 'webFrontendKeyVaultCryptoRoleAssignment'
  scope: rg
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: webFrontend.outputs.?systemAssignedMIPrincipalId ?? ''
    principalType: 'ServicePrincipal'
    roleName: 'Key Vault Crypto Officer'
  }
}

// ============================================================================
// Populate Central App Configuration with Environment-Specific Values
// ============================================================================
// All configuration values are stored in the central App Config with
// environment labels (dev, staging, prod)

// SQL Connection String
module appConfigSqlConnection './modules/app-config-key-value.bicep' = if (provisionSqlDatabase) {
  name: 'cfg-1-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'ConnectionStrings:DefaultConnection'
    value: sqlConnectionString
    label: environmentName
    contentType: 'text/plain'
  }
}

// Azure Maps Primary Key as Key Vault Reference
module appConfigMapsKey './modules/app-config-key-value.bicep' = {
  name: 'cfg-2-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'AzureMaps:SubscriptionKey'
    value: '{"uri":"${keyVault.outputs.vaultUri}secrets/azure-maps-primary-key"}'
    label: environmentName
    contentType: 'application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8'
  }
  dependsOn: [mapsPrimaryKeyInKeyVault]
}

// Azure Maps Client ID
module appConfigMapsClientId './modules/app-config-key-value.bicep' = {
  name: 'cfg-3-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'AzureMaps:ClientId'
    value: mapsAccount.outputs.resourceId
    label: environmentName
    contentType: 'text/plain'
  }
}

// Entra External ID - Instance
module appConfigEntraInstance './modules/app-config-key-value.bicep' = if (!empty(entraExternalIdInstance)) {
  name: 'cfg-4-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'AzureAd:Instance'
    value: entraExternalIdInstance
    label: environmentName
    contentType: 'text/plain'
  }
}

// Entra External ID - Tenant ID
module appConfigEntraTenantId './modules/app-config-key-value.bicep' = if (!empty(entraExternalIdTenantId)) {
  name: 'cfg-5-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'AzureAd:TenantId'
    value: entraExternalIdTenantId
    label: environmentName
    contentType: 'text/plain'
  }
}

// Entra External ID - Client ID
module appConfigEntraClientId './modules/app-config-key-value.bicep' = if (!empty(entraExternalIdClientId)) {
  name: 'cfg-6-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'AzureAd:ClientId'
    value: entraExternalIdClientId
    label: environmentName
    contentType: 'text/plain'
  }
}

// Entra External ID - Domain
module appConfigEntraDomain './modules/app-config-key-value.bicep' = if (!empty(entraExternalIdDomain)) {
  name: 'cfg-7-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'AzureAd:Domain'
    value: entraExternalIdDomain
    label: environmentName
    contentType: 'text/plain'
  }
}

// Key Vault URL (External ID Key Vault for certificates)
module appConfigKeyVaultUrl './modules/app-config-key-value.bicep' = if (!empty(keyVaultUrl)) {
  name: 'cfg-8-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'KeyVault:Url'
    value: keyVaultUrl
    label: environmentName
    contentType: 'text/plain'
  }
}

// Certificate Name
module appConfigCertificateName './modules/app-config-key-value.bicep' = if (!empty(certificateName)) {
  name: 'cfg-9-${resourceToken}'
  scope: resourceGroup(centralAppConfigResourceGroup)
  params: {
    appConfigName: centralAppConfigName
    key: 'KeyVault:CertificateName'
    value: certificateName
    label: environmentName
    contentType: 'text/plain'
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The name of the Container Apps Environment')
output containerAppsEnvironmentName string = containerAppsEnvironment.outputs.name

@description('The name of the Container Registry')
output containerRegistryName string = containerRegistry.outputs.name

@description('The login server of the Container Registry')
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer

@description('The name of the Log Analytics Workspace')
output logAnalyticsWorkspaceName string = logAnalytics.outputs.name

@description('The name of the Application Insights instance')
output applicationInsightsName string = applicationInsights.outputs.name

@description('The name of the API Service Container App')
output apiServiceName string = apiService.outputs.name

@description('The FQDN of the API Service Container App')
output apiServiceFqdn string = apiService.outputs.fqdn

@description('The principal ID of the API Service managed identity (use this for SQL user creation)')
output apiServiceIdentityPrincipalId string = apiService.outputs.?systemAssignedMIPrincipalId ?? ''

@description('The name of the Web Frontend Container App')
output webFrontendName string = webFrontend.outputs.name

@description('The FQDN of the Web Frontend Container App')
output webFrontendFqdn string = webFrontend.outputs.fqdn

@description('The URL of the Web Frontend')
output webFrontendUrl string = 'https://${webFrontend.outputs.fqdn}'

@description('The URL of the Aspire Dashboard')
output aspireDashboardUrl string = 'https://${aspireDashboard.outputs.fqdn}'

@description('The principal ID of the Web Frontend managed identity')
output webFrontendIdentityPrincipalId string = webFrontend.outputs.?systemAssignedMIPrincipalId ?? ''

@description('The resource group name')
output resourceGroupName string = rg.name

@description('The Azure Maps account resource ID')
output mapsAccountResourceId string = mapsAccount.outputs.resourceId

@description('The name of the Azure Maps Account')
output mapsAccountName string = mapsAccount.outputs.name

@description('The custom domain (if configured)')
output customDomainName string = customDomain

@description('The Container Apps Environment default domain for CNAME setup')
output environmentDefaultDomain string = containerAppsEnvironment.outputs.defaultDomain

@description('The verification ID for custom domain TXT record')
output customDomainVerificationId string = containerAppsEnvironment.outputs.domainVerificationId

@description('The SQL Server name (if provisioned)')
output outputSqlServerName string = provisionSqlDatabase ? sqlServerNameValue : ''

@description('The SQL Server FQDN (if provisioned)')
output outputSqlServerFqdn string = provisionSqlDatabase ? sqlServerFqdnValue : ''

@description('The SQL Database name (if provisioned)')
output outputSqlDatabaseName string = provisionSqlDatabase ? sqlDatabaseNameValue : ''

@description('The Key Vault name storing the SQL admin credential (if provisioned)')
output sqlCredentialKeyVaultName string = provisionSqlDatabase && !empty(externalidRg) ? 'sql-admin-password-${environmentName}' : ''

@description('The central App Configuration endpoint')
output appConfigEndpoint string = 'https://${centralAppConfigName}.azconfig.io'

@description('The central App Configuration name')
output appConfigName string = centralAppConfigName

@description('The central App Configuration resource group')
output appConfigResourceGroup string = centralAppConfigResourceGroup

@description('The Key Vault URI')
output keyVaultUri string = keyVault.outputs.vaultUri

@description('The Key Vault name')
output keyVaultName string = keyVault.outputs.name

// ============================================================================
// Assign Key Vault Certificate User role to the managed identity
// ============================================================================

module externalKeyVaultAccessModule './keyvault-access.bicep' = if (!empty(externalidRg)) {
  name: 'externalKeyVaultAccessModule'
  scope: resourceGroup(externalidRg)
  params: {
    keyVaultName: externalIdKeyVault.name
    principalId: webFrontend.outputs.?systemAssignedMIPrincipalId ?? ''
    principalType: 'ServicePrincipal'
    roleName: 'Key Vault Certificate User'
  }
}
