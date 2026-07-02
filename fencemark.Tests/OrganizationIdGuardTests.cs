using fencemark.ApiService.Data;
using fencemark.ApiService.Data.Models;
using fencemark.ApiService.Features.Components;
using fencemark.ApiService.Features.Discounts;
using fencemark.ApiService.Features.Drawings;
using fencemark.ApiService.Features.Fences;
using fencemark.ApiService.Features.FenceSegments;
using fencemark.ApiService.Features.Gates;
using fencemark.ApiService.Features.GatePositions;
using fencemark.ApiService.Features.Jobs;
using fencemark.ApiService.Features.Parcels;
using fencemark.ApiService.Features.Pricing;
using fencemark.ApiService.Features.TaxRegions;
using fencemark.ApiService.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace fencemark.Tests;

/// <summary>
/// Regression tests for the "Missing Null Check for OrganizationId" issue: when an
/// authenticated user has no organization membership, Create endpoints used to fall back
/// to an empty-string OrganizationId (`currentUser.OrganizationId ?? string.Empty`),
/// silently creating entities with no valid tenant and breaking RLS/data isolation.
/// Every Create handler must now reject the request instead.
/// </summary>
public class OrganizationIdGuardTests
{
    private class NoOrgCurrentUserService : ICurrentUserService
    {
        public string? UserId => "user-1";
        public string? Email => "user@test.com";
        public string? OrganizationId => null;
        public string? Role => null;
        public bool IsAuthenticated => true;
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void AssertBadRequest(IResult result)
    {
        var statusResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(400, statusResult.StatusCode);
    }

    [Fact]
    public async Task CreateJob_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateJob()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new Job { Name = "Test Job", CustomerName = "Test Customer", OrganizationId = "ignored" };

        var result = await JobEndpoints.CreateJob(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.Jobs);
    }

    [Fact]
    public async Task CreateComponent_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateComponent()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new Component { Name = "Test Component", OrganizationId = "ignored", Category = "Post" };

        var result = await ComponentEndpoints.CreateComponent(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.Components);
    }

    [Fact]
    public async Task CreateFence_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateFenceType()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new FenceType { Name = "Test Fence", OrganizationId = "ignored" };

        var result = await FenceEndpoints.CreateFence(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.FenceTypes);
    }

    [Fact]
    public async Task CreateGate_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateGateType()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new GateType { Name = "Test Gate", OrganizationId = "ignored" };

        var result = await GateEndpoints.CreateGate(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.GateTypes);
    }

    [Fact]
    public async Task CreateTaxRegion_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateTaxRegion()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new TaxRegion { Name = "Test Region", OrganizationId = "ignored" };

        var result = await TaxRegionEndpoints.CreateTaxRegion(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.TaxRegions);
    }

    [Fact]
    public async Task CreateDiscount_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateDiscount()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new DiscountRule { Name = "Test Discount", OrganizationId = "ignored" };

        var result = await DiscountEndpoints.CreateDiscount(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.DiscountRules);
    }

    [Fact]
    public async Task CreatePricingConfig_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateConfig()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new PricingConfig { Name = "Test Config", OrganizationId = "ignored" };

        var result = await PricingEndpoints.CreatePricingConfig(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.PricingConfigs);
    }

    [Fact]
    public async Task CreateParcel_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateParcel()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new Parcel { Name = "Test Parcel", OrganizationId = "ignored", JobId = "some-job" };

        var result = await ParcelEndpoints.CreateParcel(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.Parcels);
    }

    [Fact]
    public async Task CreateDrawing_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateDrawing()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new DrawingEndpoints.DrawingRequest(
            JobId: null, ParcelId: null, Name: "Test Drawing", Description: null,
            DrawingType: null, FileName: "test.pdf", FilePath: "/tmp/test.pdf", MimeType: null, FileSize: 100);

        var result = await DrawingEndpoints.CreateDrawing(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.Drawings);
    }

    [Fact]
    public async Task CreateFenceSegment_WithNoOrganization_ReturnsBadRequestAndDoesNotCreateSegment()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new FenceSegmentEndpoints.FenceSegmentRequest(
            JobId: "some-job", ParcelId: null, Name: "Test Segment", FenceTypeId: "some-fence-type",
            GeoJsonGeometry: "{}", LengthInMetres: 10, IsSnappedToBoundary: false, Notes: null, IsVerifiedOnsite: false, OnsiteVerifiedLengthInMetres: null);

        var result = await FenceSegmentEndpoints.CreateFenceSegment(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.FenceSegments);
    }

    [Fact]
    public async Task CreateGatePosition_WithNoOrganization_ReturnsBadRequestAndDoesNotCreatePosition()
    {
        await using var db = CreateDbContext();
        var currentUser = new NoOrgCurrentUserService();
        var request = new GatePositionEndpoints.GatePositionRequest(
            FenceSegmentId: "some-segment", GateTypeId: null, Name: "Test Gate Position",
            GeoJsonLocation: "{}", PositionAlongSegment: 0.5m, Notes: null, IsVerifiedOnsite: false);

        var result = await GatePositionEndpoints.CreateGatePosition(request, db, currentUser, CancellationToken.None);

        AssertBadRequest(result);
        Assert.Empty(db.GatePositions);
    }
}
