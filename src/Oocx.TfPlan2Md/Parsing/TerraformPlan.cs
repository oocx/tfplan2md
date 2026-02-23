using System.Text.Json;
using System.Text.Json.Serialization;

namespace Oocx.TfPlan2Md.Parsing;

/// <summary>
/// Represents a Terraform plan JSON structure.
/// </summary>
public record TerraformPlan(
    [property: JsonPropertyName("format_version")] string FormatVersion,
    [property: JsonPropertyName("terraform_version")] string TerraformVersion,
    [property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange> ResourceChanges,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null,
    [property: JsonPropertyName("configuration")] JsonElement? Configuration = null,
    [property: JsonPropertyName("output_changes")] IReadOnlyDictionary<string, OutputChange>? OutputChanges = null
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
    [property: JsonPropertyName("change")] Change Change,
    [property: JsonPropertyName("action_reason")] string? ActionReason = null,
    [property: JsonPropertyName("previous_address")] string? PreviousAddress = null
);

/// <summary>
/// Represents the change details for a resource.
/// </summary>
public record Change
{
    /// <summary>
    /// Gets the the ordered list of actions applied to the resource.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("actions")]
    public IReadOnlyList<string> Actions { get; init; }

    /// <summary>
    /// Gets the optional state before the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("before")]
    public object? Before { get; init; }

    /// <summary>
    /// Gets the optional state after the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("after")]
    public object? After { get; init; }

    /// <summary>
    /// Gets the Attributes with unknown values after the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("after_unknown")]
    public object? AfterUnknown { get; init; }

    /// <summary>
    /// Gets the Sensitive values before the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("before_sensitive")]
    public object? BeforeSensitive { get; init; }

    /// <summary>
    /// Gets the Sensitive values after the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("after_sensitive")]
    public object? AfterSensitive { get; init; }

    /// <summary>
    /// Gets the Paths that require replacement due to the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("replace_paths")]
    [JsonConverter(typeof(ReplacePathsConverter))]
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; init; }

    /// <summary>
    /// Gets the import metadata when a resource is being imported.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    [JsonPropertyName("importing")]
    public Importing? Importing { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> class for deserialization.
    /// Related feature: docs/spec.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the resource.</param>
    /// <param name="before">Optional state before the change.</param>
    /// <param name="after">Optional state after the change.</param>
    /// <param name="afterUnknown">Attributes with unknown values after the change.</param>
    /// <param name="beforeSensitive">Sensitive values before the change.</param>
    /// <param name="afterSensitive">Sensitive values after the change.</param>
    /// <param name="replacePaths">Paths that require replacement due to the change.</param>
    /// <param name="importing">Import metadata for resources managed via import blocks.</param>
    [JsonConstructor]
    public Change(
        IReadOnlyList<string> actions,
        object? before,
        object? after,
        object? afterUnknown,
        object? beforeSensitive,
        object? afterSensitive,
        IReadOnlyList<IReadOnlyList<object>>? replacePaths = null,
        Importing? importing = null)
    {
        Actions = actions;
        Before = before;
        After = after;
        AfterUnknown = afterUnknown;
        BeforeSensitive = beforeSensitive;
        AfterSensitive = afterSensitive;
        ReplacePaths = replacePaths;
        Importing = importing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> class with only actions specified.
    /// Related feature: docs/spec.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the resource.</param>
    public Change(IReadOnlyList<string> actions)
        : this(actions, null, null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> class with before/after state.
    /// Related feature: docs/spec.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the resource.</param>
    /// <param name="before">Optional state before the change.</param>
    /// <param name="after">Optional state after the change.</param>
    public Change(IReadOnlyList<string> actions, object? before, object? after)
        : this(actions, before, after, null, null, null, null, null)
    {
    }
}

/// <summary>
/// Represents import metadata for a resource change.
/// </summary>
public record Importing
{
    /// <summary>
    /// Gets the import identifier used by Terraform.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

/// <summary>
/// Represents an output value change in the Terraform plan.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
public record OutputChange
{
    /// <summary>
    /// Gets the ordered list of actions applied to the output.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    [JsonPropertyName("actions")]
    public IReadOnlyList<string> Actions { get; init; }

    /// <summary>
    /// Gets the optional value before the change.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    [JsonPropertyName("before")]
    public object? Before { get; init; }

    /// <summary>
    /// Gets the optional value after the change.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    [JsonPropertyName("after")]
    public object? After { get; init; }

    /// <summary>
    /// Gets a value indicating whether the value is unknown/computed after the change.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    [JsonPropertyName("after_unknown")]
    public bool AfterUnknown { get; init; }

    /// <summary>
    /// Gets whether the value was sensitive before the change.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    [JsonPropertyName("before_sensitive")]
    public object? BeforeSensitive { get; init; }

    /// <summary>
    /// Gets whether the value is sensitive after the change.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    [JsonPropertyName("after_sensitive")]
    public object? AfterSensitive { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputChange"/> class.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the output.</param>
    /// <param name="before">Optional value before the change.</param>
    /// <param name="after">Optional value after the change.</param>
    /// <param name="afterUnknown">Whether the value is unknown/computed after the change.</param>
    /// <param name="beforeSensitive">Whether the value was sensitive before the change.</param>
    /// <param name="afterSensitive">Whether the value is sensitive after the change.</param>
    [JsonConstructor]
    public OutputChange(
        IReadOnlyList<string> actions,
        object? before = null,
        object? after = null,
        bool afterUnknown = false,
        object? beforeSensitive = null,
        object? afterSensitive = null)
    {
        Actions = actions;
        Before = before;
        After = after;
        AfterUnknown = afterUnknown;
        BeforeSensitive = beforeSensitive;
        AfterSensitive = afterSensitive;
    }
}
