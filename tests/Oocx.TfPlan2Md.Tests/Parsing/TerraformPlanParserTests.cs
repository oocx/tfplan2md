using AwesomeAssertions;
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
        plan.TerraformVersion.Should().Be("1.14.0");
        plan.FormatVersion.Should().Be("1.2");
    }

    [Fact]
    public void Parse_ValidPlan_ReturnsCorrectResourceCount()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceChanges.Should().HaveCount(6);
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
        resourceGroup.Change.Actions.Should().ContainSingle()
            .Which.Should().Be("create");
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
        keyVault.Change.Actions.Should().ContainSingle()
            .Which.Should().Be("update");
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
        vnet.Change.Actions.Should().ContainSingle()
            .Which.Should().Be("delete");
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
        gitRepo.Change.Actions.Should().HaveCount(2)
            .And.Contain("create")
            .And.Contain("delete");
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
        project.ProviderName.Should().Be("registry.terraform.io/microsoft/azuredevops");
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsTerraformPlanParseException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var act = () => _parser.Parse(invalidJson);

        // Assert
        act.Should().Throw<TerraformPlanParseException>();
    }

    [Fact]
    public void Parse_EmptyJson_ThrowsTerraformPlanParseException()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var act = () => _parser.Parse(emptyJson);

        // Assert
        act.Should().Throw<TerraformPlanParseException>();
    }

    [Fact]
    public void Parse_EmptyPlan_ReturnsEmptyResourceChanges()
    {
        // Arrange
        var json = File.ReadAllText("TestData/empty-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceChanges.Should().BeEmpty();
        plan.TerraformVersion.Should().Be("1.14.0");
        plan.FormatVersion.Should().Be("1.2");
    }

    [Fact]
    public void Parse_NoOpPlan_ParsesNoOpAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-op-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceChanges.Should().ContainSingle()
            .Which.Change.Actions.Should().ContainSingle()
            .Which.Should().Be("no-op");
    }

    [Fact]
    public void Parse_MinimalPlan_HandlesNullBeforeAndAfter()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var resource = plan.ResourceChanges.Should().ContainSingle().Subject;
        resource.Change.Before.Should().BeNull();
        resource.Change.After.Should().BeNull();
    }

    [Fact]
    public void Parse_CreateOnlyPlan_ParsesMultipleCreates()
    {
        // Arrange
        var json = File.ReadAllText("TestData/create-only-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceChanges.Should().HaveCount(2)
            .And.OnlyContain(r => r.Change.Actions.Contains("create"));
    }

    [Fact]
    public void Parse_DeleteOnlyPlan_ParsesMultipleDeletes()
    {
        // Arrange
        var json = File.ReadAllText("TestData/delete-only-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceChanges.Should().HaveCount(2)
            .And.OnlyContain(r => r.Change.Actions.Contains("delete"));
    }
}
