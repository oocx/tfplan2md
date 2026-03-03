namespace Oocx.TfPlan2Md.Providers.AzApi.Helpers.Models;

/// <summary>
/// Grouping kind for AzAPI flattened body paths.
/// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
/// </summary>
internal enum AzApiGroupKind
{
    /// <summary>
    /// Group created from array paths.
    /// </summary>
    Array,

    /// <summary>
    /// Group created from non-array shared prefixes.
    /// </summary>
    Prefix
}

/// <summary>
/// Describes a grouped set of flattened body properties.
/// Related feature: docs/features/034-azapi-attribute-grouping/specification.md.
/// </summary>
/// <param name="Prefix">Group prefix path.</param>
/// <param name="Kind">Grouping kind.</param>
/// <param name="MemberIndexes">Indices into the source flattened list.</param>
internal sealed record AzApiGroup(string Prefix, AzApiGroupKind Kind, IReadOnlyList<int> MemberIndexes)
{
    /// <summary>
    /// Gets the first source index for stable rendering order.
    /// </summary>
    internal int FirstIndex => MemberIndexes.Count == 0 ? int.MaxValue : MemberIndexes[0];
}
