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
public record Change
{
    /// <summary>
    /// The ordered list of actions applied to the resource.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("actions")]
    public IReadOnlyList<string> Actions { get; init; }

    /// <summary>
    /// Optional state before the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("before")]
    public object? Before { get; init; }

    /// <summary>
    /// Optional state after the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("after")]
    public object? After { get; init; }

    /// <summary>
    /// Attributes with unknown values after the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("after_unknown")]
    public object? AfterUnknown { get; init; }

    /// <summary>
    /// Sensitive values before the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("before_sensitive")]
    public object? BeforeSensitive { get; init; }

    /// <summary>
    /// Sensitive values after the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("after_sensitive")]
    public object? AfterSensitive { get; init; }

    /// <summary>
    /// Paths that require replacement due to the change.
    /// Related feature: docs/spec.md.
    /// </summary>
    [JsonPropertyName("replace_paths")]
    [JsonConverter(typeof(ReplacePathsConverter))]
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> record for deserialization.
    /// Related feature: docs/spec.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the resource.</param>
    /// <param name="before">Optional state before the change.</param>
    /// <param name="after">Optional state after the change.</param>
    /// <param name="afterUnknown">Attributes with unknown values after the change.</param>
    /// <param name="beforeSensitive">Sensitive values before the change.</param>
    /// <param name="afterSensitive">Sensitive values after the change.</param>
    /// <param name="replacePaths">Paths that require replacement due to the change.</param>
    [JsonConstructor]
    public Change(
        IReadOnlyList<string> actions,
        object? before,
        object? after,
        object? afterUnknown,
        object? beforeSensitive,
        object? afterSensitive,
        IReadOnlyList<IReadOnlyList<object>>? replacePaths = null)
    {
        Actions = actions;
        Before = before;
        After = after;
        AfterUnknown = afterUnknown;
        BeforeSensitive = beforeSensitive;
        AfterSensitive = afterSensitive;
        ReplacePaths = replacePaths;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> record with only actions specified.
    /// Related feature: docs/spec.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the resource.</param>
    public Change(IReadOnlyList<string> actions)
        : this(actions, null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> record with before/after state.
    /// Related feature: docs/spec.md.
    /// </summary>
    /// <param name="actions">The ordered list of actions applied to the resource.</param>
    /// <param name="before">Optional state before the change.</param>
    /// <param name="after">Optional state after the change.</param>
    public Change(IReadOnlyList<string> actions, object? before, object? after)
        : this(actions, before, after, null, null, null, null)
    {
    }
}
