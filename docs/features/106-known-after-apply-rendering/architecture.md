# Architecture: Known-After-Apply Rendering

## Status

Proposed

## Context

Feature specification: [specification.md](specification.md)

### Problem Statement

Terraform plan JSON represents computed/unknown post-apply values via the `after_unknown` field:

- **Attribute-level unknown**: `after: { attr: null }` and `after_unknown: { attr: true }`
- **Whole-resource unknown**: `after: null` and `after_unknown: true`

Today, tfplan2md silently drops all of these from reports:

1. **Attribute tables show `_No attribute changes._`** for any resource where all `after` values are `null` — because `null == null` is treated as "unchanged". The `after_unknown` field is never read during resource rendering.
2. **AzureAD group member summary lines are blank** (just "→") when both `group_object_id` and `member_object_id` are computed, because `FormatCodeSummary("")` silently returns an empty string.

This feature must:

- Surface computed attributes in attribute tables using `(known after apply)` (and optionally a configuration reference label).
- Fix `azuread_group_member` summary lines so they never render blank when IDs are computed.
- Use Terraform configuration `expressions.references` (when present) as safe, human-meaningful labels.

### Constraints

- Output must remain compatible with GitHub + Azure DevOps markdown rendering.
- Sensitive values must never be revealed; references from configuration are expression paths and are safe to display.
- Provider-specific formatting must remain isolated under `src/Oocx.TfPlan2Md/Providers/<ProviderName>/` (see `docs/architecture-rules.md`).
- NativeAOT-compatible: no reflection, no dynamic code generation.

---

## Codebase Background

This section provides the developer with enough context to understand the rendering pipeline. The key data flow is:

```text
Terraform plan JSON
→ Parsing layer (TerraformPlan, Change, ResourceChange records)
→ Model-building layer (ReportModelBuilder → ResourceChangeModel + AttributeChangeModel)
→ Template rendering (Scriban .sbn templates)
→ Markdown output
```

### Parsing Layer

**File:** `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`

The plan JSON is deserialized into immutable C# records:

- `TerraformPlan`: Top-level record with `ResourceChanges` (list of `ResourceChange`) and `Configuration` (`JsonElement?`).
- `ResourceChange`: Per-resource record with `Address` (e.g., `azuread_group_member.this["key"]`), `Type`, `Name`, `ProviderName`, and a nested `Change` object.
- `Change`: Contains:
  - `Actions` — ordered list of action strings (`["create"]`, `["update"]`, `["delete", "create"]`, etc.)
  - `Before` (`object?`) — resource state before the change (deserialized as `JsonElement`)
  - `After` (`object?`) — resource state after the change (deserialized as `JsonElement`)
  - `AfterUnknown` (`object?`) — marks which attributes have values that are not yet determined (deserialized as `JsonElement`)
  - `BeforeSensitive` / `AfterSensitive` (`object?`) — marks which attributes are sensitive

All `object?` fields are deserialized by `System.Text.Json` as `JsonElement` values. When the plan JSON has `null` for a field, the `JsonElement` has `ValueKind == JsonValueKind.Null`.

**Critical:** `AfterUnknown` is currently stored on the `Change` record but is **never read** by any rendering code in the main project. This is the root cause of the bug.

