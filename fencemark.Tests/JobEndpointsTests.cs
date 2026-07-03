using System.Text.Json;
using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Features.Jobs;
using fencemark.ApiService.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace fencemark.Tests;

/// <summary>
/// Regression tests for the "Domain Model Used as API Contract" over-posting issue:
/// JobEndpoints used to bind the Job domain model directly from the request body, so a
/// client could set Id, OrganizationId, CreatedAt (or inject data via the Organization/
/// LineItems navigation properties) in the JSON payload. CreateJob/UpdateJob now bind a
/// dedicated JobRequest DTO that only exposes the fields the UI actually edits.
/// </summary>
public class JobEndpointsTests
{
    private class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; } = "user-1";
        public string? Email { get; set; } = "user@test.com";
        public string? OrganizationId { get; set; }
        public string? Role { get; set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateJob_OverPostedIdOrganizationIdAndCreatedAt_AreIgnored()
    {
        // Simulate a malicious/buggy client posting fields that are not part of the JobRequest
        // contract. System.Text.Json silently ignores unknown properties by default, which is
        // exactly the behavior we're relying on to close the over-posting hole.
        var maliciousJson = """
        {
            "id": "attacker-controlled-id",
            "organizationId": "someone-elses-org",
            "createdAt": "2000-01-01T00:00:00Z",
            "name": "Test Job",
            "customerName": "Test Customer"
        }
        """;

        var request = JsonSerializer.Deserialize<JobEndpoints.JobRequest>(
            maliciousJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        await using var db = CreateDbContext();
        var currentUser = new TestCurrentUserService { OrganizationId = "real-org-id" };
        var before = DateTime.UtcNow;

        var result = await JobEndpoints.CreateJob(request, db, currentUser, CancellationToken.None);

        Assert.Equal(201, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var createdJob = Assert.Single(db.Jobs);
        Assert.NotEqual("attacker-controlled-id", createdJob.Id);
        Assert.Equal("real-org-id", createdJob.OrganizationId);
        Assert.True(createdJob.CreatedAt >= before, "CreatedAt should be server-generated, not the client-supplied value.");
        Assert.Equal("Test Job", createdJob.Name);
        Assert.Equal("Test Customer", createdJob.CustomerName);
    }

    [Fact]
    public async Task CreateJob_WithValidRequest_CreatesJobScopedToCallerOrganization()
    {
        await using var db = CreateDbContext();
        var currentUser = new TestCurrentUserService { OrganizationId = "org-1" };
        var request = new JobEndpoints.JobRequest(
            Name: "Backyard Fence",
            CustomerName: "Jane Doe",
            CustomerEmail: "jane@example.com",
            CustomerPhone: null,
            InstallationAddress: "1 Test St");

        var result = await JobEndpoints.CreateJob(request, db, currentUser, CancellationToken.None);

        Assert.Equal(201, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var createdJob = Assert.Single(db.Jobs);
        Assert.Equal("org-1", createdJob.OrganizationId);
        Assert.False(string.IsNullOrEmpty(createdJob.Id));
        Assert.Equal(JobStatus.Draft, createdJob.Status);
    }

    [Fact]
    public async Task UpdateJob_DoesNotAllowChangingOrganizationId()
    {
        await using var db = CreateDbContext();
        var job = new Job
        {
            Id = "job-1",
            Name = "Original",
            CustomerName = "Original Customer",
            OrganizationId = "org-1",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUserService { OrganizationId = "org-1" };
        // JobRequest has no OrganizationId member at all, so there is no way for the
        // request body to influence tenant assignment on update.
        var request = new JobEndpoints.JobRequest(
            Name: "Updated Name",
            CustomerName: "Updated Customer",
            CustomerEmail: null,
            CustomerPhone: null,
            InstallationAddress: null);

        var result = await JobEndpoints.UpdateJob("job-1", request, db, currentUser, CancellationToken.None);

        Assert.Equal(200, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        var updatedJob = await db.Jobs.SingleAsync(j => j.Id == "job-1");
        Assert.Equal("org-1", updatedJob.OrganizationId);
        Assert.Equal("Updated Name", updatedJob.Name);
        Assert.Equal("Updated Customer", updatedJob.CustomerName);
    }
}
