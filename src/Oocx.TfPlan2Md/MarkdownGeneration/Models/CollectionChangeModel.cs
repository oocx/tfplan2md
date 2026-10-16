using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Describes what one side of a primitive collection holds.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md.
/// </summary>
public enum CollectionState
{
    /// <summary>The attribute does not exist on this side.</summary>
    Absent,

    /// <summary>The attribute is JSON <c>null</c> on this side.</summary>
    Null,

    /// <summary>The attribute is an empty collection.</summary>
    Empty,

    /// <summary>The attribute holds known elements.</summary>
    Elements,

    /// <summary>The whole collection is unknown after apply.</summary>
    Unknown,

    /// <summary>The collection is sensitive and masked; no element data is carried.</summary>
    Masked
}

/// <summary>
/// The diff status of one collection element occurrence.
/// </summary>
public enum CollectionElementStatus
{
    /// <summary>The occurrence is retained in the same relative order.</summary>
    Unchanged,

    /// <summary>The occurrence exists only before.</summary>
    Removed,

    /// <summary>The occurrence exists only after.</summary>
    Added,

    /// <summary>The occurrence is retained but sits at a different relative position.</summary>
    Moved,

    /// <summary>The element is unknown after apply, so no status can be computed.</summary>
    Unknown
}

/// <summary>
/// One occurrence of a primitive value inside a collection.
/// </summary>
/// <param name="Index">Index of the occurrence on its own side.</param>
/// <param name="Value">Text of the value; <c>null</c> for a JSON null element.</param>
/// <param name="Kind">The JSON kind, so <c>"1"</c> and <c>1</c> stay different elements.</param>
/// <param name="Status">The diff status of this occurrence.</param>
public sealed record CollectionElementModel(int Index, string? Value, JsonValueKind Kind, CollectionElementStatus Status);

/// <summary>
/// Structured diff of a collection of primitive values, rendered as one logical attribute.
/// Related feature: docs/features/148-compact-collection-diffs/architecture.md.
/// </summary>
public sealed class CollectionChangeModel
{
    /// <summary>
    /// Gets the state of the collection before the change.
    /// </summary>
    public CollectionState BeforeState { get; init; }

    /// <summary>
    /// Gets the state of the collection after the change.
    /// </summary>
    public CollectionState AfterState { get; init; }

    /// <summary>
    /// Gets the before-side occurrences in their original order. Empty when masked.
    /// </summary>
    public IReadOnlyList<CollectionElementModel> Before { get; init; } = [];

    /// <summary>
    /// Gets the after-side occurrences in their final order. Empty when masked.
    /// </summary>
    public IReadOnlyList<CollectionElementModel> After { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether retained occurrences changed their relative order.
    /// </summary>
    public bool OrderChanged { get; init; }

    /// <summary>
    /// Gets a value indicating whether unknown values prevent a definitive diff.
    /// </summary>
    public bool IsUnresolved { get; init; }
}
