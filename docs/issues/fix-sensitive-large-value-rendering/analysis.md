# Issue: Sensitive + Large Output Values Leak Secrets, and Large JSON Values Are Not Pretty-Printed

## Problem Description

Two bugs exist in the rendering of large output values (values classified as "large enough to show below the table") in `ReportRenderer.cs`:

1. **Bug 1 (Critical Security):** When an output value is **both** sensitive (masked) **and** a large value, the table cell correctly shows `_(see below)_` but the below-table code block renders the actual secret verbatim. Sensitive values are therefore leaked in the generated markdown.

2. **Bug 2 (Formatting):** When a large output value is a JSON object or array, the below-table code block renders it as a compact single-line string instead of pretty-printing it with indentation and line breaks.

---

## Steps to Reproduce

### Bug 1 – Sensitive + Large Value Leaks Secret

Create a Terraform plan with a sensitive output whose JSON value exceeds 80 characters compact:

```json
{
  "output_changes": {
    "large_secret": {
      "actions": ["create"],
      "before": null,
      "after": {"token": "ey...very-long-jwt-token-here...", "expires": "2025-12-31"},
      "after_sensitive": true
    }
  }
}
```

Expected: The output table shows `(sensitive value)`, and **no below-table block** is emitted.
Actual: The output table shows `_(see below)_`, and the below-table code block shows the raw secret JSON.

### Bug 2 – Large JSON Not Pretty-Printed

Create a Terraform plan with a large (> 80 chars compact) JSON array output:

```json
{
  "output_changes": {
    "role_assignments": {
      "actions": ["create"],
      "before": null,
      "after": [{"principal": "user@example.com", "role": "Contributor"}, {"principal": "sp@tenant.io", "role": "Reader"}],
      "after_sensitive": false
    }
  }
}
```

Expected: The below-table code block shows formatted JSON with line breaks and indentation.
Actual: The below-table code block shows `[{"principal":"user@example.com","role":"Contributor"},{"principal":"sp@tenant.io","role":"Reader"}]` on a single unformatted line.

---

## Expected Behavior

- **Bug 1:** A masked (sensitive) output value must never appear verbatim in the rendered output, regardless of whether it is also classified as a large value. When `IsMasked = true`, the table cell should show `(sensitive value)` and no below-table block should be emitted.
- **Bug 2:** A large JSON object or array value must be pretty-printed (indented, with line breaks) in the below-table code block.

---

## Actual Behavior

- **Bug 1:** Table cell shows `_(see below)_`; below-table block shows the raw sensitive value as a code block.
- **Bug 2:** Below-table code block shows a compact, single-line JSON string.

---

## Root Cause Analysis

### Affected Component

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`  
**Method:** `RenderOutputTable` (line 163)

---

### Bug 1 – Wrong Check Order + Missing Mask Guard in Below-Table Loop

**Location 1: Table row rendering (lines 171–175)**

```csharp
if (output.IsLargeOutputValue)          // line 171 — checked FIRST
{
    value = "_(see below)_";
}
else if (output.IsMasked)               // line 175 — never reached when large
{
    value = "(sensitive value)";
}
```

`IsLargeOutputValue` is evaluated before `IsMasked`. If a value is both large **and** sensitive, the table cell always shows `_(see below)_` even though it should show `(sensitive value)`.

**Location 2: Below-table rendering loop (lines 216–227)**

```csharp
foreach (var output in outputs)
{
    if (!output.IsLargeOutputValue)     // line 218
    {
        continue;
    }

    writer.Paragraph($"**...**");
    writer.BlankLine();
    writer.Code(output.Value?.ToString() ?? string.Empty, "json");   // line 225 — no IsMasked check!
    writer.BlankLine();
}
```

There is **no check for `output.IsMasked`** in this loop. Every output flagged as `IsLargeOutputValue = true` has its raw value written verbatim — including sensitive/masked values.

**Why `IsLargeOutputValue` can be true for a masked value:**  
`IsLargeOutputValue` is computed in `ReportModelBuilder.Outputs.cs` (line 76) by calling `MarkdownHelpers.IsLargeOutputValue(value)`, which checks the raw JSON size. The `IsMasked` flag is determined separately from `isSensitive` and `_showSensitive`. Both flags are set independently, so a value can be simultaneously `IsLargeOutputValue = true` and `IsMasked = true`.

---

### Bug 2 – `JsonElement.ToString()` Returns Compact JSON

**Location: Below-table rendering loop (line 225)**

```csharp
writer.Code(output.Value?.ToString() ?? string.Empty, "json");
```

`output.Value` is of type `object?` and originates from `outputChange.After` / `outputChange.Before` in `TerraformPlan.OutputChange`. When `System.Text.Json` deserializes the Terraform plan, these `object?` properties are stored as `JsonElement` instances.

**What `JsonElement.ToString()` returns:**  
In .NET 5+, `JsonElement.ToString()` returns the raw JSON representation for object and array value kinds. Since Terraform plan JSON is generated programmatically (compact), the raw text is a compact single-line string — e.g. `[{"a":1},{"b":2}]`.

**Why this surfaces only for large values:**  
`IsLargeOutputValue` returns `true` for `JsonElement` objects/arrays whose compact representation exceeds 80 characters. For shorter values, `TryFormatJsonOutputValue` in the table-row path already pretty-prints the JSON using `Utf8JsonWriter`. But the below-table path at line 225 bypasses `TryFormatJsonOutputValue` and calls `.ToString()` directly.

---

## Suggested Fix Approach

Both bugs are localized to the second `foreach` loop in `RenderOutputTable` (lines 216–227).

### Fix for Bug 1

Add an `IsMasked` guard before rendering the value. Two sub-fixes needed:

1. **Swap check order in the table-row loop** (lines 171–175): evaluate `IsMasked` before `IsLargeOutputValue` so that masked values always show `(sensitive value)` in the table cell.

   ```csharp
   if (output.IsMasked)                    // check FIRST
   {
       value = "(sensitive value)";
   }
   else if (output.IsLargeOutputValue)     // then large
   {
       value = "_(see below)_";
   }
   ```

2. **Add `IsMasked` guard in the below-table loop** (lines 216–227): skip masked outputs entirely (they must not appear verbatim under any circumstances).

   ```csharp
   foreach (var output in outputs)
   {
       if (!output.IsLargeOutputValue || output.IsMasked)   // skip masked
       {
           continue;
       }
       // ... render as before ...
   }
   ```

### Fix for Bug 2

Replace the `.ToString()` call on line 225 with a helper that pretty-prints `JsonElement` values using `Utf8JsonWriter` with `Indented = true`. This mirrors the existing pattern already used in `TryFormatJsonOutputValue` (lines 292–296 in the same file) and in `FormatJson` in `LargeValues.cs`.

**Proposed helper (can be a private static method in `ReportRenderer`):**

```csharp
private static string FormatLargeOutputValueContent(object? value)
{
    if (value is JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                element.WriteTo(writer);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        // String element that contains embedded JSON (detected by IsLargeOutputValue)
        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString();
            if (str is not null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(str);
                    if (doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        using var stream = new MemoryStream();
                        using (var innerWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                        {
                            doc.RootElement.WriteTo(innerWriter);
                        }
                        return Encoding.UTF8.GetString(stream.ToArray());
                    }
                }
                catch (JsonException) { /* fall through */ }
                return str;
            }
        }
    }

    return value?.ToString() ?? string.Empty;
}
```

Then replace line 225:
```csharp
// Before:
writer.Code(output.Value?.ToString() ?? string.Empty, "json");

