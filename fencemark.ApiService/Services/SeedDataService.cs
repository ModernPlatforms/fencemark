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
        {
            // Data already exists, nothing to do
            return;
        }

        // Seed Components
        var components = new List<Component>
        {
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "150x150mm Treated Post",
                Description = "Pressure treated 150mm x 150mm fence post",
                Category = "Post",
                Sku = "POST-150X150-PT",
                UnitOfMeasure = "Each",
                UnitPrice = 65.00m,
                Material = "Pressure Treated Pine",
                Dimensions = "150mm x 150mm x 2400mm",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "90x45mm Treated Rail",
                Description = "Pressure treated 90mm x 45mm horizontal rail",
                Category = "Rail",
                Sku = "RAIL-90X45-PT",
                UnitOfMeasure = "Linear Metre",
                UnitPrice = 12.50m,
                Material = "Pressure Treated Pine",
                Dimensions = "90mm x 45mm x 2400mm",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Component
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "1800mm Privacy Panel",
                Description = "Treated pine privacy fence panel",
                Category = "Panel",
                Sku = "PANEL-1800-PT",
                UnitOfMeasure = "Each",
                UnitPrice = 85.00m,
                Material = "Treated Pine",
                Dimensions = "1800mm x 2400mm",
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
                UnitPrice = 35.00m,
                Material = "Galvanised Steel",
                Dimensions = "300mm",
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
                UnitPrice = 25.00m,
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
                Name = "1800mm Privacy Fence",
                Description = "Standard 1800mm treated pine privacy fence",
                HeightInFeet = 5.91m, // 1800mm in feet
                Material = "Treated Pine",
                Style = "Privacy",
                PricePerLinearFoot = 115.00m, // per metre
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new FenceType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "1200mm Picket Fence",
                Description = "Classic 1200mm timber picket fence",
                HeightInFeet = 3.94m, // 1200mm in feet
                Material = "Timber",
                Style = "Picket",
                PricePerLinearFoot = 95.00m, // per metre
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new FenceType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "2100mm Privacy Fence",
                Description = "Extra tall 2100mm treated pine privacy fence",
                HeightInFeet = 6.89m, // 2100mm in feet
                Material = "Treated Pine",
                Style = "Privacy",
                PricePerLinearFoot = 145.00m, // per metre
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
                Description = "Standard 900mm walk gate",
                WidthInFeet = 2.95m, // 900mm in feet
                HeightInFeet = 5.91m, // 1800mm in feet
                Material = "Treated Pine",
                BasePrice = 385.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GateType
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Double Driveway Gate",
                Description = "3000mm double driveway gate",
                WidthInFeet = 9.84m, // 3000mm in feet
                HeightInFeet = 5.91m, // 1800mm in feet
                Material = "Treated Pine",
                BasePrice = 1250.00m,
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
            LaborRatePerHour = 85.00m,
            HoursPerLinearMeter = 0.5m, // ~0.5 hours per linear metre
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
                MaxHeightInMeters = 1.8m, // 1800mm
                Multiplier = 1.0m,
                Description = "Standard height (up to 1800mm)"
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInMeters = 1.8m,
                MaxHeightInMeters = 2.1m, // 2100mm
                Multiplier = 1.25m,
                Description = "Tall fence surcharge (1800mm-2100mm, 25% increase)"
            },
            new HeightTier
            {
                Id = Guid.NewGuid().ToString(),
                PricingConfigId = pricingConfig.Id,
                MinHeightInMeters = 2.1m,
                MaxHeightInMeters = null,
                Multiplier = 1.5m,
                Description = "Extra tall fence (over 2100mm, 50% increase)"
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
                Name = "Australia",
                Code = "AU",
                TaxRate = 0.10m, // 10% GST
                Description = "Australian GST",
                IsDefault = true,
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
                Description = "10% off for orders over 150 linear metres",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 0.10m,
                MinimumLinearFeet = 492.13m, // ~150 metres in feet
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DiscountRule
            {
                Id = Guid.NewGuid().ToString(),
                OrganizationId = organizationId,
                Name = "Early Bird Special",
                Description = "$750 off for bookings 60 days in advance",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 750.00m,
                MinimumOrderValue = 5000,
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
