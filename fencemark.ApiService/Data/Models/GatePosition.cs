namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a gate positioned on a fence segment
/// </summary>
public class GatePosition : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the gate position
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this gate position
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Fence segment this gate is placed on
    /// </summary>
    public required string FenceSegmentId { get; set; }

    /// <summary>
    /// Gate type being used
    /// </summary>
    public string? GateTypeId { get; set; }

    /// <summary>
    /// Name or label for the gate
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// GeoJSON point geometry for the gate location
    /// Example: {"type":"Point","coordinates":[-122.4187,37.7752]}
    /// </summary>
    public required string GeoJsonLocation { get; set; }

    /// <summary>
    /// Position along the fence segment (0.0 = start, 1.0 = end)
    /// </summary>
    public decimal PositionAlongSegment { get; set; }

    /// <summary>
    /// Notes or comments about this gate
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this gate position has been verified onsite
    /// </summary>
    public bool IsVerifiedOnsite { get; set; }

    /// <summary>
    /// When the gate position was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the gate position was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for the fence segment
    /// </summary>
    public FenceSegment? FenceSegment { get; set; }

    /// <summary>
    /// Navigation property for the gate type
    /// </summary>
    public GateType? GateType { get; set; }
}
