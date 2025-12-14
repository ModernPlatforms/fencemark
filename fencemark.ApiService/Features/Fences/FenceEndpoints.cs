using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Fences;

public static class FenceEndpoints
{
    public static IEndpointRouteBuilder MapFenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fences")
            .WithTags("Fences")
            .RequireAuthorization();

        group.MapGet("/", GetAllFences)
            .WithName("GetFenceTypes");

        group.MapGet("/{id}", GetFenceById)
            .WithName("GetFenceTypeById");

        group.MapPost("/", CreateFence)
            .WithName("CreateFenceType");

        group.MapPut("/{id}", UpdateFence)
            .WithName("UpdateFenceType");

        group.MapDelete("/{id}", DeleteFence)
            .WithName("DeleteFenceType");

        return app;
    }

    private static async Task<IResult> GetAllFences(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var fences = await db.FenceTypes
            .Where(f => f.OrganizationId == currentUser.OrganizationId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
        return Results.Ok(fences);
    }

    private static async Task<IResult> GetFenceById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var fence = await db.FenceTypes
            .Include(f => f.Components)
                .ThenInclude(fc => fc.Component)
            .FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
        
        return fence != null ? Results.Ok(fence) : Results.NotFound();
    }

    private static async Task<IResult> CreateFence(
        FenceType request,
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

        db.FenceTypes.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/fences/{request.Id}", request);
    }

    private static async Task<IResult> UpdateFence(
        string id,
        FenceType request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var fence = await db.FenceTypes.FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
        if (fence == null)
            return Results.NotFound();

        fence.Name = request.Name;
        fence.Description = request.Description;
        fence.HeightInFeet = request.HeightInFeet;
        fence.Material = request.Material;
        fence.Style = request.Style;
        fence.PricePerLinearFoot = request.PricePerLinearFoot;
        fence.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(fence);
    }

    private static async Task<IResult> DeleteFence(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var fence = await db.FenceTypes.FirstOrDefaultAsync(f => f.Id == id && f.OrganizationId == currentUser.OrganizationId, ct);
        if (fence == null)
            return Results.NotFound();

        db.FenceTypes.Remove(fence);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
