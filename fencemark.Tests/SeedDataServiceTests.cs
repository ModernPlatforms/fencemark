using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.Tests;

/// <summary>
/// Unit tests for SeedDataService to ensure sample data seeding works correctly
/// </summary>
public class SeedDataServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SeedSampleDataAsync_CreatesAllExpectedEntities()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var organizationId = Guid.NewGuid().ToString();

        // Act
        await service.SeedSampleDataAsync(organizationId);

        // Assert - Verify all entity types were created
        var components = await context.Components.Where(c => c.OrganizationId == organizationId).ToListAsync();
        var fenceTypes = await context.FenceTypes.Where(f => f.OrganizationId == organizationId).ToListAsync();
        var gateTypes = await context.GateTypes.Where(g => g.OrganizationId == organizationId).ToListAsync();
        var pricingConfigs = await context.PricingConfigs.Where(p => p.OrganizationId == organizationId).ToListAsync();
        var taxRegions = await context.TaxRegions.Where(t => t.OrganizationId == organizationId).ToListAsync();
        var discountRules = await context.DiscountRules.Where(d => d.OrganizationId == organizationId).ToListAsync();

        Assert.NotEmpty(components);
        Assert.NotEmpty(fenceTypes);
        Assert.NotEmpty(gateTypes);
        Assert.NotEmpty(pricingConfigs);
        Assert.NotEmpty(taxRegions);
        Assert.NotEmpty(discountRules);
    }

    [Fact]
    public async Task HasSampleDataAsync_ReturnsFalse_WhenNoDataExists()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var organizationId = Guid.NewGuid().ToString();

        // Act
        var hasSampleData = await service.HasSampleDataAsync(organizationId);

        // Assert
        Assert.False(hasSampleData);
    }

    [Fact]
    public async Task HasSampleDataAsync_ReturnsTrue_WhenComponentsExist()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var organizationId = Guid.NewGuid().ToString();

        var component = new Component
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = organizationId,
            Name = "Test Component",
            Category = "Test"
        };
        context.Components.Add(component);
        await context.SaveChangesAsync();

        // Act
        var hasSampleData = await service.HasSampleDataAsync(organizationId);

        // Assert
        Assert.True(hasSampleData);
    }

    [Fact]
    public async Task HasSampleDataAsync_ReturnsTrue_WhenFenceTypesExist()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var organizationId = Guid.NewGuid().ToString();

        var fenceType = new FenceType
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = organizationId,
            Name = "Test Fence"
        };
        context.FenceTypes.Add(fenceType);
        await context.SaveChangesAsync();

        // Act
        var hasSampleData = await service.HasSampleDataAsync(organizationId);

        // Assert
        Assert.True(hasSampleData);
    }

    [Fact]
    public async Task SeedSampleDataAsync_CanBeCalledMultipleTimes_WithoutErrors()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var organizationId = Guid.NewGuid().ToString();

        // Act - Seed twice
        await service.SeedSampleDataAsync(organizationId);
        await service.SeedSampleDataAsync(organizationId);

        // Assert - Should not throw and should have data (possibly duplicated)
        var components = await context.Components.Where(c => c.OrganizationId == organizationId).ToListAsync();
        var fenceTypes = await context.FenceTypes.Where(f => f.OrganizationId == organizationId).ToListAsync();

        Assert.NotEmpty(components);
        Assert.NotEmpty(fenceTypes);
    }

    [Fact]
    public async Task SeedSampleDataAsync_OnlyCreatesDataForSpecifiedOrganization()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var org1Id = Guid.NewGuid().ToString();
        var org2Id = Guid.NewGuid().ToString();

        // Act - Seed for org1 only
        await service.SeedSampleDataAsync(org1Id);

        // Assert - Org1 has data, Org2 does not
        var org1Components = await context.Components.Where(c => c.OrganizationId == org1Id).ToListAsync();
        var org2Components = await context.Components.Where(c => c.OrganizationId == org2Id).ToListAsync();

        Assert.NotEmpty(org1Components);
        Assert.Empty(org2Components);
    }

    [Fact]
    public async Task HasSampleDataAsync_ReturnsFalse_ForDifferentOrganization()
    {
        // Arrange
        var context = CreateDbContext();
        var service = new SeedDataService(context);
        var org1Id = Guid.NewGuid().ToString();
        var org2Id = Guid.NewGuid().ToString();

        // Seed data for org1
        await service.SeedSampleDataAsync(org1Id);

        // Act - Check if org2 has data
        var org2HasData = await service.HasSampleDataAsync(org2Id);

        // Assert
        Assert.False(org2HasData);
    }
}
