namespace DataBridge.Infrastructure.Files;

internal static class HeaderNormalizer
{
    public static string Normalize(string value) =>
        value.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
}
