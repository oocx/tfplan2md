using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Represents a lifecycle action trigger that ties an action invocation to a resource change event.
/// Related feature: docs/features/122-terraform-1-15-support/specification.md.
/// </summary>
public record LifecycleActionTrigger(
    [property: JsonPropertyName("triggering_resource_address")] string TriggeringResourceAddress,
    [property: JsonPropertyName("action_trigger_event")] string ActionTriggerEvent,
    [property: JsonPropertyName("action_trigger_block_index")] int? ActionTriggerBlockIndex = null,
    [property: JsonPropertyName("actions_list_index")] int? ActionsListIndex = null
);
