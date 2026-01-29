<#
.SYNOPSIS
    Push API configuration settings to Azure App Configuration with environment-specific labels.

.DESCRIPTION
    This script pushes configuration values from the API appsettings to Azure App Configuration.
    Settings are labeled by environment (dev, staging, prod).
    Secrets are stored in Key Vault and referenced from App Configuration.

.PARAMETER Environment
    The environment name (dev, staging, prod). Used as the label in App Configuration.

.PARAMETER AppConfigName
    The name of the Azure App Configuration store. Defaults to 'appcs-fencemark'.

.PARAMETER ResourceGroup
    The resource group containing the App Configuration store. Defaults to 'rg-fencemark-shared'.

.PARAMETER KeyVaultName
    The name of the Key Vault for this environment. Required for secret references.

.PARAMETER CorsOrigins
    Comma-separated list of allowed CORS origins for this environment.

.PARAMETER SkipCadastral
    Skip pushing Cadastral settings (they're typically the same across environments).

.EXAMPLE
    .\Push-AppConfiguration.ps1 -Environment dev -KeyVaultName kv-fencemark-dev -CorsOrigins "https://localhost:7173,https://dev.fencemark.com.au"

.EXAMPLE
    .\Push-AppConfiguration.ps1 -Environment prod -KeyVaultName kv-fencemark-prod -CorsOrigins "https://fencemark.com.au"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$AppConfigName = 'appcs-fencemark',

    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = 'rg-fencemark-shared',

    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,

    [Parameter(Mandatory = $false)]
    [string]$CorsOrigins,

    [Parameter(Mandatory = $false)]
    [switch]$SkipCadastral
)

$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Push App Configuration - $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify Azure CLI is logged in
Write-Host "Verifying Azure CLI login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged into Azure CLI. Run 'az login' first."
    exit 1
}
Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green

# Get Key Vault URI
Write-Host "Getting Key Vault URI..." -ForegroundColor Yellow
$keyVaultUri = az keyvault show --name $KeyVaultName --query "properties.vaultUri" -o tsv
if (-not $keyVaultUri) {
    Write-Error "Could not find Key Vault: $KeyVaultName"
    exit 1
}
Write-Host "Key Vault URI: $keyVaultUri" -ForegroundColor Green

