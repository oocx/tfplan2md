using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Applies GitHub-specific sanitization and attribute alignment.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
internal static class GitHubHtmlPostProcessor
{
    /// <summary>
    /// Matches inline style attributes so output mirrors GitHub sanitization rules.
    /// </summary>
    private static readonly Regex StyleRegex = new("(\\s)style\\s*=\\s*\"[^\"]*\"|(\\s)style\\s*=\\s*'[^']*'", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Applies the GitHub rendering adjustments to the provided HTML fragment.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>Adjusted HTML fragment.</returns>
    public static string Process(string html)
    {
        var withDirection = AddDirAuto(html);
        var withWrappedTables = WrapTablesForGitHub(withDirection);
        var withNotranslate = AddNotranslateToCode(withWrappedTables);
        var withoutIds = RemoveHeadingIds(withNotranslate);
        var withoutStyles = RemoveStyleAttributes(withoutIds);
        return CollapseDanglingTagWhitespace(withoutStyles);
    }

    /// <summary>
    /// Adds dir="auto" to headings and paragraphs when missing to mirror platform output.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with dir attributes on headings and paragraphs.</returns>
    private static string AddDirAuto(string html)
    {
        return Regex.Replace(html, "<(h[1-6]|p)(?=[\\s>])([^>]*)>", match =>
        {
            var tagName = match.Groups[1].Value;
            var attributes = match.Groups[2].Value;
            if (Regex.IsMatch(attributes, "\\sdir=", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            {
                return match.Value;
            }

            return $"<{tagName} dir=\"auto\"{attributes}>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Wraps tables with GitHub's accessibility helper and assigns role attributes.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with wrapped tables for GitHub fidelity.</returns>
    private static string WrapTablesForGitHub(string html)
    {
        return Regex.Replace(html, "<table(?<attrs>[^>]*)>(?<body>.*?)</table>", match =>
        {
            if (match.Value.Contains("markdown-accessiblity-table", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            var attributes = match.Groups["attrs"].Value;
            if (!Regex.IsMatch(attributes, "\\srole=", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            {
                attributes = $"{attributes} role=\"table\"";
            }

            if (!Regex.IsMatch(attributes, "\\stabindex=", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            {
                attributes = $"{attributes} tabindex=\"0\"";
            }

            return $"<markdown-accessiblity-table data-catalyst=\"\"><table{attributes}>{match.Groups["body"].Value}</table></markdown-accessiblity-table>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Adds GitHub-style notranslate classes to code elements to mirror platform rendering.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with notranslate applied to code tags.</returns>
    private static string AddNotranslateToCode(string html)
    {
        return Regex.Replace(html, "<code(?<attrs>[^>]*)>", match =>
        {
            var attributes = match.Groups["attrs"].Value;
            if (Regex.IsMatch(attributes, "notranslate", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            {
                return match.Value;
            }

            var classMatch = Regex.Match(attributes, "\\bclass\\s*=\\s*([\"'])(.*?)\\1", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
            if (classMatch.Success)
            {
                var beforeClass = attributes[..classMatch.Index];
                var afterClass = attributes[(classMatch.Index + classMatch.Length)..];
                var quote = classMatch.Groups[1].Value;
                var classes = $"{classMatch.Groups[2].Value} notranslate".Trim();
                return $"<code{beforeClass} class={quote}{classes}{quote}{afterClass}>";
            }

            return $"<code class=\"notranslate\"{attributes}>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Removes heading id attributes to align with GitHub rendering where ids are absent.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML without heading ids.</returns>
    private static string RemoveHeadingIds(string html)
    {
        return Regex.Replace(html, "<(h[1-6])(.*?)\\sid=\".*?\"(.*?)>", "<$1$2$3>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Strips style attributes from rendered HTML to mimic GitHub sanitization.
    /// </summary>
    /// <param name="html">HTML fragment to sanitize.</param>
    /// <returns>Sanitized HTML without inline style attributes.</returns>
    private static string RemoveStyleAttributes(string html)
    {
        return StyleRegex.Replace(html, match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
    }

    /// <summary>
    /// Removes dangling whitespace before tag closures introduced by attribute stripping.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with compact tag delimiters.</returns>
    private static string CollapseDanglingTagWhitespace(string html)
    {
        return Regex.Replace(html, "<(\\w+)\\s+>", "<$1>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }
}
