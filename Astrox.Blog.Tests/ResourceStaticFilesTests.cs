using Astrox.Blog.Services;
using Microsoft.Extensions.FileProviders;

namespace Astrox.Blog.Tests;

public class ResourceStaticFilesTests
{
    [Theory]
    [InlineData(".zip", "application/zip")]
    [InlineData(".7z", "application/x-7z-compressed")]
    [InlineData(".rar", "application/vnd.rar")]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".json", "application/json")]
    [InlineData(".czml", "application/json")]
    [InlineData(".csv", "text/csv")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".py", "text/x-python")]
    [InlineData(".cs", "text/plain")]
    [InlineData(".hgt", "application/octet-stream")]
    [InlineData(".tif", "image/tiff")]
    public void ContentTypeProvider_MapsCommonDownloadExtensions(string extension, string expected)
    {
        var provider = ResourceStaticFiles.CreateContentTypeProvider();
        Assert.True(provider.TryGetContentType("file" + extension, out var contentType));
        Assert.Equal(expected, contentType);
    }

    [Fact]
    public void CreateOptions_EnablesUnknownTypesAndResourcesPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "astrox-resources-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = ResourceStaticFiles.CreateOptions(root);
            Assert.Equal(ResourceStaticFiles.RequestPath, options.RequestPath.Value);
            Assert.True(options.ServeUnknownFileTypes);
            Assert.Equal("application/octet-stream", options.DefaultContentType);
            Assert.IsAssignableFrom<IFileProvider>(options.FileProvider);
            Assert.True(Directory.Exists(Path.Combine(root, ResourceStaticFiles.FolderName)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
