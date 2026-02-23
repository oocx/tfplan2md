# Architecture: Terraform Outputs Support

## Status

Proposed

## Context

Terraform plan JSON includes output information in two locations:
1. **`output_changes`** (root level): Contains value changes with actions, sensitivity markers, and before/after values
2. **`configuration.root_module.outputs`** (and `configuration.root_module.modules[].outputs`): Contains metadata like descriptions

The feature must:
- Parse both locations and correlate them
- Position module outputs within module sections, global outputs at the end
- Render outputs as 4-column tables (Name, Description, Sensitive, Value)
- Mask sensitive values by default (`--show-sensitive` flag to reveal)
- Apply existing display name mappings to output values automatically
- Handle computed values (`(known after apply)`)
- Support all output actions: create, update, delete, no-op

## Design Overview

### High-Level Architecture

The design follows the existing three-layer architecture:

```
┌─────────────────────────────────────────────────────────┐
│ 1. Parsing Layer                                        │
│    • TerraformPlan extended with OutputChanges          │
│    • Parse output_changes from JSON                     │
│    • Parse configuration outputs (metadata)             │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│ 2. Model Building Layer                                 │
│    • OutputChangeModel (report model)                   │
│    • ReportModelBuilder extensions                      │
│    • ModuleChangeGroup extended with Outputs property   │
│    • Value formatting via existing pipeline             │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│ 3. Rendering Layer                                      │
│    • Template extensions (_outputs.sbn partial)         │
│    • Scriban helper for output value formatting         │
│    • Sensitivity masking via existing mechanism         │
└─────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. Parsing Layer Extensions

#### 1.1 TerraformPlan Record Extension

Extend `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`:

```csharp
public record TerraformPlan(
    [property: JsonPropertyName("format_version")] string FormatVersion,
    [property: JsonPropertyName("terraform_version")] string TerraformVersion,
    [property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange> ResourceChanges,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null,
    [property: JsonPropertyName("configuration")] JsonElement? Configuration = null,
    // NEW: Parse output_changes
    [property: JsonPropertyName("output_changes")] IReadOnlyDictionary<string, OutputChange>? OutputChanges = null
);
```

**Rationale:** `output_changes` is an optional dictionary where keys are output names and values are change objects. Using `IReadOnlyDictionary<string, OutputChange>?` provides direct access by output name while maintaining immutability.

#### 1.2 OutputChange Record (New)

Create new record in `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`:

```csharp
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
    /// Gets whether the value is unknown/computed after the change.
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
```

**Rationale:** Mirrors the `Change` record structure for consistency. Sensitivity markers can be boolean or nested objects, so we use `object?` and will handle detection in the model builder.

### 2. Model Building Layer

#### 2.1 OutputChangeModel (New)

Create `src/Oocx.TfPlan2Md/MarkdownGeneration/OutputChangeModel.cs`:

```csharp
namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents an output value change for rendering in the report.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
internal class OutputChangeModel
{
    /// <summary>
    /// Gets the output name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description from configuration.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this output is marked as sensitive in the configuration.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets the primary action for this output (create, update, delete, no-op).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the output value (before or after depending on action).
    /// This is the raw value; templates will format it via helpers.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Gets whether the value is computed (known after apply).
    /// </summary>
    public bool IsComputed { get; init; }

    /// <summary>
    /// Gets whether the value should be masked (sensitive and not --show-sensitive).
    /// </summary>
    public bool IsMasked { get; init; }

    /// <summary>
    /// Gets the module address this output belongs to (empty string for root).
    /// </summary>
    public required string ModuleAddress { get; init; }
}
```

**Rationale:** This model contains all information needed for rendering. The `IsMasked` flag is pre-computed during model building based on `--show-sensitive` and sensitivity metadata, following the "mask at the boundary" pattern from ADR-009.

#### 2.2 ModuleChangeGroup Extension

Extend `src/Oocx.TfPlan2Md/MarkdownGeneration/ModuleChangeGroup.cs`:

```csharp
public class ModuleChangeGroup
{
    /// <summary>
    /// Gets the module address (e.g. "module.network.module.subnet"). Empty string represents the root module.
    /// </summary>
    public required string ModuleAddress { get; init; }

    /// <summary>
    /// Gets the list of resource changes within this module.
    /// </summary>
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }

    /// <summary>
    /// Gets the list of outputs for this module, ordered alphabetically by name.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public IReadOnlyList<OutputChangeModel> Outputs { get; init; } = Array.Empty<OutputChangeModel>();
}
```

**Rationale:** Module outputs are logically associated with their containing module. Adding an `Outputs` property to `ModuleChangeGroup` keeps the grouping consistent with resource changes.

#### 2.3 ReportModel Extension

Extend `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`:

```csharp
internal class ReportModel
{
    // ... existing properties ...

