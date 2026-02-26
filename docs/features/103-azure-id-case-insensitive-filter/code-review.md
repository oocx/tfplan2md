# Code Review: Case-Insensitive Attribute Change Filter (Feature 103)

## Summary

This review covers the implementation of the `--ignore-case-changes` CLI flag, which suppresses
Azure resource ID attribute changes where before/after values differ only in casing. The feature
adds a new `IAttributeChangeFilter`/`AttributeChangeFilterRegistry` extension point (mirroring
`IValueFormatter`/`ValueFormatterRegistry`) with Azure-specific logic in `Providers/AzureRM/`.

Overall: the architecture and core implementation are correct and well-structured. One **Blocker**
rendering bug was found and fixed, two **Major** issues were addressed, and the feature is approved
after fixes.

---

## Verification Results

- **Tests:** ✅ Pass — 1,299 tests passed (1,297 original + 2 new factory tests added during review)
- **Build:** ✅ Success
- **Docker:** Not verified (Docker unavailable in this environment)
- **Markdownlint:** ✅ 0 errors (`artifacts/comprehensive-demo.md`, `uat-plan.md`)
- **CodeQL:** ✅ 0 alerts

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| CLI flag `--ignore-case-changes` in help text | ✅ | ✅ TC-08, TC-10 | `CliParser.cs`, `HelpTextProvider.cs` |
| Flag absent → no regression | ✅ | ✅ TC-01 | `ignoreCaseChanges = false` default |
| azurerm + Azure resource ID + casing-only → suppressed | ✅* | ✅ TC-02, TC-03, TC-17 | *Bug fixed — see below |
| Non-Azure-ID attributes NOT suppressed | ✅ | ✅ TC-15, TC-18 | `IsAzureResourceId()` guard |
| `--ignore-case-changes` takes precedence over `--show-unchanged-values` | ✅ | ✅ TC-07 | Guard order in `BuildAttributeChanges()` |
| Filter logic in `Providers/AzureRM/` only | ✅ | — | `AzureResourceIdCaseChangeFilter.cs` |
| `ignore_case_changes` Scriban variable | ✅ | ✅ TC-14 | `AotScriptObjectMapper` + `ReportModel` |
| Non-azurerm provider NOT filtered | ✅ | ✅ TC-16, TC-19 | Provider regex guard |
| README updated | ✅ | — | New section `#case-insensitive-azure-resource-id-filter` |

**Spec Deviations Found:** 1 (fixed — see Blockers)

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty input (no resource changes) | Pass | Handled by existing pipeline |
| Null BeforeValue | Pass | TC-04, TC-20 — Guard 1 returns false |
| Null AfterValue | Pass | TC-05, TC-21 — Guard 1 returns false |
| Numeric attribute change | Pass | TC-06 — Not an Azure resource ID |
| Non-Azure-ID string casing | Pass | TC-15, TC-18 — `IsAzureResourceId()` returns false |
| Non-azurerm provider | Pass | TC-16, TC-19 — Provider regex guard |
| Empty filter registry | Pass | TC-22 — Always returns false |
| All-filtered azurerm_role_assignment | ✅ Fixed | Was re-populating from raw data — see Blocker B-01 |
| Genuine content change not suppressed | Pass | TC-03 — OrdinalIgnoreCase equality check |

---

## Review Decision

**Status:** ✅ Approved (after fixes applied during review)

---

## Snapshot Changes

- Snapshot files changed: No
- `SNAPSHOT_UPDATE_OK` token: N/A

---

## Issues Found

### Blockers

#### B-01 — `RoleAssignmentViewModelFactory` bypasses filter via `BuildDefaultAttributes()` fallback ✅ FIXED

**File:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`

**Problem:** When `--ignore-case-changes` filters all attribute changes for an
`azurerm_role_assignment` resource, `attributeChanges` is empty (`Count == 0`). The factory's
existing fallback:

```csharp
var allAttributes = attributeChanges.Count > 0
    ? attributeChanges
    : BuildDefaultAttributes();  // ← Re-adds scope, role_definition_id etc. from raw JSON
