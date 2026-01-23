# Run comprehensive screenshot tests against dev.fencemark.com.au
# Set these environment variables before running:
#   $env:TEST_USER_EMAIL = "your-email@example.com"
#   $env:TEST_USER_PASSWORD = "your-password"

$env:TEST_BASE_URL = "https://dev.fencemark.com.au"
$env:TEST_HEADLESS = "false"

# Validate required environment variables
if (-not $env:TEST_USER_EMAIL -or -not $env:TEST_USER_PASSWORD) {
    Write-Host "ERROR: TEST_USER_EMAIL and TEST_USER_PASSWORD environment variables must be set" -ForegroundColor Red
    Write-Host "Example:" -ForegroundColor Yellow
    Write-Host '  $env:TEST_USER_EMAIL = "your-email@example.com"' -ForegroundColor Gray
    Write-Host '  $env:TEST_USER_PASSWORD = "your-password"' -ForegroundColor Gray
    exit 1
}

Write-Host "Running comprehensive screenshot E2E tests..." -ForegroundColor Cyan
Write-Host "Base URL: $env:TEST_BASE_URL" -ForegroundColor Yellow

# Run the tests
& ".\fencemark.Tests\bin\Debug\net10.0\fencemark.Tests.exe" --filter-class "fencemark.Tests.E2E.ComprehensiveScreenshotE2ETests" --output Detailed 2>&1

# Check for screenshots
$screenshotDirs = Get-ChildItem -Path "screenshots" -Directory -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($screenshotDirs) {
    Write-Host "`nScreenshots saved to: $($screenshotDirs.FullName)" -ForegroundColor Green
    Get-ChildItem -Path $screenshotDirs.FullName -Filter "*.png" | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Gray
    }
}
