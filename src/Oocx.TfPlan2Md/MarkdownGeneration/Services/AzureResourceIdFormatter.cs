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
    /// Optional scope formatter for display name enrichment.
    /// </summary>
    private readonly EnrichedAzureScopeFormatter? _scopeFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureResourceIdFormatter"/> class.
    /// </summary>
    /// <param name="scopeFormatter">Optional formatter for display name enrichment.</param>
    internal AzureResourceIdFormatter(EnrichedAzureScopeFormatter? scopeFormatter = null)
    {
        _scopeFormatter = scopeFormatter;
    }

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

        if (!AzureScopeParser.IsAzureResourceId(context.Value))
        {
            return null;
        }

        return _scopeFormatter != null
            ? _scopeFormatter.FormatScope(context.Value)
            : AzureScopeParser.ParseScope(context.Value);
    }
}
