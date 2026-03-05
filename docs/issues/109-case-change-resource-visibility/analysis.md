# Issue: Case-Change Resources Still Counted in Summary After PR #574

## Problem Description

When `--ignore-azure-id-case-changes` is active (which is the **default**), resources whose only
attribute changes are Azure resource ID casing differences (e.g., `APP-RG-GWC` vs `app-rg-gwc`)
are correctly **excluded from the rendered report body** — they do not appear in `model.Changes`.

However, these resources are **still counted in the summary** (`model.Summary.ToChange`). The
summary therefore reports "N resource(s) to change" even though none of those resources appear in
the rendered report. This creates confusion for reviewers who see a non-zero "to change" count but
no corresponding resource details.

## Steps to Reproduce

1. Create a Terraform plan JSON with an `azurerm_role_assignment` or `azapi` resource whose only
   changes are Azure resource ID casing differences (e.g., `scope` attribute before:
   `/subscriptions/ABC123/resourceGroups/my-rg`, after:
   `/subscriptions/abc123/resourceGroups/my-rg`).
2. Run `tfplan2md plan.json` (default flags — `--ignore-azure-id-case-changes` is on by default).
3. Observe that the resource does **not** appear in the rendered change list.
4. Observe that the summary still shows "1 resource(s) to change".

A concrete example already exists in the test data at
`src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-case-only-ids-plan.json`, specifically
`azurerm_role_assignment.casing_only`.

## Expected Behavior

When a resource has **only** Azure resource ID casing differences (all attribute changes suppressed
by `--ignore-azure-id-case-changes`) AND no other meaningful data (no code-analysis findings, no
import ID, no moved-from address, no whole-resource unknown-after-apply):

