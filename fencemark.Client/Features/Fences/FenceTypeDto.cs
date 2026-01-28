namespace fencemark.Client.Features.Fences;

/// <summary>
/// Data transfer object for fence type information
/// </summary>
public class FenceTypeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public decimal HeightInMm { get; set; }
    public string? Material { get; set; }
    public string? Style { get; set; }
    public decimal PricePerLinearMetre { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
