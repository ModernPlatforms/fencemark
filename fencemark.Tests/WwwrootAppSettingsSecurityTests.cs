using Xunit;

namespace fencemark.Tests;

/// <summary>
/// Regression tests for "Sensitive Configuration in wwwroot appsettings" (#111).
///
/// The Blazor WASM bundle (including everything under wwwroot) is delivered to every
/// browser, so nothing checked into fencemark.Client/wwwroot/appsettings*.json can ever
/// be a real secret - the Azure AD tenant/client IDs and API URLs already there are
/// public identifiers by design, visible to anyone via a browser's network tab regardless
/// of git history. This test guards against someone accidentally introducing an actual
/// secret (API key, connection string, client secret) into one of these files in future.
/// </summary>
public class WwwrootAppSettingsSecurityTests
{
    private static readonly string[] ForbiddenKeyFragments =
    [
        "clientsecret",
        "apikey",
        "api_key",
        "password",
        "connectionstring",
        "privatekey",
        "secretkey",
        "accesskey",
        "subscriptionkey"
    ];

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "fencemark.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException(
                $"Could not locate repository root (fencemark.slnx) walking up from {AppContext.BaseDirectory}");
    }

    private static string[] GetWwwrootAppSettingsFiles()
    {
        var wwwrootPath = Path.Combine(FindRepoRoot(), "fencemark.Client", "wwwroot");
        return Directory.GetFiles(wwwrootPath, "appsettings*.json");
    }

    [Fact]
    public void WwwrootAppSettingsFiles_CanBeLocated()
    {
        // Sanity check that the path-walking logic above actually finds the files -
        // if this starts failing, the other test in this file is silently vacuous.
        var files = GetWwwrootAppSettingsFiles();

        Assert.NotEmpty(files);
    }

    [Fact]
    public void WwwrootAppSettingsFiles_ContainNoSecretLikeKeys()
    {
        foreach (var file in GetWwwrootAppSettingsFiles())
        {
            var content = File.ReadAllText(file).ToLowerInvariant();

            foreach (var forbidden in ForbiddenKeyFragments)
            {
                Assert.DoesNotContain(forbidden, content);
            }
        }
    }
}
