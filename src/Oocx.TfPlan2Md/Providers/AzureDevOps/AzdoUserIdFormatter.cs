using System;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;

namespace Oocx.TfPlan2Md.Providers.AzureDevOps;

/// <summary>
/// Formats Azure DevOps user identifiers with mapped display names.
/// </summary>
/// <remarks>
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </remarks>
internal sealed class AzdoUserIdFormatter : IValueFormatter
{
    /// <summary>
    /// Mapper used to resolve Azure DevOps user display names.
    /// </summary>
    private readonly AzdoUserMapper _userMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzdoUserIdFormatter"/> class.
    /// </summary>
    /// <param name="userMapper">Mapper used for user name resolution.</param>
    internal AzdoUserIdFormatter(AzdoUserMapper userMapper)
    {
        ArgumentNullException.ThrowIfNull(userMapper);
        _userMapper = userMapper;
    }

    /// <summary>
    /// Attempts to format Azure DevOps user identifiers into mapped display names.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>Formatted user name when resolved; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.Value))
        {
            return null;
        }

        var displayName = _userMapper.GetName(context.Value);
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Equals(context.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var enriched = $"👤\u00A0{displayName} ({context.Value})";
        return MarkdownHelpers.FormatCodeTable(enriched);
    }
}
