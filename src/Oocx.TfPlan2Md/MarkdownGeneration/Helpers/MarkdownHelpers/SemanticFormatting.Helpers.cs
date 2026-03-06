namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Helper routines that apply semantic icons and detection rules for attribute formatting.
/// </summary>
internal static partial class MarkdownHelpers
{
    /// <summary>
    /// Determines whether a value represents a boolean and formats it with icons.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="value">The raw value to evaluate.</param>
    /// <param name="context">The rendering context (table or summary).</param>
    /// <param name="formatted">Formatted result when the value is boolean.</param>
    /// <returns>True when the value was formatted as a boolean; otherwise false.</returns>
    private static bool TryFormatBoolean(string value, ValueFormatContext context, out string formatted)
    {
        if (bool.TryParse(value, out var boolValue))
        {
            var icon = boolValue ? "✅" : "❌";
            var text = boolValue ? "true" : "false";
            var iconText = $"{icon}{NonBreakingSpace}{text}";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(iconText) : iconText;
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Formats access rules (Allow/Deny) with semantic icons when applicable.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic application.</param>
    /// <param name="value">The access value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when formatted; otherwise false.</returns>
    private static bool TryFormatAccess(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (!attributeName.Equals("access", StringComparison.OrdinalIgnoreCase)
            && !attributeName.Equals("action", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        if (value.Equals("allow", StringComparison.OrdinalIgnoreCase))
        {
            const string allowText = "✅" + NonBreakingSpace + "Allow";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(allowText) : allowText;
            return true;
        }

        if (value.Equals("deny", StringComparison.OrdinalIgnoreCase))
        {
            const string denyText = "⛔" + NonBreakingSpace + "Deny";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(denyText) : denyText;
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Formats network direction values with semantic icons.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic application.</param>
    /// <param name="value">The direction value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when formatted; otherwise false.</returns>
    private static bool TryFormatDirection(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (!attributeName.Equals("direction", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        if (value.Equals("inbound", StringComparison.OrdinalIgnoreCase))
        {
            const string inboundText = "⬇️" + NonBreakingSpace + "Inbound";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(inboundText) : inboundText;
            return true;
        }

        if (value.Equals("outbound", StringComparison.OrdinalIgnoreCase))
        {
            const string outboundText = "⬆️" + NonBreakingSpace + "Outbound";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(outboundText) : outboundText;
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Formats protocol values with semantic icons.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name driving semantic application.</param>
    /// <param name="value">The protocol value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when formatted; otherwise false.</returns>
    private static bool TryFormatProtocol(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (!attributeName.Equals("protocol", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        if (value.Equals("tcp", StringComparison.OrdinalIgnoreCase))
        {
            const string tcpText = "🔗" + NonBreakingSpace + "TCP";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(tcpText) : tcpText;
            return true;
        }

        if (value.Equals("udp", StringComparison.OrdinalIgnoreCase))
        {
            const string udpText = "📨" + NonBreakingSpace + "UDP";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(udpText) : udpText;
            return true;
        }

        if (value.Equals("icmp", StringComparison.OrdinalIgnoreCase))
        {
            const string icmpText = "📡" + NonBreakingSpace + "ICMP";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(icmpText) : icmpText;
            return true;
        }

        if (value.Equals("*", StringComparison.OrdinalIgnoreCase))
        {
            formatted = context == ValueFormatContext.Table ? FormatCodeTable("✳️") : "✳️";
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Determines whether an attribute represents a port and formats it with the port icon.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted result when the attribute is a port.</param>
    /// <returns>True when the attribute was formatted as a port; otherwise false.</returns>
    private static bool TryFormatPort(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        var isPortAttribute = attributeName.Contains("port", StringComparison.OrdinalIgnoreCase)
                              || attributeName.Contains("destination_port", StringComparison.OrdinalIgnoreCase)
                              || attributeName.Contains("source_port", StringComparison.OrdinalIgnoreCase);

        if (!isPortAttribute)
        {
            formatted = string.Empty;
            return false;
        }

        if (value.Equals("*", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        if (int.TryParse(value, out _) || value.Contains('-', StringComparison.Ordinal))
        {
            var portText = $"🔌{NonBreakingSpace}{value}";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(portText) : portText;
            return true;
        }

        formatted = string.Empty;
        return false;
    }

}
