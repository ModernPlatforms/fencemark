# E2E Tests for Fencemark Application

This directory contains Playwright-based E2E tests for verifying the Fencemark application functionality.

## Test Files

### 1. `AllEndpointsE2ETests.cs`
Comprehensive tests covering the full user journey from unauthenticated access through login, authenticated operations, and logout.

**Test Flow:**
1. ✅ Test_01: Access home page unauthenticated
2. ✅ Test_02: Verify protected routes redirect when unauthenticated
3. ✅ Test_03: Login with test user credentials
4. ✅ Test_04: Access components page (authenticated)
5. ✅ Test_05: Access jobs page (authenticated)
6. ✅ Test_06: Access fences page (authenticated)
7. ✅ Test_07: Access gates page (authenticated)
8. ✅ Test_08: Create and read component via API
9. ✅ Test_09: Create and read job via API
10. ✅ Test_10: List all components
11. ✅ Test_11: List all jobs
12. ✅ Test_12: List all fences
13. ✅ Test_13: List all gates
14. ✅ Test_14: Logout successfully

Each test generates a numbered screenshot showing the application state at that point in the flow.

### 2. `AuthenticatedModeE2ETests.cs`
Tests the application when Azure AD/CIAM authentication is properly configured.

**Tests include:**
- Login with CIAM credentials
- Access to protected pages (Jobs, Fences, Gates, Components, Organization)
- Logout functionality
- UI elements for authenticated users (user name, logout button)
- Login button visibility on home page

### 3. `JobFlowE2ETests.cs`
Tests the complete job creation and management workflow.

## Prerequisites

### 1. Install Playwright Browsers
```bash
# Install Playwright browsers (only needed once)
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### 2. Application Must Be Running
Start the application using the AppHost:
```bash
cd fencemark.AppHost
dotnet run
```

The application will be available at:
- Web: `https://localhost:7074` (or check console output)
- API: `https://localhost:58267` (or check console output)

### 3. Create Test User Account
Ensure a test user exists in your database with known credentials. You can create one through:
- Registration endpoint: `POST /api/auth/register`
- Or manually seed the database with a test user

## Running the Tests

### Environment Variables

**Required:**
```bash
# Windows (PowerShell)
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"

# Linux/Mac (Bash)
export TEST_USER_EMAIL="test@fencemark.local"
export TEST_USER_PASSWORD="TestPassword123!"
```

**Optional:**
```bash
# Base URL (defaults to https://localhost:7074)
$env:TEST_BASE_URL="https://localhost:7074"

# Headless mode (defaults to true)
$env:TEST_HEADLESS="false"  # Set to false to see browser

# Cleanup after tests (defaults to true)
$env:TEST_CLEANUP="true"
```

### Run All E2E Tests
```bash
# Run all E2E tests in the AllEndpointsE2ETests class
dotnet test fencemark.Tests --filter "FullyQualifiedName~AllEndpointsE2ETests"

# Run with verbose output
dottem test fencemark.Tests --filter "FullyQualifiedName~AllEndpointsE2ETests" -v detailed
```

### Run Specific Test
```bash
# Run a single test
dotnet test --filter "FullyQualifiedName~AllEndpointsE2ETests.Test_03_User_CanLogin"

# Run tests 1-7 (unauthenticated through page access)
dotnet test --filter "FullyQualifiedName~AllEndpointsE2ETests.Test_0"
```

### Run in Visible Browser Mode
```bash
# See the browser during test execution
$env:TEST_HEADLESS="false"
dotnet test --filter "FullyQualifiedName~AllEndpointsE2ETests"
```

## Test Results and Screenshots

### Screenshots
Each test automatically captures a screenshot showing the page state. Screenshots are saved to:
```
fencemark.Tests/bin/Debug/net10.0/E2E/Screenshots/YYYY-MM-DD_HH-mm-ss/
```

Screenshot naming convention:
- `01_home_page_unauthenticated.png`
- `02_protected_route_unauthenticated.png`
- `03_login_complete.png`
- `04_components_page_authenticated.png`
- ...
- `14_logout_complete.png`

