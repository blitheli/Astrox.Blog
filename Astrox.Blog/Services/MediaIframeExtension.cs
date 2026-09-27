using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Astrox.Blog.Services;

/// <summary>
/// 在 <c>DisableHtml()</c> 的前提下，白名单放行「单独一行」的站内 iframe：
/// <c>&lt;iframe src="/media/..." ...&gt;&lt;/iframe&gt;</c>。
/// 只保留 src / width / height / style / title / allowfullscreen / loading / frameborder，
/// 其余属性丢弃；src 不以 <c>/media/</c> 开头的整行照旧按普通文本转义。
/// </summary>
public sealed class MediaIframeExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<MediaIframeBlockParser>())
            pipeline.BlockParsers.Insert(0, new MediaIframeBlockParser());
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<MediaIframeRenderer>())
            html.ObjectRenderers.Insert(0, new MediaIframeRenderer());
    }
}

public sealed class MediaIframeBlock : LeafBlock
{
    public MediaIframeBlock(BlockParser parser) : base(parser) { }

    /// <summary>已按白名单重建并编码的 iframe 标签。</summary>
    public string SafeHtml { get; set; } = string.Empty;
}

public sealed partial class MediaIframeBlockParser : BlockParser
{
    public const string AllowedSrcPrefix = "/media/";

    public MediaIframeBlockParser()
    {
        OpeningCharacters = ['<'];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent)
            return BlockState.None;

        var line = processor.Line.ToString();
        var safe = TryBuildSafeIframe(line);
        if (safe is null)
            return BlockState.None;

        var start = processor.Line.Start;
        processor.NewBlocks.Push(new MediaIframeBlock(this)
        {
            SafeHtml = safe,
            Line = processor.LineIndex,
            Column = processor.Column,
            Span = new SourceSpan(start, processor.Line.End)
        });
        processor.Line.Start = processor.Line.End + 1;
        return BlockState.BreakDiscard;
    }

    /// <summary>整行合法则返回重建后的安全 HTML，否则返回 null。</summary>
    internal static string? TryBuildSafeIframe(string line)
    {
        var m = IframeLine().Match(line);
        if (!m.Success)
            return null;

        var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var rest = m.Groups["attrs"].Value;
        var pos = 0;
        foreach (Match a in Attribute().Matches(rest))
        {
            // 属性之间只能是空白，出现其它残留字符（未闭合引号等）视为不合法。
            if (!string.IsNullOrWhiteSpace(rest[pos..a.Index]))
                return null;
            pos = a.Index + a.Length;
            var name = a.Groups["name"].Value;
            string? value = a.Groups["dq"].Success ? a.Groups["dq"].Value
                : a.Groups["sq"].Success ? a.Groups["sq"].Value
                : a.Groups["uq"].Success ? a.Groups["uq"].Value
                : null;
            attrs.TryAdd(name, value);
        }
        if (!string.IsNullOrWhiteSpace(rest[pos..]))
            return null;

        if (!attrs.TryGetValue("src", out var src) || !IsAllowedSrc(src))
            return null;

        var sb = new StringBuilder("<iframe src=\"").Append(Enc(src!)).Append('"');
        AppendIf(sb, attrs, "width", SizeValue());
        AppendIf(sb, attrs, "height", SizeValue());
        if (attrs.TryGetValue("style", out var style) && style is not null)
            sb.Append(" style=\"").Append(Enc(style)).Append('"');
        if (attrs.TryGetValue("title", out var title) && title is not null)
            sb.Append(" title=\"").Append(Enc(title)).Append('"');
        AppendIf(sb, attrs, "frameborder", Digits());
        if (attrs.TryGetValue("loading", out var loading)
            && (string.Equals(loading, "lazy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(loading, "eager", StringComparison.OrdinalIgnoreCase)))
            sb.Append(" loading=\"").Append(loading!.ToLowerInvariant()).Append('"');
        if (attrs.ContainsKey("allowfullscreen"))
            sb.Append(" allowfullscreen");
        sb.Append("></iframe>");
        return sb.ToString();
    }

    internal static bool IsAllowedSrc(string? src)
    {
        if (string.IsNullOrWhiteSpace(src) || !src.StartsWith(AllowedSrcPrefix, StringComparison.Ordinal))
            return false;
        foreach (var c in src)
        {
            if (char.IsControl(c) || char.IsWhiteSpace(c) || c is '\\' or '"' or '\'' or '<' or '>')
                return false;
        }
        return true;
    }

    private static void AppendIf(StringBuilder sb, Dictionary<string, string?> attrs, string name, Regex valid)
    {
        if (attrs.TryGetValue(name, out var v) && v is not null && valid.IsMatch(v))
            sb.Append(' ').Append(name).Append("=\"").Append(v).Append('"');
    }

    private static string Enc(string s) => WebUtility.HtmlEncode(s);

    [GeneratedRegex(@"^\s*<iframe(?<attrs>(?:\s[^<>]*)?)>\s*</iframe>\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex IframeLine();

    [GeneratedRegex(@"(?<name>[A-Za-z][A-Za-z0-9_:-]*)(?:\s*=\s*(?:""(?<dq>[^""]*)""|'(?<sq>[^']*)'|(?<uq>[^\s""'=<>`]+)))?")]
    private static partial Regex Attribute();

    [GeneratedRegex(@"^\d{1,5}(?:%|px)?$")]
    private static partial Regex SizeValue();

    [GeneratedRegex(@"^\d{1,2}$")]
    private static partial Regex Digits();
}

public sealed class MediaIframeRenderer : HtmlObjectRenderer<MediaIframeBlock>
{
    protected override void Write(HtmlRenderer renderer, MediaIframeBlock obj)
    {
        renderer.EnsureLine();
        if (renderer.EnableHtmlForBlock)
            renderer.Write(obj.SafeHtml);
        renderer.EnsureLine();
    }
}
