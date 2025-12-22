using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderUnchangedValuesTests
{
    private readonly TerraformPlanParser _parser = new();

    [Fact]
    public void Build_Default_HidesUnchangedAttributes()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: false, showUnchangedValues: false);

        // Act
        var model = builder.Build(plan);

        // Assert
        var keyVault = model.Changes.First(c => c.Address == "azurerm_key_vault.main");
        var attributeNames = keyVault.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().HaveCount(2);
        attributeNames.Should().Contain("sku_name");
        attributeNames.Should().Contain("soft_delete_retention_days");
        attributeNames.Should().NotContain("name");
        attributeNames.Should().NotContain("location");
    }

    [Fact]
    public void Build_WithShowUnchangedValues_UsesAllAttributes()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(showSensitive: false, showUnchangedValues: true);

        // Act
        var model = builder.Build(plan);

        // Assert
        var keyVault = model.Changes.First(c => c.Address == "azurerm_key_vault.main");
        var attributeNames = keyVault.AttributeChanges.Select(a => a.Name).ToList();

        attributeNames.Should().HaveCount(4);
        attributeNames.Should().Contain("sku_name");
        attributeNames.Should().Contain("soft_delete_retention_days");
        attributeNames.Should().Contain("name");
        attributeNames.Should().Contain("location");
    }
}