# Function to set a plain text value in App Configuration
function Set-AppConfigValue {
    param(
        [string]$Key,
        [string]$Value,
        [string]$Label = $Environment
    )

    Write-Host "  Setting $Key = $Value" -ForegroundColor Gray
    az appconfig kv set `
        --name $AppConfigName `
        --key $Key `
        --value $Value `
        --label $Label `
        --content-type "text/plain" `
        --yes `
        --only-show-errors | Out-Null
}

# Function to set a Key Vault reference in App Configuration
function Set-AppConfigKeyVaultRef {
    param(
        [string]$Key,
        [string]$SecretName,
        [string]$Label = $Environment
    )

    $kvRefValue = "{`"uri`":`"${keyVaultUri}secrets/${SecretName}`"}"
    Write-Host "  Setting $Key -> KeyVault:$SecretName" -ForegroundColor Gray
    az appconfig kv set `
        --name $AppConfigName `
        --key $Key `
        --value $kvRefValue `
        --label $Label `
        --content-type "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8" `
        --yes `
        --only-show-errors | Out-Null
}

# Function to set a secret in Key Vault
function Set-KeyVaultSecret {
    param(
        [string]$SecretName,
        [string]$SecretValue
    )

    Write-Host "  Storing secret: $SecretName" -ForegroundColor Gray
    az keyvault secret set `
        --vault-name $KeyVaultName `
        --name $SecretName `
        --value $SecretValue `
        --only-show-errors | Out-Null
}

# ============================================================================
# CORS Settings (Environment-Specific)
# ============================================================================
Write-Host ""
Write-Host "Setting CORS configuration..." -ForegroundColor Yellow

if ($CorsOrigins) {
    # Store as JSON array
    $originsArray = $CorsOrigins -split ',' | ForEach-Object { $_.Trim() }
    $originsJson = $originsArray | ConvertTo-Json -Compress
    Set-AppConfigValue -Key "Cors:AllowedOrigins" -Value $originsJson
    Write-Host "  CORS origins set for $Environment" -ForegroundColor Green
} else {
    Write-Host "  Skipping CORS (no origins provided)" -ForegroundColor Yellow
}

# ============================================================================
# Logging Settings
# ============================================================================
Write-Host ""
Write-Host "Setting Logging configuration..." -ForegroundColor Yellow

# Default log levels - can be overridden per environment
$logLevel = switch ($Environment) {
    'prod' { 'Warning' }
    'staging' { 'Information' }
    default { 'Information' }
}

$aspNetLogLevel = switch ($Environment) {
    'prod' { 'Warning' }
    default { 'Warning' }
}

Set-AppConfigValue -Key "Logging:LogLevel:Default" -Value $logLevel
Set-AppConfigValue -Key "Logging:LogLevel:Microsoft.AspNetCore" -Value $aspNetLogLevel

# ============================================================================
# Cadastral API Settings (Shared across environments)
# ============================================================================
if (-not $SkipCadastral) {
    Write-Host ""
    Write-Host "Setting Cadastral API configuration..." -ForegroundColor Yellow

    # NSW
    Set-AppConfigValue -Key "Cadastral:NSW:Endpoint" -Value "https://maps.six.nsw.gov.au/arcgis/rest/services/public/NSW_Cadastre/MapServer/9"
    Set-AppConfigValue -Key "Cadastral:NSW:Enabled" -Value "true"

    # VIC
    Set-AppConfigValue -Key "Cadastral:VIC:Endpoint" -Value "https://services.land.vic.gov.au/catalogue/publicproxy/guest/dv_geoserver/wfs"
    Set-AppConfigValue -Key "Cadastral:VIC:Enabled" -Value "true"

    # QLD
    Set-AppConfigValue -Key "Cadastral:QLD:Endpoint" -Value "https://spatial-gis.information.qld.gov.au/arcgis/rest/services/PlanningCadastre/LandParcelPropertyFramework/MapServer/0"
    Set-AppConfigValue -Key "Cadastral:QLD:Enabled" -Value "true"

    # TAS
    Set-AppConfigValue -Key "Cadastral:TAS:Endpoint" -Value "https://services.thelist.tas.gov.au/arcgis/rest/services/Public/CadastreAndAdministrative/MapServer/38"
    Set-AppConfigValue -Key "Cadastral:TAS:Enabled" -Value "true"

    # ACT
    Set-AppConfigValue -Key "Cadastral:ACT:Endpoint" -Value "https://data.actmapi.act.gov.au/arcgis/rest/services/data_extract/Land_Parcels/MapServer/0"
    Set-AppConfigValue -Key "Cadastral:ACT:Enabled" -Value "true"

    # NT
    Set-AppConfigValue -Key "Cadastral:NT:Endpoint" -Value "https://www.ntlis.nt.gov.au"
    Set-AppConfigValue -Key "Cadastral:NT:Enabled" -Value "true"

    # SA (disabled - requires paid subscription)
    Set-AppConfigValue -Key "Cadastral:SA:Endpoint" -Value "https://location.sa.gov.au/arcgis/rest/services"
    Set-AppConfigValue -Key "Cadastral:SA:Enabled" -Value "false"

    # WA (disabled - requires Landgate SLIP subscription)
    Set-AppConfigValue -Key "Cadastral:WA:Endpoint" -Value "https://services.slip.wa.gov.au/arcgis/rest/services"
    Set-AppConfigValue -Key "Cadastral:WA:Enabled" -Value "false"

    Write-Host "  Cadastral settings configured" -ForegroundColor Green
}

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Configuration pushed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "App Config:  $AppConfigName" -ForegroundColor Cyan
Write-Host "Key Vault:   $KeyVaultName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Note: The following are managed by Bicep deployment:" -ForegroundColor Yellow
Write-Host "  - AzureAd:Instance, TenantId, ClientId, Domain" -ForegroundColor Gray
Write-Host "  - AzureMaps:ClientId, SubscriptionKey" -ForegroundColor Gray
Write-Host "  - ConnectionStrings:DefaultConnection" -ForegroundColor Gray
Write-Host "  - KeyVault:Url, CertificateName" -ForegroundColor Gray
Write-Host ""
