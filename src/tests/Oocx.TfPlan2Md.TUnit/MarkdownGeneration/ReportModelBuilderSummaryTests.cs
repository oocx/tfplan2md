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
    private const string CreateAction = "create";
    private const string ManagedMode = "managed";
    private const string DefaultProviderName = "azurerm";

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

        var createWithTags = model.Changes.First(c => c.Action == CreateAction && c.TagsBadges is not null);
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
            $"3🔧{Nbsp}➕{Nbsp}<code>allow-dns</code>, 🔄{Nbsp}<code>allow-http</code>, ❌{Nbsp}<code>allow-ssh-old</code>");
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

    [Test]
    public void Build_SummaryHtml_SubscriptionFallback_IncludesKeyIcon()
    {
        var afterDocument = JsonDocument.Parse("{\"subscription_id\":\"11111111-2222-3333-4444-555555555555\",\"subscription\":\"Production\"}");
        var change = new Change(
            [CreateAction],
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
                    ManagedMode,
                    "azurerm_subscription",
                    "demo",
                    DefaultProviderName,
                    change)
            });
        var builder = new ReportModelBuilder();

        var model = builder.Build(plan);

        model.Changes.Should().ContainSingle();
        model.Changes.Single().SummaryHtml.Should().Contain("🔑");
    }

    [Test]
    public void Build_SummaryHtml_RespectsFactoryOverride()
    {
        var afterDocument = JsonDocument.Parse("{\"name\":\"example\"}");
        var change = new Change(
            [CreateAction],
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
                    ManagedMode,
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
