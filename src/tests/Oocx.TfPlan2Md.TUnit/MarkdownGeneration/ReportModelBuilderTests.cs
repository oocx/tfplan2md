using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderTests
{
    private const string AzurermAzureDevopsPlanPath = "TestData/azurerm-azuredevops-plan.json";
    private const string CreateAction = "create";
    private const string UpdateAction = "update";
    private const string DeleteAction = "delete";
    private const string ReplaceAction = "replace";
    private const string ManagedMode = "managed";
    private const string ProviderName = "provider";
    private const string TypeA = "type_a";
    private const string TypeB = "type_b";
    private const string TypeC = "type_c";

    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Build_ValidPlan_ReturnsCorrectSummary()
    {
        // Arrange
        var json = File.ReadAllText(AzurermAzureDevopsPlanPath);
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

    [Test]
    public void Build_ValidPlan_ReturnsCorrectActionSymbols()
    {
        // Arrange
        var json = File.ReadAllText(AzurermAzureDevopsPlanPath);
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        var createChange = model.Changes.First(c => c.Action == CreateAction);
        createChange.ActionSymbol.Should().Be(ActionIcons.Add);

        var updateChange = model.Changes.First(c => c.Action == UpdateAction);
        updateChange.ActionSymbol.Should().Be(ActionIcons.Update);

        var deleteChange = model.Changes.First(c => c.Action == DeleteAction);
        deleteChange.ActionSymbol.Should().Be(ActionIcons.Delete);

        var replaceChange = model.Changes.First(c => c.Action == ReplaceAction);
        replaceChange.ActionSymbol.Should().Be(ActionIcons.Replace);
    }

    [Test]
    public void Build_WithSensitiveValues_MasksByDefault()
    {
        // Arrange
        var json = File.ReadAllText(AzurermAzureDevopsPlanPath);
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

    [Test]
    public void Build_WithShowSensitiveTrue_DoesNotMask()
    {
        // Arrange - need a plan with actual sensitive values to test this properly
        // For now, we verify the flag is being used
        var json = File.ReadAllText(AzurermAzureDevopsPlanPath);
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

    [Test]
    public void Build_WithSensitiveArrayAttributes_MasksByDefault()
    {
        // Arrange - test with plan1.json which has azuredevops_build_definition with sensitive variable array
        var json = File.ReadAllText("TestData/TerraformShow/plan1.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: false);

        // Act
        var model = builder.Build(plan);

        // Assert - variable array is marked sensitive, so all variable[*] attributes should be masked
        var buildDef = model.Changes.FirstOrDefault(c => c.Address == "azuredevops_build_definition.example2");
        buildDef.Should().NotBeNull();

        // Check that variable array items are masked
        var variableNameAttr = buildDef!.AttributeChanges.FirstOrDefault(a => a.Name == "variable[0].name");
        variableNameAttr.Should().NotBeNull();
        variableNameAttr!.IsSensitive.Should().BeTrue("entire variable array is marked sensitive");
        variableNameAttr.After.Should().Be("(sensitive)", "array items should be masked when parent array is sensitive");

        var variableSecretAttr = buildDef.AttributeChanges.FirstOrDefault(a => a.Name == "variable[0].secret_value");
        variableSecretAttr.Should().NotBeNull();
        variableSecretAttr!.IsSensitive.Should().BeTrue("entire variable array is marked sensitive");
        variableSecretAttr.After.Should().Be("(sensitive)", "secret_value should be masked when parent array is sensitive");

        var variableValueAttr = buildDef.AttributeChanges.FirstOrDefault(a => a.Name == "variable[0].value");
        variableValueAttr.Should().NotBeNull();
        variableValueAttr!.IsSensitive.Should().BeTrue("entire variable array is marked sensitive");
        variableValueAttr.After.Should().Be("(sensitive)", "array items should be masked when parent array is sensitive");
    }

    [Test]
    public void Build_WithSensitiveArrayAttributes_ShowsWhenFlagSet()
    {
        // Arrange - test with plan1.json which has azuredevops_build_definition with sensitive variable array
        var json = File.ReadAllText("TestData/TerraformShow/plan1.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: true);

        // Act
        var model = builder.Build(plan);

        // Assert - with showSensitive=true, values should be shown even though array is marked sensitive
        var buildDef = model.Changes.FirstOrDefault(c => c.Address == "azuredevops_build_definition.example2");
        buildDef.Should().NotBeNull();

        // Check that variable array items are still marked as sensitive but values are NOT masked
        var variableNameAttr = buildDef!.AttributeChanges.FirstOrDefault(a => a.Name == "variable[0].name");
        variableNameAttr.Should().NotBeNull();
        variableNameAttr!.IsSensitive.Should().BeTrue("entire variable array is marked sensitive");
        variableNameAttr.After.Should().NotBe("(sensitive)", "values should be shown when --show-sensitive is set");
        variableNameAttr.After.Should().Be("BUILD_CONFIGURATION", "actual value should be shown");

        var variableValueAttr = buildDef.AttributeChanges.FirstOrDefault(a => a.Name == "variable[0].value");
        variableValueAttr.Should().NotBeNull();
        variableValueAttr!.IsSensitive.Should().BeTrue("entire variable array is marked sensitive");
        variableValueAttr.After.Should().NotBe("(sensitive)", "values should be shown when --show-sensitive is set");
        variableValueAttr.After.Should().Be("Release", "actual value should be shown");
    }

    [Test]
    public void Build_WithNestedSensitiveAttributes_MasksNestedValues()
    {
        // Arrange - test with plan1.json which has azuredevops_variable_group with sensitive secret_variable array
        var json = File.ReadAllText("TestData/TerraformShow/plan1.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: false, showUnchangedValues: true);

        // Act
        var model = builder.Build(plan);

        // Assert - secret_variable array is marked sensitive in before_sensitive/after_sensitive
        var variableGroup = model.Changes.FirstOrDefault(c => c.Address == "azuredevops_variable_group.example");
        variableGroup.Should().NotBeNull();

        // Check that secret_variable array items are masked
        var secretNameAttr = variableGroup!.AttributeChanges.FirstOrDefault(a => a.Name == "secret_variable[0].name");
        secretNameAttr.Should().NotBeNull("secret_variable array should have name attribute");
        secretNameAttr!.IsSensitive.Should().BeTrue("entire secret_variable array is marked sensitive");
        secretNameAttr.After.Should().Be("(sensitive)", "array items should be masked when parent array is sensitive");

        var secretValueAttr = variableGroup.AttributeChanges.FirstOrDefault(a => a.Name == "secret_variable[0].value");
        secretValueAttr.Should().NotBeNull("secret_variable array should have value attribute");
        secretValueAttr!.IsSensitive.Should().BeTrue("entire secret_variable array is marked sensitive");
        secretValueAttr.After.Should().Be("(sensitive)", "secret value should be masked when parent array is sensitive");
    }

    [Test]
    public void Build_ValidPlan_PreservesTerraformVersion()
    {
        // Arrange
        var json = File.ReadAllText(AzurermAzureDevopsPlanPath);
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.TerraformVersion.Should().Be("1.14.0");
        model.FormatVersion.Should().Be("1.2");
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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
        change.Action.Should().Be(CreateAction);
        // With null before and after, there should be no attribute changes
        change.AttributeChanges.Should().BeEmpty();
    }

    [Test]
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
        model.Changes.Should().OnlyContain(c => c.Action == CreateAction);
    }

    [Test]
    public void Build_ComputesBreakdownByTypePerAction()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new("type_a.one", null, ManagedMode, TypeA, "one", ProviderName, new Change([CreateAction])),
                new("type_a.two", null, ManagedMode, TypeA, "two", ProviderName, new Change([CreateAction])),
                new("type_b.one", null, ManagedMode, TypeB, "one", ProviderName, new Change([CreateAction]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToAdd.Count.Should().Be(3);
        model.Summary.ToAdd.Breakdown.Should().HaveCount(2);
        model.Summary.ToAdd.Breakdown.First(b => b.Type == TypeA).Count.Should().Be(2);
        model.Summary.ToAdd.Breakdown.First(b => b.Type == TypeB).Count.Should().Be(1);
    }

    [Test]
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

    [Test]
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

    [Test]
    public void Build_SortsBreakdownAlphabetically()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new("type_b.one", null, ManagedMode, TypeB, "one", ProviderName, new Change([UpdateAction])),
                new("type_c.one", null, ManagedMode, TypeC, "one", ProviderName, new Change([UpdateAction])),
                new("type_a.one", null, ManagedMode, TypeA, "one", ProviderName, new Change([UpdateAction]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToChange.Breakdown.Select(b => b.Type).Should().Equal(TypeA, TypeB, TypeC);
    }

    [Test]
    public void Build_ActionWithNoResources_HasEmptyBreakdown()
    {
        // Arrange
        var plan = new TerraformPlan(
            "1.0",
            "1.0",
            new List<ResourceChange>
            {
                new("type_a.one", null, ManagedMode, TypeA, "one", ProviderName, new Change([CreateAction]))
            });

        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Summary.ToDestroy.Count.Should().Be(0);
        model.Summary.ToDestroy.Breakdown.Should().BeEmpty();
    }

    [Test]
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
        model.Changes.Should().OnlyContain(c => c.Action == DeleteAction);
    }
}
