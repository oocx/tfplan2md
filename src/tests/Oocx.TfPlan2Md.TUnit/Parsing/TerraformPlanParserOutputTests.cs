using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Tests for parsing Terraform output changes from plan JSON.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
public class TerraformPlanParserOutputTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Parse_PlanWithOutputs_ParsesOutputChanges()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-basic-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.OutputChanges.Should().NotBeNull();
        plan.OutputChanges.Should().HaveCount(4);
    }

    [Test]
    public void Parse_PlanWithOutputs_ParsesCreateAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-basic-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var createdOutput = plan.OutputChanges!["created_output"];
        createdOutput.Actions.Should().ContainSingle().Which.Should().Be("create");
        ((JsonElement)createdOutput.After!).GetString().Should().Be("new-value-123");
        createdOutput.Before.Should().BeNull();
        createdOutput.AfterUnknown.Should().BeFalse();
    }

    [Test]
    public void Parse_PlanWithOutputs_ParsesUpdateAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-basic-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var updatedOutput = plan.OutputChanges!["updated_output"];
        updatedOutput.Actions.Should().ContainSingle().Which.Should().Be("update");
        ((JsonElement)updatedOutput.Before!).GetString().Should().Be("old-value");
        ((JsonElement)updatedOutput.After!).GetString().Should().Be("new-value");
    }

    [Test]
    public void Parse_PlanWithOutputs_ParsesDeleteAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-basic-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var deletedOutput = plan.OutputChanges!["deleted_output"];
        deletedOutput.Actions.Should().ContainSingle().Which.Should().Be("delete");
        ((JsonElement)deletedOutput.Before!).GetString().Should().Be("value-to-remove");
        deletedOutput.After.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithOutputs_ParsesSensitiveFlag()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-sensitive-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var sensitiveOutput = plan.OutputChanges!["api_key"];
        sensitiveOutput.AfterSensitive.Should().NotBeNull();
    }

    [Test]
    public void Parse_PlanWithOutputs_ParsesComputedFlag()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-computed-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        var computedOutput = plan.OutputChanges!["resource_id"];
        computedOutput.AfterUnknown.Should().BeTrue();
        computedOutput.After.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithoutOutputs_ReturnsNullOutputChanges()
    {
        // Arrange
        var json = File.ReadAllText("TestData/outputs-no-outputs-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.OutputChanges.Should().BeNull();
    }
}
