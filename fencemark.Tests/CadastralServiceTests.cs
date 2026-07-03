using System.Net;
using System.Text;
using fencemark.ApiService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace fencemark.Tests;

/// <summary>
/// Tests for the real NSW/VIC ArcGIS REST cadastral integration added for issue #182
/// (child of #174). Sample JSON below is captured from live queries against the actual
/// NSW_Cadastre (Lot layer 9) and Vicmap_Parcel (layer 0) ArcGIS REST services for the
/// coordinates named in the issue (Sydney / Melbourne CBD), so the parsing tests exercise
/// real field names and shapes rather than an invented schema.
/// </summary>
public class CadastralServiceTests
{
    private const string NswSampleResponse = """
        {"type":"FeatureCollection","crs":{"type":"name","properties":{"name":"EPSG:4326"}},"features":[{"type":"Feature","id":1236669,"geometry":{"type":"Polygon","coordinates":[[[151.2095,-33.8678],[151.2088,-33.8684],[151.2095,-33.8678]]]},"properties":{"objectid":1236669,"cadid":102169538,"planoid":47274,"plannumber":598704,"planlabel":"DP598704","lotnumber":"1","planlotarea":null,"lotidstring":"1//DP598704","urbanity":"U","shape_Length":488.14303267080732,"shape_Area":10328.589025760246}}]}
        """;

    private const string VicSampleResponse = """
        {"type":"FeatureCollection","crs":{"type":"name","properties":{"name":"EPSG:4326"}},"features":[{"type":"Feature","id":2068618,"geometry":{"type":"Polygon","coordinates":[[[144.9637,-37.8139],[144.9633,-37.8141],[144.9637,-37.8139]]]},"properties":{"OBJECTID":2068618,"parcel_ufi":872303044,"parcel_pfi":"152191430","parcel_spi":"PC366537","parcel_plan_number":"PC366537","parcel_status":"A","Shape__Area":6193.015625,"Shape__Length":345.89735383088475}}]}
        """;

    private const string EmptyFeatureCollectionResponse = """{"type":"FeatureCollection","features":[]}""";

    private class CapturingHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private class SingleClientHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private static CadastralService CreateService(HttpMessageHandler handler, CadastralOptions options)
    {
        return new CadastralService(
            new SingleClientHttpClientFactory(handler),
            new Logger<CadastralService>(new LoggerFactory()),
            Options.Create(options));
    }

    // --- ParseArcGisParcelResponse (pure parsing logic) ---

    [Fact]
    public void ParseArcGisParcelResponse_WithRealNswResponse_ExtractsLotIdAndArea()
    {
        var result = CadastralService.ParseArcGisParcelResponse(
            NswSampleResponse, "NSW", parcelIdField: "lotidstring", fallbackIdField: "planlabel", areaField: "shape_Area");

        Assert.NotNull(result);
        Assert.Equal("NSW", result!.State);
        Assert.Equal("1//DP598704", result.ParcelId);
        Assert.Equal(10328.589025760246, result.AreaSquareMetres);
        Assert.Equal(NswSampleResponse, result.GeoJson);
    }

    [Fact]
    public void ParseArcGisParcelResponse_WithRealVicResponse_ExtractsSpiAndArea()
    {
        var result = CadastralService.ParseArcGisParcelResponse(
            VicSampleResponse, "VIC", parcelIdField: "parcel_spi", fallbackIdField: "parcel_plan_number", areaField: "Shape__Area");

        Assert.NotNull(result);
        Assert.Equal("VIC", result!.State);
        Assert.Equal("PC366537", result.ParcelId);
        Assert.Equal(6193.015625, result.AreaSquareMetres);
    }

    [Fact]
    public void ParseArcGisParcelResponse_WithNoFeatures_ReturnsNull()
    {
        var result = CadastralService.ParseArcGisParcelResponse(
            EmptyFeatureCollectionResponse, "NSW", "lotidstring", "planlabel", "shape_Area");

        Assert.Null(result);
    }

    [Fact]
    public void ParseArcGisParcelResponse_WhenPrimaryIdFieldMissing_FallsBackToSecondField()
    {
        var responseWithNullPrimaryId = """
            {"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Polygon","coordinates":[]},"properties":{"lotidstring":null,"planlabel":"DP999999","shape_Area":100.0}}]}
            """;

        var result = CadastralService.ParseArcGisParcelResponse(
            responseWithNullPrimaryId, "NSW", "lotidstring", "planlabel", "shape_Area");

        Assert.NotNull(result);
        Assert.Equal("DP999999", result!.ParcelId);
    }

    // --- End-to-end through GetParcelByCoordinateAsync (state detection + HTTP + parsing) ---

