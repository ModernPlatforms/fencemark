using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

        group.MapGet("/azure-maps-token", GetAzureMapsToken)
            .WithName("GetAzureMapsToken")
            .WithDescription("Get Azure Maps access token for client-side map initialization");

        group.MapGet("/search-address", SearchAddress)
            .WithName("SearchAddress")
            .WithDescription("Search for an address using Azure Maps geocoding and return candidate locations");

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

    private static async Task<IResult> GetAzureMapsToken(
        IAzureMapsTokenService azureMapsTokenService,
        ICurrentUserService currentUser,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var logger = loggerFactory.CreateLogger("MappingEndpoints");

        try
        {
            var tokenResult = await azureMapsTokenService.GetTokenAsync(ct);

            return Results.Ok(new
            {
                token = tokenResult.Token,
                expiresOn = tokenResult.ExpiresOn,
                clientId = tokenResult.ClientId,
                useSubscriptionKey = tokenResult.UseSubscriptionKey
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Azure Maps not configured");
            return Results.BadRequest(new { error = "Azure Maps is not configured on the server" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to acquire Azure Maps token");
            return Results.StatusCode(500);
        }
    }

    private static readonly JsonSerializerOptions AzureMapsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<IResult> SearchAddress(
        string query,
        IAzureMapsTokenService azureMapsTokenService,
        IHttpClientFactory httpClientFactory,
        ICurrentUserService currentUser,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
        {
            return Results.BadRequest(new { error = "Query must be at least 3 characters" });
        }

        var logger = loggerFactory.CreateLogger("MappingEndpoints");

        try
        {
            var tokenResult = await azureMapsTokenService.GetTokenAsync(ct);
            var client = httpClientFactory.CreateClient();

            var baseUrl = $"https://atlas.microsoft.com/search/address/json?api-version=1.0&countrySet=AU&limit=5&query={Uri.EscapeDataString(query.Trim())}";
            var requestUrl = tokenResult.UseSubscriptionKey
                ? $"{baseUrl}&subscription-key={Uri.EscapeDataString(tokenResult.Token)}"
                : baseUrl;

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            if (!tokenResult.UseSubscriptionKey)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
                request.Headers.Add("x-ms-client-id", tokenResult.ClientId);
            }

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Azure Maps address search failed with status {StatusCode}", response.StatusCode);
                return Results.StatusCode(502);
            }

            var payload = await response.Content.ReadFromJsonAsync<AzureMapsSearchResponse>(AzureMapsJsonOptions, ct);
            var results = (payload?.Results ?? [])
                .Where(r => r.Position is not null)
                .Select(r => new AddressSearchResult(
                    r.Address?.FreeformAddress ?? query,
                    r.Position!.Lat,
                    r.Position!.Lon))
                .ToList();

            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search address via Azure Maps");
            return Results.StatusCode(500);
        }
    }

    private record AddressSearchResult(string Address, double Lat, double Lng);

    private record AzureMapsSearchResponse(List<AzureMapsSearchResultItem>? Results);

    private record AzureMapsSearchResultItem(AzureMapsAddress? Address, AzureMapsPosition? Position);

    private record AzureMapsAddress(string? FreeformAddress);

    private record AzureMapsPosition(double Lat, double Lon);
}
