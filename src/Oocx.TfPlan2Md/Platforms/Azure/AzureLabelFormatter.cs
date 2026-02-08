namespace Oocx.TfPlan2Md.Platforms.Azure;

/// <summary>
/// Provides shared icon formatting for Azure tenant and management group labels.
/// </summary>
/// <remarks>
/// Centralizes icon placement and spacing rules for Azure entity labels.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </remarks>
internal static class AzureLabelFormatter
{
    /// <summary>
    /// Non-breaking space used to keep icons attached to labels.
    /// </summary>
    internal const string NonBreakingSpace = "\u00A0";

    /// <summary>
    /// Icon for Azure tenant labels.
    /// </summary>
    internal const string TenantIcon = "🏢";

    /// <summary>
    /// Icon for Azure management group labels.
    /// </summary>
    internal const string ManagementGroupIcon = "🗂️";

    /// <summary>
    /// Formats a tenant label with the tenant icon and non-breaking space.
    /// </summary>
    /// <param name="label">The tenant label to prefix with the icon.</param>
    /// <returns>The icon-prefixed label, or empty string when the label is blank.</returns>
    internal static string FormatTenantLabel(string? label)
    {
        return FormatIconLabel(TenantIcon, label);
    }

    /// <summary>
    /// Formats a management group label with the management group icon and non-breaking space.
    /// </summary>
    /// <param name="label">The management group label to prefix with the icon.</param>
    /// <returns>The icon-prefixed label, or empty string when the label is blank.</returns>
    internal static string FormatManagementGroupLabel(string? label)
    {
        return FormatIconLabel(ManagementGroupIcon, label);
    }

    /// <summary>
    /// Builds a shared icon + label string with a non-breaking space.
    /// </summary>
    /// <param name="icon">The icon to prefix.</param>
    /// <param name="label">The label to append after the icon.</param>
    /// <returns>The icon-prefixed label, or empty string when the label is blank.</returns>
    private static string FormatIconLabel(string icon, string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        return $"{icon}{NonBreakingSpace}{label}";
    }
}
