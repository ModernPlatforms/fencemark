namespace fencemark.Client.Features.Gates;

/// <summary>
/// Data transfer object for gate type information
/// </summary>
public class GateTypeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public decimal WidthInFeet { get; set; }
    public decimal HeightInFeet { get; set; }
    public string? Material { get; set; }
    public string? Style { get; set; }
    public decimal BasePrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
