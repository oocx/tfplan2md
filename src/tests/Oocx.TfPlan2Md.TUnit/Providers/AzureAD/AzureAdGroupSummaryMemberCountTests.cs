using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureAD;

/// <summary>
/// Tests for Azure AD group summary member count fix.
/// Related issue: docs/issues/070-parent-child-summary-member-counts/analysis.md.
/// </summary>
public class AzureAdGroupSummaryMemberCountTests
{
    /// <summary>
    /// Verifies that Azure AD group summaries show correct member counts when members are only inline.
    /// </summary>
    [Test]
    public void Build_AzureAdGroup_InlineMembersOnly_ShowsCorrectCounts()
    {
        // Arrange
        var plan = BuildPlanWithInlineMembersOnly();
        var model = BuildModel(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert
        group.SummaryHtml.Should().Contain("2 👤");  // 2 users
        group.SummaryHtml.Should().Contain("1 👥");  // 1 group
        group.SummaryHtml.Should().Contain("0 💻");  // 0 service principals
    }

    /// <summary>
    /// Verifies that Azure AD group summaries show correct member counts when members are only separate child resources.
    /// </summary>
    [Test]
    public void Build_AzureAdGroup_SeparateMembersOnly_ShowsCorrectCounts()
    {
        // Arrange
        var plan = BuildPlanWithSeparateMembersOnly();
        var model = BuildModel(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert - Summary should show counts from merged child resources
        group.SummaryHtml.Should().Contain("2 👤");  // 2 users
        group.SummaryHtml.Should().Contain("1 👥");  // 1 group
        group.SummaryHtml.Should().Contain("0 💻");  // 0 service principals

        // Verify child resources were merged
        group.ChildResourceGroups.Should().ContainSingle();
        group.ChildResourceGroups[0].Rows.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that Azure AD group summaries show correct member counts when members are both inline and separate (mixed).
    /// This is the bug scenario that was reported.
    /// </summary>
    [Test]
    public void Build_AzureAdGroup_MixedMembers_ShowsCorrectCounts()
    {
        // Arrange
        var plan = BuildPlanWithMixedMembers();
        var model = BuildModel(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert - Summary should show combined counts from both inline and separate members
        group.SummaryHtml.Should().Contain("3 👤");  // 3 users (2 inline + 1 separate)
        group.SummaryHtml.Should().Contain("1 👥");  // 1 group (inline)
        group.SummaryHtml.Should().Contain("1 💻");  // 1 service principal (separate)

        // Verify child resources were merged
        group.ChildResourceGroups.Should().ContainSingle();
        group.ChildResourceGroups[0].Rows.Should().HaveCount(5);
        group.ChildResourceGroups[0].HasMixedSources.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Azure AD group summaries handle groups with no members correctly.
    /// </summary>
    [Test]
    public void Build_AzureAdGroup_NoMembers_ShowsZeroCounts()
    {
        // Arrange
        var plan = BuildPlanWithNoMembers();
        var model = BuildModel(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert - Summary should show 0 counts for groups with no members
        group.SummaryHtml.Should().Contain("0 👤");
        group.SummaryHtml.Should().Contain("0 👥");
        group.SummaryHtml.Should().Contain("0 💻");
        group.ChildResourceGroups.Should().BeEmpty();
    }

    /// <summary>
    /// Builds a ReportModel from a TerraformPlan with Azure AD provider configured.
    /// </summary>
    /// <param name="plan">The plan to build.</param>
    /// <returns>The generated report model.</returns>
    private static ReportModel BuildModel(TerraformPlan plan)
    {
        var principals = new Dictionary<string, string>
        {
            ["user-1"] = "Alice",
            ["user-2"] = "Bob",
            ["user-3"] = "Charlie",
            ["group-1"] = "TeamA",
            ["sp-1"] = "AppService"
        };

        var principalTypes = new Dictionary<string, string>
        {
            ["user-1"] = "User",
            ["user-2"] = "User",
            ["user-3"] = "User",
            ["group-1"] = "Group",
            ["sp-1"] = "ServicePrincipal"
        };

        var principalMapper = new PrincipalMapper(principals, principalTypes);
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        var builder = new ReportModelBuilder(
            principalMapper: principalMapper,
            providerRegistry: providerRegistry);

        return builder.Build(plan);
    }

    /// <summary>
    /// Builds a plan with an Azure AD group that has only inline members.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithInlineMembersOnly()
    {
        var after = JsonDocument.Parse("""
        {
            "id": "group-id-1",
            "display_name": "Engineering Team",
            "members": ["user-1", "user-2", "group-1"]
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.engineering",
                    null,
                    "managed",
                    "azuread_group",
                    "engineering",
                    "azuread",
                    new Change(["create"], null, after, null, null, null))
            });
    }

    /// <summary>
    /// Builds a plan with an Azure AD group that has only separate child member resources.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithSeparateMembersOnly()
    {
        var groupAfter = JsonDocument.Parse("""
        {
            "id": "group-id-1",
            "display_name": "Engineering Team"
        }
        """).RootElement;

        var member1After = JsonDocument.Parse("""
        {
            "group_object_id": "group-id-1",
            "member_object_id": "user-1"
        }
        """).RootElement;

        var member2After = JsonDocument.Parse("""
        {
            "group_object_id": "group-id-1",
            "member_object_id": "user-2"
        }
        """).RootElement;

        var member3After = JsonDocument.Parse("""
        {
            "group_object_id": "group-id-1",
            "member_object_id": "group-1"
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.engineering",
                    null,
                    "managed",
                    "azuread_group",
                    "engineering",
                    "azuread",
                    new Change(["create"], null, groupAfter, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member1",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member1",
                    "azuread",
                    new Change(["create"], null, member1After, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member2",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member2",
                    "azuread",
                    new Change(["create"], null, member2After, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member3",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member3",
                    "azuread",
                    new Change(["create"], null, member3After, null, null, null))
            });
    }

    /// <summary>
    /// Builds a plan with an Azure AD group that has both inline and separate members (mixed).
    /// This is the bug scenario: inline members (user-1, user-2, group-1) + separate members (user-3, sp-1).
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithMixedMembers()
    {
        var groupAfter = JsonDocument.Parse("""
        {
            "id": "group-id-1",
            "display_name": "Engineering Team",
            "members": ["user-1", "user-2", "group-1"]
        }
        """).RootElement;

        var member1After = JsonDocument.Parse("""
        {
            "group_object_id": "group-id-1",
            "member_object_id": "user-3"
        }
        """).RootElement;

        var member2After = JsonDocument.Parse("""
        {
            "group_object_id": "group-id-1",
            "member_object_id": "sp-1"
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.engineering",
                    null,
                    "managed",
                    "azuread_group",
                    "engineering",
                    "azuread",
                    new Change(["create"], null, groupAfter, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member1",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member1",
                    "azuread",
                    new Change(["create"], null, member1After, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member2",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member2",
                    "azuread",
                    new Change(["create"], null, member2After, null, null, null))
            });
    }

    /// <summary>
    /// Builds a plan with an Azure AD group that has no members.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithNoMembers()
    {
        var after = JsonDocument.Parse("""
        {
            "id": "group-id-1",
            "display_name": "Empty Team"
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.empty",
                    null,
                    "managed",
                    "azuread_group",
                    "empty",
                    "azuread",
                    new Change(["create"], null, after, null, null, null))
            });
    }
}
