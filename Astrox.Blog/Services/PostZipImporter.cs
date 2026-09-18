using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Astrox.Blog.Services;

public sealed class PostZipImportException : Exception
{
    public PostZipImportException(string message) : base(message)
    {
    }
}

public sealed class PostZipImportResult
{
    public required string MediaFolder { get; init; }
    public required IReadOnlyList<ImportedMarkdownPost> Posts { get; init; }
    public required IReadOnlyList<string> SavedImages { get; init; }
}

public sealed class ImportedMarkdownPost
{
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Markdown { get; set; }
    public string? Summary { get; set; }
}

public sealed partial class PostZipImporter
{
    public const long MaxZipBytes = 20 * 1024 * 1024;
    public const long MaxUncompressedBytes = 80 * 1024 * 1024;
    public const int MaxEntries = 100;
    public const long MaxEntryBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".bmp"
    };

    private readonly string _mediaRoot;

    public PostZipImporter(string mediaRoot)
    {
        _mediaRoot = Path.GetFullPath(mediaRoot);
    }

    public PostZipImportResult Import(Stream zipStream, string zipFileName)
    {
        if (zipStream.CanSeek && zipStream.Length > MaxZipBytes)
            throw new PostZipImportException($"zip 不能超过 {MaxZipBytes / (1024 * 1024)} MB");

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries
            .Where(e => !IsDirectory(e) && !IsJunk(e.FullName))
            .ToList();

        if (entries.Count == 0)
            throw new PostZipImportException("zip 中没有可用文件");
        if (entries.Count > MaxEntries)
            throw new PostZipImportException($"zip 内文件过多（最多 {MaxEntries} 个）");

        long uncompressed = 0;
        var normalized = new List<(ZipArchiveEntry Entry, string Path)>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry.Length > MaxEntryBytes)
                throw new PostZipImportException($"文件过大：{entry.Name}");
            uncompressed += entry.Length;
            if (uncompressed > MaxUncompressedBytes)
                throw new PostZipImportException("zip 解压后体积过大");

            var path = NormalizeEntryPath(entry.FullName)
                       ?? throw new PostZipImportException("zip 含有非法路径");
            normalized.Add((entry, path));
        }

        var packageFolder = DetectPackageFolder(normalized.Select(x => x.Path), zipFileName);
        var markdownEntries = normalized
            .Where(x => x.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (markdownEntries.Count == 0)
            throw new PostZipImportException("zip 中未找到 Markdown（.md）文件");

        var destFolder = Path.Combine(_mediaRoot, packageFolder);
        Directory.CreateDirectory(destFolder);
        var destPrefix = destFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;

        var saved = new List<string>();
        foreach (var (entry, path) in normalized.Where(x => ImageExtensions.Contains(Path.GetExtension(x.Path))))
        {
            var relative = StripPackagePrefix(path, packageFolder);
            var destPath = Path.GetFullPath(Path.Combine(destFolder, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!destPath.StartsWith(destPrefix, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(destPath, destFolder, StringComparison.OrdinalIgnoreCase))
                throw new PostZipImportException("zip 含有非法路径");

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            using var input = entry.Open();
            using var output = File.Create(destPath);
            input.CopyTo(output);
            saved.Add($"{MediaRootResolver.RequestPath}/{packageFolder}/{relative.Replace('\\', '/')}");
        }

        var posts = new List<ImportedMarkdownPost>(markdownEntries.Count);
        foreach (var (entry, path) in markdownEntries)
        {
            var relative = StripPackagePrefix(path, packageFolder);
            var markdownDirectory = Path.GetDirectoryName(relative)?.Replace('\\', '/') ?? string.Empty;
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            var fileTitle = Path.GetFileNameWithoutExtension(relative);
            posts.Add(new ImportedMarkdownPost
            {
                Title = ExtractTitle(text) ?? fileTitle,
                Slug = SlugHelper.FromTitle(fileTitle),
                Markdown = MarkdownImageRewriter.Rewrite(text, packageFolder, markdownDirectory),
                Summary = ExtractSummary(text)
            });
        }

        return new PostZipImportResult
        {
            MediaFolder = packageFolder,
            Posts = posts,
            SavedImages = saved
        };
    }

    internal static string DetectPackageFolder(IEnumerable<string> paths, string zipFileName)
    {
        var list = paths.ToList();
        var firstSegments = list
            .Select(p => p.Split('/', StringSplitOptions.RemoveEmptyEntries)[0])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (firstSegments.Count == 1 && list.All(p => p.Contains('/')))
            return SanitizeFolderName(firstSegments[0]);

        return SanitizeFolderName(Path.GetFileNameWithoutExtension(zipFileName));
    }

    internal static string? NormalizeEntryPath(string fullName)
    {
        var name = fullName.Replace('\\', '/').Trim();
        if (name.Length == 0 || name.StartsWith('/') || name.Contains(':'))
            return null;

        var parts = name.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts.Any(p => p is "." or ".."))
            return null;

        return string.Join('/', parts);
    }

    internal static string StripPackagePrefix(string path, string packageFolder)
    {
        var prefix = packageFolder + "/";
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path[prefix.Length..]
            : path;
    }

    internal static string SanitizeFolderName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "post";

        var cleaned = string.Concat(name.Trim().Where(c =>
            !Path.GetInvalidFileNameChars().Contains(c) && c is not '/' and not '\\'));
        cleaned = cleaned.Replace("..", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(cleaned) ? "post" : cleaned;
    }

    private static bool IsDirectory(ZipArchiveEntry entry) =>
        entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\') || entry.Name.Length == 0;

    private static bool IsJunk(string fullName)
    {
        var name = fullName.Replace('\\', '/');
        if (name.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase)
            || name.Contains("/__MACOSX/", StringComparison.OrdinalIgnoreCase))
            return true;

        var file = Path.GetFileName(name);
        return file is ".DS_Store" or "Thumbs.db"
               || file.StartsWith("._", StringComparison.Ordinal);
    }

    private static string? ExtractTitle(string markdown)
    {
        using var reader = new StringReader(StripFrontMatter(markdown));
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var match = HeadingLine().Match(line);
            return match.Success ? StripHeadingDecor(match.Groups[1].Value) : null;
        }

        return null;
    }

    private static string? ExtractSummary(string markdown)
    {
        using var reader = new StringReader(StripFrontMatter(markdown));
        var skippedHeading = false;
        var buffer = new StringBuilder();
        while (reader.ReadLine() is { } line)
        {
            if (!skippedHeading && HeadingLine().IsMatch(line))
            {
                skippedHeading = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (buffer.Length > 0)
                    break;
                continue;
            }

            if (buffer.Length > 0)
                buffer.Append(' ');
            buffer.Append(line.Trim());
        }

        var summary = buffer.ToString().Trim();
        if (summary.Length == 0)
            return null;
        return summary.Length <= 500 ? summary : summary[..500];
    }

    private static string StripFrontMatter(string markdown)
    {
        if (!markdown.StartsWith("---", StringComparison.Ordinal))
            return markdown;

        var end = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
            return markdown;
        var after = end + 4;
        if (after < markdown.Length && markdown[after] == '\n')
            after++;
        return after < markdown.Length ? markdown[after..] : string.Empty;
    }

    private static string StripHeadingDecor(string title) =>
        title.Trim().Trim('*').Trim().Trim('_').Trim();

    [GeneratedRegex(@"^#{1,6}\s+(.+?)\s*#*\s*$")]
    private static partial Regex HeadingLine();
}
