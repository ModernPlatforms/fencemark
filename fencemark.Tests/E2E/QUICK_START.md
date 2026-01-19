# Quick Start: Running AllEndpointsE2ETests

## Prerequisites Checklist
- [ ] Application is running via AppHost
- [ ] Test user account exists in database
- [ ] Playwright browsers installed
- [ ] Environment variables set

## Step-by-Step Guide

### 1. Start the Application
```powershell
# Terminal 1: Start AppHost
cd C:\repos\fencemark\fencemark.AppHost
dotnet run
```

Wait for output showing:
```
Now listening on: https://localhost:7173
```

### 2. Set Environment Variables
```powershell
# Terminal 2: Set test credentials
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"
$env:TEST_HEADLESS="false"  # Show browser during tests
$env:TEST_BASE_URL="https://localhost:7173"
```

### 3. Install Playwright (First Time Only)
```powershell
cd C:\repos\fencemark\fencemark.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### 4. Run the Tests
```powershell
# Run all E2E tests using the helper script
.\E2E\Run-E2ETests.ps1

# Or run manually with xunit.v3 syntax
dotnet test --report-trx --filter-class "*AllEndpointsE2ETests"

# Run specific test
dotnet test --report-trx --filter-method "*Test_03_User_CanLogin*"
```

## What You'll See

The tests will:
1. ? Open Chrome browser (if TEST_HEADLESS="false")
2. ? Navigate through 14 test scenarios sequentially
3. ? Capture screenshot at each step
4. ? Show PASS/FAIL for each test
5. ? Clean up test data automatically

## Expected Test Output

```
Starting test execution, please wait...
A total of 14 test files matched the specified pattern.

Passed! - Test_01_UnauthenticatedUser_CanAccessHomePage
Passed! - Test_02_UnauthenticatedUser_CannotAccessProtectedRoutes
Passed! - Test_03_User_CanLogin
Passed! - Test_04_AuthenticatedUser_CanAccessComponentsPage
Passed! - Test_05_AuthenticatedUser_CanAccessJobsPage
Passed! - Test_06_AuthenticatedUser_CanAccessFencesPage
Passed! - Test_07_AuthenticatedUser_CanAccessGatesPage
Passed! - Test_08_CanCreateAndReadComponent
Passed! - Test_09_CanCreateAndReadJob
Passed! - Test_10_CanListComponents
Passed! - Test_11_CanListJobs
Passed! - Test_12_CanListFences
Passed! - Test_13_CanListGates
Passed! - Test_14_User_CanLogout

Test Run Successful.
Total tests: 14
     Passed: 14
```

## Screenshots Location

After running tests, find screenshots at:
```
C:\repos\fencemark\fencemark.Tests\bin\Debug\net10.0\E2E\Screenshots\2025-01-15_14-30-00\
```

Each screenshot is numbered:
- `01_home_page_unauthenticated.png`
- `02_protected_route_unauthenticated.png`
- `03_login_complete.png`
- ... through to ...
- `14_logout_complete.png`

## Common Issues

### Issue: "TEST_USER_EMAIL environment variable must be set"
**Fix:**
```powershell
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"
```

### Issue: "Application is not running"
**Fix:** Start AppHost in another terminal:
```powershell
cd fencemark.AppHost
dotnet run
```

### Issue: "Login should succeed" fails
**Fix:** Verify test user exists by checking database or creating new user via registration API

### Issue: Tests timeout
**Fix:** 
1. Ensure AppHost is running
2. Check firewall isn't blocking localhost
3. Increase timeout in test code if needed

### Issue: "Unknown option --filter"
**Fix:** This project uses xunit.v3 with Microsoft Testing Platform. Use the correct syntax:
```powershell
# Correct - filter by class name
dotnet test --report-trx --filter-class "*AllEndpointsE2ETests"

# Correct - filter by method name
dotnet test --report-trx --filter-method "*Test_03*"

# Wrong - old syntax (won't work)
dotnet test --filter "AllEndpointsE2ETests"  # ? Don't use this
```

## Video Tutorial (Visual Learner?)

1. Start AppHost ? See "Now listening on..." message
2. Set environment variables ? 4 lines in PowerShell
3. Run `.\E2E\Run-E2ETests.ps1` ? Watch browser automate tests
4. Check screenshots folder ? See visual proof of each step

## Next Steps

- Review screenshots to understand test flow
- Modify tests to add new scenarios
- Run specific failing test to debug
- Check README.md for advanced configuration

## Support

If stuck:
1. Check screenshots to see what happened
2. Run with `$env:TEST_HEADLESS="false"` to watch browser
3. Check AppHost console for API errors
4. Review README.md for detailed troubleshooting
