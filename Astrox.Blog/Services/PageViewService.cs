using System.Text.RegularExpressions;
using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Astrox.Blog.Services;

/// <summary>
/// 站内轻量访问计数：总 PV + 文章阅读次数。
/// 冷却与爬虫过滤在内存中完成；计数写入 SQLite。
/// </summary>
public sealed class PageViewService
{
    public const string TotalPvKey = "TotalPv";
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(45);

    private static readonly Regex BotUaRegex = new(
        @"bot|crawler|spider|slurp|bingpreview|facebookexternalhit|bytespider|semrush|ahrefs",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly CommentAntiSpamService _antiSpam;
    private readonly ILogger<PageViewService> _logger;

    public PageViewService(
        ApplicationDbContext db,
        IMemoryCache cache,
        CommentAntiSpamService antiSpam,
        ILogger<PageViewService> logger)
    {
        _db = db;
        _cache = cache;
        _antiSpam = antiSpam;
        _logger = logger;
    }

    public async Task<long> GetTotalPvAsync(CancellationToken ct = default)
    {
        var row = await _db.SiteStats.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == TotalPvKey, ct);
        return row?.Value ?? 0;
    }

    public async Task<long> GetPostViewCountAsync(int postId, CancellationToken ct = default)
    {
        var row = await _db.PostViewCounts.AsNoTracking()
            .FirstOrDefaultAsync(v => v.PostId == postId, ct);
        return row?.Count ?? 0;
    }

    /// <summary>
    /// 尝试记录一次公开页访问。冷却期内或疑似爬虫时跳过。
    /// </summary>
    public async Task TryRecordAsync(
        string urlKey,
        string? ip,
        string? userAgent,
        string? postSlug,
        CancellationToken ct = default)
    {
        if (IsLikelyBot(userAgent))
            return;

        var ipHash = _antiSpam.HashIp(ip);
        var cooldownKey = $"pv-cooldown:{ipHash}:{urlKey}";
        if (_cache.TryGetValue(cooldownKey, out _))
            return;

        _cache.Set(cooldownKey, true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Cooldown
        });

        await IncrementSitePvAsync(ct);

        if (!string.IsNullOrWhiteSpace(postSlug))
            await IncrementPostViewAsync(postSlug.Trim(), ct);
    }

    public static bool IsLikelyBot(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return false;
        return BotUaRegex.IsMatch(userAgent);
    }

    private async Task IncrementSitePvAsync(CancellationToken ct)
    {
        var updated = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "SiteStats" SET "Value" = "Value" + 1 WHERE "Key" = {TotalPvKey}""",
            ct);

        if (updated > 0)
            return;

        _db.SiteStats.Add(new SiteStat { Key = TotalPvKey, Value = 1 });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "SiteStats" SET "Value" = "Value" + 1 WHERE "Key" = {TotalPvKey}""",
                ct);
        }
    }

    private async Task IncrementPostViewAsync(string slug, CancellationToken ct)
    {
        var postId = await _db.Posts.AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (postId is null)
            return;

        var updated = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "PostViewCounts" SET "Count" = "Count" + 1 WHERE "PostId" = {postId.Value}""",
            ct);

        if (updated > 0)
            return;

        _db.PostViewCounts.Add(new PostViewCount { PostId = postId.Value, Count = 1 });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE "PostViewCounts" SET "Count" = "Count" + 1 WHERE "PostId" = {postId.Value}""",
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "文章阅读计数写入失败 PostId={PostId}", postId.Value);
        }
    }
}
