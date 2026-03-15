using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

public class TerraformPlanParserTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
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

    [Test]
    public void Parse_ValidPlan_ReturnsCorrectResourceCount()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceChanges.Should().HaveCount(6);
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public void Parse_ImportPlan_ParsesImportId()
    {
        // Arrange
        var json = File.ReadAllText("TestData/import-resource.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var resource = plan.ResourceChanges.Should().ContainSingle().Subject;
        resource.Change.Importing.Should().NotBeNull();
        resource.Change.Importing!.Id.Should().Be("rg-existing");
    }

    [Test]
    public void Parse_MovedPlan_ParsesPreviousAddress()
    {
        // Arrange
        var json = File.ReadAllText("TestData/moved-resource.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var resource = plan.ResourceChanges.Should().ContainSingle().Subject;
        resource.PreviousAddress.Should().Be("module.old.azurerm_virtual_network.hub");
    }

    [Test]
    public void Parse_InvalidJson_ThrowsTerraformPlanParseException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var act = () => _parser.Parse(invalidJson);

        // Assert
        act.Should().Throw<TerraformPlanParseException>();
    }

    [Test]
    public void Parse_EmptyJson_ThrowsTerraformPlanParseException()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var act = () => _parser.Parse(emptyJson);

        // Assert
        act.Should().Throw<TerraformPlanParseException>();
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    /// <summary>
    /// Ensures the configuration block is parsed when present.
    /// </summary>
    [Test]
    public void Parse_PlanWithConfiguration_ParsesConfigurationBlock()
    {
        var json = """
                        {
                            "format_version": "1.2",
                            "terraform_version": "1.14.0",
                            "resource_changes": [],
                            "configuration": {
                                "root_module": {
                                    "resources": [
                                        {
                                            "address": "example.resource",
                                            "mode": "managed",
                                            "type": "example",
                                            "name": "resource",
                                            "expressions": {
                                                "parent_id": {
                                                    "references": ["example.parent.id"]
                                                }
                                            }
                                        }
                                    ]
                                }
                            }
                        }
                        """;

        var plan = _parser.Parse(json);

        plan.Configuration.HasValue.Should().BeTrue();
        plan.Configuration!.Value.TryGetProperty("root_module", out _).Should().BeTrue();
    }

    /// <summary>
    /// Ensures parsing succeeds when the configuration block is absent.
    /// </summary>
    [Test]
    public void Parse_PlanWithoutConfiguration_ConfigurationIsNull()
    {
        var json = File.ReadAllText("TestData/minimal-plan.json");

        var plan = _parser.Parse(json);

        plan.Configuration.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithTimestamp_ParsesTimestamp()
    {
        // Arrange
        var json = File.ReadAllText("TestData/timestamp-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.Timestamp.Should().Be("2025-12-20T10:00:00Z");
    }

    [Test]
    public void Parse_NullInput_ThrowsTerraformPlanParseException()
    {
        // Arrange
        string? nullJson = null;

        // Act
        var act = () => _parser.Parse(nullJson!);

        // Assert
        act.Should().Throw<TerraformPlanParseException>()
            .WithMessage("*cannot be null*");
    }

    /// <summary>
    /// TC-113-1: Plan JSON without a resource_changes key should parse without throwing.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void Parse_PlanWithMissingResourceChanges_DoesNotThrow()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-resource-changes-plan.json");

        // Act
        var act = () => _parser.Parse(json);

        // Assert – parsing itself should succeed; ResourceChanges may be null or empty
        act.Should().NotThrow();
    }

    /// <summary>
    /// TC-113-2: Plan JSON with explicit null resource_changes should parse without throwing.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void Parse_PlanWithNullResourceChanges_DoesNotThrow()
    {
        // Arrange
        var json = File.ReadAllText("TestData/null-resource-changes-plan.json");

        // Act
        var act = () => _parser.Parse(json);

        // Assert – parsing itself should succeed; ResourceChanges may be null or empty
        act.Should().NotThrow();
    }
}
