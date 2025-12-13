using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.Tests;

public class PricingServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Organization org, PricingConfig config, FenceType fenceType, GateType gateType, Component[] components)> SetupTestDataAsync(ApplicationDbContext context)
    {
        // Create organization
        var org = new Organization
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Organization"
        };
        context.Organizations.Add(org);

        // Create components
        var components = new[]
        {
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                Name = "6x6 Post",
                OrganizationId = org.Id,
                Category = "Posts",
                UnitOfMeasure = "Each",
                UnitPrice = 45.00m
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                Name = "2x4 Rail",
                OrganizationId = org.Id,
                Category = "Rails",
                UnitOfMeasure = "Linear Foot",
                UnitPrice = 3.50m
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Fence Panel",
                OrganizationId = org.Id,
                Category = "Panels",
                UnitOfMeasure = "Each",
                UnitPrice = 65.00m
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Gate Hinge",
                OrganizationId = org.Id,
                Category = "Gate Hardware",
                UnitOfMeasure = "Each",
                UnitPrice = 12.50m
            }
        };
        context.Components.AddRange(components);

        // Create pricing config
        var pricingConfig = new PricingConfig
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = org.Id,
            Name = "Standard Pricing",
            LaborRatePerHour = 50.00m,
            HoursPerLinearFoot = 0.15m,
            ContingencyPercentage = 0.10m,
            ProfitMarginPercentage = 0.20m,
            IsDefault = true
        };
        context.PricingConfigs.Add(pricingConfig);

        // Create height tiers
        var heightTiers = new[]
        {
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInFeet = 0,
                MaxHeightInFeet = 6,
                Multiplier = 1.0m,
                Description = "Standard height"
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInFeet = 6,
                MaxHeightInFeet = 8,
                Multiplier = 1.25m,
                Description = "Tall fence surcharge"
            }
        };
        context.HeightTiers.AddRange(heightTiers);

        // Create fence type
        var fenceType = new FenceType
        {
            Id = Guid.NewGuid().ToString(),
            Name = "6ft Privacy Fence",
            OrganizationId = org.Id,
            HeightInFeet = 6.0m,
            Material = "Wood",
            Style = "Privacy",
            PricePerLinearFoot = 25.00m
        };
        context.FenceTypes.Add(fenceType);

        // Add fence components
        var fenceComponents = new[]
        {
            new FenceComponent
            {
                Id = Guid.NewGuid().ToString(),
                FenceTypeId = fenceType.Id,
                ComponentId = components[0].Id,
                QuantityPerLinearFoot = 0.125m // 1 post per 8 feet
            },
            new FenceComponent
            {
                Id = Guid.NewGuid().ToString(),
                FenceTypeId = fenceType.Id,
                ComponentId = components[1].Id,
                QuantityPerLinearFoot = 3.0m // 3 rails per foot
            },
            new FenceComponent
            {
                Id = Guid.NewGuid().ToString(),
                FenceTypeId = fenceType.Id,
                ComponentId = components[2].Id,
                QuantityPerLinearFoot = 0.125m // 1 panel per 8 feet
            }
        };
        context.FenceComponents.AddRange(fenceComponents);

        // Create gate type
        var gateType = new GateType
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Single Walk Gate",
            OrganizationId = org.Id,
            WidthInFeet = 4.0m,
            HeightInFeet = 6.0m,
            Material = "Wood",
            Style = "Walk-through",
            BasePrice = 350.00m
        };
        context.GateTypes.Add(gateType);

        // Add gate components
        var gateComponents = new[]
        {
            new GateComponent
            {
                Id = Guid.NewGuid().ToString(),
                GateTypeId = gateType.Id,
                ComponentId = components[3].Id,
                QuantityPerGate = 2.0m // 2 hinges per gate
            }
        };
        context.GateComponents.AddRange(gateComponents);

        await context.SaveChangesAsync();

        return (org, pricingConfig, fenceType, gateType, components);
    }

    [Fact]
    public async Task GenerateQuoteAsync_WithValidJob_GeneratesQuoteWithBOM()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, fenceType, gateType, components) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "John Doe",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m
        };
        context.Jobs.Add(job);

        var lineItems = new[]
        {
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Fence,
                FenceTypeId = fenceType.Id,
                Description = "100ft 6ft Privacy Fence",
                Quantity = 100.0m,
                UnitPrice = 25.00m,
                TotalPrice = 2500.00m
            },
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Gate,
                GateTypeId = gateType.Id,
                Description = "Single Walk Gate",
                Quantity = 1.0m,
                UnitPrice = 350.00m,
                TotalPrice = 350.00m
            }
        };
        context.JobLineItems.AddRange(lineItems);
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // Act
        var quote = await service.GenerateQuoteAsync(job.Id);

        // Assert
        Assert.NotNull(quote);
        Assert.Equal(job.Id, quote.JobId);
        Assert.Equal(org.Id, quote.OrganizationId);
        Assert.True(quote.MaterialsCost > 0);
        Assert.True(quote.LaborCost > 0);
        Assert.True(quote.ContingencyAmount > 0);
        Assert.True(quote.ProfitAmount > 0);
        Assert.True(quote.TotalAmount > 0);
        Assert.NotEmpty(quote.BillOfMaterials);
        Assert.Single(quote.Versions);
        Assert.Equal(1, quote.CurrentVersion);
    }

    [Fact]
    public async Task GenerateQuoteAsync_CalculatesLaborCostCorrectly()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, fenceType, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Jane Smith",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m
        };
        context.Jobs.Add(job);

        var lineItem = new JobLineItem
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            ItemType = LineItemType.Fence,
            FenceTypeId = fenceType.Id,
            Description = "100ft fence",
            Quantity = 100.0m,
            UnitPrice = 25.00m,
            TotalPrice = 2500.00m
        };
        context.JobLineItems.Add(lineItem);
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // Act
        var quote = await service.GenerateQuoteAsync(job.Id);

        // Assert
        // Expected: 100 ft * 0.15 hours/ft * $50/hour = $750
        Assert.Equal(750.00m, quote.LaborCost);
    }

    [Fact]
    public async Task GenerateQuoteAsync_AppliesContingencyAndProfitCorrectly()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, fenceType, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m
        };
        context.Jobs.Add(job);

        var lineItem = new JobLineItem
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            ItemType = LineItemType.Fence,
            FenceTypeId = fenceType.Id,
            Description = "100ft fence",
            Quantity = 100.0m,
            UnitPrice = 25.00m,
            TotalPrice = 2500.00m
        };
        context.JobLineItems.Add(lineItem);
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // Act
        var quote = await service.GenerateQuoteAsync(job.Id);

        // Assert
        var expectedSubtotal = quote.MaterialsCost + quote.LaborCost;
        Assert.Equal(expectedSubtotal, quote.Subtotal);

        var expectedContingency = expectedSubtotal * 0.10m;
        Assert.Equal(expectedContingency, quote.ContingencyAmount);

        var expectedProfit = (expectedSubtotal + expectedContingency) * 0.20m;
        Assert.Equal(expectedProfit, quote.ProfitAmount);

        var expectedTotal = expectedSubtotal + expectedContingency + expectedProfit;
        Assert.Equal(expectedTotal, quote.TotalAmount);
    }

    [Fact]
    public async Task CalculateBillOfMaterialsAsync_IncludesAllComponents()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, fenceType, gateType, components) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            TotalLinearFeet = 80.0m
        };
        context.Jobs.Add(job);

        var lineItems = new[]
        {
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Fence,
                FenceTypeId = fenceType.Id,
                Description = "80ft fence",
                Quantity = 80.0m,
                UnitPrice = 25.00m,
                TotalPrice = 2000.00m
            },
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Gate,
                GateTypeId = gateType.Id,
                Description = "Gate",
                Quantity = 2.0m,
                UnitPrice = 350.00m,
                TotalPrice = 700.00m
            }
        };
        context.JobLineItems.AddRange(lineItems);
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // Act
        var bom = await service.CalculateBillOfMaterialsAsync(job.Id);

        // Assert
        Assert.NotEmpty(bom);
        
        // Check for posts (80 ft * 0.125 = 10 posts)
        var posts = bom.FirstOrDefault(b => b.Description == "6x6 Post");
        Assert.NotNull(posts);
        Assert.Equal(10.0m, posts.Quantity);
        
        // Check for rails (80 ft * 3 = 240 linear feet)
        var rails = bom.FirstOrDefault(b => b.Description == "2x4 Rail");
        Assert.NotNull(rails);
        Assert.Equal(240.0m, rails.Quantity);
        
        // Check for panels (80 ft * 0.125 = 10 panels)
        var panels = bom.FirstOrDefault(b => b.Description == "Fence Panel");
        Assert.NotNull(panels);
        Assert.Equal(10.0m, panels.Quantity);
        
        // Check for gate hinges (2 gates * 2 hinges = 4 hinges)
        var hinges = bom.FirstOrDefault(b => b.Description == "Gate Hinge");
        Assert.NotNull(hinges);
        Assert.Equal(4.0m, hinges.Quantity);
        
        // Check for labor
        var labor = bom.FirstOrDefault(b => b.Category == "Labor");
        Assert.NotNull(labor);
    }

    [Fact]
    public async Task GetHeightMultiplier_ReturnsCorrectMultiplierForHeight()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new PricingService(context);

        var tiers = new List<HeightTier>
        {
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = "test",
                MinHeightInFeet = 0,
                MaxHeightInFeet = 6,
                Multiplier = 1.0m
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = "test",
                MinHeightInFeet = 6,
                MaxHeightInFeet = 8,
                Multiplier = 1.25m
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = "test",
                MinHeightInFeet = 8,
                MaxHeightInFeet = null,
                Multiplier = 1.50m
            }
        };

        // Act & Assert
        Assert.Equal(1.0m, service.GetHeightMultiplier(tiers, 4.0m));
        Assert.Equal(1.0m, service.GetHeightMultiplier(tiers, 6.0m));
        Assert.Equal(1.25m, service.GetHeightMultiplier(tiers, 7.0m));
        Assert.Equal(1.50m, service.GetHeightMultiplier(tiers, 9.0m));
        Assert.Equal(1.50m, service.GetHeightMultiplier(tiers, 12.0m));
    }

    [Fact]
    public async Task RecalculateQuoteAsync_UpdatesQuoteAndCreatesNewVersion()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, fenceType, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            TotalLinearFeet = 100.0m
        };
        context.Jobs.Add(job);

        var lineItem = new JobLineItem
        {
            Id = Guid.NewGuid().ToString(),
            JobId = job.Id,
            ItemType = LineItemType.Fence,
            FenceTypeId = fenceType.Id,
            Description = "100ft fence",
            Quantity = 100.0m,
            UnitPrice = 25.00m,
            TotalPrice = 2500.00m
        };
        context.JobLineItems.Add(lineItem);
        await context.SaveChangesAsync();

        var service = new PricingService(context);
        var quote = await service.GenerateQuoteAsync(job.Id);
        var originalTotal = quote.TotalAmount;
        var originalVersion = quote.CurrentVersion;

        // Update job to have more linear feet
        job.TotalLinearFeet = 150.0m;
        lineItem.Quantity = 150.0m;
        lineItem.TotalPrice = 3750.00m;
        await context.SaveChangesAsync();

        // Act
        var updatedQuote = await service.RecalculateQuoteAsync(quote.Id, "Increased fence length");

        // Assert
        Assert.Equal(originalVersion + 1, updatedQuote.CurrentVersion);
        Assert.True(updatedQuote.TotalAmount > originalTotal);
        Assert.Equal(QuoteStatus.Revised, updatedQuote.Status);
        Assert.Equal(2, await context.QuoteVersions.CountAsync(qv => qv.QuoteId == quote.Id));
    }

    [Fact]
    public async Task GenerateQuoteAsync_ThrowsException_WhenJobNotFound()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = new PricingService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GenerateQuoteAsync("nonexistent-id"));
    }

    [Fact]
    public async Task GenerateQuoteAsync_ThrowsException_WhenNoPricingConfig()
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
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.GenerateQuoteAsync(job.Id));
    }

    [Fact]
    public async Task GenerateQuoteAsync_GeneratesUniqueQuoteNumbers()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, config, fenceType, _, _) = await SetupTestDataAsync(context);

        var jobs = new[]
        {
            new Job
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Job 1",
                CustomerName = "Customer 1",
                OrganizationId = org.Id,
                TotalLinearFeet = 100.0m
            },
            new Job
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Job 2",
                CustomerName = "Customer 2",
                OrganizationId = org.Id,
                TotalLinearFeet = 100.0m
            }
        };
        context.Jobs.AddRange(jobs);

        foreach (var job in jobs)
        {
            var lineItem = new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Fence,
                FenceTypeId = fenceType.Id,
                Description = "100ft fence",
                Quantity = 100.0m,
                UnitPrice = 25.00m,
                TotalPrice = 2500.00m
            };
            context.JobLineItems.Add(lineItem);
        }
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // Act
        var quote1 = await service.GenerateQuoteAsync(jobs[0].Id);
        var quote2 = await service.GenerateQuoteAsync(jobs[1].Id);

        // Assert
        Assert.NotEqual(quote1.QuoteNumber, quote2.QuoteNumber);
        Assert.StartsWith("Q-", quote1.QuoteNumber);
        Assert.StartsWith("Q-", quote2.QuoteNumber);
    }
}
