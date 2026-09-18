using Astrox.Blog.Services;

namespace Astrox.Blog.Tests;

public class MediaRootResolverTests
{
    [Fact]
    public void Uses_absolute_configured_path()
    {
        var resolved = MediaRootResolver.Resolve(@"D:\IIS\astrox-blog-media", contentRoot: @"C:\app");

        Assert.Equal(Path.GetFullPath(@"D:\IIS\astrox-blog-media"), resolved);
    }

    [Fact]
    public void Combines_relative_path_with_content_root()
    {
        var resolved = MediaRootResolver.Resolve("astrox-blog-media", contentRoot: @"C:\app");

        Assert.Equal(Path.GetFullPath(@"C:\app\astrox-blog-media"), resolved);
    }

    [Fact]
    public void Defaults_to_content_root_media_folder_when_empty()
    {
        var resolved = MediaRootResolver.Resolve("  ", contentRoot: @"C:\app");

        Assert.Equal(Path.GetFullPath(@"C:\app\astrox-blog-media"), resolved);
    }
}
