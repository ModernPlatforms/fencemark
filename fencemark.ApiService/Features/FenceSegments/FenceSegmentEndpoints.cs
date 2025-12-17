using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.FenceSegments;

public static class FenceSegmentEndpoints
{
    public static IEndpointRouteBuilder MapFenceSegmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fence-segments")
            .WithTags("FenceSegments")
            .RequireAuthorization();

        group.MapGet("/", GetAllFenceSegments)
            .WithName("GetFenceSegments");

        group.MapGet("/by-job/{jobId}", GetFenceSegmentsByJob)
            .WithName("GetFenceSegmentsByJob");

        group.MapGet("/by-parcel/{parcelId}", GetFenceSegmentsByParcel)
            .WithName("GetFenceSegmentsByParcel");

        group.MapGet("/{id}", GetFenceSegmentById)
            .WithName("GetFenceSegmentById");

        group.MapPost("/", CreateFenceSegment)
            .WithName("CreateFenceSegment");

        group.MapPut("/{id}", UpdateFenceSegment)
            .WithName("UpdateFenceSegment");

        group.MapDelete("/{id}", DeleteFenceSegment)
            .WithName("DeleteFenceSegment");

        return app;
    }

    private static async Task<IResult> GetAllFenceSegments(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var segments = await db.FenceSegments
            .Include(f => f.FenceType)
            .Include(f => f.GatePositions)
            .Where(f => f.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(segments);
    }

    private static async Task<IResult> GetFenceSegmentsByJob(
        string jobId,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var segments = await db.FenceSegments
            .Include(f => f.FenceType)
            .Include(f => f.GatePositions)
            .Where(f => f.JobId == jobId && f.OrganizationId == currentUser.OrganizationId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
        return Results.Ok(segments);
    }

    private static async Task<IResult> GetFenceSegmentsByParcel(
        string parcelId,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var segments = await db.FenceSegments
            .Include(f => f.FenceType)
            .Include(f => f.GatePositions)
            .Where(f => f.ParcelId == parcelId && f.OrganizationId == currentUser.OrganizationId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
        return Results.Ok(segments);
    }

    private static async Task<IResult> GetFenceSegmentById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var segment = await db.FenceSegments
            .Include(f => f.FenceType)
            .Include(f => f.GatePositions)
            .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
        
        return segment != null ? Results.Ok(segment) : Results.NotFound();
    }

    private static async Task<IResult> CreateFenceSegment(
        FenceSegmentRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify job exists and belongs to the organization
        var job = await db.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId && j.OrganizationId == currentUser.OrganizationId, ct);
        
        if (job == null)
            return Results.BadRequest(new { error = "Job not found or access denied" });

        // Verify parcel exists if provided
        if (!string.IsNullOrEmpty(request.ParcelId))
        {
            var parcel = await db.Parcels
                .FirstOrDefaultAsync(p => p.Id == request.ParcelId && p.OrganizationId == currentUser.OrganizationId, ct);
            
            if (parcel == null)
                return Results.BadRequest(new { error = "Parcel not found or access denied" });
        }

        var segment = new FenceSegment
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = currentUser.OrganizationId ?? string.Empty,
            JobId = request.JobId,
            ParcelId = request.ParcelId,
            Name = request.Name,
            FenceTypeId = request.FenceTypeId,
            GeoJsonGeometry = request.GeoJsonGeometry,
            LengthInFeet = request.LengthInFeet,
            LengthInMeters = request.LengthInMeters,
            IsSnappedToBoundary = request.IsSnappedToBoundary,
            Notes = request.Notes,
            IsVerifiedOnsite = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.FenceSegments.Add(segment);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/fence-segments/{segment.Id}", segment);
    }

    private static async Task<IResult> UpdateFenceSegment(
        string id,
        FenceSegmentRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var segment = await db.FenceSegments
            .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
        
        if (segment == null)
            return Results.NotFound();

        segment.Name = request.Name;
        segment.FenceTypeId = request.FenceTypeId;
        segment.GeoJsonGeometry = request.GeoJsonGeometry;
        segment.LengthInFeet = request.LengthInFeet;
        segment.LengthInMeters = request.LengthInMeters;
        segment.IsSnappedToBoundary = request.IsSnappedToBoundary;
        segment.Notes = request.Notes;
        segment.IsVerifiedOnsite = request.IsVerifiedOnsite;
        segment.OnsiteVerifiedLengthInFeet = request.OnsiteVerifiedLengthInFeet;
        segment.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(segment);
    }

    private static async Task<IResult> DeleteFenceSegment(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var segment = await db.FenceSegments
            .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
        
        if (segment == null)
            return Results.NotFound();

        db.FenceSegments.Remove(segment);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }

    public record FenceSegmentRequest(
        string JobId,
        string? ParcelId,
        string? Name,
        string? FenceTypeId,
        string GeoJsonGeometry,
        decimal LengthInFeet,
        decimal LengthInMeters,
        bool IsSnappedToBoundary,
        string? Notes,
        bool IsVerifiedOnsite = false,
        decimal? OnsiteVerifiedLengthInFeet = null
    );
}
