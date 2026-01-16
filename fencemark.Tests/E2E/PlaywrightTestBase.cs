using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// Base class for Playwright E2E tests with common setup and teardown logic
/// Tests inherit from this and call SetupAsync() at the start and TeardownAsync() at the end
/// </summary>
public abstract class PlaywrightTestBase : IDisposable
{
    protected IPlaywright? Playwright { get; private set; }
    protected IBrowser? Browser { get; private set; }
    protected IBrowserContext? Context { get; private set; }
    protected IPage? Page { get; private set; }

    /// <summary>
    /// Base URL for the application under test
    /// Can be overridden via environment variable TEST_BASE_URL
    /// </summary>
    protected virtual string BaseUrl => UrlHelper.NormalizeUrl(
        Environment.GetEnvironmentVariable("TEST_BASE_URL"), 
        "http://localhost:5000");

    /// <summary>
    /// Whether to run tests in headless mode
    /// </summary>
    protected virtual bool Headless => true;

    protected async Task SetupAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = Headless,
            SlowMo = 100 // Slow down by 100ms for better observability
        });

        Context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            RecordVideoDir = "videos/", // Record videos for debugging
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        });

        Page = await Context.NewPageAsync();
    }

    protected async Task TeardownAsync()
    {
        if (Page != null)
            await Page.CloseAsync();
        
        if (Context != null)
            await Context.CloseAsync();
        
        if (Browser != null)
            await Browser.CloseAsync();
        
        Playwright?.Dispose();
    }

    public void Dispose()
    {
        try
        {
            TeardownAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Suppress errors during cleanup
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Navigate to a relative path within the application
    /// </summary>
    protected async Task NavigateToAsync(string path)
    {
        if (Page == null)
            throw new InvalidOperationException("Page is not initialized");

        var url = $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        await Page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
    }

    /// <summary>
    /// Take a screenshot for debugging purposes
    /// </summary>
    protected async Task<byte[]> TakeScreenshotAsync(string? name = null)
    {
        if (Page == null)
            throw new InvalidOperationException("Page is not initialized");

        var fileName = name ?? $"screenshot-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png";
        var path = Path.Combine("screenshots", fileName);
        Directory.CreateDirectory("screenshots");
        
        return await Page.ScreenshotAsync(new() { Path = path, FullPage = true });
    }

    /// <summary>
    /// Wait for a selector to appear on the page
    /// </summary>
    protected async Task WaitForSelectorAsync(string selector, int timeoutMs = 30000)
    {
        if (Page == null)
            throw new InvalidOperationException("Page is not initialized");

        await Page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
    }
}
