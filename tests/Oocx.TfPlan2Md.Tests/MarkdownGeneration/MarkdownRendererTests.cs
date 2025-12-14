using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void Render_ValidPlan_ContainsSummarySection()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        Assert.Contains("## Summary", markdown);
        Assert.Contains("➕ Add", markdown);
        Assert.Contains("🔄 Change", markdown);
        Assert.Contains("❌ Destroy", markdown);
    }

    [Fact]
    public void Render_ValidPlan_ContainsResourceChanges()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        Assert.Contains("azurerm_resource_group.main", markdown);
        Assert.Contains("azurerm_storage_account.main", markdown);
        Assert.Contains("azurerm_key_vault.main", markdown);
        Assert.Contains("azuredevops_project.main", markdown);
    }

    [Fact]
    public void Render_ValidPlan_ContainsTerraformVersion()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        Assert.Contains("1.14.0", markdown);
    }

    [Fact]
    public void Render_ValidPlan_ContainsProviderInfo()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        Assert.Contains("registry.terraform.io/hashicorp/azurerm", markdown);
        Assert.Contains("registry.terraform.io/microsoft/azuredevops", markdown);
    }

    [Fact]
    public void Render_ValidPlan_ContainsActionSymbols()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        Assert.Contains("+ azurerm_resource_group.main", markdown);
        Assert.Contains("~ azurerm_key_vault.main", markdown);
        Assert.Contains("- azurerm_virtual_network.old", markdown);
        Assert.Contains("-/+ azuredevops_git_repository.main", markdown);
    }
}
