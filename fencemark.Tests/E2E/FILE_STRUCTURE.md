# E2E Test Suite - File Structure

```
fencemark.Tests/E2E/
??? ?? AllEndpointsE2ETests.cs          ? Main test file (14 sequential tests)
??? ?? AuthenticatedModeE2ETests.cs     ? CIAM authentication tests
??? ?? JobFlowE2ETests.cs                ? Job workflow tests
?
??? ?? PlaywrightTestBase.cs            ? Base class for Playwright tests
??? ?? PlaywrightAuthHelper.cs          ? Authentication helper methods
??? ?? TestConfiguration.cs              ? Environment configuration
??? ?? UrlHelper.cs                      ? URL utilities
?
??? ?? README.md                         ? Complete documentation
??? ?? QUICK_START.md                    ? Fast getting-started guide
??? ?? SETUP_COMPLETE.md                 ? This setup summary
??? ?? FILE_STRUCTURE.md                 ? This file
?
??? ?? Run-E2ETests.ps1                  ? PowerShell test runner
??? ?? Run-E2ETests.bat                  ? Windows batch launcher
```

## Generated During Test Runs

```
fencemark.Tests/bin/Debug/net10.0/
??? E2E/Screenshots/
?   ??? 2025-01-15_14-30-00/
?       ??? 01_home_page_unauthenticated.png
?       ??? 02_protected_route_unauthenticated.png
?       ??? 03_login_complete.png
?       ??? 04_components_page_authenticated.png
?       ??? 05_jobs_page_authenticated.png
?       ??? 06_fences_page_authenticated.png
?       ??? 07_gates_page_authenticated.png
?       ??? 08_component_created.png
?       ??? 09_job_created.png
?       ??? 10_components_list.png
?       ??? 11_jobs_list.png
?       ??? 12_fences_list.png
?       ??? 13_gates_list.png
?       ??? 14_logout_complete.png
?
??? videos/
    ??? [video files on test failure]
```

## Key Files Explained

### Test Files

**AllEndpointsE2ETests.cs**
- Complete user journey from unauthenticated to authenticated to logout
- 14 sequential tests
- Tests page navigation, API operations, and session management
- Main test suite for comprehensive E2E validation

**AuthenticatedModeE2ETests.cs**
- Tests Azure AD/CIAM authentication integration
- Verifies authenticated user flows
- Tests protected page access

**JobFlowE2ETests.cs**
- Tests complete job creation and management workflow
- End-to-end job lifecycle testing

### Infrastructure Files

**PlaywrightTestBase.cs**
- Base class providing browser automation
- Setup/Teardown methods
- Screenshot capture functionality
- Navigation helpers

**PlaywrightAuthHelper.cs**
- Login/Logout methods
- User registration helpers
- Current user information retrieval
- Account management

**TestConfiguration.cs**
- Reads environment variables
- Provides test configuration defaults
- Validates required settings

### Documentation Files

**README.md**
- Complete test documentation
- Prerequisites and setup instructions
- Troubleshooting guide
- CI/CD integration examples

**QUICK_START.md**
- Fast getting-started guide
- Step-by-step execution instructions
- Common issue quick fixes
- Visual learner friendly

**SETUP_COMPLETE.md**
- Summary of what was created
- Quick reference for running tests
- Checklist for prerequisites

### Helper Scripts

**Run-E2ETests.ps1**
- Automated environment setup
- Application health check
- Test execution with proper logging
- Result summary

**Run-E2ETests.bat**
- Windows double-click launcher
- Calls PowerShell script
- No manual environment setup needed

## Usage Patterns

### For Developers
```powershell
# Quick test run
.\Run-E2ETests.ps1

# Run with custom settings
.\Run-E2ETests.ps1 -Headless:$false -TestFilter "Test_03"
```

### For CI/CD
```yaml
env:
  TEST_USER_EMAIL: ${{ secrets.TEST_USER_EMAIL }}
  TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
  TEST_HEADLESS: "true"
run: |
  dotnet test --filter "AllEndpointsE2ETests"
```

### For Manual Testing
```powershell
# Set environment
$env:TEST_USER_EMAIL="test@fencemark.local"
$env:TEST_USER_PASSWORD="TestPassword123!"

# Run specific test
dotnet test --filter "Test_03_User_CanLogin"
```

## File Dependencies

```
AllEndpointsE2ETests.cs
    ? inherits
PlaywrightTestBase.cs
    ? uses
Playwright NuGet Package

AllEndpointsE2ETests.cs
    ? uses
PlaywrightAuthHelper.cs
    ? uses
TestConfiguration.cs
```

## Screenshot Naming Convention

Format: `{TestNumber}_{description}.png`

- `01_` - Indicates test order
- `home_page_unauthenticated` - Describes what's shown
- `.png` - Screenshot format

This makes it easy to:
1. See the test sequence
2. Understand what each screenshot shows
3. Correlate screenshots with test code

## Best Practices

1. **Keep test files organized** - One test class per concern
2. **Use descriptive names** - Test names should explain intent
3. **Document new tests** - Update README when adding tests
4. **Follow naming convention** - Use consistent screenshot names
5. **Clean up test data** - Enable cleanup in TestConfiguration

---

**Quick Navigation:**
- [Main Tests](AllEndpointsE2ETests.cs)
- [Documentation](README.md)
- [Quick Start](QUICK_START.md)
- [Run Tests](Run-E2ETests.ps1)
