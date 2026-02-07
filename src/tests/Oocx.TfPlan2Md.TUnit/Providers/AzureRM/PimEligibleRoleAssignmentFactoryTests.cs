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
/// Tests for PIM eligible role assignment summary factory overrides.
/// </summary>
public class PimEligibleRoleAssignmentFactoryTests
{
    private const string Nbsp = "\u00A0";
    private const string CreateAction = "create";
    private const string OwnerRoleId = "8e3af657-a8ff-443c-a75c-2fe8c4bcb635";

    /// <summary>
    /// Verifies the factory sets Summary and SummaryHtml using resolved role and principal names.
    /// </summary>
    [Test]
    public void ApplyViewModel_SetsSummaryAndSummaryHtml()
    {
        var afterDocument = JsonDocument.Parse($"{{\"principal_id\":\"user-123\",\"role_definition_id\":\"{OwnerRoleId}\"}}");
        var change = new Change(
            [CreateAction],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_pim_eligible_role_assignment.example",
            null,
            "managed",
            "azurerm_pim_eligible_role_assignment",
            "example",
            "azurerm",
            change);
        var principalMapper = new PrincipalMapper(
            new Dictionary<string, string> { ["user-123"] = "Jane Doe" },
            new Dictionary<string, string>());
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
        var factory = new PimEligibleRoleAssignmentFactory(principalMapper);

        factory.ApplyViewModel(model, resourceChange, CreateAction, model.AttributeChanges, principalMapper, null);

        model.Summary.Should().Be("Assign `Owner` to `Jane Doe`");
        model.SummaryHtml.Should().Be(
            $"{ActionIcons.Add}{Nbsp}azurerm_pim_eligible_role_assignment <b><code>example</code></b> — Assign <code>Owner</code> to <code>Jane Doe</code>");
    }
}
