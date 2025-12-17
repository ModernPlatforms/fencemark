using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.TaxRegions;

public static class TaxRegionEndpoints
{
    public static IEndpointRouteBuilder MapTaxRegionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tax-regions")
            .WithTags("Tax Regions")
            .RequireAuthorization();

        group.MapGet("/", GetAllTaxRegions)
            .WithName("GetTaxRegions");

        group.MapGet("/{id}", GetTaxRegionById)
            .WithName("GetTaxRegionById");

        group.MapPost("/", CreateTaxRegion)
            .WithName("CreateTaxRegion");

        group.MapPut("/{id}", UpdateTaxRegion)
            .WithName("UpdateTaxRegion");

        group.MapDelete("/{id}", DeleteTaxRegion)
            .WithName("DeleteTaxRegion");

        return app;
    }

    private static async Task<IResult> GetAllTaxRegions(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var regions = await db.TaxRegions
            .Where(t => t.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
        return Results.Ok(regions);
    }

    private static async Task<IResult> GetTaxRegionById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var region = await db.TaxRegions
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == currentUser.OrganizationId, ct);
        
        return region != null ? Results.Ok(region) : Results.NotFound();
    }

    private static async Task<IResult> CreateTaxRegion(
        TaxRegion request,
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

        // If this is set as default, unset other defaults
        if (request.IsDefault)
        {
            var existingDefaults = await db.TaxRegions
                .Where(t => t.OrganizationId == currentUser.OrganizationId && t.IsDefault)
                .ToListAsync(ct);
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        db.TaxRegions.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/tax-regions/{request.Id}", request);
    }

    private static async Task<IResult> UpdateTaxRegion(
        string id,
        TaxRegion request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var region = await db.TaxRegions
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == currentUser.OrganizationId, ct);
        
        if (region == null)
            return Results.NotFound();

        // If setting this as default, unset other defaults
        if (request.IsDefault && !region.IsDefault)
        {
            var existingDefaults = await db.TaxRegions
                .Where(t => t.OrganizationId == currentUser.OrganizationId && t.IsDefault && t.Id != id)
                .ToListAsync(ct);
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        region.Name = request.Name;
        region.Code = request.Code;
        region.TaxRate = request.TaxRate;
        region.Description = request.Description;
        region.IsDefault = request.IsDefault;
        region.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(region);
    }

    private static async Task<IResult> DeleteTaxRegion(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var region = await db.TaxRegions
            .FirstOrDefaultAsync(t => t.Id == id && t.OrganizationId == currentUser.OrganizationId, ct);
        
        if (region == null)
            return Results.NotFound();

        db.TaxRegions.Remove(region);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
