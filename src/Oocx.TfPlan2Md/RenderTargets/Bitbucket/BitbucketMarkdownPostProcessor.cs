using System.Net;
using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.RenderTargets.Bitbucket;

/// <summary>
/// Converts tfplan2md's HTML-enhanced markdown into markdown-only output that Bitbucket comments can render.
/// </summary>
/// <remarks>
/// Bitbucket Cloud PR comments do not support arbitrary HTML, so the renderer removes
/// details/summary containers, converts HTML code/bold tags to markdown, and degrades
/// HTML line breaks to plain text separators.
/// </remarks>
internal static class BitbucketMarkdownPostProcessor
{
    private static readonly Regex DetailsBlockRegex = new(
        "(?:<br\\s*/?>\\s*)?<details[^>]*>\\s*<summary>(?<summary>.*?)</summary>\\s*(?:<br\\s*/?>)?",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(2));

    private static readonly Regex ClosingDetailsRegex = new(
        "</details>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private static readonly Regex SpanTagRegex = new(
        "</?span[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private static readonly Regex PreCodeRegex = new(
        "<pre[^>]*><code[^>]*>(?<content>.*?)</code></pre>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(2));

    private static readonly Regex CodeRegex = new(
        "<code[^>]*>(?<content>.*?)</code>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(2));

    private static readonly Regex BoldRegex = new(
        "<b>(?<content>.*?)</b>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private static readonly Regex BreakRegex = new(
        "<br\\s*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private static readonly Regex ResidualTagRegex = new(
        "</?(details|summary|pre|code|b)[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    private static readonly Regex ExcessBlankLinesRegex = new(
        "\\n{3,}",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// Rewrites HTML-enhanced markdown into Bitbucket-compatible markdown.
    /// </summary>
    /// <param name="markdown">The rendered markdown document.</param>
    /// <returns>Markdown with HTML-only constructs removed or downgraded.</returns>
    public static string Process(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return markdown;
        }

        var normalized = markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("&nbsp;", " ", StringComparison.Ordinal);

        normalized = SpanTagRegex.Replace(normalized, string.Empty);
        normalized = DetailsBlockRegex.Replace(normalized, match => $"\n\n{match.Groups["summary"].Value}\n\n");
        normalized = ClosingDetailsRegex.Replace(normalized, "\n");
        normalized = PreCodeRegex.Replace(normalized, match => RenderFencedCode(NormalizeBlockCode(match.Groups["content"].Value)));
        normalized = CodeRegex.Replace(normalized, match => RenderInlineCode(NormalizeInlineCode(match.Groups["content"].Value)));
        normalized = BoldRegex.Replace(normalized, match => $"**{match.Groups["content"].Value}**");
        normalized = BreakRegex.Replace(normalized, " / ");
        normalized = ResidualTagRegex.Replace(normalized, string.Empty);
        normalized = ExcessBlankLinesRegex.Replace(normalized, "\n\n");

        return normalized.TrimEnd();
    }

    /// <summary>
    /// Normalizes HTML-encoded inline code content into a single markdown-safe line.
    /// </summary>
    /// <param name="content">The HTML code-tag inner content.</param>
    /// <returns>Markdown-safe inline code content.</returns>
    private static string NormalizeInlineCode(string content)
    {
        var normalized = ConvertHtmlToPlainText(content, " / ")
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return normalized;
    }

    /// <summary>
    /// Normalizes HTML-encoded block code content into newline-preserving plain text.
    /// </summary>
    /// <param name="content">The HTML code-tag inner content.</param>
    /// <returns>Plain text block content.</returns>
    private static string NormalizeBlockCode(string content)
    {
        return ConvertHtmlToPlainText(content, "\n").Trim('\n');
    }

    /// <summary>
    /// Converts HTML fragments used in markdown output into plain text.
    /// </summary>
    /// <param name="content">The HTML fragment to simplify.</param>
    /// <param name="lineBreakReplacement">Replacement used for HTML break tags.</param>
    /// <returns>Plain text content.</returns>
    private static string ConvertHtmlToPlainText(string content, string lineBreakReplacement)
    {
        var decoded = WebUtility.HtmlDecode(content);
        var withoutBreaks = BreakRegex.Replace(decoded, lineBreakReplacement);
        return withoutBreaks.Replace("\u00A0", " ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Renders a markdown inline code span using a fence that can contain escaped backticks.
    /// </summary>
    /// <param name="content">Inline content to wrap.</param>
    /// <returns>Markdown inline code span.</returns>
    private static string RenderInlineCode(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var fence = GetInlineCodeFence(content);
        return $"{fence}{content}{fence}";
    }

    /// <summary>
    /// Selects an inline-code fence longer than any backtick run in the content.
    /// </summary>
    /// <param name="content">Inline content to wrap.</param>
    /// <returns>A markdown code-span fence that does not collide with the content.</returns>
    private static string GetInlineCodeFence(string content)
    {
        var longestBacktickRun = 0;
        var currentBacktickRun = 0;

        foreach (var character in content)
        {
            if (character == '`')
            {
                currentBacktickRun++;
                longestBacktickRun = Math.Max(longestBacktickRun, currentBacktickRun);
            }
            else
            {
                currentBacktickRun = 0;
            }
        }

        return new string('`', longestBacktickRun + 1);
    }

    /// <summary>
    /// Renders a fenced code block for multiline content.
    /// </summary>
    /// <param name="content">Block content to render.</param>
    /// <returns>Markdown fenced code block.</returns>
    private static string RenderFencedCode(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        return $"```\n{content}\n```";
    }
}
