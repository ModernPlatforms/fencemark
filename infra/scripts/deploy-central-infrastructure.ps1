# ============================================================================
# Deploy Central Infrastructure
# ============================================================================
# This script deploys the central App Configuration and DNS zone that are
# shared across all environments.
#
# Usage:
#   .\deploy-central-infrastructure.ps1
# ============================================================================

param(
    [string]$Location = "australiaeast"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Deploying central infrastructure..." -ForegroundColor Cyan
Write-Host ""

# Deploy central infrastructure
Write-Host "📦 Deploying App Configuration and DNS Zone..." -ForegroundColor Yellow

$deployment = az deployment sub create `
    --location $Location `
    --template-file "$PSScriptRoot\..\central-appconfig.bicep" `
    --parameters "$PSScriptRoot\..\central-appconfig.bicepparam" `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Deployment failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Central infrastructure deployed successfully!" -ForegroundColor Green
Write-Host ""

# Extract outputs
$outputs = $deployment.properties.outputs

Write-Host "📋 Deployment Outputs:" -ForegroundColor Cyan
Write-Host "  Resource Group: $($outputs.centralConfigResourceGroupName.value)" -ForegroundColor White
Write-Host "  App Config Name: $($outputs.appConfigName.value)" -ForegroundColor White
Write-Host "  App Config Endpoint: $($outputs.appConfigEndpoint.value)" -ForegroundColor White
Write-Host "  DNS Zone: $($outputs.dnsZoneName.value)" -ForegroundColor White
Write-Host ""

# Display name servers
Write-Host "🌐 DNS Name Servers (configure these at your domain registrar):" -ForegroundColor Yellow
$nameServers = $outputs.nameServers.value
foreach ($ns in $nameServers) {
    Write-Host "  • $ns" -ForegroundColor White
}
Write-Host ""

Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Configure the name servers above at your domain registrar (fencemark.com.au)" -ForegroundColor White
Write-Host "  2. Wait for DNS propagation (up to 48 hours)" -ForegroundColor White
Write-Host "  3. Deploy environments: azd deploy --environment dev" -ForegroundColor White
Write-Host ""
