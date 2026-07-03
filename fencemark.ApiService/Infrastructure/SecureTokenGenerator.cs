using System.Security.Cryptography;

namespace fencemark.ApiService.Infrastructure;

/// <summary>
/// Generates cryptographically secure, URL-safe tokens (e.g. for invitation links),
/// as opposed to GUIDs which are not designed to be unguessable.
/// </summary>
public static class SecureTokenGenerator
{
    private const int DefaultByteLength = 32;

    /// <summary>
    /// Generates a cryptographically secure, URL-safe (base64url, unpadded) token.
    /// </summary>
    /// <param name="byteLength">Number of random bytes of entropy backing the token.</param>
    public static string Generate(int byteLength = DefaultByteLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(byteLength, 0);

        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
