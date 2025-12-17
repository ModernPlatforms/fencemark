using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Parcels;

public static class ParcelEndpoints
{
    public static IEndpointRouteBuilder MapParcelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parcels")
            .WithTags("Parcels")
            .RequireAuthorization();

        group.MapGet("/", GetAllParcels)
            .WithName("GetParcels");

        group.MapGet("/by-job/{jobId}", GetParcelsByJob)
            .WithName("GetParcelsByJob");

        group.MapGet("/{id}", GetParcelById)
            .WithName("GetParcelById");

        group.MapPost("/", CreateParcel)
            .WithName("CreateParcel");

        group.MapPut("/{id}", UpdateParcel)
            .WithName("UpdateParcel");

        group.MapDelete("/{id}", DeleteParcel)
            .WithName("DeleteParcel");

        return app;
    }

    private static async Task<IResult> GetAllParcels(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var parcels = await db.Parcels
            .Include(p => p.Drawings)
            .Where(p => p.OrganizationId == currentUser.OrganizationId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return Results.Ok(parcels);
    }

    private static async Task<IResult> GetParcelsByJob(
        string jobId,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var parcels = await db.Parcels
            .Include(p => p.Drawings)
            .Where(p => p.JobId == jobId && p.OrganizationId == currentUser.OrganizationId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return Results.Ok(parcels);
    }

    private static async Task<IResult> GetParcelById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var parcel = await db.Parcels
            .Include(p => p.Drawings)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == currentUser.OrganizationId, ct);
        
        return parcel != null ? Results.Ok(parcel) : Results.NotFound();
    }

    private static async Task<IResult> CreateParcel(
        Parcel request,
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

        request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
        request.Id = Guid.NewGuid().ToString();
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        db.Parcels.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/parcels/{request.Id}", request);
    }

    private static async Task<IResult> UpdateParcel(
        string id,
        Parcel request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var parcel = await db.Parcels
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == currentUser.OrganizationId, ct);
        
        if (parcel == null)
            return Results.NotFound();

        parcel.Name = request.Name;
        parcel.Address = request.Address;
        parcel.ParcelNumber = request.ParcelNumber;
        parcel.TotalArea = request.TotalArea;
        parcel.AreaUnit = request.AreaUnit;
        parcel.Coordinates = request.Coordinates;
        parcel.Notes = request.Notes;
        parcel.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(parcel);
    }

    private static async Task<IResult> DeleteParcel(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var parcel = await db.Parcels
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == currentUser.OrganizationId, ct);
        
        if (parcel == null)
            return Results.NotFound();

        db.Parcels.Remove(parcel);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
