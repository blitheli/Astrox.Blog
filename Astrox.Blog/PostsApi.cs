using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
            });

        group.MapGet("/", ListAsync);
        group.MapGet("/{slug}", GetBySlugAsync);
        group.MapPost("/", CreateAsync);
        group.MapPost("/from-zip", ImportFromZipAsync)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(PostZipImporter.MaxZipBytes));
        group.MapPut("/{slug}", UpdateAsync);
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
        [FromBody] ApiPostRequest request,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PostsApi");
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Markdown))
            return Results.BadRequest(new { error = "title 与 markdown 为必填项" });

        var slug = SlugHelper.Normalize(request.Slug, request.Title);
        if (await db.Posts.AnyAsync(p => p.Slug == slug))
        {
            logger.LogWarning("API 创建文章冲突，slug 已存在：{Slug}", slug);
            return Results.Conflict(new { error = $"slug 已存在：{slug}" });
        }

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Title = request.Title.Trim(),
            Slug = slug,
            Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim(),
            Markdown = request.Markdown,
            Tags = request.NormalizedTags(),
            IsPublished = request.Publish,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = request.Publish ? now : null
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
        [FromBody] ApiPostRequest request,
        ApplicationDbContext db,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PostsApi");
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Slug == slug);
        if (post is null)
        {
            logger.LogWarning("API 更新文章不存在：{Slug}", slug);
            return Results.NotFound(new { error = "文章不存在" });
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Markdown))
            return Results.BadRequest(new { error = "title 与 markdown 为必填项" });

        var newSlug = SlugHelper.Normalize(request.Slug ?? slug, request.Title);
        if (newSlug != post.Slug && await db.Posts.AnyAsync(p => p.Slug == newSlug))
            return Results.Conflict(new { error = $"slug 已存在：{newSlug}" });

        var wasPublished = post.IsPublished;
        post.Title = request.Title.Trim();
        post.Slug = newSlug;
        post.Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary.Trim();
        post.Markdown = request.Markdown;
        post.Tags = request.NormalizedTags();
        post.IsPublished = request.Publish;
        post.UpdatedAt = DateTime.UtcNow;
        if (request.Publish && !wasPublished)
            post.PublishedAt = DateTime.UtcNow;
        else if (!request.Publish)
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
}
