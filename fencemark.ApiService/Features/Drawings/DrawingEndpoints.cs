using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Drawings;

public static class DrawingEndpoints
{
    public static IEndpointRouteBuilder MapDrawingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drawings")
            .WithTags("Drawings")
            .RequireAuthorization();

        group.MapGet("/", GetAllDrawings)
            .WithName("GetDrawings");

        group.MapGet("/by-job/{jobId}", GetDrawingsByJob)
            .WithName("GetDrawingsByJob");

        group.MapGet("/by-parcel/{parcelId}", GetDrawingsByParcel)
            .WithName("GetDrawingsByParcel");

        group.MapGet("/{id}", GetDrawingById)
            .WithName("GetDrawingById");

        group.MapPost("/", CreateDrawing)
            .WithName("CreateDrawing");

        group.MapPut("/{id}", UpdateDrawing)
            .WithName("UpdateDrawing");

        group.MapDelete("/{id}", DeleteDrawing)
            .WithName("DeleteDrawing");

        return app;
    }

    private static async Task<IResult> GetAllDrawings(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var drawings = await db.Drawings
            .Where(d => d.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(drawings);
    }

    private static async Task<IResult> GetDrawingsByJob(
        string jobId,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var drawings = await db.Drawings
            .Where(d => d.JobId == jobId && d.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(drawings);
    }

    private static async Task<IResult> GetDrawingsByParcel(
        string parcelId,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var drawings = await db.Drawings
            .Where(d => d.ParcelId == parcelId && d.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return Results.Ok(drawings);
    }

    private static async Task<IResult> GetDrawingById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var drawing = await db.Drawings
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == currentUser.OrganizationId, ct);
        
        return drawing != null ? Results.Ok(drawing) : Results.NotFound();
    }

    private static async Task<IResult> CreateDrawing(
        DrawingRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Verify job exists if provided
        if (!string.IsNullOrEmpty(request.JobId))
        {
            var job = await db.Jobs
                .FirstOrDefaultAsync(j => j.Id == request.JobId && j.OrganizationId == currentUser.OrganizationId, ct);
            
            if (job == null)
                return Results.BadRequest(new { error = "Job not found or access denied" });
        }

        // Verify parcel exists if provided
        if (!string.IsNullOrEmpty(request.ParcelId))
        {
            var parcel = await db.Parcels
                .FirstOrDefaultAsync(p => p.Id == request.ParcelId && p.OrganizationId == currentUser.OrganizationId, ct);
            
            if (parcel == null)
                return Results.BadRequest(new { error = "Parcel not found or access denied" });
        }

        var drawing = new Drawing
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = currentUser.OrganizationId ?? string.Empty,
            JobId = request.JobId,
            ParcelId = request.ParcelId,
            Name = request.Name,
            Description = request.Description,
            DrawingType = request.DrawingType,
            FileName = request.FileName,
            FilePath = request.FilePath,
            MimeType = request.MimeType,
            FileSize = request.FileSize,
            Version = request.Version,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Drawings.Add(drawing);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/drawings/{drawing.Id}", drawing);
    }

    private static async Task<IResult> UpdateDrawing(
        string id,
        DrawingRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var drawing = await db.Drawings
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == currentUser.OrganizationId, ct);
        
        if (drawing == null)
            return Results.NotFound();

        drawing.Name = request.Name;
        drawing.Description = request.Description;
        drawing.DrawingType = request.DrawingType;
        drawing.Version = request.Version;
        drawing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(drawing);
    }

    private static async Task<IResult> DeleteDrawing(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var drawing = await db.Drawings
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == currentUser.OrganizationId, ct);
        
        if (drawing == null)
            return Results.NotFound();

        // TODO: Delete physical file from storage
        // await _fileStorageService.DeleteFileAsync(drawing.FilePath);

        db.Drawings.Remove(drawing);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }

    public record DrawingRequest(
        string? JobId,
        string? ParcelId,
        string Name,
        string? Description,
        string? DrawingType,
        string FileName,
        string FilePath,
        string? MimeType,
        long FileSize,
        int Version = 1
    );
}
