using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Pricing;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pricing-configs")
            .WithTags("Pricing")
            .RequireAuthorization();

        group.MapGet("/", GetAllPricingConfigs)
            .WithName("GetPricingConfigs");

        group.MapGet("/{id}", GetPricingConfigById)
            .WithName("GetPricingConfigById");

        group.MapPost("/", CreatePricingConfig)
            .WithName("CreatePricingConfig");

        group.MapPut("/{id}", UpdatePricingConfig)
            .WithName("UpdatePricingConfig");

        group.MapDelete("/{id}", DeletePricingConfig)
            .WithName("DeletePricingConfig");

        return app;
    }

    private static async Task<IResult> GetAllPricingConfigs(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var configs = await db.PricingConfigs
            .Include(p => p.HeightTiers)
            .Where(p => p.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);
        return Results.Ok(configs);
    }

    private static async Task<IResult> GetPricingConfigById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var config = await db.PricingConfigs
            .Include(p => p.HeightTiers)
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == currentUser.OrganizationId, ct);
        
        return config != null ? Results.Ok(config) : Results.NotFound();
    }

    private static async Task<IResult> CreatePricingConfig(
        PricingConfig request,
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
            var existingDefaults = await db.PricingConfigs
                .Where(p => p.OrganizationId == currentUser.OrganizationId && p.IsDefault)
                .ToListAsync(ct);
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        db.PricingConfigs.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/pricing-configs/{request.Id}", request);
    }

    private static async Task<IResult> UpdatePricingConfig(
        string id,
        PricingConfig request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var config = await db.PricingConfigs
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == currentUser.OrganizationId, ct);
        
        if (config == null)
            return Results.NotFound();

        // If setting this as default, unset other defaults
        if (request.IsDefault && !config.IsDefault)
        {
            var existingDefaults = await db.PricingConfigs
                .Where(p => p.OrganizationId == currentUser.OrganizationId && p.IsDefault && p.Id != id)
                .ToListAsync(ct);
            
            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
            }
        }

        config.Name = request.Name;
        config.Description = request.Description;
        config.LaborRatePerHour = request.LaborRatePerHour;
        config.HoursPerLinearMeter = request.HoursPerLinearMeter;
        config.ContingencyPercentage = request.ContingencyPercentage;
        config.ProfitMarginPercentage = request.ProfitMarginPercentage;
        config.IsDefault = request.IsDefault;
        config.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(config);
    }

    private static async Task<IResult> DeletePricingConfig(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var config = await db.PricingConfigs
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == currentUser.OrganizationId, ct);
        
        if (config == null)
            return Results.NotFound();

        db.PricingConfigs.Remove(config);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }
}
