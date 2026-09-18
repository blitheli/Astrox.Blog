using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Data;

/// <summary>
/// EnsureCreated 不会给已有 SQLite 库补新表。启动时显式 CREATE TABLE IF NOT EXISTS，
/// 保证生产库（如 D:/IIS/astrox-blog.db）无需手工删库即可获得 Comments。
/// </summary>
public static class DbSchemaUpgrader
{
    public static async Task EnsureCommentsTableAsync(ApplicationDbContext db, ILogger logger)
    {
        // SQLite：EnsureCreated 之后对已有库补表；新库由 EF 建表，IF NOT EXISTS 仍安全。
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Comments" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Comments" PRIMARY KEY AUTOINCREMENT,
                "PostId" INTEGER NOT NULL,
                "AuthorName" TEXT NOT NULL,
                "AuthorEmail" TEXT NULL,
                "Body" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "IpHash" TEXT NOT NULL,
                "UserAgent" TEXT NULL,
                "IsDeleted" INTEGER NOT NULL,
                CONSTRAINT "FK_Comments_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Comments_PostId" ON "Comments" ("PostId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Comments_PostId_IsDeleted_CreatedAt"
            ON "Comments" ("PostId", "IsDeleted", "CreatedAt");
            """);

        logger.LogInformation("已确认 Comments 表存在（CREATE TABLE IF NOT EXISTS）。");
    }
}
