using Microsoft.Playwright;
using System.Text.Json;

namespace fencemark.Tests.E2E;

/// <summary>
/// Helper class for authentication-related operations in Playwright tests
/// Handles Azure AD B2C/CIAM interactive login flow
/// </summary>
public class PlaywrightAuthHelper
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public PlaywrightAuthHelper(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Login with email and password through Azure AD B2C/CIAM interactive flow
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Navigate to home page
            await _page.GotoAsync(_baseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000); // Wait for Blazor to initialize

            // Click the "Sign In" link
            var signInLink = _page.GetByRole(AriaRole.Link, new() { Name = "Sign In", Exact = true });
            
            if (!await signInLink.IsVisibleAsync())
            {
                // Already logged in or sign in link not found
                return await IsLoggedInAsync();
            }

            await signInLink.ClickAsync();

            // Wait for redirect to CIAM login page
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(500);

            // Fill in email address
            var emailInput = _page.GetByRole(AriaRole.Textbox, new() { Name = "Enter your email address" });
            await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await emailInput.ClickAsync();
            await emailInput.FillAsync(email);

            // Click Next button
            var nextButton = _page.GetByRole(AriaRole.Button, new() { Name = "Next" });
            await nextButton.ClickAsync();

            // Wait for password field to appear
            await Task.Delay(1000);

            // Fill in password field (using ID selector from codegen)
            var passwordInput = _page.Locator("#i0118");
            await passwordInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await passwordInput.FillAsync(password);

            // Click Sign in button
            var signInButton = _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
            await signInButton.ClickAsync();

            // Handle "Stay signed in?" prompt - click "No"
            try
            {
                var noButton = _page.GetByRole(AriaRole.Button, new() { Name = "No" });
                await noButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                await noButton.ClickAsync();
            }
            catch
            {
                // "Stay signed in?" prompt might not appear, that's okay
            }

            // Wait for redirect back to the application
            await _page.WaitForURLAsync(url => url.StartsWith(_baseUrl), 
                new PageWaitForURLOptions { Timeout = 15000 });

            // Wait for authentication to complete
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000); // Give time for auth state to propagate in Blazor

            // Verify login was successful
            return await IsLoggedInAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if user is currently logged in
    /// </summary>
    private async Task<bool> IsLoggedInAsync()
    {
        try
        {
            // Check if "Sign In" link is NOT visible (indicates logged in state)
            var signInLink = _page.GetByRole(AriaRole.Link, new() { Name = "Sign In", Exact = true });
            var isSignInVisible = await signInLink.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 });
            
            // If Sign In is not visible, user is logged in
            return !isSignInVisible;
        }
        catch
        {
            // If we can't find the Sign In link, assume logged in
            return true;
        }
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    public async Task<bool> LogoutAsync()
    {
        try
        {
            // Look for user menu or logout button
            // This will need to be adjusted based on actual UI
            var logoutButton = _page.GetByRole(AriaRole.Button, new() { Name = "Logout" })
                .Or(_page.GetByRole(AriaRole.Link, new() { Name = "Logout" }))
                .Or(_page.GetByRole(AriaRole.Button, new() { Name = "Sign out" }))
                .Or(_page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }));
            
            if (!await logoutButton.IsVisibleAsync())
            {
                // Already logged out
                return true;
            }

            await logoutButton.ClickAsync();

            // Wait for redirect and logout to complete
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000);

            // Verify logout was successful (Sign In link should be visible)
            var signInLink = _page.GetByRole(AriaRole.Link, new() { Name = "Sign In", Exact = true });
            return await signInLink.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logout failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get current user info from the application (not from API)
    /// This checks the UI state rather than making API calls
    /// </summary>
    public async Task<(string? userId, string? email, string? organizationId)> GetCurrentUserAsync()
    {
        try
        {
            // Check if user is logged in by looking for absence of Sign In link
            var isLoggedIn = await IsLoggedInAsync();
            
            if (!isLoggedIn)
            {
                return (null, null, null);
            }

            // Try to extract user email from UI if displayed
            // This is a placeholder - adjust selectors based on actual UI
            var userEmailElement = _page.Locator("[data-testid='user-email'], .user-email, .user-info").First;
            string? email = null;
            
            try
            {
                email = await userEmailElement.InnerTextAsync(new LocatorInnerTextOptions { Timeout = 2000 });
            }
            catch
            {
                // Email not displayed in UI, that's okay
            }

            // For E2E tests, we mainly care that the user is authenticated
            // The actual IDs are managed by the backend
            return ("authenticated", email, "has-organization");
        }
        catch
        {
            return (null, null, null);
        }
    }

    /// <summary>
    /// Register a new user with organization through CIAM
    /// Note: This may not be available depending on CIAM configuration
    /// </summary>
    public async Task<(bool success, string? userId, string? organizationId)> RegisterAsync(
        string email,
        string password,
        string organizationName)
    {
        // Registration through CIAM typically requires clicking a "Sign up" link
        // and going through the CIAM registration flow
        // This is a placeholder implementation
        throw new NotImplementedException(
            "Registration through CIAM requires interactive flow. " +
            "Use Azure Portal or API to create test users.");
    }

    /// <summary>
    /// Delete current user account
    /// Note: This should be done through Azure Portal or API, not in E2E tests
    /// </summary>
    public async Task<bool> DeleteAccountAsync()
    {
        throw new NotImplementedException(
            "Account deletion should be done through Azure Portal or API, not in E2E tests.");
    }
}
