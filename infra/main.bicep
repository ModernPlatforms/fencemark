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
// Azure Entra External ID (CIAM) Parameters
// ============================================================================

@description('Enable Azure Entra External ID tenant deployment')
param enableEntraExternalId bool = false

@description('The name of the CIAM tenant (will become {name}.onmicrosoft.com). Max 26 chars, alphanumeric only.')
@maxLength(26)
param ciamTenantName string = ''

@description('The data residency location for the CIAM tenant')
@allowed([
  'United States'
  'Europe'
  'Asia Pacific'
  'Australia'
])
param ciamLocation string = 'United States'

@description('The display name for the CIAM tenant')
param ciamDisplayName string = 'Fencemark Identity'

@description('Country code for data residency (e.g., US, GB, DE, AU)')
param ciamCountryCode string = 'US'

@description('The SKU name for the CIAM tenant')
@allowed([
  'Base'
  'PremiumP1'
  'PremiumP2'
])
param ciamSkuName string = 'Base'

@description('The SKU tier for the CIAM tenant')
param ciamSkuTier string = 'A0'

@description('Custom domain name for sign-in experience (e.g., login.fencemark.com)')
param customDomain string = ''

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
  scope: rg
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
          {
            name: 'AzureMaps__ClientId'
            value: mapsAccount.outputs.resourceId
          }
          {
            name: 'AzureAd__Instance'
            value: enableEntraExternalId ? 'https://${ciamTenantName}.ciamlogin.com/' : ''
          }
          {
            name: 'AzureAd__TenantId'
            // TenantId will be available after CIAM deployment - initially empty, update via app settings
            value: ''
          }
          {
            name: 'AzureAd__ClientId'
            value: '' // Set after manual app registration in CIAM tenant
          }
          {
            name: 'AzureAd__Domain'
            value: enableEntraExternalId ? '${ciamTenantName}.onmicrosoft.com' : ''
          }
          {
            name: 'AzureAd__CallbackPath'
            value: '/signin-oidc'
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
// Azure Entra External ID Configuration
// ============================================================================

module entraExternalId './entra-external-id.bicep' = if (enableEntraExternalId) {
  name: 'entraExternalId'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    tags: defaultTags
    ciamTenantName: ciamTenantName
    ciamLocation: ciamLocation
    ciamDisplayName: ciamDisplayName
    ciamCountryCode: ciamCountryCode
    ciamSkuName: ciamSkuName
    ciamSkuTier: ciamSkuTier
    customDomain: customDomain
    // The redirect URI will be determined after webFrontend deployment
    // It follows pattern: https://{containerAppName}.{envDefaultDomain}/signin-oidc
    webFrontendRedirectUri: ''
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

@description('The name of the Azure Maps Account')
output mapsAccountName string = mapsAccount.outputs.name

@description('The resource ID of the Azure Maps Account')
output mapsAccountResourceId string = mapsAccount.outputs.resourceId

// ============================================================================
// Azure Entra External ID (CIAM) Outputs
// ============================================================================

@description('The tenant ID for Azure Entra External ID CIAM')
output entraExternalIdTenantId string = entraExternalId.?outputs.?tenantId ?? ''

@description('The domain name for Azure Entra External ID (e.g., tenant.onmicrosoft.com)')
output entraExternalIdDomainName string = entraExternalId.?outputs.?domainName ?? ''

@description('The CIAM tenant name')
output entraExternalIdCiamTenantName string = entraExternalId.?outputs.?ciamTenantName ?? ''

@description('The CIAM login URL')
output entraExternalIdCiamLoginUrl string = entraExternalId.?outputs.?ciamLoginUrl ?? ''

@description('The authority URL for authentication')
output entraExternalIdAuthorityUrl string = entraExternalId.?outputs.?authorityUrl ?? ''

@description('The OIDC configuration endpoint')
output entraExternalIdOidcEndpoint string = entraExternalId.?outputs.?oidcConfigurationEndpoint ?? ''

@description('The Key Vault name storing Entra External ID secrets')
output entraExternalIdKeyVaultName string = entraExternalId.?outputs.?keyVaultName ?? ''

@description('The custom domain for sign-in (if configured)')
output entraExternalIdCustomDomain string = entraExternalId.?outputs.?customDomain ?? ''

@description('DNS zone name servers for custom domain verification')
output entraExternalIdDnsNameServers array = entraExternalId.?outputs.?dnsZoneNameServers ?? []

@description('The redirect URI to configure in app registration (use this value after deployment)')
output entraExternalIdRedirectUri string = 'https://${webFrontend.outputs.fqdn}/signin-oidc'

