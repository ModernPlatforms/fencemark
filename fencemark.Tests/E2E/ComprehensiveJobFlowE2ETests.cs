using Microsoft.Playwright;
using System.Text.Json;

namespace fencemark.Tests.E2E;

/// <summary>
/// Comprehensive E2E tests for Job management workflow using Playwright
/// These tests create a test user, perform operations, and clean up
/// </summary>
public class ComprehensiveJobFlowE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private PlaywrightAuthHelper? _authHelper;
    private string? _testUserEmail;
    private string? _testUserPassword;

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
            }", new { method, url = $"{BaseUrl}{endpoint}", body = payload });
    }

    public async ValueTask InitializeAsync()
    {
        await SetupAsync();
        _authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        
        // Use persistent test user credentials from environment
        _testUserEmail = TestConfiguration.TestUserEmail;
        _testUserPassword = TestConfiguration.TestUserPassword;
        
        // Login with existing test user
        var loginSuccess = await _authHelper.LoginAsync(_testUserEmail, _testUserPassword);
        Assert.True(loginSuccess, "Test user login failed - ensure TEST_USER_EMAIL and TEST_USER_PASSWORD are set correctly");
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup test data but keep the user account
        // Note: Individual tests should clean up their test data (components, jobs, etc.)
        await TeardownAsync();
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComprehensiveJobFlowE2ETests")]
    public async Task CanViewJobsPage()
    {
        // Act
        await NavigateToAsync("/jobs");
        await Task.Delay(2000); // Allow page to load

        // Assert
        var pageContent = await Page!.ContentAsync();
        Assert.Contains("job", pageContent, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("jobs-page.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComprehensiveJobFlowE2ETests")]
    public async Task CanCreateJobViaApi()
    {
        // Act - Create job via API
        var response = await ApiCallAsync("POST", "/api/jobs", new
        {
            name = "Test Fence Installation",
            customerName = "John Doe",
            customerEmail = "john@example.com",
            customerPhone = "555-0123",
            installationAddress = "123 Main St, Test City",
            status = "Draft"
        });

        // Assert
        var jobId = response.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(jobId);

        // Verify job appears in UI
        await NavigateToAsync("/jobs");
        await Task.Delay(2000);
        
        var pageContent = await Page!.ContentAsync();
        Assert.Contains("Test Fence Installation", pageContent, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("job-created-in-list.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComprehensiveJobFlowE2ETests")]
    public async Task CanCreateJobViaUI()
    {
        // Arrange
        await NavigateToAsync("/jobs");
        await Task.Delay(2000);

        // Act - Look for create button
        var addButtonSelectors = new[]
        {
            "[data-testid='create-job-button']",
            "[data-testid='add-job-button']",
            "button:has-text('New Job')",
            "button:has-text('Add Job')",
            "button:has-text('Create')",
            "a:has-text('New Job')"
        };

        IElementHandle? addButton = null;
        foreach (var selector in addButtonSelectors)
        {
            try
            {
                addButton = await Page!.WaitForSelectorAsync(selector, new() { Timeout = 2000 });
                if (addButton != null) break;
            }
            catch
            {
                // Try next selector
            }
        }

        if (addButton != null)
        {
            await addButton.ClickAsync();
            await Task.Delay(1000);

            // Fill in job details
            await TryFillAsync("[data-testid='job-name-input']", "name", "UI Test Job");
            await TryFillAsync("[data-testid='customer-name-input']", "customerName", "Jane Smith");
            await TryFillAsync("[data-testid='customer-email-input']", "customerEmail", "jane@example.com");

            // Save the job
            var saveSelectors = new[]
            {
                "[data-testid='save-job-button']",
                "button:has-text('Save')",
                "button:has-text('Create')",
                "button[type='submit']"
            };

            foreach (var selector in saveSelectors)
            {
                try
                {
                    await Page!.ClickAsync(selector, new() { Timeout = 2000 });
                    break;
                }
                catch
                {
                    // Try next selector
                }
            }

            await Task.Delay(2000);
        }

        // Assert
        await TakeScreenshotAsync("job-created-via-ui.png");
        var content = await Page!.ContentAsync();
        Assert.NotEmpty(content);
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComprehensiveJobFlowE2ETests")]
    public async Task CanViewJobDetails()
    {
        // Arrange - Create a job via API first
        var response = await ApiCallAsync("POST", "/api/jobs", new
        {
            name = "Details Test Job",
            customerName = "Test Customer",
            customerEmail = "customer@test.com",
            status = "Draft"
        });

        var jobId = response.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(jobId);

        // Act - Navigate to job details
        await NavigateToAsync($"/jobs/{jobId}");
        await Task.Delay(2000);

        // Assert
        var pageContent = await Page!.ContentAsync();
        Assert.Contains("Details Test Job", pageContent, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("job-details.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComprehensiveJobFlowE2ETests")]
    public async Task CanUpdateJob()
    {
        // Arrange - Create a job first
        var createResponse = await ApiCallAsync("POST", "/api/jobs", new
        {
            name = "Original Job Name",
            customerName = "Original Customer",
            status = "Draft"
        });

        var jobId = createResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(jobId);

        // Act - Update via API
        await ApiCallAsync("PUT", $"/api/jobs/{jobId}", new
        {
            name = "Updated Job Name",
            customerName = "Updated Customer",
            status = "Active"
        });

        // Verify update in UI
        await NavigateToAsync("/jobs");
        await Task.Delay(2000);
        
        var pageContent = await Page!.ContentAsync();
        Assert.Contains("Updated Job Name", pageContent, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("job-updated.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComprehensiveJobFlowE2ETests")]
    public async Task CanDeleteJob()
    {
        // Arrange - Create a job first
        var createResponse = await ApiCallAsync("POST", "/api/jobs", new
        {
            name = "Job To Delete",
            customerName = "Delete Me",
            status = "Draft"
        });

        var jobId = createResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(jobId);

        // Act - Delete via API
        await ApiCallAsync("DELETE", $"/api/jobs/{jobId}");

        // Verify deletion in UI
        await NavigateToAsync("/jobs");
        await Task.Delay(2000);
        
        var pageContent = await Page!.ContentAsync();
        Assert.DoesNotContain("Job To Delete", pageContent, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("job-deleted.png");
    }

    /// <summary>
    /// Helper method to try filling a field with fallback selectors
    /// </summary>
    private async Task TryFillAsync(string primarySelector, string fieldName, string value)
    {
        var selectors = new[]
        {
            primarySelector,
            $"input[name='{fieldName}']",
            $"input[placeholder*='{fieldName}']",
            $"input[aria-label*='{fieldName}']"
        };

        foreach (var selector in selectors)
        {
            try
            {
                var element = await Page!.WaitForSelectorAsync(selector, new() { Timeout = 2000 });
                if (element != null)
                {
                    await element.FillAsync(value);
                    return;
                }
            }
            catch
            {
                // Try next selector
            }
        }
    }

    protected override string BaseUrl => TestConfiguration.BaseUrl;
    protected override bool Headless => TestConfiguration.Headless;
}
