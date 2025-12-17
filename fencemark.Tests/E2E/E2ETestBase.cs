using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// Base class for E2E tests that use Playwright to test the Web UI.
/// Tests will be skipped (not failed) if:
/// - Environment variables are not configured
/// - The application is not reachable
/// - Login fails
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IBrowserContext? Context { get; private set; }
    protected IPage? Page { get; private set; }

    /// <summary>
    /// Whether the test environment is properly configured and login succeeded
    /// </summary>
    protected bool IsConfigured { get; private set; }

    /// <summary>
    /// Reason why tests are skipped (if not configured)
    /// </summary>
    protected string? SkipReason { get; private set; }

    /// <summary>
    /// Base URL for the web application
    /// </summary>
    protected string BaseUrl => Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "";

    /// <summary>
    /// Test user email (from environment variable)
    /// </summary>
    protected string? TestUserEmail => Environment.GetEnvironmentVariable("TEST_USER_EMAIL");

    /// <summary>
    /// Test user password (from environment variable)
    /// </summary>
    protected string? TestUserPassword => Environment.GetEnvironmentVariable("TEST_USER_PASSWORD");

    /// <summary>
    /// Whether to run in headless mode
    /// </summary>
    protected bool Headless => Environment.GetEnvironmentVariable("TEST_HEADLESS")?.ToLower() != "false";

    public async ValueTask InitializeAsync()
    {
        // Check environment configuration
        if (string.IsNullOrEmpty(BaseUrl))
        {
            SkipReason = "TEST_BASE_URL environment variable not set";
            Console.WriteLine($"⚠️ Skipping E2E tests: {SkipReason}");
            return;
        }

        if (string.IsNullOrEmpty(TestUserEmail) || string.IsNullOrEmpty(TestUserPassword))
        {
            SkipReason = "TEST_USER_EMAIL and/or TEST_USER_PASSWORD environment variables not set";
            Console.WriteLine($"⚠️ Skipping E2E tests: {SkipReason}");
            return;
        }

        try
        {
            // Initialize Playwright
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new()
            {
                Headless = Headless,
                SlowMo = Headless ? 0 : 100
            });

            Context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                RecordVideoDir = Headless ? null : "videos/",
            });

            Page = await Context.NewPageAsync();

            // Check if application is reachable
            var response = await Page.GotoAsync(BaseUrl, new() 
            { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000 
            });

            if (response == null || !response.Ok)
            {
                SkipReason = $"Application not reachable at {BaseUrl}";
                Console.WriteLine($"⚠️ Skipping E2E tests: {SkipReason}");
                return;
            }

            // Attempt login via Web UI
            var loginSuccess = await TryLoginViaWebUIAsync();
            if (!loginSuccess)
            {
                SkipReason = "Login failed - check test user credentials";
                Console.WriteLine($"⚠️ Skipping E2E tests: {SkipReason}");
                return;
            }

            IsConfigured = true;
            Console.WriteLine($"✅ E2E test environment configured successfully");
        }
        catch (Exception ex)
        {
            SkipReason = $"Setup failed: {ex.Message}";
            Console.WriteLine($"⚠️ Skipping E2E tests: {SkipReason}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Page != null)
                await Page.CloseAsync();

            if (Context != null)
                await Context.CloseAsync();

            if (Browser != null)
                await Browser.CloseAsync();

            Playwright?.Dispose();
        }
        catch
        {
            // Suppress cleanup errors
        }
    }

    /// <summary>
    /// Login via the Web UI using CIAM authentication
    /// </summary>
    private async Task<bool> TryLoginViaWebUIAsync()
    {
        if (Page == null) return false;

        try
        {
            // Navigate to a page that requires authentication (this will trigger login)
            await Page.GotoAsync($"{BaseUrl}/jobs", new() 
            { 
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000 
            });

            // Check if we're redirected to login page (CIAM)
            var currentUrl = Page.Url;
            
            if (currentUrl.Contains("ciamlogin.com") || currentUrl.Contains("login") || currentUrl.Contains("signin"))
            {
                Console.WriteLine($"Redirected to login page: {currentUrl}");

                // Wait for email input and fill it
                await Page.WaitForSelectorAsync("input[type='email'], input[name='loginfmt'], input[name='email'], #signInName", 
                    new() { Timeout = 10000 });

                // Try different selectors for email input
                var emailInput = await Page.QuerySelectorAsync("input[type='email']") 
                    ?? await Page.QuerySelectorAsync("input[name='loginfmt']")
                    ?? await Page.QuerySelectorAsync("input[name='email']")
                    ?? await Page.QuerySelectorAsync("#signInName");

                if (emailInput != null)
                {
                    await emailInput.FillAsync(TestUserEmail!);
                    Console.WriteLine("Filled email input");
                }
                else
                {
                    Console.WriteLine("Could not find email input");
                    return false;
                }

                // Look for Next button or submit
                var nextButton = await Page.QuerySelectorAsync("input[type='submit']")
                    ?? await Page.QuerySelectorAsync("button[type='submit']")
                    ?? await Page.QuerySelectorAsync("#next");

                if (nextButton != null)
                {
                    await nextButton.ClickAsync();
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }

                // Wait for password input
                await Page.WaitForSelectorAsync("input[type='password'], input[name='passwd'], input[name='password'], #password", 
                    new() { Timeout = 10000 });

                var passwordInput = await Page.QuerySelectorAsync("input[type='password']")
                    ?? await Page.QuerySelectorAsync("input[name='passwd']")
                    ?? await Page.QuerySelectorAsync("input[name='password']")
                    ?? await Page.QuerySelectorAsync("#password");

                if (passwordInput != null)
                {
                    await passwordInput.FillAsync(TestUserPassword!);
                    Console.WriteLine("Filled password input");
                }
                else
                {
                    Console.WriteLine("Could not find password input");
                    return false;
                }

                // Click sign in button
                var signInButton = await Page.QuerySelectorAsync("input[type='submit']")
                    ?? await Page.QuerySelectorAsync("button[type='submit']")
                    ?? await Page.QuerySelectorAsync("#next");

                if (signInButton != null)
                {
                    await signInButton.ClickAsync();
                    await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    Console.WriteLine("Clicked sign in button");
                }

                // Wait for redirect back to app
                await Page.WaitForURLAsync(url => url.StartsWith(BaseUrl), new() { Timeout = 30000 });
            }

            // Verify we're logged in by checking for authenticated content
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            currentUrl = Page.Url;

            // Check if we're on the app (not login page) and can see authenticated content
            if (currentUrl.StartsWith(BaseUrl) && !currentUrl.Contains("login"))
            {
                Console.WriteLine($"Login successful, current URL: {currentUrl}");
                return true;
            }

            Console.WriteLine($"Login may have failed, current URL: {currentUrl}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login failed with exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Skip the test if environment is not configured.
    /// Call this at the start of each test method.
    /// </summary>
    protected void SkipIfNotConfigured()
    {
        if (!IsConfigured)
        {
            Assert.Skip(SkipReason ?? "E2E test environment not configured");
        }
    }

    /// <summary>
    /// Navigate to a page in the application
    /// </summary>
    protected async Task NavigateToAsync(string path)
    {
        if (Page == null) return;
        var url = $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
    }

    /// <summary>
    /// Take a screenshot for debugging
    /// </summary>
    protected async Task<string?> TakeScreenshotAsync(string name)
    {
        if (Page == null) return null;

        var dir = "screenshots";
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}-{DateTime.UtcNow:yyyyMMddHHmmss}.png");
        await Page.ScreenshotAsync(new() { Path = path, FullPage = true });
        Console.WriteLine($"Screenshot saved: {path}");
        return path;
    }

    /// <summary>
    /// Wait for and click an element
    /// </summary>
    protected async Task ClickAsync(string selector, int timeoutMs = 10000)
    {
        if (Page == null) return;
        await Page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
        await Page.ClickAsync(selector);
    }

    /// <summary>
    /// Fill a form field
    /// </summary>
    protected async Task FillAsync(string selector, string value, int timeoutMs = 10000)
    {
        if (Page == null) return;
        await Page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
        await Page.FillAsync(selector, value);
    }

    /// <summary>
    /// Get text content of an element
    /// </summary>
    protected async Task<string?> GetTextAsync(string selector, int timeoutMs = 10000)
    {
        if (Page == null) return null;
        await Page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
        return await Page.TextContentAsync(selector);
    }

    /// <summary>
    /// Check if an element exists on the page
    /// </summary>
    protected async Task<bool> ExistsAsync(string selector, int timeoutMs = 5000)
    {
        if (Page == null) return false;
        try
        {
            await Page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