    /// <summary>
    /// Gets the global/root outputs (outputs not belonging to any module).
    /// Ordered alphabetically by output name.
    /// Related feature: docs/features/097-terraform-outputs/specification.md.
    /// </summary>
    public IReadOnlyList<OutputChangeModel> GlobalOutputs { get; init; } = Array.Empty<OutputChangeModel>();
}
```

**Rationale:** Global outputs are rendered in a separate section after all modules, so they're stored separately from module outputs.

#### 2.4 ReportModelBuilder Extensions

Create new partial file `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs`:

**Key methods:**

1. **BuildOutputModels(TerraformPlan plan) → List<OutputChangeModel>**
   - Iterate through `plan.OutputChanges`
   - For each output, extract metadata from `plan.Configuration` (description, sensitive flag)
   - Correlate output name with module address from configuration structure
   - Determine action (create, update, delete, no-op) from `actions` array
   - Select value: `after` for create/update/no-op, `before` for delete
   - Check if value is computed: `after_unknown == true`
   - Determine if masked: check sensitivity and `_showSensitive` flag
   - Return list of `OutputChangeModel`

2. **ExtractOutputMetadata(JsonElement? configuration, string outputName, string moduleAddress) → (string? description, bool sensitive)**
   - Navigate JSON to `configuration.root_module.outputs[outputName]` for root outputs
   - Navigate to `configuration.root_module.modules[...].outputs[outputName]` for module outputs
   - Extract `description` field (null if not present)
   - Extract `sensitive` field (default to false if not present)

3. **IsSensitiveValue(object? sensitivityMarker) → bool**
   - Handle boolean: `true` means sensitive
   - Handle object: check if any nested path indicates sensitivity (recursive)
   - Default to `false` for `null`

**Integration into Build() method:**

```csharp
public ReportModel Build(TerraformPlan plan)
{
    // ... existing code ...
    
    // Build output models
    var allOutputs = BuildOutputModels(plan);
    
    // Separate global vs module outputs
    var globalOutputs = allOutputs
        .Where(o => o.ModuleAddress == string.Empty)
        .OrderBy(o => o.Name, StringComparer.Ordinal)
        .ToList();
    
    // Group module outputs by module address
    var outputsByModule = allOutputs
        .Where(o => o.ModuleAddress != string.Empty)
        .GroupBy(o => o.ModuleAddress)
        .ToDictionary(g => g.Key, g => g.OrderBy(o => o.Name, StringComparer.Ordinal).ToList());
    
    // Enhance ModuleChangeGroup with outputs
    var moduleGroups = displayChanges
        .GroupBy(c => c.ModuleAddress ?? string.Empty)
        .Select(g => new ModuleChangeGroup
        {
            ModuleAddress = g.Key,
            Changes = g.ToList(),
            Outputs = outputsByModule.TryGetValue(g.Key, out var outputs) 
                ? outputs 
                : Array.Empty<OutputChangeModel>()
        })
        .ToList();
    
    // Handle modules with ONLY outputs (no resource changes)
    foreach (var (moduleAddress, outputs) in outputsByModule)
    {
        if (!moduleGroups.Any(m => m.ModuleAddress == moduleAddress))
        {
            moduleGroups.Add(new ModuleChangeGroup
            {
                ModuleAddress = moduleAddress,
                Changes = Array.Empty<ResourceChangeModel>(),
                Outputs = outputs
            });
        }
    }
    
    return new ReportModel
    {
        // ... existing properties ...
        GlobalOutputs = globalOutputs
    };
}
```

**Rationale:** This approach:
- Correlates output_changes with configuration metadata by output name and module path
- Pre-computes masking decision at the model boundary (defense in depth, per ADR-009)
- Handles edge case: modules with only outputs (no resource changes) are still rendered
- Maintains alphabetical ordering as specified

### 3. Rendering Layer

#### 3.1 Template Structure

Create new partial template `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_outputs.sbn`:

```scriban
{{~ # Render outputs table for a list of output models
    # Parameters:
    #   - outputs: IReadOnlyList<OutputChangeModel>
    #   - header_level: String like "####" for module outputs or "##" for global
~}}
{{ if outputs.size > 0 }}
{{ header_level }} Outputs

