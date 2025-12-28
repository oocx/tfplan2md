using System.Globalization;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderSummaryTests
{
    [Fact]
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

    [Fact]
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
        update.SummaryHtml.Should().StartWith($"{update.ActionSymbol} {update.Type} <b><code>{update.Name}</code></b>");

        var createWithTags = model.Changes.First(c => c.Action == "create" && c.TagsBadges is not null);
        createWithTags.TagsBadges.Should().Contain("🏷️");
        createWithTags.TagsBadges.Should().Contain("environment: production");
    }

    [Fact]
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
        update.ChangedAttributesSummary.Should().Be("4 🔧 account_replication_type, https_only, kind, +1 more");
    }

    [Fact]
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

    [Fact]
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
        vnetDelete.SummaryHtml.Should().Contain("<code>🌍 westeurope</code>");
        vnetDelete.SummaryHtml.Should().Contain("<code>🌐 10.0.0.0/16</code>");
    }
}
