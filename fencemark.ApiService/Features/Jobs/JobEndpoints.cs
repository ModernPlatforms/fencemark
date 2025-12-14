using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Jobs;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs")
            .RequireAuthorization();

        group.MapGet("/", GetAllJobs)
            .WithName("GetJobs");

        group.MapGet("/{id}", GetJobById)
            .WithName("GetJobById");

        group.MapPost("/", CreateJob)
            .WithName("CreateJob");

        group.MapPut("/{id}", UpdateJob)
            .WithName("UpdateJob");

        group.MapDelete("/{id}", DeleteJob)
            .WithName("DeleteJob");

        return app;
    }

    private static async Task<IResult> GetAllJobs(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var jobs = await db.Jobs
            .Where(j => j.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(jobs);
    }

    private static async Task<IResult> GetJobById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var job = await db.Jobs
            .Include(j => j.LineItems)
                .ThenInclude(li => li.FenceType)
            .Include(j => j.LineItems)
                .ThenInclude(li => li.GateType)
            .FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId, ct);
        
        return job != null ? Results.Ok(job) : Results.NotFound();
    }

    private static async Task<IResult> CreateJob(
        Job request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
        request.Id = Guid.NewGuid().ToString();
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        db.Jobs.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/jobs/{request.Id}", request);
    }

    private static async Task<IResult> UpdateJob(
        string id,
        Job request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId, ct);
        if (job == null)
            return Results.NotFound();

        job.Name = request.Name;
        job.CustomerName = request.CustomerName;
        job.CustomerEmail = request.CustomerEmail;
        job.CustomerPhone = request.CustomerPhone;
        job.InstallationAddress = request.InstallationAddress;
        job.Status = request.Status;
        job.TotalLinearFeet = request.TotalLinearFeet;
        job.LaborCost = request.LaborCost;
        job.MaterialsCost = request.MaterialsCost;
        job.TotalCost = request.TotalCost;
        job.Notes = request.Notes;
        job.EstimatedStartDate = request.EstimatedStartDate;
        job.EstimatedCompletionDate = request.EstimatedCompletionDate;
        job.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(job);
    }

    private static async Task<IResult> DeleteJob(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.OrganizationId == currentUser.OrganizationId, ct);
        if (job == null)
            return Results.NotFound();

        db.Jobs.Remove(job);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
