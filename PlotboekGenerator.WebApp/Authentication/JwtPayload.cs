using System.Text.Json;

namespace PlotboekGenerator.WebApp.Authentication;

/// <summary>
/// Reads the payload of a JWT the sign-in handler already validated, to look at a claim it did not
/// copy onto the identity.
/// </summary>
internal static class JwtPayload
{
    /// <summary>Base64url-decodes and parses a JWT payload.</summary>
    /// <param name="token">The raw token, or <c>null</c>.</param>
    /// <returns>The payload, which the caller disposes; <c>null</c> when the token is absent or malformed.</returns>
    public static JsonDocument? Parse(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(Base64UrlDecode(parts[1]));
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var value = input.Replace('-', '+').Replace('_', '/');
        value += (value.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };

        return Convert.FromBase64String(value);
    }
}
