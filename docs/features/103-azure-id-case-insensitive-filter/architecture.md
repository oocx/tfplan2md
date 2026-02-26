# Architecture: Case-Insensitive Attribute Change Filter

## Status

No architectural changes required. This feature extends the existing CLI flag + filtering pattern established by the `--show-unchanged-values` flag (feature 014).

## Analysis

The codebase already has a clear, well-established pattern for CLI flags that influence attribute-change filtering:

1. **CLI parsing** — `CliOptions` record + `CliParser.Parse()` in `src/Oocx.TfPlan2Md/CLI/CliParser.cs`
2. **Filtering** — `ReportModelBuilder.BuildAttributeChanges()` in `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
3. **Model propagation** — `ReportModel` property + `AotScriptObjectMapper` Scriban variable in `src/Oocx.TfPlan2Md/MarkdownGeneration/`
4. **Composition wiring** — `CompositionRoot.CreateReportModelBuilder()` in `src/Oocx.TfPlan2Md/CompositionRoot.cs`
5. **Help text** — `HelpTextProvider.GetHelpText()` in `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs`

The `--ignore-case-changes` flag maps perfectly onto this existing pipeline. No new architectural patterns are needed.

## Filter Placement Decision

The filter is applied at **model-building time** inside `BuildAttributeChanges()`, immediately after the existing `valuesEqual` check. This is the correct location because:

- It is consistent with the existing `_showUnchangedValues` filter — both filters suppress rows before the model is passed to templates.
- It operates on **raw (unmasked) values** (`beforeValue` / `afterValue` from the flat dictionary), not display values. This matches the existing sensitive-masking pattern and avoids false positives from "(sensitive)" mask strings.
- Rows filtered here simply never appear in `AttributeChanges`, so the filter applies uniformly to all templates (built-in and custom) automatically.
- The `ReportModel.IgnoreCaseChanges` property (see below) exposes the flag to templates that need to customise their rendering (e.g., show a tooltip or banner).

### Filter Logic

After computing `valuesEqual` (ordinal comparison), an additional `isCasingOnlyChange` flag is computed:

```text
isCasingOnlyChange = _ignoreCaseChanges
    AND beforeValue is not null
    AND afterValue is not null
    AND string.Equals(beforeValue, afterValue, StringComparison.OrdinalIgnoreCase)
    AND NOT valuesEqual
```

Row suppression conditions (evaluated in order):
1. If `isCasingOnlyChange` → **always skip** (takes precedence over `--show-unchanged-values`)
2. Else if `!_showUnchangedValues && valuesEqual` → skip (existing behaviour, unchanged)
3. Otherwise → include row

This satisfies the spec requirement: _"rows suppressed by `--ignore-case-changes` remain hidden even when `--show-unchanged-values` is also passed."_

### Non-String Values

After `ConvertToFlatDictionary`, all plan values are represented as `string?`. In practice:
- JSON numbers (e.g., `42`) and booleans (`true`/`false`) are always lowercase in Terraform plan output — they will already be ordinally equal (`valuesEqual = true`) and filtered by the existing unchanged-values check, or shown regardless.
- The `isCasingOnlyChange` condition requires `NOT valuesEqual`, so it only triggers when ordinal comparison fails. A number like `42` will never change case. Boolean `true`/`false` won't differ in casing in legitimate Terraform output.
- For null values: either `beforeValue` or `afterValue` being null causes `isCasingOnlyChange = false`, so the row is shown normally.

The spec's "non-string values are not subject to the filter" constraint is therefore satisfied automatically without needing type-level checks.

## Implementation Guidance

This feature follows the same file-by-file pattern as the `--show-unchanged-values` feature (feature 014). The following components require changes:

### 1. `src/Oocx.TfPlan2Md/CLI/CliParser.cs`

**CliOptions record**: Add property:
```csharp
/// <summary>
/// Gets a value indicating whether attribute change rows where before and after values
/// differ only in casing are suppressed.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
public bool IgnoreCaseChanges { get; init; }
```

**CliParser.Parse()**: Add switch case:
```csharp
case "--ignore-case-changes":
    ignoreCaseChanges = true;
    break;
