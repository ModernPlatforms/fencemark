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
    public static string GetLoggingLevel(IConfiguration? configuration)
    {
        if (configuration == null)
        {
            return "Verbose";
        }
        
        return configuration.GetValue<string>("Logging:LoggingLevel") ?? "Verbose";
    }

    /// <summary>
    /// Determines if verbose logging is enabled based on the logging level
    /// </summary>
    public static bool IsVerboseLoggingEnabled(string? loggingLevel)
    {
        if (string.IsNullOrEmpty(loggingLevel))
        {
            return true; // Default to verbose if not specified
        }
        
        return loggingLevel.Equals("Verbose", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Masks sensitive data in a connection string for safe logging
    /// </summary>
    public static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "[empty]";
        }

        // Mask password values in connection string
        var masked = PasswordRegex().Replace(connectionString, "Password=***$1");
        masked = PwdRegex().Replace(masked, "pwd=***$1");
        
        // Mask User ID values
        masked = UserIdRegex().Replace(masked, "User Id=***$1");
        masked = UidRegex().Replace(masked, "uid=***$1");
        
        // Mask any security tokens or keys
        masked = SecurityTokenRegex().Replace(masked, "Security Token=***$1");
        
        return masked;
    }

    // Regex patterns for matching sensitive connection string parts
    // Capture optional trailing semicolon to preserve connection string format
    [GeneratedRegex(@"Password=[^;]*(;?)", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();

    [GeneratedRegex(@"pwd=[^;]*(;?)", RegexOptions.IgnoreCase)]
    private static partial Regex PwdRegex();

    [GeneratedRegex(@"User Id=[^;]*(;?)", RegexOptions.IgnoreCase)]
    private static partial Regex UserIdRegex();

    [GeneratedRegex(@"uid=[^;]*(;?)", RegexOptions.IgnoreCase)]
    private static partial Regex UidRegex();

    [GeneratedRegex(@"Security Token=[^;]*(;?)", RegexOptions.IgnoreCase)]
    private static partial Regex SecurityTokenRegex();
}