// After:
writer.Code(FormatLargeOutputValueContent(output.Value), "json");
```

**No new dependencies** are required — `System.Text.Json`, `System.IO.MemoryStream`, and `System.Text.Encoding` are already imported in `ReportRenderer.cs`.

---

## Related Tests

The following tests should be added to catch these regressions:

### Bug 1

- [ ] `ReportRenderer_SensitiveLargeOutput_TableShowsSensitiveValue`: When `IsLargeOutputValue = true` and `IsMasked = true`, the output table cell must contain `(sensitive value)`, **not** `_(see below)_`.
- [ ] `ReportRenderer_SensitiveLargeOutput_BelowTableBlockOmitted`: When `IsLargeOutputValue = true` and `IsMasked = true`, no below-table code block must be emitted for the masked output.
- [ ] `OutputsSnapshotTests_SensitiveLargeOutput_MatchesBaseline`: End-to-end snapshot test using a plan JSON where a large-value output (`after_sensitive: true`) produces masked output in both table and below-table sections.

### Bug 2

- [ ] `ReportRenderer_LargeJsonArrayOutput_BelowTableIsPrettyPrinted`: When `IsLargeOutputValue = true` and the value is a compact JSON array, the below-table code block must contain indented (pretty-printed) JSON.
- [ ] `ReportRenderer_LargeJsonObjectOutput_BelowTableIsPrettyPrinted`: Same for JSON objects.
- [ ] `OutputsSnapshotTests_LargeJsonArrayOutput_MatchesBaseline`: End-to-end snapshot test using a plan JSON with a large JSON array output whose below-table block is pretty-printed.

---

## Additional Context

### Relevant Files

| File | Role |
|------|------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` | **Primary bug location** — `RenderOutputTable` at lines 163–228 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/OutputChangeModel.cs` | `OutputChangeModel` with `IsLargeOutputValue` and `IsMasked` properties |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Outputs.cs` | Builds `OutputChangeModel` instances; sets `IsLargeOutputValue` and `IsMasked` independently |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/ValueFormatting.cs` | `IsLargeOutputValue` implementation (lines 113–138) |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/LargeValues.cs` | `TryFormatStructuredContent`, `FormatJson`, `TryFormatJson` helpers (reference pattern for pretty-printing) |
| `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/OutputsSnapshotTests.cs` | Snapshot test class to extend |
| `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportRendererTests.cs` | Unit test class to extend |

### Related Issues

- `docs/issues/098-sensitive-info-exposure/` — prior work on `SensitivityHelper` for resource attribute masking
- `docs/issues/093-sensitive-attribute-disclosure/` — hierarchical sensitivity detection

### Prior Art (Inline Pretty-Printing)

The same `Utf8JsonWriter(stream, Indented = true)` pattern is already used in:
- `ReportRenderer.cs` — `TryFormatJsonOutputValue` (lines 292–296) for table-cell JSON formatting
- `LargeValues.cs` — `FormatJson` (lines 189–196) for large attribute value formatting
