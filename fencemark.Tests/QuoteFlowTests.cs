using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.Tests;

/// <summary>
/// Unit tests for Quote workflow scenarios
/// Tests quote generation, versioning, status changes, and export preparation
/// </summary>
public class QuoteFlowTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Organization org, PricingConfig config, Job job, FenceType fenceType)> SetupTestDataAsync(ApplicationDbContext context)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Fence Company"
        };
        context.Organizations.Add(org);

        var component = new Component
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Post",
            OrganizationId = org.Id,
            Category = "Posts",
            UnitOfMeasure = "Each",
            UnitPrice = 50.00m
        };
        context.Components.Add(component);

        var pricingConfig = new PricingConfig
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = org.Id,
            Name = "Standard Pricing",
            LaborRatePerHour = 60.00m,
            HoursPerLinearMeter = 0.5m,
            ContingencyPercentage = 0.10m,
            ProfitMarginPercentage = 0.20m,
            IsDefault = true
        };
        context.PricingConfigs.Add(pricingConfig);

        var heightTier = new HeightTier
        {
            Id = Guid.NewGuid().ToString(),
            PricingConfigId = pricingConfig.Id,
            MinHeightInMeters = 0,
            MaxHeightInMeters = 2.0m,
            Multiplier = 1.0m,
            Description = "Standard height"
        };
        context.HeightTiers.Add(heightTier);

        var fenceType = new FenceType
        {
            Id = Guid.NewGuid().ToString(),
            Name = "6ft Wood Fence",
            OrganizationId = org.Id,
            HeightInFeet = 6.0m,
            Material = "Wood",
            Style = "Privacy",
            PricePerLinearFoot = 20.00m
        };
        context.FenceTypes.Add(fenceType);

        var fenceComponent = new FenceComponent
        {
            Id = Guid.NewGuid().ToString(),
            FenceTypeId = fenceType.Id,
            ComponentId = component.Id,
            QuantityPerLinearFoot = 0.125m
        };
        context.FenceComponents.Add(fenceComponent);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);

        var lineItem = new JobLineItem
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            ItemType = LineItemType.Fence,
            FenceTypeId = fenceType.Id,
            Description = "100ft Wood Fence",
            Quantity = 100.0m,
            UnitPrice = 20.00m,
            TotalPrice = 2000.00m
        };
        context.JobLineItems.Add(lineItem);

        await context.SaveChangesAsync();

        return (org, pricingConfig, job, fenceType);
    }

    [Fact]
    public async Task GenerateQuote_CreatesQuoteWithCorrectStatus()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var quote = await service.GenerateQuoteAsync(job.Id, null, cancellationToken);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal(QuoteStatus.Draft, quote.Status);
        Assert.Equal(1, quote.CurrentVersion);
        Assert.Equal(job.Id, quote.JobId);
        Assert.Equal(org.Id, quote.OrganizationId);
    }

    [Fact]
    public async Task Quote_ChangesStatus_FromDraftToSent()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);

        // Act
        quote.Status = QuoteStatus.Sent;
        quote.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updatedQuote = await context.Quotes.FindAsync(quote.Id);
        Assert.NotNull(updatedQuote);
        Assert.Equal(QuoteStatus.Sent, updatedQuote.Status);
    }

    [Fact]
    public async Task Quote_ChangesStatus_FromSentToAccepted()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);
        quote.Status = QuoteStatus.Sent;
        quote.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Act
        quote.Status = QuoteStatus.Accepted;
        quote.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var acceptedQuote = await context.Quotes.FindAsync(quote.Id);
        Assert.NotNull(acceptedQuote);
        Assert.Equal(QuoteStatus.Accepted, acceptedQuote.Status);
    }

    [Fact]
    public async Task Quote_ChangesStatus_ToRejected()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);
        quote.Status = QuoteStatus.Sent;
        await context.SaveChangesAsync();

        // Act
        quote.Status = QuoteStatus.Rejected;
        quote.Notes = "Customer chose different vendor";
        await context.SaveChangesAsync();

        // Assert
        var rejectedQuote = await context.Quotes.FindAsync(quote.Id);
        Assert.NotNull(rejectedQuote);
        Assert.Equal(QuoteStatus.Rejected, rejectedQuote.Status);
        Assert.NotNull(rejectedQuote.Notes);
        Assert.Contains("different vendor", rejectedQuote.Notes);
    }

    [Fact]
    public async Task RecalculateQuote_CreatesNewVersion()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);
        var originalVersion = quote.CurrentVersion;

        // Update job details
        job.TotalLinearFeet = 120.0m;
        var lineItem = await context.JobLineItems.FirstAsync(li => li.JobId == job.Id);
        lineItem.Quantity = 120.0m;
        lineItem.TotalPrice = 2400.00m;
        await context.SaveChangesAsync();

        // Act
        var revisedQuote = await service.RecalculateQuoteAsync(quote.Id, "Customer requested additional 20ft");

        // Assert
        Assert.Equal(originalVersion + 1, revisedQuote.CurrentVersion);
        Assert.Equal(QuoteStatus.Revised, revisedQuote.Status);
        
        var versions = await context.QuoteVersions
            .Where(qv => qv.QuoteId == quote.Id)
            .ToListAsync();
        Assert.Equal(2, versions.Count);
    }

    [Fact]
    public async Task Quote_HasQuoteNumber_UniquePerOrganization()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var job2 = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Second Job",
            CustomerName = "Another Customer",
            OrganizationId = org.Id,
            TotalLinearFeet = 50.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job2);

        var lineItem2 = new JobLineItem
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job2.Id,
            ItemType = LineItemType.Fence,
            FenceTypeId = fenceType.Id,
            Description = "50ft Wood Fence",
            Quantity = 50.0m,
            UnitPrice = 20.00m,
            TotalPrice = 1000.00m
        };
        context.JobLineItems.Add(lineItem2);
        await context.SaveChangesAsync();

        // Act
        var quote1 = await service.GenerateQuoteAsync(job.Id);
        var quote2 = await service.GenerateQuoteAsync(job2.Id);

        // Assert
        Assert.NotEqual(quote1.QuoteNumber, quote2.QuoteNumber);
        Assert.StartsWith("Q-", quote1.QuoteNumber);
        Assert.StartsWith("Q-", quote2.QuoteNumber);
    }

    [Fact]
    public async Task Quote_IncludesBillOfMaterials()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        // Act
        var quote = await service.GenerateQuoteAsync(job.Id);

        // Assert
        Assert.NotEmpty(quote.BillOfMaterials);
        var bomFromDb = await context.BillOfMaterialsItems
            .Where(b => b.QuoteId == quote.Id)
            .ToListAsync();
        Assert.NotEmpty(bomFromDb);
    }

    [Fact]
    public async Task Quote_HasCorrectCostBreakdown()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        // Act
        var quote = await service.GenerateQuoteAsync(job.Id);

        // Assert
        Assert.True(quote.MaterialsCost > 0, "Materials cost should be greater than 0");
        Assert.True(quote.LaborCost > 0, "Labor cost should be greater than 0");
        Assert.True(quote.ContingencyAmount > 0, "Contingency should be greater than 0");
        Assert.True(quote.ProfitAmount > 0, "Profit should be greater than 0");
        
        var expectedSubtotal = quote.MaterialsCost + quote.LaborCost;
        Assert.Equal(expectedSubtotal, quote.Subtotal);
        
        var expectedTotal = quote.Subtotal + quote.ContingencyAmount + quote.ProfitAmount;
        Assert.Equal(expectedTotal, quote.TotalAmount);
    }

    [Fact]
    public async Task Quote_ValidityPeriod_CanBeSet()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);

        // Act
        quote.ValidUntil = DateTime.UtcNow.AddDays(30);
        await context.SaveChangesAsync();

        // Assert
        var savedQuote = await context.Quotes.FindAsync(quote.Id);
        Assert.NotNull(savedQuote);
        Assert.NotNull(savedQuote.ValidUntil);
        Assert.True(savedQuote.ValidUntil > DateTime.UtcNow);
    }

    [Fact]
    public async Task Quote_SupportsTermsAndConditions()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);

        // Act
        quote.Terms = "50% deposit required. Balance due upon completion.";
        await context.SaveChangesAsync();

        // Assert
        var savedQuote = await context.Quotes.FindAsync(quote.Id);
        Assert.NotNull(savedQuote);
        Assert.NotNull(savedQuote.Terms);
        Assert.Contains("50% deposit", savedQuote.Terms);
    }

    [Fact]
    public async Task Quote_VersionHistory_PreservesChanges()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);
        var version1Total = quote.TotalAmount;

        // Modify job
        job.TotalLinearFeet = 150.0m;
        var lineItem = await context.JobLineItems.FirstAsync(li => li.JobId == job.Id);
        lineItem.Quantity = 150.0m;
        lineItem.TotalPrice = 3000.00m;
        await context.SaveChangesAsync();

        // Act
        var revisedQuote = await service.RecalculateQuoteAsync(quote.Id, "Increased scope");

        // Assert
        var versions = await context.QuoteVersions
            .Where(qv => qv.QuoteId == quote.Id)
            .OrderBy(qv => qv.VersionNumber)
            .ToListAsync();

        Assert.Equal(2, versions.Count);
        Assert.Equal(1, versions[0].VersionNumber);
        Assert.Equal(2, versions[1].VersionNumber);
        Assert.NotEqual(versions[0].TotalAmount, versions[1].TotalAmount);
    }

    [Fact]
    public async Task Quote_CanHaveNotes()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, job, fenceType) = await SetupTestDataAsync(context);
        var service = new PricingService(context);

        var quote = await service.GenerateQuoteAsync(job.Id);

        // Act
        quote.Notes = "Customer interested in premium wood upgrade. Provide pricing in revision.";
        await context.SaveChangesAsync();

        // Assert
        var savedQuote = await context.Quotes.FindAsync(quote.Id);
        Assert.NotNull(savedQuote);
        Assert.NotNull(savedQuote.Notes);
        Assert.Contains("premium wood", savedQuote.Notes);
    }
}
