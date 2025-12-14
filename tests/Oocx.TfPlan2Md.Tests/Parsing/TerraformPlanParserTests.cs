using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.Parsing;

public class TerraformPlanParserTests
{
    private readonly TerraformPlanParser _parser = new();

    [Fact]
    public void Parse_ValidPlan_ReturnsCorrectVersion()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        Assert.Equal("1.14.0", plan.TerraformVersion);
        Assert.Equal("1.2", plan.FormatVersion);
    }

    [Fact]
    public void Parse_ValidPlan_ReturnsCorrectResourceCount()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        Assert.Equal(6, plan.ResourceChanges.Count);
    }

    [Fact]
    public void Parse_ValidPlan_ParsesCreateAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var resourceGroup = plan.ResourceChanges.First(r => r.Address == "azurerm_resource_group.main");
        Assert.Single(resourceGroup.Change.Actions);
        Assert.Contains("create", resourceGroup.Change.Actions);
    }

    [Fact]
    public void Parse_ValidPlan_ParsesUpdateAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var keyVault = plan.ResourceChanges.First(r => r.Address == "azurerm_key_vault.main");
        Assert.Single(keyVault.Change.Actions);
        Assert.Contains("update", keyVault.Change.Actions);
    }

    [Fact]
    public void Parse_ValidPlan_ParsesDeleteAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var vnet = plan.ResourceChanges.First(r => r.Address == "azurerm_virtual_network.old");
        Assert.Single(vnet.Change.Actions);
        Assert.Contains("delete", vnet.Change.Actions);
    }

    [Fact]
    public void Parse_ValidPlan_ParsesReplaceAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var gitRepo = plan.ResourceChanges.First(r => r.Address == "azuredevops_git_repository.main");
        Assert.Equal(2, gitRepo.Change.Actions.Count);
        Assert.Contains("create", gitRepo.Change.Actions);
        Assert.Contains("delete", gitRepo.Change.Actions);
    }

    [Fact]
    public void Parse_ValidPlan_ParsesAzureDevOpsProvider()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var project = plan.ResourceChanges.First(r => r.Type == "azuredevops_project");
        Assert.Equal("registry.terraform.io/microsoft/azuredevops", project.ProviderName);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsTerraformPlanParseException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.Throws<TerraformPlanParseException>(() => _parser.Parse(invalidJson));
    }

    [Fact]
    public void Parse_EmptyJson_ThrowsTerraformPlanParseException()
    {
        // Arrange
        var emptyJson = "";

        // Act & Assert
        Assert.Throws<TerraformPlanParseException>(() => _parser.Parse(emptyJson));
    }
}
