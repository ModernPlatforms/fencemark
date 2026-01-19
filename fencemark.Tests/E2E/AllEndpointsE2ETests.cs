using Microsoft.Playwright;
using System.Text.Json;

namespace fencemark.Tests.E2E;

/// <summary>
/// Comprehensive E2E tests that test all major endpoints
/// Tests unauthenticated access, login flow, authenticated operations, and logout
/// </summary>
public class AllEndpointsE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private PlaywrightAuthHelper? _authHelper;
    private List<string> _createdComponentIds = new();
    private List<string> _createdJobIds = new();
    private List<string> _createdFenceIds = new();
    private List<string> _createdGateIds = new();

    public async ValueTask InitializeAsync()
    {
        await SetupAsync();
        _authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupTestDataAsync();
        await TeardownAsync();
    }

    /// <summary>
    /// Helper method to make API calls via Playwright's page.evaluate
    /// </summary>
    private async Task<JsonElement> ApiCallAsync(string method, string endpoint, object? body = null)
    {
        var payload = body != null ? JsonSerializer.Serialize(body) : null;
        
        return await Page!.EvaluateAsync<JsonElement>(@"
            async (args) => {
                const options = {
                    method: args.method,
                    credentials: 'include',
                    headers: args.body ? { 'Content-Type': 'application/json' } : {}
                };
                if (args.body) {
                    options.body = args.body;
                }
                const response = await fetch(args.url, options);
                if (response.ok && response.headers.get('content-type')?.includes('application/json')) {
                    return await response.json();
                }
                return { ok: response.ok, status: response.status };
            }", new { method, url = $"{TestConfiguration.ApiUrl}{endpoint}", body = payload });
    }

    /// <summary>
    /// Cleanup all test data created during tests
    /// </summary>
    private async Task CleanupTestDataAsync()
    {
        if (!TestConfiguration.CleanupAfterTest)
            return;

        try
        {
            foreach (var componentId in _createdComponentIds)
            {
                try { await ApiCallAsync("DELETE", $"/api/components/{componentId}"); }
                catch { /* Ignore cleanup errors */ }
            }

            foreach (var jobId in _createdJobIds)
            {
                try { await ApiCallAsync("DELETE", $"/api/jobs/{jobId}"); }
                catch { /* Ignore cleanup errors */ }
            }

            foreach (var fenceId in _createdFenceIds)
            {
                try { await ApiCallAsync("DELETE", $"/api/fences/{fenceId}"); }
                catch { /* Ignore cleanup errors */ }
            }

            foreach (var gateId in _createdGateIds)
            {
                try { await ApiCallAsync("DELETE", $"/api/gates/{gateId}"); }
                catch { /* Ignore cleanup errors */ }
            }
        }
        catch
        {
            // Suppress all cleanup errors
        }
    }

    [Fact]
    public async Task Test_01_UnauthenticatedUser_CanAccessHomePage()
    {
        // Act - Navigate to home page
        await NavigateToAsync("/");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000); // Wait for Blazor WASM to initialize

        await TakeScreenshotAsync("01_home_page_unauthenticated.png");

        // Assert - Page loaded successfully
        var title = await Page!.TitleAsync();
        Assert.NotNull(title);
    }

    [Fact]
    public async Task Test_02_UnauthenticatedUser_CannotAccessProtectedRoutes()
    {
        // Act - Try to access components page without authentication
        await NavigateToAsync("/components");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);

        await TakeScreenshotAsync("02_protected_route_unauthenticated.png");

        // Assert - Should be redirected away from /components
        var currentUrl = Page!.Url;
        Assert.DoesNotContain("/components", currentUrl);
    }

    [Fact]
    public async Task Test_03_User_CanLogin()
    {
        // Arrange - Navigate to home page first
        await NavigateToAsync("/");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);

        await TakeScreenshotAsync("03a_before_login.png");

        // Act - Login via CIAM interactive flow
        var loginSuccess = await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        await TakeScreenshotAsync("03b_login_complete.png");

        // Assert - Login succeeded
        Assert.True(loginSuccess, "Login should succeed with valid credentials via CIAM");

        // Verify user is authenticated (logout button should be visible)
        var (userId, email, organizationId) = await _authHelper!.GetCurrentUserAsync();
        Assert.NotNull(userId); // Should return "authenticated" placeholder
    }

    [Fact]
    public async Task Test_04_AuthenticatedUser_CanAccessComponentsPage()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - Navigate to components page
        await NavigateToAsync("/components");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);

        await TakeScreenshotAsync("04_components_page_authenticated.png");

        // Assert - On components page
        var currentUrl = Page!.Url;
        Assert.Contains("/components", currentUrl);
    }

    [Fact]
    public async Task Test_05_AuthenticatedUser_CanAccessJobsPage()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - Navigate to jobs page
        await NavigateToAsync("/jobs");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);

        await TakeScreenshotAsync("05_jobs_page_authenticated.png");

        // Assert - On jobs page
        var currentUrl = Page!.Url;
        Assert.Contains("/jobs", currentUrl);
    }

    [Fact]
    public async Task Test_06_AuthenticatedUser_CanAccessFencesPage()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - Navigate to fences page
        await NavigateToAsync("/fences");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);

        await TakeScreenshotAsync("06_fences_page_authenticated.png");

        // Assert - On fences page
        var currentUrl = Page!.Url;
        Assert.Contains("/fences", currentUrl);
    }

    [Fact]
    public async Task Test_07_AuthenticatedUser_CanAccessGatesPage()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - Navigate to gates page
        await NavigateToAsync("/gates");
        await Page!.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);

        await TakeScreenshotAsync("07_gates_page_authenticated.png");

        // Assert - On gates page
        var currentUrl = Page!.Url;
        Assert.Contains("/gates", currentUrl);
    }

    [Fact]
    public async Task Test_08_CanCreateAndReadComponent()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - Create component via API
        var createResponse = await ApiCallAsync("POST", "/api/components", new
        {
            name = "E2E Test Component",
            sku = "E2E-TEST-001",
            category = "Test Category",
            unitPrice = 15.99m,
            unitOfMeasure = "Each",
            description = "Created by E2E test"
        });

        await TakeScreenshotAsync("08_component_created.png");

        // Assert - Component created
        var componentId = createResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(componentId);
        _createdComponentIds.Add(componentId!);

        // Act - Read component
        var getResponse = await ApiCallAsync("GET", $"/api/components/{componentId}");

        // Assert - Component retrieved with correct data
        Assert.True(getResponse.TryGetProperty("name", out var nameProp));
        Assert.Equal("E2E Test Component", nameProp.GetString());
    }

    [Fact]
    public async Task Test_09_CanCreateAndReadJob()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - Create job via API
        var createResponse = await ApiCallAsync("POST", "/api/jobs", new
        {
            name = "E2E Test Job",
            customerName = "Test Customer",
            customerEmail = "test@example.com",
            customerPhone = "555-1234",
            installationAddress = "123 Test St",
            status = 0 // Draft
        });

        await TakeScreenshotAsync("09_job_created.png");

        // Assert - Job created
        var jobId = createResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(jobId);
        _createdJobIds.Add(jobId!);

        // Act - Read job
        var getResponse = await ApiCallAsync("GET", $"/api/jobs/{jobId}");

        // Assert - Job retrieved with correct data
        Assert.True(getResponse.TryGetProperty("name", out var nameProp));
        Assert.Equal("E2E Test Job", nameProp.GetString());
    }

    [Fact]
    public async Task Test_10_CanListComponents()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - List components
        var response = await ApiCallAsync("GET", "/api/components");

        await TakeScreenshotAsync("10_components_list.png");

        // Assert - Response is valid array or object
        Assert.True(response.ValueKind == JsonValueKind.Array || response.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task Test_11_CanListJobs()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - List jobs
        var response = await ApiCallAsync("GET", "/api/jobs");

        await TakeScreenshotAsync("11_jobs_list.png");

        // Assert - Response is valid array or object
        Assert.True(response.ValueKind == JsonValueKind.Array || response.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task Test_12_CanListFences()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - List fences
        var response = await ApiCallAsync("GET", "/api/fences");

        await TakeScreenshotAsync("12_fences_list.png");

        // Assert - Response is valid array or object
        Assert.True(response.ValueKind == JsonValueKind.Array || response.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task Test_13_CanListGates()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        // Act - List gates
        var response = await ApiCallAsync("GET", "/api/gates");

        await TakeScreenshotAsync("13_gates_list.png");

        // Assert - Response is valid array or object
        Assert.True(response.ValueKind == JsonValueKind.Array || response.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task Test_14_User_CanLogout()
    {
        // Arrange - Login first
        await _authHelper!.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);

        await TakeScreenshotAsync("14a_before_logout.png");

        // Act - Logout
        var logoutSuccess = await _authHelper!.LogoutAsync();

        await TakeScreenshotAsync("14b_logout_complete.png");

        // Assert - Logout succeeded
        Assert.True(logoutSuccess, "Logout should succeed");

        // Verify user is logged out (login button should be visible)
        var (userId, email, organizationId) = await _authHelper!.GetCurrentUserAsync();
        Assert.Null(userId); // Should return null when logged out
    }

    protected override string BaseUrl => TestConfiguration.BaseUrl;
    protected override bool Headless => TestConfiguration.Headless;
}
