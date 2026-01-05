// ============================================================================
// DNS Zone Parameters for fencemark.com.au
// ============================================================================

using './dns-zone.bicep'

param domainName = 'fencemark.com.au'
param tags = {
  Environment: 'shared'
  Purpose: 'DNS'
}
