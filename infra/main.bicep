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

// Validate authentication configuration if any auth parameter is provided
var isAuthConfigured = !empty(entraExternalIdClientId) || !empty(keyVaultUrl)
var authValidationMessage = isAuthConfigured && empty(entraExternalIdTenantId) ? 'ERROR: entraExternalIdTenantId is required when authentication is configured. Use infra/get-tenant-id.sh to retrieve it.' : ''

// ============================================================================
// Variables
// ============================================================================

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, resourceGroupName, environmentName))
var defaultTags = union(tags, {
  'azd-env-name': environmentName
})

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
  dependsOn: [logAnalytics]
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
  dependsOn: [containerRegistry]
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
    // Enable .NET Aspire Dashboard
    appInsightsConnectionString: '' // Optional: Add Application Insights connection string
    openTelemetryConfiguration: {
      tracesDestination: 'appInsights'
      logsDestination: 'appInsights'
    }
  }
}

// ============================================================================
// .NET Aspire Dashboard Container App
// ============================================================================

module aspireDashboard 'br/public:avm/res/app/container-app:0.16.0' = {
  name: 'aspireDashboard'
  scope: rg
  params: {
    name: '${abbrs.appContainerApps}aspire-dashboard-${resourceToken}'
    location: location
    tags: union(defaultTags, {
      'azd-service-name': 'aspire-dashboard'
    })
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    workloadProfileName: 'Consumption'
    containers: [
      {
        name: 'aspire-dashboard'
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
  }
}

// ============================================================================
// API Service Container App
// ============================================================================

module apiService 'br/public:avm/res/app/container-app:0.16.0' = {
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
            name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
            value: 'http://${abbrs.appContainerApps}aspire-dashboard-${resourceToken}:18889'
          }
          {
            name: 'OTEL_SERVICE_NAME'
            value: 'apiservice'
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
    secrets: [
      {
        name: 'acr-password'
        value: acr.listCredentials().passwords[0].value
      }
    ]
  }
}

// ============================================================================
// Web Frontend Container App
// ============================================================================

module webFrontend 'br/public:avm/res/app/container-app:0.16.0' = {
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
            name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
            value: 'http://${abbrs.appContainerApps}aspire-dashboard-${resourceToken}:18889'
          }
          {
            name: 'OTEL_SERVICE_NAME'
            value: 'webfrontend'
          }
          {
            name: 'services__apiservice__http__0'
            value: 'http://${abbrs.appContainerApps}apiservice-${resourceToken}'
          }
          {
            name: 'services__apiservice__https__0'
            value: 'https://${abbrs.appContainerApps}apiservice-${resourceToken}'
          }
          {
            name: 'AzureMaps__ClientId'
            value: mapsAccount.outputs.resourceId
          }
          // Azure AD / Entra External ID settings
          {
            name: 'AzureAd__Instance'
            value: entraExternalIdInstance
          }
          {
            name: 'AzureAd__TenantId'
            value: entraExternalIdTenantId
          }
          {
            name: 'AzureAd__ClientId'
            value: entraExternalIdClientId
          }
          {
            name: 'AzureAd__Domain'
            value: entraExternalIdDomain
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
            name: 'KeyVault__Url'
            value: keyVaultUrl
          }
          {
            name: 'KeyVault__CertificateName'
            value: certificateName
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
    registries: [
      {
        server: containerRegistry.outputs.loginServer
        username: containerRegistry.outputs.name
        passwordSecretRef: 'acr-password'
      }
    ]
    secrets: [
      {
        name: 'acr-password'
        value: acr.listCredentials().passwords[0].value
      }
    ]
  }
}

// ============================================================================
// Reference to your Key Vault resource
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: 'kv-ciambfwyw65gna5lu'
  scope: resourceGroup(externalidRg)
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

@description('The name of the API Service Container App')
output apiServiceName string = apiService.outputs.name

@description('The FQDN of the API Service Container App')
output apiServiceFqdn string = apiService.outputs.fqdn

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

// ============================================================================
// Assign Key Vault Certificate User role to the managed identity
// ============================================================================

module keyVaultAccessModule './keyvault-access.bicep' = {
  name: 'keyVaultAccessModule'
  scope: resourceGroup(externalidRg)
  params: {
    keyVaultName: keyVault.name
    principalId: webFrontend.outputs.?systemAssignedMIPrincipalId ?? ''
    principalType: 'ServicePrincipal'
    roleName: 'Key Vault Certificate User'
  }
}
