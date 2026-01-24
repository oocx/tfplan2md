using System.Linq;
using System.Text;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Diff utilities used by large value and array diff formatting.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Appends a styled line prefixing removal or addition markers with consistent coloring.
    /// </summary>
    /// <param name="sb">String builder receiving the styled content.</param>
    /// <param name="line">Line text to render.</param>
    /// <param name="removed">True when the line represents a removal.</param>
    private static void AppendStyledLine(StringBuilder sb, string line, bool removed)
    {
        var lineStyle = removed
            ? "background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: 0;"
            : "background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: 0;";

        sb.Append("<span style=\"");
        sb.Append(lineStyle);
        sb.Append('"');
        sb.Append('>');
        sb.Append(removed ? "- " : "+ ");
        sb.Append(HtmlEncode(line));
        sb.AppendLine("</span>");
    }

    /// <summary>
    /// Appends a styled line while highlighting character-level differences against another line.
    /// </summary>
    /// <param name="sb">String builder receiving the styled content.</param>
    /// <param name="line">Line to render.</param>
    /// <param name="otherLine">Line to compare for highlighting common characters.</param>
    /// <param name="removed">True when rendering a removed line.</param>
    private static void AppendStyledLineWithCharDiff(StringBuilder sb, string line, string otherLine, bool removed)
    {
        var pairs = ComputeLcsPairs(line, otherLine);
        var commonMask = BuildCommonMask(line.Length, pairs.Select(p => p.BeforeIndex));
        var highlightColor = removed ? "#ffc0c0" : "#acf2bd";
        var lineStyle = removed
            ? "background-color: #fff5f5; border-left: 3px solid #d73a49; color: #24292e; display: block; padding-left: 8px; margin-left: 0;"
            : "background-color: #f0fff4; border-left: 3px solid #28a745; color: #24292e; display: block; padding-left: 8px; margin-left: 0;";

        sb.Append("<span style=\"");
        sb.Append(lineStyle);
        sb.Append('"');
        sb.Append('>');
        sb.Append(removed ? "- " : "+ ");
        sb.Append(ApplyCharHighlights(line, commonMask, highlightColor));
        sb.AppendLine("</span>");
    }

    /// <summary>
    /// Highlights differing character regions using span tags while preserving common characters.
    /// </summary>
    /// <param name="line">Line to render.</param>
    /// <param name="commonMask">Boolean mask indicating which positions are shared.</param>
    /// <param name="highlightColor">Background color for differing segments.</param>
    /// <returns>HTML string with highlighted differences.</returns>
    private static string ApplyCharHighlights(string line, bool[] commonMask, string highlightColor)
    {
        var sb = new StringBuilder();
        var inHighlight = false;

        for (var i = 0; i < line.Length; i++)
        {
            var isCommon = i < commonMask.Length && commonMask[i];
            if (!isCommon && !inHighlight)
            {
                sb.Append("<span style=\"background-color: ");
                sb.Append(highlightColor);
                sb.Append("; color: #24292e;\">");
                inHighlight = true;
            }
            else if (isCommon && inHighlight)
            {
                sb.Append("</span>");
                inHighlight = false;
            }

            sb.Append(HtmlEncode(line[i].ToString()));
        }

        if (inHighlight)
        {
            sb.Append("</span>");
        }

        return sb.ToString();
    }

}
