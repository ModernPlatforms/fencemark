namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a discount rule that can be applied to quotes
/// </summary>
public class DiscountRule : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the discount rule
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this discount rule
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Name of the discount rule (e.g., "Volume Discount", "Early Bird Special")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of the discount rule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of discount (Percentage, FixedAmount, PerLinearFoot)
    /// </summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>
    /// Discount value (percentage as decimal like 0.10 for 10%, or fixed amount in dollars)
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// Minimum order value to qualify for discount (optional)
    /// </summary>
    public decimal? MinimumOrderValue { get; set; }

    /// <summary>
    /// Minimum linear footage to qualify for discount (optional)
    /// </summary>
    public decimal? MinimumLinearFeet { get; set; }

    /// <summary>
    /// Start date for the discount (optional)
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End date for the discount (optional)
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Whether this discount is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Promotional code for this discount (optional)
    /// </summary>
    public string? PromoCode { get; set; }

    /// <summary>
    /// When the discount rule was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the discount rule was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }
}

/// <summary>
/// Types of discounts that can be applied
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Percentage discount (e.g., 10% off)
    /// </summary>
    Percentage,

    /// <summary>
    /// Fixed dollar amount discount (e.g., $100 off)
    /// </summary>
    FixedAmount,

    /// <summary>
    /// Discount per linear foot (e.g., $0.50 off per foot)
    /// </summary>
    PerLinearFoot
}
