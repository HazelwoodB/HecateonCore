using System.Security.Cryptography;
using System.Text;

namespace Hecateon.Infrastructure;

public static class LogRedaction
{
    public static string RedactIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        if (value.Length <= 8)
        {
            return "***";
        }

        return $"{value[..4]}...{value[^4..]}";
    }

    public static string Sha256Prefix(string? value, int length = 12)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var hex = Convert.ToHexString(bytes);
        return hex[..Math.Min(length, hex.Length)];
    }
}