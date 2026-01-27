using System.Globalization;
using System.Linq;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureRM;
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
    public void Build_SummaryHtml_ApimOperation_IncludesContext()
    {
        var afterDocument = JsonDocument.Parse("{\"display_name\":\"Get Profile\",\"operation_id\":\"get-profile\",\"api_name\":\"user-api\",\"api_management_name\":\"example-apim\",\"resource_group_name\":\"rg-apim\"}");
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
                    "azurerm_api_management_api_operation.this",
                    null,
                    "managed",
                    "azurerm_api_management_api_operation",
                    "this",
                    "azurerm",
                    change)
            });
        var builder = new ReportModelBuilder();

        var model = builder.Build(plan);

        model.Changes.Should().ContainSingle();
        model.Changes.Single().SummaryHtml.Should().Be(
            $"➕{Nbsp}azurerm_api_management_api_operation <b><code>this</code></b> <code>Get Profile</code> — <code>get-profile</code> <code>user-api</code> <code>example-apim</code> in <code>📁{Nbsp}rg-apim</code>");
    }

    [Test]
    public void Build_SummaryHtml_ApimNamedValue_IncludesApiManagementName()
    {
        var afterDocument = JsonDocument.Parse("{\"name\":\"IDP-WEB-CLIENT-ID\",\"api_management_name\":\"example-apim\",\"resource_group_name\":\"rg-apim\"}");
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
                    "azurerm_api_management_named_value.this",
                    null,
                    "managed",
                    "azurerm_api_management_named_value",
                    "this",
                    "azurerm",
                    change)
            });
        var builder = new ReportModelBuilder();

        var model = builder.Build(plan);

        model.Changes.Should().ContainSingle();
        model.Changes.Single().SummaryHtml.Should().Be(
            $"➕{Nbsp}azurerm_api_management_named_value <b><code>this</code></b> — <code>🆔{Nbsp}IDP-WEB-CLIENT-ID</code> <code>example-apim</code> in <code>📁{Nbsp}rg-apim</code>");
    }
}
