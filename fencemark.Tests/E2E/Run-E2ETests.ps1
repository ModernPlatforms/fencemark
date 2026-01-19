# Run E2E Tests Helper Script
# This script sets up environment and runs AllEndpointsE2ETests

param(
    [string]$TestUserEmail = "test@fencemark.local",
    [string]$TestUserPassword = "TestPassword123!",
    [string]$BaseUrl = "https://localhost:7173",
    [string]$ApiUrl = "https://localhost:7385",
    [switch]$Headless = $false,
    [switch]$Cleanup = $true,
    [string]$TestFilter = "AllEndpointsE2ETests"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Fencemark E2E Test Runner" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
Write-Host "Setting environment variables..." -ForegroundColor Yellow
$env:TEST_USER_EMAIL = $TestUserEmail
$env:TEST_USER_PASSWORD = $TestUserPassword
$env:TEST_BASE_URL = $BaseUrl
$env:TEST_API_URL = $ApiUrl
$env:TEST_HEADLESS = if ($Headless) { "true" } else { "false" }
$env:TEST_CLEANUP = if ($Cleanup) { "true" } else { "false" }

Write-Host "  TEST_USER_EMAIL: $TestUserEmail" -ForegroundColor Gray
Write-Host "  TEST_BASE_URL: $BaseUrl" -ForegroundColor Gray
Write-Host "  TEST_API_URL: $ApiUrl" -ForegroundColor Gray
Write-Host "  TEST_HEADLESS: $($env:TEST_HEADLESS)" -ForegroundColor Gray
Write-Host "  TEST_CLEANUP: $($env:TEST_CLEANUP)" -ForegroundColor Gray
Write-Host ""

# Check if application is running
Write-Host "Checking if application is running at $BaseUrl..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri $BaseUrl -Method Head -TimeoutSec 5 -SkipCertificateCheck -ErrorAction Stop
    Write-Host "  ✓ Application is running" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Application is NOT running!" -ForegroundColor Red
    Write-Host "  Please start the application first:" -ForegroundColor Red
    Write-Host "    cd fencemark.AppHost" -ForegroundColor Gray
    Write-Host "    dotnet run" -ForegroundColor Gray
    Write-Host ""
    exit 1
}
Write-Host ""

# Check if Playwright is installed
Write-Host "Checking Playwright installation..." -ForegroundColor Yellow
$playwrightPath = "bin\Debug\net10.0\playwright.ps1"
if (Test-Path $playwrightPath) {
    Write-Host "  ✓ Playwright found" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Playwright may need to be installed" -ForegroundColor Yellow
    Write-Host "  Run this after first build:" -ForegroundColor Yellow
    Write-Host "    pwsh bin\Debug\net10.0\playwright.ps1 install" -ForegroundColor Gray
}
Write-Host ""

# Run tests
Write-Host "Running E2E tests..." -ForegroundColor Yellow
Write-Host "  Filter: $TestFilter" -ForegroundColor Gray
Write-Host ""

# For xunit.v3 with Microsoft Testing Platform
# Use --filter-class for class filtering or --filter-method for method filtering
if ($TestFilter -eq "AllEndpointsE2ETests") {
    # Run all tests in the AllEndpointsE2ETests class
    & dotnet test --report-xunit-trx --filter-class "*$TestFilter"
} else {
    # Run specific test by method name
    & dotnet test --report-xunit-trx --filter-method "*$TestFilter*"
}

$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan

if ($exitCode -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Screenshots saved to:" -ForegroundColor Gray
    Write-Host "  bin\Debug\net10.0\E2E\Screenshots\" -ForegroundColor Gray
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check screenshots for details:" -ForegroundColor Gray
    Write-Host "  bin\Debug\net10.0\E2E\Screenshots\" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Check videos (on failure):" -ForegroundColor Gray
    Write-Host "  bin\Debug\net10.0\videos\" -ForegroundColor Gray
}

Write-Host "================================================" -ForegroundColor Cyan

exit $exitCode
