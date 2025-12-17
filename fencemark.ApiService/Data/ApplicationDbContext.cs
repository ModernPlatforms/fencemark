using fencemark.ApiService.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Data;

/// <summary>
/// Application database context integrating Identity and custom entities
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// Organizations in the system
    /// </summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>
    /// Organization memberships
    /// </summary>
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

    /// <summary>
    /// Fence types
    /// </summary>
    public DbSet<FenceType> FenceTypes => Set<FenceType>();

    /// <summary>
    /// Gate types
    /// </summary>
    public DbSet<GateType> GateTypes => Set<GateType>();

    /// <summary>
    /// Components
    /// </summary>
    public DbSet<Component> Components => Set<Component>();

    /// <summary>
    /// Fence components (junction table)
    /// </summary>
    public DbSet<FenceComponent> FenceComponents => Set<FenceComponent>();

    /// <summary>
    /// Gate components (junction table)
    /// </summary>
    public DbSet<GateComponent> GateComponents => Set<GateComponent>();

    /// <summary>
    /// Jobs/Projects
    /// </summary>
    public DbSet<Job> Jobs => Set<Job>();

    /// <summary>
    /// Job line items
    /// </summary>
    public DbSet<JobLineItem> JobLineItems => Set<JobLineItem>();

    /// <summary>
    /// Pricing configurations
    /// </summary>
    public DbSet<PricingConfig> PricingConfigs => Set<PricingConfig>();

    /// <summary>
    /// Height tiers for pricing
    /// </summary>
    public DbSet<HeightTier> HeightTiers => Set<HeightTier>();

    /// <summary>
    /// Quotes
    /// </summary>
    public DbSet<Quote> Quotes => Set<Quote>();

    /// <summary>
    /// Quote versions
    /// </summary>
    public DbSet<QuoteVersion> QuoteVersions => Set<QuoteVersion>();

    /// <summary>
    /// Bill of materials items
    /// </summary>
    public DbSet<BillOfMaterialsItem> BillOfMaterialsItems => Set<BillOfMaterialsItem>();

    /// <summary>
    /// Parcels (properties)
    /// </summary>
    public DbSet<Parcel> Parcels => Set<Parcel>();

    /// <summary>
    /// Drawings and blueprints
    /// </summary>
    public DbSet<Drawing> Drawings => Set<Drawing>();

    /// <summary>
    /// Tax regions
    /// </summary>
    public DbSet<TaxRegion> TaxRegions => Set<TaxRegion>();

    /// <summary>
    /// Discount rules
    /// </summary>
    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Organization
        builder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        // Configure OrganizationMember
        builder.Entity<OrganizationMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.OrganizationId }).IsUnique();
            entity.HasIndex(e => e.InvitationToken);
        });

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Email);
        });

        // Configure FenceType
        builder.Entity<FenceType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PricePerLinearFoot).HasPrecision(18, 2);
            entity.Property(e => e.HeightInFeet).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
        });

        // Configure GateType
        builder.Entity<GateType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BasePrice).HasPrecision(18, 2);
            entity.Property(e => e.WidthInFeet).HasPrecision(18, 2);
            entity.Property(e => e.HeightInFeet).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
        });

        // Configure Component
        builder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Category);
        });

        // Configure FenceComponent
        builder.Entity<FenceComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityPerLinearFoot).HasPrecision(18, 4);
            
            entity.HasOne(e => e.FenceType)
                .WithMany(f => f.Components)
                .HasForeignKey(e => e.FenceTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Component)
                .WithMany()
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.FenceTypeId, e.ComponentId }).IsUnique();
        });

        // Configure GateComponent
        builder.Entity<GateComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityPerGate).HasPrecision(18, 4);
            
            entity.HasOne(e => e.GateType)
                .WithMany(g => g.Components)
                .HasForeignKey(e => e.GateTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Component)
                .WithMany()
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.GateTypeId, e.ComponentId }).IsUnique();
        });

        // Configure Job
        builder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalLinearFeet).HasPrecision(18, 2);
            entity.Property(e => e.LaborCost).HasPrecision(18, 2);
            entity.Property(e => e.MaterialsCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
        });

        // Configure JobLineItem
        builder.Entity<JobLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Job)
                .WithMany(j => j.LineItems)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FenceType)
                .WithMany()
                .HasForeignKey(e => e.FenceTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.GateType)
                .WithMany()
                .HasForeignKey(e => e.GateTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.JobId);
        });

        // Configure PricingConfig
        builder.Entity<PricingConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LaborRatePerHour).HasPrecision(18, 2);
            entity.Property(e => e.HoursPerLinearMeter).HasPrecision(18, 4);
            entity.Property(e => e.ContingencyPercentage).HasPrecision(5, 4);
            entity.Property(e => e.ProfitMarginPercentage).HasPrecision(5, 4);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.IsDefault });
        });

        // Configure HeightTier
        builder.Entity<HeightTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MinHeightInMeters).HasPrecision(18, 2);
            entity.Property(e => e.MaxHeightInMeters).HasPrecision(18, 2);
            entity.Property(e => e.Multiplier).HasPrecision(18, 4);
            
            entity.HasOne(e => e.PricingConfig)
                .WithMany(p => p.HeightTiers)
                .HasForeignKey(e => e.PricingConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PricingConfigId);
        });

        // Configure Quote
        builder.Entity<Quote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuoteNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MaterialsCost).HasPrecision(18, 2);
            entity.Property(e => e.LaborCost).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.ContingencyAmount).HasPrecision(18, 2);
            entity.Property(e => e.ProfitAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.GrandTotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PricingConfig)
                .WithMany()
                .HasForeignKey(e => e.PricingConfigId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TaxRegion)
                .WithMany()
                .HasForeignKey(e => e.TaxRegionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.DiscountRule)
                .WithMany()
                .HasForeignKey(e => e.DiscountRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.QuoteNumber).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        // Configure QuoteVersion
        builder.Entity<QuoteVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaterialsCost).HasPrecision(18, 2);
            entity.Property(e => e.LaborCost).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.ContingencyAmount).HasPrecision(18, 2);
            entity.Property(e => e.ProfitAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.GrandTotal).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Quote)
                .WithMany(q => q.Versions)
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.QuoteId, e.VersionNumber }).IsUnique();
        });

        // Configure BillOfMaterialsItem
        builder.Entity<BillOfMaterialsItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Quote)
                .WithMany(q => q.BillOfMaterials)
                .HasForeignKey(e => e.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Component)
                .WithMany()
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.QuoteId);
            entity.HasIndex(e => e.Category);
        });

        // Configure Parcel
        builder.Entity<Parcel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ParcelNumber).HasMaxLength(100);
            entity.Property(e => e.TotalArea).HasPrecision(18, 2);
            entity.Property(e => e.AreaUnit).HasMaxLength(20);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.JobId);
        });

        // Configure Drawing
        builder.Entity<Drawing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DrawingType).HasMaxLength(100);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.MimeType).HasMaxLength(100);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Parcel)
                .WithMany(p => p.Drawings)
                .HasForeignKey(e => e.ParcelId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.ParcelId);
        });

        // Configure TaxRegion
        builder.Entity<TaxRegion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.TaxRate).HasPrecision(5, 4);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.IsDefault });
        });

        // Configure DiscountRule
        builder.Entity<DiscountRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountValue).HasPrecision(18, 4);
            entity.Property(e => e.MinimumOrderValue).HasPrecision(18, 2);
            entity.Property(e => e.MinimumLinearFeet).HasPrecision(18, 2);
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.PromoCode);
            entity.HasIndex(e => e.IsActive);
        });

        // ============================================================================
        // Removed Global Query Filters for Multi-Tenant Data Isolation
        // ============================================================================
        // The SQL Server RLS + SESSION_CONTEXT in TenantConnectionInterceptor
        // is now the single source of tenant isolation, making these filters redundant.

        // var currentOrganizationId = _currentUserService?.OrganizationId;

        // // Only apply filters if we have a current organization context
        // // This allows migrations and system operations to work without a user context
        // if (!string.IsNullOrEmpty(currentOrganizationId))
        // {
        //     builder.Entity<Component>()
        //         .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);

        //     builder.Entity<FenceType>()
        //         .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);

        //     builder.Entity<GateType>()
        //         .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);

        //     builder.Entity<Job>()
        //         .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);

        //     builder.Entity<PricingConfig>()
        //         .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);

        //     builder.Entity<Quote>()
        //         .HasQueryFilter(e => e.OrganizationId == currentOrganizationId);
        // }
    }
}
