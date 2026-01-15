# E2E Tests for Authentication Modes

This directory contains Playwright-based E2E tests for verifying both **authenticated** and **unauthenticated** modes of the Fencemark application.

## Test Files

### 1. `AuthenticatedModeE2ETests.cs`
Tests the application when Azure AD/CIAM authentication is properly configured.

**Tests include:**
- Login with CIAM credentials
- Access to protected pages (Jobs, Fences, Gates, Components, Organization)
- Logout functionality
- UI elements for authenticated users (user name, logout button)
- Login button visibility on home page

### 2. `UnauthenticatedModeE2ETests.cs`
Tests the application when Azure AD is NOT configured (`isValidAzureAdConfig = false`).

**Tests include:**
- No login button shown on home page or in layout
- Protected pages redirect to home instead of crashing
- No console errors when accessing protected pages
- Graceful degradation without authentication

### 3. `AuthenticationFlowE2ETests.cs` (existing)
Original authentication tests using API endpoints.

## Running the Tests

### Prerequisites
- Application must be running (locally or deployed)
- Playwright browsers installed: `pwsh bin/Debug/net10.0/playwright.ps1 install`

### Authenticated Mode Tests

These tests verify the app works correctly with Azure AD configured.

**Required Environment Variables:**
```bash
export TEST_BASE_URL="https://your-app-url.azurewebsites.net"
export TEST_USER_EMAIL="<from KeyVault: test-user-email>"
export TEST_USER_PASSWORD="<from KeyVault: test-user-password>"
export TEST_HEADLESS="false"  # Optional: set to false to see browser
```

**Get credentials from KeyVault:**
```bash
# Get test user email
az keyvault secret show --vault-name kv-ciambfwyw65gna5lu --name test-user-email --query value -o tsv

# Get test user password
az keyvault secret show --vault-name kv-ciambfwyw65gna5lu --name test-user-password --query value -o tsv
```

**Run tests:**
```bash
# Run all authenticated mode tests
dotnet test --filter "FullyQualifiedName~AuthenticatedModeE2ETests"

# Run specific test
dotnet test --filter "FullyQualifiedName~AuthenticatedModeE2ETests.CanLoginWithCIAMCredentials"
```

### Unauthenticated Mode Tests

These tests verify the app works correctly WITHOUT Azure AD configured.

**Setup:**
1. Configure the app with placeholder/invalid Azure AD settings:
   ```json
   {
     "AzureAd": {
       "Authority": "https://test.ciamlogin.com/",
       "ClientId": "00000000-0000-0000-0000-000000000000"
     }
   }
   ```

2. Start the application in unauthenticated mode

**Required Environment Variables:**
```bash
export TEST_BASE_URL="https://localhost:5001"
export TEST_UNAUTHENTICATED_MODE="true"
export TEST_HEADLESS="false"  # Optional: set to false to see browser
```

**Run tests:**
```bash
# Run all unauthenticated mode tests
dotnet test --filter "FullyQualifiedName~UnauthenticatedModeE2ETests"

# Run specific test
dotnet test --filter "FullyQualifiedName~UnauthenticatedModeE2ETests.AuthorizedPage_RedirectsToHome_WhenAuthNotConfigured"
```

## CI/CD Integration

These tests are currently skipped by default in CI. To enable them:

### For Authenticated Mode (Azure deployment)
Add to your GitHub Actions workflow:
```yaml
- name: Run Authenticated Mode E2E Tests
  env:
    TEST_BASE_URL: ${{ steps.deploy.outputs.web-url }}
    TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
    TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
    TEST_HEADLESS: "true"
  run: |
    dotnet test --filter "FullyQualifiedName~AuthenticatedModeE2ETests" \
      --logger "trx;LogFileName=e2e-auth-results.trx"
```

### For Unauthenticated Mode (Local dev)
```yaml
- name: Run Unauthenticated Mode E2E Tests
  env:
    TEST_BASE_URL: "https://localhost:5001"
    TEST_UNAUTHENTICATED_MODE: "true"
    TEST_HEADLESS: "true"
  run: |
    # Start app in unauthenticated mode
    dotnet run --project fencemark.Client &
    
    # Wait for app to start
    sleep 10
    
    # Run tests
    dotnet test --filter "FullyQualifiedName~UnauthenticatedModeE2ETests" \
      --logger "trx;LogFileName=e2e-unauth-results.trx"
```

## Viewing Test Results

Tests automatically capture screenshots in the `screenshots/` directory. Each test creates a timestamped screenshot showing:
- Success state: What the page looks like when test passes
- Failure state: What caused the test to fail

Screenshots are named descriptively:
- `home-page-authenticated-mode-20260115033012.png`
- `jobs-redirect-to-home-20260115033015.png`
- `after-login-20260115033020.png`

## Test Coverage

| Scenario | Test File | Coverage |
|----------|-----------|----------|
| Login with CIAM | AuthenticatedModeE2ETests | ✅ |
| Access protected pages (auth) | AuthenticatedModeE2ETests | ✅ |
| Logout | AuthenticatedModeE2ETests | ✅ |
| UI visibility (auth mode) | AuthenticatedModeE2ETests | ✅ |
| Redirect to home (no auth) | UnauthenticatedModeE2ETests | ✅ |
| No login buttons (no auth) | UnauthenticatedModeE2ETests | ✅ |
| No console errors (no auth) | UnauthenticatedModeE2ETests | ✅ |

## Troubleshooting

### "Login failed" errors
- Verify TEST_USER_EMAIL and TEST_USER_PASSWORD are correct
- Check that the test user exists in DEV External ID
- Ensure the app is configured with correct CIAM settings

### "Timeout" errors
- Increase timeout in test configuration
- Check network connectivity to the app
- Verify the app is running and reachable

### "Element not found" errors
- UI may have changed - update selectors in test code
- Check screenshots to see actual page state
- Run with `TEST_HEADLESS="false"` to observe browser

### Tests skip with "not configured"
- Ensure all required environment variables are set
- Check that TEST_BASE_URL is accessible
- For unauthenticated tests, verify app is in unauthenticated mode
