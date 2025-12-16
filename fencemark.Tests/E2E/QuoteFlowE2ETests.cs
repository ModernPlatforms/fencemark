namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for the Quote workflow using Playwright
/// These tests require the application to be running
/// Skip by default for CI/CD - run manually with: dotnet test --filter QuoteFlowE2ETests
/// </summary>
public class QuoteFlowE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanGenerateQuoteFromJob()
    {
        // Arrange
        await NavigateToAsync("/jobs/test-job-id");

        // Act
        await Page!.ClickAsync("[data-testid='generate-quote-button']");
        await WaitForSelectorAsync("[data-testid='quote-generated-message']", 10000);

        // Assert
        var message = await Page.TextContentAsync("[data-testid='quote-generated-message']");
        Assert.Contains("Quote generated", message ?? "", StringComparison.OrdinalIgnoreCase);

        // Verify quote number is displayed
        var quoteNumber = await Page.TextContentAsync("[data-testid='quote-number']");
        Assert.NotNull(quoteNumber);
        Assert.StartsWith("Q-", quoteNumber);

        await TakeScreenshotAsync("quote-generated.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanViewQuoteDetails()
    {
        // Arrange
        await NavigateToAsync("/quotes/test-quote-id");

        // Assert - Verify key sections are present
        await WaitForSelectorAsync("[data-testid='quote-details']", 10000);
        
        var quoteNumber = await Page!.TextContentAsync("[data-testid='quote-number']");
        Assert.NotNull(quoteNumber);
        
        var totalAmount = await Page.TextContentAsync("[data-testid='quote-total']");
        Assert.NotNull(totalAmount);
        
        var materialsSection = await Page.IsVisibleAsync("[data-testid='materials-cost']");
        Assert.True(materialsSection);
        
        var laborSection = await Page.IsVisibleAsync("[data-testid='labor-cost']");
        Assert.True(laborSection);

        await TakeScreenshotAsync("quote-details.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanUpdateQuoteAndCreateNewVersion()
    {
        // Arrange
        await NavigateToAsync("/quotes/test-quote-id");

        // Act
        await Page!.ClickAsync("[data-testid='edit-quote-button']");
        await Page.FillAsync("[data-testid='change-summary-input']", "Customer requested additional footage");
        await Page.ClickAsync("[data-testid='recalculate-quote-button']");

        // Assert
        await WaitForSelectorAsync("[data-testid='quote-version']", 10000);
        var version = await Page.TextContentAsync("[data-testid='quote-version']");
        Assert.Contains("Version 2", version ?? "");

        await TakeScreenshotAsync("quote-version-2.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanChangeQuoteStatus()
    {
        // Arrange
        await NavigateToAsync("/quotes/test-quote-id");

        // Act
        await Page!.SelectOptionAsync("[data-testid='quote-status-select']", "Sent");
        await Page.ClickAsync("[data-testid='update-status-button']");

        // Assert
        await WaitForSelectorAsync("[data-testid='status-updated-message']", 10000);
        var status = await Page.TextContentAsync("[data-testid='quote-status']");
        Assert.Equal("Sent", status);

        await TakeScreenshotAsync("quote-status-sent.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanViewBillOfMaterials()
    {
        // Arrange
        await NavigateToAsync("/quotes/test-quote-id");

        // Act
        await Page!.ClickAsync("[data-testid='view-bom-button']");
        await WaitForSelectorAsync("[data-testid='bom-table']", 10000);

        // Assert
        var bomRows = await Page.Locator("[data-testid='bom-row']").CountAsync();
        Assert.True(bomRows > 0, "BOM should have at least one item");

        await TakeScreenshotAsync("bill-of-materials.png");
    }
}
