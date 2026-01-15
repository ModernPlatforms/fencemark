using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for unauthenticated mode behavior.
/// Tests verify that the application gracefully handles pages with [Authorize] attribute
/// when Azure AD is not configured (isValidAzureAdConfig = false).
/// 
/// These tests require the application to be running WITHOUT valid Azure AD configuration.
/// Set TEST_UNAUTHENTICATED_MODE=true to enable these tests.
/// </summary>
public class UnauthenticatedModeE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task HomePage_ShowsNoLoginButton_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Navigate to home page
            await Page!.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Verify no login button is shown
            var loginButton = await Page.QuerySelectorAsync("a[href*='authentication/login']");
            var signInButton = await Page.QuerySelectorAsync("button:has-text('Sign In')");

            Assert.Null(loginButton);
            Assert.Null(signInButton);

            await TakeScreenshotAsync("home-page-unauthenticated.png");
            Console.WriteLine("✅ Home page correctly hides login button in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task AuthorizedPage_RedirectsToHome_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Try to navigate to a page with [Authorize] attribute
            await Page!.GotoAsync($"{BaseUrl}/jobs", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should redirect to home page, not crash
            var currentUrl = Page.Url;
            Assert.Equal(BaseUrl.TrimEnd('/') + "/", currentUrl.TrimEnd('/'));

            await TakeScreenshotAsync("jobs-redirect-to-home.png");
            Console.WriteLine("✅ Jobs page correctly redirects to home in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task FencesPage_RedirectsToHome_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Try to navigate to Fences page (has [Authorize])
            await Page!.GotoAsync($"{BaseUrl}/fences", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should redirect to home page
            var currentUrl = Page.Url;
            Assert.Equal(BaseUrl.TrimEnd('/') + "/", currentUrl.TrimEnd('/'));

            await TakeScreenshotAsync("fences-redirect-to-home.png");
            Console.WriteLine("✅ Fences page correctly redirects to home in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task GatesPage_RedirectsToHome_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Try to navigate to Gates page (has [Authorize])
            await Page!.GotoAsync($"{BaseUrl}/gates", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should redirect to home page
            var currentUrl = Page.Url;
            Assert.Equal(BaseUrl.TrimEnd('/') + "/", currentUrl.TrimEnd('/'));

            await TakeScreenshotAsync("gates-redirect-to-home.png");
            Console.WriteLine("✅ Gates page correctly redirects to home in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task ComponentsPage_RedirectsToHome_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Try to navigate to Components page (has [Authorize])
            await Page!.GotoAsync($"{BaseUrl}/components", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should redirect to home page
            var currentUrl = Page.Url;
            Assert.Equal(BaseUrl.TrimEnd('/') + "/", currentUrl.TrimEnd('/'));

            await TakeScreenshotAsync("components-redirect-to-home.png");
            Console.WriteLine("✅ Components page correctly redirects to home in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task OrganizationPage_RedirectsToHome_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Try to navigate to Organization page (has [Authorize])
            await Page!.GotoAsync($"{BaseUrl}/organization", new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Should redirect to home page
            var currentUrl = Page.Url;
            Assert.Equal(BaseUrl.TrimEnd('/') + "/", currentUrl.TrimEnd('/'));

            await TakeScreenshotAsync("organization-redirect-to-home.png");
            Console.WriteLine("✅ Organization page correctly redirects to home in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task MainLayout_ShowsNoLoginButton_WhenAuthNotConfigured()
    {
        // Arrange
        await SetupAsync();

        try
        {
            // Act - Navigate to home page
            await Page!.GotoAsync(BaseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

            // Assert - Verify no "Sign In" button in the header/layout
            var signInButton = await Page.QuerySelectorAsync("button:has-text('Sign In')");
            var loginLink = await Page.QuerySelectorAsync("a:has-text('Sign In')");

            Assert.Null(signInButton);
            Assert.Null(loginLink);

            await TakeScreenshotAsync("layout-no-signin-button.png");
            Console.WriteLine("✅ MainLayout correctly hides Sign In button in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    [Fact(Skip = "Run manually with TEST_UNAUTHENTICATED_MODE=true when app is in unauthenticated mode")]
    public async Task NoConsoleErrors_WhenNavigatingProtectedPages()
    {
        // Arrange
        await SetupAsync();
        var consoleMessages = new List<string>();
        var consoleErrors = new List<string>();

        Page!.Console += (_, msg) =>
        {
            consoleMessages.Add($"{msg.Type}: {msg.Text}");
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        try
        {
            // Act - Navigate to multiple protected pages
            var protectedPages = new[] { "/jobs", "/fences", "/gates", "/components", "/organization" };

            foreach (var page in protectedPages)
            {
                await Page.GotoAsync($"{BaseUrl}{page}", new() { WaitUntil = WaitUntilState.NetworkIdle });
                await Task.Delay(1000); // Give time for any errors to surface
            }

            // Assert - No console errors related to authentication
            var authErrors = consoleErrors.Where(e =>
                e.Contains("RedirectToLogin", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("MSAL", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("authorize", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (authErrors.Any())
            {
                await TakeScreenshotAsync("console-errors.png");
                Console.WriteLine($"❌ Found authentication-related console errors:");
                foreach (var error in authErrors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Assert.Empty(authErrors);
            Console.WriteLine("✅ No authentication-related console errors in unauthenticated mode");
        }
        finally
        {
            await TeardownAsync();
        }
    }

    protected override string BaseUrl => 
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://localhost:5001";
    
    protected override bool Headless => 
        Environment.GetEnvironmentVariable("TEST_HEADLESS")?.ToLower() != "false";
}
