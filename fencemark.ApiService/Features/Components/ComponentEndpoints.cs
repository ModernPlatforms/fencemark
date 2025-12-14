using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Components;

public static class ComponentEndpoints
{
    public static IEndpointRouteBuilder MapComponentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/components")
            .WithTags("Components")
            .RequireAuthorization();

        group.MapGet("/", GetAllComponents)
            .WithName("GetComponents");

        group.MapGet("/{id}", GetComponentById)
            .WithName("GetComponentById");

        group.MapPost("/", CreateComponent)
            .WithName("CreateComponent");

        group.MapPut("/{id}", UpdateComponent)
            .WithName("UpdateComponent");

        group.MapDelete("/{id}", DeleteComponent)
            .WithName("DeleteComponent");

        return app;
    }

    private static async Task<IResult> GetAllComponents(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var components = await db.Components
            .Where(c => c.OrganizationId == currentUser.OrganizationId)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);
        return Results.Ok(components);
    }

    private static async Task<IResult> GetComponentById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var component = await db.Components
            .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == currentUser.OrganizationId, ct);
        
        return component != null ? Results.Ok(component) : Results.NotFound();
    }

    private static async Task<IResult> CreateComponent(
        Component request,
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

        db.Components.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/components/{request.Id}", request);
    }

    private static async Task<IResult> UpdateComponent(
        string id,
        Component request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var component = await db.Components.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == currentUser.OrganizationId, ct);
        if (component == null)
            return Results.NotFound();

        component.Name = request.Name;
        component.Description = request.Description;
        component.Sku = request.Sku;
        component.Category = request.Category;
        component.UnitOfMeasure = request.UnitOfMeasure;
        component.UnitPrice = request.UnitPrice;
        component.Material = request.Material;
        component.Dimensions = request.Dimensions;
        component.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(component);
    }

    private static async Task<IResult> DeleteComponent(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var component = await db.Components.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == currentUser.OrganizationId, ct);
        if (component == null)
            return Results.NotFound();

        db.Components.Remove(component);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
