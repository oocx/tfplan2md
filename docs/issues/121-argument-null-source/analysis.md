# Issue: ArgumentNullException When Parsing Plan Without `resource_changes`

## Problem Description

When running `tfplan2md --output plan.md tfplan.json` with a Terraform plan JSON that either:
- **omits** the `resource_changes` field entirely, or
- **explicitly sets** `"resource_changes": null`

the tool crashes with:

```
Unexpected error: ArgumentNull_Generic Arg_ParamName_Name, source
```

The message format `ArgumentNull_Generic Arg_ParamName_Name, source` is the .NET AOT/trimmed-build representation of `ArgumentNullException` where `source` is the parameter name. This is the standard exception thrown by LINQ extension methods (e.g., `Enumerable.Select`, `Enumerable.Where`) when passed a null collection for the `source` argument.

## Steps to Reproduce

1. Create a `tfplan.json` that omits the `resource_changes` field:
   ```json
   {
     "format_version": "1.2",
     "terraform_version": "1.9.0",
     "output_changes": {
       "my_output": {
         "actions": ["create"],
         "after": "value",
         "after_unknown": false,
         "before": null,
         "before_sensitive": false,
         "after_sensitive": false
       }
     }
   }
   ```
2. Run: `tfplan2md --output plan.md tfplan.json`
3. Observe: `Unexpected error: ArgumentNull_Generic Arg_ParamName_Name, source`

Real-world trigger: A Terraform plan that only changes outputs (no infrastructure resources) may not emit a `resource_changes` key in the JSON, or a plan file that uses a newer/unusual Terraform format.

## Expected Behavior

`tfplan2md` should handle a missing or null `resource_changes` field gracefully, treating it as an empty list and producing a report with only output changes (or an empty resource-change section).

## Actual Behavior

The tool crashes with an unhandled `ArgumentNullException` propagated as the generic "Unexpected error" message from `ProgramEntry.cs:75`.

## Root Cause Analysis

### Affected Components

- **Primary**: `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs#L142`
- **Model**: `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs#L12`
- **Secondary**: `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs#L43` (`Change.Actions`)
- **Secondary**: `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs#L175` (`OutputChange.Actions`)
- **Error surface**: `src/Oocx.TfPlan2Md/ProgramEntry.cs#L75`

### What's Broken

**Primary bug — null `ResourceChanges`:**

`TerraformPlan` is a C# record with a primary constructor:

```csharp
// TerraformPlan.cs:12
public record TerraformPlan(
    ...
    [property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange> ResourceChanges,
    ...
);
```

Although `ResourceChanges` is declared as a non-nullable `IReadOnlyList<ResourceChange>`, System.Text.Json's source-generated deserializer does **not** enforce non-nullability. When the `resource_changes` key is absent from the JSON (or is `null`), the deserializer sets `ResourceChanges` to `null` at runtime (the .NET nullability annotation is not enforced by the deserializer).

The crash occurs in `ResourceChangeStage.Build()`:

```csharp
// ResourceChangeStage.cs:142-144
return plan.ResourceChanges              // null when field is absent
    .Select(resourceChange => BuildResourceChangeModel(...))
    .ToList();
```

`Enumerable.Select(IEnumerable<T> source, ...)` throws `ArgumentNullException("source")` when `source` is null. Since this exception is neither a `TerraformPlanParseException` nor a `MarkdownRenderException`, it falls through to the catch-all handler in `ProgramEntry.cs:73-77`, which prints `Unexpected error: <ex.Message>`.

In the AOT/trimmed build (per `docs/features/037-aot-trimmed-image/`), exception messages use .NET resource-string keys rather than resolved text, producing the literal `ArgumentNull_Generic Arg_ParamName_Name, source` instead of the human-readable `"Value cannot be null. (Parameter 'source')"`.

**Secondary bug — null `Change.Actions`:**

```csharp
// TerraformPlan.cs:43
public IReadOnlyList<string> Actions { get; init; }
```

If a resource_change entry has no `actions` field in the JSON, `Actions` will be null. In `DetermineAction`:

```csharp
// ResourceChangeStage.Helpers.cs:136
if (actions.Count == 0)  // → NullReferenceException if actions is null
```

This throws `NullReferenceException`, not `ArgumentNullException`. However, it would surface the same way via the generic error handler.

