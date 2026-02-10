using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Registry interface for parent-child resource relationships.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
internal interface IParentChildRelationshipRegistry
{
    /// <summary>
    /// Registers a parent-child relationship definition.
    /// </summary>
    /// <param name="relationship">The relationship to register.</param>
    void Register(ParentChildRelationship relationship);

    /// <summary>
    /// Gets the registered relationships for a parent resource type.
    /// </summary>
    /// <param name="parentResourceType">The parent resource type to query.</param>
    /// <returns>The relationships associated with the parent type.</returns>
    IReadOnlyList<ParentChildRelationship> GetRelationshipsForParent(string parentResourceType);

    /// <summary>
    /// Gets all registered child resource types.
    /// </summary>
    /// <returns>The set of child resource types.</returns>
    IReadOnlySet<string> GetAllChildResourceTypes();

    /// <summary>
    /// Determines whether the specified type is a registered child resource type.
    /// </summary>
    /// <param name="resourceType">The resource type to evaluate.</param>
    /// <returns><c>true</c> if the type is a registered child resource; otherwise, <c>false</c>.</returns>
    bool IsChildResourceType(string resourceType);
}
