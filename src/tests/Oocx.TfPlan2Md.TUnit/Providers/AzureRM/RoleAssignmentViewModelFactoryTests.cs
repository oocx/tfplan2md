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
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: null);

        viewModel.SummaryText.Should().Be("remove <code>🛡️\u00A0Contributor</code> on subscription <code>🔑\u00A0sub-id</code> from <code>👤\u00A0principal-1</code>");
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
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: null);

        viewModel.SummaryText.Should().Be("recreate as <code>👥\u00A0principal-1</code> → <code>🛡️\u00A0Contributor</code> on subscription <code>🔑\u00A0sub-id</code>");

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
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: null);

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
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: null);

        var principal = viewModel.SmallAttributes.Single(item => item.Name == "principal_id");
        principal.After.Should().Be("`👤\u00A0principal-1 (User)` [`principal-1`]");

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
            var mappingResult = AzureMappingFileLoader.Load(mappingPath, diagnosticContext: null);
            var mapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes);
            var change = CreateChange(before: null, after: after, actions: ["create"]);

            var viewModel = RoleAssignmentViewModelFactory.Build(
                change,
                action: "create",
                attributeChanges: [],
                principalMapper: mapper,
                scopeFormatter: null);

            var principal = viewModel.SmallAttributes.Single(item => item.Name == "principal_id");
            principal.After.Should().Be("`👤\u00A0user@example.com (User)` [`user-123`]");
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
            var mappingResult = AzureMappingFileLoader.Load(mappingPath, diagnosticContext: null);
            var mapper = new PrincipalMapper(mappingResult.Principals, mappingResult.PrincipalTypes);
            var change = CreateChange(before: null, after: after, actions: ["create"]);

            var viewModel = RoleAssignmentViewModelFactory.Build(
                change,
                action: "create",
                attributeChanges: [],
                principalMapper: mapper,
                scopeFormatter: null);

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
    /// Verifies scope table formatting uses subscription display names when provided.
    /// </summary>
    [Test]
    public async Task Build_WhenScopeFormatterProvided_UsesSubscriptionDisplayName()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-demo",
                "role_definition_id": "role-id",
                "role_definition_name": "Reader",
                "principal_id": "principal-1",
                "principal_type": "User"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);
        var subscriptions = new List<MappingEntry>
        {
            new("12345678-1234-1234-1234-123456789012", "Production")
        };
        var entityMapper = new AzureEntityMapper(subscriptions, [], []);
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: [],
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: scopeFormatter);

        var scope = viewModel.SmallAttributes.Single(item => item.Name == "scope");
        scope.After.Should().Be("`📁\u00A0rg-demo` in subscription `🔑\u00A0Production (12345678-1234-1234-1234-123456789012)`");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the summary uses the subscription name with the 🔑 icon when a mapping exists.
    /// </summary>
    [Test]
    public async Task Build_WhenSubscriptionScopeAndNameMapped_SummaryShowsNameWithKeyIcon()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/12345678-1234-1234-1234-123456789012",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "principal-1",
                "principal_type": "ServicePrincipal"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);
        var subscriptions = new List<MappingEntry>
        {
            new("12345678-1234-1234-1234-123456789012", "My Production Subscription")
        };
        var entityMapper = new AzureEntityMapper(subscriptions, [], []);
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: [],
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: scopeFormatter);

        // Summary shows the human-readable name with 🔑 icon (UUID is absent since name is mapped)
        viewModel.SummaryText.Should().Be("<code>💻\u00A0principal-1</code> → <code>🛡️\u00A0Contributor</code> on subscription <code>🔑\u00A0My Production Subscription</code>");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the summary falls back to the raw subscription ID with the 🔑 icon when no name mapping exists.
    /// </summary>
    [Test]
    public async Task Build_WhenSubscriptionScopeAndNotMapped_SummaryShowsIdWithKeyIcon()
    {
        var after = JsonDocument.Parse("""
            {
                "scope": "/subscriptions/sub-unmapped-id",
                "role_definition_id": "role-id",
                "role_definition_name": "Contributor",
                "principal_id": "principal-1",
                "principal_type": "ServicePrincipal"
            }
            """).RootElement;

        var change = CreateChange(before: null, after: after, actions: ["create"]);
        // No subscription mapping provided
        var entityMapper = new AzureEntityMapper([], [], []);
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);

        var viewModel = RoleAssignmentViewModelFactory.Build(
            change,
            action: "create",
            attributeChanges: [],
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: scopeFormatter);

        // Summary shows the raw ID and the 🔑 icon (backward-compatible behavior)
        viewModel.SummaryText.Should().Be("<code>💻\u00A0principal-1</code> → <code>🛡️\u00A0Contributor</code> on subscription <code>🔑\u00A0sub-unmapped-id</code>");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies the summary shows raw subscription ID with 🔑 icon when no scope formatter is provided.
    /// </summary>
    [Test]
    public async Task Build_WhenSubscriptionScopeAndNoScopeFormatter_SummaryShowsIdWithKeyIcon()
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
            principalMapper: new NullPrincipalMapper(),
            scopeFormatter: null);

        viewModel.SummaryText.Should().Be("<code>👤\u00A0principal-1</code> → <code>🛡️\u00A0Contributor</code> on subscription <code>🔑\u00A0sub-id</code>");

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