- The resource **does not appear** in the rendered report body. ✅ _(already works after PR #574)_
- The resource **is not counted** in `model.Summary.ToChange`. ❌ _(still broken)_
- The resource **is counted** in `model.FilteredResourceCount`. ✅ _(already works)_

## Actual Behavior

- `model.Changes` does **not** include `azurerm_role_assignment.casing_only`. ✅
- `model.Summary.ToChange.Count` **does** include `azurerm_role_assignment.casing_only`. ❌
- `model.FilteredResourceCount` is non-zero. ✅

## Root Cause Analysis

### Affected Components

| File | Location | Role |
|------|----------|------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` | Lines 44–46 | Computes summary counts from `allChanges` (pre-filter) |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` | Lines 64–73 | Filters `displayChanges` to exclude casing-only resources |

### What's Broken

The summary counts are computed **before** the `displayChanges` filter that removes casing-only
resources:

```csharp
// Lines 33–36: Build all change models (action = "update" for casing-only resources)
var allChanges = plan.ResourceChanges
    .Select(BuildResourceChangeModel)
    .ToList();

// Lines 44–46: SUMMARY COMPUTED HERE — from allChanges (includes casing-only resources)
var toAdd    = BuildActionSummary(allChanges.Where(c => c.Action == "create"));
var toChange = BuildActionSummary(allChanges.Where(c => c.Action is "update" or "unknown"));
// ...

// Lines 64–73: DISPLAY FILTER — excludes casing-only resources
var displayChanges = afterNoOpFilter
    .Where(c => !_ignoreAzureIdCaseChanges
        || c.Action is not (UpdateAction or UnknownAction)
        || c.AttributeChanges.Count > 0      // ← 0 for casing-only resources
        || c.CodeAnalysisFindings.Count > 0
        || c.ImportId is not null
        || c.MovedFromAddress is not null
        || c.HasWholeResourceUnknownAfterApply
        || HasChildrenWithChanges(c))
    .ToList();
```

A resource with only casing-only attribute changes has `AttributeChanges.Count == 0` after
`BuildAttributeChanges()` suppresses the casing rows. The `displayChanges` filter correctly excludes
it. But `toChange` was already computed from `allChanges` on line 45, including that resource.

### Why AttributeChanges.Count Is Zero for Casing-Only Resources

`BuildAttributeChanges()` in `ReportModelBuilder.ResourceChanges.cs` (lines 138–144) applies the
`_attributeChangeFilterRegistry` before appending each row:

```csharp
if (_ignoreAzureIdCaseChanges
    && !valuesEqual
    && _attributeChangeFilterRegistry.ShouldSuppress(
           new Services.AttributeChangeFilterContext(providerName, key, beforeValue, afterValue)))
{
    continue;   // ← attribute NOT added to changes list
}
```

For `azurerm` resources, `AzureResourceIdCaseChangeFilter` (registered by `AzureRMModule`) suppresses
casing-only Azure ID attributes. For `azapi` resources, `AzApiResourceIdCaseChangeFilter` (registered
by `AzApiModule` since PR #574) suppresses them too. The result is `AttributeChanges.Count == 0`.

### What PR #574 Did — and Didn't Do

PR #574 (merged 2026-03-04) fixed issue 108 (`docs/issues/108-azapi-body-casing-filter/`):

**What it did correctly:**
1. Added `AzApiResourceIdCaseChangeFilter` and registered it in `AzApiModule.RegisterAttributeChangeFilters()`.
   This suppressed `azapi` body attributes from `AttributeChanges`, preventing them from inflating
   `AttributeChanges.Count`.
2. Extended `AzApiBodyRenderer.AreEqual()` with `ignoreAzureIdCaseChanges` parameter so the body
   rendering pipeline also suppresses casing-only body changes.
3. The `displayChanges` filter in `Build.cs` (lines 64–73) correctly excludes resources where
   `AttributeChanges.Count == 0` after filtering.

**What it missed:**
- The summary counts on lines 44–46 are computed from `allChanges` BEFORE the `displayChanges`
  filter runs. Resources filtered from display are still counted in `toChange`.
- The existing code comment (`// CRITICAL: Calculate summary BEFORE parent-child merging`) explains
  why summary is computed early (to avoid parent-child grouping affecting counts), but this comment
  does NOT address the case-change filter. The case-change filter is a semantic filter, not just
  visual, and filtered resources should not be counted.

### Confirmed Impact

The test `ReportModelBuilderIgnoreAzureIdCaseChangesTests.Build_IgnoreAzureIdCaseChangesTrue_AllAzureIdCasingOnly_ResourceSuppressed`
(TC-02) confirms the resource is absent from `model.Changes`. However, there is **no test** that
verifies `model.Summary.ToChange.Count` excludes the suppressed resource.

The following assertion would currently **fail** (expected 0, actual 1):
```csharp
// Using azurerm-case-only-ids-plan.json with ignoreCaseChanges: true
model.Summary.ToChange.Count.Should().Be(0,
    "a resource whose only changes are casing differences should not be counted");
```

Note: `azurerm-case-only-ids-plan.json` contains these update resources when casing filter is off:
- `azurerm_role_assignment.casing_only` — only casing diffs (should be filtered)
- `azurerm_role_assignment.mixed_changes` — one casing diff + one genuine diff (should remain)
- `azurerm_role_assignment.display_name_casing` — non-Azure-ID casing (should remain)
- Other resources with various actions

With casing filter ON, `toChange.Count` should decrease by exactly 1 (for `casing_only`).

## Suggested Fix Approach

### Fix Location

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`

### Approach: Apply Same Conditions as displayChanges Filter to Summary Computation

The `toChange` summary should only count `update`/`unknown` resources that have at least one
meaningful attribute change after filtering. Introduce a helper predicate that mirrors the
`displayChanges` filter conditions:

```csharp
// A resource is "effectively changed" if it has attribute changes OR other meaningful content
// after the casing filter is applied.
// Note: HasChildrenWithChanges cannot be checked here (pre-parent-child-merge), but that edge
// case is acceptable: a no-op parent with only casing-difference children would be counted as
// changed in the summary, consistent with how it would be counted even without this fix.
bool IsEffectivelyChanged(ResourceChangeModel c) =>
    !_ignoreAzureIdCaseChanges
    || c.Action is not (UpdateAction or UnknownAction)
    || c.AttributeChanges.Count > 0
    || c.CodeAnalysisFindings.Count > 0
    || c.ImportId is not null
    || c.MovedFromAddress is not null
    || c.HasWholeResourceUnknownAfterApply;

var toChange = BuildActionSummary(
    allChanges.Where(c => c.Action is "update" or "unknown" && IsEffectivelyChanged(c)));
```

### Why This Approach is Safe

1. **No parent-child interaction**: Summary is computed before parent-child merging. The predicate
   does not use `HasChildrenWithChanges` (which requires post-merge data). This matches the existing
   comment `// CRITICAL: Calculate summary BEFORE parent-child merging`. The edge case of a parent
   whose only children have casing-only changes is negligibly rare and acceptable.

2. **No regression for other actions**: The filter only activates when `action is "update" or "unknown"`
   AND `_ignoreAzureIdCaseChanges` is true. Create, delete, replace, no-op, and read actions are
   unaffected.

3. **Consistent with displayChanges**: The conditions in `IsEffectivelyChanged` are a subset of the
   conditions in the `displayChanges` filter (minus `HasChildrenWithChanges`), ensuring consistency
   between summary counts and the rendered list.

4. **FilteredResourceCount already correct**: The `filteredResourceCount = afterNoOpFilter.Count -
   displayChanges.Count` calculation is unrelated to the summary and remains correct.

### Required Test Additions

After the fix, the following tests should be added to
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderIgnoreCaseChangesTests.cs`:

- **TC-20**: `Build_IgnoreAzureIdCaseChangesTrue_CasingOnlyResource_NotCountedInSummary`
  — `ignoreCaseChanges: true` + `azurerm-case-only-ids-plan.json` → `model.Summary.ToChange.Count`
  does NOT include `azurerm_role_assignment.casing_only`.

- **TC-21**: `Build_IgnoreAzureIdCaseChangesFalse_CasingOnlyResource_IsCountedInSummary`
  — `ignoreCaseChanges: false` + same plan → `model.Summary.ToChange.Count` DOES include
  `azurerm_role_assignment.casing_only` (regression guard).

- **TC-22 (azapi)**: `Build_IgnoreAzureIdCaseChangesTrue_AzApiCasingOnlyResource_NotCountedInSummary`
  — An `azapi` resource with only body casing changes is also excluded from `toChange.Count`.
  Requires a new test plan JSON with an `azapi_update_resource` resource whose only body changes
  are casing-only Azure IDs.

- **TC-23**: `Build_IgnoreAzureIdCaseChangesTrue_MixedResource_IsCountedInSummary`
  — `azurerm_role_assignment.mixed_changes` (one casing diff + one genuine diff) is still counted.

## Related Tests

Tests that must pass after the fix:

- [ ] TC-20 (new): Casing-only resource excluded from `toChange.Count` when filter enabled
- [ ] TC-21 (new): Casing-only resource included in `toChange.Count` when filter disabled
- [ ] TC-22 (new): azapi casing-only resource excluded from `toChange.Count`
- [ ] TC-23 (new): Mixed resource still counted in `toChange.Count`
- [ ] TC-02 (existing): Casing-only resource suppressed from `model.Changes`
- [ ] TC-17/TC-19 (existing): `FilteredResourceCount` is non-zero
- [ ] All other `ReportModelBuilderIgnoreAzureIdCaseChangesTests` pass

## Additional Context

- **This IS a bug, not a scope expansion**: Feature 103's original spec excluded both display
  suppression and summary count suppression from scope. However, PR #574 subsequently added display
  suppression (the `displayChanges` filter in `Build.cs`). That partial implementation created an
  **internal inconsistency**: resources are hidden from the rendered body but still counted in the
  summary. The summary therefore promises detail (e.g., "1 resource to change") that the body does
  not deliver. This inconsistency is the bug — it was introduced by PR #574's partial implementation
  rather than being intentional design. The fix is to extend the same suppression logic to the
  summary counts to restore consistency.

- **PR #574 added display suppression**: The `displayChanges` filter in `Build.cs` lines 64–73 was
  added by PR #574 to correctly exclude casing-only resources from display. This was a step in the
  right direction but the summary computation was not updated to match.

- **FilteredResourceCount is a partial mitigation**: The `filteredResourceCount` field in the model
  gives the template a count of suppressed resources. The template could potentially use this to
  annotate the summary (e.g., "1 to change + 1 suppressed by casing filter"). However, the cleaner
  fix is to simply exclude suppressed resources from the `toChange` count.

- **Related files:**
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` — fix location
  - `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` — attribute filter
  - `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs` — azurerm filter
  - `src/Oocx.TfPlan2Md/Providers/AzApi/AzApiResourceIdCaseChangeFilter.cs` — azapi filter
  - `src/tests/.../TestData/azurerm-case-only-ids-plan.json` — existing test data
  - `src/tests/.../MarkdownGeneration/ReportModelBuilderIgnoreCaseChangesTests.cs` — existing tests
