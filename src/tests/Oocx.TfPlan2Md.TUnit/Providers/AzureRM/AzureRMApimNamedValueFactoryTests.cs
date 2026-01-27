using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for API Management named value factory summary and sensitivity overrides.
/// </summary>
public class AzureRMApimNamedValueFactoryTests
{
    private const string Nbsp = "\u00A0";
    private const string CreateAction = "create";
    private const string ValueAttributeName = "value";
    private const string SensitivePlaceholder = "(sensitive)";

    /// <summary>
    /// Verifies named value summaries include api_management_name.
    /// </summary>
    [Test]
    public void ApplyViewModel_SetsNamedValueSummaryHtml()
    {
        var afterDocument = JsonDocument.Parse("{\"name\":\"IDP-WEB-CLIENT-ID\",\"api_management_name\":\"apim-demo\",\"resource_group_name\":\"rg-apim\"}");
        var change = new Change(
            [CreateAction],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_api_management_named_value.this",
            null,
            "managed",
            "azurerm_api_management_named_value",
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
            Action = CreateAction,
            ActionSymbol = "➕",
            AttributeChanges = []
        };
        var factory = new AzureRMApimNamedValueFactory();

        factory.ApplyViewModel(model, resourceChange, "create", model.AttributeChanges);

        model.SummaryHtml.Should().Be(
            $"➕{Nbsp}azurerm_api_management_named_value <b><code>this</code></b> — <code>🆔{Nbsp}IDP-WEB-CLIENT-ID</code> <code>apim-demo</code> in <code>📁{Nbsp}rg-apim</code>");
    }

    /// <summary>
    /// Verifies named value secret=false exposes the real value even when Terraform marks it sensitive.
    /// </summary>
    [Test]
    public void ApplyViewModel_WhenSecretFalse_UnmasksValue()
    {
        var afterDocument = JsonDocument.Parse("{\"secret\":false,\"value\":\"https://example.com\"}");
        var change = new Change(
            [CreateAction],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_api_management_named_value.client_id",
            null,
            "managed",
            "azurerm_api_management_named_value",
            "client_id",
            "azurerm",
            change);
        var model = new ResourceChangeModel
        {
            Address = resourceChange.Address,
            ModuleAddress = resourceChange.ModuleAddress,
            Type = resourceChange.Type,
            Name = resourceChange.Name,
            ProviderName = resourceChange.ProviderName,
            Action = CreateAction,
            ActionSymbol = "➕",
            AttributeChanges = new List<AttributeChangeModel>
            {
                new()
                {
                    Name = ValueAttributeName,
                    Before = SensitivePlaceholder,
                    After = SensitivePlaceholder,
                    IsSensitive = true,
                    IsLarge = false
                }
            }
        };
        var factory = new AzureRMApimNamedValueFactory();

        factory.ApplyViewModel(model, resourceChange, CreateAction, model.AttributeChanges);

        var valueChange = model.AttributeChanges.Single(item => item.Name == ValueAttributeName);
        valueChange.IsSensitive.Should().BeFalse();
        valueChange.After.Should().Be("https://example.com");
        valueChange.Before.Should().BeNull();
    }

    /// <summary>
    /// Verifies named value secret=true retains the masked value.
    /// </summary>
    [Test]
    public void ApplyViewModel_WhenSecretTrue_KeepsMaskedValue()
    {
        var afterDocument = JsonDocument.Parse("{\"secret\":true,\"value\":\"super-secret\"}");
        var change = new Change(
            [CreateAction],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_api_management_named_value.secret",
            null,
            "managed",
            "azurerm_api_management_named_value",
            "secret",
            "azurerm",
            change);
        var model = new ResourceChangeModel
        {
            Address = resourceChange.Address,
            ModuleAddress = resourceChange.ModuleAddress,
            Type = resourceChange.Type,
            Name = resourceChange.Name,
            ProviderName = resourceChange.ProviderName,
            Action = CreateAction,
            ActionSymbol = "➕",
            AttributeChanges = new List<AttributeChangeModel>
            {
                new()
                {
                    Name = ValueAttributeName,
                    Before = SensitivePlaceholder,
                    After = SensitivePlaceholder,
                    IsSensitive = true,
                    IsLarge = false
                }
            }
        };
        var factory = new AzureRMApimNamedValueFactory();

        factory.ApplyViewModel(model, resourceChange, CreateAction, model.AttributeChanges);

        var valueChange = model.AttributeChanges.Single(item => item.Name == ValueAttributeName);
        valueChange.IsSensitive.Should().BeTrue();
        valueChange.After.Should().Be(SensitivePlaceholder);
    }
}
