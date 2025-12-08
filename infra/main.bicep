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
// Azure Entra External ID Parameters
// ============================================================================

@description('Enable Azure Entra External ID configuration')
param enableEntraExternalId bool = false

@description('The tenant ID of the Azure Entra External ID tenant')
param externalIdTenantId string = ''

@description('The primary domain of the Azure Entra External ID tenant (e.g., contoso.onmicrosoft.com)')
param externalIdPrimaryDomain string = ''

@description('Custom domain name for sign-in experience (e.g., login.fencemark.com)')
param customDomain string = ''

@description('Company/organization name for branding')
param companyName string = 'Fencemark'

@description('Privacy policy URL for branding')
param privacyPolicyUrl string = ''

@description('Terms of use URL for branding')
param termsOfUseUrl string = ''

@description('Enable custom branding configuration')
param enableCustomBranding bool = true

@description('Background color for sign-in page (hex color code, e.g., #FFFFFF)')
param brandingBackgroundColor string = '#0078D4'

@description('Banner logo URL for custom branding')
param brandingBannerLogoUrl string = ''

@description('Square logo URL for custom branding')
param brandingSquareLogoUrl string = ''

@description('Sign-in audience for the application')
@allowed([
  'AzureADMyOrg'
  'AzureADMultipleOrgs'
  'AzureADandPersonalMicrosoftAccount'
])
param signInAudience string = 'AzureADMyOrg'

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
// Azure Maps Account
// ============================================================================

resource mapsAccount 'Microsoft.Maps/accounts@2024-01-01-preview' = {
  name: '${abbrs.mapsAccounts}${resourceToken}'
  location: 'global'
  tags: defaultTags
  sku: {
    name: 'G2'
  }
  kind: 'Gen2'
  properties: {
    disableLocalAuth: false
    cors: {
      corsRules: []
    }
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
          {
            name: 'AzureMaps__ClientId'
            value: mapsAccount.id
          }
          {
            name: 'AzureAd__Instance'
            value: enableEntraExternalId ? entraExternalId.outputs.instanceUrl : ''
          }
          {
            name: 'AzureAd__TenantId'
            value: enableEntraExternalId ? externalIdTenantId : ''
          }
          {
            name: 'AzureAd__ClientId'
            value: enableEntraExternalId ? 'not-configured' : '' // Set after deployment via update script
          }
          {
            name: 'AzureAd__Domain'
            value: enableEntraExternalId ? externalIdPrimaryDomain : ''
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
  params: {
    environmentName: environmentName
    location: location
    tags: defaultTags
    externalIdTenantId: externalIdTenantId
    externalIdPrimaryDomain: externalIdPrimaryDomain
    customDomain: customDomain
    companyName: companyName
    privacyPolicyUrl: privacyPolicyUrl
    termsOfUseUrl: termsOfUseUrl
    webFrontendRedirectUri: enableEntraExternalId ? 'https://${webFrontend.outputs.fqdn}/signin-oidc' : ''
    signInAudience: signInAudience
    enableCustomBranding: enableCustomBranding
    brandingBackgroundColor: brandingBackgroundColor
    brandingBannerLogoUrl: brandingBannerLogoUrl
    brandingSquareLogoUrl: brandingSquareLogoUrl
  }
  dependsOn: [
    webFrontend
  ]
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
output mapsAccountName string = mapsAccount.name

@description('The resource ID of the Azure Maps Account')
output mapsAccountResourceId string = mapsAccount.id

// ============================================================================
// Azure Entra External ID Outputs
// ============================================================================

@description('The application (client) ID for Azure Entra External ID')
output entraExternalIdApplicationId string = enableEntraExternalId ? entraExternalId.outputs.applicationId : ''

@description('The tenant ID for Azure Entra External ID')
output entraExternalIdTenantId string = enableEntraExternalId ? entraExternalId.outputs.tenantId : ''

@description('The primary domain for Azure Entra External ID')
output entraExternalIdPrimaryDomain string = enableEntraExternalId ? entraExternalId.outputs.primaryDomain : ''

@description('The authority URL for authentication')
output entraExternalIdAuthorityUrl string = enableEntraExternalId ? entraExternalId.outputs.authorityUrl : ''

@description('The OIDC configuration endpoint')
output entraExternalIdOidcEndpoint string = enableEntraExternalId ? entraExternalId.outputs.oidcConfigurationEndpoint : ''

@description('The Key Vault name storing Entra External ID secrets')
output entraExternalIdKeyVaultName string = enableEntraExternalId ? entraExternalId.outputs.keyVaultName : ''

@description('The custom domain for sign-in (if configured)')
output entraExternalIdCustomDomain string = enableEntraExternalId ? entraExternalId.outputs.customDomain : ''

@description('DNS zone name servers for custom domain verification')
output entraExternalIdDnsNameServers array = enableEntraExternalId ? entraExternalId.outputs.dnsZoneNameServers : []

