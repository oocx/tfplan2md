using System.Net;
using System.Text.RegularExpressions;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Identity-related semantic formatting helpers.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Determines whether an attribute represents a principal type and formats it with the appropriate icon.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted result when the attribute is a principal type.</param>
    /// <returns>True when the attribute was formatted as a principal type; otherwise false.</returns>
    private static bool TryFormatPrincipalType(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (!attributeName.Equals("principal_type", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        if (value.Equals("User", StringComparison.OrdinalIgnoreCase))
        {
            const string userText = "👤" + NonBreakingSpace + "User";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(userText) : userText;
            return true;
        }

        if (value.Equals("Group", StringComparison.OrdinalIgnoreCase))
        {
            const string groupText = "👥" + NonBreakingSpace + "Group";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(groupText) : groupText;
            return true;
        }

        if (value.Equals("ServicePrincipal", StringComparison.OrdinalIgnoreCase))
        {
            const string spText = "💻" + NonBreakingSpace + "ServicePrincipal";
            formatted = context == ValueFormatContext.Table ? FormatCodeTable(spText) : spText;
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Determines whether an attribute represents a role definition and formats it with the role icon.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted result when the attribute is a role definition.</param>
    /// <returns>True when the attribute was formatted as a role definition; otherwise false.</returns>
    private static bool TryFormatRoleDefinition(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (!attributeName.Equals("role_definition_name", StringComparison.OrdinalIgnoreCase)
            && !attributeName.Equals("role_definition_id", StringComparison.OrdinalIgnoreCase)
            && !attributeName.Equals("role", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        var roleText = $"🛡️{NonBreakingSpace}{value}";
        formatted = context == ValueFormatContext.Table ? FormatCodeTable(roleText) : roleText;
        return true;
    }

    /// <summary>
    /// Determines whether an attribute represents a subscription value and formats it with the key icon.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted result when the attribute is a subscription identifier.</param>
    /// <returns>True when the attribute was formatted; otherwise false.</returns>
    private static bool TryFormatSubscriptionAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (!attributeName.Equals("subscription_id", StringComparison.OrdinalIgnoreCase)
            && !attributeName.Equals("subscription", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        formatted = FormatIconValue($"🔑 {value}", context, false);
        return true;
    }

    /// <summary>
    /// Determines whether an attribute represents Azure AD identity values and formats them with icons.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when the attribute was formatted; otherwise false.</returns>
    private static bool TryFormatIdentityAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (attributeName.Equals("user_principal_name", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValue($"🆔 {value}", context, false);
            return true;
        }

        if (attributeName.Equals("mail", StringComparison.OrdinalIgnoreCase)
            || attributeName.Equals("user_email_address", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValue($"📧 {value}", context, false);
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Formats icon-bearing values with context-aware code wrapping and optional parentheses.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="iconValue">The value including the icon prefix.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="wrapInParentheses">Whether the summary context should wrap the value in parentheses.</param>
    /// <returns>Formatted value with appropriate code wrapping.</returns>
    private static string FormatIconValue(string iconValue, ValueFormatContext context, bool wrapInParentheses)
    {
        var formattedValue = EnsureNonBreakingIconSpacing(iconValue);
        var formatted = context == ValueFormatContext.Table ? FormatCodeTable(formattedValue) : FormatCodeSummary(formattedValue);
        if (wrapInParentheses && context == ValueFormatContext.Summary)
        {
            return $"({formatted})";
        }

        return formatted;
    }

    /// <summary>
    /// Formats icon-prefixed values for summary rendering with code spans and non-breaking spacing.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="iconValue">The icon-prefixed value to format.</param>
    /// <returns>Formatted value suitable for summary contexts.</returns>
    public static string FormatIconValueSummary(string? iconValue)
    {
        if (string.IsNullOrWhiteSpace(iconValue))
        {
            return string.Empty;
        }

        return FormatIconValue(iconValue, ValueFormatContext.Summary, false);
    }

    /// <summary>
    /// Formats icon-prefixed values for table rendering with code spans and non-breaking spacing.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="iconValue">The icon-prefixed value to format.</param>
    /// <returns>Formatted value suitable for table contexts.</returns>
    public static string FormatIconValueTable(string? iconValue)
    {
        if (string.IsNullOrWhiteSpace(iconValue))
        {
            return string.Empty;
        }

        return FormatIconValue(iconValue, ValueFormatContext.Table, false);
    }

    /// <summary>
    /// Formats name-related attributes with semantic icons for the requested context.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate (e.g., name or resource_group_name).</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="context">The rendering context.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when the attribute was formatted; otherwise false.</returns>
    private static bool TryFormatNameAttribute(string attributeName, string value, ValueFormatContext context, out string formatted)
    {
        if (attributeName.Equals("resource_group_name", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValue($"📁 {value}", context, false);
            return true;
        }

        if (attributeName.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValue($"🆔 {value}", context, false);
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Formats name-related attributes with semantic icons without applying code wrapping.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when the attribute was formatted; otherwise false.</returns>
    private static bool TryFormatNameAttributePlain(string attributeName, string value, out string formatted)
    {
        if (attributeName.Equals("resource_group_name", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValuePlain($"📁 {value}");
            return true;
        }

        if (attributeName.Equals("name", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValuePlain($"🆔 {value}");
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Determines whether an attribute represents Azure AD identity values and formats them without code wrapping.
    /// Related feature: docs/features/053-azuread-resources-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="formatted">Formatted output when matched.</param>
    /// <returns>True when the attribute was formatted; otherwise false.</returns>
    private static bool TryFormatIdentityAttributePlain(string attributeName, string value, out string formatted)
    {
        if (attributeName.Equals("user_principal_name", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValuePlain($"🆔 {value}");
            return true;
        }

        if (attributeName.Equals("mail", StringComparison.OrdinalIgnoreCase)
            || attributeName.Equals("user_email_address", StringComparison.OrdinalIgnoreCase))
        {
            formatted = FormatIconValuePlain($"📧 {value}");
            return true;
        }

        formatted = string.Empty;
        return false;
    }

    /// <summary>
    /// Determines whether an attribute represents a subscription value and formats it with the key icon.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <param name="value">The raw attribute value.</param>
    /// <param name="formatted">Formatted result when the attribute is a subscription identifier.</param>
    /// <returns>True when the attribute was formatted; otherwise false.</returns>
    private static bool TryFormatSubscriptionAttributePlain(string attributeName, string value, out string formatted)
    {
        if (!attributeName.Equals("subscription_id", StringComparison.OrdinalIgnoreCase)
            && !attributeName.Equals("subscription", StringComparison.OrdinalIgnoreCase))
        {
            formatted = string.Empty;
            return false;
        }

        formatted = FormatIconValuePlain($"🔑 {value}");
        return true;
    }

    /// <summary>
    /// Applies non-breaking spacing to icon-prefixed values without adding code fences.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    /// <param name="iconValue">Icon-prefixed value.</param>
    /// <returns>Value with non-breaking spacing preserved.</returns>
    private static string FormatIconValuePlain(string iconValue)
    {
        return EnsureNonBreakingIconSpacing(iconValue);
    }

    /// <summary>
    /// Replaces the first regular space after an icon with a non-breaking space to prevent icon-value separation in rendered markdown.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="iconValue">The icon-prefixed value to normalize.</param>
    /// <returns>The icon value with a non-breaking space between the icon and text.</returns>
    private static string EnsureNonBreakingIconSpacing(string iconValue)
    {
        var firstSpace = iconValue.IndexOf(' ');
        if (firstSpace < 0)
        {
            return iconValue;
        }

        return iconValue[..firstSpace] + NonBreakingSpace + iconValue[(firstSpace + 1)..];
    }

    /// <summary>
    /// Determines whether an attribute name represents a location value.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeName">The attribute name to evaluate.</param>
    /// <returns>True when the attribute represents a location.</returns>
    private static bool IsLocationAttribute(string attributeName)
    {
        return attributeName.Equals("location", StringComparison.OrdinalIgnoreCase)
               || attributeName.Equals("region", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a value resembles an IP address or CIDR block.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>True when the value is an IP address or CIDR.</returns>
    private static bool IsIpAddressOrCidr(string value)
    {
        if (!value.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        if (IPAddress.TryParse(value, out _))
        {
            return true;
        }

        return Regex.IsMatch(value, "^([0-9]{1,3}\\.){3}[0-9]{1,3}/[0-9]{1,2}$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    }
}
