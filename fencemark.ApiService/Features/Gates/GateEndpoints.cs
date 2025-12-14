using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Gates;

public static class GateEndpoints
{
    public static IEndpointRouteBuilder MapGateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gates")
            .WithTags("Gates")
            .RequireAuthorization();

        group.MapGet("/", GetAllGates)
            .WithName("GetGateTypes");

        group.MapGet("/{id}", GetGateById)
            .WithName("GetGateTypeById");

        group.MapPost("/", CreateGate)
            .WithName("CreateGateType");

        group.MapPut("/{id}", UpdateGate)
            .WithName("UpdateGateType");

        group.MapDelete("/{id}", DeleteGate)
            .WithName("DeleteGateType");

        return app;
    }

    private static async Task<IResult> GetAllGates(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var gates = await db.GateTypes
            .Where(g => g.OrganizationId == currentUser.OrganizationId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
        return Results.Ok(gates);
    }

    private static async Task<IResult> GetGateById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var gate = await db.GateTypes
            .Include(g => g.Components)
                .ThenInclude(gc => gc.Component)
            .FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
        
        return gate != null ? Results.Ok(gate) : Results.NotFound();
    }

    private static async Task<IResult> CreateGate(
        GateType request,
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

        db.GateTypes.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/gates/{request.Id}", request);
    }

    private static async Task<IResult> UpdateGate(
        string id,
        GateType request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var gate = await db.GateTypes.FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
        if (gate == null)
            return Results.NotFound();

        gate.Name = request.Name;
        gate.Description = request.Description;
        gate.WidthInFeet = request.WidthInFeet;
        gate.HeightInFeet = request.HeightInFeet;
        gate.Material = request.Material;
        gate.Style = request.Style;
        gate.BasePrice = request.BasePrice;
        gate.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(gate);
    }

    private static async Task<IResult> DeleteGate(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var gate = await db.GateTypes.FirstOrDefaultAsync(g => g.Id == id && g.OrganizationId == currentUser.OrganizationId, ct);
        if (gate == null)
            return Results.NotFound();

        db.GateTypes.Remove(gate);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
