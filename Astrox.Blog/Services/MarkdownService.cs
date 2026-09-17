using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace Astrox.Blog.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .DisableHtml() // strip raw HTML for safer rendering
            .Build();
    }

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;
        return Markdown.ToHtml(markdown, _pipeline);
    }
}
