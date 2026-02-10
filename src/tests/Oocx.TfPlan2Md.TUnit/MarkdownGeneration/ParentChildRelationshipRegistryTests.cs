using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests the parent-child relationship registry infrastructure.
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// </summary>
public class ParentChildRelationshipRegistryTests
{
    /// <summary>
    /// Ensures multiple relationships can be registered for a single parent resource.
    /// </summary>
    [Test]
    public void ParentChildRelationshipRegistry_MultipleRelationshipsPerParent_ReturnsAll()
    {
        var registry = new ParentChildRelationshipRegistry();
        var members = CreateRelationship("azuredevops_team", "azuredevops_team_members", "Members");
        var administrators = CreateRelationship("azuredevops_team", "azuredevops_team_administrators", "Administrators");

        registry.Register(members);
        registry.Register(administrators);

        var relationships = registry.GetRelationshipsForParent("azuredevops_team");

        relationships.Should().HaveCount(2);
        relationships.Should().Contain(relationship => relationship.ChildResourceType == "azuredevops_team_members");
        relationships.Should().Contain(relationship => relationship.ChildResourceType == "azuredevops_team_administrators");
    }

    /// <summary>
    /// Ensures child resource types are tracked and resolved by the registry.
    /// </summary>
    [Test]
    public void ParentChildRelationshipRegistry_ChildTypes_ReturnsRegisteredTypes()
    {
        var registry = new ParentChildRelationshipRegistry();
        var groupMember = CreateRelationship("azuread_group", "azuread_group_member", "Members");
        var teamMember = CreateRelationship("azuredevops_team", "azuredevops_team_members", "Members");

        registry.Register(groupMember);
        registry.Register(teamMember);

        registry.GetAllChildResourceTypes().Should().BeEquivalentTo(new HashSet<string>
        {
            "azuread_group_member",
            "azuredevops_team_members"
        });

        registry.IsChildResourceType("azuread_group_member").Should().BeTrue();
        registry.IsChildResourceType("azuredevops_team_members").Should().BeTrue();
        registry.IsChildResourceType("azuredevops_team").Should().BeFalse();
    }

    /// <summary>
    /// Builds a relationship definition for registry testing.
    /// </summary>
    /// <param name="parentType">The parent resource type.</param>
    /// <param name="childType">The child resource type.</param>
    /// <param name="label">The child group label.</param>
    /// <returns>A relationship definition for tests.</returns>
    private static ParentChildRelationship CreateRelationship(string parentType, string childType, string label)
    {
        return new ParentChildRelationship
        {
            ParentResourceType = parentType,
            ChildResourceType = childType,
            ChildGroupLabel = label,
            TableColumns = [new ChildTableColumn { Header = "Member", PropertyName = "member" }],
            RowExtractor = new DummyRowExtractor()
        };
    }

    /// <summary>
    /// Row extractor used for registry tests.
    /// </summary>
    private sealed class DummyRowExtractor : IChildRowExtractor
    {
        /// <summary>
        /// Returns an empty row to satisfy the interface contract.
        /// </summary>
        /// <param name="childState">The child state object.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="valueFormatterRegistry">The value formatter registry.</param>
        /// <param name="iconProviderRegistry">The icon provider registry.</param>
        /// <returns>An empty dictionary.</returns>
        public IReadOnlyDictionary<string, string> ExtractRow(
            object? childState,
            string providerName,
            ValueFormatterRegistry? valueFormatterRegistry,
            IconProviderRegistry? iconProviderRegistry)
        {
            return new Dictionary<string, string>();
        }
    }
}
