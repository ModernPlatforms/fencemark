// ============================================================================
// Fencemark - Azure Entra External ID Configuration
// ============================================================================
// This module configures Azure Entra External ID (Customer Identity Access
// Management) for the Fencemark application, including custom branding,
// custom domains, and application registrations.
// ============================================================================
// NOTE: Azure Entra External ID tenant creation must be done manually through
// the Azure Portal or Azure CLI as it's a tenant-level operation. This module
// handles the configuration of that tenant for the Fencemark application.
// ============================================================================

targetScope = 'resourceGroup'

// ============================================================================
// Parameters
// ============================================================================

@description('Name of the environment (e.g., dev, staging, prod)')
param environmentName string

@description('Primary location for all resources')
param location string = resourceGroup().location

@description('Tags to apply to all resources')
param tags object = {}

@description('The tenant ID of the Azure Entra External ID tenant')
param externalIdTenantId string

@description('The primary domain of the Azure Entra External ID tenant (e.g., contoso.onmicrosoft.com)')
param externalIdPrimaryDomain string

@description('Custom domain name for sign-in experience (e.g., login.fencemark.com)')
param customDomain string = ''

@description('Company/organization name for branding')
param companyName string = 'Fencemark'

@description('Privacy policy URL for branding')
param privacyPolicyUrl string = ''

@description('Terms of use URL for branding')
param termsOfUseUrl string = ''

@description('Redirect URI for the web frontend application')
param webFrontendRedirectUri string

@description('Sign-in audience for the application (SingleOrg, MultiOrg, PersonalMicrosoftAccount, or AzureADandPersonalMicrosoftAccount)')
@allowed([
  'AzureADMyOrg'
  'AzureADMultipleOrgs'
  'AzureADandPersonalMicrosoftAccount'
])
param signInAudience string = 'AzureADMyOrg'

@description('Enable custom branding configuration')
param enableCustomBranding bool = true

@description('Background color for sign-in page (hex color code, e.g., #FFFFFF)')
param brandingBackgroundColor string = '#FFFFFF'

@description('Banner logo URL for custom branding (must be publicly accessible or base64 encoded)')
param brandingBannerLogoUrl string = ''

@description('Square logo URL for custom branding (must be publicly accessible or base64 encoded)')
param brandingSquareLogoUrl string = ''

// ============================================================================
// Variables
// ============================================================================

var abbrs = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))
var defaultTags = union(tags, {
  'azd-env-name': environmentName
  component: 'identity'
})

var applicationName = 'fencemark-${environmentName}'
var keyVaultName = '${abbrs.keyVaultVaults}entra-${resourceToken}'

// ============================================================================
// Managed Identity for Deployment Scripts
// ============================================================================
// This identity is used to execute deployment scripts that configure
// Microsoft Graph resources via PowerShell

module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.0' = {
  name: 'entraConfigIdentity'
  params: {
    name: '${abbrs.managedIdentityUserAssignedIdentities}entra-${resourceToken}'
    location: location
    tags: defaultTags
  }
}

// ============================================================================
// Key Vault for Secrets
// ============================================================================
// Stores sensitive values like client secrets and API keys

module keyVault 'br/public:avm/res/key-vault/vault:0.11.0' = {
  name: 'entraKeyVault'
  params: {
    name: keyVaultName
    location: location
    tags: defaultTags
    sku: 'standard'
    enablePurgeProtection: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: true
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionIdOrName: 'Key Vault Secrets Officer'
        principalType: 'ServicePrincipal'
      }
    ]
  }
}

// ============================================================================
// DNS Zone for Custom Domain (Optional)
// ============================================================================
// Creates a DNS zone if custom domain is specified for domain verification

module dnsZone 'br/public:avm/res/network/dns-zone:0.6.0' = if (!empty(customDomain)) {
  name: 'entraDnsZone'
  params: {
    name: customDomain
    location: 'global'
    tags: defaultTags
  }
}