### JsonFlattener

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/JsonFlattener.cs`

`ConvertToFlatDictionary(object? obj, string prefix)` recursively walks a `JsonElement` and produces a `Dictionary<string, string?>` with dotted/bracketed keys:

- Object properties use dot notation: `{ "tags": { "env": "prod" } }` → `{ "tags.env": "prod" }`
- Array elements use bracket notation: `{ "rules": ["a", "b"] }` → `{ "rules[0]": "a", "rules[1]": "b" }`
- `JsonValueKind.Null` produces a `null` string value in the dictionary (the key is present, but the value is `null`)
- `JsonValueKind.String` produces the string value
- Booleans are lowercased: `"true"` / `"false"`
- Numbers use `GetRawText()`

This means when Terraform writes `"group_object_id": null` in the `after` object, the flattener produces `{ "group_object_id": null }` — the key exists but the value is `null`.

### ReportModelBuilder — The Heart of Model Construction

**Files:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs` — class declaration, constructor (primary constructor with 12 parameters), field declarations, registry creation
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` — `Build(TerraformPlan plan)` entry point
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — `BuildResourceChangeModel()`, `BuildAttributeChanges()`, action/symbol helpers

#### Build Flow

`Build(TerraformPlan plan)` in `ReportModelBuilder.Build.cs`:

1. Builds the configuration reference index: `_configurationReferenceIndex = ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration)`
2. Maps each `ResourceChange` to a `ResourceChangeModel` via `BuildResourceChangeModel()`
3. Calculates action summaries (add/change/destroy/replace counts) **before** parent-child merging
4. Merges parent-child relationships (visual grouping only — does not affect counts)
5. Filters no-op resources from display
6. Groups by module for template rendering

#### BuildResourceChangeModel (in ResourceChanges.cs)

For each `ResourceChange rc`:

1. Determines action string (create/update/delete/replace/etc.) from `rc.Change.Actions`
2. Calls `BuildAttributeChanges(rc.Change, rc.ProviderName)` → `List<AttributeChangeModel>`
3. Creates a `ResourceChangeModel` with all properties
4. If a view model factory is registered for `rc.Type`, applies it (this is how provider-specific summaries are built — see the Provider System section below)
5. Falls back to generic summary/summaryHtml if the factory didn't set one
6. Builds `ChangedAttributesSummary` (e.g., "2 🔧 attr1, attr2") — only for update actions
7. Builds `TagsBadges` for create/delete actions

#### BuildAttributeChanges — THE BUG LOCATION (in ResourceChanges.cs)

This is the method that must be enhanced. Current logic:

```text
1. Flatten change.Before → beforeDict (Dictionary<string, string?>)
2. Flatten change.After  → afterDict  (Dictionary<string, string?>)
3. Flatten change.BeforeSensitive → beforeSensitiveDict
4. Flatten change.AfterSensitive  → afterSensitiveDict
5. Union all keys from beforeDict and afterDict
6. For each key:
   a. Get beforeValue and afterValue from the dictionaries
   b. Check sensitivity via SensitivityHelper.IsSensitiveAttribute()
   c. Mask sensitive values to "(sensitive)" unless --show-sensitive
   d. Compare RAW values (before masking) for equality
   e. If !showUnchangedValues && valuesEqual → SKIP (continue)
   f. Build AttributeChangeModel { Name, Before, After, IsSensitive, IsLarge }
```

**Root cause:** Step (e) skips the attribute when `beforeValue == null` and `afterValue == null` (both null means "equal"). The method never reads `change.AfterUnknown`, so it cannot distinguish "value is null" from "value is unknown/computed".

For a create action where `After` has `{ "group_object_id": null }` and `AfterUnknown` has `{ "group_object_id": true }`:
- `beforeDict` is empty (Before is null for creates)
- `afterDict` has `{ "group_object_id": null }`
- Key `"group_object_id"` unions both dicts → `beforeValue = null`, `afterValue = null`
- `null == null` → valuesEqual = true → row is skipped

Even if the row survived filtering, there is a second gate: the template's `format_attribute_value_table(attr.name, attr.after, ...)` function returns empty string for null/whitespace values (see Template Rendering below), and the template's `if value != ""` check would skip the row anyway.

### AttributeChangeModel

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/AttributeChangeModel.cs`

Simple record with:
- `Name` (required string) — the flattened attribute path (e.g., `"group_object_id"`, `"tags.env"`, `"rules[0].priority"`)
- `Before` (string?) — display value before the change (may be masked to "(sensitive)")
- `After` (string?) — display value after the change
- `IsSensitive` (bool) — whether this attribute is marked sensitive
- `IsLarge` (bool) — whether the value exceeds the threshold for inline table display

### ResourceChangeModel

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`

The main model class passed to templates. Key properties:

- `Address`, `Type`, `Name`, `ProviderName`, `Action`, `ActionSymbol` — identity and action
- `AttributeChanges` (`IReadOnlyList<AttributeChangeModel>`) — the attribute rows for tables
- `BeforeJson`, `AfterJson` (`object?`) — raw JSON state, used by provider-specific templates
- `BeforeSensitive`, `AfterSensitive` (`object?`) — raw sensitivity trees
- `Summary`, `SummaryHtml`, `ChangedAttributesSummary`, `TagsBadges` — precomputed display strings
- `ChildResourceGroups` — grouped child resources (from parent-child merging)
- `ImportId`, `MovedFromAddress`, `IsRefactoringAlreadyApplied` — refactoring metadata
- `ResourceChange` (internal) — stores the original parsing record for factory access

### SensitivityHelper

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs`

