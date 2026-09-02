using System.Text.RegularExpressions;

namespace SpyBrowser.Core;

public static partial class IdentityId
{
    private const int MaximumLength = 64;

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumLength ||
            normalized is "." or ".." ||
            !ValidIdentityId().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Identity id must start with a letter or digit and contain only " +
                "lowercase letters, digits, '.', '_' or '-' (maximum 64 characters).",
                nameof(value));
        }

        return normalized;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidIdentityId();
}
