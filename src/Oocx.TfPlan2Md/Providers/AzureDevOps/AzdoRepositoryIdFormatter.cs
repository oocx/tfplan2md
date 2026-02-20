using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Formats Azure DevOps repository identifiers with mapped display names.
/// </summary>
/// <remarks>
/// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
/// </remarks>
internal sealed class AzdoRepositoryIdFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve Azure DevOps repository display names.
    /// </summary>
    private readonly AzdoRepositoryMapper _repositoryMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoRepositoryIdFormatter"/> class.
    /// </summary>
    /// <param name="repositoryMapper">Mapper used for repository name resolution.</param>
    internal AzdoRepositoryIdFormatter(AzdoRepositoryMapper repositoryMapper)
    {
        ArgumentNullException.ThrowIfNull(repositoryMapper);
        _repositoryMapper = repositoryMapper;
    }

    /// <summary>
    /// Attempts to format Azure DevOps repository identifiers into mapped display names.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted repository name when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var displayName = _repositoryMapper.GetName(context.Value);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Equals(context.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var enriched = $"🗃️\u00A0{displayName} ({context.Value})";
        return ScribanHelpers.FormatCodeTable(enriched);
    }
}