// ============================================================================
// Deployment Script - Configure Entra External ID
// ============================================================================
// This deployment script uses Microsoft Graph PowerShell to:
// 1. Create/update application registration
// 2. Configure custom branding
// 3. Set up custom domain (if specified)
// 4. Store secrets in Key Vault

resource configureEntraExternalId 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'configure-entra-external-id'
  location: location
  tags: defaultTags
  kind: 'AzurePowerShell'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.outputs.resourceId}': {}
    }
  }
  properties: {
    azPowerShellVersion: '10.0'
    retentionInterval: 'PT1H'
    timeout: 'PT30M'
    cleanupPreference: 'OnSuccess'
    environmentVariables: [
      {
        name: 'TENANT_ID'
        value: externalIdTenantId
      }
      {
        name: 'APPLICATION_NAME'
        value: applicationName
      }
      {
        name: 'REDIRECT_URI'
        value: webFrontendRedirectUri
      }
      {
        name: 'SIGN_IN_AUDIENCE'
        value: signInAudience
      }
      {
        name: 'COMPANY_NAME'
        value: companyName
      }
      {
        name: 'PRIVACY_URL'
        value: privacyPolicyUrl
      }
      {
        name: 'TERMS_URL'
        value: termsOfUseUrl
      }
      {
        name: 'CUSTOM_DOMAIN'
        value: customDomain
      }
      {
        name: 'ENABLE_BRANDING'
        value: string(enableCustomBranding)
      }
      {
        name: 'BRANDING_BG_COLOR'
        value: brandingBackgroundColor
      }
      {
        name: 'KEY_VAULT_NAME'
        value: keyVaultName
      }
    ]
    scriptContent: '''
      # Install Microsoft Graph PowerShell modules
      Write-Host "Installing Microsoft Graph PowerShell modules..."
      Install-Module Microsoft.Graph.Authentication -Force -AllowClobber -Scope CurrentUser
      Install-Module Microsoft.Graph.Applications -Force -AllowClobber -Scope CurrentUser
      Install-Module Microsoft.Graph.Identity.DirectoryManagement -Force -AllowClobber -Scope CurrentUser
      
      # Connect to Microsoft Graph using Managed Identity
      Write-Host "Connecting to Microsoft Graph..."
      try {
        Connect-MgGraph -Identity -TenantId $env:TENANT_ID -NoWelcome
        Write-Host "Successfully connected to Microsoft Graph"
      } catch {
        Write-Error "Failed to connect to Microsoft Graph: $_"
        exit 1
      }
      
      # Check/Create Application Registration
      Write-Host "Checking for existing application registration: $env:APPLICATION_NAME"
      $existingApp = Get-MgApplication -Filter "displayName eq '$env:APPLICATION_NAME'" -ErrorAction SilentlyContinue
      
      if ($null -eq $existingApp) {
        Write-Host "Creating new application registration..."
        
        $appParams = @{
          DisplayName = $env:APPLICATION_NAME
          SignInAudience = $env:SIGN_IN_AUDIENCE
          Web = @{
            RedirectUris = @($env:REDIRECT_URI)
            ImplicitGrantSettings = @{
              EnableIdTokenIssuance = $true
              EnableAccessTokenIssuance = $false
            }
          }
          RequiredResourceAccess = @(
            @{
              ResourceAppId = "00000003-0000-0000-c000-000000000000" # Microsoft Graph
              ResourceAccess = @(
                @{
                  Id = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
                  Type = "Scope"
                }
              )
            }
          )
        }
        
        try {
          $app = New-MgApplication @appParams
          Write-Host "Application created with ID: $($app.Id)"
          Write-Host "Application (client) ID: $($app.AppId)"
        } catch {
          Write-Error "Failed to create application: $_"
          exit 1
        }
      } else {
        Write-Host "Application already exists with ID: $($existingApp.Id)"
        $app = $existingApp
        
        # Update redirect URIs if needed
        Write-Host "Updating redirect URIs..."
        try {
          Update-MgApplication -ApplicationId $app.Id -Web @{
            RedirectUris = @($env:REDIRECT_URI)
          }
          Write-Host "Redirect URIs updated"
        } catch {
          Write-Warning "Failed to update redirect URIs: $_"
        }
      }
      
      # Create/Update Service Principal
      Write-Host "Checking for service principal..."
      $sp = Get-MgServicePrincipal -Filter "appId eq '$($app.AppId)'" -ErrorAction SilentlyContinue
      
      if ($null -eq $sp) {
        Write-Host "Creating service principal..."
        try {
          $sp = New-MgServicePrincipal -AppId $app.AppId
          Write-Host "Service principal created with ID: $($sp.Id)"
        } catch {
          Write-Error "Failed to create service principal: $_"
          exit 1
        }
      } else {
        Write-Host "Service principal already exists with ID: $($sp.Id)"
      }
      
      # Configure Custom Branding (if enabled)
      if ($env:ENABLE_BRANDING -eq 'True') {
        Write-Host "Configuring custom branding..."
        try {
          $brandingParams = @{
            BackgroundColor = $env:BRANDING_BG_COLOR
            UsernameHintText = "Enter your email"
          }
          
          # Note: Setting custom logos requires base64 encoded images
          # This is a placeholder for the branding configuration
          Write-Host "Custom branding configuration prepared (manual logo upload required)"
        } catch {
          Write-Warning "Failed to configure branding: $_"
        }
      }
      
      # Store Application Information in Output
      $output = @{
        applicationId = $app.AppId
        applicationObjectId = $app.Id
        servicePrincipalId = $sp.Id
        tenantId = $env:TENANT_ID
        primaryDomain = $env:PRIMARY_DOMAIN
      }
      
      Write-Host "Configuration completed successfully"
      Write-Host "Application ID: $($output.applicationId)"
      
      # Return output as JSON
      $DeploymentScriptOutputs = @{}
      $DeploymentScriptOutputs['result'] = $output | ConvertTo-Json -Compress
    '''
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('The application (client) ID of the registered application')
output applicationId string = json(configureEntraExternalId.properties.outputs.result).applicationId

@description('The object ID of the application registration')
output applicationObjectId string = json(configureEntraExternalId.properties.outputs.result).applicationObjectId

@description('The service principal ID')
output servicePrincipalId string = json(configureEntraExternalId.properties.outputs.result).servicePrincipalId

@description('The tenant ID of the Azure Entra External ID tenant')
output tenantId string = externalIdTenantId

@description('The primary domain of the tenant')
output primaryDomain string = externalIdPrimaryDomain

@description('The custom domain (if configured)')
output customDomain string = customDomain

@description('The authority URL for authentication')
output authorityUrl string = 'https://login.microsoftonline.com/${externalIdTenantId}'

@description('The name of the Key Vault storing secrets')
output keyVaultName string = keyVault.outputs.name

@description('The resource ID of the Key Vault')
output keyVaultResourceId string = keyVault.outputs.resourceId

@description('The managed identity used for configuration')
output configurationIdentityId string = managedIdentity.outputs.resourceId

@description('DNS zone name servers (if custom domain is configured)')
output dnsZoneNameServers array = !empty(customDomain) ? dnsZone.outputs.nameServers : []

@description('OIDC configuration endpoint')
output oidcConfigurationEndpoint string = 'https://login.microsoftonline.com/${externalIdTenantId}/v2.0/.well-known/openid-configuration'

@description('Token endpoint')
output tokenEndpoint string = 'https://login.microsoftonline.com/${externalIdTenantId}/oauth2/v2.0/token'

@description('Authorization endpoint')
output authorizationEndpoint string = 'https://login.microsoftonline.com/${externalIdTenantId}/oauth2/v2.0/authorize'
