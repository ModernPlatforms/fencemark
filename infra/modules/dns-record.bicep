// ============================================================================
// DNS Record Module
// ============================================================================
// Creates a DNS record (A or CNAME) in the central DNS zone

targetScope = 'resourceGroup'

// ============================================================================
// Parameters
// ============================================================================

@description('The name of the DNS zone')
param dnsZoneName string

@description('The subdomain name (e.g., "dev" for dev.fencemark.com.au)')
param recordName string

@description('The type of DNS record')
@allowed(['A', 'CNAME', 'TXT'])
param recordType string

@description('The TTL in seconds')
param ttl int = 3600

@description('The target value for the record (FQDN for CNAME, IP for A, text for TXT)')
param targetValue string

// ============================================================================
// DNS Zone Reference
// ============================================================================

resource dnsZone 'Microsoft.Network/dnsZones@2023-07-01-preview' existing = {
  name: dnsZoneName
}

// ============================================================================
// DNS Records
// ============================================================================

resource cnameRecord 'Microsoft.Network/dnsZones/CNAME@2023-07-01-preview' = if (recordType == 'CNAME') {
  parent: dnsZone
  name: recordName
  properties: {
    TTL: ttl
    CNAMERecord: {
      cname: targetValue
    }
  }
}

resource aRecord 'Microsoft.Network/dnsZones/A@2023-07-01-preview' = if (recordType == 'A') {
  parent: dnsZone
  name: recordName
  properties: {
    TTL: ttl
    ARecords: [
      {
        ipv4Address: targetValue
      }
    ]
  }
}

resource txtRecord 'Microsoft.Network/dnsZones/TXT@2023-07-01-preview' = if (recordType == 'TXT') {
  parent: dnsZone
  name: recordName
  properties: {
    TTL: ttl
    TXTRecords: [
      {
        value: [
          targetValue
        ]
      }
    ]
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The FQDN of the created record')
output fqdn string = recordType == 'CNAME' ? cnameRecord.properties.fqdn : (recordType == 'A' ? aRecord.properties.fqdn : txtRecord.properties.fqdn)
