using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers.AzureRM.Models;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Providers.AzureRM;

/// <summary>
/// Tests for private DNS A record summary factory overrides.
/// </summary>
public class AzureRMPrivateDnsARecordFactoryTests
{
    private const string Nbsp = "\u00A0";
    private const string CreateAction = "create";

    /// <summary>
    /// Verifies the factory sets Summary and SummaryHtml using the fully qualified name.
    /// </summary>
    [Test]
    public void ApplyViewModel_SetsSummaryAndSummaryHtml()
    {
        var afterDocument = JsonDocument.Parse("{\"name\":\"record1\",\"zone_name\":\"contoso.local\"}");
        var change = new Change(
            [CreateAction],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var resourceChange = new ResourceChange(
            "azurerm_private_dns_a_record.example",
            null,
            "managed",
            "azurerm_private_dns_a_record",
            "example",
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
            ActionSymbol = ActionIcons.Add,
            AttributeChanges = []
        };
        var factory = new AzureRMPrivateDnsARecordFactory();

        factory.ApplyViewModel(model, resourceChange, CreateAction, model.AttributeChanges, new NullPrincipalMapper(), null);

        model.Summary.Should().Be("`record1.contoso.local`");
        model.SummaryHtml.Should().Be(
            $"{ActionIcons.Add}{Nbsp}azurerm_private_dns_a_record <b><code>record1.contoso.local</code></b>");
    }
}