```

…triggered `BuildDefaultAttributes()`, which injected a default list of attribute names and then
called `FormatRoleValue()` with the raw `ResourceChange` JSON — **bypassing the filter entirely**.
The rendered output would show the casing-only rows that were supposed to be suppressed.

**Verified:** Running `tfplan2md uat-plan.json --ignore-case-changes` before the fix showed
the `scope` and `role_definition_id` rows still appearing for `azurerm_role_assignment.app_contributor`
despite both values differing only in casing.

**Root cause:** The factory's `BuildDefaultAttributes()` fallback was designed for no-op resources
with no attribute changes. Post-filter, an update action with all changes filtered also produces
`Count == 0`, incorrectly triggering the fallback.

**Fix applied:** Changed the condition to only fall back to defaults when the action is neither
`update` nor `replace` (the only actions for which casing-only filtering can produce an empty list):

```csharp
var allAttributes = (attributeChanges.Count > 0
                     || action == UpdateAction
                     || action == ReplaceAction)
    ? attributeChanges
    : BuildDefaultAttributes();
```

Additionally added `NoOpAction` constant (removed by subsequent refactor) and constants
`UpdateAction = "update"` and `ReplaceAction = "replace"`. Added two new tests:
- `Build_WhenUpdateActionAndEmptyAttributeChanges_SmallAttributesIsEmpty`
- `Build_WhenReplaceActionAndEmptyAttributeChanges_SmallAttributesIsEmpty`

**Unit tests do not catch rendering bugs:** TC-02 checks `resource.AttributeChanges` (model
level), not the rendered template output. Rendering bugs in provider view model factories require
end-to-end rendering tests or manual verification. The factory unit tests call `Build()` directly
with `attributeChanges: []` in multiple tests — distinguishing "empty due to filter" from
"empty as test input" required analysing action type semantics.

---

### Major Issues

#### M-01 — UAT plan artifacts missing ✅ FIXED

**Requirement:** `docs/features/103-azure-id-case-insensitive-filter/uat-test-plan.md` requires
`uat-plan.json` and `uat-plan.md` to be present for UAT testing.

**Problem:** Neither artifact existed. The Developer work log mentions "updated: all demo
artifacts" but the UAT plan artifacts were not created.

**Fix applied:** Created `uat-plan.json` with two `azurerm_role_assignment` resources:
- `app_contributor`: all attribute changes are Azure ID casing-only → all suppressed
- `storage_reader`: mixed changes (casing-only scope + genuine description change)

Generated `uat-plan.md` using `tfplan2md uat-plan.json --ignore-case-changes`. The rendered
output correctly demonstrates:
- `app_contributor` appears with no attribute table rows (all suppressed)
- `storage_reader` shows only the `description` change (casing rows suppressed)

#### M-02 — `docs/architecture.md` not updated for new `IAttributeChangeFilter` pattern ✅ FIXED

The `IAttributeChangeFilter`/`AttributeChangeFilterRegistry` extension point is a significant
new architectural pattern (equivalent to `IValueFormatter`/`ValueFormatterRegistry`). The global
`docs/architecture.md` documented the `ValueFormatterRegistry` but did not mention the new filter
registry, leaving the architecture diagram and table out of date.

**Fix applied:**
1. Added `AttributeChangeFilterRegistry.cs`, `IAttributeChangeFilter.cs`, and
   `AttributeChangeFilterContext.cs` to the `Services/` file tree section
2. Added `AttributeChangeFilterRegistry` to the `Services/` directory table entry
3. Added `RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry) { }` to the
   `IProviderModule` code snippet

---

### Minor Issues

#### m-01 — Filter receives display values, not raw values (architecture discrepancy)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` (line 140)

The `AttributeChangeFilterContext` is documented in the architecture spec as carrying "raw value
from the plan's 'before' state" and "raw value from the plan's 'after' state". However, the
implementation passes `beforeDisplay` and `afterDisplay` (which are potentially masked values
like `"(sensitive)"` when `--show-sensitive` is inactive).

**Impact:** No incorrect behavior in practice because:
- Sensitive attributes → `beforeDisplay = afterDisplay = "(sensitive)"` → `IsAzureResourceId("(sensitive)") = false` → Guard 3 fails → not suppressed (correct)
- Known-after-apply → modified display value → not an Azure resource ID → Guard 3 fails → not suppressed (correct)

