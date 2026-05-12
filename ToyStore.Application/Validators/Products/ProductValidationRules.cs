using System.Net;
using System.Text.RegularExpressions;

namespace ToyStore.Application.Validators.Products;

internal static partial class ProductValidationRules
{
    public const int DescriptionMinTextLength = 10;
    public const int DescriptionMaxStorageLength = 1500;
    public const int ImageUrlMaxLength = 500;

    public static string NormalizeRichTextHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed == "<p><br></p>" ? string.Empty : trimmed;
    }

    public static int GetDescriptionTextLength(string? value)
    {
        var normalized = NormalizeRichTextHtml(value);
        if (normalized.Length == 0)
        {
            return 0;
        }

        var withoutTags = HtmlTagRegex().Replace(normalized, string.Empty);
        var decoded = WebUtility.HtmlDecode(withoutTags).Replace('\u00A0', ' ').Trim();
        return decoded.Length;
    }

    public static int GetDescriptionStorageLength(string? value)
    {
        return NormalizeRichTextHtml(value).Length;
    }

    public static bool HasMinimumDescriptionTextLength(string? value)
    {
        return GetDescriptionTextLength(value) >= DescriptionMinTextLength;
    }

    public static bool HasMaximumDescriptionStorageLength(string? value)
    {
        return GetDescriptionStorageLength(value) <= DescriptionMaxStorageLength;
    }

    public static bool HasValidImageUrlLength(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim().Length <= ImageUrlMaxLength;
    }

    public static bool HasUniqueNormalizedUrls(IReadOnlyCollection<string>? urls)
    {
        if (urls == null)
        {
            return true;
        }

        var sanitizedUrls = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .ToList();

        return sanitizedUrls.Count == sanitizedUrls.Distinct(StringComparer.Ordinal).Count();
    }

    [GeneratedRegex("<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
