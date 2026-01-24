using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Performs normalization steps shared by all rendering flavors.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
internal static class CommonHtmlNormalization
{
    /// <summary>
    /// Applies baseline normalization for break tags, rule tags, and non-breaking spaces.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>Normalized HTML fragment.</returns>
    public static string Normalize(string html)
    {
        var withNormalizedBreaks = NormalizeBreakTags(html);
        var withNormalizedRules = NormalizeHorizontalRuleTags(withNormalizedBreaks);
        return ReplaceNonBreakingSpaces(withNormalizedRules);
    }

    /// <summary>
    /// Normalizes line break tags to self-closing form for consistent comparisons.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with canonical &lt;br/&gt; tags.</returns>
    private static string NormalizeBreakTags(string html)
    {
        return Regex.Replace(html, "<br\\s*/?>", "<br/>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Normalizes horizontal rule tags to self-closing form for consistent comparisons.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with canonical &lt;hr/&gt; tags.</returns>
    private static string NormalizeHorizontalRuleTags(string html)
    {
        return Regex.Replace(html, "<hr\\s*/?>", "<hr/>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Replaces non-breaking space characters with HTML entities for clarity.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with explicit &amp;nbsp; entities.</returns>
    private static string ReplaceNonBreakingSpaces(string html)
    {
        return html.Replace("\u00A0", "&nbsp;", StringComparison.Ordinal);
    }
}
