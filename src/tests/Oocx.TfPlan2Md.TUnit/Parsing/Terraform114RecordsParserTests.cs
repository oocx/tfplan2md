using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Parsing;

/// <summary>
/// Parser tests for new Terraform 1.14/1.15 records (ActionInvocation, LifecycleActionTrigger, etc.).
/// Related feature: docs/features/122-terraform-1-15-support/test-plan.md Task 1.
/// </summary>
public class Terraform114RecordsParserTests
{
    [Test]
    public void Parse_ActionInvocation_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "address": "example_action.test",
            "type": "example_action",
            "name": "test",
            "provider_name": "registry.terraform.io/example/example",
            "config_values": {"key": "value"},
            "config_sensitive": {"key": false},
            "config_unknown": {"key": false},
            "lifecycle_action_trigger": {
                "triggering_resource_address": "example_resource.a",
                "action_trigger_event": "before_create",
                "action_trigger_block_index": 0,
                "actions_list_index": 0
            }
        }
        """;

        // Act
        var action = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.ActionInvocation);

        // Assert
        action.Should().NotBeNull();
        action!.Address.Should().Be("example_action.test");
        action.Type.Should().Be("example_action");
        action.Name.Should().Be("test");
        action.ProviderName.Should().Be("registry.terraform.io/example/example");
        action.ConfigValues.Should().NotBeNull();
        action.ConfigSensitive.Should().NotBeNull();
        action.ConfigUnknown.Should().NotBeNull();
        action.LifecycleActionTrigger.Should().NotBeNull();
        action.InvokeActionTrigger.Should().BeNull();
        action.Status.Should().BeNull();
        action.Diagnostics.Should().BeNull();
    }

    [Test]
    public void Parse_ActionInvocation_WithInvokeTrigger_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "address": "example_action.invoke_test",
            "type": "example_action",
            "name": "invoke_test",
            "provider_name": "registry.terraform.io/example/example",
            "invoke_action_trigger": {}
        }
        """;

        // Act
        var action = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.ActionInvocation);

        // Assert
        action.Should().NotBeNull();
        action!.InvokeActionTrigger.Should().NotBeNull();
        action.LifecycleActionTrigger.Should().BeNull();
    }

    [Test]
    public void Parse_ActionInvocation_WithStatusAndDiagnostics_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "address": "example_action.errored",
            "type": "example_action",
            "name": "errored",
            "provider_name": "registry.terraform.io/example/example",
            "invoke_action_trigger": {},
            "status": {"errored": true},
            "diagnostics": [{"severity": "error", "summary": "action failed"}]
        }
        """;

        // Act
        var action = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.ActionInvocation);

        // Assert
        action.Should().NotBeNull();
        action!.Status.Should().NotBeNull();
        action.Diagnostics.Should().NotBeNull();
        action.Status!.Value.ValueKind.Should().Be(JsonValueKind.Object);
        action.Diagnostics!.Value.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Test]
    public void Parse_LifecycleActionTrigger_WithAllFields_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "triggering_resource_address": "example_resource.parent",
            "action_trigger_event": "after_update",
            "action_trigger_block_index": 2,
            "actions_list_index": 1
        }
        """;

        // Act
        var trigger = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.LifecycleActionTrigger);

        // Assert
        trigger.Should().NotBeNull();
        trigger!.TriggeringResourceAddress.Should().Be("example_resource.parent");
        trigger.ActionTriggerEvent.Should().Be("after_update");
        trigger.ActionTriggerBlockIndex.Should().Be(2);
        trigger.ActionsListIndex.Should().Be(1);
    }

    [Test]
    public void Parse_LifecycleActionTrigger_WithoutOptionalFields_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "triggering_resource_address": "example_resource.minimal",
            "action_trigger_event": "before_destroy"
        }
        """;

        // Act
        var trigger = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.LifecycleActionTrigger);

        // Assert
        trigger.Should().NotBeNull();
        trigger!.TriggeringResourceAddress.Should().Be("example_resource.minimal");
        trigger.ActionTriggerEvent.Should().Be("before_destroy");
        trigger.ActionTriggerBlockIndex.Should().BeNull();
        trigger.ActionsListIndex.Should().BeNull();
    }

    [Test]
    public void Parse_InvokeActionTrigger_Empty_RoundTrips()
    {
        // Arrange
        var json = "{}";

        // Act
        var trigger = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.InvokeActionTrigger);

        // Assert
        trigger.Should().NotBeNull();
        trigger!.Reason.Should().BeNull();
    }

    [Test]
    public void Parse_InvokeActionTrigger_WithReason_RoundTrips()
    {
        // Arrange
        var json = """{"reason": "explicit invocation"}""";

        // Act
        var trigger = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.InvokeActionTrigger);

        // Assert
        trigger.Should().NotBeNull();
        trigger!.Reason.Should().Be("explicit invocation");
    }

    [Test]
    public void Parse_RelevantAttribute_HeterogeneousPath_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "resource": "example_resource.upstream",
            "attribute": ["tags", "environment"]
        }
        """;

        // Act
        var attr = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.RelevantAttribute);

        // Assert
        attr.Should().NotBeNull();
        attr!.Resource.Should().Be("example_resource.upstream");
        attr.Attribute.Should().HaveCount(2);
        attr.Attribute[0].Should().Be("tags");
        attr.Attribute[1].Should().Be("environment");
    }

    [Test]
    public void Parse_RelevantAttribute_MixedPathSegments_RoundTrips()
    {
        // Arrange
        var json = """
        {
            "resource": "example_resource.array_ref",
            "attribute": ["nested", 0, "item", 2, "value"]
        }
        """;

        // Act
        var attr = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.RelevantAttribute);

        // Assert
        attr.Should().NotBeNull();
        attr!.Attribute.Should().HaveCount(5);
        attr.Attribute[0].Should().Be("nested");
        attr.Attribute[1].Should().Be(0);
        attr.Attribute[2].Should().Be("item");
        attr.Attribute[3].Should().Be(2);
        attr.Attribute[4].Should().Be("value");
    }

    [Test]
    public void Parse_RelevantAttribute_EmptyPath_ReturnsEmptyList()
    {
        // Arrange
        var json = """
        {
            "resource": "example_resource.empty",
            "attribute": []
        }
        """;

        // Act
        var attr = JsonSerializer.Deserialize(json, TfPlanJsonContext.Default.RelevantAttribute);

        // Assert
        attr.Should().NotBeNull();
        attr!.Attribute.Should().BeEmpty();
    }
}
