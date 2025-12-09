namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a component that can be used in fences or gates
/// </summary>
public class Component
{
    /// <summary>
    /// Unique identifier for the component
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the component (e.g., "6x6 Post", "2x4 Rail", "Gate Hinge")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the component
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Organization that owns this component
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// SKU or product code
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Category of the component (e.g., "Post", "Rail", "Panel", "Hardware", "Gate Hardware")
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Unit of measure (e.g., "Each", "Linear Foot", "Board Foot")
    /// </summary>
    public string UnitOfMeasure { get; set; } = "Each";

    /// <summary>
    /// Price per unit
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Material of the component (e.g., "Pressure Treated Pine", "Cedar", "Vinyl", "Steel")
    /// </summary>
    public string? Material { get; set; }

    /// <summary>
    /// Dimensions or size information
    /// </summary>
    public string? Dimensions { get; set; }

    /// <summary>
    /// When the component was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the component was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }
}