`IsSensitiveAttribute(key, beforeSensitive, afterSensitive)` checks hierarchical paths. For `"variable[0].secret_value"`, it checks: `"variable[0].secret_value"`, `"variable[0]"`, `"variable"`. Also checks root-level sensitivity (`""` key with value `"true"`). This pattern of hierarchical path checking is relevant because `after_unknown` uses the same tree structure as `before_sensitive`/`after_sensitive`.

### Template Rendering

**Default template:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`

Templates are Scriban `.sbn` files embedded as assembly resources. The default template:

1. Receives a `change` variable (a `ResourceChangeModel`)
2. Splits `change.attribute_changes` into `small_attrs` and `large_attrs` based on `IsLarge`
3. For **create** action: renders a single "Value" column
4. For **update** action: renders "Before" / "After" columns
5. For each small attribute, calls `format_attribute_value_table(attr.name, attr.after, change.provider_name)` which maps to the C# method `FormatAttributeValueTableWithRegistry()`
6. **Critical guard:** `{{ if value != "" }}` — if the formatted value is empty string, the row is skipped entirely
7. When `small_attrs.size == 0 && large_attrs.size == 0` and no tags badges → shows `_No attribute changes._`

**FormatAttributeValueTableWithRegistry** (in `SemanticFormatting.Registry.cs`):

```csharp
internal static string FormatAttributeValueTableWithRegistry(
    string? attributeName, string? value, string? providerName,
    ValueFormatterRegistry? valueFormatterRegistry, IconProviderRegistry? iconProviderRegistry)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;  // ← THIS is why null "after" values produce empty cells
    }
    // ... semantic formatting, icon resolution, code wrapping
}
```

This means even if `BuildAttributeChanges` included a computed attribute with `After = null`, the template would render it as empty (and the `if value != ""` guard would suppress the row). **The fix must set `After` to a non-null display string** (e.g., `"(known after apply)"`) so the template renders it.

### ConfigurationReferenceResolver — Already Implemented

**File:** `src/Oocx.TfPlan2Md/Parsing/ConfigurationReferenceResolver.cs`

`BuildReferenceIndex(JsonElement? configuration)` walks the plan's `configuration.root_module` (and nested `module_calls`) and builds:

```text
Dictionary<(string Address, string Attribute), IReadOnlyList<string>>
```

- **Key:** `(resource_address, attribute_name)` — e.g., `("azuread_group_member.this", "group_object_id")`
- **Value:** list of reference strings from the `expressions.<attr>.references` array — e.g., `["azuread_group.admins.object_id", "azuread_group.admins"]`
- Uses case-insensitive comparison via `ConfigurationReferenceKeyComparer`
- `NormalizeReferenceAddress()` prepends the module prefix for non-module references
- Handles nested modules via `module_calls` recursion

**Already integrated:** `ReportModelBuilder.Build()` calls `ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration)` at the start and stores the result in `_configurationReferenceIndex`. This index is currently only used for parent-child relationship matching; this feature will also use it for computed-attribute labels.

### Provider Module System

The project uses a registry-based provider module pattern for provider-specific behavior.

**Interface:** `IProviderModule` (in `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/IProviderModule.cs`)

Each provider module registers:
- **View model factories** — `IResourceViewModelFactory` implementations that customize `ResourceChangeModel` properties (summary, summaryHtml, template, etc.) for specific resource types
- **Icon providers** — semantic icons for attribute values (e.g., location → 📍)
- **Value formatters** — custom value formatting (e.g., tenant ID → display name)
- **Parent-child relationships** — rules for grouping child resources under parents (e.g., `azuread_group_member` under `azuread_group`)
- **Post-merge callbacks** — logic that runs after parent-child merging (e.g., updating group summaries with member counts)

**AzureAD Module:** `src/Oocx.TfPlan2Md/Providers/AzureAD/AzureADModule.cs`

Registers:
- `AzureAdSummaryFactory` for 6 resource types: `azuread_user`, `azuread_group`, `azuread_group_without_members`, `azuread_group_member`, `azuread_service_principal`, `azuread_invitation`
- Parent-child relationship: parent=`azuread_group`, child=`azuread_group_member`, childRef=`group_object_id`, parentId via `name`
- Post-merge callback: `AzureAdGroupSummaryRebuilder.UpdateGroupSummaries`

**View model factory flow:**

In `BuildResourceChangeModel()`, after creating the base model:

```csharp
if (_viewModelFactoryRegistry.TryGetFactory(rc.Type, out var factory) && factory is not null)
{
    factory.ApplyViewModel(model, rc, action, attributeChanges, _principalMapper, _iconProviderRegistry);
}
```

The `AzureAdSummaryFactory.ApplyViewModel()` dispatches to `AzureAdSummaryBuilder` which builds provider-specific `SummaryHtml` strings.

### AzureAD Group Member Summary — THE SECOND BUG

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs`