    [Fact]
    public async Task GetParcelByCoordinateAsync_ForSydneyCoordinates_QueriesNswAndReturnsParsedParcel()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, NswSampleResponse);
        var options = new CadastralOptions
        {
            NSW = new StateConfig { Endpoint = "https://example.test/nsw-cadastre", Enabled = true }
        };
        var service = CreateService(handler, options);

        // Sydney Opera House area - the coordinate named in issue #182's test plan.
        var result = await service.GetParcelByCoordinateAsync(-33.8688, 151.2093, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NSW", result!.State);
        Assert.Equal("1//DP598704", result.ParcelId);
        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("geometryType=esriGeometryPoint", handler.LastRequestUri!.Query);
        Assert.Contains("f=geojson", handler.LastRequestUri.Query);
        Assert.Contains("151.2093", handler.LastRequestUri.Query);
    }

    [Fact]
    public async Task GetParcelByCoordinateAsync_ForMelbourneCoordinates_QueriesVicAndReturnsParsedParcel()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, VicSampleResponse);
        var options = new CadastralOptions
        {
            VIC = new StateConfig { Endpoint = "https://example.test/vic-cadastre", Enabled = true }
        };
        var service = CreateService(handler, options);

        // Melbourne CBD - the coordinate named in issue #182's test plan.
        var result = await service.GetParcelByCoordinateAsync(-37.8136, 144.9631, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("VIC", result!.State);
        Assert.Equal("PC366537", result.ParcelId);
    }

    [Fact]
    public async Task GetParcelByCoordinateAsync_WhenStateDisabled_ReturnsNullWithoutCallingApi()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, NswSampleResponse);
        var options = new CadastralOptions
        {
            NSW = new StateConfig { Endpoint = "https://example.test/nsw-cadastre", Enabled = false }
        };
        var service = CreateService(handler, options);

        var result = await service.GetParcelByCoordinateAsync(-33.8688, 151.2093, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(handler.LastRequestUri);
    }

    [Fact]
    public async Task GetParcelByCoordinateAsync_WhenApiReturnsError_ReturnsNull()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.InternalServerError, "");
        var options = new CadastralOptions
        {
            NSW = new StateConfig { Endpoint = "https://example.test/nsw-cadastre", Enabled = true }
        };
        var service = CreateService(handler, options);

        var result = await service.GetParcelByCoordinateAsync(-33.8688, 151.2093, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetParcelByCoordinateAsync_ForStateWithoutRealImplementation_ReturnsNull()
    {
        // QLD is configured with an endpoint but has no real implementation yet -
        // must fail closed (null), not throw.
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK, NswSampleResponse);
        var options = new CadastralOptions
        {
            QLD = new StateConfig { Endpoint = "https://example.test/qld-cadastre", Enabled = true }
        };
        var service = CreateService(handler, options);

        // Brisbane CBD
        var result = await service.GetParcelByCoordinateAsync(-27.4698, 153.0251, CancellationToken.None);

        Assert.Null(result);
        Assert.Null(handler.LastRequestUri);
    }

    // --- GetStateFromCoordinateAsync bounding-box detection ---

    [Theory]
    [InlineData(-33.8688, 151.2093, "NSW")] // Sydney
    [InlineData(-37.8136, 144.9631, "VIC")] // Melbourne
    [InlineData(-27.4698, 153.0251, "QLD")] // Brisbane
    [InlineData(-35.2809, 149.1300, "ACT")] // Canberra
    [InlineData(-42.8821, 147.3272, "TAS")] // Hobart
    public async Task GetStateFromCoordinateAsync_ForKnownCityCoordinates_ReturnsExpectedState(
        double lat, double lng, string expectedState)
    {
        var service = CreateService(new CapturingHttpMessageHandler(HttpStatusCode.OK, "{}"), new CadastralOptions());

        var state = await service.GetStateFromCoordinateAsync(lat, lng, CancellationToken.None);

        Assert.Equal(expectedState, state);
    }

    [Fact]
    public async Task GetStateFromCoordinateAsync_OutsideAustralia_ReturnsNull()
    {
        var service = CreateService(new CapturingHttpMessageHandler(HttpStatusCode.OK, "{}"), new CadastralOptions());

        // Auckland, New Zealand
        var state = await service.GetStateFromCoordinateAsync(-36.8485, 174.7633, CancellationToken.None);

        Assert.Null(state);
    }

    // --- Live smoke tests against the real government endpoints ---
    // Skipped by default (external network dependency, not suitable for CI), but kept
    // as an easy manual check that the real ArcGIS services still respond the way this
    // implementation expects. Run manually by temporarily removing the Skip parameter below.

    [Fact(Skip = "Hits a live external government API - run manually to verify against real data")]
    public async Task Live_GetParcelByCoordinateAsync_ForSydneyOperaHouse_ReturnsNswParcel()
    {
        var service = new CadastralService(
            new RealHttpClientFactory(),
            new Logger<CadastralService>(new LoggerFactory()),
            Options.Create(new CadastralOptions
            {
                NSW = new StateConfig
                {
                    Endpoint = "https://maps.six.nsw.gov.au/arcgis/rest/services/public/NSW_Cadastre/MapServer/9",
                    Enabled = true
                }
            }));

        var result = await service.GetParcelByCoordinateAsync(-33.8688, 151.2093, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NSW", result!.State);
        Assert.False(string.IsNullOrEmpty(result.ParcelId));
        Assert.True(result.AreaSquareMetres > 0);
    }

    [Fact(Skip = "Hits a live external government API - run manually to verify against real data")]
    public async Task Live_GetParcelByCoordinateAsync_ForMelbourneCbd_ReturnsVicParcel()
    {
        var service = new CadastralService(
            new RealHttpClientFactory(),
            new Logger<CadastralService>(new LoggerFactory()),
            Options.Create(new CadastralOptions
            {
                VIC = new StateConfig
                {
                    Endpoint = "https://services-ap1.arcgis.com/P744lA0wf4LlBZ84/ArcGIS/rest/services/Vicmap_Parcel/FeatureServer/0",
                    Enabled = true
                }
            }));

        var result = await service.GetParcelByCoordinateAsync(-37.8136, 144.9631, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("VIC", result!.State);
        Assert.False(string.IsNullOrEmpty(result.ParcelId));
        Assert.True(result.AreaSquareMetres > 0);
    }

    private class RealHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
