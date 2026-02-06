using System;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Formats Azure resource IDs into human-readable scope summaries.
/// </summary>
/// <remarks>
/// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
/// </remarks>
internal sealed class AzureResourceIdFormatter : IValueFormatter
{
    /// <summary>
    /// Attempts to format Azure resource IDs into readable summaries.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted summary when the value is an Azure resource ID; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        return AzureScopeParser.IsAzureResourceId(context.Value)
            ? AzureScopeParser.ParseScope(context.Value)
            : null;
    }
}
