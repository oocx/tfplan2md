using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Parser tests for TerraformPlan extensions (optional Terraform 1.14/1.15 fields).
/// Related feature: docs/features/122-terraform-1-15-support/test-plan.md Task 2.
/// </summary>
public class TerraformPlanExtensionsParserTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Parse_PlanWithoutActions_LeavesActionsCollectionEmpty()
    {
        // Arrange
        var json = File.ReadAllText("TestData/tf-1-13-baseline-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ActionInvocations.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithoutDeferredActions_LeavesDeferredCollectionEmpty()
    {
        // Arrange
        var json = File.ReadAllText("TestData/tf-1-13-baseline-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.DeferredActionInvocations.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithoutDrift_LeavesDriftCollectionEmpty()
    {
        // Arrange
        var json = File.ReadAllText("TestData/tf-1-13-baseline-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceDrift.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithoutRelevantAttributes_LeavesCollectionEmpty()
    {
        // Arrange
        var json = File.ReadAllText("TestData/tf-1-13-baseline-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.RelevantAttributes.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithoutStatusBooleans_LeavesBooleansNull()
    {
        // Arrange
        var json = File.ReadAllText("TestData/tf-1-13-baseline-plan.json");

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.Applyable.Should().BeNull();
        plan.Complete.Should().BeNull();
        plan.Errored.Should().BeNull();
    }

    [Test]
    public void Parse_PlanWithFormatVersion12_AcceptsAllNewFields()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "applyable": true,
          "complete": false,
          "errored": false,
          "resource_changes": [],
          "action_invocations": [
            {
              "address": "example_action.test",
              "type": "example_action",
              "name": "test",
              "provider_name": "registry.terraform.io/example/example",
              "invoke_action_trigger": {}
            }
          ],
          "deferred_action_invocations": [],
          "resource_drift": [],
          "relevant_attributes": [
            {
              "resource": "example_resource.upstream",
              "attribute": ["name"]
            }
          ],
          "variables": {
            "test_var": {
              "value": "example"
            }
          }
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert (NFR-2: format_version 1.2 accepted with all new fields)
        plan.FormatVersion.Should().Be("1.2");
        plan.TerraformVersion.Should().Be("1.14.0");
        plan.Applyable.Should().Be(true);
        plan.Complete.Should().Be(false);
        plan.Errored.Should().Be(false);
        plan.ActionInvocations.Should().HaveCount(1);
        plan.DeferredActionInvocations.Should().HaveCount(0);
        plan.ResourceDrift.Should().HaveCount(0);
        plan.RelevantAttributes.Should().HaveCount(1);
        plan.Variables.Should().HaveCount(1);
    }

    [Test]
    public void Parse_PlanWithActionInvocations_PopulatesCollection()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [],
          "action_invocations": [
            {
              "address": "example_action.a",
              "type": "example_action",
              "name": "a",
              "provider_name": "registry.terraform.io/example/example",
              "invoke_action_trigger": {}
            },
            {
              "address": "example_action.b",
              "type": "example_action",
              "name": "b",
              "provider_name": "registry.terraform.io/example/example",
              "lifecycle_action_trigger": {
                "triggering_resource_address": "example_resource.parent",
                "action_trigger_event": "before_create"
              }
            }
          ]
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ActionInvocations.Should().HaveCount(2);
        plan.ActionInvocations![0].Address.Should().Be("example_action.a");
        plan.ActionInvocations[1].Address.Should().Be("example_action.b");
    }

    [Test]
    public void Parse_PlanWithDeferredActions_PopulatesCollection()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [],
          "deferred_action_invocations": [
            {
              "address": "example_action.deferred",
              "type": "example_action",
              "name": "deferred",
              "provider_name": "registry.terraform.io/example/example",
              "lifecycle_action_trigger": {
                "triggering_resource_address": "example_resource.parent",
                "action_trigger_event": "after_create"
              }
            }
          ]
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.DeferredActionInvocations.Should().HaveCount(1);
        plan.DeferredActionInvocations![0].Address.Should().Be("example_action.deferred");
    }

    [Test]
    public void Parse_PlanWithResourceDrift_PopulatesCollection()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [],
          "resource_drift": [
            {
              "address": "example_resource.drifted",
              "mode": "managed",
              "type": "example_resource",
              "name": "drifted",
              "provider_name": "registry.terraform.io/example/example",
              "change": {
                "actions": ["update"],
                "before": {"value": "old"},
                "after": {"value": "new"},
                "after_unknown": {},
                "before_sensitive": {},
                "after_sensitive": {}
              }
            }
          ]
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.ResourceDrift.Should().HaveCount(1);
        plan.ResourceDrift![0].Address.Should().Be("example_resource.drifted");
        plan.ResourceDrift[0].Change.Actions.Should().ContainSingle().Which.Should().Be("update");
    }

    [Test]
    public void Parse_PlanWithRelevantAttributes_PopulatesCollection()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [],
          "relevant_attributes": [
            {
              "resource": "example_resource.upstream",
              "attribute": ["name"]
            },
            {
              "resource": "example_resource.other",
              "attribute": ["tags", 0, "key"]
            }
          ]
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.RelevantAttributes.Should().HaveCount(2);
        plan.RelevantAttributes![0].Resource.Should().Be("example_resource.upstream");
        plan.RelevantAttributes[1].Resource.Should().Be("example_resource.other");
        plan.RelevantAttributes[1].Attribute.Should().HaveCount(3);
    }

    [Test]
    public void Parse_PlanWithStatusBooleans_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.14.0",
          "resource_changes": [],
          "applyable": false,
          "complete": true,
          "errored": false
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.Applyable.Should().Be(false);
        plan.Complete.Should().Be(true);
        plan.Errored.Should().Be(false);
    }

    [Test]
    public void Parse_PlanWithVariables_PopulatesCollection()
    {
        // Arrange
        var json = """
        {
          "format_version": "1.2",
          "terraform_version": "1.15.0",
          "resource_changes": [],
          "variables": {
            "var1": {"value": "test1"},
            "var2": {"value": "test2"}
          }
        }
        """;

        // Act
        var plan = _parser.Parse(json);

        // Assert
        plan.Variables.Should().HaveCount(2);
        plan.Variables!.Keys.Should().Contain("var1");
        plan.Variables.Keys.Should().Contain("var2");
    }
}
