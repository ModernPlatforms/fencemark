// ============================================================================
// DNS Zone for fencemark.com.au
// ============================================================================
// NOTE: This file is now integrated into central-appconfig.bicep
// Use central-appconfig.bicep for deployment instead.
// This file is kept for reference only.
//
// This Bicep file creates a central DNS zone for the fencemark.com.au domain
// in the central resource group. This zone is shared across all environments.
//
// Deploy this separately with:
// az deployment group create --resource-group rg-fencemark-central-config --template-file dns-zone.bicep --parameters dns-zone.bicepparam

targetScope = 'resourceGroup'

// ============================================================================
// Parameters
// ============================================================================

@description('The domain name for the DNS zone')
param domainName string

@description('Azure region for the resource')
param location string = resourceGroup().location

@description('Tags to apply to resources')
param tags object = {}

// ============================================================================
// DNS Zone
// ============================================================================

resource dnsZone 'Microsoft.Network/dnsZones@2023-07-01-preview' = {
  name: domainName
  location: 'global' // DNS zones are always global
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

@description('The name servers for the DNS zone - Configure these at your domain registrar')
output nameServers array = dnsZone.properties.nameServers
