namespace fencemark.ApiService.Data.Models;

/// <summary>
/// Represents a fence installation job/project
/// </summary>
public class Job
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Job name or title
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Customer name
    /// </summary>
    public required string CustomerName { get; set; }

    /// <summary>
    /// Customer email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Customer phone
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Installation address
    /// </summary>
    public string? InstallationAddress { get; set; }

    /// <summary>
    /// Organization that owns this job
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>
    /// Job status
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Draft;

    /// <summary>
    /// Total linear feet of fence
    /// </summary>
    public decimal TotalLinearFeet { get; set; }

    /// <summary>
    /// Labor cost
    /// </summary>
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Total materials cost (calculated from line items)
    /// </summary>
    public decimal MaterialsCost { get; set; }

    /// <summary>
    /// Total job cost (materials + labor)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Additional notes about the job
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the job was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Estimated start date
    /// </summary>
    public DateTime? EstimatedStartDate { get; set; }

    /// <summary>
    /// Estimated completion date
    /// </summary>
    public DateTime? EstimatedCompletionDate { get; set; }

    /// <summary>
    /// Navigation property for the organization
    /// </summary>
    public Organization? Organization { get; set; }

    /// <summary>
    /// Navigation property for job line items
    /// </summary>
    public ICollection<JobLineItem> LineItems { get; set; } = [];
}

/// <summary>
/// Status of a job
/// </summary>
public enum JobStatus
{
    Draft,
    Quoted,
    Approved,
    InProgress,
    Completed,
    Cancelled
}