```

Return object initialisation: add `IgnoreCaseChanges = ignoreCaseChanges`.

### 2. `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs`

Add entry to the `options` array, positioned after `--show-unchanged-values`:
```csharp
("--ignore-case-changes", "Suppress attribute changes where before/after values differ only in casing."),
```

### 3. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs`

Add constructor parameter (after `showUnchangedValues`):
```csharp
bool ignoreCaseChanges = false,
```

Add backing field:
```csharp
private readonly bool _ignoreCaseChanges = ignoreCaseChanges;
```

Update the constructor XML doc comment to reference this parameter.

### 4. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

In `BuildAttributeChanges()`, after the `valuesEqual` computation and before the existing skip check, add:

```csharp
var isCasingOnlyChange = _ignoreCaseChanges
    && beforeValue is not null
    && afterValue is not null
    && !valuesEqual
    && string.Equals(beforeValue, afterValue, StringComparison.OrdinalIgnoreCase);

if (isCasingOnlyChange)
{
    continue;
}

if (!_showUnchangedValues && valuesEqual)
{
    continue;
}
```

Replace the existing single `if (!_showUnchangedValues && valuesEqual) continue;` with the above two-guard pattern.

### 5. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`

Add property:
```csharp
/// <summary>
/// Gets a value indicating whether attribute change rows where before and after values
/// differ only in casing are suppressed.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
public required bool IgnoreCaseChanges { get; init; }
```

### 6. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`

In `Build()`, add to the `return new ReportModel { ... }` initialiser:
```csharp
IgnoreCaseChanges = _ignoreCaseChanges,
```

### 7. `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`

Add mapping alongside the existing `show_unchanged_values` entry:
```csharp
scriptObject["ignore_case_changes"] = model.IgnoreCaseChanges;
```

### 8. `src/Oocx.TfPlan2Md/CompositionRoot.cs`

In `CreateReportModelBuilder()`, pass the new option:
```csharp
ignoreCaseChanges: options.IgnoreCaseChanges,
```

## Components Affected

| File | Change Type |
|------|-------------|
| `src/Oocx.TfPlan2Md/CLI/CliParser.cs` | Add `IgnoreCaseChanges` property + parser case |
| `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` | Add help entry for `--ignore-case-changes` |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs` | Add constructor param + backing field |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` | Add `isCasingOnlyChange` guard in `BuildAttributeChanges()` |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` | Add `IgnoreCaseChanges` property |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` | Populate `IgnoreCaseChanges` in returned model |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` | Expose `ignore_case_changes` to Scriban templates |
| `src/Oocx.TfPlan2Md/CompositionRoot.cs` | Pass `IgnoreCaseChanges` to `ReportModelBuilder` |

No changes are required to templates, providers, or any other layer. The filter is provider-agnostic and applies transparently to all resource types.

## Test Guidance

Tests should follow the pattern in `ReportModelBuilderUnchangedValuesTests.cs`. Recommended new test class: `ReportModelBuilderIgnoreCaseChangesTests.cs`.

Required test cases (from spec success criteria):
1. **Flag absent (no regression)** — `BuildAttributeChanges` with `ignoreCaseChanges: false` includes all rows regardless of casing.
2. **All rows casing-only** — All attribute change rows are suppressed when `ignoreCaseChanges: true`.
3. **Mixed changes** — Only casing-only rows are suppressed; genuine changes remain.
4. **Interaction with `--show-unchanged-values`** — Casing-only rows are still hidden when both `ignoreCaseChanges: true` and `showUnchangedValues: true`.

Also update `CliParserTests.cs` to verify `--ignore-case-changes` sets `IgnoreCaseChanges = true`.

## ADR Reference

No new ADR is required. This feature applies the same design as ADR-006 (Pure DI) and the established CLI flag pattern for filter options.
