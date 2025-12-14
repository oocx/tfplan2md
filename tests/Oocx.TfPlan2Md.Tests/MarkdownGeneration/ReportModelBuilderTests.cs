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
        Assert.Equal(3, model.Summary.ToAdd);      // resource_group, storage_account, azuredevops_project
        Assert.Equal(1, model.Summary.ToChange);   // key_vault
        Assert.Equal(1, model.Summary.ToDestroy);  // virtual_network
        Assert.Equal(1, model.Summary.ToReplace);  // git_repository
        Assert.Equal(6, model.Summary.Total);
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
        Assert.Equal("+", createChange.ActionSymbol);

        var updateChange = model.Changes.First(c => c.Action == "update");
        Assert.Equal("~", updateChange.ActionSymbol);

        var deleteChange = model.Changes.First(c => c.Action == "delete");
        Assert.Equal("-", deleteChange.ActionSymbol);

        var replaceChange = model.Changes.First(c => c.Action == "replace");
        Assert.Equal("-/+", replaceChange.ActionSymbol);
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
        Assert.NotNull(sensitiveAttr);
        Assert.True(sensitiveAttr.IsSensitive);
        Assert.Equal("(sensitive)", sensitiveAttr.After);
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
        Assert.NotNull(sensitiveAttr);
        Assert.True(sensitiveAttr.IsSensitive);
        // The value should NOT be "(sensitive)" when showSensitive is true
        Assert.NotEqual("(sensitive)", sensitiveAttr.After);
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
        Assert.Equal("1.14.0", model.TerraformVersion);
        Assert.Equal("1.2", model.FormatVersion);
    }
}
