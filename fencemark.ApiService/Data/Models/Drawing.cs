namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a drawing, blueprint, or site plan attachment
/// </summary>
public class Drawing : IOrganizationScoped
{
    /// <summary>
    /// Unique identifier for the drawing
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Organization that owns this drawing
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Job this drawing belongs to (optional - can be standalone)
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Parcel this drawing belongs to (optional)
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// Drawing title or name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of the drawing
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Drawing type (e.g., "Site Plan", "Blueprint", "Survey", "Photo")
    /// </summary>
    public string? DrawingType { get; set; }

    /// <summary>
    /// File name of the drawing
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// File path or URL where the drawing is stored
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// MIME type of the file (e.g., "image/png", "application/pdf")
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Version number of the drawing
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the drawing was uploaded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the drawing was last updated
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
}
