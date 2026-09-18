namespace Astrox.Blog.Services;

public static class MediaRootResolver
{
    public const string DefaultFolderName = "astrox-blog-media";
    public const string RequestPath = "/media";

    public static string Resolve(string? configured, string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(Path.Combine(contentRoot, DefaultFolderName));

        configured = configured.Trim();
        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(contentRoot, configured));
    }
}
