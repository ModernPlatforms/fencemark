using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace fencemark.Tests;

/// <summary>
/// Unit tests for Job/Drawing workflow scenarios
/// Tests creation, modification, and management of jobs including fence drawing
/// </summary>
public class JobFlowTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Organization org, FenceType fenceType, GateType gateType)> SetupTestDataAsync(ApplicationDbContext context)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Fence Company"
        };
        context.Organizations.Add(org);

        var fenceType = new FenceType
        {
            Id = Guid.NewGuid().ToString(),
            Name = "1.8m Chain Link",
            OrganizationId = org.Id,
            HeightInMm = 1800,
            Material = "Chain Link",
            Style = "Standard",
            PricePerLinearMetre = 49.21m
        };
        context.FenceTypes.Add(fenceType);

        var gateType = new GateType
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Chain Link Gate",
            OrganizationId = org.Id,
            WidthInMm = 1200,
            HeightInMm = 1800,
            Material = "Chain Link",
            Style = "Walk-through",
            BasePrice = 250.00m
        };
        context.GateTypes.Add(gateType);

        await context.SaveChangesAsync();

        return (org, fenceType, gateType);
    }

    [Fact]
    public async Task CreateJob_WithValidData_CreatesJobSuccessfully()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Backyard Fence Installation",
            CustomerName = "John Smith",
            CustomerEmail = "john@example.com",
            CustomerPhone = "+1-555-0123",
            InstallationAddress = "123 Main St, Sydney NSW 2000",
            OrganizationId = org.Id,
            Status = JobStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Assert
        var savedJob = await context.Jobs.FindAsync(job.Id);
        Assert.NotNull(savedJob);
        Assert.Equal("Backyard Fence Installation", savedJob.Name);
        Assert.Equal("John Smith", savedJob.CustomerName);
        Assert.Equal(JobStatus.Draft, savedJob.Status);
        Assert.Equal(org.Id, savedJob.OrganizationId);
    }

    [Fact]
    public async Task CreateJob_WithLineItems_CreatesJobWithFenceComponents()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, fenceType, gateType) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Complete Perimeter Fence",
            CustomerName = "Jane Doe",
            OrganizationId = org.Id,
            TotalLinearMetres = 45.7m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);

        var lineItems = new[]
        {
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Fence,
                FenceTypeId = fenceType.Id,
                Description = "North side - 1.8m Chain Link",
                Quantity = 30.5m,
                UnitPrice = 49.21m,
                TotalPrice = 1501.41m
            },
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Fence,
                FenceTypeId = fenceType.Id,
                Description = "East side - 1.8m Chain Link",
                Quantity = 15.2m,
                UnitPrice = 49.21m,
                TotalPrice = 748.19m
            },
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Gate,
                GateTypeId = gateType.Id,
                Description = "Front entrance gate",
                Quantity = 1.0m,
                UnitPrice = 250.00m,
                TotalPrice = 250.00m
            }
        };
        context.JobLineItems.AddRange(lineItems);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var savedJob = await context.Jobs
            .Include(j => j.LineItems)
            .FirstOrDefaultAsync(j => j.Id == job.Id);

        Assert.NotNull(savedJob);
        Assert.Equal(3, savedJob.LineItems.Count);
        Assert.Equal(2, savedJob.LineItems.Count(li => li.ItemType == LineItemType.Fence));
        Assert.Single(savedJob.LineItems.Where(li => li.ItemType == LineItemType.Gate));
        Assert.Equal(45.7m, savedJob.TotalLinearMetres);
    }

    [Fact]
    public async Task UpdateJob_ChangesStatus_UpdatesSuccessfully()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            Status = JobStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        job.Status = JobStatus.InProgress;
        job.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var updatedJob = await context.Jobs.FindAsync(job.Id);
        Assert.NotNull(updatedJob);
        Assert.Equal(JobStatus.InProgress, updatedJob.Status);
    }

    [Fact]
    public async Task DeleteJob_RemovesJobSuccessfully()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Job to Delete",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        context.Jobs.Remove(job);
        await context.SaveChangesAsync();

        // Assert
        var deletedJob = await context.Jobs.FindAsync(job.Id);
        Assert.Null(deletedJob);
    }

    [Fact]
    public async Task GetJobsByOrganization_ReturnsOnlyOrganizationJobs()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org1, _, _) = await SetupTestDataAsync(context);

        var org2 = new Organization
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Another Fence Company"
        };
        context.Organizations.Add(org2);
        await context.SaveChangesAsync();

        var job1 = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Org1 Job",
            CustomerName = "Customer 1",
            OrganizationId = org1.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var job2 = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Org2 Job",
            CustomerName = "Customer 2",
            OrganizationId = org2.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.AddRange(job1, job2);
        await context.SaveChangesAsync();

        // Act
        var org1Jobs = await context.Jobs
            .Where(j => j.OrganizationId == org1.Id)
            .ToListAsync();

        // Assert
        Assert.Single(org1Jobs);
        Assert.Equal("Org1 Job", org1Jobs[0].Name);
    }

    [Fact]
    public async Task Job_CalculatesTotalCost_BasedOnLineItems()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, fenceType, gateType) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Cost Calculation Test",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            TotalLinearMetres = 24.4m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);

        var lineItems = new[]
        {
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Fence,
                FenceTypeId = fenceType.Id,
                Description = "Chain Link Fence",
                Quantity = 24.4m,
                UnitPrice = 49.21m,
                TotalPrice = 1200.72m
            },
            new JobLineItem
            {
                Id = Guid.NewGuid().ToString(),
                JobId = job.Id,
                ItemType = LineItemType.Gate,
                GateTypeId = gateType.Id,
                Description = "Gate",
                Quantity = 1.0m,
                UnitPrice = 250.00m,
                TotalPrice = 250.00m
            }
        };
        context.JobLineItems.AddRange(lineItems);
        await context.SaveChangesAsync();

        // Act
        var totalLineItemCost = await context.JobLineItems
            .Where(li => li.JobId == job.Id)
            .SumAsync(li => li.TotalPrice);

        job.MaterialsCost = totalLineItemCost;
        job.LaborCost = 500.00m;
        job.TotalCost = job.MaterialsCost + job.LaborCost;
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(1450.72m, job.MaterialsCost);
        Assert.Equal(500.00m, job.LaborCost);
        Assert.Equal(1950.72m, job.TotalCost);
    }

    [Fact]
    public async Task Job_WithNotes_StoresAndRetrievesNotes()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Job with Notes",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            Notes = "Customer prefers black chain link. Install next week if weather permits.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        var retrievedJob = await context.Jobs.FindAsync(job.Id);

        // Assert
        Assert.NotNull(retrievedJob);
        Assert.NotNull(retrievedJob.Notes);
        Assert.Contains("black chain link", retrievedJob.Notes);
    }

    [Fact]
    public async Task Job_ProgressThroughStatuses_Validates()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, _, _) = await SetupTestDataAsync(context);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Status Progression Test",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            Status = JobStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act & Assert - Progress through statuses
        Assert.Equal(JobStatus.Draft, job.Status);

        job.Status = JobStatus.InProgress;
        await context.SaveChangesAsync();
        Assert.Equal(JobStatus.InProgress, job.Status);

        job.Status = JobStatus.Completed;
        await context.SaveChangesAsync();
        Assert.Equal(JobStatus.Completed, job.Status);
    }

    [Fact]
    public async Task Job_WithEstimatedDates_StoresCorrectly()
    {
        // Arrange
        await using var context = CreateDbContext();
        var (org, _, _) = await SetupTestDataAsync(context);

        var startDate = DateTime.UtcNow.AddDays(7);
        var completionDate = DateTime.UtcNow.AddDays(14);

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Scheduled Job",
            CustomerName = "Test Customer",
            OrganizationId = org.Id,
            EstimatedStartDate = startDate,
            EstimatedCompletionDate = completionDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        var retrievedJob = await context.Jobs.FindAsync(job.Id);

        // Assert
        Assert.NotNull(retrievedJob);
        Assert.NotNull(retrievedJob.EstimatedStartDate);
        Assert.NotNull(retrievedJob.EstimatedCompletionDate);
        Assert.True(retrievedJob.EstimatedCompletionDate > retrievedJob.EstimatedStartDate);
    }
}
