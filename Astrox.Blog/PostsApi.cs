using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog;

public static class PostsApi
{
    public static RouteGroupBuilder MapPostsApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/posts")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = ApiKeyAuthDefaults.Scheme
            })
            .DisableAntiforgery();

        group.MapGet("/", ListAsync);
        group.MapGet("/{slug}", GetBySlugAsync);
        group.MapPost("/", CreateAsync)
            .WithMetadata(new RequestSizeLimitAttribute(PostZipImporter.MaxZipBytes));
        group.MapPost("/from-zip", ImportFromZipAsync)
            .WithMetadata(new RequestSizeLimitAttribute(PostZipImporter.MaxZipBytes));
        group.MapPut("/{slug}", UpdateAsync)
            .WithMetadata(new RequestSizeLimitAttribute(PostZipImporter.MaxZipBytes));
        group.MapDelete("/{slug}", DeleteAsync);

        return group;
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext db)
    {
        var posts = await db.Posts
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Title,
                p.Slug,
                p.Summary,
                tags = p.Tags,
                p.IsPublished,
                p.CreatedAt,
                p.UpdatedAt,
                p.PublishedAt
            })
            .ToListAsync();
        return Results.Ok(posts);
    }

    private static async Task<IResult> GetBySlugAsync(string slug, ApplicationDbContext db)
    {
        var post = await db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug);
        if (post is null) return Results.NotFound(new { error = "文章不存在" });
        return Results.Ok(ToDto(post));
    }

    private static async Task<IResult> CreateAsync(
        HttpRequest request,
        ApplicationDbContext db,
        PostMediaService media,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PostsApi");
        var parsed = await ParsePostPayloadAsync(request);
        if (parsed.Error is not null)
            return Results.BadRequest(new { error = parsed.Error });

        var body = parsed.Body!;
        if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Markdown))
            return Results.BadRequest(new { error = "title 与 markdown 为必填项" });

        var slug = SoftNormalizeSlug(body.Slug, body.Title);
        if (await db.Posts.AnyAsync(p => p.Slug == slug))
        {
            logger.LogWarning("API 创建文章冲突，slug 已存在：{Slug}", slug);
            return Results.Conflict(new { error = $"slug 已存在：{slug}" });
        }

        var markdown = await ApplyMediaAsync(media, slug, body.Markdown, parsed.Files, logger);

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Title = body.Title.Trim(),
            Slug = slug,
            Summary = string.IsNullOrWhiteSpace(body.Summary) ? null : body.Summary.Trim(),
            Markdown = markdown,
            Tags = body.NormalizedTags(),
            IsPublished = body.Publish,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = body.Publish ? now : null
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync();
        logger.LogInformation("API 已创建文章 {Slug}，发布={Published}", post.Slug, post.IsPublished);
        return Results.Created($"/api/posts/{post.Slug}", ToDto(post));
    }

    private static async Task<IResult> ImportFromZipAsync(
        HttpRequest request,
        ApplicationDbContext db,
        PostZipImporter importer,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PostsApi");
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "请使用 multipart/form-data 上传 zip" });

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file") ?? form.Files.GetFile("zip");
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "请上传 zip 文件（字段名 file）" });
        if (file.Length > PostZipImporter.MaxZipBytes)
            return Results.BadRequest(new { error = $"zip 不能超过 {PostZipImporter.MaxZipBytes / (1024 * 1024)} MB" });

        var fileName = file.FileName ?? string.Empty;
        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "仅支持 .zip 文件" });

        PostZipImportResult imported;
        try
        {
            await using var stream = file.OpenReadStream();
            imported = importer.Import(stream, Path.GetFileName(fileName));
        }
        catch (PostZipImportException ex)
        {
            logger.LogWarning(ex, "zip 导入被拒绝：{File}", fileName);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "zip 文件无效：{File}", fileName);
            return Results.BadRequest(new { error = "不是有效的 zip 文件" });
        }

        var publish = ParseFlag(form["publish"]);
        var tags = NormalizeFormTags(form["tags"]);
        if (imported.Posts.Count == 1)
        {
            var only = imported.Posts[0];
            var titleOverride = NullIfEmpty(form["title"]);
            var slugOverride = NullIfEmpty(form["slug"]);
            var summaryOverride = NullIfEmpty(form["summary"]);
            if (titleOverride is not null)
                only.Title = titleOverride;
            if (slugOverride is not null)
                only.Slug = SlugHelper.Normalize(slugOverride, only.Title);
            if (summaryOverride is not null)
                only.Summary = summaryOverride;
        }

        var created = 0;
        var updated = 0;
        var now = DateTime.UtcNow;
        var savedPosts = new List<Post>(imported.Posts.Count);
        foreach (var incoming in imported.Posts)
        {
            var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == incoming.Slug);
            if (post is null)
            {
                post = new Post
                {
                    Title = incoming.Title.Trim(),
                    Slug = incoming.Slug,
                    Summary = incoming.Summary,
                    Markdown = incoming.Markdown,
                    Tags = tags,
                    IsPublished = publish,
                    CreatedAt = now,
                    UpdatedAt = now,
                    PublishedAt = publish ? now : null
                };
                db.Posts.Add(post);
                created++;
            }
            else
            {
                var wasPublished = post.IsPublished;
                post.Title = incoming.Title.Trim();
                post.Summary = incoming.Summary;
                post.Markdown = incoming.Markdown;
                post.Tags = tags;
                post.IsPublished = publish;
                post.UpdatedAt = now;
                if (publish && !wasPublished)
                    post.PublishedAt = now;
                else if (!publish)
                    post.PublishedAt = null;
                updated++;
            }

            savedPosts.Add(post);
        }

        await db.SaveChangesAsync();
        logger.LogInformation(
            "API 已从 zip 导入 {File}，媒体目录 {Folder}，新建 {Created}，更新 {Updated}，图片 {Images}",
            fileName,
            imported.MediaFolder,
            created,
            updated,
            imported.SavedImages.Count);

        return Results.Ok(new
        {
            mediaFolder = imported.MediaFolder,
            images = imported.SavedImages,
            created,
            updated,
            publish,
            posts = savedPosts.Select(ToDto)
        });
    }

    private static bool ParseFlag(string? raw) =>
        raw is not null
        && raw.Trim() is var value
        && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("on", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase));

    private static string? NullIfEmpty(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

    private static string? NormalizeFormTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : string.Join(',', parts);
    }

    private static async Task<IResult> UpdateAsync(
        string slug,
        HttpRequest request,
        ApplicationDbContext db,
        PostMediaService media,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PostsApi");
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
        if (post is null)
        {
            logger.LogWarning("API 更新文章不存在：{Slug}", slug);
            return Results.NotFound(new { error = "文章不存在" });
        }

        var parsed = await ParsePostPayloadAsync(request);
        if (parsed.Error is not null)
            return Results.BadRequest(new { error = parsed.Error });

        var body = parsed.Body!;
        if (string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Markdown))
            return Results.BadRequest(new { error = "title 与 markdown 为必填项" });

        var newSlug = SoftNormalizeSlug(body.Slug ?? slug, body.Title);
        if (newSlug != post.Slug && await db.Posts.AnyAsync(p => p.Slug == newSlug))
            return Results.Conflict(new { error = $"slug 已存在：{newSlug}" });

        var markdown = await ApplyMediaAsync(media, newSlug, body.Markdown, parsed.Files, logger);

        var wasPublished = post.IsPublished;
        post.Title = body.Title.Trim();
        post.Slug = newSlug;
        post.Summary = string.IsNullOrWhiteSpace(body.Summary) ? null : body.Summary.Trim();
        post.Markdown = markdown;
        post.Tags = body.NormalizedTags();
        post.IsPublished = body.Publish;
        post.UpdatedAt = DateTime.UtcNow;
        if (body.Publish && !wasPublished)
            post.PublishedAt = DateTime.UtcNow;
        else if (!body.Publish)
            post.PublishedAt = null;

        await db.SaveChangesAsync();
        logger.LogInformation("API 已更新文章 {OldSlug} → {Slug}，发布={Published}", slug, post.Slug, post.IsPublished);
        return Results.Ok(ToDto(post));
    }

    private static async Task<IResult> DeleteAsync(string slug, ApplicationDbContext db, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PostsApi");
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
        if (post is null)
        {
            logger.LogWarning("API 删除文章不存在：{Slug}", slug);
            return Results.NotFound(new { error = "文章不存在" });
        }
        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        logger.LogInformation("API 已删除文章 {Slug}", slug);
        return Results.NoContent();
    }

    private static async Task<string> ApplyMediaAsync(
        PostMediaService media,
        string slug,
        string markdown,
        IReadOnlyList<IFormFile> files,
        ILogger logger)
    {
        foreach (var file in files)
        {
            try
            {
                await media.SaveUploadAsync(slug, file);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "API 保存附件图片失败：{Name}", file.FileName);
            }
        }

        var uploads = PostMediaService.IndexFormFiles(files);
        var result = media.ProcessMarkdown(markdown, slug, uploads);
        foreach (var w in result.Warnings)
            logger.LogWarning("API Markdown 图片：{Warning}", w);
        foreach (var r in result.Rewritten)
            logger.LogInformation("API Markdown 图片已改写：{Info}", r);
        return result.Markdown;
    }

    private static async Task<ParsedPostPayload> ParsePostPayloadAsync(HttpRequest request)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            var title = form["title"].ToString();
            var slug = form["slug"].ToString();
            var summary = form["summary"].ToString();
            var markdown = form["markdown"].ToString();
            var tagsRaw = form["tags"].ToString();
            var publishRaw = form["publish"].ToString();

            bool publish = false;
            if (!string.IsNullOrWhiteSpace(publishRaw))
            {
                publish = publishRaw is "1" or "true" or "True" or "on" or "yes" or "Yes";
            }

            object? tags = string.IsNullOrWhiteSpace(tagsRaw) ? null : tagsRaw;
            // tags 也可多次提交：tags=a&tags=b
            var tagValues = form["tags"];
            if (tagValues.Count > 1)
                tags = tagValues.Select(t => t?.ToString()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

            var body = new ApiPostRequest
            {
                Title = title,
                Slug = string.IsNullOrWhiteSpace(slug) ? null : slug,
                Summary = string.IsNullOrWhiteSpace(summary) ? null : summary,
                Markdown = markdown,
                Tags = tags,
                Publish = publish
            };

            var files = form.Files.Where(f => f.Length > 0).ToList();
            return new ParsedPostPayload(body, files, null);
        }

        try
        {
            var body = await request.ReadFromJsonAsync<ApiPostRequest>();
            if (body is null)
                return new ParsedPostPayload(null, Array.Empty<IFormFile>(), "请求体无效");
            return new ParsedPostPayload(body, Array.Empty<IFormFile>(), null);
        }
        catch (Exception)
        {
            return new ParsedPostPayload(null, Array.Empty<IFormFile>(), "无法解析 JSON 请求体；也可使用 multipart/form-data");
        }
    }

    private static string SoftNormalizeSlug(string? slug, string title) =>
        SlugHelper.Normalize(slug, title);

    private static object ToDto(Post p) => new
    {
        p.Id,
        p.Title,
        p.Slug,
        p.Summary,
        markdown = p.Markdown,
        tags = p.Tags,
        publish = p.IsPublished,
        p.CreatedAt,
        p.UpdatedAt,
        p.PublishedAt
    };

    private sealed record ParsedPostPayload(
        ApiPostRequest? Body,
        IReadOnlyList<IFormFile> Files,
        string? Error);
}
