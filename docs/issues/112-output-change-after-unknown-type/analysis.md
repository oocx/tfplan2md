# Issue: OutputChange `after_unknown` Type Mismatch — Parse Failure for Non-Boolean Values

## Problem Description

`tfplan2md` fails to parse a Terraform plan JSON when any `output_changes[*].after_unknown`
value is not a plain `true`/`false` boolean. Terraform itself can emit a JSON object (`{}`)
or other non-boolean values for this field in some plan scenarios (e.g., when an output
depends on a computed resource that produces an object type). The C# model hard-codes the
field as `bool`, so `System.Text.Json` throws a `DeserializeUnableToConvertValue` exception.

## Steps to Reproduce

1. Obtain or craft a Terraform plan JSON that contains an output with a non-boolean
   `after_unknown`, for example:

   ```json
   {
     "output_changes": {
       "cross_direct_wif": {
         "actions": ["create"],
         "before": null,
         "after": null,
         "after_unknown": {},
         "before_sensitive": false,
         "after_sensitive": false
       }
     }
   }
   ```

2. Run `tfplan2md` against that plan file.

3. Observe the error:

   ```
   Error: Failed to parse Terraform plan JSON: DeserializeUnableToConvertValue,
   Oocx.TfPlan2Md.Parsing.OutputChange
   Path: $.output_changes.cross_direct_wif.after_unknown | LineNumber: 479 | BytePositionInLine: 24.
   ```

## Expected Behavior

`tfplan2md` should parse the plan successfully and treat a non-`true` `after_unknown` value
(including an empty or non-empty object like `{}` or `{"key": true}`) as **not simply fully
computed at the top level**, behaving the same way it handles non-boolean `after_unknown`
for resource changes (using `AfterUnknownHelper.IsWholeResourceUnknownAfterApply`).

## Actual Behavior

Deserialization fails immediately with a type-conversion exception because `OutputChange`
declares `AfterUnknown` as `bool`, and `System.Text.Json` cannot convert the JSON object
token `{` to `bool`.

## Root Cause Analysis

### Affected Components

| File | Location | Issue |
|------|----------|-------|
| `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` | Lines 195–196, 227–228 | `OutputChange.AfterUnknown` is typed `bool`; constructor parameter is `bool afterUnknown = false` |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs` | Line 47 | `var isComputed = outputChange.AfterUnknown;` reads directly as `bool` |
| `src/tests/Oocx.TfPlan2Md.TUnit/Parsing/TerraformPlanParserOutputTests.cs` | Line 104 | `computedOutput.AfterUnknown.Should().BeTrue()` will no longer compile after type change |

### What's Broken

**`TerraformPlan.cs` — `OutputChange` record (lines 194–238)**

```csharp
// ❌ BROKEN — forces bool deserialization; crashes on any non-boolean JSON token
[JsonPropertyName("after_unknown")]
public bool AfterUnknown { get; init; }          // line 196

// ❌ BROKEN — constructor parameter
public OutputChange(
    IReadOnlyList<string> actions,
    object? before = null,
    object? after = null,
    bool afterUnknown = false,                    // line 227  ← should be object? afterUnknown = null
    object? beforeSensitive = null,
    object? afterSensitive = null)
```

**For comparison, `Change` (same file, lines 63–64) already handles this correctly:**

```csharp
// ✅ CORRECT — accepts any JSON token
[JsonPropertyName("after_unknown")]
public object? AfterUnknown { get; init; }
```

### Why It Happened

When the `OutputChange` model was introduced (feature #097), it was assumed that
`output_changes[*].after_unknown` is always a plain boolean. The Terraform JSON format
[documentation](https://developer.hashicorp.com/terraform/internals/json-format#change-representation)
states that `after_unknown` is "true if the attribute's new value is not yet known" but does
not restrict the type to scalar boolean at the output level — Terraform can emit an object
structure mirroring the output's own type when the output value is itself a complex type
(object or map). The `Change` model was already fixed to use `object?` (likely for the same
reason for resource changes), but `OutputChange` was never updated.

## Suggested Fix Approach

All changes are minimal and localized to three files:

### 1. `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`

Change `OutputChange.AfterUnknown` from `bool` to `object?`:

```csharp
// Before (line 196):
public bool AfterUnknown { get; init; }

