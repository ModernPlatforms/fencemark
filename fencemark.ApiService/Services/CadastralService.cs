using System.Text.Json;
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
    Task<CadastralParcelResult?> GetParcelByCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parcel boundary for a given address
    /// </summary>
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
        logger.LogWarning("GetParcelByAddressAsync not yet implemented for address: {Address}", address);
        return Task.FromResult<CadastralParcelResult?>(null);
    }

    public Task<string?> GetStateFromCoordinateAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
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
            return state.ToUpperInvariant() switch
            {
                "NSW" => await QueryNswCadastralApiAsync(stateConfig.Endpoint, latitude, longitude, cancellationToken),
                "VIC" => await QueryVicCadastralApiAsync(stateConfig.Endpoint, latitude, longitude, cancellationToken),
                "TAS" => await QueryTasCadastralApiAsync(stateConfig.Endpoint, latitude, longitude, cancellationToken),
                _ => await QueryGenericArcGisApiAsync(state, stateConfig.Endpoint, latitude, longitude, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying cadastral API for state {State}", state);
            return null;
        }
    }

    /// <summary>
    /// Query NSW Cadastre ArcGIS REST API
    /// </summary>
    private async Task<CadastralParcelResult?> QueryNswCadastralApiAsync(
        string endpoint,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // NSW uses ArcGIS REST API - query by geometry
        var queryUrl = $"{endpoint}/query?" +
            $"geometry={longitude},{latitude}&" +
            $"geometryType=esriGeometryPoint&" +
            $"inSR=4326&" +
            $"spatialRel=esriSpatialRelIntersects&" +
            $"outFields=*&" +
            $"returnGeometry=true&" +
            $"outSR=4326&" +
            $"f=geojson";

        logger.LogInformation("Querying NSW cadastral API: {Url}", queryUrl);

        var response = await client.GetAsync(queryUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(json);

        var features = result.RootElement.GetProperty("features");
        if (features.GetArrayLength() == 0)
        {
            logger.LogInformation("No parcel found at NSW coordinates {Lat}, {Lng}", latitude, longitude);
            return null;
        }

        var feature = features[0];
        var properties = feature.GetProperty("properties");

        // Extract parcel ID from NSW properties
        var parcelId = properties.TryGetProperty("lotidstring", out var lotId)
            ? lotId.GetString()
            : properties.TryGetProperty("cadid", out var cadId)
                ? cadId.GetString()
                : null;

        var address = properties.TryGetProperty("address", out var addr)
            ? addr.GetString()
            : null;

        var area = properties.TryGetProperty("shape_Area", out var shapeArea)
            ? shapeArea.GetDouble()
            : (double?)null;

        // Return the feature as GeoJSON
        return new CadastralParcelResult(
            State: "NSW",
            ParcelId: parcelId,
            Address: address,
            GeoJson: feature.GetRawText(),
            AreaSquareMetres: area,
            LastUpdated: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Query VIC Cadastre WFS API
    /// </summary>
    private async Task<CadastralParcelResult?> QueryVicCadastralApiAsync(
        string endpoint,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // VIC uses WFS - create a bounding box around the point
        var buffer = 0.0001; // ~10m buffer
        var bbox = $"{longitude - buffer},{latitude - buffer},{longitude + buffer},{latitude + buffer}";

        var queryUrl = $"{endpoint}?" +
            $"service=WFS&" +
            $"version=2.0.0&" +
            $"request=GetFeature&" +
            $"typeNames=VMPROP_PARCEL_MP&" +
            $"bbox={bbox},EPSG:4326&" +
            $"srsName=EPSG:4326&" +
            $"outputFormat=application/json&" +
            $"count=1";

        logger.LogInformation("Querying VIC cadastral API: {Url}", queryUrl);

        try
        {
            var response = await client.GetAsync(queryUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonDocument.Parse(json);

            if (!result.RootElement.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            {
                logger.LogInformation("No parcel found at VIC coordinates {Lat}, {Lng}", latitude, longitude);
                return null;
            }

            var feature = features[0];
            var properties = feature.GetProperty("properties");

            var parcelId = properties.TryGetProperty("PFI", out var pfi)
                ? pfi.GetString()
                : properties.TryGetProperty("PARCEL_PFI", out var parcelPfi)
                    ? parcelPfi.GetString()
                    : null;

            var address = properties.TryGetProperty("PROPERTY_ADDRESS", out var addr)
                ? addr.GetString()
                : null;

            return new CadastralParcelResult(
                State: "VIC",
                ParcelId: parcelId,
                Address: address,
                GeoJson: feature.GetRawText(),
                AreaSquareMetres: null,
                LastUpdated: DateTime.UtcNow
            );
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "VIC cadastral API request failed, may require authentication");
            return null;
        }
    }

    /// <summary>
    /// Query TAS LIST ArcGIS REST API
    /// </summary>
    private async Task<CadastralParcelResult?> QueryTasCadastralApiAsync(
        string endpoint,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // TAS uses ArcGIS REST API similar to NSW
        var queryUrl = $"{endpoint}/query?" +
            $"geometry={longitude},{latitude}&" +
            $"geometryType=esriGeometryPoint&" +
            $"inSR=4326&" +
            $"spatialRel=esriSpatialRelIntersects&" +
            $"outFields=*&" +
            $"returnGeometry=true&" +
            $"outSR=4326&" +
            $"f=geojson";

        logger.LogInformation("Querying TAS cadastral API: {Url}", queryUrl);

        var response = await client.GetAsync(queryUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonDocument.Parse(json);

        var features = result.RootElement.GetProperty("features");
        if (features.GetArrayLength() == 0)
        {
            logger.LogInformation("No parcel found at TAS coordinates {Lat}, {Lng}", latitude, longitude);
            return null;
        }

        var feature = features[0];
        var properties = feature.GetProperty("properties");

        var parcelId = properties.TryGetProperty("PID", out var pid)
            ? pid.GetString()
            : null;

        return new CadastralParcelResult(
            State: "TAS",
            ParcelId: parcelId,
            Address: null,
            GeoJson: feature.GetRawText(),
            AreaSquareMetres: null,
            LastUpdated: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Generic ArcGIS REST API query for other states
    /// </summary>
    private async Task<CadastralParcelResult?> QueryGenericArcGisApiAsync(
        string state,
        string endpoint,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // Try standard ArcGIS REST API pattern
        var queryUrl = $"{endpoint}/query?" +
            $"geometry={longitude},{latitude}&" +
            $"geometryType=esriGeometryPoint&" +
            $"inSR=4326&" +
            $"spatialRel=esriSpatialRelIntersects&" +
            $"outFields=*&" +
            $"returnGeometry=true&" +
            $"outSR=4326&" +
            $"f=geojson";

        logger.LogInformation("Querying {State} cadastral API: {Url}", state, queryUrl);

        try
        {
            var response = await client.GetAsync(queryUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonDocument.Parse(json);

            if (!result.RootElement.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            {
                logger.LogInformation("No parcel found at {State} coordinates {Lat}, {Lng}", state, latitude, longitude);
                return null;
            }

            var feature = features[0];

            return new CadastralParcelResult(
                State: state,
                ParcelId: null,
                Address: null,
                GeoJson: feature.GetRawText(),
                AreaSquareMetres: null,
                LastUpdated: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{State} cadastral API query failed", state);
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
