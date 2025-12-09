// ============================================================================
// Azure Entra External ID (CIAM) Directory Module
// ============================================================================
// This module deploys the CIAM directory resource at resource group scope
// ============================================================================

targetScope = 'resourceGroup'

@description('The CIAM domain name (e.g., tenant.onmicrosoft.com)')
param ciamDomainName string

@description('The data residency location for the CIAM tenant')
param ciamLocation string

@description('Tags to apply to the resource')
param tags object

@description('The SKU name for the CIAM tenant')
param ciamSkuName string

@description('The SKU tier for the CIAM tenant')
param ciamSkuTier string

@description('Country code for data residency')
param ciamCountryCode string

@description('Display name for the CIAM tenant')
param ciamDisplayName string

// ============================================================================
// CIAM Directory Resource
// ============================================================================

resource ciamDirectory 'Microsoft.AzureActiveDirectory/ciamDirectories@2025-08-01-preview' = {
  #disable-next-line BCP335
  name: ciamDomainName
  location: ciamLocation
  tags: tags
  sku: {
    name: ciamSkuName
    tier: ciamSkuTier
  }

  properties: any({
    createTenantProperties: {
      countryCode: ciamCountryCode
      displayName: ciamDisplayName
      IsGoLocalTenant: true
    }
  })
}

// ============================================================================
// Outputs
// ============================================================================

@description('The tenant ID of the CIAM directory')
output tenantId string = ciamDirectory.properties.tenantId ?? ''

@description('The domain name of the CIAM tenant')
output domainName string = ciamDirectory.properties.domainName ?? ciamDomainName

@description('The provisioning state of the CIAM directory')
output provisioningState string = ciamDirectory.properties.provisioningState ?? 'Unknown'

@description('The resource ID of the CIAM directory')
output resourceId string = ciamDirectory.id
