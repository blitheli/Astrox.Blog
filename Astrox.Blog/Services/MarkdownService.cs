using System.Text;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.GenericAttributes;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Astrox.Blog.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .DisableHtml(); // strip raw HTML for safer rendering

        // {r} / {ITRS} 会被 GenericAttributes 收成 HTML 属性，破坏 LaTeX 下标。
        for (var i = builder.Extensions.Count - 1; i >= 0; i--)
        {
            if (builder.Extensions[i] is GenericAttributesExtension)
                builder.Extensions.RemoveAt(i);
        }

        ReplaceMathInlineParser(builder);
        _pipeline = builder.Build();
    }

    private static void ReplaceMathInlineParser(MarkdownPipelineBuilder builder)
    {
        var parsers = builder.InlineParsers;
        for (var i = 0; i < parsers.Count; i++)
        {
            if (parsers[i] is MathInlineParser and not CjkAwareMathInlineParser)
            {
                parsers.RemoveAt(i);
                parsers.Insert(i, new CjkAwareMathInlineParser());
                return;
            }
        }

        parsers.Add(new CjkAwareMathInlineParser());
    }

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;
        return Markdown.ToHtml(markdown, _pipeline);
    }

    public IReadOnlyList<TocEntry> ExtractToc(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return Array.Empty<TocEntry>();

        var document = Markdown.Parse(markdown, _pipeline);
        var entries = new List<TocEntry>();

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.Level is < 2 or > 3)
                continue;

            var id = heading.TryGetAttributes()?.Id;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var text = GetPlainText(heading.Inline);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            entries.Add(new TocEntry(heading.Level, text, id));
        }

        return entries;
    }

    private static string GetPlainText(ContainerInline? inline)
    {
        if (inline is null)
            return string.Empty;

        var sb = new StringBuilder();
        AppendInline(inline.FirstChild, sb);
        return sb.ToString().Trim();
    }

    private static void AppendInline(Inline? inline, StringBuilder sb)
    {
        while (inline is not null)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case MathInline math:
                    sb.Append(math.Content.ToString());
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case LineBreakInline:
                    sb.Append(' ');
                    break;
                case ContainerInline container:
                    AppendInline(container.FirstChild, sb);
                    break;
            }

            inline = inline.NextSibling;
        }
    }
}

public sealed record TocEntry(int Level, string Text, string Id);
