using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Astrox.Blog.Services;

/// <summary>
/// 文章可下载静态资源：仓库 <c>wwwroot/resources/</c>，对外 URL <c>/resources/...</c>。
/// </summary>
public static class ResourceStaticFiles
{
    public const string FolderName = "resources";
    public const string RequestPath = "/resources";

    public static FileExtensionContentTypeProvider CreateContentTypeProvider()
    {
        var provider = new FileExtensionContentTypeProvider();

        // 常见下载 / 数据格式（覆盖或补全默认映射）
        provider.Mappings[".zip"] = "application/zip";
        provider.Mappings[".7z"] = "application/x-7z-compressed";
        provider.Mappings[".rar"] = "application/vnd.rar";
        provider.Mappings[".pdf"] = "application/pdf";
        provider.Mappings[".json"] = "application/json";
        provider.Mappings[".czml"] = "application/json";
        provider.Mappings[".csv"] = "text/csv";
        provider.Mappings[".txt"] = "text/plain";
        provider.Mappings[".md"] = "text/markdown";
        provider.Mappings[".py"] = "text/x-python";
        provider.Mappings[".cs"] = "text/plain";
        provider.Mappings[".hgt"] = "application/octet-stream";
        provider.Mappings[".tif"] = "image/tiff";
        provider.Mappings[".tiff"] = "image/tiff";
        provider.Mappings[".glb"] = "model/gltf-binary";
        provider.Mappings[".gltf"] = "model/gltf+json";
        provider.Mappings[".kml"] = "application/vnd.google-earth.kml+xml";
        provider.Mappings[".kmz"] = "application/vnd.google-earth.kmz";
        provider.Mappings[".geojson"] = "application/geo+json";
        provider.Mappings[".bin"] = "application/octet-stream";
        provider.Mappings[".dat"] = "application/octet-stream";

        return provider;
    }

    public static StaticFileOptions CreateOptions(string webRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webRootPath);

        var physicalRoot = Path.GetFullPath(Path.Combine(webRootPath, FolderName));
        Directory.CreateDirectory(physicalRoot);

        return new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalRoot),
            RequestPath = RequestPath,
            ContentTypeProvider = CreateContentTypeProvider(),
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream",
            OnPrepareResponse = ctx =>
            {
                // 促使浏览器下载而非内联打开（文本类仍可被 curl / 工具直接读取）
                var fileName = Path.GetFileName(ctx.File.Name);
                if (string.IsNullOrEmpty(fileName))
                    return;

                ctx.Context.Response.Headers.ContentDisposition =
                    $"attachment; filename=\"{fileName}\"";
            }
        };
    }
}
