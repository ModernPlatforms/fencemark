using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.GatePositions;

public static class GatePositionEndpoints
{
    public static IEndpointRouteBuilder MapGatePositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gate-positions")
            .WithTags("GatePositions")
            .RequireAuthorization();

        group.MapGet("/", GetAllGatePositions)
            .WithName("GetGatePositions");

        group.MapGet("/by-segment/{segmentId}", GetGatePositionsBySegment)
            .WithName("GetGatePositionsBySegment");

        group.MapGet("/{id}", GetGatePositionById)
            .WithName("GetGatePositionById");

        group.MapPost("/", CreateGatePosition)
            .WithName("CreateGatePosition");

        group.MapPut("/{id}", UpdateGatePosition)
            .WithName("UpdateGatePosition");

        group.MapDelete("/{id}", DeleteGatePosition)
            .WithName("DeleteGatePosition");

        return app;
    }

    private static async Task<IResult> GetAllGatePositions(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var positions = await db.GatePositions
            .Include(g => g.GateType)
            .Include(g => g.FenceSegment)
            .Where(g => g.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(positions);
    }

    private static async Task<IResult> GetGatePositionsBySegment(
        string segmentId,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var positions = await db.GatePositions
            .Include(g => g.GateType)
            .Where(g => g.FenceSegmentId == segmentId && g.OrganizationId == currentUser.OrganizationId)
            .OrderBy(g => g.PositionAlongSegment)
            .ToListAsync(ct);
        return Results.Ok(positions);
    }

    private static async Task<IResult> GetGatePositionById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var position = await db.GatePositions
            .Include(g => g.GateType)
            .Include(g => g.FenceSegment)
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
        
        return position != null ? Results.Ok(position) : Results.NotFound();
    }

    private static async Task<IResult> CreateGatePosition(
        GatePositionRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify fence segment exists and belongs to the organization
        var segment = await db.FenceSegments
            .FirstOrDefaultAsync(f => f.Id == request.FenceSegmentId && f.OrganizationId == currentUser.OrganizationId, ct);
        
        if (segment == null)
            return Results.BadRequest(new { error = "Fence segment not found or access denied" });

        var position = new GatePosition
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = currentUser.OrganizationId ?? string.Empty,
            FenceSegmentId = request.FenceSegmentId,
            GateTypeId = request.GateTypeId,
            Name = request.Name,
            GeoJsonLocation = request.GeoJsonLocation,
            PositionAlongSegment = request.PositionAlongSegment,
            Notes = request.Notes,
            IsVerifiedOnsite = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.GatePositions.Add(position);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/gate-positions/{position.Id}", position);
    }

    private static async Task<IResult> UpdateGatePosition(
        string id,
        GatePositionRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var position = await db.GatePositions
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
        
        if (position == null)
            return Results.NotFound();

        position.GateTypeId = request.GateTypeId;
        position.Name = request.Name;
        position.GeoJsonLocation = request.GeoJsonLocation;
        position.PositionAlongSegment = request.PositionAlongSegment;
        position.Notes = request.Notes;
        position.IsVerifiedOnsite = request.IsVerifiedOnsite;
        position.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(position);
    }

    private static async Task<IResult> DeleteGatePosition(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var position = await db.GatePositions
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
        
        if (position == null)
            return Results.NotFound();

        db.GatePositions.Remove(position);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }

    public record GatePositionRequest(
        string FenceSegmentId,
        string? GateTypeId,
        string? Name,
        string GeoJsonLocation,
        decimal PositionAlongSegment,
        string? Notes,
        bool IsVerifiedOnsite = false
    );
}
