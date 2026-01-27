using System.Globalization;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderSummaryTests
{
    private const string Nbsp = "\u00A0";

    [Test]
    public void Build_PopulatesSummaryAndReplacePaths()
    {
        // Arrange
        var json = File.ReadAllText("TestData/replace-paths-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var change = model.Changes.Should().ContainSingle().Subject;
        change.ReplacePaths.Should().NotBeNull();
        change.ReplacePaths!.Should().HaveCount(1);
        change.Summary.Should().Be("recreate `snet-db` (address_prefixes[0] changed: force replacement)");
    }

    [Test]
    public void Build_ComputesPreformattedSummaries()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var update = model.Changes.Should().Contain(c => c.Action == "update").Subject;
        update.ChangedAttributesSummary.Should().Contain(update.AttributeChanges.Count.ToString(CultureInfo.InvariantCulture));
        update.ChangedAttributesSummary.Should().Contain("🔧");
        update.SummaryHtml.Should().StartWith($"{update.ActionSymbol}{Nbsp}{update.Type} <b><code>{update.Name}</code></b>");

        var createWithTags = model.Changes.First(c => c.Action == "create" && c.TagsBadges is not null);
        createWithTags.TagsBadges.Should().Contain("🏷️");
        createWithTags.TagsBadges.Should().Contain("environment: production");
    }

    [Test]
    public void Build_ChangedAttributesSummary_TruncatesAndFormats()
    {
        // Arrange
        var json = File.ReadAllText("TestData/summary-changed-attributes.json");
        var plan = new TerraformPlanParser().Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var update = model.Changes.Should().ContainSingle(c => c.Action == "update").Subject;
        update.ChangedAttributesSummary.Should().Be($"4🔧{Nbsp}account_replication_type, https_only, kind, +1 more");
    }

    [Test]
    public void Build_ChangedAttributesSummary_UsesFirewallRuleChanges()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = new TerraformPlanParser().Parse(json);
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));
        var builder = new ReportModelBuilder(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        // Act
        var model = builder.Build(plan);

        // Assert
        var update = model.Changes
            .First(c => c.Type == "azurerm_firewall_network_rule_collection" && c.Action == "update");

        update.ChangedAttributesSummary.Should().Be(
            $"3🔧{Nbsp}➕{Nbsp}<code>🆔{Nbsp}allow-dns</code>, 🔄{Nbsp}<code>🆔{Nbsp}allow-http</code>, ❌{Nbsp}<code>🆔{Nbsp}allow-ssh-old</code>");
    }

    [Test]
    public void Build_TagsBadges_AreNullWhenNoTags()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var createWithoutTags = model.Changes.First(c => c.Action == "create" && c.TagsBadges is null && c.Type == "azuredevops_project");
        createWithoutTags.TagsBadges.Should().BeNull();
    }

    [Test]
    public void Build_SummaryHtml_FormatsLocationAndAddressSpace()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = new TerraformPlanParser().Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var vnetDelete = model.Changes.First(c => c.Type == "azurerm_virtual_network" && c.Action == "delete");
        vnetDelete.SummaryHtml.Should().Contain("<code>🌍 westeurope</code>");
        vnetDelete.SummaryHtml.Should().Contain("<code>🌐 10.0.0.0/16</code>");
    }

    /// <summary>
    /// Verifies that subscription resources surface subscription attributes in the summary HTML.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <returns>None.</returns>
    [Test]
    public void Build_SummaryHtml_IncludesSubscriptionAttributes()
    {
        var afterDocument = JsonDocument.Parse("{\"subscription_id\":\"sub-123\",\"subscription\":\"Production\"}");
        var change = new Change(
            ["create"],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "azurerm_subscription.demo",
                    null,
                    "managed",
                    "azurerm_subscription",
                    "demo",
                    "registry.terraform.io/hashicorp/azurerm",
                    change)
            });
        var builder = new ReportModelBuilder();

        var model = builder.Build(plan);

        var summary = model.Changes.Single().SummaryHtml;
        summary.Should().Contain($"<code>🔑{Nbsp}sub-123</code>");
        summary.Should().Contain($"<code>🔑{Nbsp}Production</code>");
    }

    [Test]
    public void Build_SummaryHtml_RespectsFactoryOverride()
    {
        var afterDocument = JsonDocument.Parse("{\"name\":\"example\"}");
        var change = new Change(
            ["create"],
            null,
            afterDocument.RootElement,
            null,
            null,
            null);
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new[]
            {
                new ResourceChange(
                    "custom_resource.example",
                    null,
                    "managed",
                    "custom_resource",
                    "example",
                    "custom",
                    change)
            });
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new SummaryOverrideProviderModule());
        var builder = new ReportModelBuilder(providerRegistry: providerRegistry);

        var model = builder.Build(plan);

        model.Changes.Should().ContainSingle();
        model.Changes.Single().SummaryHtml.Should().Be(SummaryOverrideFactory.OverrideSummaryHtml);
    }

    private sealed class SummaryOverrideProviderModule : IProviderModule
    {
        public string ProviderName => "custom";

        public string TemplateResourcePrefix => "";

        public void RegisterHelpers(ScriptObject scriptObject)
        {
        }

        public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
        {
            registry.RegisterFactory("custom_resource", new SummaryOverrideFactory());
        }
    }

    private sealed class SummaryOverrideFactory : IResourceViewModelFactory
    {
        public const string OverrideSummaryHtml = "<code>factory-summary</code>";

        public void ApplyViewModel(
            ResourceChangeModel model,
            ResourceChange resourceChange,
            string action,
            System.Collections.Generic.IReadOnlyList<AttributeChangeModel> attributeChanges)
        {
            model.SummaryHtml = OverrideSummaryHtml;
        }
    }
}
