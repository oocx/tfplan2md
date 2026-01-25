using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for role assignment view model creation to validate summary and attribute formatting branches.
/// </summary>
public class RoleAssignmentViewModelFactoryTests
{
    /// <summary>
    /// Verifies delete actions use before state and emit remove summary text.
    /// </summary>
    [Test]
    public async Task Build_WhenDeleteAction_UsesBeforeStateAndRemoveSummary()
    {
        var before = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "principal-1",
                "principal_type": "User",
                "name": "assignment",
                "description": "desc"
            }
            """).RootElement;

        var change = CreateChange(before: before, after: null, actions: ["delete"]);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "delete",
            attributeChanges: [],
            principalMapper: new NullPrincipalMapper());

        viewModel.SummaryText.Should().Contain("remove");
        viewModel.SummaryText.Should().Contain("🛡️");
        viewModel.SummaryText.Should().Contain("subscription");
        viewModel.SmallAttributes.Should().Contain(item => item.Name == "scope");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies replace actions use recreate wording in summary text.
    /// </summary>
    [Test]
    public async Task Build_WhenReplaceAction_UsesRecreateSummary()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "principal-1",
                "principal_type": "Group",
                "name": "assignment"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create", "delete"]);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "replace",
            attributeChanges: [],
            principalMapper: new NullPrincipalMapper());

        viewModel.SummaryText.Should().Contain("recreate as");
        viewModel.SummaryText.Should().Contain("👥");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies custom attribute lists are ordered and large attributes separated.
    /// </summary>
    [Test]
    public async Task Build_WhenCustomAttributesProvided_OrdersAndSplitsLarge()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Reader",
                "principal_id": "principal-1",
                "principal_type": "ServicePrincipal",
                "name": "assignment",
                "description": "desc"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);
        var attributes = new List<AttributeChangeModel>
        {
            new() { Name = "description", IsLarge = true },
            new() { Name = "scope", IsLarge = false },
            new() { Name = "name", IsLarge = false }
        };

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: attributes,
            principalMapper: new NullPrincipalMapper());

        viewModel.LargeAttributes.Should().ContainSingle(item => item.Name == "description");
        viewModel.SmallAttributes.Select(item => item.Name).First().Should().Be("scope");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies principal id formatting includes icon, type, and id when available.
    /// </summary>
    [Test]
    public async Task Build_WhenPrincipalTypeProvided_FormatsPrincipalId()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "principal-1",
                "principal_type": "User"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: [],
            principalMapper: new NullPrincipalMapper());

        var principal = viewModel.SmallAttributes.Single(item => item.Name == "principal_id");
        principal.After.Should().Contain("👤");
        principal.After.Should().Contain("principal-1");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies missing principal types are inferred from nested principal mappings.
    /// </summary>
    [Test]
    public async Task Build_WhenPrincipalTypeMissing_InfersTypeFromMapping()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "user-123"
            }
            """).RootElement;

        var mappingPath = CreateTempMapping("""
            {
              "users": {
                "user-123": "user@example.com"
              }
            }
            """);

        try
        {
            var mapper = new PrincipalMapper(mappingPath);
            var change = CreateChange(before: null, after: after, actions: ["create"]);

            var viewModel = RoleAssignmentViewModelFactory.Build(
                change,
                action: "create",
                attributeChanges: [],
                principalMapper: mapper);

            var principal = viewModel.SmallAttributes.Single(item => item.Name == "principal_id");
            principal.After.Should().Contain("👤");
            principal.After.Should().Contain("(User)");
            principal.After.Should().Contain("user-123");
            viewModel.SummaryText.Should().Contain("👤");
        }
        finally
        {
            File.Delete(mappingPath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies missing principal types are not decorated when mapping lacks type metadata.
    /// </summary>
    [Test]
    public async Task Build_WhenPrincipalTypeMissingAndMappingFlat_DoesNotDecorate()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "user-123"
            }
            """).RootElement;

        var mappingPath = CreateTempMapping("""
            {
              "user-123": "user@example.com"
            }
            """);

        try
        {
            var mapper = new PrincipalMapper(mappingPath);
            var change = CreateChange(before: null, after: after, actions: ["create"]);

            var viewModel = RoleAssignmentViewModelFactory.Build(
                change,
                action: "create",
                attributeChanges: [],
                principalMapper: mapper);

            var principal = viewModel.SmallAttributes.Single(item => item.Name == "principal_id");
            principal.After.Should().Contain("user@example.com");
            principal.After.Should().Contain("user-123");
            principal.After.Should().NotContain("👤");
            principal.After.Should().NotContain("(User)");
            viewModel.SummaryText.Should().NotContain("👤");
        }
        finally
        {
            File.Delete(mappingPath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a resource change model for tests.
    /// </summary>
    /// <param name="before">Before state JSON.</param>
    /// <param name="after">After state JSON.</param>
    /// <param name="actions">Terraform action list.</param>
    /// <returns>Resource change instance.</returns>
    private static ResourceChange CreateChange(JsonElement? before, JsonElement? after, IReadOnlyList<string> actions)
    {
        return new ResourceChange(
            Address: "azurerm_role_assignment.example",
            ModuleAddress: null,
            Mode: "managed",
            Type: "azurerm_role_assignment",
            Name: "example",
            ProviderName: "registry.terraform.io/hashicorp/azurerm",
            Change: new Change(actions, before, after));
    }

    /// <summary>
    /// Creates a temporary principal mapping file for tests.
    /// </summary>
    /// <param name="content">JSON content to write to the mapping file.</param>
    /// <returns>The path to the temporary mapping file.</returns>
    private static string CreateTempMapping(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}
