using fencemark.ApiService.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace fencemark.Tests;

public class LoggingHelperTests
{
    [Fact]
    public void MaskConnectionString_WithPassword_MasksPassword()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=fencemark;User Id=sa;Password=MyP@ssw0rd;TrustServerCertificate=True;";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("MyP@ssw0rd", masked);
        Assert.Contains("Password=***;", masked);
    }

    [Fact]
    public void MaskConnectionString_WithPwd_MasksPwd()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=fencemark;uid=sa;pwd=MySecret123;";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("MySecret123", masked);
        Assert.Contains("pwd=***;", masked);
    }

    [Fact]
    public void MaskConnectionString_WithUserId_MasksUserId()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=fencemark;User Id=adminuser;Password=pass123;";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("adminuser", masked);
        Assert.Contains("User Id=***;", masked);
    }

    [Fact]
    public void MaskConnectionString_WithUid_MasksUid()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=fencemark;uid=dbadmin;pwd=secret;";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("dbadmin", masked);
        Assert.Contains("uid=***;", masked);
    }

    [Fact]
    public void MaskConnectionString_WithTrustedConnection_DoesNotModify()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=fencemark;Trusted_Connection=True;TrustServerCertificate=True;";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.Equal(connectionString, masked);
    }

    [Fact]
    public void MaskConnectionString_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var connectionString = "";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.Equal("[empty]", masked);
    }

    [Fact]
    public void MaskConnectionString_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? connectionString = null;

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.Equal("[empty]", masked);
    }

    [Fact]
    public void GetLoggingLevel_WithConfiguredValue_ReturnsValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Logging:LoggingLevel", "Error" }
            })
            .Build();

        // Act
        var level = LoggingHelper.GetLoggingLevel(configuration);

        // Assert
        Assert.Equal("Error", level);
    }

    [Fact]
    public void GetLoggingLevel_WithoutConfiguredValue_ReturnsVerbose()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var level = LoggingHelper.GetLoggingLevel(configuration);

        // Assert
        Assert.Equal("Verbose", level);
    }

    [Fact]
    public void GetLoggingLevel_WithNullConfiguration_ReturnsVerbose()
    {
        // Act
        var level = LoggingHelper.GetLoggingLevel(null);

        // Assert
        Assert.Equal("Verbose", level);
    }

    [Theory]
    [InlineData("Verbose", true)]
    [InlineData("verbose", true)]
    [InlineData("VERBOSE", true)]
    [InlineData("Error", false)]
    [InlineData("Warning", false)]
    [InlineData("Information", false)]
    public void IsVerboseLoggingEnabled_WithVariousLevels_ReturnsExpected(string loggingLevel, bool expected)
    {
        // Act
        var result = LoggingHelper.IsVerboseLoggingEnabled(loggingLevel);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsVerboseLoggingEnabled_WithNull_ReturnsTrue()
    {
        // Act
        var result = LoggingHelper.IsVerboseLoggingEnabled(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsVerboseLoggingEnabled_WithEmptyString_ReturnsTrue()
    {
        // Act
        var result = LoggingHelper.IsVerboseLoggingEnabled("");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MaskConnectionString_WithComplexConnectionString_MasksAllSecrets()
    {
        // Arrange
        var connectionString = "Server=myserver.database.windows.net;Database=mydb;User Id=myuser@myserver;Password=C0mpl3x!P@ssw0rd;Encrypt=True;";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("myuser@myserver", masked);
        Assert.DoesNotContain("C0mpl3x!P@ssw0rd", masked);
        Assert.Contains("User Id=***;", masked);
        Assert.Contains("Password=***;", masked);
        Assert.Contains("Server=myserver.database.windows.net", masked);
        Assert.Contains("Database=mydb", masked);
    }

    [Fact]
    public void MaskConnectionString_WithoutTrailingSemicolon_MasksPassword()
    {
        // Arrange - Last parameter has no trailing semicolon
        var connectionString = "Server=localhost;Database=fencemark;User Id=sa;Password=MyP@ssw0rd";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("MyP@ssw0rd", masked);
        Assert.DoesNotContain("sa", masked);
        Assert.Contains("Password=***", masked);
        Assert.Contains("User Id=***", masked);
    }

    [Fact]
    public void MaskConnectionString_WithPasswordAtEnd_MasksCorrectly()
    {
        // Arrange - Password is the last parameter without semicolon
        var connectionString = "Server=localhost;Database=fencemark;Password=Secret123!";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("Secret123!", masked);
        Assert.Contains("Password=***", masked);
        Assert.Contains("Server=localhost", masked);
        Assert.Contains("Database=fencemark", masked);
    }

    [Fact]
    public void MaskConnectionString_MixedCaseParameters_MasksCorrectly()
    {
        // Arrange - Test case insensitivity for matching
        var connectionString = "SERVER=localhost;DATABASE=fencemark;PASSWORD=Test123;USER ID=admin";

        // Act
        var masked = LoggingHelper.MaskConnectionString(connectionString);

        // Assert
        Assert.DoesNotContain("Test123", masked);
        Assert.DoesNotContain("admin", masked);
        // Note: Replacement normalizes to standard case
        Assert.Contains("Password=***", masked);
        Assert.Contains("User Id=***", masked);
    }
}