| Name | Description | Sensitive | Value |
|------|-------------|-----------|-------|
{{ for output in outputs }}
| {{ format_code_inline output.name }} | {{ if output.description }}{{ output.description | escape_markdown }}{{ else }}-{{ end }} | {{ if output.is_sensitive }}Yes{{ else }}-{{ end }} | {{ format_output_value output }} |
{{ end }}
{{ end }}
```

**Rationale:** Reusable partial for rendering output tables. The `format_output_value` helper handles all value formatting logic.

#### 3.2 Template Integration

**Module outputs** - Modify `default.sbn`:

```scriban
{{ for module in module_changes }}
{{ if for.index > 0 }}
---

{{ end }}
### 📦 Module: {{ if module.module_address && module.module_address != "" }}{{ format_code_table(module.module_address) }}{{ else }}root{{ end }}

{{ for change in module.changes }}
{{ include (resolve_template change.type) }}

{{ end }}
{{~ # Add module outputs after resource changes ~}}
{{ include "_outputs.sbn" header_level:"####" outputs:module.outputs }}
{{ end }}
```

**Global outputs** - Modify `default.sbn` (after refactoring operations section):

```scriban
{{~ # Add global outputs section at the end ~}}
{{ include "_outputs.sbn" header_level:"##" outputs:global_outputs }}
```

**Rationale:** Module outputs appear within the module section (after resources), global outputs appear at the end. Header level differs to match document hierarchy.

#### 3.3 Scriban Helper Functions

Add to `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers/ValueFormatting.cs`:

```csharp
/// <summary>
/// Formats an output value for display in the outputs table.
/// Handles masking, computed values, and applies display name mappings.
/// Related feature: docs/features/097-terraform-outputs/specification.md.
/// </summary>
/// <param name="output">The output change model to format.</param>
/// <returns>Formatted markdown string (code-formatted or plain text).</returns>
public static string FormatOutputValue(OutputChangeModel output)
{
    // Masked values: plain text (not code-formatted)
    if (output.IsMasked)
    {
        return "(sensitive value)";
    }
    
    // Computed values: plain text (not code-formatted)
    if (output.IsComputed)
    {
        return "(known after apply)";
    }
    
    // Format the value through the existing value formatting pipeline
    // This automatically applies display name mappings
    var formattedValue = FormatValueWithDisplayNameMappings(output.Value);
    
    // Return code-formatted
    return FormatCodeInline(formattedValue);
}

/// <summary>
/// Applies existing value formatters and display name mappings to a value.
/// This reuses the existing ValueFormatterRegistry pipeline.
/// </summary>
private static string FormatValueWithDisplayNameMappings(object? value)
{
    // Convert value to JSON string representation
    var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
    
    // Try to apply value formatters (Azure resource ID formatting, etc.)
    if (_valueFormatterRegistry is not null)
    {
        var context = new ServiceResolutionContext(
            providerName: null,  // Outputs don't have provider context
            resourceType: null,
            attributeName: null,
            value: jsonValue
        );
        
        var formatted = _valueFormatterRegistry.TryFormat(context);
        if (formatted is not null)
        {
            return formatted;
        }
    }
    
    // Fallback: return JSON representation
    return jsonValue;
}
```

**Rationale:** This helper:
- Handles masking and computed values with plain text (not code-formatted) as specified
- Leverages existing `ValueFormatterRegistry` to automatically apply display name mappings
- Follows the same formatting pipeline used for resource attributes (consistency)

### 4. CLI Flag

#### 4.1 CliOptions Extension

The `--show-sensitive` flag already exists in `src/Oocx.TfPlan2Md/CLI/CliOptions.cs` (added for Issue 098). No changes needed.

The existing flag is passed through to `ReportModelBuilder` and used to set `ReportModel.ShowSensitive`, which is then used when building `OutputChangeModel` to set the `IsMasked` flag.

**Rationale:** Reusing the existing flag maintains consistency. Output sensitivity masking follows the same pattern as resource attribute masking.

## Architecture Decisions

### Decision 1: Parse Outputs Eagerly vs On-Demand

**Chosen:** Parse `output_changes` eagerly during `TerraformPlanParser.Parse()`.

**Rationale:** 
- Consistent with how `resource_changes` are parsed
- Simpler error handling (parse errors detected early)
- No performance concern (outputs list is typically small)

### Decision 2: Correlation Strategy

**Chosen:** Correlate by output name + module address during model building.

**Alternatives considered:**
- Build a map during parsing: More complex, couples parsing to model logic
- Lazy correlation in templates: Error-prone, harder to test

**Rationale:** Model building layer is responsible for transforming parsed data into renderable models. This is the natural place for correlation logic.

### Decision 3: Module Address Resolution

**Chosen:** Derive module address from the `configuration` structure hierarchy.

The configuration JSON has this structure:
```json
{
  "configuration": {
    "root_module": {
      "outputs": { ... },
      "modules": [
        {
          "address": "module.database",
          "outputs": { ... }
        }
      ]
    }
  }
}
```

We navigate this structure to find which module each output belongs to.

**Rationale:** Module address is not stored directly in `output_changes`. The configuration structure provides the definitive source.

### Decision 4: Sensitivity Detection Hierarchy

**Chosen:** Use the following precedence:
1. `after_sensitive` (for create/update/no-op actions)
2. `before_sensitive` (for delete actions)
3. `configuration.*.outputs[name].sensitive` (fallback)
4. Default to `false`

**Rationale:** `output_changes` sensitivity is the runtime truth. Configuration sensitivity is the declared intent. Runtime truth takes precedence.

### Decision 5: Value Formatting Pipeline

**Chosen:** Reuse existing `ValueFormatterRegistry` for output values.

**Rationale:** 
- Automatic display name mappings (Azure resource IDs, principals, etc.)
- Consistent formatting between resource attributes and outputs
- No code duplication
- Same extension mechanism for providers

### Decision 6: Handling Modules with Only Outputs

**Chosen:** Create `ModuleChangeGroup` entries even when a module has no resource changes.

**Rationale:** Without this, outputs from modules with no resource changes would be invisible. This edge case is rare but must be handled.

### Decision 7: No Summary Counts

**Chosen:** Do not add output counts to the Summary section.

**Rationale:** 
- Specification explicitly states "Out of Scope: Module output summary counts"
- Outputs are informational, not changes (unlike resources)
- Simpler summary table

## Implementation Guidance

### File Changes

**New Files:**
1. `src/Oocx.TfPlan2Md/MarkdownGeneration/OutputChangeModel.cs`
2. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs`
3. `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_outputs.sbn`

**Modified Files:**
1. `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` - Add `OutputChanges` property and `OutputChange` record
2. `src/Oocx.TfPlan2Md/MarkdownGeneration/ModuleChangeGroup.cs` - Add `Outputs` property
3. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` - Add `GlobalOutputs` property
4. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` - Integrate output building
5. `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn` - Add output rendering
6. `src/Oocx.TfPlan2Md/MarkdownGeneration/ScribanHelpers/ValueFormatting.cs` - Add `format_output_value` helper

### Testing Strategy

**Unit Tests:**
- `OutputChangeModelBuilderTests.cs` - Test output model building logic
- `OutputMetadataExtractionTests.cs` - Test configuration correlation
- `OutputSensitivityDetectionTests.cs` - Test sensitivity precedence rules
- `OutputValueFormattingTests.cs` - Test value formatting and masking

**Integration Tests:**
- Module outputs appear in correct module sections
- Global outputs appear at the end
- `--show-sensitive` flag reveals masked values
- Computed values show "(known after apply)"
- Alphabetical ordering is maintained

**Snapshot Tests:**
- Full plan with global and module outputs
- Outputs with display name mappings applied
- Sensitive outputs masked/unmasked
- Computed outputs
- All output actions (create, update, delete, no-op)

### Edge Cases to Handle

1. **No outputs**: Entire output section omitted (both module and global)
2. **Module with only outputs**: Create ModuleChangeGroup for rendering
3. **Output without description**: Show `-` in description column
4. **Nested sensitive values**: Detect sensitivity in nested objects
5. **Complex output values**: Arrays, objects (format as JSON)
6. **Missing configuration metadata**: Graceful degradation (no description, assume not sensitive)
7. **Computed sensitive values**: Show "(sensitive value)" not "(known after apply)"

## Open Questions Resolved

### Question 1: Data Model Extension
**Answer:** Extend `TerraformPlan` with optional `OutputChanges` property. Parse eagerly.

### Question 2: Output Metadata Correlation
**Answer:** Correlate by output name + module address during model building using configuration structure navigation.

### Question 3: Module Output Parsing
**Answer:** Build complete output list during model building, then group by module address. Handle modules with only outputs.

### Question 4: Value Rendering Pipeline
**Answer:** Reuse existing `ValueFormatterRegistry` by passing output value through the same pipeline as resource attributes.

### Question 5: Sensitivity Detection
**Answer:** Use `after_sensitive`/`before_sensitive` from `output_changes` as primary source, fall back to configuration if needed. Pre-compute `IsMasked` flag during model building.

### Question 6: Update Actions
**Answer:** Show only `after` value for updates in current implementation (before → after diff is explicitly out of scope as a future enhancement).

## Security Considerations

1. **Masked by Default**: Sensitive output values are masked unless `--show-sensitive` is provided
2. **Pre-computed Masking**: `IsMasked` flag is set during model building (defense in depth, per ADR-009)
3. **Consistent with Resources**: Uses the same masking mechanism as resource attributes
4. **Template Safety**: Templates receive `IsMasked` flag and cannot accidentally leak secrets

## Performance Considerations

1. **Small Data Set**: Typical plans have few outputs (<50), so performance is not a concern
2. **Single Pass**: Build all output models in one pass through `output_changes`
3. **Configuration Parsing**: Parse configuration once and cache during correlation
4. **No Additional API Calls**: All data available in plan JSON

## Backward Compatibility

- **Templates**: Custom templates without output support will continue to work (outputs sections simply won't render)
- **CLI**: No breaking changes (new flag is optional)
- **JSON Schema**: Uses existing Terraform plan format (no custom extensions)

## Future Enhancements (Out of Scope)

1. **Before/After Diff for Updates**: Show `before → after` in a 2-row format
2. **Output Dependency Visualization**: Show which resources an output depends on (from `expression.references`)
3. **Output Grouping**: Group outputs by type or category
4. **Output Value History**: Show historical values across plan versions
5. **Filter by Sensitivity**: CLI flags to show only sensitive or non-sensitive outputs
