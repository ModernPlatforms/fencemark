namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a property or parcel where fencing will be installed
/// </summary>
public class Parcel : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the parcel
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this parcel
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Job this parcel belongs to
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Parcel name or identifier (e.g., "Front Yard", "Lot 5")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Street address of the parcel
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Assessor's parcel number or legal description
    /// </summary>
    public string? ParcelNumber { get; set; }

    /// <summary>
    /// Total area of the parcel (in square feet or square meters)
    /// </summary>
    public decimal? TotalArea { get; set; }

    /// <summary>
    /// Unit of measurement for area
    /// </summary>
    public string? AreaUnit { get; set; } = "sqft";

    /// <summary>
    /// GPS coordinates in JSON format (e.g., {"lat": 37.7749, "lng": -122.4194})
    /// </summary>
    public string? Coordinates { get; set; }

    /// <summary>
    /// Additional notes about the parcel
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the parcel was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the parcel was last updated
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
    /// Navigation property for drawings associated with this parcel
    /// </summary>
    public ICollection<Drawing> Drawings { get; set; } = [];
}
