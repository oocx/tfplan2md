using System.Collections.Generic;
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
/// Tests for management group formatting in role assignment summaries and tables.
/// Related feature: docs/features/065-tenant-display-mapping/specification.md.
/// </summary>
public class RoleAssignmentManagementGroupFormattingTests
{
    /// <summary>
    /// Verifies management group scopes include the icon inside summary code spans.
    /// </summary>
    [Test]
    public async Task Build_WhenManagementGroupScope_FormatsSummaryWithIcon()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/providers/Microsoft.Management/managementGroups/mg-root",
                "role_definition_id": "role-id",
                "role_definition_name": "Reader",
                "principal_id": "principal-1",
                "principal_type": "User"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);
        var entityMapper = new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [new MappingEntry("mg-root", "Core Platform")],
            tenants: []);
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: [],
            scopeFormatter: scopeFormatter);

        viewModel.SummaryText.Should().Be("<code>👤\u00A0principal-1</code> → <code>🛡️\u00A0Reader</code> on management group <code>🗂️\u00A0Core Platform</code>");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies management group scopes include the icon inside table code spans.
    /// </summary>
    [Test]
    public async Task Build_WhenManagementGroupScope_FormatsScopeAttributeWithIcon()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/providers/Microsoft.Management/managementGroups/mg-root",
                "role_definition_id": "role-id",
                "role_definition_name": "Reader",
                "principal_id": "principal-1",
                "principal_type": "User"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);
        var entityMapper = new AzureEntityMapper(
            subscriptions: [],
            managementGroups: [new MappingEntry("mg-root", "Core Platform")],
            tenants: []);
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: [],
            scopeFormatter: scopeFormatter);

        var scope = viewModel.SmallAttributes.Single(item => item.Name == "scope");
        scope.After.Should().Be("`🗂️\u00A0Core Platform (Management Group)`");

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
}
