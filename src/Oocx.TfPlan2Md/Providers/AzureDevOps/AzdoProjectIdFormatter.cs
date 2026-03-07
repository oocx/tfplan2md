using System;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Formats Azure DevOps project identifiers with mapped display names.
/// </summary>
/// <remarks>
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoProjectIdFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve Azure DevOps project display names.
    /// </summary>
    private readonly AzdoProjectMapper _projectMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoProjectIdFormatter"/> class.
    /// </summary>
    /// <param name="projectMapper">Mapper used for project name resolution.</param>
    internal AzdoProjectIdFormatter(AzdoProjectMapper projectMapper)
    {
        ArgumentNullException.ThrowIfNull(projectMapper);
        _projectMapper = projectMapper;
    }

    /// <summary>
    /// Attempts to format Azure DevOps project identifiers into mapped display names.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted project name when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return AzdoFormatterHelper.TryFormat(context.Value, _projectMapper.GetName, "📋");
    }
}
