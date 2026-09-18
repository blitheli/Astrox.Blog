using Astrox.Blog.Services;

namespace Astrox.Blog.Tests;

public class MediaRootResolverTests
{
    [Fact]
    public void Uses_absolute_configured_path()
    {
        var absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "astrox-blog-media-abs"));
        var resolved = MediaRootResolver.Resolve(absolute, contentRoot: Path.GetTempPath());

        Assert.Equal(Path.GetFullPath(absolute), resolved);
    }

    [Fact]
    public void Recognizes_windows_drive_path_as_absolute_even_on_unix()
    {
        var resolved = MediaRootResolver.Resolve(@"D:\IIS\astrox-blog-media", contentRoot: Path.GetTempPath());

        if (OperatingSystem.IsWindows())
            Assert.Equal(Path.GetFullPath(@"D:\IIS\astrox-blog-media"), resolved);
        else
            Assert.Equal("D:/IIS/astrox-blog-media", resolved);
    }

    [Fact]
    public void Combines_relative_path_with_content_root()
    {
        var contentRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "astrox-content-root"));
        var resolved = MediaRootResolver.Resolve("astrox-blog-media", contentRoot: contentRoot);

        Assert.Equal(Path.GetFullPath(Path.Combine(contentRoot, "astrox-blog-media")), resolved);
    }

    [Fact]
    public void Defaults_to_content_root_media_folder_when_empty()
    {
        var contentRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "astrox-content-root"));
        var resolved = MediaRootResolver.Resolve("  ", contentRoot: contentRoot);

        Assert.Equal(Path.GetFullPath(Path.Combine(contentRoot, "astrox-blog-media")), resolved);
    }
}