// After:
public object? AfterUnknown { get; init; }
```

Change the constructor parameter (line 227):

```csharp
// Before:
bool afterUnknown = false,

// After:
object? afterUnknown = null,
```

Update the constructor body assignment (line 234 — no change needed, assignment is the same)
and XML doc comment (line 219) to reflect the new type:

```csharp
// Before:
/// <param name="afterUnknown">Whether the value is unknown/computed after the change.</param>

// After:
/// <param name="afterUnknown">Whether the value is unknown/computed after the change (boolean true, or a nested object for complex output types).</param>
```

### 2. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs`

Update the `isComputed` derivation (line 47) to use `AfterUnknownHelper` instead of casting directly:

```csharp
// Before (line 47):
var isComputed = outputChange.AfterUnknown;

// After:
var isComputed = AfterUnknownHelper.IsWholeResourceUnknownAfterApply(outputChange.AfterUnknown);
```

This reuses the existing helper that already handles `object?`, `JsonElement`, and `bool`
values (see `AfterUnknownHelper.TryGetJsonElement`), so no changes to `AfterUnknownHelper`
are needed.

Note: `using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;` is already present in
`ReportModelBuilder.Outputs.cs` (line 4 imports it transitively via the partial class
— verify the using directive is present or add it).

### 3. `src/tests/Oocx.TfPlan2Md.TUnit/Parsing/TerraformPlanParserOutputTests.cs`

Update the existing computed-flag test (line 104) to assert on the typed value after the
`bool`→`object?` change:

```csharp
// Before (line 104):
computedOutput.AfterUnknown.Should().BeTrue();

// After (asserting on the JsonElement's kind, consistent with other object? fields):
((System.Text.Json.JsonElement)computedOutput.AfterUnknown!).ValueKind
    .Should().Be(System.Text.Json.JsonValueKind.True);
```

**Add a new regression test** and corresponding test data file for the non-boolean scenario:

- **New test data file**: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/outputs-computed-object-plan.json`
  — A minimal plan where `after_unknown` is `{}` (empty object).

- **New test method** in `TerraformPlanParserOutputTests`:

  ```csharp
  [Test]
  public void Parse_PlanWithOutputs_ParsesComputedFlagAsObject()
  {
      // Arrange
      var json = File.ReadAllText("TestData/outputs-computed-object-plan.json");

      // Act
      var plan = _parser.Parse(json);

      // Assert — should NOT throw; AfterUnknown should be a JsonElement with Object kind
      var output = plan.OutputChanges!["cross_direct_wif"];
      ((JsonElement)output.AfterUnknown!).ValueKind.Should().Be(JsonValueKind.Object);
  }
  ```

- **Add a `ReportModelBuilder` integration test** (or update `MarkdownFuzzTests`) to verify
  that a plan with `after_unknown: {}` renders without throwing, and that `IsComputed` is
  `false` (since the object is not a top-level `true`).

## Related Tests

Tests that should pass after the fix:

- [ ] `TerraformPlanParserOutputTests.Parse_PlanWithOutputs_ParsesComputedFlag` (updated assertion)
- [ ] `TerraformPlanParserOutputTests.Parse_PlanWithOutputs_ParsesComputedFlagAsObject` (new regression test)
- [ ] All existing output-related markdown-generation tests in `MarkdownFuzzTests` / `TerraformShowRendererTests`
- [ ] New integration test verifying `IsComputed = false` when `after_unknown` is `{}`
- [ ] Full test suite (no regressions)

## Additional Context

- **`AfterUnknownHelper`** (`src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/AfterUnknownHelper.cs`):
  Already handles all needed cases via `TryGetJsonElement` (lines 258–275). No changes needed.
- **`OutputChangeModel.IsComputed`** (`src/Oocx.TfPlan2Md/MarkdownGeneration/OutputChangeModel.cs`,
  line 57): Remains `bool` — the fix happens in the builder, not the model.
- **`AotScriptObjectMapper.cs`** (line 207): `obj["is_computed"] = output.IsComputed;` — no
  change needed; it reads from `OutputChangeModel.IsComputed` which stays `bool`.
- Terraform JSON format reference:
  https://developer.hashicorp.com/terraform/internals/json-format#change-representation
- Similar fix was already applied for `Change.AfterUnknown` in `TerraformPlan.cs` (line 64)
  — the resource-change model already uses `object?`.
