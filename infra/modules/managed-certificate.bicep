// ============================================================================
// Managed Certificate for Container Apps Environment
// ============================================================================
// Creates a managed SSL certificate for a custom domain
// ============================================================================

@description('The name of the certificate')
param name string

@description('Location for the certificate')
param location string

@description('The resource ID of the Container Apps Environment')
param environmentId string

@description('The custom domain name for the certificate')
param subjectName string

@description('Tags to apply to the certificate')
param tags object = {}

resource managedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = {
  name: name
  location: location
  tags: tags
  parent: containerAppsEnvironment
  properties: {
    subjectName: subjectName
    domainControlValidation: 'CNAME'
  }
}

// Reference to parent environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: split(environmentId, '/')[8]
}

@description('The resource ID of the managed certificate')
output resourceId string = managedCertificate.id

@description('The name of the managed certificate')
output certificateName string = managedCertificate.name
