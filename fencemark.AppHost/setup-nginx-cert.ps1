# Setup nginx SSL certificate from ASP.NET Core dev cert
# This script exports the ASP.NET Core dev cert and converts it to nginx-compatible format

$certDir = Join-Path $PSScriptRoot "nginx-certs"
$pfxPath = Join-Path $certDir "aspnetapp.pfx"
$crtPath = Join-Path $certDir "aspnetapp.crt"
$keyPath = Join-Path $certDir "aspnetapp.key"
$password = "DevCertPassword123"

# Create cert directory if it doesn't exist
if (-not (Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir | Out-Null
    Write-Host "Created certificate directory: $certDir"
}

# Export the ASP.NET Core dev certificate
Write-Host "Exporting ASP.NET Core development certificate..."
dotnet dev-certs https -ep $pfxPath -p $password --trust

if (-not (Test-Path $pfxPath)) {
    Write-Error "Failed to export certificate"
    exit 1
}

# Convert PFX to CRT and KEY using OpenSSL (requires OpenSSL to be installed)
# Check if OpenSSL is available
$opensslPath = Get-Command openssl -ErrorAction SilentlyContinue

if (-not $opensslPath) {
    Write-Host ""
    Write-Host "OpenSSL is not installed or not in PATH." -ForegroundColor Yellow
    Write-Host "You can install OpenSSL via:" -ForegroundColor Yellow
    Write-Host "  - Chocolatey: choco install openssl" -ForegroundColor Cyan
    Write-Host "  - Or download from: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "After installing OpenSSL, run this script again." -ForegroundColor Yellow
    exit 1
}

Write-Host "Converting certificate to nginx format..."

# Extract certificate
openssl pkcs12 -in $pfxPath -clcerts -nokeys -out $crtPath -password pass:$password -passin pass:$password

# Extract private key
openssl pkcs12 -in $pfxPath -nocerts -nodes -out $keyPath -password pass:$password -passin pass:$password

if ((Test-Path $crtPath) -and (Test-Path $keyPath)) {
    Write-Host "Certificate setup complete!" -ForegroundColor Green
    Write-Host "  Certificate: $crtPath"
    Write-Host "  Private Key: $keyPath"
    Write-Host ""
    Write-Host "You can now run the AppHost with HTTPS support." -ForegroundColor Green
} else {
    Write-Error "Failed to convert certificate"
    exit 1
}
