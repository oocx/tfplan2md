using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for API Management subresource summary formatting.
/// </summary>
public class AzureRMApimSubresourceFactoryTests
{
    private const string Nbsp = "\u00A0";

    /// <summary>
    /// Verifies subresource summaries include the API Management name and resource group.
    /// </summary>
    [Test]
    public void ApplyViewModel_IncludesApiManagementNameInSummary()
    {
        var resourceTypes = new[]
        {
            "azurerm_api_management_api_policy",
            "azurerm_api_management_product"
        };
        var factory = new AzureRMApimSubresourceFactory(resourceTypes);

        foreach (var resourceType in resourceTypes)
        {
            var afterDocument = JsonDocument.Parse("{\"name\":\"sample\",\"api_management_name\":\"apim-demo\",\"resource_group_name\":\"rg-apim\"}");
            var change = new Change(
                ["create"],
                null,
                afterDocument.RootElement,
                null,
                null,
                null);
            var resourceChange = new ResourceChange(
                $"{resourceType}.this",
                null,
                "managed",
                resourceType,
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
            factory.ApplyViewModel(model, resourceChange, "create", model.AttributeChanges);

            model.SummaryHtml.Should().Be(
                $"➕{Nbsp}{resourceType} <b><code>this</code></b> — <code>🆔{Nbsp}sample</code> <code>apim-demo</code> in <code>📁{Nbsp}rg-apim</code>");
        }
    }
}
