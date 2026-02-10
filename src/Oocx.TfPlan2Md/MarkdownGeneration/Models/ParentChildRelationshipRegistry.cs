using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Stores and resolves parent-child resource relationships for inline rendering.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </remarks>
internal sealed class ParentChildRelationshipRegistry : IParentChildRelationshipRegistry
{
    /// <summary>
    /// Maps parent resource types to their registered relationships.
    /// </summary>
    private readonly Dictionary<string, List<ParentChildRelationship>> _relationshipsByParentType =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks all child resource types for quick lookup.
    /// </summary>
    private readonly HashSet<string> _childResourceTypes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a parent-child relationship definition.
    /// </summary>
    /// <param name="relationship">The relationship to register.</param>
    public void Register(ParentChildRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        if (!_relationshipsByParentType.TryGetValue(relationship.ParentResourceType, out var relationships))
        {
            relationships = [];
            _relationshipsByParentType[relationship.ParentResourceType] = relationships;
        }

        relationships.Add(relationship);
        _childResourceTypes.Add(relationship.ChildResourceType);
    }

    /// <summary>
    /// Gets the registered relationships for a parent resource type.
    /// </summary>
    /// <param name="parentResourceType">The parent resource type to query.</param>
    /// <returns>The relationships associated with the parent type.</returns>
    public IReadOnlyList<ParentChildRelationship> GetRelationshipsForParent(string parentResourceType)
    {
        ArgumentNullException.ThrowIfNull(parentResourceType);

        return _relationshipsByParentType.TryGetValue(parentResourceType, out var relationships)
            ? relationships
            : [];
    }

    /// <summary>
    /// Gets all registered child resource types.
    /// </summary>
    /// <returns>The set of child resource types.</returns>
    public IReadOnlySet<string> GetAllChildResourceTypes()
    {
        return _childResourceTypes;
    }

    /// <summary>
    /// Determines whether the specified type is a registered child resource type.
    /// </summary>
    /// <param name="resourceType">The resource type to evaluate.</param>
    /// <returns><c>true</c> if the type is a registered child resource; otherwise, <c>false</c>.</returns>
    public bool IsChildResourceType(string resourceType)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        return _childResourceTypes.Contains(resourceType);
    }
}
