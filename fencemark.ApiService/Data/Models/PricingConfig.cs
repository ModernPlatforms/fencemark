namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents tenant-specific pricing configuration
/// </summary>
public class PricingConfig : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the pricing configuration
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this pricing configuration
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Name of the pricing configuration (e.g., "Standard", "Premium")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of this pricing configuration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Base labor rate per hour
    /// </summary>
    public decimal LaborRatePerHour { get; set; }

    /// <summary>
    /// Estimated hours per linear meter of fence
    /// </summary>
    public decimal HoursPerLinearMeter { get; set; }

    /// <summary>
    /// Contingency percentage (e.g., 0.10 for 10%)
    /// </summary>
    public decimal ContingencyPercentage { get; set; } = 0.10m;

    /// <summary>
    /// Profit margin percentage (e.g., 0.20 for 20%)
    /// </summary>
    public decimal ProfitMarginPercentage { get; set; } = 0.20m;

    /// <summary>
    /// Whether this is the default pricing configuration for the organization
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// When the pricing configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the pricing configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for height tiers
    /// </summary>
    public ICollection<HeightTier> HeightTiers { get; set; } = [];
}

/// <summary>
/// Represents a pricing tier based on fence height
/// </summary>
public class HeightTier
{
    /// <summary>
    /// Unique identifier for the height tier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Pricing configuration this tier belongs to
    /// </summary>
    public required string PricingConfigId { get; set; }

    /// <summary>
    /// Minimum height in meters for this tier
    /// </summary>
    public decimal MinHeightInMeters { get; set; }

    /// <summary>
    /// Maximum height in meters for this tier (null for unlimited)
    /// </summary>
    public decimal? MaxHeightInMeters { get; set; }

    /// <summary>
    /// Multiplier to apply to base price (e.g., 1.0 for standard, 1.25 for 25% increase)
    /// </summary>
    public decimal Multiplier { get; set; } = 1.0m;

    /// <summary>
    /// Description of this tier (e.g., "Standard Height", "Tall Fence Surcharge")
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property for the pricing configuration
    /// </summary>
    public PricingConfig? PricingConfig { get; set; }
}