`BuildGroupMemberSummaryHtml(model, state, principalMapper, iconProviderRegistry)`:

1. Reads `group_object_id` from the JSON state: `JsonStateReader.GetStringProperty(state, "group_object_id") ?? string.Empty`
2. Reads `member_object_id` similarly
3. Attempts to resolve names via `principalMapper.GetName(groupId, ...)` — returns the ID itself when unmapped
4. If the ID is empty string (because the JSON value was null and the null-coalescing produced `""`):
   - `groupName = principalMapper.GetName("", ...) ?? ""` → returns `""`
   - `groupIsMapped` is false (empty string equals itself)
   - Falls through to `FormatCodeSummary(groupId)` which is `FormatCodeSummary("")`
   - `FormatCodeSummary("")` produces empty output — **blank summary**

The summary line becomes just ` → ` with no group or member context. This is the second bug this feature fixes.

**Key observation:** The `state` parameter passed to `BuildGroupMemberSummaryHtml` is `model.AfterJson` (or `model.BeforeJson` for deletes) — the raw JSON. When `After` has `"group_object_id": null`, `JsonStateReader.GetStringProperty(state, "group_object_id")` returns `null`, which the `?? string.Empty` coerces to empty string.

### Reference Implementation: after_unknown Navigation

**File:** `src/tools/Oocx.TfPlan2Md.TerraformShowRenderer/Rendering/DiffRenderer.Paths.cs`

This tool project (separate from the main library) has `IsUnknownPath(JsonElement? root, IReadOnlyList<string> path)`:

```csharp
private static bool IsUnknownPath(JsonElement? root, IReadOnlyList<string> path)
{
    if (root is null) return false;
    var current = root.Value;
    foreach (var segment in path)
    {
        if (current.ValueKind == JsonValueKind.Object)
        {
            var match = current.EnumerateObject()
                .FirstOrDefault(p => string.Equals(p.Name, segment, StringComparison.Ordinal));
            if (match.Value.ValueKind == JsonValueKind.Undefined) return false;
            current = match.Value;
        }
        else if (current.ValueKind == JsonValueKind.Array
            && int.TryParse(segment, ..., out var index)
            && index < current.GetArrayLength())
        {
            current = current.EnumerateArray().ElementAt(index);
        }
        else return false;
    }
    return current.ValueKind == JsonValueKind.True;
}
```

This is in a different assembly (`Oocx.TfPlan2Md.TerraformShowRenderer`) which the main library must not depend on. The new helper should port this minimal logic into `MarkdownGeneration/Helpers/`.

**Key difference from that code:** The tool project receives pre-split path segments (`IReadOnlyList<string>`). The main library works with flattened keys from `JsonFlattener` (e.g., `"tags.env"`, `"rules[0].priority"`). The new helper must parse flattened keys into segments — splitting on `.` and extracting array indices from `[N]` brackets.

---

## Options Considered

### Option 1: Teach JsonFlattener to merge after_unknown into after

- Pros
  - Centralizes "computed value" awareness in one place.
- Cons
  - Mixes two distinct Terraform concepts ("value is null" vs "value exists but is unknown").
  - Makes all consumers of `JsonFlattener` implicitly sensitive to `after_unknown` semantics.
  - Harder to implement the spec's Decision A ("only attributes explicitly present in `after` as `null` should appear").
  - `JsonFlattener` is a pure utility with no domain knowledge; adding Terraform-specific logic would break its single responsibility.

