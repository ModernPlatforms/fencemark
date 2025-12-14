namespace fencemark.Web.Features.Jobs;

/// <summary>
/// Data transfer object for job information
/// </summary>
public class JobDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? InstallationAddress { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public JobStatusDto Status { get; set; } = JobStatusDto.Draft;
    public decimal TotalLinearFeet { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EstimatedStartDate { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
}

/// <summary>
/// Status of a job
/// </summary>
public enum JobStatusDto
{
    Draft,
    Quoted,
    Approved,
    InProgress,
    Completed,
    Cancelled
}
