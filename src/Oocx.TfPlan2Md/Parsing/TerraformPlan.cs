using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Represents a Terraform plan JSON structure.
/// </summary>
public record TerraformPlan(
    [property: JsonPropertyName("format_version")] string FormatVersion,
    [property: JsonPropertyName("terraform_version")] string TerraformVersion,
    [property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange> ResourceChanges,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null
);

/// <summary>
/// Represents a resource change in the Terraform plan.
/// </summary>
public record ResourceChange(
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("module_address")] string? ModuleAddress,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("provider_name")] string ProviderName,
    [property: JsonPropertyName("change")] Change Change
);

/// <summary>
/// Represents the change details for a resource.
/// </summary>
public record Change(
    [property: JsonPropertyName("actions")] IReadOnlyList<string> Actions,
    [property: JsonPropertyName("before")] object? Before,
    [property: JsonPropertyName("after")] object? After,
    [property: JsonPropertyName("after_unknown")] object? AfterUnknown,
    [property: JsonPropertyName("before_sensitive")] object? BeforeSensitive,
    [property: JsonPropertyName("after_sensitive")] object? AfterSensitive
);
