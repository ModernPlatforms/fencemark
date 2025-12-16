namespace fencemark.Tests.E2E;

/// <summary>
/// E2E tests for the Billing workflow using Playwright
/// These tests require the application to be running
/// Skip by default for CI/CD - run manually with: dotnet test --filter BillingFlowE2ETests
/// </summary>
public class BillingFlowE2ETests : PlaywrightTestBase
{
    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanViewPricingConfiguration()
    {
        // Arrange & Act
        await NavigateToAsync("/pricing");
        await WaitForSelectorAsync("[data-testid='pricing-config']", 10000);

        // Assert
        var heading = await Page!.TextContentAsync("h1");
        Assert.Contains("Pricing", heading ?? "", StringComparison.OrdinalIgnoreCase);

        var laborRate = await Page.IsVisibleAsync("[data-testid='labor-rate']");
        Assert.True(laborRate, "Labor rate should be visible");

        await TakeScreenshotAsync("pricing-config.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanExportQuoteToPDF()
    {
        // Arrange
        await NavigateToAsync("/quotes/test-quote-id");

        // Act
        await Page!.ClickAsync("[data-testid='export-pdf-button']");

        // Wait for download to start
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await Page.ClickAsync("[data-testid='confirm-export-button']");
        });

        // Assert
        Assert.NotNull(download);
        Assert.Contains(".pdf", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("quote-export.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanViewCostBreakdown()
    {
        // Arrange
        await NavigateToAsync("/quotes/test-quote-id");
        await WaitForSelectorAsync("[data-testid='cost-breakdown']", 10000);

        // Assert - Verify all cost components are displayed
        var materialsCost = await Page!.IsVisibleAsync("[data-testid='materials-cost']");
        Assert.True(materialsCost);

        var laborCost = await Page.IsVisibleAsync("[data-testid='labor-cost']");
        Assert.True(laborCost);

        var subtotal = await Page.IsVisibleAsync("[data-testid='subtotal']");
        Assert.True(subtotal);

        var contingency = await Page.IsVisibleAsync("[data-testid='contingency']");
        Assert.True(contingency);

        var profit = await Page.IsVisibleAsync("[data-testid='profit']");
        Assert.True(profit);

        var total = await Page.IsVisibleAsync("[data-testid='total-amount']");
        Assert.True(total);

        await TakeScreenshotAsync("cost-breakdown.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually")]
    public async Task CanUpdatePricingConfiguration()
    {
        // Arrange
        await NavigateToAsync("/pricing");
        await WaitForSelectorAsync("[data-testid='edit-pricing-button']", 10000);

        // Act
        await Page!.ClickAsync("[data-testid='edit-pricing-button']");
        await Page.FillAsync("[data-testid='labor-rate-input']", "75");
        await Page.FillAsync("[data-testid='contingency-input']", "12");
        await Page.ClickAsync("[data-testid='save-pricing-button']");

        // Assert
        await WaitForSelectorAsync("[data-testid='pricing-saved-message']", 10000);
        var message = await Page.TextContentAsync("[data-testid='pricing-saved-message']");
        Assert.Contains("saved", message ?? "", StringComparison.OrdinalIgnoreCase);

        await TakeScreenshotAsync("pricing-updated.png");
    }
}
