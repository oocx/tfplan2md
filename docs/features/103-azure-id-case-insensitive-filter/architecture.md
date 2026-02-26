# Architecture: Case-Insensitive Attribute Change Filter

## Status

Revised (2025-07-14): The initial design placed the filter logic in the core pipeline
(`BuildAttributeChanges()`). This revision moves the filter logic into Azure platform-specific
code, with the core providing only a new `IAttributeChangeFilter` extension point.

> **Critical constraint (from maintainer):** The case-insensitive filter MUST only apply to
> Azure resource ID attributes and MUST be implemented in Azure platform-specific code under
> `src/Oocx.TfPlan2Md/Providers/AzureRM/` and `src/Oocx.TfPlan2Md/Platforms/Azure/`.
> It MUST NOT be placed in `MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` as a
> blanket string comparison.

## Analysis

### Existing Pattern: `IValueFormatter` / `ValueFormatterRegistry`

The codebase already has a clean extension point for provider-specific attribute value handling:

- **`IValueFormatter`** (`MarkdownGeneration/Services/`) — interface that each provider implements
- **`ValueFormatterRegistry`** (`MarkdownGeneration/Services/`) — core registry that stores
  `(MatchPattern, IValueFormatter)` pairs and dispatches based on provider/resource/attribute/value
- **`AzureResourceIdFormatter`** (`MarkdownGeneration/Services/`) — implements `IValueFormatter`
  for the azurerm provider; calls `AzureScopeParser.IsAzureResourceId()` to detect Azure IDs
- **`AzureRmValueFormatterRegistration`** (`Providers/AzureRM/`) — registers the formatter
  for the `(^azurerm$|.*/azurerm$)` provider pattern
- **`IProviderModule.RegisterValueFormatters()`** — each module's hook, with default no-op in
  the interface

Azure resource ID *detection* is already centralised in `AzureScopeParser.IsAzureResourceId()`
(`Platforms/Azure/`), which parses the scope string and returns `true` when it resolves to a
known Azure scope level (subscription, resource group, resource, management group).

### Key Insight

The same `IValueFormatter` / `ValueFormatterRegistry` pattern can be mirrored for *filtering*:
a new `IAttributeChangeFilter` / `AttributeChangeFilterRegistry` extension point in the core,
with the Azure-specific implementation living entirely in `Providers/AzureRM/`.

The core's `BuildAttributeChanges()` method changes minimally: it gains a single call to the
filter registry when `_ignoreCaseChanges` is active. It does **not** contain any Azure-specific
logic or regex patterns.

## Architecture Decision

### New Extension Point: `IAttributeChangeFilter` + `AttributeChangeFilterRegistry`

A new pair of types is added to `MarkdownGeneration/Services/`, mirroring the existing
`IValueFormatter` / `ValueFormatterRegistry` pattern:

**`AttributeChangeFilterContext`** — carries the full context needed for a filter decision:

```text
ProviderName  : string?   (e.g. "registry.terraform.io/hashicorp/azurerm")
AttributeName : string?   (e.g. "scope", "role_definition_id")
BeforeValue   : string?   (raw value from the plan's "before" state)
AfterValue    : string?   (raw value from the plan's "after" state)
```

**`IAttributeChangeFilter`** — single method:

```text
bool ShouldSuppress(AttributeChangeFilterContext context)
```

Returns `true` to suppress the attribute change row (hide it from the report).

**`AttributeChangeFilterRegistry`** — list of `IAttributeChangeFilter` instances:

```text
Register(IAttributeChangeFilter filter)
bool ShouldSuppress(AttributeChangeFilterContext context)
  → iterates all registered filters; returns true if any returns true
```

> **Design note:** Unlike `ValueFormatterRegistry`, the filter registry does not use
> `MatchPattern` for routing. Each filter's `ShouldSuppress()` implementation is responsible
> for self-selecting based on its own criteria (e.g., provider name, value pattern). This avoids
> the ambiguity of which value (before or after) to pass to the `MatchPattern` value dimension,
> and is sufficient given the small number of expected filters.

### Azure-Specific Filter: `AzureResourceIdCaseChangeFilter`

New class in **`src/Oocx.TfPlan2Md/Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs`**:

