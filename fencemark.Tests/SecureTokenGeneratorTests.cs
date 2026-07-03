using System.Text.RegularExpressions;
using fencemark.ApiService.Infrastructure;
using Xunit;

namespace fencemark.Tests;

public class SecureTokenGeneratorTests
{
    [Fact]
    public void Generate_ReturnsNonEmptyToken()
    {
        var token = SecureTokenGenerator.Generate();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_ReturnsUrlSafeCharactersOnly()
    {
        var token = SecureTokenGenerator.Generate();

        Assert.Matches(new Regex("^[A-Za-z0-9_-]+$"), token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }

    [Fact]
    public void Generate_MultipleCalls_ProduceUniqueTokens()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => SecureTokenGenerator.Generate()).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    [Fact]
    public void Generate_DefaultLength_HasMoreEntropyThanAGuid()
    {
        // A GUID has 122 bits of usable entropy (~36 chars incl. hyphens).
        // The default 32-byte token has 256 bits of entropy, encoded as ~43 base64url chars.
        var token = SecureTokenGenerator.Generate();

        Assert.True(token.Length > 36, $"Expected token longer than a GUID's 36 characters, got {token.Length}");
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void Generate_WithCustomByteLength_ScalesTokenLength(int byteLength)
    {
        var token = SecureTokenGenerator.Generate(byteLength);

        // Unpadded base64 length is ceil(byteLength * 4 / 3)
        var expectedLength = (int)Math.Ceiling(byteLength * 4 / 3.0);
        Assert.Equal(expectedLength, token.Length);
    }

    [Fact]
    public void Generate_WithZeroOrNegativeLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureTokenGenerator.Generate(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureTokenGenerator.Generate(-1));
    }

    [Fact]
    public void Generate_DoesNotProduceAGuidFormattedString()
    {
        var token = SecureTokenGenerator.Generate();

        Assert.False(Guid.TryParse(token, out _), "Invitation tokens must not be GUIDs - they are not cryptographically secure.");
    }
}
