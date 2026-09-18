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
}
