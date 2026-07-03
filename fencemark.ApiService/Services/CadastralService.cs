using System.Globalization;
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
            return state.ToUpperInvariant() switch
            {
                "NSW" => await GetNswParcelAsync(latitude, longitude, stateConfig.Endpoint, cancellationToken),
                "VIC" => await GetVicParcelAsync(latitude, longitude, stateConfig.Endpoint, cancellationToken),
                _ => LogUnimplementedState(state)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying cadastral API for state {State}", state);
            return null;
        }
    }

    private CadastralParcelResult? LogUnimplementedState(string state)
    {
        logger.LogInformation("Cadastral API integration not yet implemented for state {State}", state);
        return null;
    }

    /// <summary>
    /// Queries the NSW_Cadastre "Lot" layer (id 9) on the NSW Spatial Services ArcGIS
    /// MapServer for the parcel intersecting the given point.
    /// See https://maps.six.nsw.gov.au/arcgis/rest/services/public/NSW_Cadastre/MapServer/9
    /// </summary>
    private Task<CadastralParcelResult?> GetNswParcelAsync(
        double latitude,
        double longitude,
        string endpoint,
        CancellationToken cancellationToken)
        => QueryArcGisParcelAsync(
            endpoint,
            "NSW",
            latitude,
            longitude,
            parcelIdField: "lotidstring",
            fallbackIdField: "planlabel",
            areaField: "shape_Area",
            cancellationToken);

    /// <summary>
    /// Queries the Vicmap_Parcel "Parcel Map Polygons" layer (id 0) on the Victorian
    /// Government's ArcGIS FeatureServer for the parcel intersecting the given point.
    /// See https://services-ap1.arcgis.com/P744lA0wf4LlBZ84/ArcGIS/rest/services/Vicmap_Parcel/FeatureServer/0
    /// </summary>
    private Task<CadastralParcelResult?> GetVicParcelAsync(
        double latitude,
        double longitude,
        string endpoint,
        CancellationToken cancellationToken)
        => QueryArcGisParcelAsync(
            endpoint,
            "VIC",
            latitude,
            longitude,
            parcelIdField: "parcel_spi",
            fallbackIdField: "parcel_plan_number",
            areaField: "Shape__Area",
            cancellationToken);

    /// <summary>
    /// Performs an ArcGIS REST "query" spatial intersection lookup for a point and returns
    /// the first matching parcel as GeoJSON. Both NSW and VIC cadastral services expose this
    /// same ArcGIS REST query API shape (just with different field names per state), so the
    /// query/parsing logic is shared.
    /// </summary>
    private async Task<CadastralParcelResult?> QueryArcGisParcelAsync(
        string endpoint,
        string state,
        double latitude,
        double longitude,
        string parcelIdField,
        string fallbackIdField,
        string areaField,
        CancellationToken cancellationToken)
    {
        var queryUrl =
            $"{endpoint.TrimEnd('/')}/query" +
            $"?geometry={longitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)}" +
            "&geometryType=esriGeometryPoint" +
            "&inSR=4326" +
            "&spatialRel=esriSpatialRelIntersects" +
            "&outFields=*" +
            "&returnGeometry=true" +
            "&outSR=4326" +
            "&f=geojson";

        var client = httpClientFactory.CreateClient();
        using var response = await client.GetAsync(queryUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Cadastral API for {State} returned {StatusCode} for coordinates ({Lat}, {Lng})",
                state, response.StatusCode, latitude, longitude);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseArcGisParcelResponse(body, state, parcelIdField, fallbackIdField, areaField);
    }

    /// <summary>
    /// Parses an ArcGIS REST GeoJSON query response into a <see cref="CadastralParcelResult"/>.
    /// Internal (rather than private) so it can be unit tested directly against canned
    /// responses without requiring a live HTTP call.
    /// </summary>
    internal static CadastralParcelResult? ParseArcGisParcelResponse(
        string geoJsonResponseBody,
        string state,
        string parcelIdField,
        string fallbackIdField,
        string areaField)
    {
        using var document = JsonDocument.Parse(geoJsonResponseBody);

        if (!document.RootElement.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array ||
            features.GetArrayLength() == 0)
        {
            return null;
        }

        var firstFeature = features[0];
        if (!firstFeature.TryGetProperty("properties", out var properties))
        {
            return null;
        }

        var parcelId = GetStringOrNull(properties, parcelIdField) ?? GetStringOrNull(properties, fallbackIdField);
        var areaSquareMetres = GetDoubleOrNull(properties, areaField);

        return new CadastralParcelResult(
            State: state,
            ParcelId: parcelId,
            Address: null,
            GeoJson: geoJsonResponseBody,
            AreaSquareMetres: areaSquareMetres,
            LastUpdated: DateTime.UtcNow);
    }

    private static string? GetStringOrNull(JsonElement properties, string fieldName)
    {
        if (!properties.TryGetProperty(fieldName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }

    private static double? GetDoubleOrNull(JsonElement properties, string fieldName)
    {
        if (!properties.TryGetProperty(fieldName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.GetDouble();
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
