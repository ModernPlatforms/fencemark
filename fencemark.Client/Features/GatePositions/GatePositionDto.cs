namespace fencemark.Client.Features.GatePositions;

public class GatePositionDto
{
    public string Id { get; set; } = string.Empty;
    public required string OrganizationId { get; set; }
    public required string FenceSegmentId { get; set; }
    public string? GateTypeId { get; set; }
    public string? Name { get; set; }
    public required string GeoJsonLocation { get; set; }
    public decimal PositionAlongSegment { get; set; }
    public string? Notes { get; set; }
    public bool IsVerifiedOnsite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
