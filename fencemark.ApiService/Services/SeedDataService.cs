using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for seeding sample data for demo and testing purposes
/// </summary>
public interface ISeedDataService
{
    Task SeedSampleDataAsync(string organizationId);
    Task<bool> HasSampleDataAsync(string organizationId);
}

public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _db;

    public SeedDataService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasSampleDataAsync(string organizationId)
    {
        // Check if organization already has sample data
        var hasComponents = await _db.Components.AnyAsync(c => c.OrganizationId == organizationId);
        var hasFences = await _db.FenceTypes.AnyAsync(f => f.OrganizationId == organizationId);
        
        return hasComponents || hasFences;
    }

    public async Task SeedSampleDataAsync(string organizationId)
    {
        // Don't seed if data already exists
        if (await HasSampleDataAsync(organizationId))
            return;

        // Seed Components
        var components = new List<Component>
        {
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "6x6 Treated Post",
                Description = "Pressure treated 6\"x6\" fence post",
                Category = "Post",
                Sku = "POST-6X6-PT",
                UnitOfMeasure = "Each",
                UnitPrice = 45.00m,
                Material = "Pressure Treated Pine",
                Dimensions = "6\" x 6\" x 8'",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "2x4 Treated Rail",
                Description = "Pressure treated 2\"x4\" horizontal rail",
                Category = "Rail",
                Sku = "RAIL-2X4-PT",
                UnitOfMeasure = "Linear Foot",
                UnitPrice = 8.20m,
                Material = "Pressure Treated Pine",
                Dimensions = "2\" x 4\" x 8'",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "6' Privacy Panel",
                Description = "Cedar privacy fence panel",
                Category = "Panel",
                Sku = "PANEL-6FT-CEDAR",
                UnitOfMeasure = "Each",
                UnitPrice = 12.69m,
                Material = "Western Red Cedar",
                Dimensions = "6' x 8'",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Gate Hinges Heavy Duty",
                Description = "Heavy duty gate hinges (pair)",
                Category = "Gate Hardware",
                Sku = "HINGE-HD-PAIR",
                UnitOfMeasure = "Pair",
                UnitPrice = 12.50m,
                Material = "Galvanized Steel",
                Dimensions = "12\"",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Gate Latch",
                Description = "Self-closing gate latch",
                Category = "Gate Hardware",
                Sku = "LATCH-SC",
                UnitOfMeasure = "Each",
                UnitPrice = 8.95m,
                Material = "Stainless Steel",
                Dimensions = "Standard",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _db.Components.AddRangeAsync(components);

        // Seed Fence Types
        var fenceTypes = new List<FenceType>
        {
            new FenceType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "6ft Privacy Fence",
                Description = "Standard 6-foot cedar privacy fence",
                HeightInFeet = 6.0m,
                Material = "Cedar",
                Style = "Privacy",
                PricePerLinearFoot = 35.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new FenceType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "4ft Picket Fence",
                Description = "Classic 4-foot white picket fence",
                HeightInFeet = 4.0m,
                Material = "Vinyl",
                Style = "Picket",
                PricePerLinearFoot = 28.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _db.FenceTypes.AddRangeAsync(fenceTypes);

        // Seed Gate Types
        var gateTypes = new List<GateType>
        {
            new GateType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Single Walk Gate",
                Description = "Standard 3-foot walk gate",
                WidthInFeet = 3.0m,
                HeightInFeet = 6.0m,
                Material = "Cedar",
                BasePrice = 250.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GateType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Double Drive Gate",
                Description = "10-foot double drive gate",
                WidthInFeet = 10.0m,
                HeightInFeet = 6.0m,
                Material = "Cedar",
                BasePrice = 850.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _db.GateTypes.AddRangeAsync(gateTypes);

        // Seed Pricing Config
        var pricingConfig = new PricingConfig
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = organizationId,
            Name = "Standard Pricing 2024",
            Description = "Default pricing for residential projects",
            LaborRatePerHour = 50.00m,
            HoursPerLinearMeter = 0.492m, // ~1.5 hours per 10 feet
            ContingencyPercentage = 0.10m, // 10%
            ProfitMarginPercentage = 0.20m, // 20%
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.PricingConfigs.AddAsync(pricingConfig);

        // Seed Height Tiers for the pricing config
        var heightTiers = new List<HeightTier>
        {
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInMeters = 0,
                MaxHeightInMeters = 1.83m, // 6 feet
                Multiplier = 1.0m,
                Description = "Standard height"
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInMeters = 1.83m,
                MaxHeightInMeters = 2.44m, // 8 feet
                Multiplier = 1.25m,
                Description = "Tall fence surcharge (25% increase)"
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInMeters = 2.44m,
                MaxHeightInMeters = null,
                Multiplier = 1.5m,
                Description = "Extra tall fence (50% increase)"
            }
        };

        await _db.HeightTiers.AddRangeAsync(heightTiers);

        // Seed Tax Regions
        var taxRegions = new List<TaxRegion>
        {
            new TaxRegion
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "California",
                Code = "CA",
                TaxRate = 0.0875m, // 8.75%
                Description = "California state sales tax",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TaxRegion
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Texas",
                Code = "TX",
                TaxRate = 0.0625m, // 6.25%
                Description = "Texas state sales tax",
                IsDefault = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _db.TaxRegions.AddRangeAsync(taxRegions);

        // Seed Discount Rules
        var discountRules = new List<DiscountRule>
        {
            new DiscountRule
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Volume Discount",
                Description = "10% off for orders over 500 linear feet",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 0.10m,
                MinimumLinearFeet = 500,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DiscountRule
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Early Bird Special",
                Description = "$500 off for bookings 60 days in advance",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 500.00m,
                MinimumOrderValue = 3000,
                PromoCode = "EARLY2024",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _db.DiscountRules.AddRangeAsync(discountRules);

        await _db.SaveChangesAsync();
    }
}
