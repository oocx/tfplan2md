using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
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
    /// Verifies the factory sets Summary and SummaryHtml using the fully qualified name and record values.
    /// </summary>
    [Test]
    public void ApplyViewModel_SetsSummaryAndSummaryHtml()
    {
        var afterDocument = JsonDocument.Parse("{\"name\":\"record1\",\"zone_name\":\"contoso.local\",\"records\":[\"10.0.0.4\",\"10.0.0.5\",\"10.0.0.6\",\"10.0.0.7\"]}");
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

        factory.ApplyViewModel(new ApplyViewModelContext(model, resourceChange, CreateAction, model.AttributeChanges, new NullPrincipalMapper(), null));

        model.Summary.Should().Be("`🆔\u00A0record1` — `record1.contoso.local` `🌐\u00A010.0.0.4` `🌐\u00A010.0.0.5` `🌐\u00A010.0.0.6`");
        model.SummaryHtml.Should().Be(
            $"{ActionIcons.Add}{Nbsp}azurerm_private_dns_a_record <b><code>🆔{Nbsp}record1</code></b> — <code>record1.contoso.local</code> <code>🌐{Nbsp}10.0.0.4</code> <code>🌐{Nbsp}10.0.0.5</code> <code>🌐{Nbsp}10.0.0.6</code>");
    }
}
