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
