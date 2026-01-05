// ============================================================================
// DNS Zone Module
// ============================================================================
// Simple module to deploy a DNS zone to a resource group

targetScope = 'resourceGroup'

// ============================================================================
// Parameters
// ============================================================================

@description('The domain name for the DNS zone')
param domainName string

@description('Tags to apply to the DNS zone')
param tags object = {}

// ============================================================================
// DNS Zone
// ============================================================================

resource dnsZone 'Microsoft.Network/dnsZones@2023-07-01-preview' = {
  name: domainName
  location: 'global'
  tags: tags
  properties: {
    zoneType: 'Public'
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The name of the DNS zone')
output dnsZoneName string = dnsZone.name

@description('The resource ID of the DNS zone')
output dnsZoneId string = dnsZone.id

@description('The name servers for the DNS zone')
output nameServers array = dnsZone.properties.nameServers
