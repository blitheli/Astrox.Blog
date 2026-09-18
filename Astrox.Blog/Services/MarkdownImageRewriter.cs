using System.Text.RegularExpressions;

namespace Astrox.Blog.Services;

public static partial class MarkdownImageRewriter
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp"
    };

    public static string Rewrite(string markdown, string mediaFolder, string markdownDirectory = "")
    {
        if (string.IsNullOrEmpty(markdown) || string.IsNullOrWhiteSpace(mediaFolder))
            return markdown ?? string.Empty;

        var withInline = InlineImage().Replace(markdown, match =>
        {
            var rewritten = RewriteUrl(match.Groups["url"].Value, mediaFolder, markdownDirectory);
            if (rewritten is null)
                return match.Value;

            var title = match.Groups["title"].Success ? " " + match.Groups["title"].Value : string.Empty;
            return $"![{match.Groups["alt"].Value}]({rewritten}{title})";
        });

        return ReferenceDefinition().Replace(withInline, match =>
        {
            var url = match.Groups["url"].Value;
            if (!LooksLikeImage(url))
                return match.Value;

            var rewritten = RewriteUrl(url, mediaFolder, markdownDirectory);
            if (rewritten is null)
                return match.Value;

            var rest = match.Groups["rest"].Success ? match.Groups["rest"].Value : string.Empty;
            return $"{match.Groups["id"].Value}: {rewritten}{rest}";
        });
    }

    internal static string? RewriteUrl(string url, string mediaFolder, string markdownDirectory)
    {
        url = url.Trim();
        if (url.Length == 0)
            return null;

        if (url.StartsWith('<') && url.EndsWith('>'))
            url = url[1..^1].Trim();

        if (IsRemoteOrAbsolute(url))
            return null;

        var relative = url.Replace('\\', '/');
        while (relative.StartsWith("./", StringComparison.Ordinal))
            relative = relative[2..];

        if (relative is ".." || relative.StartsWith("../", StringComparison.Ordinal) || relative.Contains("/../", StringComparison.Ordinal))
            return null;

        var fromDoc = CombineRelative(markdownDirectory, relative);
        var folder = mediaFolder.Trim().Replace('\\', '/').Trim('/');
        return $"{MediaRootResolver.RequestPath}/{folder}/{fromDoc}";
    }

    private static bool LooksLikeImage(string url)
    {
        var path = url.Split('?', '#')[0];
        return ImageExtensions.Contains(Path.GetExtension(path));
    }

    private static bool IsRemoteOrAbsolute(string url)
    {
        if (url.StartsWith('/') || url.StartsWith('#') || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Scheme is "http" or "https" or "mailto" or "ftp" or "data";
    }

    private static string CombineRelative(string? directory, string relative)
    {
        var dir = (directory ?? string.Empty).Replace('\\', '/').Trim('/');
        var rel = relative.TrimStart('/');
        return string.IsNullOrEmpty(dir) ? rel : $"{dir}/{rel}";
    }

    [GeneratedRegex(@"!\[(?<alt>[^\]]*)\]\((?<url>[^)\s]+)(?:\s+(?<title>""[^""]*""|'[^']*'))?\)")]
    private static partial Regex InlineImage();

    [GeneratedRegex(@"^(?<id>\[[^\]]+\]):\s*(?<url>\S+)(?<rest>\s+(?:""[^""]*""|'[^']*'))?\s*$", RegexOptions.Multiline)]
    private static partial Regex ReferenceDefinition();
}
