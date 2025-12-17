using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Middleware;
using Microsoft.EntityFrameworkCore;

namespace fencemark.ApiService.Features.Discounts;

public static class DiscountEndpoints
{
    public static IEndpointRouteBuilder MapDiscountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/discounts")
            .WithTags("Discounts")
            .RequireAuthorization();

        group.MapGet("/", GetAllDiscounts)
            .WithName("GetDiscounts");

        group.MapGet("/{id}", GetDiscountById)
            .WithName("GetDiscountById");

        group.MapPost("/", CreateDiscount)
            .WithName("CreateDiscount");

        group.MapPut("/{id}", UpdateDiscount)
            .WithName("UpdateDiscount");

        group.MapDelete("/{id}", DeleteDiscount)
            .WithName("DeleteDiscount");

        group.MapPost("/validate-promo", ValidatePromoCode)
            .WithName("ValidatePromoCode");

        return app;
    }

    private static async Task<IResult> GetAllDiscounts(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var discounts = await db.DiscountRules
            .Where(d => d.OrganizationId == currentUser.OrganizationId)
            .OrderByDescending(d => d.IsActive)
            .ThenBy(d => d.Name)
            .ToListAsync(ct);
        return Results.Ok(discounts);
    }

    private static async Task<IResult> GetDiscountById(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var discount = await db.DiscountRules
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == currentUser.OrganizationId, ct);
        
        return discount != null ? Results.Ok(discount) : Results.NotFound();
    }

    private static async Task<IResult> CreateDiscount(
        DiscountRule request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Validate promo code uniqueness if provided
        if (!string.IsNullOrEmpty(request.PromoCode))
        {
            var existingPromo = await db.DiscountRules
                .AnyAsync(d => d.OrganizationId == currentUser.OrganizationId 
                    && d.PromoCode == request.PromoCode, ct);
            
            if (existingPromo)
                return Results.BadRequest(new { error = "Promo code already exists" });
        }

        request.OrganizationId = currentUser.OrganizationId ?? string.Empty;
        request.Id = Guid.NewGuid().ToString();
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        db.DiscountRules.Add(request);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/discounts/{request.Id}", request);
    }

    private static async Task<IResult> UpdateDiscount(
        string id,
        DiscountRule request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var discount = await db.DiscountRules
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == currentUser.OrganizationId, ct);
        
        if (discount == null)
            return Results.NotFound();

        // Validate promo code uniqueness if changed
        if (!string.IsNullOrEmpty(request.PromoCode) && request.PromoCode != discount.PromoCode)
        {
            var existingPromo = await db.DiscountRules
                .AnyAsync(d => d.OrganizationId == currentUser.OrganizationId 
                    && d.PromoCode == request.PromoCode 
                    && d.Id != id, ct);
            
            if (existingPromo)
                return Results.BadRequest(new { error = "Promo code already exists" });
        }

        discount.Name = request.Name;
        discount.Description = request.Description;
        discount.DiscountType = request.DiscountType;
        discount.DiscountValue = request.DiscountValue;
        discount.MinimumOrderValue = request.MinimumOrderValue;
        discount.MinimumLinearFeet = request.MinimumLinearFeet;
        discount.ValidFrom = request.ValidFrom;
        discount.ValidUntil = request.ValidUntil;
        discount.IsActive = request.IsActive;
        discount.PromoCode = request.PromoCode;
        discount.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(discount);
    }

    private static async Task<IResult> DeleteDiscount(
        string id,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var discount = await db.DiscountRules
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == currentUser.OrganizationId, ct);
        
        if (discount == null)
            return Results.NotFound();

        db.DiscountRules.Remove(discount);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { success = true });
    }

    private static async Task<IResult> ValidatePromoCode(
        PromoCodeRequest request,
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var discount = await db.DiscountRules
            .FirstOrDefaultAsync(d => d.OrganizationId == currentUser.OrganizationId 
                && d.PromoCode == request.PromoCode 
                && d.IsActive, ct);
        
        if (discount == null)
            return Results.BadRequest(new { error = "Invalid promo code" });

        // Check date validity
        var now = DateTime.UtcNow;
        if (discount.ValidFrom.HasValue && now < discount.ValidFrom.Value)
            return Results.BadRequest(new { error = "Promo code is not yet active" });

        if (discount.ValidUntil.HasValue && now > discount.ValidUntil.Value)
            return Results.BadRequest(new { error = "Promo code has expired" });

        // Check minimum requirements if provided in request
        if (discount.MinimumOrderValue.HasValue && request.OrderValue.GetValueOrDefault() < discount.MinimumOrderValue.Value)
            return Results.BadRequest(new { error = $"Minimum order value of ${discount.MinimumOrderValue.Value:F2} required" });

        if (discount.MinimumLinearFeet.HasValue && request.LinearFeet.GetValueOrDefault() < discount.MinimumLinearFeet.Value)
            return Results.BadRequest(new { error = $"Minimum {discount.MinimumLinearFeet.Value:F2} linear feet required" });

        return Results.Ok(discount);
    }

    public record PromoCodeRequest(string PromoCode, decimal? OrderValue = null, decimal? LinearFeet = null);
}
