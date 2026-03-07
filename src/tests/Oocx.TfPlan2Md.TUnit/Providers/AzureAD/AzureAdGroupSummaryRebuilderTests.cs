using System;
using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureAD.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureAD;

/// <summary>
/// Tests for the Azure AD group summary rebuilder callback implementation.
/// Related issue: docs/issues/059-parent-child-summary-member-counts/analysis.md.
/// </summary>
public class AzureAdGroupSummaryRebuilderTests
{
    /// <summary>
    /// Verifies that AzureADModule registers the post-merge callback.
    /// </summary>
    [Test]
    public void AzureADModule_RegistersPostMergeCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var module = new AzureADModule();
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(module);

        // Create a plan that will trigger parent-child merging
        var plan = BuildPlanWithAzureAdGroupAndMembers();
        var principals = new Dictionary<string, string> { ["user-1"] = "Alice" };
        var principalTypes = new Dictionary<string, string> { ["user-1"] = "User" };
        var principalMapper = new PrincipalMapper(principals, principalTypes);

        var builder = new ReportModelBuilder(
            providerRegistry: providerRegistry);

        // Override the callback to detect invocation
        builder.RegisterPostMergeCallback((changes, mapper) =>
        {
            if (changes.Any(c => c.Type == "azuread_group" && c.ChildResourceGroups.Count > 0))
            {
                callbackInvoked = true;
            }
        });

        // Act
        builder.Build(plan);

