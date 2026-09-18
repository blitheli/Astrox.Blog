using Astrox.Blog.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Astrox.Blog.Data;

public static class DbSeeder
{
    private const string SampleSlug = "welcome-to-astrox-blog";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var options = sp.GetRequiredService<IOptions<BlogOptions>>().Value;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await db.Database.EnsureCreatedAsync();
        await DbSchemaUpgrader.EnsureCommentsTableAsync(db, logger);

        await EnsureOwnerAsync(userManager, options, logger);
        await EnsureSamplePostAsync(db, logger);
        await AlignPostCategoriesAsync(db, options);
    }

    private static async Task EnsureOwnerAsync(
        UserManager<IdentityUser> userManager,
        BlogOptions options,
        ILogger logger)
    {
        var email = options.AdminEmail?.Trim();
        var password = options.AdminPassword;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!userManager.Users.Any())
            {
                logger.LogWarning(
                    "未配置 Blog:AdminEmail / Blog:AdminPassword，且库中尚无用户。请通过环境变量或 appsettings 设置后重启以创建所有者账号。");
            }
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            await SyncOwnerPasswordAsync(userManager, existing, password, email, logger);
            return;
        }

        // 配置邮箱在库中不存在：创建该账号。不删除库中其他用户（单所有者意图下以配置邮箱为准）。
        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            logger.LogInformation("已创建所有者账号：{Email}", email);
        }
        else
        {
            logger.LogError("创建所有者失败：{Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SyncOwnerPasswordAsync(
        UserManager<IdentityUser> userManager,
        IdentityUser user,
        string password,
        string email,
        ILogger logger)
    {
        // 已与配置一致则跳过，避免每次启动无谓改写与日志噪声
        if (await userManager.CheckPasswordAsync(user, password))
            return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (result.Succeeded)
        {
            logger.LogInformation("已同步所有者账号密码：{Email}", email);
        }
        else
        {
            logger.LogError("同步所有者密码失败：{Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task EnsureSamplePostAsync(ApplicationDbContext db, ILogger logger)
    {
        if (await db.Posts.AnyAsync(p => p.Slug == SampleSlug))
            return;

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Title = "欢迎来到 Astrox.Blog",
            Slug = SampleSlug,
            Summary = "航天与技术交汇处的个人笔记：关于 Yunfei Li / Astrox，以及这座博客如何运转。",
            Tags = "Web",
            IsPublished = true,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = now,
            Markdown = """
# 欢迎来到 Astrox.Blog

这里是 **Yunfei Li** 的个人博客——航天、工程与软件的交汇点。品牌 **Astrox** 代表把视线投向轨道之外，却把双手放在可运行的代码上。

## 这座博客能做什么

- 公开读者浏览已发布文章
- 所有者通过 Web 后台撰写 Markdown
- 通过 Bearer API Key 从 CI / 本地脚本远程发布

## Markdown 示例

行内代码：`dotnet run`

```csharp
Console.WriteLine("Hello, orbit.");
```

> 星辰不问赶路人，代码却要你写对每一行。

祝阅读愉快。
"""
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync();
        logger.LogInformation("已写入示例文章：{Slug}", SampleSlug);
    }

    private static async Task AlignPostCategoriesAsync(ApplicationDbContext db, BlogOptions options)
    {
        var official = options.CategoryList.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var posts = await db.Posts.ToListAsync();
        var changed = false;

        foreach (var post in posts)
        {
            var kept = post.TagList.Where(official.Contains).ToArray();
            if (kept.Length > 0)
            {
                var normalized = string.Join(",", kept);
                if (!string.Equals(normalized, post.Tags, StringComparison.Ordinal))
                {
                    post.Tags = normalized;
                    changed = true;
                }
                continue;
            }

            if (post.Slug.Contains("itrs", StringComparison.OrdinalIgnoreCase)
                || post.Slug.Contains("j2000", StringComparison.OrdinalIgnoreCase))
            {
                post.Tags = "轨道力学";
                changed = true;
            }
            else if (post.Slug == SampleSlug)
            {
                post.Tags = "Web";
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync();
    }
}
