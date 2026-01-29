using Microsoft.Extensions.Options;

namespace fencemark.ApiService.Services;

/// <summary>
/// Service for retrieving cadastral (lot/parcel boundary) data from Australian state APIs
/// </summary>
public interface ICadastralService
{
    /// <summary>
    /// Gets the parcel boundary for a given coordinate
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cadastral parcel result, or null if not found</returns>
    Task<CadastralParcelResult?> GetParcelByCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parcel boundary for a given address
    /// </summary>
    /// <param name="address">Street address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cadastral parcel result, or null if not found</returns>
    Task<CadastralParcelResult?> GetParcelByAddressAsync(
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines which Australian state/territory a coordinate is in
    /// </summary>
    Task<string?> GetStateFromCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration status for all states
    /// </summary>
    Dictionary<string, bool> GetEnabledStates();
}

/// <summary>
/// Result from a cadastral parcel lookup
/// </summary>
public record CadastralParcelResult(
    string State,
    string? ParcelId,
    string? Address,
    string GeoJson,
    double? AreaSquareMetres,
    DateTime? LastUpdated
);

/// <summary>
/// Configuration options for cadastral APIs
/// </summary>
public class CadastralOptions
{
    public const string SectionName = "Cadastral";

    public StateConfig NSW { get; set; } = new();
    public StateConfig VIC { get; set; } = new();
    public StateConfig QLD { get; set; } = new();
    public StateConfig TAS { get; set; } = new();
    public StateConfig ACT { get; set; } = new();
    public StateConfig NT { get; set; } = new();
    public StateConfig SA { get; set; } = new();
    public StateConfig WA { get; set; } = new();
}

/// <summary>
/// Configuration for a single state's cadastral API
/// </summary>
public class StateConfig
{
    public string? Endpoint { get; set; }
    public bool Enabled { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Implementation of cadastral service that queries Australian state cadastral APIs
/// </summary>
public class CadastralService(
    IHttpClientFactory httpClientFactory,
    ILogger<CadastralService> logger,
    IOptions<CadastralOptions> options) : ICadastralService
{
    private readonly CadastralOptions _options = options.Value;

    public async Task<CadastralParcelResult?> GetParcelByCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateFromCoordinateAsync(latitude, longitude, cancellationToken);
        if (state is null)
        {
            logger.LogWarning("Could not determine state for coordinates {Lat}, {Lng}", latitude, longitude);
            return null;
        }

        return await GetParcelFromStateApiAsync(state, latitude, longitude, cancellationToken);
    }

    public Task<CadastralParcelResult?> GetParcelByAddressAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement geocoding to get coordinates from address
        // Then call GetParcelByCoordinateAsync
        logger.LogWarning("GetParcelByAddressAsync not yet implemented for address: {Address}", address);
        return Task.FromResult<CadastralParcelResult?>(null);
    }

    public Task<string?> GetStateFromCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        // Simple bounding box check for Australian states
        var state = DetermineStateFromCoordinates(latitude, longitude);
        return Task.FromResult(state);
    }

    public Dictionary<string, bool> GetEnabledStates()
    {
        return new Dictionary<string, bool>
        {
            ["NSW"] = _options.NSW.Enabled,
            ["VIC"] = _options.VIC.Enabled,
            ["QLD"] = _options.QLD.Enabled,
            ["TAS"] = _options.TAS.Enabled,
            ["ACT"] = _options.ACT.Enabled,
            ["NT"] = _options.NT.Enabled,
            ["SA"] = _options.SA.Enabled,
            ["WA"] = _options.WA.Enabled
        };
    }

    private static string? DetermineStateFromCoordinates(double lat, double lng)
    {
        // Check if within Australia bounds first
        if (lat > -10 || lat < -44 || lng < 113 || lng > 154)
            return null;

        // Check for ACT first (it's within NSW bounds)
        // ACT: roughly -35 to -36 lat, 148.7 to 149.4 lng
        if (lat >= -36 && lat <= -35 && lng >= 148.7 && lng <= 149.4)
            return "ACT";

        // TAS: roughly -39 to -44 lat, 144 to 149 lng
        if (lat >= -44 && lat < -39.5 && lng >= 144 && lng <= 149)
            return "TAS";

        // VIC: roughly -34 to -39 lat, 141 to 150 lng
        if (lat >= -39.5 && lat < -34 && lng >= 141 && lng <= 150)
            return "VIC";

        // NSW: roughly -28 to -37 lat, 141 to 154 lng
        if (lat >= -37.5 && lat <= -28 && lng >= 141 && lng <= 154)
            return "NSW";

        // QLD: roughly -10 to -29 lat, 138 to 154 lng
        if (lat >= -29 && lat <= -10 && lng >= 138 && lng <= 154)
            return "QLD";

        // SA: roughly -26 to -38 lat, 129 to 141 lng
        if (lat >= -38 && lat <= -26 && lng >= 129 && lng < 141)
            return "SA";

        // WA: roughly -13 to -35 lat, 113 to 129 lng
        if (lat >= -35 && lat <= -13 && lng >= 113 && lng < 129)
            return "WA";

        // NT: roughly -10 to -26 lat, 129 to 138 lng
        if (lat >= -26 && lat <= -10 && lng >= 129 && lng < 138)
            return "NT";

        return null;
    }

    private async Task<CadastralParcelResult?> GetParcelFromStateApiAsync(
        string state,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var stateConfig = GetStateConfig(state);
        if (stateConfig is null || !stateConfig.Enabled)
        {
            logger.LogInformation("Cadastral API not enabled for state {State}", state);
            return null;
        }

        if (string.IsNullOrEmpty(stateConfig.Endpoint))
        {
            logger.LogWarning("No endpoint configured for state {State}", state);
            return null;
        }

        try
        {
            // TODO: Implement actual API calls for each state
            // Each state has different API formats (ArcGIS REST, WFS, etc.)
            logger.LogInformation(
                "Would query {State} cadastral API at {Endpoint} for coordinates ({Lat}, {Lng})",
                state, stateConfig.Endpoint, latitude, longitude);

            // Placeholder - return null until real implementations are added
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying cadastral API for state {State}", state);
            return null;
        }
    }

    private StateConfig? GetStateConfig(string state)
    {
        return state.ToUpperInvariant() switch
        {
            "NSW" => _options.NSW,
            "VIC" => _options.VIC,
            "QLD" => _options.QLD,
            "TAS" => _options.TAS,
            "ACT" => _options.ACT,
            "NT" => _options.NT,
            "SA" => _options.SA,
            "WA" => _options.WA,
            _ => null
        };
    }
}
