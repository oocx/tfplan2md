using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Represents an action invocation in a Terraform 1.14+ plan.
/// Actions can be triggered by resource lifecycle events or invoked explicitly via -invoke flag.
/// Related feature: docs/features/122-terraform-1-15-support/specification.md.
/// </summary>
public record ActionInvocation(
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("provider_name")] string ProviderName,
    [property: JsonPropertyName("config_values")] JsonElement? ConfigValues = null,
    [property: JsonPropertyName("config_sensitive")] JsonElement? ConfigSensitive = null,
    [property: JsonPropertyName("config_unknown")] JsonElement? ConfigUnknown = null,
    [property: JsonPropertyName("lifecycle_action_trigger")] LifecycleActionTrigger? LifecycleActionTrigger = null,
    [property: JsonPropertyName("invoke_action_trigger")] InvokeActionTrigger? InvokeActionTrigger = null,
    [property: JsonPropertyName("status")] JsonElement? Status = null,
    [property: JsonPropertyName("diagnostics")] JsonElement? Diagnostics = null
);