```text
Implements: IAttributeChangeFilter

ShouldSuppress(context):
  1. Return false if BeforeValue or AfterValue is null.
  2. Return false if the provider is not azurerm
       (check: providerName matches regex "(^azurerm$|.*/azurerm$)").
  3. Return false if NEITHER BeforeValue NOR AfterValue is an Azure resource ID
       (check: AzureScopeParser.IsAzureResourceId(BeforeValue)
            || AzureScopeParser.IsAzureResourceId(AfterValue)).
  4. Return false if values are ordinally equal (already handled by valuesEqual check).
  5. Return true if string.Equals(BeforeValue, AfterValue, OrdinalIgnoreCase).
     (casing-only change on an Azure resource ID — suppress it)
```

The Azure resource ID detection uses the **already-existing**
`AzureScopeParser.IsAzureResourceId()` from `Platforms/Azure/AzureScopeParser.cs`. No new
detection logic is introduced in the core pipeline.

### Registration via `IProviderModule`

A new method is added to `IProviderModule` (with a default no-op body to keep all existing
provider modules compatible):

```text
void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
{
    // Default no-op
}
```

`AzureRMModule` overrides this method and registers the filter:

```text
public void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
{
    registry.Register(new AzureResourceIdCaseChangeFilter());
}
```

`ProviderRegistry` gains a matching bulk-registration method (consistent with the existing
`RegisterAllValueFormatters`, `RegisterAllIconProviders`, etc. pattern):

```text
public void RegisterAllAttributeChangeFilters(AttributeChangeFilterRegistry registry)
{
    foreach (var provider in _providers)
        provider.RegisterAttributeChangeFilters(registry);
}
```

### Core Change: `BuildAttributeChanges()` (Minimal)

The only change to the core pipeline is a single guard clause that delegates entirely to the
registry. The core does **not** know about Azure, Azure resource IDs, or casing rules:

```text
After the valuesEqual computation, before the existing _showUnchangedValues check:

if (_ignoreCaseChanges
    && !valuesEqual
    && _attributeChangeFilterRegistry.ShouldSuppress(
           new AttributeChangeFilterContext(providerName, key, beforeValue, afterValue)))
{
    continue;   // Azure ID casing-only change — suppress row
}

if (!_showUnchangedValues && valuesEqual)
{
    continue;   // unchanged value — suppress row (existing behaviour, unchanged)
}
```

This satisfies the spec requirement that casing-only rows remain hidden even when
`--show-unchanged-values` is also passed (the Azure ID filter guard comes first).

### Non-Azure-ID Attributes

Attributes whose values are not Azure resource IDs (plain strings, numbers, booleans) are
never matched by `AzureResourceIdCaseChangeFilter` because `IsAzureResourceId()` returns false.
They continue to use the existing `valuesEqual` logic unchanged.

## Filter Placement: Why Not in `BuildAttributeChanges()` Directly

| Approach | Pros | Cons |
|---|---|---|
| **Original**: blanket `isCasingOnlyChange` in core `BuildAttributeChanges()` | Simplest code change | Filters ALL string attributes regardless of type; Azure logic bleeds into core |
| **Revised**: `IAttributeChangeFilter` registry + `AzureResourceIdCaseChangeFilter` | Azure logic isolated in provider code; extensible to other providers; matches existing `IValueFormatter` pattern | Slightly more files |

The revised approach is chosen because it respects the architectural boundary (provider-specific
logic stays in `Providers/`) and uses the exact same extension-point pattern already established
for `IValueFormatter`.

## Implementation Guidance

### New files to create

| File | Purpose |
|------|---------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AttributeChangeFilterContext.cs` | Context record for filter decisions |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/IAttributeChangeFilter.cs` | Filter interface |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AttributeChangeFilterRegistry.cs` | Registry holding all registered filters |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs` | Azure-specific filter implementation |

### Files to modify

