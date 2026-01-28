namespace fencemark.Client.Features.FenceSegments;

public class FenceSegmentDto
{
    public string Id { get; set; } = string.Empty;
    public required string OrganizationId { get; set; }
    public required string JobId { get; set; }
    public string? ParcelId { get; set; }
    public string? Name { get; set; }
    public string? FenceTypeId { get; set; }
    public required string GeoJsonGeometry { get; set; }
    public decimal LengthInMetres { get; set; }
    public bool IsSnappedToBoundary { get; set; }
    public string? Notes { get; set; }
    public bool IsVerifiedOnsite { get; set; }
    public decimal? OnsiteVerifiedLengthInMetres { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