**Recommendation:** The architecture doc should be updated to note that the filter receives
display-format values, OR the implementation should be updated to pass raw values and re-document
the architecture. Since the current behavior is safe and the filter's Guard 3 prevents any
incorrect suppression, this is low priority. Not fixed in this review.

#### m-02 — Help text doesn't mention "Azure resource IDs only"

**File:** `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` (line 33)

Help text: `"Suppress attribute changes where before/after values differ only in casing."`

This is technically incomplete — the flag only suppresses Azure resource ID attributes, not all
casing-only string changes. A user might expect it to suppress `"MyApp"` → `"myapp"` changes,
which it does not. However, the README has detailed documentation, and CLI help text is
necessarily brief. Not fixed; acceptable as-is.

---

### Suggestions

#### S-01 — No rendering-level test for `azurerm_role_assignment` filter behavior

The existing tests check `resource.AttributeChanges` (model level) but not the rendered markdown
output. This means the B-01 bug was not caught by tests. A snapshot or integration test that
renders an `azurerm_role_assignment` resource with `--ignore-case-changes` and verifies the
attribute table is empty would provide deeper regression protection.

#### S-02 — No test for sensitive attribute interaction with `--ignore-case-changes`

There is no test verifying that a sensitive Azure resource ID attribute with a casing-only change
is NOT suppressed (because its display value is `"(sensitive)"`, not an Azure resource ID).
While the behavior is correct due to Guard 3, an explicit test would document this intentional
edge case.

---

## Critical Questions Answered

- **What could make this code fail?**
  The B-01 bug was the main failure path: view model factories with `BuildDefaultAttributes()`
  fallbacks can bypass the filter when all attribute changes are filtered out. Fixed by scoping
  the fallback to non-update/replace actions.

- **What edge cases might not be handled?**
  The `attributeChanges` context uses display values (minor discrepancy). Sensitive Azure
  resource IDs are handled correctly by Guard 3 (not suppressed). Other provider modules with
  similar `BuildDefaultAttributes()` patterns could have the same issue — these were checked and
  no other factories have this pattern.

- **Are all error paths tested?**
  The filter's five guards are all tested in `AzureResourceIdCaseChangeFilterTests.cs` (TC-17–TC-21).
  The registry's empty/OR semantics are tested in `AttributeChangeFilterRegistryTests.cs` (TC-22–TC-24).

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ (after B-01 fix) |
| Spec Compliance | ✅ (all criteria met) |
| Code Quality | ✅ |
| Architecture | ✅ (docs/architecture.md updated) |
| Testing | ✅ (1,299 tests pass) |
| Documentation | ✅ (README, features.md, architecture.md, UAT plan) |
| CHANGELOG not modified | ✅ |
| Markdownlint | ✅ 0 errors |
| CodeQL | ✅ 0 alerts |

---

## Work Protocol & Documentation Verification

| Document | Status |
|----------|--------|
| `work-protocol.md` exists | ✅ |
| All required agents logged | ✅ Requirements Engineer, Architect, Quality Engineer, Task Planner (×2), Developer, Technical Writer, Release Manager |
| `docs/features.md` updated | ✅ New section added by Technical Writer |
| `docs/architecture.md` updated | ✅ Fixed during this review (file tree, table, IProviderModule snippet) |
| `docs/testing-strategy.md` | ✅ No new test frameworks or patterns requiring updates |
| `README.md` updated | ✅ Features list + `#case-insensitive-azure-resource-id-filter` section |
| UAT plan artifacts | ✅ Created during this review (`uat-plan.json`, `uat-plan.md`) |

---

## Next Steps

Feature is **approved**. The following fixes were applied during review:
1. ✅ `RoleAssignmentViewModelFactory.Build` — fix `BuildDefaultAttributes()` bypass (B-01)
2. ✅ UAT plan artifacts created (`uat-plan.json`, `uat-plan.md`) (M-01)
3. ✅ `docs/architecture.md` updated with new extension point (M-02)
4. ✅ Two new tests for factory behavior added

Since this feature has user-facing rendering changes (attribute table filtering), the **UAT Tester**
agent should validate the rendered output in a real GitHub and Azure DevOps PR before release.

The minor issue m-01 (display values vs raw values in filter context) is a documentation
discrepancy with no behavioral impact and can be addressed in a follow-up if desired.
