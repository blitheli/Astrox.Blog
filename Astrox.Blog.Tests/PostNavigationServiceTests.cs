using Astrox.Blog.Data;
using Astrox.Blog.Models;
using Astrox.Blog.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Tests;

public class PostNavigationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly PostNavigationService _nav;

    public PostNavigationServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
        _nav = new PostNavigationService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Middle_post_has_newer_previous_and_older_next()
    {
        Seed(
            ("旧文", "old", daysAgo: 3),
            ("中间", "mid", daysAgo: 2),
            ("新文", "new", daysAgo: 1));

        var mid = await _db.Posts.SingleAsync(p => p.Slug == "mid");
        var neighbors = await _nav.GetNeighborsAsync(mid.PublishedAt!.Value, mid.Id);

        Assert.NotNull(neighbors.Previous);
        Assert.Equal("new", neighbors.Previous!.Slug);
        Assert.Equal("新文", neighbors.Previous.Title);

        Assert.NotNull(neighbors.Next);
        Assert.Equal("old", neighbors.Next!.Slug);
        Assert.Equal("旧文", neighbors.Next.Title);
    }

    [Fact]
    public async Task Newest_post_has_no_previous()
    {
        Seed(
            ("旧文", "old", daysAgo: 3),
            ("新文", "new", daysAgo: 1));

        var newest = await _db.Posts.SingleAsync(p => p.Slug == "new");
        var neighbors = await _nav.GetNeighborsAsync(newest.PublishedAt!.Value, newest.Id);

        Assert.Null(neighbors.Previous);
        Assert.NotNull(neighbors.Next);
        Assert.Equal("old", neighbors.Next!.Slug);
    }

    [Fact]
    public async Task Oldest_post_has_no_next()
    {
        Seed(
            ("旧文", "old", daysAgo: 3),
            ("新文", "new", daysAgo: 1));

        var oldest = await _db.Posts.SingleAsync(p => p.Slug == "old");
        var neighbors = await _nav.GetNeighborsAsync(oldest.PublishedAt!.Value, oldest.Id);

        Assert.NotNull(neighbors.Previous);
        Assert.Equal("new", neighbors.Previous!.Slug);
        Assert.Null(neighbors.Next);
    }

    [Fact]
    public async Task Ignores_unpublished_posts()
    {
        Seed(
            ("旧文", "old", 3, true),
            ("草稿", "draft", 2, false),
            ("新文", "new", 1, true));

        var newest = await _db.Posts.SingleAsync(p => p.Slug == "new");
        var neighbors = await _nav.GetNeighborsAsync(newest.PublishedAt!.Value, newest.Id);

        Assert.Null(neighbors.Previous);
        Assert.NotNull(neighbors.Next);
        Assert.Equal("old", neighbors.Next!.Slug);
        Assert.DoesNotContain("draft", new[] { neighbors.Previous?.Slug, neighbors.Next?.Slug });
    }

    [Fact]
    public async Task Uses_created_at_when_published_at_is_null()
    {
        var older = new Post
        {
            Title = "靠创建时间",
            Slug = "by-created",
            Markdown = "x",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            PublishedAt = null
        };
        var newer = new Post
        {
            Title = "有发布时间",
            Slug = "by-published",
            Markdown = "y",
            IsPublished = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            PublishedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Posts.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var sortKey = older.PublishedAt ?? older.CreatedAt;
        var neighbors = await _nav.GetNeighborsAsync(sortKey, older.Id);

        Assert.NotNull(neighbors.Previous);
        Assert.Equal("by-published", neighbors.Previous!.Slug);
        Assert.Null(neighbors.Next);
    }

    [Fact]
    public async Task Same_timestamp_uses_higher_id_as_previous()
    {
        var stamp = DateTime.UtcNow.AddDays(-1);
        var first = new Post
        {
            Title = "先入库",
            Slug = "first",
            Markdown = "a",
            IsPublished = true,
            CreatedAt = stamp,
            UpdatedAt = stamp,
            PublishedAt = stamp
        };
        var second = new Post
        {
            Title = "后入库",
            Slug = "second",
            Markdown = "b",
            IsPublished = true,
            CreatedAt = stamp,
            UpdatedAt = stamp,
            PublishedAt = stamp
        };
        _db.Posts.Add(first);
        await _db.SaveChangesAsync();
        _db.Posts.Add(second);
        await _db.SaveChangesAsync();

        Assert.True(second.Id > first.Id);

        var neighbors = await _nav.GetNeighborsAsync(stamp, first.Id);

        Assert.NotNull(neighbors.Previous);
        Assert.Equal("second", neighbors.Previous!.Slug);
        Assert.Null(neighbors.Next);
    }

    [Fact]
    public async Task Query_does_not_require_loading_markdown_bodies()
    {
        Seed(
            ("旧文", "old", daysAgo: 3),
            ("中间", "mid", daysAgo: 2),
            ("新文", "new", daysAgo: 1));

        // 用新 DbContext 仅调用导航查询，验证 Select 投影可独立执行
        await using var db2 = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        var nav2 = new PostNavigationService(db2);
        var mid = await db2.Posts.AsNoTracking().SingleAsync(p => p.Slug == "mid");
        var neighbors = await nav2.GetNeighborsAsync(mid.PublishedAt!.Value, mid.Id);

        Assert.Equal("new", neighbors.Previous!.Slug);
        Assert.Equal("old", neighbors.Next!.Slug);
    }

    private void Seed(params (string Title, string Slug, int daysAgo, bool published)[] items)
    {
        foreach (var (title, slug, daysAgo, published) in items)
        {
            var at = DateTime.UtcNow.AddDays(-daysAgo);
            _db.Posts.Add(new Post
            {
                Title = title,
                Slug = slug,
                Markdown = $"# {title}\n\n很长的正文内容，导航查询不应依赖它。",
                IsPublished = published,
                CreatedAt = at,
                UpdatedAt = at,
                PublishedAt = published ? at : null
            });
        }

        _db.SaveChanges();
    }

    private void Seed(params (string Title, string Slug, int daysAgo)[] items)
        => Seed(items.Select(i => (i.Title, i.Slug, i.daysAgo, true)).ToArray());
}
