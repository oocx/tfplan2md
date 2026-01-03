using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderTests
{
    private readonly TerraformPlanParser _parser = new();

    [Fact]
    public void Build_ValidPlan_ReturnsCorrectSummary()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(3);      // resource_group, storage_account, azuredevops_project
        model.Summary.ToChange.Count.Should().Be(1);   // key_vault
        model.Summary.ToDestroy.Count.Should().Be(1);  // virtual_network
        model.Summary.ToReplace.Count.Should().Be(1);  // git_repository
        model.Summary.Total.Should().Be(6);
    }

    [Fact]
    public void Build_ValidPlan_ReturnsCorrectActionSymbols()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var createChange = model.Changes.First(c => c.Action == "create");
        createChange.ActionSymbol.Should().Be("➕");

        var updateChange = model.Changes.First(c => c.Action == "update");
        updateChange.ActionSymbol.Should().Be("🔄");

        var deleteChange = model.Changes.First(c => c.Action == "delete");
        deleteChange.ActionSymbol.Should().Be("❌");

        var replaceChange = model.Changes.First(c => c.Action == "replace");
        replaceChange.ActionSymbol.Should().Be("♻️");
    }

    [Fact]
    public void Build_WithSensitiveValues_MasksByDefault()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: false);

        // Act
        var model = builder.Build(plan);

        // Assert
        var storageAccount = model.Changes.First(c => c.Address == "azurerm_storage_account.main");
        var sensitiveAttr = storageAccount.AttributeChanges.FirstOrDefault(a => a.Name == "primary_access_key");
        sensitiveAttr.Should().NotBeNull();
        sensitiveAttr!.IsSensitive.Should().BeTrue();
        sensitiveAttr.After.Should().Be("(sensitive)");
    }

    [Fact]
    public void Build_WithShowSensitiveTrue_DoesNotMask()
    {
        // Arrange - need a plan with actual sensitive values to test this properly
        // For now, we verify the flag is being used
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: true);

        // Act
        var model = builder.Build(plan);

        // Assert - when showSensitive is true, sensitive values should not be masked
        var storageAccount = model.Changes.First(c => c.Address == "azurerm_storage_account.main");
        var sensitiveAttr = storageAccount.AttributeChanges.FirstOrDefault(a => a.Name == "primary_access_key");
        sensitiveAttr.Should().NotBeNull();
        sensitiveAttr!.IsSensitive.Should().BeTrue();
        // The value should NOT be "(sensitive)" when showSensitive is true
        sensitiveAttr.After.Should().NotBe("(sensitive)");
    }

    [Fact]
    public void Build_ValidPlan_PreservesTerraformVersion()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.TerraformVersion.Should().Be("1.14.0");
        model.FormatVersion.Should().Be("1.2");
    }

    [Fact]
    public void Build_PlanWithTimestamp_PreservesTimestamp()
    {
        // Arrange
        var json = File.ReadAllText("TestData/timestamp-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Timestamp.Should().Be("2025-12-20T10:00:00Z");
    }

    [Fact]
    public void Build_EmptyPlan_ReturnsZeroSummary()
    {
        // Arrange
        var json = File.ReadAllText("TestData/empty-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(0);
        model.Summary.ToChange.Count.Should().Be(0);
        model.Summary.ToDestroy.Count.Should().Be(0);
        model.Summary.ToReplace.Count.Should().Be(0);
        model.Summary.NoOp.Count.Should().Be(0);
        model.Summary.Total.Should().Be(0);
        model.Changes.Should().BeEmpty();
    }

    [Fact]
    public void Build_NoOpPlan_CountsNoOpCorrectly()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-op-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert - no-op resources are counted in summary but not included in Changes
        // to avoid exceeding Scriban's iteration limit on large plans
        model.Summary.ToAdd.Count.Should().Be(0);
        model.Summary.ToChange.Count.Should().Be(0);
        model.Summary.ToDestroy.Count.Should().Be(0);
        model.Summary.ToReplace.Count.Should().Be(0);
        model.Summary.NoOp.Count.Should().Be(1);
        model.Summary.Total.Should().Be(0);
        model.Changes.Should().BeEmpty(); // no-op resources are filtered out
    }

    [Fact]
    public void Build_MinimalPlan_HandlesNullBeforeAndAfter()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(1);
        var change = model.Changes.Should().ContainSingle().Subject;
        change.Action.Should().Be("create");
        // With null before and after, there should be no attribute changes
        change.AttributeChanges.Should().BeEmpty();
    }

    [Fact]
    public void Build_CreateOnlyPlan_CountsCreatesCorrectly()
    {
        // Arrange
        var json = File.ReadAllText("TestData/create-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(2);
        model.Summary.ToChange.Count.Should().Be(0);
        model.Summary.ToDestroy.Count.Should().Be(0);
        model.Summary.Total.Should().Be(2);
        model.Changes.Should().OnlyContain(c => c.Action == "create");
    }

    [Fact]
    public void Build_ComputesBreakdownByTypePerAction()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new("type_a.one", null, "managed", "type_a", "one", "provider", new Change(["create"])),
                new("type_a.two", null, "managed", "type_a", "two", "provider", new Change(["create"])),
                new("type_b.one", null, "managed", "type_b", "one", "provider", new Change(["create"]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(3);
        model.Summary.ToAdd.Breakdown.Should().HaveCount(2);
        model.Summary.ToAdd.Breakdown.First(b => b.Type == "type_a").Count.Should().Be(2);
        model.Summary.ToAdd.Breakdown.First(b => b.Type == "type_b").Count.Should().Be(1);
    }

    [Fact]
    public void Build_WithReportTitle_EscapesMarkdownCharacters()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(reportTitle: "My # Title [Draft]");

        // Act
        var model = builder.Build(plan);

        // Assert
        model.ReportTitle.Should().Be("My \\# Title \\[Draft\\]");
    }

    [Fact]
    public void Build_WithoutReportTitle_SetsReportTitleToNull()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.ReportTitle.Should().BeNull();
    }

    [Fact]
    public void Build_SortsBreakdownAlphabetically()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new("type_b.one", null, "managed", "type_b", "one", "provider", new Change(["update"])),
                new("type_c.one", null, "managed", "type_c", "one", "provider", new Change(["update"])),
                new("type_a.one", null, "managed", "type_a", "one", "provider", new Change(["update"]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToChange.Breakdown.Select(b => b.Type).Should().Equal("type_a", "type_b", "type_c");
    }

    [Fact]
    public void Build_ActionWithNoResources_HasEmptyBreakdown()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new("type_a.one", null, "managed", "type_a", "one", "provider", new Change(["create"]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToDestroy.Count.Should().Be(0);
        model.Summary.ToDestroy.Breakdown.Should().BeEmpty();
    }

    [Fact]
    public void Build_DeleteOnlyPlan_CountsDeletesCorrectly()
    {
        // Arrange
        var json = File.ReadAllText("TestData/delete-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(0);
        model.Summary.ToChange.Count.Should().Be(0);
        model.Summary.ToDestroy.Count.Should().Be(2);
        model.Summary.Total.Should().Be(2);
        model.Changes.Should().OnlyContain(c => c.Action == "delete");
    }
}
