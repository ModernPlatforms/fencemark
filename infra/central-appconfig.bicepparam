using './central-appconfig.bicep'

// ============================================================================
// Central App Configuration Parameters
// ============================================================================
// This central App Config serves all environments (dev, staging, prod)
// using labels to differentiate configuration per environment.
// ============================================================================

param location = readEnvironmentVariable('AZURE_LOCATION', 'australiaeast')

param centralConfigResourceGroupName = 'rg-fencemark-central-config'

param appConfigNamePrefix = 'appcs-fencemark'

// ============================================================================
// Tags
// ============================================================================

param tags = {
  project: 'fencemark'
  environment: 'shared'
  purpose: 'central-configuration'
}
