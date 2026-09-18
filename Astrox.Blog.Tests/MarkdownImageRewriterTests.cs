using Astrox.Blog.Services;

namespace Astrox.Blog.Tests;

public class MarkdownImageRewriterTests
{
    [Fact]
    public void Rewrites_relative_inline_image_to_media_url()
    {
        var markdown = "见图：![地轴](axis.png)";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "ITRS-GCRS-J2000");

        Assert.Equal("见图：![地轴](/media/ITRS-GCRS-J2000/axis.png)", rewritten);
    }

    [Fact]
    public void Rewrites_dot_slash_and_nested_relative_paths()
    {
        var markdown = "![a](./axis.png)\n![b](images/EOP.png)";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "ITRS-GCRS-J2000", markdownDirectory: "");

        Assert.Contains("(/media/ITRS-GCRS-J2000/axis.png)", rewritten);
        Assert.Contains("(/media/ITRS-GCRS-J2000/images/EOP.png)", rewritten);
    }

    [Fact]
    public void Resolves_image_relative_to_markdown_directory()
    {
        var markdown = "![a](axis.png)";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "pack", markdownDirectory: "sub");

        Assert.Equal("![a](/media/pack/sub/axis.png)", rewritten);
    }

    [Fact]
    public void Keeps_remote_and_site_absolute_urls()
    {
        var markdown = "![a](https://cdn.example.com/a.png) ![b](/images/logo.png)";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "ITRS-GCRS-J2000");

        Assert.Equal(markdown, rewritten);
    }

    [Fact]
    public void Preserves_optional_image_title()
    {
        var markdown = "![EOP](EOP.png \"数据示例\")";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "ITRS-GCRS-J2000");

        Assert.Equal("![EOP](/media/ITRS-GCRS-J2000/EOP.png \"数据示例\")", rewritten);
    }

    [Fact]
    public void Rewrites_reference_style_image_definitions()
    {
        var markdown = "见图 ![x][eop]\n\n[eop]: EOP.png";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "ITRS-GCRS-J2000");

        Assert.Contains("[eop]: /media/ITRS-GCRS-J2000/EOP.png", rewritten);
        Assert.Contains("![x][eop]", rewritten);
    }

    [Fact]
    public void Does_not_rewrite_non_image_reference_links()
    {
        var markdown = "[说明][doc]\n\n[doc]: notes.md";

        var rewritten = MarkdownImageRewriter.Rewrite(markdown, "ITRS-GCRS-J2000");

        Assert.Equal(markdown, rewritten);
    }
}
