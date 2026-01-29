using fencemark.ApiService.Middleware;
using fencemark.ApiService.Services;

namespace fencemark.ApiService.Features.Mapping;

public static class MappingEndpoints
{
    public static IEndpointRouteBuilder MapMappingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mapping")
            .WithTags("Mapping")
            .RequireAuthorization();

        group.MapGet("/parcel-boundary", GetParcelByCoordinate)
            .WithName("GetParcelBoundary")
            .WithDescription("Get parcel boundary from state cadastral API by coordinates");

        group.MapGet("/parcel-by-address", GetParcelByAddress)
            .WithName("GetParcelByAddress")
            .WithDescription("Get parcel boundary from state cadastral API by address");

        group.MapGet("/state-from-coordinate", GetStateFromCoordinate)
            .WithName("GetStateFromCoordinate")
            .WithDescription("Determine which Australian state a coordinate is in");

        group.MapGet("/enabled-states", GetEnabledStates)
            .WithName("GetEnabledStates")
            .WithDescription("Get list of states with enabled cadastral APIs");

        return app;
    }

    private static async Task<IResult> GetParcelByCoordinate(
        double lat,
        double lng,
        ICadastralService cadastralService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Validate coordinates are within reasonable bounds for Australia
        if (lat < -45 || lat > -10 || lng < 110 || lng > 155)
        {
            return Results.BadRequest(new { error = "Coordinates must be within Australia" });
        }

        var result = await cadastralService.GetParcelByCoordinateAsync(lat, lng, ct);

        if (result is null)
        {
            return Results.NotFound(new {
                message = "No parcel found at these coordinates",
                lat,
                lng
            });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetParcelByAddress(
        string address,
        ICadastralService cadastralService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(address))
        {
            return Results.BadRequest(new { error = "Address is required" });
        }

        var result = await cadastralService.GetParcelByAddressAsync(address, ct);

        if (result is null)
        {
            return Results.NotFound(new {
                message = "No parcel found for this address",
                address
            });
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> GetStateFromCoordinate(
        double lat,
        double lng,
        ICadastralService cadastralService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        // Validate coordinates are within reasonable bounds for Australia
        if (lat < -45 || lat > -10 || lng < 110 || lng > 155)
        {
            return Results.BadRequest(new { error = "Coordinates must be within Australia" });
        }

        var state = await cadastralService.GetStateFromCoordinateAsync(lat, lng, ct);

        if (state is null)
        {
            return Results.NotFound(new {
                message = "Could not determine state for these coordinates",
                lat,
                lng
            });
        }

        return Results.Ok(new { state, lat, lng });
    }

    private static IResult GetEnabledStates(
        ICadastralService cadastralService,
        ICurrentUserService currentUser)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var enabledStates = cadastralService.GetEnabledStates();

        return Results.Ok(new
        {
            states = enabledStates,
            summary = new
            {
                enabled = enabledStates.Count(s => s.Value),
                disabled = enabledStates.Count(s => !s.Value)
            }
        });
    }
}
