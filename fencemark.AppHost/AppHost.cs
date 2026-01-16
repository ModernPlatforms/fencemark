using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// Environment Detection and Configuration
// ============================================================================
var environmentName = builder.Environment.EnvironmentName;
var isLocal = environmentName == "Development";

Console.WriteLine($"Starting Aspire orchestration for environment: {environmentName}");

// ============================================================================
// Shared SQL Server (Aspire resource)
// ============================================================================
// When running under Aspire locally, this SQL resource will be started
// in a container and its connection string will be injected as
// ConnectionStrings:DefaultConnection into referencing projects.
var sql = builder.AddSqlServer("sql", port: 1433)
    .WithLifetime(ContainerLifetime.Persistent);

var sqldb=sql.AddDatabase("fencemark");
 

// ============================================================================
// API Service Configuration
// ============================================================================
var apiService = builder.AddProject<Projects.fencemark_ApiService>("apiservice")
    .WaitFor(sqldb)
    .WithReference(sqldb)
    .WithReplicas(GetMinReplicas(environmentName, "ApiService"));

// Apply environment-specific resource limits for non-local environments
if (!isLocal)
{
    apiService = ApplyResourceLimits(apiService, environmentName, "ApiService");
}

// ============================================================================
// Web Frontend Configuration
// ============================================================================
// Build and publish the Blazor WASM client, then serve via nginx container

// Publish the WASM client to get all static files in one directory
var publishOutputPath = Path.Combine("..", "fencemark.Client", "bin", "Release", "net10.0", "publish", "wwwroot");
var nginxConfigPath = Path.Combine(builder.AppHostDirectory, "nginx.conf");
var nginxCertsPath = Path.Combine(builder.AppHostDirectory, "nginx-certs");

// Check if certificates exist
if (!Directory.Exists(nginxCertsPath) || 
    !File.Exists(Path.Combine(nginxCertsPath, "aspnetapp.crt")) ||
    !File.Exists(Path.Combine(nginxCertsPath, "aspnetapp.key")))
{
    Console.WriteLine("WARNING: nginx SSL certificates not found.");
    Console.WriteLine($"Please run: .\\fencemark.AppHost\\setup-nginx-cert.ps1");
    Console.WriteLine("Continuing without HTTPS support...");
}

// nginx container to serve the Blazor WASM static files with HTTPS support
var webFrontend = builder.AddContainer("webfrontend", "nginx", "latest")
    .WithBindMount(publishOutputPath, "/usr/share/nginx/html", isReadOnly: false)
    .WithBindMount(nginxConfigPath, "/etc/nginx/nginx.conf", isReadOnly: true)
    .WithBindMount(nginxCertsPath, "/etc/nginx/certs", isReadOnly: true)
    .WithHttpEndpoint(port:5173, targetPort: 80, name: "http")
    .WithHttpsEndpoint(port: 7173, targetPort: 443, name: "https")
    .WithExternalHttpEndpoints()
    .WaitFor(apiService);

var host = builder.Build();

Console.WriteLine($"Aspire orchestration configured for {environmentName} environment");
Console.WriteLine($"API Service: {GetMinReplicas(environmentName, "ApiService")} replica(s)");

host.Run();

// ============================================================================
// Helper Methods for Environment-Specific Configuration
// ============================================================================

static int GetMinReplicas(string environment, string service)
{
    return environment switch
    {
        "Development" => 1,
        "Test" => 1,
        "Staging" => 1,
        "Production" => 2,
        _ => 1
    };
}

static IResourceBuilder<ProjectResource> ApplyResourceLimits(
    IResourceBuilder<ProjectResource> resource,
    string environment,
    string serviceName)
{
    // Resource limits are primarily enforced in Azure Container Apps via Bicep
    // This method is a placeholder for any Aspire-specific resource configuration
    // that might be needed for local testing or other orchestration scenarios
    return resource;
}
