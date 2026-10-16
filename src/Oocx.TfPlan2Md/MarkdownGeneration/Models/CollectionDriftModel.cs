using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// One changed collection path of a drifted resource together with its element diff.
/// </summary>
/// <param name="Path">The attribute path of the collection.</param>
/// <param name="Collection">The structured element diff.</param>
internal sealed record CollectionDriftPath(string Path, CollectionChangeModel Collection);

/// <summary>
/// Drift detected outside Terraform in primitive collections of one resource. Collection drift is never grouped
/// across resources because its element deltas depend on each resource's own membership and order.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md (section 8).
/// </summary>
internal sealed class CollectionDriftModel
{
    /// <summary>
    /// Gets the Terraform resource type.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Gets the Terraform address of the drifted resource.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the naturally ordered changed collection paths of this resource.
    /// </summary>
    public required IReadOnlyList<CollectionDriftPath> Paths { get; init; }
}
