using System.IO.Compression;
using System.Text;
using Astrox.Blog.Services;

namespace Astrox.Blog.Tests;

public class PostZipImporterTests : IDisposable
{
    private readonly string _mediaRoot = Path.Combine(Path.GetTempPath(), "astrox-blog-media-tests", Guid.NewGuid().ToString("N"));

    public PostZipImporterTests()
    {
        Directory.CreateDirectory(_mediaRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_mediaRoot))
            Directory.Delete(_mediaRoot, recursive: true);
    }

    [Fact]
    public void Imports_nested_folder_zip_and_rewrites_images()
    {
        using var zip = CreateZip(
            ("ITRS-GCRS-J2000/ITRS-GCRS-J2000坐标系的相互转换.md",
                Encoding.UTF8.GetBytes("# 标题\n\n正文 ![地轴](axis.png)\n")),
            ("ITRS-GCRS-J2000/axis.png", [0x89, 0x50, 0x4E, 0x47]));

        var result = new PostZipImporter(_mediaRoot).Import(zip, "ITRS-GCRS-J2000.zip");

        var post = Assert.Single(result.Posts);
        Assert.Equal("ITRS-GCRS-J2000", result.MediaFolder);
        Assert.Equal("标题", post.Title);
        Assert.Contains("/media/ITRS-GCRS-J2000/axis.png", post.Markdown);
        Assert.True(File.Exists(Path.Combine(_mediaRoot, "ITRS-GCRS-J2000", "axis.png")));
    }

    [Fact]
    public void Uses_zip_file_name_when_entries_are_flat()
    {
        using var zip = CreateZip(
            ("note.md", Encoding.UTF8.GetBytes("没有标题的正文。\n\n![图](pic.jpg)")),
            ("pic.jpg", [0xFF, 0xD8, 0xFF]));

        var result = new PostZipImporter(_mediaRoot).Import(zip, "my-article.zip");

        var post = Assert.Single(result.Posts);
        Assert.Equal("my-article", result.MediaFolder);
        Assert.Equal("note", post.Title);
        Assert.Contains("/media/my-article/pic.jpg", post.Markdown);
    }

    [Fact]
    public void Rejects_zip_slip_entries()
    {
        using var zip = CreateZip(("../escape.md", Encoding.UTF8.GetBytes("# 坏")));

        var ex = Assert.Throws<PostZipImportException>(
            () => new PostZipImporter(_mediaRoot).Import(zip, "evil.zip"));

        Assert.Contains("非法", ex.Message);
    }

    [Fact]
    public void Rejects_zip_without_markdown()
    {
        using var zip = CreateZip(("only.png", [0x89, 0x50, 0x4E, 0x47]));

        var ex = Assert.Throws<PostZipImportException>(
            () => new PostZipImporter(_mediaRoot).Import(zip, "pics.zip"));

        Assert.Contains("Markdown", ex.Message);
    }

    [Fact]
    public void Skips_os_metadata_and_keeps_real_markdown()
    {
        using var zip = CreateZip(
            ("__MACOSX/._doc.md", Encoding.UTF8.GetBytes("junk")),
            (".DS_Store", [0x00]),
            ("doc.md", Encoding.UTF8.GetBytes("# 真文章\n")));

        var result = new PostZipImporter(_mediaRoot).Import(zip, "pack.zip");

        Assert.Equal("真文章", Assert.Single(result.Posts).Title);
    }

    [Fact]
    public void Summary_skips_markdown_image_tokens()
    {
        using var zip = CreateZip(
            ("wind.md", Encoding.UTF8.GetBytes(
                "# 看不见的全球风\n\n" +
                "开篇说明。![在这里插入图片描述](a22485d24dc348ca85e63b16cc2ae5d0.png) 后续文字。\n")));

        var result = new PostZipImporter(_mediaRoot).Import(zip, "wind.zip");

        var post = Assert.Single(result.Posts);
        Assert.Equal("看不见的全球风", post.Title);
        Assert.Equal("开篇说明。 后续文字。", post.Summary);
        Assert.DoesNotContain("a22485d24dc348ca85e63b16cc2ae5d0.png", post.Summary);
        Assert.DoesNotContain("![", post.Summary);
    }

    private static MemoryStream CreateZip(params (string Name, byte[] Data)[] files)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, data) in files)
            {
                var entry = archive.CreateEntry(name);
                using var stream = entry.Open();
                stream.Write(data);
            }
        }

        ms.Position = 0;
        return ms;
    }
}
