using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for authenticated mode behavior with Azure AD/CIAM.
/// Tests verify that the application works correctly when Azure AD is properly configured.
/// 
/// These tests use the test-user-email and test-user-password from KeyVault.
/// Set environment variables:
/// - TEST_USER_EMAIL=<from KeyVault>
/// - TEST_USER_PASSWORD=<from KeyVault>
/// - TEST_BASE_URL=<app URL>
/// </summary>
public class AuthenticatedModeE2ETests : PlaywrightTestBase
{
    private static void SkipIfEnvironmentNotConfigured()
    {
        var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");
        var baseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(baseUrl))
        {
            Assert.Skip("Skipping authenticated mode tests: TEST_USER_EMAIL, TEST_USER_PASSWORD, or TEST_BASE_URL not set");
        }
    }

    [Fact]
    public async Task CanLoginWithCIAMCredentials()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Navigate to a protected page which should redirect to login
            await Page!.GotoAsync($"{BaseUrl}/jobs", new() 
            { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000 
            });

            var currentUrl = Page.Url;
            Console.WriteLine($"Current URL after navigation: {currentUrl}");

            // If we're on the CIAM login page, perform login
            if (currentUrl.Contains("ciamlogin.com") || currentUrl.Contains("login"))
            {
                Console.WriteLine("Detected CIAM login page, attempting to log in...");
                
                var loginSuccess = await PerformCIAMLoginAsync();
                Assert.True(loginSuccess, "Login with CIAM credentials should succeed");

                await TakeScreenshotAsync("after-login.png");
            }
            else
            {
                Console.WriteLine("Already logged in or redirected to app");
            }

            // Assert - Should be on the jobs page or redirected back to app
            currentUrl = Page.Url;
            Assert.Contains(BaseUrl, currentUrl);
            Assert.DoesNotContain("ciamlogin.com", currentUrl);

            await TakeScreenshotAsync("authenticated-jobs-page.png");
            Console.WriteLine("✅ Successfully logged in with CIAM credentials");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact]
    public async Task CanAccessProtectedPages_WhenAuthenticated()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Login first
            await LoginIfNeededAsync();

            // Navigate to each protected page
            var protectedPages = new[] 
            { 
                ("/jobs", "Jobs"),
                ("/fences", "Fences"),
                ("/gates", "Gates"),
                ("/components", "Components"),
                ("/organization", "Organization")
            };

            foreach (var (path, name) in protectedPages)
            {
                await Page!.GotoAsync($"{BaseUrl}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
                
                var currentUrl = Page.Url;
                
                // Should NOT be redirected to login or home
                Assert.DoesNotContain("ciamlogin.com", currentUrl);
                Assert.DoesNotContain("authentication/login", currentUrl);
                
                // Should be on the intended page
                Assert.Contains(path, currentUrl);

                await TakeScreenshotAsync($"authenticated-{name.ToLower()}-page.png");
                Console.WriteLine($"✅ Successfully accessed {name} page");
            }
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact]
    public async Task HomePage_ShowsLoginButton_WhenAuthConfigured()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Navigate to home page (before login)
            await Page!.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should show login/sign in button
            var loginButton = await Page.QuerySelectorAsync("a[href*='authentication/login']");
            var signInButton = await Page.QuerySelectorAsync("button:has-text('Sign In'), a:has-text('Sign In')");

            var hasLoginUI = loginButton != null || signInButton != null;
            Assert.True(hasLoginUI, "Home page should show login UI when auth is configured");

            await TakeScreenshotAsync("home-page-authenticated-mode.png");
            Console.WriteLine("✅ Home page correctly shows login button in authenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact]
    public async Task CanLogoutSuccessfully()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Login first
            await LoginIfNeededAsync();

            // Find and click logout button
            var logoutLink = await Page!.QuerySelectorAsync("a[href*='authentication/logout']");
            Assert.NotNull(logoutLink);

            await logoutLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await TakeScreenshotAsync("after-logout.png");

            // Try to access a protected page
            await Page.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle });

            var currentUrl = Page.Url;
            
            // Should be redirected to login or home
            var isRedirected = currentUrl.Contains("ciamlogin.com") || 
                             currentUrl.Contains("authentication/login") ||
                             currentUrl == BaseUrl.TrimEnd('/') + "/";

            Assert.True(isRedirected, "Should be redirected after logout when accessing protected page");

            await TakeScreenshotAsync("logout-redirect.png");
            Console.WriteLine("✅ Successfully logged out");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact]
    public async Task MainLayout_ShowsUserName_WhenAuthenticated()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Login first
            await LoginIfNeededAsync();

            // Navigate to a page
            await Page!.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should show user name in the layout
            var userNameElement = await Page.QuerySelectorAsync(".modern-user-name, [data-testid='user-name']");
            
            if (userNameElement != null)
            {
                var userName = await userNameElement.TextContentAsync();
                Assert.NotNull(userName);
                Assert.NotEmpty(userName.Trim());
                Console.WriteLine($"User name displayed: {userName}");
            }

            // Should show logout button
            var logoutButton = await Page.QuerySelectorAsync("a[href*='logout'], button:has-text('Sign Out')");
            Assert.NotNull(logoutButton);

            await TakeScreenshotAsync("authenticated-layout.png");
            Console.WriteLine("✅ MainLayout correctly shows authenticated user info");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    private async Task<bool> PerformCIAMLoginAsync()
    {
        if (Page == null) return false;

        try
        {
            var email = Environment.GetEnvironmentVariable("TEST_USER_EMAIL");
            var password = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("TEST_USER_EMAIL or TEST_USER_PASSWORD not set");
                return false;
            }

            Console.WriteLine($"Attempting login with email: {email}");

            // Wait for email input
            await Page.WaitForSelectorAsync(
                "input[type='email'], input[name='loginfmt'], input[name='email'], #signInName", 
                new() { Timeout = 10000 });

            var emailInput = await Page.QuerySelectorAsync("input[type='email']") 
                ?? await Page.QuerySelectorAsync("input[name='loginfmt']")
                ?? await Page.QuerySelectorAsync("input[name='email']")
                ?? await Page.QuerySelectorAsync("#signInName");

            if (emailInput != null)
            {
                await emailInput.FillAsync(email);
                Console.WriteLine("Filled email input");
            }

            // Click Next if it exists
            var nextButton = await Page.QuerySelectorAsync("input[type='submit'], button[type='submit'], #next");
            if (nextButton != null)
            {
                await nextButton.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // Wait for password input
            await Page.WaitForSelectorAsync(
                "input[type='password'], input[name='passwd'], input[name='password'], #password", 
                new() { Timeout = 10000 });

            var passwordInput = await Page.QuerySelectorAsync("input[type='password']")
                ?? await Page.QuerySelectorAsync("input[name='passwd']")
                ?? await Page.QuerySelectorAsync("input[name='password']")
                ?? await Page.QuerySelectorAsync("#password");

            if (passwordInput != null)
            {
                await passwordInput.FillAsync(password);
                Console.WriteLine("Filled password input");
            }

            // Click sign in
            var signInButton = await Page.QuerySelectorAsync("input[type='submit'], button[type='submit'], #next");
            if (signInButton != null)
            {
                await signInButton.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                Console.WriteLine("Clicked sign in button");
            }

            // Wait for redirect back to app
            await Page.WaitForURLAsync(url => url.StartsWith(BaseUrl), new() { Timeout = 30000 });

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed: {ex.Message}");
            await TakeScreenshotAsync("login-failed.png");
            return false;
        }
    }

    private async Task LoginIfNeededAsync()
    {
        if (Page == null) return;

        // Navigate to a protected page
        await Page.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle });

        var currentUrl = Page.Url;

        // If on login page, perform login
        if (currentUrl.Contains("ciamlogin.com") || currentUrl.Contains("authentication/login"))
        {
            var loginSuccess = await PerformCIAMLoginAsync();
            if (!loginSuccess)
            {
                throw new Exception("Failed to login before running test");
            }
        }
    }

    protected override string BaseUrl => 
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://localhost:5001";
    
    protected override bool Headless => 
        Environment.GetEnvironmentVariable("TEST_HEADLESS")?.ToLower() != "false";
}
