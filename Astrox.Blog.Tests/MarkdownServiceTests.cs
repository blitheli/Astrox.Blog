using Astrox.Blog.Services;

namespace Astrox.Blog.Tests;

public class MarkdownServiceTests
{
    private readonly MarkdownService _markdown = new();

    [Fact]
    public void Turns_display_math_into_math_block()
    {
        var html = _markdown.ToHtml("$$UT1=UTC+(UT1-UTC)$$");

        Assert.Contains("class=\"math\"", html);
        Assert.Contains("UT1=UTC+(UT1-UTC)", html);
    }

    [Fact]
    public void Turns_inline_math_into_math_span()
    {
        var html = _markdown.ToHtml("儒略日 $2451545.0$ 天");

        Assert.Contains("class=\"math\"", html);
        Assert.Contains("2451545.0", html);
    }

    [Fact]
    public void Keeps_braces_inside_inline_math_instead_of_html_attributes()
    {
        var html = _markdown.ToHtml(
            "其中，$\\vec{r}_{\\mathrm{ITRS}}$和$\\vec{r}_{\\mathrm{GCRS}}$分别对应");

        Assert.Contains("\\vec{r}_{\\mathrm{ITRS}}", html);
        Assert.Contains("\\vec{r}_{\\mathrm{GCRS}}", html);
        Assert.DoesNotContain("$", html);
        Assert.DoesNotContain(" r=\"\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" itrs=\"\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" gcrs=\"\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Turns_adjacent_inline_math_after_punctuation_into_math_spans()
    {
        var html = _markdown.ToHtml(
            "上式中，$W(t)$，$R(t)$和$Q(t)$分别对应极移，自转和岁差章动转换矩阵。");

        Assert.Contains("\\(W(t)\\)", html);
        Assert.Contains("\\(R(t)\\)", html);
        Assert.Contains("\\(Q(t)\\)", html);
        Assert.DoesNotContain("$R(t)$", html);
        Assert.DoesNotContain("$Q(t)$", html);
    }

    [Fact]
    public void Turns_inline_math_glued_to_chinese_into_math_spans()
    {
        var html = _markdown.ToHtml(
            "在计算$R(t)$和$Q(t)$的时候，会有两种计算方法");

        Assert.Contains("\\(R(t)\\)", html);
        Assert.Contains("\\(Q(t)\\)", html);
        Assert.DoesNotContain("$R(t)$", html);
        Assert.DoesNotContain("$Q(t)$", html);
    }

    [Fact]
    public void Allows_single_line_media_iframe()
    {
        var html = _markdown.ToHtml(
            "前文\n\n<iframe src=\"/media/demos/cesium-demo-test/index.html\" width=\"100%\" height=\"500\" style=\"border:0\" frameborder=\"0\" allowfullscreen loading=\"lazy\" title=\"Cesium demo\"></iframe>\n\n后文");

        Assert.Contains(
            "<iframe src=\"/media/demos/cesium-demo-test/index.html\" width=\"100%\" height=\"500\" style=\"border:0\" title=\"Cesium demo\" frameborder=\"0\" loading=\"lazy\" allowfullscreen></iframe>",
            html);
        Assert.DoesNotContain("&lt;iframe", html);
        Assert.Contains("<p>前文</p>", html);
        Assert.Contains("<p>后文</p>", html);
    }

    [Theory]
    [InlineData("<iframe src=\"https://evil.example.com/x.html\"></iframe>")]
    [InlineData("<iframe src=\"//evil.example.com/media/x.html\"></iframe>")]
    [InlineData("<iframe src=\"javascript:alert(1)\"></iframe>")]
    [InlineData("<iframe src=\" /media/x.html\"></iframe>")]
    [InlineData("<iframe src=\"/media/x.html\"></iframe> <script>alert(1)</script>")]
    [InlineData("<iframe width=\"100%\"></iframe>")]
    public void Escapes_iframe_with_disallowed_src(string markdown)
    {
        var html = _markdown.ToHtml(markdown);

        Assert.DoesNotContain("<iframe", html);
        Assert.Contains("&lt;iframe", html);
    }

    [Fact]
    public void Strips_non_whitelisted_iframe_attributes()
    {
        var html = _markdown.ToHtml(
            "<iframe src=\"/media/x.html\" onload=\"alert(1)\" srcdoc=\"x\" sandbox=\"allow-scripts\" width=\"100%\"></iframe>");

        Assert.Contains("<iframe src=\"/media/x.html\" width=\"100%\"></iframe>", html);
        Assert.DoesNotContain("onload", html);
        Assert.DoesNotContain("srcdoc", html);
        Assert.DoesNotContain("sandbox", html);
    }

    [Fact]
    public void Encodes_style_and_title_values()
    {
        var html = _markdown.ToHtml(
            "<iframe src=\"/media/x.html\" title='a\"><script>alert(1)</script>'></iframe>");

        Assert.DoesNotContain("<script>", html);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<div onclick=\"alert(1)\">x</div>")]
    [InlineData("数据名称规则为：LDEM_<filespecs>。")]
    public void Still_escapes_other_raw_html(string markdown)
    {
        var html = _markdown.ToHtml(markdown);

        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("<div", html);
        Assert.DoesNotContain("<filespecs>", html);
        Assert.Contains("&lt;", html);
    }

    [Fact]
    public void Iframe_inside_code_block_stays_as_code()
    {
        var html = _markdown.ToHtml("```html\n<iframe src=\"/media/x.html\"></iframe>\n```\n\n    <iframe src=\"/media/y.html\"></iframe>");

        Assert.DoesNotContain("<iframe", html);
        Assert.Contains("&lt;iframe src=&quot;/media/x.html&quot;", html);
        Assert.Contains("&lt;iframe src=&quot;/media/y.html&quot;", html);
    }
}
