using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Represents an invoke-mode action trigger (explicit invocation via terraform plan -invoke=...).
/// The presence of this object in the JSON indicates invoke-mode; the Reason property is a placeholder.
/// Related feature: docs/features/122-terraform-1-15-support/specification.md.
/// </summary>
public record InvokeActionTrigger(
    [property: JsonPropertyName("reason")] string? Reason = null
);
