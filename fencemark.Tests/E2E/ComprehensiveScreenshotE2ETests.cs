using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// Comprehensive E2E tests that visit all client pages and capture screenshots.
/// Designed to be run against dev.fencemark.com.au to discover UI issues.
///
/// Run with:
/// $env:TEST_BASE_URL="https://dev.fencemark.com.au"
/// $env:TEST_USER_EMAIL="your@email.com"
/// $env:TEST_USER_PASSWORD="yourpassword"
/// $env:TEST_HEADLESS="false"
/// dotnet test --filter "FullyQualifiedName~ComprehensiveScreenshotE2ETests"
/// </summary>
public class ComprehensiveScreenshotE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private PlaywrightAuthHelper? _authHelper;
    private readonly string _screenshotDir = Path.Combine("screenshots", $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

    public async ValueTask InitializeAsync()
    {
        await SetupAsync();
        _authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        Directory.CreateDirectory(_screenshotDir);
    }

    public async ValueTask DisposeAsync()
    {
        await TeardownAsync();
    }

    /// <summary>
    /// Takes a named screenshot and saves to the run-specific directory
    /// </summary>
    private async Task<string> CaptureScreenshotAsync(string name)
    {
        if (Page == null)
            throw new InvalidOperationException("Page is not initialized");

        var fileName = $"{name}.png";
        var path = Path.Combine(_screenshotDir, fileName);

        await Page.ScreenshotAsync(new() { Path = path, FullPage = true });
        return path;
    }

    /// <summary>
    /// Waits for page to load and Blazor to initialize
    /// </summary>
    private async Task WaitForPageLoadAsync(int additionalDelayMs = 2000)
    {
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(additionalDelayMs); // Wait for Blazor WASM to initialize
    }

    #region Test Methods

    [Fact]
    public async Task Screenshot_AllPages_ComprehensiveCapture()
    {
        // ============================================
        // PHASE 1: Unauthenticated Pages
        // ============================================

        // 1. Home Page (unauthenticated)
        await NavigateToAsync("/");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("01_home_unauthenticated");

        // 2. Error page
        await NavigateToAsync("/Error");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("02_error_page");

        // ============================================
        // PHASE 2: Login
        // ============================================

        // Navigate back to home for login
        await NavigateToAsync("/");
        await WaitForPageLoadAsync();

        var loginSuccess = await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        Assert.True(loginSuccess, "Login should succeed with valid credentials");
        await CaptureScreenshotAsync("03_post_login");

        // ============================================
        // PHASE 3: Authenticated Pages
        // ============================================

        // 4. Home page (authenticated)
        await NavigateToAsync("/");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("04_home_authenticated");

        // 5. Jobs page
        await NavigateToAsync("/jobs");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("05_jobs_list");

        // Try to click "Add Job" or "New Job" button if exists
        try
        {
            var addJobButton = Page!.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("(?i)(add|new|create).*job") })
                .Or(Page!.Locator("button:has-text('Add Job'), button:has-text('New Job'), button:has-text('Create Job')"));

            if (await addJobButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await addJobButton.First.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("05a_jobs_add_dialog");

                // Close dialog if modal
                var closeButton = Page!.Locator("button.btn-close, button:has-text('Cancel'), button:has-text('Close'), .modal .close");
                if (await closeButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await closeButton.First.ClickAsync();
                    await Task.Delay(500);
                }
            }
        }
        catch { /* Button might not exist */ }

        // Try to click on a job row to access job details/drawing
        try
        {
            var jobRow = Page!.Locator(".job-card, .job-row, tr[data-job-id], .mud-table-row").First;
            if (await jobRow.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                // Look for "Edit" or similar button
                var editButton = Page!.Locator("button:has-text('Edit'), a:has-text('Edit'), .btn-edit").First;
                if (await editButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await editButton.ClickAsync();
                    await Task.Delay(1000);
                    await CaptureScreenshotAsync("05b_jobs_edit_dialog");

                    var closeButton = Page!.Locator("button.btn-close, button:has-text('Cancel'), button:has-text('Close')");
                    if (await closeButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                    {
                        await closeButton.First.ClickAsync();
                        await Task.Delay(500);
                    }
                }

                // Look for "Draw" or "View Map" button for JobDrawing page
                var drawButton = Page!.Locator("button:has-text('Draw'), button:has-text('Map'), a:has-text('Draw'), a[href*='drawing']").First;
                if (await drawButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await drawButton.ClickAsync();
                    await WaitForPageLoadAsync(3000); // Map takes longer to load
                    await CaptureScreenshotAsync("05c_job_drawing");

                    // Navigate back to jobs
                    await NavigateToAsync("/jobs");
                    await WaitForPageLoadAsync();
                }
            }
        }
        catch { /* Jobs might be empty */ }

        // 6. Fences page
        await NavigateToAsync("/fences");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("06_fences_list");

        // Try to click "Add Fence Type" button
        try
        {
            var addButton = Page!.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("(?i)(add|new|create)") })
                .Or(Page!.Locator("button:has-text('Add'), button.mud-button-filled"));

            if (await addButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await addButton.First.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("06a_fences_add_dialog");

                // Close dialog
                var closeButton = Page!.Locator("button:has-text('Cancel'), button.mud-dialog-close-button, button.btn-close");
                if (await closeButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await closeButton.First.ClickAsync();
                    await Task.Delay(500);
                }
            }
        }
        catch { /* Button might not exist */ }

        // 7. Gates page
        await NavigateToAsync("/gates");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("07_gates_list");

        // Try to click "Add Gate Type" button
        try
        {
            var addButton = Page!.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("(?i)(add|new|create)") })
                .Or(Page!.Locator("button:has-text('Add'), button.mud-button-filled"));

            if (await addButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await addButton.First.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("07a_gates_add_dialog");

                // Close dialog
                var closeButton = Page!.Locator("button:has-text('Cancel'), button.mud-dialog-close-button, button.btn-close");
                if (await closeButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await closeButton.First.ClickAsync();
                    await Task.Delay(500);
                }
            }
        }
        catch { /* Button might not exist */ }

        // 8. Components page
        await NavigateToAsync("/components");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("08_components_list");

        // Try to click "Add Component" button
        try
        {
            var addButton = Page!.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("(?i)(add|new|create)") })
                .Or(Page!.Locator("button:has-text('Add'), button.btn-primary"));

            if (await addButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await addButton.First.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("08a_components_add_dialog");

                // Close dialog
                var closeButton = Page!.Locator("button:has-text('Cancel'), button.btn-close, button.btn-secondary:has-text('Cancel')");
                if (await closeButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await closeButton.First.ClickAsync();
                    await Task.Delay(500);
                }
            }
        }
        catch { /* Button might not exist */ }

        // 9. Account page
        await NavigateToAsync("/account");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("09_account_page");

        // 10. Organization page (legacy Bootstrap)
        await NavigateToAsync("/organization");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("10_organization_page");

        // Try to click "Invite" button
        try
        {
            var inviteButton = Page!.Locator("button:has-text('Invite'), a:has-text('Invite')");
            if (await inviteButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await inviteButton.First.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("10a_organization_invite_dialog");

                var closeButton = Page!.Locator("button:has-text('Cancel'), button.btn-close");
                if (await closeButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await closeButton.First.ClickAsync();
                    await Task.Delay(500);
                }
            }
        }
        catch { /* Button might not exist */ }

        // 11. Organization Settings page (MudBlazor)
        await NavigateToAsync("/settings/organization");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("11_organization_settings_page");

        // Try to click expand/settings buttons
        try
        {
            var expandButtons = Page!.Locator(".mud-expand-panel-header, button[aria-expanded]");
            var count = await expandButtons.CountAsync();
            for (int i = 0; i < Math.Min(count, 3); i++)
            {
                await expandButtons.Nth(i).ClickAsync();
                await Task.Delay(500);
            }
            if (count > 0)
            {
                await CaptureScreenshotAsync("11a_organization_settings_expanded");
            }
        }
        catch { /* Expansion panels might not exist */ }

        // 12. Onboarding page (might redirect if already onboarded)
        await NavigateToAsync("/onboarding");
        await WaitForPageLoadAsync();
        await CaptureScreenshotAsync("12_onboarding_page");

        // ============================================
        // PHASE 4: Final summary
        // ============================================

        // Take one final screenshot of nav menu to show all options
        await NavigateToAsync("/");
        await WaitForPageLoadAsync();

        // Try to open the nav drawer/menu if it's collapsed
        try
        {
            var navToggle = Page!.Locator(".mud-drawer-toggle, .navbar-toggler, button[aria-label*='menu']");
            if (await navToggle.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
            {
                await navToggle.First.ClickAsync();
                await Task.Delay(500);
            }
        }
        catch { /* Nav might be already open */ }

        await CaptureScreenshotAsync("13_nav_menu_open");

        // Output the screenshot directory
        Console.WriteLine($"Screenshots saved to: {Path.GetFullPath(_screenshotDir)}");
    }

    [Fact]
    public async Task Screenshot_JobDrawingPage_IfJobExists()
    {
        // Login
        var loginSuccess = await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);
        Assert.True(loginSuccess, "Login should succeed");

        // Navigate to jobs
        await NavigateToAsync("/jobs");
        await WaitForPageLoadAsync();

        // Try to find and click on a job to get to drawing page
        try
        {
            // Look for any link that goes to job drawing
            var drawingLinks = Page!.Locator("a[href*='/drawing'], button:has-text('Draw'), button:has-text('Map')");
            if (await drawingLinks.CountAsync() > 0)
            {
                await drawingLinks.First.ClickAsync();
                await WaitForPageLoadAsync(5000); // Maps take time to load
                await CaptureScreenshotAsync("job_drawing_view");

                // Try to interact with map controls
                var controlButtons = Page!.Locator(".map-control, button[aria-label*='map'], .leaflet-control-zoom");
                if (await controlButtons.CountAsync() > 0)
                {
                    await CaptureScreenshotAsync("job_drawing_controls");
                }

                // Try to toggle sidebar
                var sidebarToggle = Page!.Locator("button:has-text('Segments'), button:has-text('Gates'), .sidebar-toggle");
                if (await sidebarToggle.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                {
                    await sidebarToggle.First.ClickAsync();
                    await Task.Delay(500);
                    await CaptureScreenshotAsync("job_drawing_sidebar");
                }
            }
            else
            {
                Console.WriteLine("No jobs with drawing links found - skipping JobDrawing screenshot");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not capture JobDrawing page: {ex.Message}");
        }
    }

    [Fact]
    public async Task Screenshot_EditDialogs_ForEachEntityType()
    {
        // Login
        var loginSuccess = await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);
        Assert.True(loginSuccess, "Login should succeed");

        // Capture Fences Edit Dialog
        await NavigateToAsync("/fences");
        await WaitForPageLoadAsync();
        try
        {
            var editButton = Page!.Locator("button:has-text('Edit'), .mud-icon-button[aria-label*='edit'], button[aria-label*='Edit']").First;
            if (await editButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await editButton.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("edit_dialog_fence");

                var cancelBtn = Page!.Locator("button:has-text('Cancel')").First;
                if (await cancelBtn.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                    await cancelBtn.ClickAsync();
            }
        }
        catch { }

        // Capture Gates Edit Dialog
        await NavigateToAsync("/gates");
        await WaitForPageLoadAsync();
        try
        {
            var editButton = Page!.Locator("button:has-text('Edit'), .mud-icon-button[aria-label*='edit'], button[aria-label*='Edit']").First;
            if (await editButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await editButton.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("edit_dialog_gate");

                var cancelBtn = Page!.Locator("button:has-text('Cancel')").First;
                if (await cancelBtn.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                    await cancelBtn.ClickAsync();
            }
        }
        catch { }

        // Capture Components Edit Dialog
        await NavigateToAsync("/components");
        await WaitForPageLoadAsync();
        try
        {
            var editButton = Page!.Locator("button:has-text('Edit'), .btn-warning, [data-bs-toggle='modal']").First;
            if (await editButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await editButton.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("edit_dialog_component");

                var cancelBtn = Page!.Locator("button:has-text('Cancel'), .btn-secondary").First;
                if (await cancelBtn.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                    await cancelBtn.ClickAsync();
            }
        }
        catch { }

        // Capture Jobs Edit Dialog
        await NavigateToAsync("/jobs");
        await WaitForPageLoadAsync();
        try
        {
            var editButton = Page!.Locator("button:has-text('Edit'), .btn-warning").First;
            if (await editButton.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 2000 }))
            {
                await editButton.ClickAsync();
                await Task.Delay(1000);
                await CaptureScreenshotAsync("edit_dialog_job");

                var cancelBtn = Page!.Locator("button:has-text('Cancel'), .btn-secondary").First;
                if (await cancelBtn.IsVisibleAsync(new LocatorIsVisibleOptions { Timeout = 1000 }))
                    await cancelBtn.ClickAsync();
            }
        }
        catch { }

        Console.WriteLine($"Edit dialog screenshots saved to: {Path.GetFullPath(_screenshotDir)}");
    }

    #endregion

    protected override string BaseUrl =>
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://dev.fencemark.com.au";

    protected override bool Headless =>
        Environment.GetEnvironmentVariable("TEST_HEADLESS")?.ToLower() != "false";
}
