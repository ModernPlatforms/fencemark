# E2E Test Authentication Fix - CIAM Interactive Flow

## Problem
Tests were failing with "TypeError: Failed to fetch" when trying to login. The authentication helper was trying to call API endpoints directly, but the application uses **Azure AD B2C/CIAM** for authentication.

## Root Cause
The `PlaywrightAuthHelper` was trying to make direct API calls to `/api/auth/login`, but this application uses **OAuth/OIDC flow** with Azure AD B2C/CIAM:

1. User clicks "Login" button
2. Application redirects to `devfencemark.ciamlogin.com`
3. User enters credentials on CIAM page
4. CIAM redirects back with authentication token
5. Application uses token for authenticated API calls

## Solution

### Updated `PlaywrightAuthHelper.cs`
Completely rewrote to handle **interactive CIAM login flow**:

```csharp
public async Task<bool> LoginAsync(string email, string password)
{
    // 1. Navigate to home page
    await _page.GotoAsync(_baseUrl);
    
    // 2. Click Login button
    await loginButton.ClickAsync();
    
    // 3. Wait for redirect to devfencemark.ciamlogin.com
    await _page.WaitForURLAsync(url => url.Contains("ciamlogin.com"));
    
    // 4. Fill in email
    await emailInput.FillAsync(email);
    await nextButton.ClickAsync();
    
    // 5. Fill in password
    await passwordInput.FillAsync(password);
    await signInButton.ClickAsync();
    
    // 6. Wait for redirect back to application
    await _page.WaitForURLAsync(url => url.StartsWith(_baseUrl));
    
    // 7. Verify login success (logout button visible)
    return await IsLoggedInAsync();
}
```

### Key Changes

1. **No Direct API Calls** - All authentication goes through the UI
2. **Handles CIAM Redirects** - Waits for redirects to/from ciamlogin.com
3. **UI-Based Verification** - Checks for Login/Logout buttons instead of API calls
4. **Interactive Flow** - Mimics real user clicking through CIAM login pages

### Updated Test Assertions

**Test_03 (Login):**
- ? Clicks Login button on home page
- ? Fills credentials on CIAM page
- ? Waits for redirect back
- ? Verifies Logout button is visible

**Test_14 (Logout):**
- ? Clicks Logout button
- ? Waits for logout to complete
- ? Verifies Login button is visible

## How to Run

### Prerequisites
Ensure you have a test user created in Azure AD B2C/CIAM:
- Email: `test@fencemark.local` (or your test user email)
- Password: `TestPassword123!` (or your test user password)

### Run Tests
```powershell
cd fencemark.Tests\E2E
.\Run-E2ETests.ps1
```

The script uses:
- `BaseUrl = "https://localhost:7173"` (Web Frontend - for login button clicks)
- `ApiUrl = "https://localhost:58267"` (API Service - for data operations after login)

## Authentication Flow

```
???????????????????????????????????????????????????????????????
? 1. User visits https://localhost:7173                       ?
?    ?                                                         ?
? 2. Clicks "Login" button                                    ?
?    ?                                                         ?
? 3. Redirects to devfencemark.ciamlogin.com                  ?
?    ?                                                         ?
? 4. Enters email + password on CIAM page                     ?
?    ?                                                         ?
? 5. CIAM validates credentials                               ?
?    ?                                                         ?
? 6. Redirects back to https://localhost:7173 with token     ?
?    ?                                                         ?
? 7. Application stores token in cookie/localStorage          ?
?    ?                                                         ?
? 8. Subsequent API calls include token automatically         ?
?    ?                                                         ?
? 9. Tests can now call https://localhost:58267/api/*        ?
???????????????????????????????????????????????????????????????
```

## Test Behavior

### Before (Direct API Calls) ?
```csharp
// Tried to call API directly - doesn't work with CIAM!
POST https://localhost:58267/api/auth/login
{
  "email": "test@fencemark.local",
  "password": "TestPassword123!"
}
// Result: Failed to fetch (no CIAM flow)
```

### After (Interactive CIAM Flow) ?
```csharp
// 1. Click Login button in browser
await loginButton.ClickAsync();

// 2. Browser redirects to CIAM
// URL: https://devfencemark.ciamlogin.com/...

// 3. Fill credentials on CIAM page
await emailInput.FillAsync("test@fencemark.local");
await passwordInput.FillAsync("TestPassword123!");
await signInButton.ClickAsync();

// 4. Browser redirects back with token
// URL: https://localhost:7173/?code=...

// 5. Application handles token automatically
// 6. Tests can now make authenticated API calls
```

## Troubleshooting

### "Login button not found"
- Check that the application is running
- Verify the button selector matches your UI
- Run with `$env:TEST_HEADLESS="false"` to see the browser

### "Timeout waiting for CIAM redirect"
- Ensure CIAM is configured correctly
- Check that redirect URIs are set up in Azure
- Verify network connectivity to ciamlogin.com

### "Authentication didn't persist"
- CIAM tokens are stored in cookies/localStorage
- Ensure `credentials: 'include'` is set in fetch calls
- Check browser console for auth errors

### "Logout button not visible after login"
- Login may have failed silently
- Check for CIAM error messages on the page
- Verify test user credentials are correct
- Ensure test user exists in Azure AD B2C

## Configuration

### Azure AD B2C/CIAM Settings Required
1. **Application Registration** - Web app registered in Azure
2. **Redirect URIs** - `https://localhost:7173/authentication/login-callback`
3. **User Flow** - Sign-in user flow configured
4. **Test User** - User created in Azure AD B2C directory

### Environment Variables
```powershell
$env:TEST_BASE_URL="https://localhost:7173"    # Web app for login UI
$env:TEST_API_URL="https://localhost:58267"    # API for data operations
$env:TEST_USER_EMAIL="test@fencemark.local"    # CIAM test user
$env:TEST_USER_PASSWORD="TestPassword123!"     # CIAM test user password
```

## Expected Test Results

All 14 tests should now pass:
- ? Test_01: Home page loads (unauthenticated)
- ? Test_02: Protected routes redirect (unauthenticated)
- ? Test_03: **CIAM login succeeds** (interactive flow)
- ? Test_04-07: **Authenticated page access** (with CIAM token)
- ? Test_08-13: **API operations** (with CIAM token)
- ? Test_14: **Logout succeeds** (clears CIAM session)

## Screenshots Generated

With the CIAM flow, you'll see additional screenshots:
- `03a_before_login.png` - Home page with Login button
- `03b_login_complete.png` - After CIAM redirect, showing Logout button
- `14a_before_logout.png` - Authenticated state before logout
- `14b_logout_complete.png` - After logout, showing Login button

---

**Status:** ? Updated for CIAM Interactive Authentication
**Next Action:** Run `.\Run-E2ETests.ps1` with CIAM test user credentials
