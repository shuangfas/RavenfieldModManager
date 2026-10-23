using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ModManagerGUI.Services.Steam;

public static partial class SteamBBCodeParser {
    private const RegexOptions RxOptions =
        RegexOptions.IgnoreCase |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // ---------- 固定正则表达式 ----------

    [GeneratedRegex(
        @"\[noparse\]([\s\S]*?)\[/noparse\]",
        RxOptions)]
    private static partial Regex NoParseRegex();

    [GeneratedRegex(@"\[b\]([\s\S]*?)\[/b\]", RxOptions)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"\[i\]([\s\S]*?)\[/i\]", RxOptions)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"\[u\]([\s\S]*?)\[/u\]", RxOptions)]
    private static partial Regex UnderlineRegex();

    [GeneratedRegex(@"\[strike\]([\s\S]*?)\[/strike\]", RxOptions)]
    private static partial Regex StrikeRegex();

    [GeneratedRegex(@"\[h1\]([\s\S]*?)\[/h1\]", RxOptions)]
    private static partial Regex H1Regex();

    [GeneratedRegex(@"\[h2\]([\s\S]*?)\[/h2\]", RxOptions)]
    private static partial Regex H2Regex();

    [GeneratedRegex(@"\[h3\]([\s\S]*?)\[/h3\]", RxOptions)]
    private static partial Regex H3Regex();

    [GeneratedRegex(@"\[code\]([\s\S]*?)\[/code\]", RxOptions)]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"\[hr\](?:\[/hr\])?", RxOptions)]
    private static partial Regex HrRegex();

    [GeneratedRegex(
        @"\[url=([\s\S]*?)\]([\s\S]*?)\[/url\]",
        RxOptions)]
    private static partial Regex UrlWithTargetRegex();

    [GeneratedRegex(
        @"\[url\]([\s\S]*?)\[/url\]",
        RxOptions)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(
        @"\[quote=([\s\S]*?)\]([\s\S]*?)\[/quote\]",
        RxOptions)]
    private static partial Regex QuoteWithAuthorRegex();

    [GeneratedRegex(
        @"\[quote\]([\s\S]*?)\[/quote\]",
        RxOptions)]
    private static partial Regex QuoteRegex();

    [GeneratedRegex(
        @"\[spoiler\]([\s\S]*?)\[/spoiler\]",
        RxOptions)]
    private static partial Regex SpoilerRegex();

    [GeneratedRegex(
        @"\[img\]([\s\S]*?)\[/img\]",
        RxOptions)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(
        @"\[olist\]([\s\S]*?)\[/olist\]",
        RxOptions)]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(
        @"\[list\]([\s\S]*?)\[/list\]",
        RxOptions)]
    private static partial Regex UnorderedListRegex();

    [GeneratedRegex(
        @"\[table\]([\s\S]*?)\[/table\]",
        RxOptions)]
    private static partial Regex TableRegex();

    [GeneratedRegex(
        @"\[tr\]([\s\S]*?)\[/tr\]",
        RxOptions)]
    private static partial Regex TableRowRegex();

    [GeneratedRegex(
        @"\[th\]([\s\S]*?)\[/th\]",
        RxOptions)]
    private static partial Regex TableHeaderRegex();

    [GeneratedRegex(
        @"\[td\]([\s\S]*?)\[/td\]",
        RxOptions)]
    private static partial Regex TableCellRegex();

    [GeneratedRegex(
        @"(<pre\b[^>]*>[\s\S]*?</pre>)",
        RxOptions)]
    private static partial Regex PreBlockRegex();

    [GeneratedRegex(
        @"(<(?:table|tr|ul|ol|blockquote|h[1-6]|hr)\b[^>]*>)" +
        @"(?:\s*<br>)*",
        RxOptions)]
    private static partial Regex BlockStartRegex();

    [GeneratedRegex(
        @"(?:<br>\s*)*(</(?:table|tr|ul|ol|blockquote|h[1-6]|hr)>)",
        RxOptions)]
    private static partial Regex BlockEndRegex();

    [GeneratedRegex(
        @"(</(?:th|td)>)(?:\s*<br>\s*)*(<(?:th|td)\b)",
        RxOptions)]
    private static partial Regex TableCellBreakRegex();

    [GeneratedRegex(@"[\r\n]+")]
    private static partial Regex NewlineRegex();

    // ---------- 对外 API ----------

    public static string ToHtml(string? input) {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var blocks = new List<string>();

        // 1. 保护 noparse
        string text = NoParseRegex().Replace(input, match => {
            int index = blocks.Count;
            blocks.Add(EscapeHtml(match.Groups[1].Value));
            return $"\0NOPARSE_{index}\0";
        });

        // 2. 转义原始文本
        text = EscapeHtml(text);

        // 3. 普通标签
        text = ReplaceRepeated(BoldRegex(), text, "<b>$1</b>");
        text = ReplaceRepeated(ItalicRegex(), text, "<i>$1</i>");
        text = ReplaceRepeated(UnderlineRegex(), text, "<u>$1</u>");
        text = ReplaceRepeated(StrikeRegex(), text, "<del>$1</del>");
        text = ReplaceRepeated(H1Regex(), text, "<h1>$1</h1>");
        text = ReplaceRepeated(H2Regex(), text, "<h2>$1</h2>");
        text = ReplaceRepeated(H3Regex(), text, "<h3>$1</h3>");
        text = ReplaceRepeated(
            CodeRegex(), text, "<pre><code>$1</code></pre>");
        text = ReplaceRepeated(HrRegex(), text, "<hr>");

        // 4. URL
        text = ReplaceRepeated(
            UrlWithTargetRegex(),
            text,
            match => {
                string url = WebUtility.HtmlDecode(
                    match.Groups[1].Value);

                string content = match.Groups[2].Value;
                string safeUrl = SanitizeUrl(url);

                return $"<a href=\"{EscapeHtml(safeUrl)}\" " +
                       "target=\"_blank\" rel=\"noopener noreferrer\">" +
                       $"{content}</a>";
            });

        text = ReplaceRepeated(
            UrlRegex(),
            text,
            match => {
                string content = match.Groups[1].Value;
                string url = WebUtility.HtmlDecode(content.Trim());
                string safeUrl = SanitizeUrl(url);

                return $"<a href=\"{EscapeHtml(safeUrl)}\" " +
                       "target=\"_blank\" rel=\"noopener noreferrer\">" +
                       $"{content}</a>";
            });

        // 5. 引用
        text = ReplaceRepeated(
            QuoteWithAuthorRegex(),
            text,
            "<blockquote><div class=\"quote-author\">" +
            "$1 said:</div>$2</blockquote>");

        text = ReplaceRepeated(
            QuoteRegex(), text, "<blockquote>$1</blockquote>");

        // 6. 剧透
        text = ReplaceRepeated(
            SpoilerRegex(),
            text,
            "<span class=\"spoiler\">$1</span>");

        // 7. 图片
        text = ReplaceRepeated(
            ImageRegex(),
            text,
            match => {
                string url = WebUtility.HtmlDecode(
                    match.Groups[1].Value.Trim());

                string safeUrl = SanitizeUrl(url);

                return $"<img src=\"{EscapeHtml(safeUrl)}\" " +
                       "alt=\"user image\" loading=\"lazy\">";
            });

        // 8. 列表
        text = ReplaceRepeated(
            OrderedListRegex(),
            text,
            match => BuildList(match.Groups[1].Value, true));

        text = ReplaceRepeated(
            UnorderedListRegex(),
            text,
            match => BuildList(match.Groups[1].Value, false));

        // 9. 表格
        text = ReplaceRepeated(
            TableRegex(),
            text,
            match => BuildTable(match.Groups[1].Value));

        // 10. 换行
        text = ConvertNewlines(text);

        // 11. 恢复 noparse
        for (int i = 0; i < blocks.Count; i++) {
            text = text.Replace(
                $"\0NOPARSE_{i}\0",
                blocks[i],
                StringComparison.Ordinal);
        }

        return text;
    }

    // ---------- 辅助方法 ----------

    private static string ReplaceRepeated(
        Regex regex,
        string input,
        string replacement) {
        for (int i = 0; i < 32; i++) {
            string next = regex.Replace(input, replacement);

            if (next == input)
                break;

            input = next;
        }

        return input;
    }

    private static string ReplaceRepeated(
        Regex regex,
        string input,
        MatchEvaluator evaluator) {
        for (int i = 0; i < 32; i++) {
            string next = regex.Replace(input, evaluator);

            if (next == input)
                break;

            input = next;
        }

        return input;
    }

    private static string BuildList(string content, bool ordered) {
        string tag = ordered ? "ol" : "ul";
        string[] items = content.Split(
            new[] { "[*]" },
            StringSplitOptions.None);

        var result = new StringBuilder();
        result.Append('<').Append(tag).Append('>');

        foreach (string item in items) {
            if (string.IsNullOrWhiteSpace(item))
                continue;

            result.Append("<li>")
                .Append(item.Trim())
                .Append("</li>");
        }

        result.Append("</").Append(tag).Append('>');
        return result.ToString();
    }

    private static string BuildTable(string content) {
        string rows = TableRowRegex().Replace(content, row => {
            string cells = TableHeaderRegex().Replace(
                row.Groups[1].Value,
                "<th>$1</th>");

            cells = TableCellRegex().Replace(cells, "<td>$1</td>");

            return "<tr>" + cells + "</tr>";
        });

        return "<table>" + rows + "</table>";
    }

    private static string ConvertNewlines(string input) {
        string[] parts = PreBlockRegex().Split(input);

        for (int i = 0; i < parts.Length; i++) {
            // Split 的奇数索引是完整的 pre 代码块
            if (i % 2 == 1)
                continue;

            parts[i] = parts[i]
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\n", "<br>");

            parts[i] = BlockStartRegex().Replace(parts[i], "$1");
            parts[i] = BlockEndRegex().Replace(parts[i], "$1");
            parts[i] = TableCellBreakRegex().Replace(parts[i], "$1$2");
        }

        return string.Concat(parts);
    }

    private static string SanitizeUrl(string url) {
        url = url.Trim();

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp ||
             uri.Scheme == Uri.UriSchemeHttps) &&
            !string.IsNullOrWhiteSpace(uri.Host)) {
            return uri.AbsoluteUri;
        }

        if (url.StartsWith("//", StringComparison.Ordinal) &&
            Uri.TryCreate("https:" + url, UriKind.Absolute, out uri) &&
            !string.IsNullOrWhiteSpace(uri.Host)) {
            return url;
        }

        return "#";
    }

    private static string EscapeHtml(string input) {
        return input.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#039;");
    }
}