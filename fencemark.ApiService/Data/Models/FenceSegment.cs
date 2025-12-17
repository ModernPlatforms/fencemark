namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a drawn fence segment on a map
/// </summary>
public class FenceSegment : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the fence segment
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this fence segment
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Job this fence segment belongs to
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Parcel this fence segment belongs to (optional)
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// Name or label for the fence segment
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Fence type being used for this segment
    /// </summary>
    public string? FenceTypeId { get; set; }

    /// <summary>
    /// GeoJSON geometry data for the fence line (LineString format)
    /// Example: {"type":"LineString","coordinates":[[-122.4194,37.7749],[-122.4180,37.7755]]}
    /// </summary>
    public required string GeoJsonGeometry { get; set; }

    /// <summary>
    /// Calculated length of the fence segment in feet
    /// </summary>
    public decimal LengthInFeet { get; set; }

    /// <summary>
    /// Calculated length of the fence segment in meters
    /// </summary>
    public decimal LengthInMeters { get; set; }

    /// <summary>
    /// Whether this segment was snapped to a lot boundary
    /// </summary>
    public bool IsSnappedToBoundary { get; set; }

    /// <summary>
    /// Notes or comments about this segment
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this segment has been verified onsite
    /// </summary>
    public bool IsVerifiedOnsite { get; set; }

    /// <summary>
    /// Onsite measurement correction (if different from satellite measurement)
    /// </summary>
    public decimal? OnsiteVerifiedLengthInFeet { get; set; }

    /// <summary>
    /// When the fence segment was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the fence segment was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for the job
    /// </summary>
    public Job? Job { get; set; }

    /// <summary>
    /// Navigation property for the parcel
    /// </summary>
    public Parcel? Parcel { get; set; }

    /// <summary>
    /// Navigation property for the fence type
    /// </summary>
    public FenceType? FenceType { get; set; }

    /// <summary>
    /// Navigation property for gates on this segment
    /// </summary>
    public ICollection<GatePosition> GatePositions { get; set; } = [];
}
