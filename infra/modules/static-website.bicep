// ============================================================================
// Static Website (Storage + Optional CDN/Front Door) Module
// ============================================================================

@description('Name of the storage account for the static website')
param name string

@description('Location for the storage account')
param location string

@description('Tags to apply to resources')
param tags object = {}

@description('Storage account SKU')
param storageSku string = 'Standard_LRS'

@description('CDN/Front Door mode: none, classic-cdn (deprecated), or frontdoor')
@allowed([
  'none'
  'classic-cdn'
  'frontdoor'
])
param cdnMode string = 'none'

@description('Azure CDN SKU (for classic-cdn mode only - deprecated)')
param cdnSku string = 'Standard_Microsoft'

@description('Custom domain name (optional)')
param customDomainName string = ''

@description('Enable custom domain HTTPS (for Front Door or classic CDN)')
param enableCustomDomainHttps bool = true

@description('Static website index document')
param indexDocument string = 'index.html'

@description('Static website error document (404)')
param errorDocument404Path string = 'index.html'

// Backwards compatibility parameters (deprecated)
@description('Enable Azure CDN (deprecated - use cdnMode instead)')
param enableCdn bool = false

// Determine effective CDN mode (handle backwards compatibility)
var effectiveCdnMode = enableCdn ? 'classic-cdn' : cdnMode

// For storage custom domain: only use when mode is 'none' and custom domain is specified
var useStorageCustomDomain = effectiveCdnMode == 'none' && !empty(customDomainName)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  sku: {
    name: storageSku
  }
  kind: 'StorageV2'
  tags: tags
  properties: {
    allowBlobPublicAccess: true
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    customDomain: useStorageCustomDomain ? {
      name: customDomainName
      useSubDomainName: false
    } : null
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    cors: {
      corsRules: []
    }
  }
}

// Deployment script to enable static website hosting
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-static-website-${uniqueString(name)}'
  location: location
  tags: tags
}

// Role assignment for the managed identity to manage storage
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, managedIdentity.id, 'StorageBlobDataContributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource staticWebsiteScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'enable-static-website-${uniqueString(name)}'
  location: location
  kind: 'AzureCLI'
  tags: tags
  properties: {
    azCliVersion: '2.52.0'
    timeout: 'PT10M'
    retentionInterval: 'PT1H'
    cleanupPreference: 'OnSuccess'
    environmentVariables: [
      {
        name: 'STORAGE_ACCOUNT_NAME'
        value: storageAccount.name
      }
      {
        name: 'INDEX_DOCUMENT'
        value: indexDocument
      }
      {
        name: 'ERROR_DOCUMENT'
        value: errorDocument404Path
      }
    ]
    scriptContent: '''
      az storage blob service-properties update \
        --account-name "$STORAGE_ACCOUNT_NAME" \
        --static-website \
        --index-document "$INDEX_DOCUMENT" \
        --404-document "$ERROR_DOCUMENT"
    '''
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  dependsOn: [
    roleAssignment
  ]
}

var staticWebsiteHost = replace(replace(storageAccount.properties.primaryEndpoints.web, 'https://', ''), '/', '')

// ============================================================================
// Classic CDN (Deprecated - for backwards compatibility only)
// ============================================================================

resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = if (effectiveCdnMode == 'classic-cdn') {
  name: 'cdnp-${name}'
  location: 'global'
  sku: {
    name: cdnSku
  }
  tags: tags
}

resource cdnEndpoint 'Microsoft.Cdn/profiles/endpoints@2023-05-01' = if (effectiveCdnMode == 'classic-cdn') {
  name: 'static-site'
  parent: cdnProfile
  location: 'global'
  properties: {
    isHttpAllowed: false
    isHttpsAllowed: true
    originHostHeader: staticWebsiteHost
    origins: [
      {
        name: 'storage-origin'
        properties: {
          hostName: staticWebsiteHost
        }
      }
    ]
  }
}

resource cdnCustomDomain 'Microsoft.Cdn/profiles/endpoints/customDomains@2023-05-01' = if (effectiveCdnMode == 'classic-cdn' && !empty(customDomainName)) {
  name: replace(customDomainName, '.', '-')
  parent: cdnEndpoint
  properties: {
    hostName: customDomainName
    customHttpsParameters: enableCustomDomainHttps ? {
      certificateSource: 'Cdn'
      protocolType: 'ServerNameIndication'
      minimumTlsVersion: 'TLS12'
    } : null
  }
}

// ============================================================================
// Azure Front Door Standard (Recommended for Production)
// ============================================================================

module frontDoor 'br/public:avm/res/cdn/profile:0.17.0' = if (effectiveCdnMode == 'frontdoor') {
  name: 'afd-${uniqueString(name)}'
  params: {
    name: 'afd-${name}'
    sku: 'Standard_AzureFrontDoor'
    location: 'global'
    tags: tags
    
    originGroups: [
      {
        name: 'storage-origin-group'
        loadBalancingSettings: {
          sampleSize: 4
          successfulSamplesRequired: 3
          additionalLatencyInMilliseconds: 50
        }
        healthProbeSettings: {
          probePath: '/'
          probeRequestType: 'HEAD'
          probeProtocol: 'Https'
          probeIntervalInSeconds: 100
        }
        sessionAffinityState: 'Disabled'
      }
    ]
    
    origins: [
      {
        name: 'storage-origin'
        originGroupName: 'storage-origin-group'
        hostName: staticWebsiteHost
        httpPort: 80
        httpsPort: 443
        originHostHeader: staticWebsiteHost
        priority: 1
        weight: 1000
        enabledState: 'Enabled'
        enforceCertificateNameCheck: true
      }
    ]
    
    afdEndpoints: [
      {
        name: 'endpoint-${uniqueString(name)}'
        enabledState: 'Enabled'
      }
    ]
    
    routes: [
      {
        name: 'default-route'
        afdEndpointName: 'endpoint-${uniqueString(name)}'
        originGroupName: 'storage-origin-group'
        supportedProtocols: [
          'Http'
          'Https'
        ]
        patternsToMatch: [
          '/*'
        ]
        forwardingProtocol: 'HttpsOnly'
        linkToDefaultDomain: 'Enabled'
        httpsRedirect: 'Enabled'
        enabledState: 'Enabled'
      }
    ]
    
    customDomains: !empty(customDomainName) ? [
      {
        name: replace(customDomainName, '.', '-')
        hostName: customDomainName
        certificateType: enableCustomDomainHttps ? 'ManagedCertificate' : 'CustomerCertificate'
        minimumTlsVersion: 'TLS12'
        afdEndpointName: 'endpoint-${uniqueString(name)}'
      }
    ] : []
  }
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Static website URL')
output staticWebsiteUrl string = storageAccount.properties.primaryEndpoints.web

@description('CDN endpoint hostname (empty if CDN is disabled)')
output cdnHostname string = effectiveCdnMode == 'classic-cdn' ? cdnEndpoint.properties.hostName : ''

@description('Front Door endpoint hostname (empty if Front Door is not used)')
output frontDoorEndpointHostname string = effectiveCdnMode == 'frontdoor' ? frontDoor.outputs.name : ''

@description('Primary hostname for the static website (custom domain, AFD, CDN, or storage)')
output primaryHostname string = !empty(customDomainName) ? customDomainName : effectiveCdnMode == 'frontdoor' ? frontDoor.outputs.name : effectiveCdnMode == 'classic-cdn' ? cdnEndpoint.properties.hostName : staticWebsiteHost

@description('CDN/Front Door mode used')
output cdnMode string = effectiveCdnMode
