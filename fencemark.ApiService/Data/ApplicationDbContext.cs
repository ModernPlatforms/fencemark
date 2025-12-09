using fencemark.ApiService.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Data;

/// <summary>
/// Application database context integrating Identity and custom entities
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
    : IdentityDbContext<ApplicationUser>(options)
{
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
    }
}
