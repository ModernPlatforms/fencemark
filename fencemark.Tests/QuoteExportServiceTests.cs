using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.Tests;

public class QuoteExportServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<Quote> CreateTestQuoteAsync(ApplicationDbContext context)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Acme Fencing Co."
        };
        context.Organizations.Add(org);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Smith Residence Fence",
            CustomerName = "John Smith",
            CustomerEmail = "john@example.com",
            CustomerPhone = "(555) 123-4567",
            InstallationAddress = "123 Main St, Anytown, USA",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m
        };
        context.Jobs.Add(job);

        var quote = new Quote
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            OrganizationId = org.Id,
            QuoteNumber = "Q-20251213-0001",
            CurrentVersion = 1,
            Status = QuoteStatus.Sent,
            MaterialsCost = 1500.00m,
            LaborCost = 750.00m,
            Subtotal = 2250.00m,
            ContingencyAmount = 225.00m,
            ProfitAmount = 495.00m,
            TotalAmount = 2970.00m,
            TaxAmount = 148.50m,
            GrandTotal = 3118.50m,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Terms = "50% deposit required upon acceptance.\nBalance due upon completion.\nPayment accepted via check, cash, or credit card.",
            Notes = "Work will begin within 2 weeks of deposit receipt."
        };
        context.Quotes.Add(quote);

        var bomItems = new[]
        {
            new BillOfMaterialsItem
            {
                Id = Guid.NewGuid().ToString(),
                QuoteId = quote.Id,
                Category = "Posts",
                Description = "6x6 Pressure Treated Post",
                Sku = "POST-6X6-PT",
                Quantity = 13.0m,
                UnitOfMeasure = "Each",
                UnitPrice = 45.00m,
                TotalPrice = 585.00m,
                SortOrder = 1
            },
            new BillOfMaterialsItem
            {
                Id = Guid.NewGuid().ToString(),
                QuoteId = quote.Id,
                Category = "Rails",
                Description = "2x4 Cedar Rail",
                Sku = "RAIL-2X4-CDR",
                Quantity = 300.0m,
                UnitOfMeasure = "Linear Foot",
                UnitPrice = 2.50m,
                TotalPrice = 750.00m,
                SortOrder = 2
            },
            new BillOfMaterialsItem
            {
                Id = Guid.NewGuid().ToString(),
                QuoteId = quote.Id,
                Category = "Panels",
                Description = "Privacy Panel 6ft",
                Sku = "PANEL-6FT-PRV",
                Quantity = 13.0m,
                UnitOfMeasure = "Each",
                UnitPrice = 12.69m,
                TotalPrice = 165.00m,
                SortOrder = 3
            },
            new BillOfMaterialsItem
            {
                Id = Guid.NewGuid().ToString(),
                QuoteId = quote.Id,
                Category = "Labor",
                Description = "Installation Labor (100.00 linear feet)",
                Quantity = 1.0m,
                UnitOfMeasure = "Job",
                UnitPrice = 750.00m,
                TotalPrice = 750.00m,
                SortOrder = 4
            }
        };
        context.BillOfMaterialsItems.AddRange(bomItems);

        await context.SaveChangesAsync();

        return quote;
    }

    [Fact]
    public async Task ExportQuoteAsHtmlAsync_GeneratesValidHtml()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var html = await service.ExportQuoteAsHtmlAsync(quote.Id);

        // Assert
        Assert.NotNull(html);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("Acme Fencing Co.", html);
        Assert.Contains("Q-20251213-0001", html);
        Assert.Contains("John Smith", html);
        Assert.Contains("john@example.com", html);
        Assert.Contains("(555) 123-4567", html);
        Assert.Contains("123 Main St, Anytown, USA", html);
    }

    [Fact]
    public async Task ExportQuoteAsHtmlAsync_IncludesAllBOMItems()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var html = await service.ExportQuoteAsHtmlAsync(quote.Id);

        // Assert
        Assert.Contains("6x6 Pressure Treated Post", html);
        Assert.Contains("2x4 Cedar Rail", html);
        Assert.Contains("Privacy Panel 6ft", html);
        Assert.Contains("Installation Labor", html);
        Assert.Contains("POST-6X6-PT", html);
        Assert.Contains("RAIL-2X4-CDR", html);
        Assert.Contains("PANEL-6FT-PRV", html);
    }

    [Fact]
    public async Task ExportQuoteAsHtmlAsync_IncludesAllTotals()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var html = await service.ExportQuoteAsHtmlAsync(quote.Id);

        // Assert
        Assert.Contains("$1,500.00", html); // Materials
        Assert.Contains("$750.00", html);   // Labor
        Assert.Contains("$2,250.00", html); // Subtotal
        Assert.Contains("$225.00", html);   // Contingency
        Assert.Contains("$495.00", html);   // Profit
        Assert.Contains("$148.50", html);   // Tax
        Assert.Contains("$3,118.50", html); // Grand Total
    }

    [Fact]
    public async Task ExportQuoteAsHtmlAsync_IncludesTermsAndNotes()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var html = await service.ExportQuoteAsHtmlAsync(quote.Id);

        // Assert
        Assert.Contains("50% deposit required upon acceptance", html);
        Assert.Contains("Work will begin within 2 weeks", html);
    }

    [Fact]
    public async Task ExportBomAsCsvAsync_GeneratesValidCsv()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var csv = await service.ExportBomAsCsvAsync(quote.Id);

        // Assert
        Assert.NotNull(csv);
        Assert.Contains("Category,Description,SKU,Quantity,Unit of Measure,Unit Price,Total Price", csv);
        Assert.Contains("Posts,6x6 Pressure Treated Post,POST-6X6-PT,13.00,Each,45.00,585.00", csv);
        Assert.Contains("Rails,2x4 Cedar Rail,RAIL-2X4-CDR,300.00,Linear Foot,2.50,750.00", csv);
        Assert.Contains("Panels,Privacy Panel 6ft,PANEL-6FT-PRV,13.00,Each,12.69,165.00", csv);
        Assert.Contains("Labor,Installation Labor (100.00 linear feet),,1.00,Job,750.00,750.00", csv);
    }

    [Fact]
    public async Task ExportBomAsCsvAsync_IncludesTotals()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var csv = await service.ExportBomAsCsvAsync(quote.Id);

        // Assert
        Assert.Contains("Materials Cost,,,,,,1500.00", csv);
        Assert.Contains("Labor Cost,,,,,,750.00", csv);
        Assert.Contains("Subtotal,,,,,,2250.00", csv);
        Assert.Contains("Contingency,,,,,,225.00", csv);
        Assert.Contains("Profit,,,,,,495.00", csv);
        Assert.Contains("Tax,,,,,,148.50", csv);
        Assert.Contains("Grand Total,,,,,,3118.50", csv);
    }

    [Fact]
    public async Task ExportBomAsCsvAsync_HandlesSpecialCharacters()
    {
        // Arrange
        await using var context = CreateDbContext();
        var org = new Organization
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Org"
        };
        context.Organizations.Add(org);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m
        };
        context.Jobs.Add(job);

        var quote = new Quote
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            OrganizationId = org.Id,
            QuoteNumber = "Q-TEST-001",
            MaterialsCost = 100.0m,
            LaborCost = 50.0m,
            Subtotal = 150.0m,
            ContingencyAmount = 15.0m,
            ProfitAmount = 33.0m,
            TotalAmount = 198.0m,
            TaxAmount = 0,
            GrandTotal = 198.0m
        };
        context.Quotes.Add(quote);

        var bomItem = new BillOfMaterialsItem
        {
            Id = Guid.NewGuid().ToString(),
            QuoteId = quote.Id,
            Category = "Test",
            Description = "Item with, comma and \"quotes\"",
            Quantity = 1.0m,
            UnitOfMeasure = "Each",
            UnitPrice = 100.0m,
            TotalPrice = 100.0m,
            SortOrder = 1
        };
        context.BillOfMaterialsItems.Add(bomItem);
        await context.SaveChangesAsync();

        var service = new QuoteExportService(context);

        // Act
        var csv = await service.ExportBomAsCsvAsync(quote.Id);

        // Assert
        Assert.Contains("\"Item with, comma and \"\"quotes\"\"\"", csv);
    }

    [Fact]
    public async Task GetQuoteExportDataAsync_ReturnsCorrectData()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        var service = new QuoteExportService(context);

        // Act
        var data = await service.GetQuoteExportDataAsync(quote.Id);

        // Assert
        Assert.NotNull(data);
        Assert.Equal(quote.Id, data.QuoteId);
        Assert.Equal("Q-20251213-0001", data.QuoteNumber);
        Assert.Equal("Acme Fencing Co.", data.OrganizationName);
        Assert.Equal("John Smith", data.CustomerName);
        Assert.Equal("john@example.com", data.CustomerEmail);
        Assert.Equal("(555) 123-4567", data.CustomerPhone);
        Assert.Equal("123 Main St, Anytown, USA", data.InstallationAddress);
        Assert.Equal(1500.00m, data.MaterialsCost);
        Assert.Equal(750.00m, data.LaborCost);
        Assert.Equal(3118.50m, data.GrandTotal);
        Assert.Equal(4, data.BomItems.Count);
    }

    [Fact]
    public async Task GetQuoteExportDataAsync_ReturnsNull_WhenQuoteNotFound()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new QuoteExportService(context);

        // Act
        var data = await service.GetQuoteExportDataAsync("nonexistent-id");

        // Assert
        Assert.Null(data);
    }

    [Fact]
    public async Task ExportQuoteAsHtmlAsync_ThrowsException_WhenQuoteNotFound()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new QuoteExportService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ExportQuoteAsHtmlAsync("nonexistent-id"));
    }

    [Fact]
    public async Task ExportBomAsCsvAsync_ThrowsException_WhenQuoteNotFound()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new QuoteExportService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ExportBomAsCsvAsync("nonexistent-id"));
    }

    [Fact]
    public async Task ExportQuoteAsHtmlAsync_OmitsTaxRow_WhenTaxIsZero()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        quote.TaxAmount = 0;
        quote.GrandTotal = quote.TotalAmount;
        await context.SaveChangesAsync();

        var service = new QuoteExportService(context);

        // Act
        var html = await service.ExportQuoteAsHtmlAsync(quote.Id);

        // Assert
        // The HTML should not contain a tax row when tax is zero
        var lines = html.Split('\n');
        var taxLines = lines.Where(line => line.Contains("<span>Tax:</span>"));
        Assert.Empty(taxLines);
    }

    [Fact]
    public async Task ExportBomAsCsvAsync_OmitsTaxRow_WhenTaxIsZero()
    {
        // Arrange
        await using var context = CreateDbContext();
        var quote = await CreateTestQuoteAsync(context);
        quote.TaxAmount = 0;
        quote.GrandTotal = quote.TotalAmount;
        await context.SaveChangesAsync();

        var service = new QuoteExportService(context);

        // Act
        var csv = await service.ExportBomAsCsvAsync(quote.Id);

        // Assert
        // When tax is zero, there should be no tax line in CSV
        var lines = csv.Split('\n');
        var taxLines = lines.Where(line => line.StartsWith("Tax,"));
        Assert.Empty(taxLines);
    }
}