### Option 2: Enhance ReportModelBuilder.BuildAttributeChanges to treat unknown paths as changes (recommended)

- Pros
  - Keeps computed-value semantics in the report model-building layer where domain logic belongs.
  - Naturally enforces Decision A because iteration is still driven by keys present in the flattened `after`/`before` dictionaries — no synthetic keys are invented.
  - Makes computed attributes participate in update summaries (Invariant 8) by being included in `AttributeChanges` with a non-null `After` display value.
  - The existing `_configurationReferenceIndex` is already available in `ReportModelBuilder`.
- Cons
  - Requires an `after_unknown` path navigator and reference-selection logic (new helper class).
  - `BuildAttributeChanges` gains approximately 15 lines of branching logic.

### Option 3: Handle computed values in templates only

- Pros
  - Minimal C# changes.
- Cons
  - Templates do not currently receive `after_unknown` data.
  - `FormatAttributeValueTableWithRegistry` returns empty for null values — the template cannot render them without C# changes anyway.
  - Pushes non-trivial logic into Scriban and risks inconsistent behavior across the 10+ provider-specific templates.

## Decision

Adopt **Option 2**:

1. Interpret `after_unknown` in `ReportModelBuilder.BuildAttributeChanges` and set `After` to a display string like `"(known after apply)"` or `"(known after apply: azuread_group.admins)"` so the template's existing rendering pipeline handles it naturally.
2. Resolve display labels from `_configurationReferenceIndex` using the priority rules in the specification (Decision B).
3. Extend provider-specific AzureAD summary rendering to use configuration references and instance-key fallbacks so `azuread_group_member` summaries never become blank.

No new global ADR is required; this is an incremental enhancement of the report model builder and AzureAD provider summary logic.

---

## Design

### 1) Unknown-After-Apply Detection Helper (core)

**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/AfterUnknownHelper.cs`

Add a static helper class with two public methods:

- `IsWholeResourceUnknownAfterApply(object? afterUnknown)` — returns `true` when `afterUnknown` is a `JsonElement` with `ValueKind == JsonValueKind.True` (the whole-resource unknown shape).
- `IsAttributeUnknownAfterApply(object? afterUnknown, string flattenedKey)` — returns `true` when the path corresponding to `flattenedKey` resolves to `JsonValueKind.True` in the `afterUnknown` tree.

Implementation notes:

- Cast `object?` to `JsonElement` (same pattern as existing code in `JsonFlattener`).
- Parse flattened keys into path segments by splitting on `.` and extracting `[N]` bracket indices. Example: `"rules[0].priority"` → `["rules", "0", "priority"]`. This parsing should be a separate private method to keep concerns clean.
- Navigate the `JsonElement` tree segment-by-segment:
  - For `JsonValueKind.Object`: look up property by name.
  - For `JsonValueKind.Array`: parse segment as integer index.
  - For `JsonValueKind.True` at any intermediate node: return `true` (the whole subtree is unknown).
  - For `JsonValueKind.Undefined` or missing property: return `false`.
- Port the navigation logic from `DiffRenderer.Paths.cs` — do not add a dependency on the tool project.

Follow the coding conventions established by `SensitivityHelper`: internal static class in `MarkdownGeneration/Helpers/`, comprehensive XML documentation, unit-testable.

### 2) Configuration Reference Selection Helper (core)

**Location:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ReferenceSelector.cs`

Add a static helper that selects the best reference label from a list of references, following the spec's Decision B priority:

`SelectBestReference(IReadOnlyList<string> references)` → `string?`

Priority order:

1. **Static resource reference** — 2-part (`type.name`) or 4-part (`module.mod_name.type.name`). Examples: `azuread_group.admins`, `module.identity.azuread_user.admin`.
2. **`each.value.<attr>`** — 3-part starting with `each.value.`.
3. **`var.<name>` or `local.<name>`** — 2-part starting with `var.` or `local.`.
4. Return `null` if no match (caller uses the bare `(known after apply)` fallback).

Skip useless references: `each.key`, `each.value` (bare, without attribute), `count.index`, `self`.

