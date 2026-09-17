using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Astrox.Blog.Services;

public static partial class SlugHelper
{
    public static string FromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return $"post-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        var normalized = title.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c is ' ' or '-' or '_' or '.')
            {
                sb.Append('-');
            }
            // keep CJK / other letters already handled by IsLetterOrDigit
        }

        var slug = CollapseDashes().Replace(sb.ToString().Normalize(NormalizationForm.FormC), "-")
            .Trim('-');

        return string.IsNullOrWhiteSpace(slug)
            ? $"post-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
            : slug;
    }

    public static string Normalize(string? slug, string fallbackTitle)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return FromTitle(fallbackTitle);
        return FromTitle(slug.Replace('/', '-'));
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashes();
}
