// ============================================================================
// Static Website (Storage + Optional CDN) Module
// ============================================================================

@description('Name of the storage account for the static website')
param name string

@description('Location for the storage account')
param location string

@description('Tags to apply to resources')
param tags object = {}

@description('Storage account SKU')
param storageSku string = 'Standard_LRS'

@description('Enable Azure CDN')
param enableCdn bool = false

@description('Azure CDN SKU')
param cdnSku string = 'Standard_Microsoft'

@description('Custom domain name for CDN (optional)')
param customDomainName string = ''

@description('Enable custom domain on CDN')
param enableCustomDomain bool = false

@description('Enable CDN managed HTTPS for the custom domain')
param enableCustomDomainHttps bool = false

@description('Static website index document')
param indexDocument string = 'index.html'

@description('Static website error document (404)')
param errorDocument404Path string = 'index.html'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: name
  location: location
  sku: {
    name: storageSku
  }
  kind: 'StorageV2'
  tags: tags
  properties: {
    allowBlobPublicAccess: true
    enableHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: '${storageAccount.name}/default'
  properties: {
    staticWebsite: {
      indexDocument: indexDocument
      error404Document: errorDocument404Path
    }
  }
}

var staticWebsiteHost = replace(replace(storageAccount.properties.primaryEndpoints.web, 'https://', ''), '/', '')

resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = if (enableCdn) {
  name: 'cdnp-${name}'
  location: 'global'
  sku: {
    name: cdnSku
  }
  tags: tags
}

resource cdnEndpoint 'Microsoft.Cdn/profiles/endpoints@2023-05-01' = if (enableCdn) {
  name: '${cdnProfile.name}/static-site'
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

resource cdnCustomDomain 'Microsoft.Cdn/profiles/endpoints/customDomains@2023-05-01' = if (enableCdn && enableCustomDomain && !empty(customDomainName)) {
  name: '${cdnProfile.name}/static-site/${replace(customDomainName, \'.\', \'-\')}'
  properties: {
    hostName: customDomainName
    customHttpsParameters: enableCustomDomainHttps ? {
      certificateSource: 'Cdn'
      protocolType: 'ServerNameIndication'
      minimumTlsVersion: 'TLS12'
    } : null
  }
}

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Static website URL')
output staticWebsiteUrl string = storageAccount.properties.primaryEndpoints.web

@description('CDN endpoint hostname (empty if CDN is disabled)')
output cdnHostname string = enableCdn ? cdnEndpoint.properties.hostName : ''