For attribute-table display (Decision B), when a reference is an attribute access like `azuread_group.admins.object_id` (3 parts for a non-module reference), strip to the resource-level label `azuread_group.admins` — see Invariant 6 in the specification.

This helper should also expose `SelectResourceLevelReference(IReadOnlyList<string> references)` → `string?` for the AzureAD summary: returns only static resource references, stripped to `type.name` (no attribute suffix).

### 3) Computed Attribute Value Rendering (core)

**File to modify:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

In `BuildAttributeChanges(Change change, string providerName)`, after the existing value comparison and before the `continue` that skips unchanged values, add a check:

**Pseudocode (not implementation — just to illustrate the logic):**

```text
for each key in allKeys:
    beforeValue = beforeDict[key]
    afterValue = afterDict[key]
    isSensitive = ...
    valuesEqual = beforeValue == afterValue

    // NEW: Check if this attribute is unknown-after-apply
    isUnknownAfterApply = (afterValue is null)
        && AfterUnknownHelper.IsAttributeUnknownAfterApply(change.AfterUnknown, key)

    if isUnknownAfterApply:
        // Override the display value for "after"
        if isSensitive && !showSensitive:
            afterDisplay = "🔒(known after apply)"
        else:
            reference = look up (normalizedAddress, topLevelAttrName) in _configurationReferenceIndex
            bestRef = ReferenceSelector.SelectBestReference(reference)
            afterDisplay = bestRef != null
                ? "(known after apply: <bestRef>)"
                : "(known after apply)"
        // Force inclusion even when valuesEqual would have been true
        valuesEqual = false

    if !showUnchangedValues && valuesEqual:
        continue
```

**Address normalization for reference lookup:** The `_configurationReferenceIndex` is keyed by the address without instance keys (e.g., `"azuread_group_member.this"` not `"azuread_group_member.this[\"key\"]"`). The `ResourceChange.Address` includes instance keys. The normalization should strip instance keys — the same pattern used in parent-child merging.

**Top-level attribute name extraction:** Flattened keys like `"tags.env"` need the top-level attribute name `"tags"` for the reference index lookup, since `configuration.expressions` only stores top-level attribute names. Extract by taking the portion before the first `.` or `[`.

**Passing `_configurationReferenceIndex` and address context:** `BuildAttributeChanges` currently receives only `(Change change, string providerName)`. It needs two additional pieces of context:
- The resource address (from `ResourceChange.Address`) for the reference index lookup
- Access to `_configurationReferenceIndex` (available as a field on `ReportModelBuilder`)

Options: (a) add parameters to the method, or (b) pass the pre-looked-up reference map. Either approach works; the developer should choose whichever keeps the method clean.

### 4) Attaching References to ResourceChangeModel (for provider consumption)

**File to modify:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`

Add an internal property:

```csharp
internal IReadOnlyDictionary<string, IReadOnlyList<string>>? ConfigurationReferences { get; set; }
```

This is a per-attribute reference map built from `_configurationReferenceIndex` for the resource's normalized address. Keyed by attribute name (e.g., `"group_object_id"`), value is the list of references.

**Set in `BuildResourceChangeModel`:** After building attribute changes, extract the subset of `_configurationReferenceIndex` entries matching the resource's normalized address and attach them to the model. This allows provider-specific factories (like `AzureAdSummaryFactory`) to access references without needing the full index.

Mark as `internal` so it is accessible to Provider code (same assembly via `InternalsVisibleTo` or same project) but not exposed to templates.

### 5) Whole-Resource Unknown Placeholder Suppression (template + model)

Scenario 8 requires that `_No attribute changes._` is not shown when `after_unknown: true` and there are no attribute rows (because the entire resource is computed).

**File to modify:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`

Add a boolean property:

```csharp
public bool HasWholeResourceUnknownAfterApply { get; set; }
```

**Set in `BuildResourceChangeModel`:** Use `AfterUnknownHelper.IsWholeResourceUnknownAfterApply(rc.Change.AfterUnknown)`.

**File to modify:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`

In the section that currently shows `_No attribute changes._` (around line 63 in the template):

```scriban
{{ if small_attrs.size == 0 && large_attrs.size == 0 && (change.tags_badges == null || change.tags_badges == "") }}
_No attribute changes._
{{ end }}
```

Add a condition: only show when `!change.has_whole_resource_unknown_after_apply`. (Scriban auto-converts PascalCase C# properties to snake_case.)

### 6) AzureAD azuread_group_member Summary Fix (provider)

**File to modify:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs`

