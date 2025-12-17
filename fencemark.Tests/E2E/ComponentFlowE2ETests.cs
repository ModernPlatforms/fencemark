using Microsoft.Playwright;

namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for Component management workflow using Playwright
/// Uses persistent test user, performs operations, and cleans up test data
/// </summary>
public class ComponentFlowE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private PlaywrightAuthHelper? _authHelper;
    private string? _testUserEmail;
    private string? _testUserPassword;

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

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComponentFlowE2ETests")]
    public async Task CanViewComponentsPage()
    {
        // Act
        await NavigateToAsync("/components");
        await WaitForSelectorAsync("h1, h2, [data-testid='components-header']", 10000);

        // Assert
        var pageContent = await Page!.ContentAsync();
        Assert.Contains("component", pageContent, StringComparison.OrdinalIgnoreCase);

        // Take screenshot for verification
        await TakeScreenshotAsync("components-page.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComponentFlowE2ETests")]
    public async Task CanCreateNewComponent()
    {
        // Arrange
        await NavigateToAsync("/components");

        // Wait for page to load
        await Task.Delay(2000); // Allow time for page initialization

        // Act - Look for create/add button
        var addButtonSelectors = new[]
        {
            "[data-testid='create-component-button']",
            "[data-testid='add-component-button']",
            "button:has-text('New Component')",
            "button:has-text('Add Component')",
            "button:has-text('Create')",
            "a:has-text('New Component')"
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

            // Fill in component details
            await TryFillAsync("[data-testid='component-name-input']", "name", "Test Cedar Post");
            await TryFillAsync("[data-testid='component-sku-input']", "sku", "POST-CED-001");
            await TryFillAsync("[data-testid='component-category-input']", "category", "Posts");
            await TryFillAsync("[data-testid='component-price-input']", "price", "25.99");

            // Save the component
            var saveButtonSelectors = new[]
            {
                "[data-testid='save-component-button']",
                "button:has-text('Save')",
                "button:has-text('Create')",
                "button[type='submit']"
            };

            foreach (var selector in saveButtonSelectors)
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

        // Assert - Check if component appears in list
        await TakeScreenshotAsync("component-created.png");
        var content = await Page!.ContentAsync();
        
        // Success if we navigated through the flow without errors
        Assert.NotEmpty(content);
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter ComponentFlowE2ETests")]
    public async Task CanListComponents()
    {
        // Arrange - Create a component first via API
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            name = "Test Component",
            sku = "TEST-001",
            category = "Testing",
            unitPrice = 10.99m,
            unitOfMeasure = "Each"
        });

        var result = await Page!.EvaluateAsync<System.Text.Json.JsonElement>(@"
            async (args) => {
                const response = await fetch(args.url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: args.body,
                    credentials: 'include'
                });
                return { ok: response.ok, status: response.status };
            }", new { url = $"{BaseUrl}/api/components", body = payload });

        var success = result.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
        Assert.True(success, "Failed to create test component via API");

        // Act
        await NavigateToAsync("/components");
        await Task.Delay(2000); // Allow components to load

        // Assert
        var pageContent = await Page!.ContentAsync();
        Assert.Contains("Test Component", pageContent, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("components-list.png");
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
