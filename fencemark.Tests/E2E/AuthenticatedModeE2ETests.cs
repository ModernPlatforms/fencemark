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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Login test failed: {ex.Message}");
            await TakeScreenshotAsync("login-test-error.png");
            throw;
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
                await Page!.GotoAsync($"{BaseUrl}{path}", new() { 
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000 
                });
                
                // Wait for page to be fully loaded
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await Task.Delay(500);
                
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Access protected pages test failed: {ex.Message}");
            await TakeScreenshotAsync("access-pages-test-error.png");
            throw;
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
            await Page!.GotoAsync(BaseUrl, new() { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000 
            });

            // Wait for page to load
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Task.Delay(1000);

            // Assert - Should show login/sign in button
            var loginButton = await Page.QuerySelectorAsync("a[href*='authentication/login']");
            var signInButton = await Page.QuerySelectorAsync("button:has-text('Sign In'), a:has-text('Sign In')");

            var hasLoginUI = loginButton != null || signInButton != null;
            Assert.True(hasLoginUI, "Home page should show login UI when auth is configured");

            await TakeScreenshotAsync("home-page-authenticated-mode.png");
            Console.WriteLine("✅ Home page correctly shows login button in authenticated mode");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Home page test failed: {ex.Message}");
            await TakeScreenshotAsync("homepage-test-error.png");
            throw;
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

            // Find logout button before clicking to avoid race condition
            var logoutLink = await Page!.QuerySelectorAsync("a[href*='authentication/logout']");
            Assert.NotNull(logoutLink);

            // Click logout and wait for navigation to complete
            // Use Promise.All to handle navigation that happens during click
            await Task.WhenAll(
                Page.WaitForURLAsync(url => !url.StartsWith(BaseUrl + "/jobs"), new() { Timeout = 30000 }),
                logoutLink.ClickAsync()
            );

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await TakeScreenshotAsync("after-logout.png");

            // Try to access a protected page
            await Page.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            var currentUrl = Page.Url;
            
            // Should be redirected to login or home
            var isRedirected = currentUrl.Contains("ciamlogin.com") || 
                             currentUrl.Contains("authentication/login") ||
                             currentUrl == BaseUrl.TrimEnd('/') + "/";

            Assert.True(isRedirected, "Should be redirected after logout when accessing protected page");

            await TakeScreenshotAsync("logout-redirect.png");
            Console.WriteLine("✅ Successfully logged out");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Logout test failed: {ex.Message}");
            await TakeScreenshotAsync("logout-test-error.png");
            throw;
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
            await Page!.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
            
            // Wait for the page to be fully loaded
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Task.Delay(1000); // Give the Blazor app time to render

            // Assert - Should show user name in the layout
            // Query selectors after navigation is complete
            var userNameElement = await Page.QuerySelectorAsync(".modern-user-name, [data-testid='user-name']");
            
            Assert.NotNull(userNameElement);
            var userName = await userNameElement.TextContentAsync();
            Assert.NotNull(userName);
            Assert.NotEmpty(userName.Trim());
            Console.WriteLine($"User name displayed: {userName}");
            
            // Should be a clickable link
            var href = await userNameElement.GetAttributeAsync("href");
            Assert.NotNull(href);
            Assert.Contains("/account", href);
            Console.WriteLine($"User name link href: {href}");

            // Should show logout button
            var logoutButton = await Page.QuerySelectorAsync("[data-testid='logout'], a[href*='logout'], button:has-text('Sign Out')");
            Assert.NotNull(logoutButton);

            await TakeScreenshotAsync("authenticated-layout.png");
            Console.WriteLine("✅ MainLayout correctly shows authenticated user info");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MainLayout test failed: {ex.Message}");
            await TakeScreenshotAsync("mainlayout-test-error.png");
            throw;
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact]
    public async Task UserName_IsClickable_NavigatesToProfile()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Login first
            await LoginIfNeededAsync();

            // Navigate to a page
            await Page!.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
            
            // Wait for the page to be fully loaded
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Task.Delay(1000); // Give the Blazor app time to render

            // Find and click the user name link
            var userNameElement = await Page.QuerySelectorAsync("[data-testid='user-name'], .modern-user-name");
            Assert.NotNull(userNameElement);

            // Click the user name
            await userNameElement.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(500);

            // Assert - Should navigate to /account page
            var currentUrl = Page.Url;
            Assert.Contains("/account", currentUrl);
            
            await TakeScreenshotAsync("account-page.png");
            Console.WriteLine("✅ User name link navigates to account page");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ User name link test failed: {ex.Message}");
            await TakeScreenshotAsync("user-name-link-test-error.png");
            throw;
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact]
    public async Task OrganizationLink_NavigatesToOrganizationPage()
    {
        SkipIfEnvironmentNotConfigured();
        
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Login first
            await LoginIfNeededAsync();

            // Navigate to a page
            await Page!.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
            
            // Wait for the page to be fully loaded
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Task.Delay(1000); // Give the Blazor app time to render

            // Find and click the organization link
            var orgLink = await Page.QuerySelectorAsync("a:has-text('Organization')");
            Assert.NotNull(orgLink);

            // Click the organization link
            await orgLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(500);

            // Assert - Should navigate to /organization page
            var currentUrl = Page.Url;
            Assert.Contains("/organization", currentUrl);
            
            await TakeScreenshotAsync("organization-page.png");
            Console.WriteLine("✅ Organization link navigates to organization page");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Organization link test failed: {ex.Message}");
            await TakeScreenshotAsync("organization-link-test-error.png");
            throw;
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
                new() { Timeout = 15000 });

            var emailInput = await Page.QuerySelectorAsync("input[type='email']") 
                ?? await Page.QuerySelectorAsync("input[name='loginfmt']")
                ?? await Page.QuerySelectorAsync("input[name='email']")
                ?? await Page.QuerySelectorAsync("#signInName");

            if (emailInput != null)
            {
                await emailInput.FillAsync(email);
                Console.WriteLine("Filled email input");
                await Task.Delay(500); // Brief pause after filling
            }

            // Click Next if it exists
            var nextButton = await Page.QuerySelectorAsync("input[type='submit'], button[type='submit'], #next");
            if (nextButton != null)
            {
                await nextButton.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(1000); // Wait for page transition
            }

            // Wait for password input
            await Page.WaitForSelectorAsync(
                "input[type='password'], input[name='passwd'], input[name='password'], #password", 
                new() { Timeout = 15000 });

            var passwordInput = await Page.QuerySelectorAsync("input[type='password']")
                ?? await Page.QuerySelectorAsync("input[name='passwd']")
                ?? await Page.QuerySelectorAsync("input[name='password']")
                ?? await Page.QuerySelectorAsync("#password");

            if (passwordInput != null)
            {
                await passwordInput.FillAsync(password);
                Console.WriteLine("Filled password input");
                await Task.Delay(500); // Brief pause after filling
            }

            // Click sign in
            var signInButton = await Page.QuerySelectorAsync("input[type='submit'], button[type='submit'], #next");
            if (signInButton != null)
            {
                Console.WriteLine("Clicking sign in button");
                await signInButton.ClickAsync();
                Console.WriteLine("Waiting for redirect back to app...");
            }

            // Wait for redirect back to app with increased timeout
            await Page.WaitForURLAsync(url => url.StartsWith(BaseUrl), new() { Timeout = 45000 });
            
            // Additional wait for the app to fully load after redirect
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000); // Extra time for Blazor to initialize
            
            Console.WriteLine("Successfully redirected back to app");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await TakeScreenshotAsync("login-failed.png");
            return false;
        }
    }

    private async Task LoginIfNeededAsync()
    {
        if (Page == null) return;

        // Navigate to a protected page
        await Page.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

        var currentUrl = Page.Url;
        Console.WriteLine($"After navigation to /jobs, current URL: {currentUrl}");

        // If on login page, perform login
        if (currentUrl.Contains("ciamlogin.com") || currentUrl.Contains("authentication/login"))
        {
            Console.WriteLine("Detected login page, performing CIAM login...");
            var loginSuccess = await PerformCIAMLoginAsync();
            if (!loginSuccess)
            {
                throw new Exception("Failed to login before running test");
            }
            Console.WriteLine("Login completed successfully");
        }
        else
        {
            Console.WriteLine("Already authenticated, no login needed");
        }
    }

    protected override string BaseUrl => 
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://localhost:5001";
    
    protected override bool Headless => 
        Environment.GetEnvironmentVariable("TEST_HEADLESS")?.ToLower() != "false";
}
