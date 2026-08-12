using System.Security.Cryptography;
using System.Text;

namespace DevContentEngine.Worker.Hangfire;

internal static class BasicAuthCredentialChecker
{
    private const string SchemePrefix = "Basic ";

    public static bool Matches(string? authorizationHeader, string expectedUsername, string expectedPassword)
    {
        if (string.IsNullOrEmpty(authorizationHeader) ||
            !authorizationHeader.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;

        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader[SchemePrefix.Length..].Trim()));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');

        if (separatorIndex < 0)
        {
            return false;
        }

        var usernameMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(decoded[..separatorIndex]),
            Encoding.UTF8.GetBytes(expectedUsername));

        var passwordMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(decoded[(separatorIndex + 1)..]),
            Encoding.UTF8.GetBytes(expectedPassword));

        return usernameMatches && passwordMatches;
    }
}
