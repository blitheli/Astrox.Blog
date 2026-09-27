using Astrox.Blog.Data;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Services;

/// <summary>
/// 按首页列表同一排序规则（PublishedAt ?? CreatedAt 倒序）定位相邻已发布文章。
/// 「上一篇」= 更新的文章；「下一篇」= 更旧的文章。
/// </summary>
public class PostNavigationService
{
    private readonly ApplicationDbContext _db;

    public PostNavigationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PostNeighbors> GetNeighborsAsync(DateTime sortKey, int currentPostId, CancellationToken ct = default)
    {
        var published = _db.Posts.AsNoTracking().Where(p => p.IsPublished);

        // 上一篇：列表中更靠前（更新）的最近一篇
        var previous = await published
            .Where(p => (p.PublishedAt ?? p.CreatedAt) > sortKey
                        || ((p.PublishedAt ?? p.CreatedAt) == sortKey && p.Id > currentPostId))
            .OrderBy(p => p.PublishedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .Select(p => new PostNavLink(p.Slug, p.Title))
            .FirstOrDefaultAsync(ct);

        // 下一篇：列表中更靠后（更旧）的最近一篇
        var next = await published
            .Where(p => (p.PublishedAt ?? p.CreatedAt) < sortKey
                        || ((p.PublishedAt ?? p.CreatedAt) == sortKey && p.Id < currentPostId))
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PostNavLink(p.Slug, p.Title))
            .FirstOrDefaultAsync(ct);

        return new PostNeighbors(previous, next);
    }
}

public sealed record PostNavLink(string Slug, string Title);

public sealed record PostNeighbors(PostNavLink? Previous, PostNavLink? Next);
