using Astrox.Blog.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Astrox.Blog.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<SiteStat> SiteStats => Set<SiteStat>();
    public DbSet<PostViewCount> PostViewCounts => Set<PostViewCount>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Post>(e =>
        {
            e.HasIndex(p => p.Slug).IsUnique();
            e.HasIndex(p => p.IsPublished);
            e.Property(p => p.Title).IsRequired();
            e.Property(p => p.Slug).IsRequired();
            e.Property(p => p.Markdown).IsRequired();
            e.HasMany(p => p.Comments)
                .WithOne(c => c.Post!)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<PostViewCount>()
                .WithOne(v => v.Post!)
                .HasForeignKey<PostViewCount>(v => v.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Comment>(e =>
        {
            e.HasIndex(c => c.PostId);
            e.HasIndex(c => new { c.PostId, c.IsDeleted, c.CreatedAt });
            e.Property(c => c.AuthorName).IsRequired().HasMaxLength(64);
            e.Property(c => c.AuthorEmail).HasMaxLength(200);
            e.Property(c => c.Body).IsRequired().HasMaxLength(2000);
            e.Property(c => c.IpHash).IsRequired().HasMaxLength(64);
            e.Property(c => c.UserAgent).HasMaxLength(300);
        });

        builder.Entity<SiteStat>(e =>
        {
            e.Property(s => s.Key).IsRequired().HasMaxLength(64);
            e.Property(s => s.Value).IsRequired();
        });

        builder.Entity<PostViewCount>(e =>
        {
            e.Property(v => v.Count).IsRequired();
        });
    }
}
