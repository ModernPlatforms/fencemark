using Microsoft.Playwright;
using System.Text.Json;

namespace fencemark.Tests.E2E;

/// <summary>
/// Comprehensive E2E tests that test all major endpoints
/// Uses a persistent test user and cleans up test data after each test
/// </summary>
public class AllEndpointsE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private PlaywrightAuthHelper? _authHelper;
    private List<string> _createdComponentIds = new();
    private List<string> _createdJobIds = new();
    private List<string> _createdFenceIds = new();
    private List<string> _createdGateIds = new();

    public async ValueTask InitializeAsync()
    {
        await SetupAsync();
        _authHelper = new PlaywrightAuthHelper(Page!, BaseUrl);
        
        // Login with persistent test user
        var loginSuccess = await _authHelper.LoginAsync(
            TestConfiguration.TestUserEmail,
            TestConfiguration.TestUserPassword);
        
        Assert.True(loginSuccess, "Test user login failed - ensure TEST_USER_EMAIL and TEST_USER_PASSWORD are set correctly");
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup all created test data
        await CleanupTestDataAsync();
        await TeardownAsync();
    }

    /// <summary>
    /// Helper method to make API calls via Playwright's page.evaluate
    /// </summary>
    private async Task<JsonElement> ApiCallAsync(string method, string endpoint, object? body = null)
    {
        var payload = body != null ? JsonSerializer.Serialize(body) : null;
        
        return await Page!.EvaluateAsync<JsonElement>(@"
            async (args) => {
                const options = {
                    method: args.method,
                    credentials: 'include',
                    headers: args.body ? { 'Content-Type': 'application/json' } : {}
                };
                if (args.body) {
                    options.body = args.body;
                }
                const response = await fetch(args.url, options);
                if (response.ok && response.headers.get('content-type')?.includes('application/json')) {
                    return await response.json();
                }
                return { ok: response.ok, status: response.status };
            }", new { method, url = $"{BaseUrl}{endpoint}", body = payload });
    }

    /// <summary>
    /// Cleanup all test data created during tests
    /// </summary>
    private async Task CleanupTestDataAsync()
    {
        // Delete components
        foreach (var componentId in _createdComponentIds)
        {
            try
            {
                await ApiCallAsync("DELETE", $"/api/components/{componentId}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Delete jobs
        foreach (var jobId in _createdJobIds)
        {
            try
            {
                await ApiCallAsync("DELETE", $"/api/jobs/{jobId}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Delete fences
        foreach (var fenceId in _createdFenceIds)
        {
            try
            {
                await ApiCallAsync("DELETE", $"/api/fences/{fenceId}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Delete gates
        foreach (var gateId in _createdGateIds)
        {
            try
            {
                await ApiCallAsync("DELETE", $"/api/gates/{gateId}");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AllEndpointsE2ETests")]
    public async Task CanCreateAndDeleteComponent()
    {
        // Act - Create component
        var response = await ApiCallAsync("POST", "/api/components", new
        {
            name = "E2E Test Component",
            sku = "E2E-TEST-001",
            category = "Test Category",
            unitPrice = 15.99m,
            unitOfMeasure = "Each",
            description = "Created by E2E test"
        });

        // Assert - Component created
        var componentId = response.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(componentId);
        _createdComponentIds.Add(componentId!);

        await TakeScreenshotAsync("component-created.png");

        // Act - Get component
        var getResponse = await ApiCallAsync("GET", $"/api/components/{componentId}");
        Assert.True(getResponse.TryGetProperty("name", out var nameProp));
        Assert.Equal("E2E Test Component", nameProp.GetString());

        // Act - Delete component
        await ApiCallAsync("DELETE", $"/api/components/{componentId}");
        _createdComponentIds.Remove(componentId!);

        await TakeScreenshotAsync("component-deleted.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AllEndpointsE2ETests")]
    public async Task CanCreateAndDeleteJob()
    {
        // Act - Create job
        var response = await ApiCallAsync("POST", "/api/jobs", new
        {
            name = "E2E Test Job",
            customerName = "Test Customer",
            customerEmail = "test@example.com",
            customerPhone = "555-1234",
            installationAddress = "123 Test St",
            status = "Draft"
        });

        // Assert - Job created
        var jobId = response.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(jobId);
        _createdJobIds.Add(jobId!);

        await TakeScreenshotAsync("job-created.png");

        // Act - Get job
        var getResponse = await ApiCallAsync("GET", $"/api/jobs/{jobId}");
        Assert.True(getResponse.TryGetProperty("name", out var nameProp));
        Assert.Equal("E2E Test Job", nameProp.GetString());

        // Act - Update job
        await ApiCallAsync("PUT", $"/api/jobs/{jobId}", new
        {
            name = "E2E Test Job Updated",
            customerName = "Test Customer Updated",
            status = "Active"
        });

        await TakeScreenshotAsync("job-updated.png");

        // Act - Delete job
        await ApiCallAsync("DELETE", $"/api/jobs/{jobId}");
        _createdJobIds.Remove(jobId!);

        await TakeScreenshotAsync("job-deleted.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AllEndpointsE2ETests")]
    public async Task CanCreateAndDeleteFence()
    {
        // Act - Create fence
        var response = await ApiCallAsync("POST", "/api/fences", new
        {
            name = "E2E Test Fence",
            style = "Privacy",
            height = 6,
            material = "Cedar",
            color = "Natural",
            pricePerLinearFoot = 45.00m
        });

        // Assert - Fence created
        var fenceId = response.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(fenceId);
        _createdFenceIds.Add(fenceId!);

        await TakeScreenshotAsync("fence-created.png");

        // Act - Get fence
        var getResponse = await ApiCallAsync("GET", $"/api/fences/{fenceId}");
        Assert.True(getResponse.TryGetProperty("name", out var nameProp));
        Assert.Equal("E2E Test Fence", nameProp.GetString());

        // Act - Delete fence
        await ApiCallAsync("DELETE", $"/api/fences/{fenceId}");
        _createdFenceIds.Remove(fenceId!);

        await TakeScreenshotAsync("fence-deleted.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AllEndpointsE2ETests")]
    public async Task CanCreateAndDeleteGate()
    {
        // Act - Create gate
        var response = await ApiCallAsync("POST", "/api/gates", new
        {
            name = "E2E Test Gate",
            style = "Single Swing",
            width = 4,
            height = 6,
            material = "Cedar",
            price = 350.00m
        });

        // Assert - Gate created
        var gateId = response.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        Assert.NotNull(gateId);
        _createdGateIds.Add(gateId!);

        await TakeScreenshotAsync("gate-created.png");

        // Act - Get gate
        var getResponse = await ApiCallAsync("GET", $"/api/gates/{gateId}");
        Assert.True(getResponse.TryGetProperty("name", out var nameProp));
        Assert.Equal("E2E Test Gate", nameProp.GetString());

        // Act - Delete gate
        await ApiCallAsync("DELETE", $"/api/gates/{gateId}");
        _createdGateIds.Remove(gateId!);

        await TakeScreenshotAsync("gate-deleted.png");
    }

    [Fact(Skip = "E2E tests require running application - run manually with dotnet test --filter AllEndpointsE2ETests")]
    public async Task CanListAllEndpoints()
    {
        // Test all list endpoints
        
        // Components
        var components = await ApiCallAsync("GET", "/api/components");
        Assert.True(components.ValueKind == JsonValueKind.Array || components.ValueKind == JsonValueKind.Object);

        // Jobs
        var jobs = await ApiCallAsync("GET", "/api/jobs");
        Assert.True(jobs.ValueKind == JsonValueKind.Array || jobs.ValueKind == JsonValueKind.Object);

        // Fences
        var fences = await ApiCallAsync("GET", "/api/fences");
        Assert.True(fences.ValueKind == JsonValueKind.Array || fences.ValueKind == JsonValueKind.Object);

        // Gates
        var gates = await ApiCallAsync("GET", "/api/gates");
        Assert.True(gates.ValueKind == JsonValueKind.Array || gates.ValueKind == JsonValueKind.Object);

        await TakeScreenshotAsync("all-endpoints-tested.png");
    }

    protected override string BaseUrl => TestConfiguration.BaseUrl;
    protected override bool Headless => TestConfiguration.Headless;
}
