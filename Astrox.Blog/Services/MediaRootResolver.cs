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
        if (IsAbsolutePath(configured))
        {
            // 非 Windows 上保留盘符绝对路径字面量（生产配置常为 D:/IIS/...），避免被当成相对路径拼到 ContentRoot
            if (!OperatingSystem.IsWindows() && LooksLikeWindowsAbsolute(configured))
                return configured.Replace('\\', '/');

            return Path.GetFullPath(configured);
        }

        return Path.GetFullPath(Path.Combine(contentRoot, configured));
    }

    internal static bool IsAbsolutePath(string path) =>
        Path.IsPathRooted(path) || LooksLikeWindowsAbsolute(path);

    internal static bool LooksLikeWindowsAbsolute(string path) =>
        path.Length >= 3
        && char.IsAsciiLetter(path[0])
        && path[1] == ':'
        && (path[2] is '\\' or '/');
}
