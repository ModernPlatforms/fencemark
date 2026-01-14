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

@description('GitHub Actions service principal ID (for deployment permissions)')
param githubActionsPrincipalId string = ''

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
      useSubDomainName: true  // Use asverify subdomain for validation
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

// $web container for static website hosting
resource webContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '$web'
  parent: blobService
  properties: {
    publicAccess: 'Blob'
  }
}

// Grant GitHub Actions service principal permissions to upload blobs
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(githubActionsPrincipalId)) {
  name: guid(storageAccount.id, githubActionsPrincipalId, 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe') // Storage Blob Data Contributor
    principalId: githubActionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Note: Static website hosting must be enabled post-deployment
// This can be done via:
// 1. Azure CLI: az storage blob service-properties update --account-name <name> --static-website --index-document index.html --404-document index.html
// 2. azd hooks (in azure.yaml)
// 3. GitHub Actions/Azure DevOps pipeline step
// 4. Azure Portal: Storage account -> Static website -> Enabled

var staticWebsiteHost = replace(replace(storageAccount.properties.primaryEndpoints.web, 'https://', ''), '/', '')

// Front Door endpoint naming
var frontDoorEndpointName = 'endpoint-${uniqueString(name)}'

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
  }
}

// ============================================================================
// Azure Front Door Standard (Recommended for Production)
// ============================================================================

resource frontDoorProfile 'Microsoft.Cdn/profiles@2024-02-01' = if (effectiveCdnMode == 'frontdoor') {
  name: 'afd-${name}'
  location: 'global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
  tags: tags
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-02-01' = if (effectiveCdnMode == 'frontdoor') {
  name: frontDoorEndpointName
  parent: frontDoorProfile
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource frontDoorOriginGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = if (effectiveCdnMode == 'frontdoor') {
  name: 'storage-origin-group'
  parent: frontDoorProfile
  properties: {
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
}

resource frontDoorOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = if (effectiveCdnMode == 'frontdoor') {
  name: 'storage-origin'
  parent: frontDoorOriginGroup
  properties: {
    hostName: staticWebsiteHost
    httpPort: 80
    httpsPort: 443
    originHostHeader: staticWebsiteHost
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

resource frontDoorRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = if (effectiveCdnMode == 'frontdoor') {
  name: 'default-route'
  parent: frontDoorEndpoint
  properties: {
    originGroup: {
      id: frontDoorOriginGroup.id
    }
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
  dependsOn: [
    frontDoorOrigin
  ]
}

resource frontDoorCustomDomain 'Microsoft.Cdn/profiles/customDomains@2024-02-01' = if (effectiveCdnMode == 'frontdoor' && !empty(customDomainName)) {
  name: replace(customDomainName, '.', '-')
  parent: frontDoorProfile
  properties: {
    hostName: customDomainName
    tlsSettings: enableCustomDomainHttps ? {
      certificateType: 'ManagedCertificate'
      minimumTlsVersion: 'TLS12'
    } : null
  }
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Static website URL')
output staticWebsiteUrl string = storageAccount.properties.primaryEndpoints.web

@description('CDN endpoint hostname (empty if CDN is disabled)')
output cdnHostname string = effectiveCdnMode == 'classic-cdn' ? cdnEndpoint!.properties.hostName : ''

@description('Front Door endpoint hostname (empty if Front Door is not used)')
output frontDoorEndpointHostname string = effectiveCdnMode == 'frontdoor' ? frontDoorEndpoint!.properties.hostName : ''

@description('Primary hostname for the static website (custom domain, AFD, CDN, or storage)')
output primaryHostname string = !empty(customDomainName) ? customDomainName : effectiveCdnMode == 'frontdoor' ? frontDoorEndpoint!.properties.hostName : effectiveCdnMode == 'classic-cdn' ? cdnEndpoint!.properties.hostName : staticWebsiteHost

@description('CDN/Front Door mode used')
output cdnMode string = effectiveCdnMode