### Videos (on failure)
Failed tests automatically record a video in:
```
fencemark.Tests/bin/Debug/net10.0/videos/
```

## Troubleshooting

### "TEST_USER_EMAIL environment variable must be set"
**Solution:** Set the required environment variables as shown above.

### "Login should succeed with valid credentials" assertion fails
**Possible causes:**
- Test user doesn't exist in database
- Wrong password
- API service not running
- Network timeout

**Solution:**
1. Verify AppHost is running
2. Check API logs for authentication errors
3. Verify test user exists in database
4. Try running with `TEST_HEADLESS="false"` to see browser

### "Page is not initialized" error
**Solution:**
- Test infrastructure issue
- Try running tests one at a time
- Check that Playwright browsers are installed

### Screenshots show "Loading..." indefinitely
**Possible causes:**
- Blazor WASM not loading
- API not accessible
- CORS issues
- Network timeout

**Solution:**
1. Increase wait times in test
2. Check browser console (run with `TEST_HEADLESS="false"`)
3. Verify API is responding: `curl https://localhost:58267/health`
4. Check CORS configuration in API

### Tests timeout on navigation
**Solution:**
1. Increase default timeout in `PlaywrightTestBase.cs`
2. Check that application URLs are correct
3. Verify no firewall blocking localhost

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run E2E Tests
  env:
    TEST_BASE_URL: ${{ steps.deploy.outputs.web-url }}
    TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
    TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
    TEST_HEADLESS: "true"
  run: |
    dotnet test fencemark.Tests \
      --filter "FullyQualifiedName~AllEndpointsE2ETests" \
      --logger "trx;LogFileName=e2e-results.trx"

- name: Upload Test Results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: e2e-test-results
    path: |
      **/TestResults/*.trx
      **/screenshots/**
      **/videos/**
```

### Azure Pipelines Example
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run E2E Tests'
  env:
    TEST_BASE_URL: $(WebAppUrl)
    TEST_USER_EMAIL: $(TestUserEmail)
    TEST_USER_PASSWORD: $(TestUserPassword)
    TEST_HEADLESS: 'true'
  inputs:
    command: test
    projects: 'fencemark.Tests'
    arguments: '--filter "FullyQualifiedName~AllEndpointsE2ETests" --logger trx'
```

## Test Coverage Summary

| Area | Coverage |
|------|----------|
| Unauthenticated Access | ✅ Home page, Protected route redirects |
| Authentication | ✅ Login, User info retrieval |
| Page Navigation (Auth) | ✅ Components, Jobs, Fences, Gates |
| API Operations | ✅ Create, Read, List for Components & Jobs |
| Data Isolation | ✅ Fences, Gates (list only) |
| Session Management | ✅ Logout, Session cleanup |

## Local Development

### Quick Start
```bash
# 1. Start the application
cd fencemark.AppHost
dotnet run

# 2. In another terminal, set credentials
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"
$env:TEST_HEADLESS="false"

# 3. Run tests
cd ..
dotnet test fencemark.Tests --filter "FullyQualifiedName~AllEndpointsE2ETests"
```

### Debugging Tests
1. Set `TEST_HEADLESS="false"` to see browser
2. Add breakpoints in test code
3. Check screenshots in output directory
4. Review API logs in AppHost console
5. Use `await Task.Delay(5000)` to pause and inspect state

## Best Practices

1. **Always run against local AppHost** - Don't run against production
2. **Use dedicated test user** - Don't use your personal account
3. **Enable cleanup** - Set `TEST_CLEANUP="true"` to remove test data
4. **Check screenshots** - They show exactly what happened
5. **Run tests sequentially** - Tests are numbered for a reason
6. **Update selectors** - If UI changes, update test selectors

## Additional Resources

- [Playwright Documentation](https://playwright.dev/dotnet/)
- [xUnit Documentation](https://xunit.net/)
- [Blazor WASM Testing Guide](https://learn.microsoft.com/aspnet/core/blazor/test)