Update `BuildGroupMemberSummaryHtml` to handle computed IDs:

**Current flow:**

```text
groupId = GetStringProperty(state, "group_object_id") ?? ""
memberId = GetStringProperty(state, "member_object_id") ?? ""
→ when both are null (computed): groupId = "", memberId = ""
→ groupName = GetName("", ...) = ""
→ FormatCodeSummary("") = "" → blank summary
```

**New flow — summary label priority (from spec Invariant 3 and scenarios):**

1. **ID is known and mapped:** Keep the existing "name (id)" behavior — no change needed here.
2. **ID is computed (empty string):** Look up the resource's `ConfigurationReferences` for the corresponding attribute (`group_object_id` / `member_object_id`). Use `ReferenceSelector.SelectResourceLevelReference()` to get a static resource reference label (e.g., `azuread_group.admins`).
3. **No reference but string instance key:** Extract the for_each key from `model.Address` (e.g., `azuread_group_member.this["admin_team"]` → `"admin_team"`) and use it as context on both sides (Scenario 3).
4. **Fallback:** `(known after apply)`.

Numeric instance key handling:
- If a numeric instance key exists AND a static group reference was found, append it: `azuread_group.admins[0]` (Scenario 5).
- Do not use numeric keys alone as a label (Invariant 4).

**Data access:** `BuildGroupMemberSummaryHtml` receives the `ResourceChangeModel model`. The configuration references are available via `model.ConfigurationReferences` (added in Design Part 4). The `AzureAdSummaryFactory.ApplyViewModel()` method also receives the `ResourceChange` (which has `Address` for instance key extraction).

**Instance key extraction:** Parse the address string. A for_each key looks like `resource.name["key"]`; a count index looks like `resource.name[0]`. Use a simple regex or string parsing to distinguish string keys (quoted) from numeric keys (unquoted).

---

## Impact on Existing Test Infrastructure

### Snapshot Tests

Existing snapshot tests that use plans with `after_unknown` fields may produce different output after this change (computed attributes will now appear instead of being hidden). Run `scripts/update-test-snapshots.sh` after implementation and verify the diffs are intentional. Include `SNAPSHOT_UPDATE_OK` in the commit message.

### New Tests Required

- **Unit tests** for `AfterUnknownHelper`:
  - `IsWholeResourceUnknownAfterApply` — true for `JsonValueKind.True`, false for objects/null
  - `IsAttributeUnknownAfterApply` — object paths, array paths, nested paths, missing paths, intermediate `true` nodes

- **Unit tests** for `ReferenceSelector`:
  - Priority ordering (static resource > each.value.attr > var/local > null)
  - Stripping attribute suffixes from resource references
  - Skipping useless meta-references
  - `SelectResourceLevelReference` — returns only static resource refs

- **Snapshot/integration tests** for each specification scenario:
  - Scenario 1: AzureAD group member with no configuration, all IDs unknown → `(known after apply)` labels
  - Scenario 2: AzureAD group member with static references → resource-level labels in summary
  - Scenario 3: for_each with string keys, no configuration → instance key labels
  - Scenario 4: for_each with static references → reference labels
  - Scenario 5: count with numeric index + static reference → reference with index
  - Scenario 6: Generic resource with mixed known/computed attributes
  - Scenario 7: Sensitive + computed attribute → `🔒(known after apply)`
  - Scenario 8: Whole-resource unknown → no rows, no `_No attribute changes._`
  - Scenario 9: Update action with computed attribute → appears in `ChangedAttributesSummary`

---

## Consequences

### Positive

- Computed values become reviewable instead of silently disappearing.
- Update summaries correctly count computed transitions.
- AzureAD group member summaries remain informative even when IDs are unknown.
- The fix is universal — all resource types benefit, not just AzureAD.

### Negative / Risks

- Terraform plan shapes vary across providers; `after_unknown` tree navigation must handle unexpected structures gracefully (return false, never throw).
- Reference selection must avoid displaying low-value meta-refs; tests should lock the priority behavior.
- Existing snapshot tests may need updating — this is expected and must be verified as intentional.
