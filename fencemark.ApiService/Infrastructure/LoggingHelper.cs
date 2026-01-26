using System.Text.RegularExpressions;

namespace fencemark.ApiService.Infrastructure;

/// <summary>
/// Helper class for secure logging operations
/// </summary>
public static partial class LoggingHelper
{
    /// <summary>
    /// Gets the configured logging level from configuration, defaulting to "Verbose" if not set
    /// </summary>
    public static string GetLoggingLevel(IConfiguration configuration)
    {
        return configuration.GetValue<string>("Logging:LoggingLevel") ?? "Verbose";
    }

    /// <summary>
    /// Determines if verbose logging is enabled based on the logging level
    /// </summary>
    public static bool IsVerboseLoggingEnabled(string loggingLevel)
    {
        return loggingLevel.Equals("Verbose", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Masks sensitive data in a connection string for safe logging
    /// </summary>
    public static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "[empty]";
        }

        // Mask password values in connection string
        var masked = PasswordRegex().Replace(connectionString, "Password=***;");
        masked = PwdRegex().Replace(masked, "pwd=***;");
        
        // Mask User ID values
        masked = UserIdRegex().Replace(masked, "User Id=***;");
        masked = UidRegex().Replace(masked, "uid=***;");
        
        // Mask any security tokens or keys
        masked = SecurityTokenRegex().Replace(masked, "Security Token=***;");
        
        return masked;
    }

    // Regex patterns for matching sensitive connection string parts
    [GeneratedRegex(@"Password=[^;]*;", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();

    [GeneratedRegex(@"pwd=[^;]*;", RegexOptions.IgnoreCase)]
    private static partial Regex PwdRegex();

    [GeneratedRegex(@"User Id=[^;]*;", RegexOptions.IgnoreCase)]
    private static partial Regex UserIdRegex();

    [GeneratedRegex(@"uid=[^;]*;", RegexOptions.IgnoreCase)]
    private static partial Regex UidRegex();

    [GeneratedRegex(@"Security Token=[^;]*;", RegexOptions.IgnoreCase)]
    private static partial Regex SecurityTokenRegex();
}
