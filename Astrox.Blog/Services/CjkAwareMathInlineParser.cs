using System.Globalization;
using Markdig.Extensions.Mathematics;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Astrox.Blog.Services;

/// <summary>
/// Markdig 默认要求 <c>$...$</c> 两侧必须是空白或标点。中文里常见
/// <c>计算$R(t)$和$Q(t)$</c>，汉字属于 OtherLetter，会被拒绝。
/// 这里把 OtherLetter（含 CJK）也当作合法邻接字符。
/// </summary>
public sealed class CjkAwareMathInlineParser : MathInlineParser
{
    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var match = slice.CurrentChar;
        var pc = slice.PeekCharExtra(-1);
        if (pc == match)
            return false;

        var startPosition = slice.Start;

        var openDollars = 1;
        var c = slice.NextChar();
        if (c == match)
        {
            openDollars++;
            c = slice.NextChar();
        }

        pc.CheckUnicodeCategory(out var openPrevIsWhiteSpace, out var openPrevIsPunctuation);
        c.CheckUnicodeCategory(out var openNextIsWhiteSpace, out _);

        if (!IsAllowedBesideDollar(pc, openPrevIsWhiteSpace, openPrevIsPunctuation))
            return false;

        var closeDollars = 0;

        while (c.IsSpaceOrTab())
            c = slice.NextChar();

        var start = slice.Start;
        var end = 0;

        pc = match;
        var lastWhiteSpace = -1;
        while (c != '\0')
        {
            if (c is '\r' or '\n')
                return false;

            if (pc != '\\')
            {
                if (c.IsSpaceOrTab())
                {
                    if (lastWhiteSpace < 0)
                        lastWhiteSpace = slice.Start;
                }
                else
                {
                    var hasClosingDollars = c == match;
                    if (hasClosingDollars)
                    {
                        closeDollars = 0;
                        while (slice.CurrentChar == match)
                        {
                            closeDollars++;
                            c = slice.NextChar();
                        }
                    }

                    if (closeDollars >= openDollars)
                        break;

                    lastWhiteSpace = -1;
                    if (hasClosingDollars)
                    {
                        pc = match;
                        continue;
                    }
                }
            }

            if (closeDollars > 0)
            {
                closeDollars = 0;
            }
            else
            {
                pc = c;
                c = slice.NextChar();
            }
        }

        if (closeDollars < openDollars)
            return false;

        pc.CheckUnicodeCategory(out var closePrevIsWhiteSpace, out _);
        c.CheckUnicodeCategory(out var closeNextIsWhiteSpace, out var closeNextIsPunctuation);

        if (!IsAllowedBesideDollar(c, closeNextIsWhiteSpace, closeNextIsPunctuation)
            || openNextIsWhiteSpace != closePrevIsWhiteSpace)
        {
            return false;
        }

        if (closePrevIsWhiteSpace && lastWhiteSpace > 0)
            end = lastWhiteSpace + openDollars - 1;
        else
            end = slice.Start - 1;

        var inline = new MathInline
        {
            Span = new SourceSpan(
                processor.GetSourcePosition(startPosition, out var line, out var column),
                processor.GetSourcePosition(slice.Start - 1)),
            Line = line,
            Column = column,
            Delimiter = match,
            DelimiterCount = openDollars,
            Content = slice
        };
        inline.Content.Start = start;
        inline.Content.End = end - openDollars;

        if (DefaultClass is not null)
            inline.GetAttributes().AddClass(DefaultClass);

        processor.Inline = inline;
        return true;
    }

    private static bool IsAllowedBesideDollar(char neighbor, bool isWhiteSpace, bool isPunctuation)
    {
        if (neighbor == '\0' || isWhiteSpace || isPunctuation)
            return true;

        return char.GetUnicodeCategory(neighbor) == UnicodeCategory.OtherLetter;
    }
}
