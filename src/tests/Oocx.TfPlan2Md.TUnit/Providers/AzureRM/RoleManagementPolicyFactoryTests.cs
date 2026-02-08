using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for role management policy summary factory overrides.
/// </summary>
public class RoleManagementPolicyFactoryTests
{
    private const string Nbsp = "\u00A0";
    private const string CreateAction = "create";
    private const string OwnerRoleId = "8e3af657-a8ff-443c-a75c-2fe8c4bcb635";

    /// <summary>
    /// Verifies the factory sets Summary and SummaryHtml using resolved role names and enriched scopes.
    /// </summary>
    [Test]
    public void ApplyViewModel_SetsSummaryAndSummaryHtml()
    {
        var afterDocument = JsonDocument.Parse($"{{\"role_definition_id\":\"{OwnerRoleId}\",\"scope\":\"/subscriptions/sub-123/resourceGroups/rg1\"}}");
        var change = new Change(
            [CreateAction],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_role_management_policy.example",
            null,
            "managed",
            "azurerm_role_management_policy",
            "example",
            "azurerm",
            change);
        var entityMapper = new AzureEntityMapper(
            new List<MappingEntry> { new("sub-123", "Production") },
            new List<MappingEntry>(),
            new List<MappingEntry>());
        var scopeFormatter = new EnrichedAzureScopeFormatter(entityMapper);
        var model = new ResourceChangeModel
        {
            Address = resourceChange.Address,
            ModuleAddress = resourceChange.ModuleAddress,
            Type = resourceChange.Type,
            Name = resourceChange.Name,
            ProviderName = resourceChange.ProviderName,
            Action = CreateAction,
            ActionSymbol = ActionIcons.Add,
            AttributeChanges = []
        };
        var factory = new RoleManagementPolicyFactory(scopeFormatter);

        factory.ApplyViewModel(model, resourceChange, CreateAction, model.AttributeChanges, new NullPrincipalMapper(), null);

        model.Summary.Should().Be("`🛡️\u00A0Owner` in `📁\u00A0rg1` in subscription `🔑\u00A0Production (sub-123)`");
        model.SummaryHtml.Should().Be(
            $"{ActionIcons.Add}{Nbsp}azurerm_role_management_policy <b><code>example</code></b> — <code>🛡️{Nbsp}Owner</code> in <code>📁{Nbsp}rg1</code> in subscription <code>🔑{Nbsp}Production (sub-123)</code>");
    }
}
