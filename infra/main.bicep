// ============================================================================
// Fencemark Infrastructure - Azure Container Apps for .NET Aspire
// ============================================================================
// This template deploys the infrastructure required to host the Fencemark
// .NET Aspire application on Azure Container Apps.
// ============================================================================

targetScope = 'resourceGroup'

// ============================================================================
// Parameters
// ============================================================================

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g., dev, staging, prod)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

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

// ============================================================================
// Variables
// ============================================================================

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))
var defaultTags = union(tags, {
  'azd-env-name': environmentName
})

// ============================================================================
// Log Analytics Workspace
// ============================================================================

module logAnalytics 'br/public:avm/res/operational-insights/workspace:0.11.1' = {
  name: 'logAnalytics'
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
  dependsOn: [logAnalytics]
}

// ============================================================================
// Container Registry
// ============================================================================

module containerRegistry 'br/public:avm/res/container-registry/registry:0.9.1' = {
  name: 'containerRegistry'
  params: {
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    tags: defaultTags
    acrSku: 'Basic'
    acrAdminUserEnabled: true
  }
}

// ============================================================================
// Container Apps Environment
// ============================================================================

module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.11.0' = {
  name: 'containerAppsEnvironment'
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
  }
}

// ============================================================================
// API Service Container App
// ============================================================================

module apiService 'br/public:avm/res/app/container-app:0.16.0' = {
  name: 'apiService'
  params: {
    name: '${abbrs.appContainerApps}apiservice-${resourceToken}'
    location: location
    tags: union(defaultTags, {
      'azd-service-name': 'apiservice'
    })
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    containers: [
      {
        name: 'apiservice'
        image: !empty(apiServiceImage) ? apiServiceImage : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        resources: {
          cpu: json(apiServiceCpu)
          memory: apiServiceMemory
        }
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
        passwordSecretRef: 'registry-password'
      }
    ]
    secrets: [
      {
        name: 'registry-password'
        value: containerRegistry.outputs.name
      }
    ]
  }
}

// ============================================================================
// Web Frontend Container App
// ============================================================================

module webFrontend 'br/public:avm/res/app/container-app:0.16.0' = {
  name: 'webFrontend'
  params: {
    name: '${abbrs.appContainerApps}webfrontend-${resourceToken}'
    location: location
    tags: union(defaultTags, {
      'azd-service-name': 'webfrontend'
    })
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
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
            name: 'services__apiservice__http__0'
            value: 'http://${abbrs.appContainerApps}apiservice-${resourceToken}'
          }
          {
            name: 'services__apiservice__https__0'
            value: 'https://${abbrs.appContainerApps}apiservice-${resourceToken}'
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
        passwordSecretRef: 'registry-password'
      }
    ]
    secrets: [
      {
        name: 'registry-password'
        value: containerRegistry.outputs.name
      }
    ]
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
