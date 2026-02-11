using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Registry for provider-specific parent summary rebuilders.
/// </summary>
/// <remarks>
/// Related issue: docs/issues/069-parent-child-summary-count-mismatch/analysis.md.
/// </remarks>
internal sealed class ParentSummaryRebuilderRegistry
{
    private readonly List<IParentSummaryRebuilder> _rebuilders = new();

    /// <summary>
    /// Registers a summary rebuilder.
    /// </summary>
    /// <param name="rebuilder">The rebuilder to register.</param>
    public void Register(IParentSummaryRebuilder rebuilder)
    {
        _rebuilders.Add(rebuilder);
    }

    /// <summary>
    /// Rebuilds the parent's summary if a registered rebuilder can handle it.
    /// </summary>
    /// <param name="context">The rebuild context.</param>
    /// <returns><c>true</c> if a rebuilder handled the parent.</returns>
    public bool TryRebuild(ParentSummaryRebuildContext context)
    {
        var rebuilder = _rebuilders.Find(r => r.CanRebuild(context.Parent));
        if (rebuilder != null)
        {
            rebuilder.RebuildSummary(context);
            return true;
        }

        return false;
    }
}
