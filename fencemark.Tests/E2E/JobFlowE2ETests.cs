namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for the Job/Drawing workflow using Playwright
/// These tests require the application to be running
/// Skip by default for CI/CD - run manually with: dotnet test --filter JobFlowE2ETests
/// </summary>
public class JobFlowE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanNavigateToJobsPage()
    {
        // Arrange & Act
        await NavigateToAsync("/jobs");
        await WaitForSelectorAsync("h1", 10000);

        // Assert
        var heading = await Page!.TextContentAsync("h1");
        Assert.NotNull(heading);
        Assert.Contains("Jobs", heading, StringComparison.OrdinalIgnoreCase);

        // Take screenshot for manual verification
        await TakeScreenshotAsync("jobs-page.png");
    }

    [Fact(Skip = "E2E tests require running application and authentication - run manually")]
    public async Task CanCreateNewJob()
    {
        // Note: This test would require authentication to be set up
        // For now, it's a placeholder demonstrating the test structure

        // Arrange
        await NavigateToAsync("/jobs");

        // Wait for page to load
        await WaitForSelectorAsync("[data-testid='create-job-button']", 10000);

        // Act
        await Page!.ClickAsync("[data-testid='create-job-button']");
        
        // Fill in job details
        await Page.FillAsync("[data-testid='job-name-input']", "Test Fence Installation");
        await Page.FillAsync("[data-testid='customer-name-input']", "John Doe");
        await Page.FillAsync("[data-testid='customer-email-input']", "john@example.com");
        
        await Page.ClickAsync("[data-testid='save-job-button']");

        // Assert
        await WaitForSelectorAsync("[data-testid='job-created-message']", 10000);
        var message = await Page.TextContentAsync("[data-testid='job-created-message']");
        Assert.Contains("created successfully", message ?? "", StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("job-created.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanAddFenceLineItemToJob()
    {
        // Note: Requires authentication and existing job
        
        // Arrange
        await NavigateToAsync("/jobs/test-job-id");

        // Act
        await Page!.ClickAsync("[data-testid='add-line-item-button']");
        await Page.SelectOptionAsync("[data-testid='item-type-select']", "Fence");
        await Page.SelectOptionAsync("[data-testid='fence-type-select']", "6ft Privacy Fence");
        await Page.FillAsync("[data-testid='quantity-input']", "100");
        await Page.ClickAsync("[data-testid='add-item-button']");

        // Assert
        await WaitForSelectorAsync("[data-testid='line-item-row']", 10000);
        var lineItem = await Page.TextContentAsync("[data-testid='line-item-row']");
        Assert.Contains("6ft Privacy Fence", lineItem ?? "");

        await TakeScreenshotAsync("fence-line-item-added.png");
    }
}
