using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace fencemark.Tests;

public class CorsConfigurationTests
{
    [Fact]
    public void CorsConfiguration_CanBeReadFromSettings()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // Act
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        // Assert
        Assert.NotNull(corsOrigins);
        Assert.NotEmpty(corsOrigins);
        Assert.Contains("https://localhost:5001", corsOrigins);
        Assert.Contains("https://localhost:7001", corsOrigins);
    }

    [Fact]
    public void CorsPolicy_CanBeConfigured()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging services required by CORS
        var corsOrigins = new[] { "https://localhost:5001", "https://localhost:7001" };

        // Act
        services.AddCors(options =>
        {
            options.AddPolicy("WasmClient", policy =>
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var corsService = serviceProvider.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsService>();

        // Assert
        Assert.NotNull(corsService);
    }
}