| File | Change |
|------|--------|
| `src/Oocx.TfPlan2Md/Providers/IProviderModule.cs` | Add `RegisterAttributeChangeFilters()` with default no-op |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ProviderRegistry.cs` | Add `RegisterAllAttributeChangeFilters()` |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs` | Override `RegisterAttributeChangeFilters()` |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs` | Add `_ignoreCaseChanges` field + `_attributeChangeFilterRegistry` dependency |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` | Add Azure ID filter guard before existing `valuesEqual` guard |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` | Add `IgnoreCaseChanges` property (for template access) |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` | Populate `IgnoreCaseChanges` in returned model |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` | Expose `ignore_case_changes` to Scriban templates |
| `src/Oocx.TfPlan2Md/CLI/CliParser.cs` | Add `IgnoreCaseChanges` property + parser switch case |
| `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` | Add help entry for `--ignore-case-changes` |
| `src/Oocx.TfPlan2Md/CompositionRoot.cs` | Create `AttributeChangeFilterRegistry`, wire it up, pass `IgnoreCaseChanges` |

### `AttributeChangeFilterContext` record

```csharp
internal sealed record AttributeChangeFilterContext(
    string? ProviderName,
    string? AttributeName,
    string? BeforeValue,
    string? AfterValue);
```

### `AzureResourceIdCaseChangeFilter` logic summary

```csharp
// In Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs
internal sealed class AzureResourceIdCaseChangeFilter : IAttributeChangeFilter
{
    // Matches both the short provider name "azurerm" and the fully-qualified Terraform
    // registry path such as "registry.terraform.io/hashicorp/azurerm".
    private static readonly Regex AzureRmProviderPattern =
        new(@"(^azurerm$|.*/azurerm$)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public bool ShouldSuppress(AttributeChangeFilterContext context)
    {
        if (context.BeforeValue is null || context.AfterValue is null) return false;
        if (!AzureRmProviderPattern.IsMatch(context.ProviderName ?? string.Empty)) return false;
        if (!AzureScopeParser.IsAzureResourceId(context.BeforeValue)
            && !AzureScopeParser.IsAzureResourceId(context.AfterValue)) return false;
        // suppress only when the difference is casing-only
        return string.Equals(context.BeforeValue, context.AfterValue, StringComparison.OrdinalIgnoreCase);
        // Note: valuesEqual (ordinal) is already false at this point (checked by caller)
    }
}
```

### `CompositionRoot` wiring

Add a new helper method:

```csharp
internal AttributeChangeFilterRegistry CreateAttributeChangeFilterRegistry(ProviderRegistry providerRegistry)
{
    var registry = new AttributeChangeFilterRegistry();
    providerRegistry.RegisterAllAttributeChangeFilters(registry);
    return registry;
}
```

Pass `ignoreCaseChanges: options.IgnoreCaseChanges` and `attributeChangeFilterRegistry` to
`ReportModelBuilder` constructor.

### `ReportModelBuilder` constructor additions

```csharp
bool ignoreCaseChanges = false,
AttributeChangeFilterRegistry? attributeChangeFilterRegistry = null,
```

Backing fields:

```csharp
private readonly bool _ignoreCaseChanges = ignoreCaseChanges;
private readonly AttributeChangeFilterRegistry _attributeChangeFilterRegistry =
    attributeChangeFilterRegistry ?? new AttributeChangeFilterRegistry();
```

## Test Guidance

### Unit tests

Follow the pattern in `ReportModelBuilderUnchangedValuesTests.cs`. Recommended new test class:
`ReportModelBuilderIgnoreCaseChangesTests.cs`.

Test cases:
1. **Flag absent (no regression)** — rows with Azure ID casing difference are shown.
2. **Azure ID casing-only, flag active** — rows are suppressed.
3. **Non-Azure-ID casing change, flag active** — rows are NOT suppressed (e.g., `"MyApp"` vs `"myapp"`).
4. **Mixed changes** — only Azure ID casing rows suppressed; genuine changes remain.
5. **Null before/after** — not suppressed regardless of flag.
6. **Interaction with `--show-unchanged-values`** — Azure ID casing rows still hidden when both flags active.
7. **Non-azurerm provider** — Azure ID-shaped values in another provider are NOT suppressed.

Also test `AzureResourceIdCaseChangeFilter` in isolation, and update `CliParserTests.cs` to
verify `--ignore-case-changes` sets `IgnoreCaseChanges = true`.

### Test data file

`src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-case-only-ids-plan.json` should include:
- `azurerm_role_assignment` with `scope` and `role_definition_id` that differ only in casing
- Same resource with a `display_name` that has a genuine content change
- Resources with null before/after values

## ADR Reference

No new ADR is required. This feature extends the existing `IValueFormatter` / `ValueFormatterRegistry`
extension-point pattern (feature 061) and the CLI flag pattern established by feature 014.
Architectural boundaries are enforced per ADR-007.
