using System.Net;
using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.HtmlRenderer.Rendering;

/// <summary>
/// Applies Azure DevOps-specific formatting and attribute adjustments.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md
/// </summary>
internal static class AzureDevOpsHtmlPostProcessor
{
    /// <summary>
    /// Applies the Azure DevOps rendering adjustments to the provided HTML fragment.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>Adjusted HTML fragment.</returns>
    public static string Process(string html)
    {
        var withNormalizedDetails = NormalizeDetailsTags(html);
        var withDiffStyles = NormalizeAzdoDiffInlineStyles(withNormalizedDetails);
        var withTrimmedStyles = TrimTrailingSemicolons(withDiffStyles);
        return RewriteHeadingIdsWithPrefix(withTrimmedStyles, "user-content-");
    }

    /// <summary>
    /// Normalizes details tags by trimming trailing style semicolons and removing stray spaces.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with normalized details tags.</returns>
    private static string NormalizeDetailsTags(string html)
    {
        return Regex.Replace(html, "<details(?<attrs>[^>]*)>", match =>
        {
            var attrs = match.Groups["attrs"].Value.Trim();
            if (string.IsNullOrEmpty(attrs))
            {
                return "<details>";
            }

            var normalized = Regex.Replace(attrs, "style=\"(?<style>[^\"]*)\"", styleMatch =>
            {
                var style = styleMatch.Groups["style"].Value.Trim();
                style = style.TrimEnd(';');
                return $"style=\"{style}\"";
            },
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

            return $"<details{(normalized.StartsWith(' ') ? string.Empty : " ")}{normalized}>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Adds Azure DevOps-like display and whitespace styles to inline diff code and span elements.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with normalized inline diff styles.</returns>
    private static string NormalizeAzdoDiffInlineStyles(string html)
    {
        var withCodeStyles = Regex.Replace(html, "<code\\s+style=\\\"(?<style>[^\\\"]*)\\\"(?<rest>[^>]*)>", match =>
        {
            var style = match.Groups["style"].Value;
            style = EnsureStyleDirective(style, "display:block");
            style = EnsureStyleDirective(style, "white-space:normal");
            style = EnsureStyleDirective(style, "padding:0");
            style = EnsureStyleDirective(style, "margin:0");
            return $"<code style=\"{style}\"{match.Groups["rest"].Value}>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var withSpanStyles = Regex.Replace(withCodeStyles, "<span\\s+style=\\\"(?<style>[^\\\"]*)\\\"(?<rest>[^>]*)>", match =>
        {
            var style = match.Groups["style"].Value;
            if (style.Contains("border-left", StringComparison.OrdinalIgnoreCase) || style.Contains("background-color", StringComparison.OrdinalIgnoreCase))
            {
                var hasDisplay = Regex.IsMatch(style, "(^|;\\s*)display\\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                if (!hasDisplay)
                {
                    style = EnsureStyleDirective(style, "display:inline-block");
                }
            }

            return $"<span style=\"{style}\"{match.Groups["rest"].Value}>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return withSpanStyles;
    }

    /// <summary>
    /// Trims trailing semicolons in style attributes to mirror platform normalization.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <returns>HTML with trimmed style declarations.</returns>
    private static string TrimTrailingSemicolons(string html)
    {
        return Regex.Replace(html, "style=\"(?<style>[^\"]*?)\"", match =>
        {
            var style = match.Groups["style"].Value.Trim();
            style = style.TrimEnd(';');
            return $"style=\"{style}\"";
        },
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Appends a CSS directive to a style string when it is not already present.
    /// </summary>
    /// <param name="style">Existing style string.</param>
    /// <param name="directive">Directive to enforce.</param>
    /// <returns>Updated style string.</returns>
    private static string EnsureStyleDirective(string style, string directive)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return directive;
        }

        if (HasStyleDirective(style, directive))
        {
            return style;
        }

        var trimmed = style.Trim();
        return trimmed.EndsWith(';') ? $"{trimmed} {directive}" : $"{trimmed}; {directive}";
    }

    /// <summary>
    /// Determines whether a style attribute already contains the given directive, ignoring whitespace variance.
    /// </summary>
    /// <param name="style">Existing style string.</param>
    /// <param name="directive">Directive to search for.</param>
    /// <returns>True if the directive already exists.</returns>
    private static bool HasStyleDirective(string style, string directive)
    {
        var parts = directive.Split(':', 2);
        if (parts.Length != 2)
        {
            return style.Contains(directive, StringComparison.OrdinalIgnoreCase);
        }

        var property = parts[0].Trim();
        var value = parts[1].Trim();

        if (property.Length == 0 || value.Length == 0)
        {
            return style.Contains(directive, StringComparison.OrdinalIgnoreCase);
        }

        var pattern = $"(^|;\\s*){Regex.Escape(property)}\\s*:\\s*{Regex.Escape(value)}(\\s*(;|$))";
        return Regex.IsMatch(style, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <summary>
    /// Prefixes heading id attributes to approximate Azure DevOps user-content ids.
    /// </summary>
    /// <param name="html">HTML fragment.</param>
    /// <param name="prefix">Prefix to apply to existing ids.</param>
    /// <returns>HTML with prefixed heading ids.</returns>
    private static string RewriteHeadingIdsWithPrefix(string html, string prefix)
    {
        return Regex.Replace(html, "<(h[1-6])([^>]*)>(.*?)</\\1>", match =>
        {
            var tag = match.Groups[1].Value;
            var attributes = match.Groups[2].Value;
            var content = match.Groups[3].Value;
            var slug = CreateSlug(content);
            if (string.IsNullOrEmpty(slug))
            {
                return match.Value;
            }

            var withoutExistingId = Regex.Replace(attributes, "\\sid=\".*?\"", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return $"<{tag}{withoutExistingId} id=\"{prefix}{slug}\">{content}</{tag}>";
        },
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    }

    /// <summary>
    /// Builds a slug from heading content that preserves emoji and punctuation before prefixing.
    /// </summary>
    /// <param name="headingContent">Inner HTML of the heading.</param>
    /// <returns>URL-encoded slug derived from the heading text.</returns>
    private static string CreateSlug(string headingContent)
    {
        if (string.IsNullOrWhiteSpace(headingContent))
        {
            return string.Empty;
        }

        var withBackticks = Regex.Replace(headingContent, "<code>(.*?)</code>", match => $"`{match.Groups[1].Value}`", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var withoutTags = Regex.Replace(withBackticks, "<.*?>", string.Empty, RegexOptions.Singleline | RegexOptions.Compiled);
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var trimmed = decoded.Trim();

        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var lowered = trimmed.ToLowerInvariant();
        var hyphenated = Regex.Replace(lowered, "\\s+", "-", RegexOptions.Compiled);
        return Uri.EscapeDataString(hyphenated);
    }
}
