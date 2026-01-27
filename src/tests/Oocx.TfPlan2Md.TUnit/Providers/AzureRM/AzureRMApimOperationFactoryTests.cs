using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for the API Management API operation factory summary formatting.
/// </summary>
public class AzureRMApimOperationFactoryTests
{
    private const string Nbsp = "\u00A0";

    /// <summary>
    /// Verifies the factory injects API operation context into SummaryHtml.
    /// </summary>
    [Test]
    public void ApplyViewModel_SetsApiOperationSummaryHtml()
    {
        var afterDocument = JsonDocument.Parse("{\"display_name\":\"Get Profile\",\"operation_id\":\"get-profile\",\"api_name\":\"users\",\"api_management_name\":\"apim-demo\",\"resource_group_name\":\"rg-apim-demo\"}");
        var change = new Change(
            ["create"],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_api_management_api_operation.this",
            null,
            "managed",
            "azurerm_api_management_api_operation",
            "this",
            "azurerm",
            change);
        var model = new ResourceChangeModel
        {
            Address = resourceChange.Address,
            ModuleAddress = resourceChange.ModuleAddress,
            Type = resourceChange.Type,
            Name = resourceChange.Name,
            ProviderName = resourceChange.ProviderName,
            Action = "create",
            ActionSymbol = "➕",
            AttributeChanges = []
        };
        var factory = new AzureRMApimApiOperationFactory();

        factory.ApplyViewModel(model, resourceChange, "create", model.AttributeChanges);

        model.SummaryHtml.Should().Be(
            $"➕{Nbsp}azurerm_api_management_api_operation <b><code>this</code></b> <code>Get Profile</code> — <code>users</code>/<code>get-profile</code> @ <code>apim-demo</code> in <code>📁{Nbsp}rg-apim-demo</code>");
    }
}
