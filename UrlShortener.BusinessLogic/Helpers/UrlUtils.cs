namespace UrlShortener.BusinessLogic.Helpers;

public static class UrlUtils
{
    public static bool TryNormalizeHttpUrl(string input, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(input)) return false;

        var s = input.AsSpan().Trim().ToString();

        if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            s = "https://" + s;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        normalized = uri.ToString();
        return true;
    }

    public static bool IsValidAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return false;
        if (alias.Length < 3 || alias.Length > 32) return false;

        foreach (var ch in alias)
        {
            var ok = char.IsLetterOrDigit(ch) || ch == '-' || ch == '_';
            if (!ok) return false;
        }
        return true;
    }
}