**Secondary bug — null `OutputChange.Actions`:**

```csharp
// TerraformPlan.cs:175
public IReadOnlyList<string> Actions { get; init; }
```

In `ReportModelBuilder.Outputs.cs:42`:
```csharp
var action = outputChange.Actions.Count > 0 ? outputChange.Actions[0] : "no-op";
// → NullReferenceException if Actions is null
```

**Note:** `ReportModelBuilder.Outputs.cs:328` already has a null check for `plan.ResourceChanges` (in `ResolveOutputProviderName`), confirming the developers were aware this could be null — but the fix was not applied at the primary crash site in `ResourceChangeStage.cs`.

### Why It Happened

The `TerraformPlan` record was designed assuming the `resource_changes` key is always present in Terraform plan JSON. Most common plans include it, so this edge case was not encountered in testing. The existing test data (`TestData/empty-plan.json`) uses `"resource_changes": []` (empty array, not missing), which does not trigger the bug.

A real-world Terraform plan can omit `resource_changes` when:
- The plan only modifies output values (no infrastructure resource changes)
- A provider generates a plan format that omits the field when there are no changes
- A partially generated or non-standard plan file is used

## Suggested Fix Approach

### Fix 1 (Primary — Most Important): Guard null `ResourceChanges` in `ResourceChangeStage.Build()`

In `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.cs`, change line 142:

```csharp
// Before:
return plan.ResourceChanges
    .Select(resourceChange => BuildResourceChangeModel(resourceChange, configurationReferencesByAddress))
    .ToList();

// After:
return (plan.ResourceChanges ?? [])
    .Select(resourceChange => BuildResourceChangeModel(resourceChange, configurationReferencesByAddress))
    .ToList();
```

### Fix 2 (Alternative / More Defensive): Make `ResourceChanges` nullable in the model

In `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`, change the `ResourceChanges` parameter to nullable with a default:

```csharp
// Before:
[property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange> ResourceChanges,

// After:
[property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange>? ResourceChanges = null,
```

This makes the intent explicit and allows the type system to flag all consuming code. This approach requires updating all callers to handle null (use `?? []`).

### Fix 3 (Secondary): Guard null `Change.Actions`

In `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.Helpers.cs`, change `DetermineAction`:

```csharp
// Before:
private static string DetermineAction(IReadOnlyList<string> actions)
{
    if (actions.Count == 0)
    ...

// After:
private static string DetermineAction(IReadOnlyList<string>? actions)
{
    if (actions is null || actions.Count == 0)
    ...
```

Or guard at the call site in `ResourceChangeStage.cs:157`:
```csharp
var action = DetermineAction(resourceChange.Change.Actions ?? []);
```

Similarly update `Change.Actions` and `OutputChange.Actions` to be nullable (or default to `[]`).

### Recommended Approach

Apply **Fix 1** for the immediate crash, and additionally apply **Fix 3** for defensive null handling of `Actions`. Optionally apply **Fix 2** to make the model's nullable contract explicit for better IDE/compiler support.

## Related Tests

Tests that should be added after the fix:
- [ ] `Parse_PlanWithMissingResourceChanges_DoesNotThrow` — parse and render a plan JSON without `resource_changes` key
- [ ] `Parse_PlanWithNullResourceChanges_DoesNotThrow` — parse and render a plan JSON with `"resource_changes": null`
- [ ] `Build_PlanWithNullResourceChanges_ReturnsEmptyList` — unit test for `ResourceChangeStage.Build()` with null `ResourceChanges`

## Additional Context

- **Error wrapper**: `src/Oocx.TfPlan2Md/ProgramEntry.cs#L73-77` — the generic catch block prints `ex.Message` as "Unexpected error"
- **Existing partial null check**: `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs#L328` — `plan.ResourceChanges is null` check already exists in `ResolveOutputProviderName`, confirming the null case was anticipated elsewhere but not fixed universally
- **AOT build**: Error message format `ArgumentNull_Generic Arg_ParamName_Name, source` is the trimmed/AOT representation (resource string key not resolved), per `docs/features/037-aot-trimmed-image/`
- **Version affected**: 1.36.0 (bug report), likely present in earlier versions
- **Related test data**: `TestData/empty-plan.json` uses `"resource_changes": []` (not missing), so it does not reproduce the bug