        // Assert
        callbackInvoked.Should().BeTrue("Azure AD callback should be invoked when groups have members");
    }

    /// <summary>
    /// Verifies that the callback updates group summaries correctly.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithMembers_UpdatesSummaryHtml()
    {
        // Arrange
        var plan = BuildPlanWithAzureAdGroupAndMembers();
        var model = BuildModelWithCallback(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert
        group.SummaryHtml.Should().Be("➕\u00A0azuread_group <b><code>test</code></b> \u2014 <code>👥\u00A0Test Group</code> | <code>1 👤\u00A00 👥\u00A00 💻</code> | ➕\u00A01 members");
    }

    /// <summary>
    /// Verifies that the callback handles groups with no members correctly.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithNoMembers_DoesNotModifySummary()
    {
        // Arrange
        var groupAfter = JsonDocument.Parse("""
        {
            "id": "group-1",
            "display_name": "Empty Group"
        }
        """).RootElement;

        var plan = new TerraformPlan(
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
                    new Change(["create"], null, groupAfter, null, null, null))
            });

        var model = BuildModelWithCallback(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert
        group.ChildResourceGroups.Should().BeEmpty("group should have no child resource groups");
        group.SummaryHtml.Should().NotBeNull("summary should still be generated");
    }

    /// <summary>
    /// Verifies that the callback handles null principal mapper gracefully.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithNullPrincipalMapper_DoesNotThrow()
    {
        // Arrange
        var changes = new List<ResourceChangeModel>
        {
            new()
            {
                Address = "azuread_group.test",
                Type = "azuread_group",
                Name = "test",
                ProviderName = "azuread",
                Action = "create",
                ActionSymbol = "+",
                AttributeChanges = new List<AttributeChangeModel>(),
                ChildResourceGroups = new List<ChildResourceGroup>
                {
                    new()
                    {
                        Label = "Members",
                        Columns = new List<ChildTableColumn>(),
                        Rows = new List<ChildResourceRow>
                        {
                            new()
                            {
                                ChangeIndicator = "+",
                                TerraformResource = "member",
                                Values = new Dictionary<string, string> { ["member"] = "user-1" }
                            }
                        }
                    }
                },
                SummaryHtml = "Test Group | <code>0 👤 0 👥 0 💻</code>"
            }
        };

        // Act & Assert
        var act = () => AzureAdGroupSummaryRebuilder.UpdateGroupSummaries(changes, null);
        act.Should().NotThrow("callback should handle null principal mapper gracefully");
    }

    /// <summary>
    /// Verifies that the callback only processes azuread_group resources.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithNonGroupResources_DoesNotModifyThem()
    {
        // Arrange
        var userAfter = JsonDocument.Parse("""
        {
            "id": "user-1",
            "display_name": "Alice"
        }
        """).RootElement;

        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_user.alice",
                    null,
                    "managed",
                    "azuread_user",
                    "alice",
                    "azuread",
                    new Change(["create"], null, userAfter, null, null, null))
            });

        var model = BuildModelWithCallback(plan);

        // Act
        var user = model.Changes.Should().ContainSingle(c => c.Type == "azuread_user").Subject;

        // Assert - User summaries should not have member count patterns like "2 👤"
        user.SummaryHtml.Should().NotMatch(@"\d+\s*👤", "user summary should not have member count patterns");
        user.SummaryHtml.Should().NotMatch(@"\d+\s*👥", "user summary should not have member count patterns");
        user.SummaryHtml.Should().NotMatch(@"\d+\s*💻", "user summary should not have member count patterns");
    }

    /// <summary>
    /// Verifies that the callback handles groups with multiple member types correctly.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithMixedMemberTypes_CountsCorrectly()
    {
        // Arrange
        var groupAfter = JsonDocument.Parse("""
        {
            "id": "group-1",
            "display_name": "Mixed Group"
        }
        """).RootElement;

        var member1After = JsonDocument.Parse("""
        {
            "group_object_id": "group-1",
            "member_object_id": "user-1"
        }
        """).RootElement;

        var member2After = JsonDocument.Parse("""
        {
            "group_object_id": "group-1",
            "member_object_id": "group-2"
        }
        """).RootElement;

        var member3After = JsonDocument.Parse("""
        {
            "group_object_id": "group-1",
            "member_object_id": "sp-1"
        }
        """).RootElement;

        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.mixed",
                    null,
                    "managed",
                    "azuread_group",
                    "mixed",
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

        var principals = new Dictionary<string, string>
        {
            ["user-1"] = "Alice",
            ["group-2"] = "Team A",
            ["sp-1"] = "App Service"
        };

        var principalTypes = new Dictionary<string, string>
        {
            ["user-1"] = "User",
            ["group-2"] = "Group",
            ["sp-1"] = "ServicePrincipal"
        };

        var principalMapper = new PrincipalMapper(principals, principalTypes);
        var model = BuildModelWithCallback(plan, principalMapper);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert
        group.SummaryHtml.Should().Be("➕\u00A0azuread_group <b><code>mixed</code></b> \u2014 <code>👥\u00A0Mixed Group</code> | <code>1 👤\u00A01 👥\u00A01 💻</code> | ➕\u00A03 members");
    }

    /// <summary>
    /// Verifies that the callback handles groups with unknown member types.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithUnknownMemberTypes_CountsAsUnknown()
    {
        // Arrange
        var groupAfter = JsonDocument.Parse("""
        {
            "id": "group-1",
            "display_name": "Group with Unknown"
        }
        """).RootElement;

        var member1After = JsonDocument.Parse("""
        {
            "group_object_id": "group-1",
            "member_object_id": "unknown-1"
        }
        """).RootElement;

        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.test",
                    null,
                    "managed",
                    "azuread_group",
                    "test",
                    "azuread",
                    new Change(["create"], null, groupAfter, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member1",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member1",
                    "azuread",
                    new Change(["create"], null, member1After, null, null, null))
            });

        // Principal mapper that doesn't know about "unknown-1"
        var principals = new Dictionary<string, string>();
        var principalTypes = new Dictionary<string, string>();
        var principalMapper = new PrincipalMapper(principals, principalTypes);
        var model = BuildModelWithCallback(plan, principalMapper);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert
        group.SummaryHtml.Should().Be("➕\u00A0azuread_group <b><code>test</code></b> \u2014 <code>👥\u00A0Group with Unknown</code> | <code>0 👤\u00A00 👥\u00A00 💻\u00A01 ❓</code> | ➕\u00A01 members");
    }

    /// <summary>
    /// Verifies that the callback preserves other parts of the summary HTML.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_PreservesOtherSummaryContent()
    {
        // Arrange
        var plan = BuildPlanWithAzureAdGroupAndMembers();
        var model = BuildModelWithCallback(plan);

        // Act
        var group = model.Changes.Should().ContainSingle(c => c.Type == "azuread_group").Subject;

        // Assert
        group.SummaryHtml.Should().Be("➕\u00A0azuread_group <b><code>test</code></b> \u2014 <code>👥\u00A0Test Group</code> | <code>1 👤\u00A00 👥\u00A00 💻</code> | ➕\u00A01 members", "summary should preserve display name and separator");
    }

    /// <summary>
    /// Verifies that member ID extraction handles formatted values with display names.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithFormattedMemberValues_ExtractsIdCorrectly()
    {
        // Arrange - This scenario happens when members have display names
        var changes = new List<ResourceChangeModel>
        {
            new()
            {
                Address = "azuread_group.test",
                Type = "azuread_group",
                Name = "test",
                ProviderName = "azuread",
                Action = "create",
                ActionSymbol = "+",
                AttributeChanges = new List<AttributeChangeModel>(),
                ChildResourceGroups = new List<ChildResourceGroup>
                {
                    new()
                    {
                        Label = "Members",
                        Columns = new List<ChildTableColumn>(),
                        Rows = new List<ChildResourceRow>
                        {
                            new()
                            {
                                ChangeIndicator = "+",
                                TerraformResource = "member",
                                Values = new Dictionary<string, string> { ["member"] = "Alice [user-1]" }
                            }
                        }
                    }
                },
                SummaryHtml = "Test Group | <code>0 👤 0 👥 0 💻</code>"
            }
        };

        var principals = new Dictionary<string, string> { ["user-1"] = "Alice" };
        var principalTypes = new Dictionary<string, string> { ["user-1"] = "User" };
        var principalMapper = new PrincipalMapper(principals, principalTypes);

        // Act
        AzureAdGroupSummaryRebuilder.UpdateGroupSummaries(changes, principalMapper);

        // Assert
        changes[0].SummaryHtml.Should().Contain("1 👤", "callback should extract ID from formatted value");
    }

    /// <summary>
    /// Verifies that member ID extraction handles backtick-wrapped values.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithBacktickWrappedIds_ExtractsIdCorrectly()
    {
        // Arrange
        var changes = new List<ResourceChangeModel>
        {
            new()
            {
                Address = "azuread_group.test",
                Type = "azuread_group",
                Name = "test",
                ProviderName = "azuread",
                Action = "create",
                ActionSymbol = "+",
                AttributeChanges = new List<AttributeChangeModel>(),
                ChildResourceGroups = new List<ChildResourceGroup>
                {
                    new()
                    {
                        Label = "Members",
                        Columns = new List<ChildTableColumn>(),
                        Rows = new List<ChildResourceRow>
                        {
                            new()
                            {
                                ChangeIndicator = "+",
                                TerraformResource = "member",
                                Values = new Dictionary<string, string> { ["member"] = "`user-1`" }
                            }
                        }
                    }
                },
                SummaryHtml = "Test Group | <code>0 👤 0 👥 0 💻</code>"
            }
        };

        var principals = new Dictionary<string, string> { ["user-1"] = "Alice" };
        var principalTypes = new Dictionary<string, string> { ["user-1"] = "User" };
        var principalMapper = new PrincipalMapper(principals, principalTypes);

        // Act
        AzureAdGroupSummaryRebuilder.UpdateGroupSummaries(changes, principalMapper);

        // Assert
        changes[0].SummaryHtml.Should().Contain("1 👤", "callback should extract ID from backtick-wrapped value");
    }

    /// <summary>
    /// Verifies that member ID extraction handles HTML code tags.
    /// </summary>
    [Test]
    public void UpdateGroupSummaries_WithHtmlCodeTags_ExtractsIdCorrectly()
    {
        // Arrange
        var changes = new List<ResourceChangeModel>
        {
            new()
            {
                Address = "azuread_group.test",
                Type = "azuread_group",
                Name = "test",
                ProviderName = "azuread",
                Action = "create",
                ActionSymbol = "+",
                AttributeChanges = new List<AttributeChangeModel>(),
                ChildResourceGroups = new List<ChildResourceGroup>
                {
                    new()
                    {
                        Label = "Members",
                        Columns = new List<ChildTableColumn>(),
                        Rows = new List<ChildResourceRow>
                        {
                            new()
                            {
                                ChangeIndicator = "+",
                                TerraformResource = "member",
                                Values = new Dictionary<string, string> { ["member"] = "<code>user-1</code>" }
                            }
                        }
                    }
                },
                SummaryHtml = "Test Group | <code>0 👤 0 👥 0 💻</code>"
            }
        };

        var principals = new Dictionary<string, string> { ["user-1"] = "Alice" };
        var principalTypes = new Dictionary<string, string> { ["user-1"] = "User" };
        var principalMapper = new PrincipalMapper(principals, principalTypes);

        // Act
        AzureAdGroupSummaryRebuilder.UpdateGroupSummaries(changes, principalMapper);

        // Assert
        changes[0].SummaryHtml.Should().Contain("1 👤", "callback should extract ID from HTML code tags");
    }

    /// <summary>
    /// Builds a Terraform plan with an Azure AD group and one member.
    /// </summary>
    /// <returns>The constructed Terraform plan.</returns>
    private static TerraformPlan BuildPlanWithAzureAdGroupAndMembers()
    {
        var groupAfter = JsonDocument.Parse("""
        {
            "id": "group-1",
            "display_name": "Test Group"
        }
        """).RootElement;

        var memberAfter = JsonDocument.Parse("""
        {
            "group_object_id": "group-1",
            "member_object_id": "user-1"
        }
        """).RootElement;

        return new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azuread_group.test",
                    null,
                    "managed",
                    "azuread_group",
                    "test",
                    "azuread",
                    new Change(["create"], null, groupAfter, null, null, null)),
                new ResourceChange(
                    "azuread_group_member.member1",
                    null,
                    "managed",
                    "azuread_group_member",
                    "member1",
                    "azuread",
                    new Change(["create"], null, memberAfter, null, null, null))
            });
    }

    /// <summary>
    /// Builds a ReportModel with the Azure AD callback configured.
    /// </summary>
    /// <param name="plan">The plan to build.</param>
    /// <param name="principalMapper">Optional principal mapper; defaults to a basic mapper with user-1.</param>
    /// <returns>The generated report model.</returns>
    private static ReportModel BuildModelWithCallback(TerraformPlan plan, IPrincipalMapper? principalMapper = null)
    {
        principalMapper ??= new PrincipalMapper(
            new Dictionary<string, string> { ["user-1"] = "Alice" },
            new Dictionary<string, string> { ["user-1"] = "User" });

        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        var builder = new ReportModelBuilder(
            providerRegistry: providerRegistry);

        return builder.Build(plan);
    }
}
