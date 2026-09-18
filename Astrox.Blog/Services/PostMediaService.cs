using System.Text.RegularExpressions;
using Astrox.Blog.Models;
using Microsoft.Extensions.Options;

namespace Astrox.Blog.Services;

/// <summary>
/// 文章 Markdown 内图片（Admin / API multipart）：
/// 外链与已托管路径跳过；相对路径拷贝到 MediaRoot/posts/{slug}/，
/// 改写为 /media/posts/{slug}/...（与 zip 导入共用 Blog:MediaRoot）。
/// </summary>
public sealed class PostMediaService
{
    public const string PostsSubfolder = "posts";

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
        // 不包含 .svg，避免作为静态文件直接打开时的 XSS 风险
    };

    private static readonly Regex MarkdownImageRegex = new(
        @"!\[(?<alt>[^\]]*)\]\((?<url><[^>]+>|[^)\s]+)(?:\s+(?:""[^""]*""|'[^']*'))?\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlImgSrcRegex = new(
        @"<img\b[^>]*?\bsrc\s*=\s*(?:""(?<url>[^""]+)""|'(?<url>[^']+)'|(?<url>[^\s>]+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IWebHostEnvironment _env;
    private readonly IOptions<BlogOptions> _options;
    private readonly ILogger<PostMediaService> _logger;

    public PostMediaService(
        IWebHostEnvironment env,
        IOptions<BlogOptions> options,
        ILogger<PostMediaService> logger)
    {
        _env = env;
        _options = options;
        _logger = logger;
    }

    /// <summary>物理媒体根目录（与 zip 导入相同），对应 URL 前缀 /media</summary>
    public string GetMediaRoot()
    {
        var root = MediaRootResolver.Resolve(_options.Value.MediaRoot, _env.ContentRootPath);
        Directory.CreateDirectory(root);
        return root;
    }

    public string GetPostMediaDirectory(string slug)
    {
        var safeSlug = SanitizePathSegment(slug);
        var dir = Path.Combine(GetMediaRoot(), PostsSubfolder, safeSlug);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string ToPublicUrl(string slug, string safeFileName) =>
        $"{MediaRootResolver.RequestPath}/{PostsSubfolder}/{SanitizePathSegment(slug)}/{safeFileName}";

    public string SaveUpload(string slug, Stream content, string originalFileName)
    {
        var safeName = SanitizeFileName(originalFileName);
        if (!IsAllowedImageExtension(safeName))
            throw new InvalidOperationException($"不支持的图片类型：{Path.GetExtension(safeName)}（允许 png/jpg/jpeg/gif/webp）");

        var dest = Path.Combine(GetPostMediaDirectory(slug), safeName);
        using (var fs = File.Create(dest))
            content.CopyTo(fs);

        _logger.LogInformation("已保存文章图片 {Slug}/{File} → {Path}", slug, safeName, dest);
        return ToPublicUrl(slug, safeName);
    }

    public async Task<string> SaveUploadAsync(string slug, IFormFile file, CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream();
        var safeName = SanitizeFileName(file.FileName);
        if (!IsAllowedImageExtension(safeName))
            throw new InvalidOperationException($"不支持的图片类型：{Path.GetExtension(safeName)}（允许 png/jpg/jpeg/gif/webp）");

        var dest = Path.Combine(GetPostMediaDirectory(slug), safeName);
        await using (var fs = File.Create(dest))
            await stream.CopyToAsync(fs, ct);

        _logger.LogInformation("已保存上传图片 {Slug}/{File}", slug, safeName);
        return ToPublicUrl(slug, safeName);
    }

    /// <summary>
    /// 扫描 Markdown，将相对路径图片拷贝到媒体目录并改写链接。外链与已托管路径跳过。
    /// </summary>
    public MarkdownImageProcessResult ProcessMarkdown(
        string markdown,
        string slug,
        IReadOnlyDictionary<string, Func<Stream>>? uploadedByName = null,
        IEnumerable<string>? extraSearchRoots = null)
    {
        if (string.IsNullOrEmpty(markdown))
            return new MarkdownImageProcessResult(markdown, Array.Empty<string>(), Array.Empty<string>());

        var warnings = new List<string>();
        var rewritten = new List<string>();
        var searchRoots = BuildSearchRoots(extraSearchRoots);
        var uploadIndex = BuildUploadIndex(uploadedByName);

        string RewriteUrl(string rawUrl)
        {
            var url = UnwrapUrl(rawUrl).Trim();
            if (string.IsNullOrWhiteSpace(url))
                return rawUrl;

            if (IsExternalUrl(url))
                return rawUrl;

            if (IsAlreadyHostedSitePath(url, slug))
                return rawUrl;

            if (url.StartsWith('/'))
                return rawUrl;

            if (!TryGetExtension(url, out var ext) || !AllowedExtensions.Contains(ext))
            {
                warnings.Add($"跳过非图片或未允许扩展名的相对路径：{url}");
                return rawUrl;
            }

            var safeFileName = SanitizeFileName(Path.GetFileName(url.Replace('\\', '/')));
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName is "." or "..")
            {
                warnings.Add($"无法安全化文件名，已保留原链接：{url}");
                return rawUrl;
            }

            var destDir = GetPostMediaDirectory(slug);
            var destPath = Path.Combine(destDir, safeFileName);
            var publicUrl = ToPublicUrl(slug, safeFileName);

            if (File.Exists(destPath))
            {
                if (!string.Equals(url, publicUrl, StringComparison.OrdinalIgnoreCase))
                    rewritten.Add($"{url} → {publicUrl}（已存在）");
                return publicUrl;
            }

            if (TryResolveSource(url, uploadIndex, searchRoots, out var sourcePath, out var openStream))
            {
                try
                {
                    if (openStream is not null)
                    {
                        using var src = openStream();
                        using var fs = File.Create(destPath);
                        src.CopyTo(fs);
                    }
                    else if (sourcePath is not null)
                    {
                        File.Copy(sourcePath, destPath, overwrite: true);
                    }

                    rewritten.Add($"{url} → {publicUrl}");
                    _logger.LogInformation("文章图片已托管 {Slug}: {From} → {To}", slug, url, publicUrl);
                    return publicUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "拷贝图片失败 {Url} → {Dest}", url, destPath);
                    warnings.Add($"拷贝失败，已保留原链接：{url}（{ex.Message}）");
                    return rawUrl;
                }
            }

            warnings.Add($"找不到相对路径图片文件，已保留原链接：{url}");
            _logger.LogWarning("无法解析文章相对图片路径：slug={Slug} url={Url}", slug, url);
            return rawUrl;
        }

        var result = MarkdownImageRegex.Replace(markdown, m =>
        {
            var original = m.Groups["url"].Value;
            var replacement = RewriteUrl(original);
            if (string.Equals(UnwrapUrl(original).Trim(), replacement, StringComparison.Ordinal))
                return m.Value;
            return m.Value.Replace(original, WrapLike(original, replacement), StringComparison.Ordinal);
        });

        result = HtmlImgSrcRegex.Replace(result, m =>
        {
            var original = m.Groups["url"].Value;
            var replacement = RewriteUrl(original);
            if (string.Equals(original.Trim(), replacement, StringComparison.Ordinal))
                return m.Value;
            return m.Value.Replace(original, replacement, StringComparison.Ordinal);
        });

        return new MarkdownImageProcessResult(result, rewritten, warnings);
    }

    public static Dictionary<string, Func<Stream>> IndexFormFiles(IEnumerable<IFormFile> files)
    {
        var dict = new Dictionary<string, Func<Stream>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (file.Length <= 0) continue;
            if (!IsAllowedImageExtension(file.FileName) && !IsAllowedImageExtension(file.Name))
                continue;

            void Add(string key)
            {
                if (string.IsNullOrWhiteSpace(key)) return;
                key = key.Replace('\\', '/').Trim();
                if (key.StartsWith("./", StringComparison.Ordinal))
                    key = key[2..];
                dict[key] = () => file.OpenReadStream();
                var baseName = Path.GetFileName(key.Replace('\\', '/'));
                if (!string.IsNullOrWhiteSpace(baseName))
                    dict[baseName] = () => file.OpenReadStream();
            }

            Add(file.FileName);
            Add(file.Name);
        }

        return dict;
    }

    public static bool IsAllowedImageExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name))
            return "image.png";

        name = name.Replace("..", "", StringComparison.Ordinal);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        name = name.Trim('.', ' ', '\t');
        if (string.IsNullOrWhiteSpace(name))
            return "image.png";
        return name;
    }

    public static string SanitizePathSegment(string slug)
    {
        var s = SlugHelper.Normalize(slug, slug);
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '-');
        return string.IsNullOrWhiteSpace(s) ? "post" : s;
    }

    public static bool IsExternalUrl(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeFtp || uri.Scheme == "data");
    }

    private bool IsAlreadyHostedSitePath(string url, string slug)
    {
        if (!url.StartsWith('/'))
            return false;

        var postsPrefix = $"{MediaRootResolver.RequestPath}/{PostsSubfolder}/{SanitizePathSegment(slug)}/";
        if (url.StartsWith(postsPrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        // zip 导入与其它 /media/...、以及仓库内 wwwroot /images/...
        if (url.StartsWith(MediaRootResolver.RequestPath + "/", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private List<string> BuildSearchRoots(IEnumerable<string>? extra)
    {
        var roots = new List<string>();
        void Add(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return;
            var full = Path.GetFullPath(p);
            if (Directory.Exists(full) && !roots.Contains(full, StringComparer.OrdinalIgnoreCase))
                roots.Add(full);
        }

        Add(_env.ContentRootPath);
        Add(Path.Combine(_env.ContentRootPath, "Docs"));
        Add(Path.Combine(_env.ContentRootPath, "..", "Docs"));
        Add(_env.WebRootPath);
        Add(Path.Combine(_env.WebRootPath ?? "", "images"));
        if (extra is not null)
        {
            foreach (var e in extra)
                Add(e);
        }

        return roots;
    }

    private static Dictionary<string, Func<Stream>> BuildUploadIndex(
        IReadOnlyDictionary<string, Func<Stream>>? uploadedByName)
    {
        var dict = new Dictionary<string, Func<Stream>>(StringComparer.OrdinalIgnoreCase);
        if (uploadedByName is null) return dict;
        foreach (var (key, value) in uploadedByName)
        {
            var k = key.Replace('\\', '/').Trim();
            if (k.StartsWith("./", StringComparison.Ordinal))
                k = k[2..];
            dict[k] = value;
            var baseName = Path.GetFileName(k);
            if (!string.IsNullOrWhiteSpace(baseName))
                dict[baseName] = value;
        }

        return dict;
    }

    private static bool TryResolveSource(
        string relativeUrl,
        IReadOnlyDictionary<string, Func<Stream>> uploads,
        IReadOnlyList<string> searchRoots,
        out string? sourcePath,
        out Func<Stream>? openStream)
    {
        sourcePath = null;
        openStream = null;

        var normalized = relativeUrl.Replace('\\', '/').Trim();
        if (normalized.StartsWith("./", StringComparison.Ordinal))
            normalized = normalized[2..];

        if (normalized.Length >= 2 && normalized[1] == ':')
            return false;

        if (uploads.TryGetValue(normalized, out var byPath))
        {
            openStream = byPath;
            return true;
        }

        var baseName = Path.GetFileName(normalized);
        if (!string.IsNullOrEmpty(baseName) && uploads.TryGetValue(baseName, out var byBase))
        {
            openStream = byBase;
            return true;
        }

        foreach (var root in searchRoots)
        {
            var candidate = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                continue;
            if (File.Exists(candidate))
            {
                sourcePath = candidate;
                return true;
            }
        }

        if (!string.IsNullOrEmpty(baseName))
        {
            foreach (var root in searchRoots)
            {
                var direct = Path.Combine(root, baseName);
                if (File.Exists(direct))
                {
                    sourcePath = direct;
                    return true;
                }

                try
                {
                    foreach (var sub in Directory.EnumerateDirectories(root))
                    {
                        var hit = Path.Combine(sub, baseName);
                        if (File.Exists(hit))
                        {
                            sourcePath = hit;
                            return true;
                        }
                    }
                }
                catch (IOException)
                {
                    // ignore
                }
            }
        }

        return false;
    }

    private static string UnwrapUrl(string url)
    {
        url = url.Trim();
        if (url.Length >= 2 && url[0] == '<' && url[^1] == '>')
            return url[1..^1].Trim();
        return url;
    }

    private static string WrapLike(string original, string replacement)
    {
        var trimmed = original.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '<' && trimmed[^1] == '>')
            return $"<{replacement}>";
        return replacement;
    }

    private static bool TryGetExtension(string url, out string ext)
    {
        var path = url.Split('?', 2)[0].Split('#', 2)[0];
        ext = Path.GetExtension(path.Replace('\\', '/'));
        return !string.IsNullOrEmpty(ext);
    }
}

public sealed record MarkdownImageProcessResult(
    string Markdown,
    IReadOnlyList<string> Rewritten,
    IReadOnlyList<string> Warnings);
