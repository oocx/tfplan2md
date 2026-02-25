# Test Plan: Known-After-Apply Rendering

## Overview

Tests verify that tfplan2md correctly surfaces computed Terraform attributes (`after_unknown: true`) in rendered reports instead of silently dropping them. This covers two root causes: (1) `ReportModelBuilder.BuildAttributeChanges` skipping attributes when `before == null && after == null`, and (2) `AzureAdSummaryBuilder` producing blank summary lines when group/member IDs are computed.

Reference: [specification.md](specification.md) and [architecture.md](architecture.md).

**Test Framework:** TUnit 1.9.26  
**Assertion Library:** AwesomeAssertions  
**Test Execution:** `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`

---

## Test Coverage Matrix

| Acceptance Criterion (from spec) | Test Case(s) | Test Type | Location |
|---|---|---|---|
| Computed attributes show `(known after apply)` in attribute tables | TC-01 to TC-04 | Unit | `MarkdownGeneration/AfterUnknownHelperTests.cs` |
| Reference selection priority: static > each.value.attr > var > null | TC-05 to TC-11 | Unit | `MarkdownGeneration/ReferenceSelectorTests.cs` |
| Useless meta-references (`each.key`, `count.index`, `self`) are skipped | TC-09 | Unit | `MarkdownGeneration/ReferenceSelectorTests.cs` |
| Reference strips trailing `.attribute` for table display | TC-10 | Unit | `MarkdownGeneration/ReferenceSelectorTests.cs` |
| `SelectResourceLevelReference` returns only static resource refs | TC-11 | Unit | `MarkdownGeneration/ReferenceSelectorTests.cs` |
| Scenario 1: All-unknown AzureAD group member, no config → `(known after apply)` labels | TC-12 | Integration (snapshot) | `Providers/AzureAD/AzureAdGroupMemberComputedTests.cs` |
| Scenario 2: Static config references in table and summary | TC-13 | Integration (snapshot) | `Providers/AzureAD/AzureAdGroupMemberComputedTests.cs` |
| Scenario 3: for_each string key in summary, `each.value.*` in table | TC-14 | Integration (snapshot) | `Providers/AzureAD/AzureAdGroupMemberComputedTests.cs` |
| Scenario 4: Mixed known/computed attributes | TC-15 | Integration (snapshot) | `Providers/AzureAD/AzureAdGroupMemberComputedTests.cs` |
| Scenario 5: Numeric instance key + static ref appended in summary | TC-16 | Integration (snapshot) | `Providers/AzureAD/AzureAdGroupMemberComputedTests.cs` |
| Scenario 6: Generic resource with computed `id` in `after` | TC-17 | Integration (snapshot) | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Scenario 6b: Generic resource where `id` absent from `after` → not in table | TC-18 | Integration | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Scenario 7: Sensitive + computed → `🔒(known after apply)` in After column | TC-19, TC-20 | Integration (snapshot) | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Scenario 7: Computed attribute counted in update summary | TC-21 | Integration | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Scenario 8: Whole-resource unknown → no `_No attribute changes._` | TC-22 | Integration (snapshot) | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Scenario 9: Child resource with computed ChildReferenceAttribute renders standalone | TC-23 | Integration | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Computed attrs on create NOT counted in attribute-level change count | TC-24 | Integration | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Attributes absent from `after` are NOT added even when present in `after_unknown` | TC-18 | Integration | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Resources with only known values still render without any `(known after apply)` rows | TC-25 | Regression | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Configuration reference strings are never sensitive values | TC-26 | Invariant | `MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs` |
| Existing snapshot tests continue to pass | TC-27 | Regression | All snapshot test classes |

---

## Test Cases

### TC-01: `IsWholeResourceUnknownAfterApply_WhenAfterUnknownIsTrue_ReturnsTrue`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AfterUnknownHelperTests.cs`

**Description:** When `afterUnknown` is a JSON `true` boolean (whole-resource unknown), the method returns `true`.

**Inputs:**
```json
{ "after_unknown": true }
```
`afterUnknown` deserialized as `JsonElement` with `ValueKind = True`.

**Expected Result:** `AfterUnknownHelper.IsWholeResourceUnknownAfterApply(afterUnknown)` returns `true`.

---

### TC-02: `IsWholeResourceUnknownAfterApply_WhenAfterUnknownIsObject_ReturnsFalse`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AfterUnknownHelperTests.cs`

**Description:** When `afterUnknown` is a JSON object (attribute-level unknowns), the method returns `false`.

**Inputs:** `afterUnknown` = `{ "id": true }` as `JsonElement`.

**Expected Result:** Returns `false`.

---

### TC-03: `IsAttributeUnknownAfterApply_SimpleAttributeMarkedTrue_ReturnsTrue`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AfterUnknownHelperTests.cs`

**Description:** A simple top-level key `"group_object_id"` marked as `true` in the unknown tree is detected.

**Inputs:** `afterUnknown` = `{ "group_object_id": true }`, `flattenedKey` = `"group_object_id"`.

**Expected Result:** Returns `true`.

**Additional sub-cases:**
- Nested key `"tags.env"` when `afterUnknown` = `{ "tags": { "env": true } }` → `true`
- Array key `"rules[0].priority"` when `afterUnknown` = `{ "rules": [{ "priority": true }] }` → `true`
- Missing key `"location"` when `afterUnknown` = `{ "id": true }` → `false`
- Whole-object unknown intermediate node: `"tags.env"` when `afterUnknown` = `{ "tags": true }` → `true` (the whole subtree is unknown)
- `null` `afterUnknown` → `false` (never throws)
- Malformed/unexpected structure → `false` (never throws)

---

### TC-04: `IsAttributeUnknownAfterApply_AttributeNotMarked_ReturnsFalse`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AfterUnknownHelperTests.cs`

**Description:** An attribute present in `after` as `null` but NOT in `after_unknown` is not treated as computed.

**Inputs:** `afterUnknown` = `{ "id": true }`, `flattenedKey` = `"location"`.

**Expected Result:** Returns `false`.

---

### TC-05: `SelectBestReference_StaticResourceReference_ReturnsTypeDotName`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** When a static resource reference is present, it is selected with highest priority and stripped to `type.name`.

**Inputs:** `references = ["azuread_group.platform_engineers.object_id", "azuread_group.platform_engineers"]`

**Expected Result:** `"azuread_group.platform_engineers"` (strips the `.object_id` suffix).

---

### TC-06: `SelectBestReference_EachValueAttributeRef_WhenNoStaticRef`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** When no static resource ref is available, `each.value.<attr>` is selected (priority 2).

**Inputs:** `references = ["each.value.group_object_id", "each.value"]`

**Expected Result:** `"each.value.group_object_id"`.

---

### TC-07: `SelectBestReference_VarReference_WhenNoStaticOrEachRef`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** When no static or `each.value.attr` refs are present, `var.<name>` is returned (priority 3).

**Inputs:** `references = ["count.index", "var.users"]`

**Expected Result:** `"var.users"`.

---

### TC-08: `SelectBestReference_LocalReference_WhenNoHigherPriority`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** `local.<name>` reference is selected at priority 3 when no higher-priority refs exist.

**Inputs:** `references = ["local.tenant_prefix"]`

**Expected Result:** `"local.tenant_prefix"`.

---

### TC-09: `SelectBestReference_UselessMetaReferences_ReturnsNull`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** Useless bare meta-references are all skipped and `null` is returned.

**Inputs:** `references = ["each.key", "each.value", "count.index", "self"]`

**Expected Result:** `null`.

---

### TC-10: `SelectBestReference_ThreePartStaticRef_StripsAttributeSuffix`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** A 3-part reference like `azuread_group.admins.object_id` is stripped to `azuread_group.admins` (Invariant 6).

**Inputs:** `references = ["azuread_group.admins.object_id"]`

**Expected Result:** `"azuread_group.admins"`.

---

### TC-11: `SelectResourceLevelReference_OnlyStaticRefs_NoEachOrVar`

**Type:** Unit  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`

**Description:** `SelectResourceLevelReference` returns only static resource references and `null` for `each.value.*`-only lists (used for summary line generation).

**Sub-cases:**
- `["azuread_group.admins.object_id", "azuread_group.admins"]` → `"azuread_group.admins"`
- `["each.value.group_object_id", "each.value"]` → `null`
- `["var.tenant_id"]` → `null`
- Module-qualified: `["module.identity.azuread_user.admin.object_id", "module.identity.azuread_user.admin"]` → `"module.identity.azuread_user.admin"`

---

### TC-12: Scenario 1 — All-Unknown AzureAD Group Member, No Configuration

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-1.json`

**Description:** `azuread_group_member` with all IDs unknown and no configuration block renders with `(known after apply)` labels throughout (Scenario 1).

**Plan shape:**
```json
{
  "address": "azuread_group_member.all_unknown",
  "change": {
    "actions": ["create"],
    "before": null,
    "after": { "group_object_id": null, "member_object_id": null, "id": null },
    "after_unknown": { "group_object_id": true, "member_object_id": true, "id": true }
  }
}
```
No `configuration` block.

**Expected summary line contains:**
```
azuread_group_member all_unknown — (known after apply) → (known after apply)
```

**Expected attribute table contains rows:**
- `group_object_id` = `` `(known after apply)` ``
- `id` = `` `(known after apply)` ``
- `member_object_id` = `` `(known after apply)` ``

**Expected:** `_No attribute changes._` NOT present in the rendered section.

---

### TC-13: Scenario 2 — Static Resource References in Configuration

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-2.json`

**Description:** AzureAD group member with static config references shows resource-level label in summary and per-attribute labels in table (Scenario 2).

**Expected summary line contains:**
```
azuread_group_member platform_admin_member — azuread_group.platform_engineers → azuread_user.admin
```

**Expected attribute table contains rows:**
- `group_object_id` = `` `(known after apply: azuread_group.platform_engineers)` ``
- `id` = `` `(known after apply)` `` (no config ref for `id`)
- `member_object_id` = `` `(known after apply: azuread_user.admin)` ``

---

### TC-14: Scenario 3 — for_each With String Instance Key, No Static Resource Ref

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-3.json`

**Description:** for_each group member with string instance key and `each.value.*` refs in configuration (Scenario 3).

**Expected summary line contains:**
```
azuread_group_member user_groups — "team-example - user@example.de" → "team-example - user@example.de"
```

**Expected attribute table contains rows:**
- `group_object_id` = `` `(known after apply: each.value.group_object_id)` ``
- `member_object_id` = `` `(known after apply: each.value.user_object_id)` ``

---

### TC-15: Scenario 4 — Mixed Known and Computed Attributes

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-4.json`

**Description:** When `group_object_id` is computed but `member_object_id` is a known value, both are rendered correctly (Scenario 4).

**Plan shape:** `after = { "group_object_id": null, "member_object_id": "user-200", "id": null }`, `after_unknown = { "group_object_id": true, "id": true }`.

**Expected summary:** `— (known after apply) → user-200`

**Expected attribute table contains rows:**
- `group_object_id` = `` `(known after apply)` ``
- `id` = `` `(known after apply)` ``
- `member_object_id` = `` `user-200` ``

---

### TC-16: Scenario 5 — Numeric Instance Key Appended to Static Group Reference

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-5.json`

**Description:** count-based group member with address `members[0]`, static group ref, `var.users` member ref (Scenario 5).

**Expected summary:** `— azuread_group.admins[0] → (known after apply)`  
(Numeric key appended to static ref, NOT used alone as a label.)

**Expected attribute table contains rows:**
- `group_object_id` = `` `(known after apply: azuread_group.admins)` `` (no numeric key in table value)
- `member_object_id` = `` `(known after apply: var.users)` ``

---

### TC-17: Scenario 6a — Generic Resource With Computed `id` Present in `after`

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-6a.json`

**Description:** `azurerm_resource_group` with `id: null` in `after` and `after_unknown: { "id": true }` — `id` appears in the attribute table (Scenario 6, first shape).

**Expected attribute table contains row:** `id` = `` `(known after apply)` ``

**Expected:** `location` and `name` rows also present with their formatted values.

---

### TC-18: Scenario 6b — Attribute Absent From `after` Not Added

**Type:** Integration  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-6b.json`

**Description:** When `id` is in `after_unknown` but **absent** from `after`, it does NOT appear in the attribute table (Decision A1, Scenario 6, second shape).

**Plan shape:** `after = { "location": "eastus", "name": "rg-demo" }`, `after_unknown = { "id": true }` (`id` absent from `after`).

**Expected:** Attribute table does NOT contain a row for `id`. Table contains only `location` and `name`.

---

### TC-19: Scenario 7a — Sensitive + Computed Attribute Shows Lock Icon

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-7.json`

**Description:** An attribute that is both sensitive (`before_sensitive: true`) and computed (`after_unknown: true`) shows `🔒(known after apply)` in the After column (Scenario 7).

**Plan shape:** `before = { "primary_access_key": "abc123" }`, `after = { "primary_access_key": null }`, `after_unknown = { "primary_access_key": true }`, `before_sensitive = { "primary_access_key": true }`.

**Expected After column for `primary_access_key`:** `` `🔒(known after apply)` ``

**Expected Before column for `primary_access_key`:** `` `(sensitive)` `` (never exposes the actual before value).

---

### TC-20: Scenario 7b — Sensitive+Computed Attribute Explicitly NOT Revealing Before Value

**Type:** Integration  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`

**Description:** Even when `--show-sensitive` is enabled, the before value of an attribute that is computed after apply still shows `(sensitive)` for before (Invariant 10).

**Expected:** After column = `` `🔒(known after apply)` ``. Before column = `` `(sensitive)` ``. The raw value `"abc123"` must not appear anywhere in the rendered output.

---

### TC-21: Scenario 7c — Computed Attribute Counted in Update Summary

**Type:** Integration  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`

**Description:** For an update action, a computed attribute is included in `ChangedAttributesSummary` side-by-side with other changed attributes (Invariant 8, Scenario 7).

**Expected `ChangedAttributesSummary`:** `"2 🔧 account_replication_type, primary_access_key"` (or equivalent — both attributes listed).

---

### TC-22: Scenario 8 — Whole-Resource Unknown Does Not Show `_No attribute changes._`

**Type:** Integration (snapshot)  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-8.json`

**Description:** `null_resource` with `after: null` and `after_unknown: true` (root boolean). No attribute rows can be materialized, but `_No attribute changes._` placeholder is NOT shown (Scenario 8).

**Plan shape:** `after: null`, `after_unknown: true`.

**Expected:** Rendered section does NOT contain `_No attribute changes._`.  
**Expected:** No attribute table rows present.

---

### TC-23: Scenario 9 — Child Resource With Computed ChildReferenceAttribute Renders Standalone

**Type:** Integration  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`  
**Test data file:** `TestData/known-after-apply-scenario-9.json`

**Description:** `azurerm_subnet` whose `virtual_network_name` is computed cannot be merged under its `azurerm_virtual_network` parent and renders as a standalone resource (Scenario 9).

**Plan shape:** Parent `azurerm_virtual_network.hub` (create, `name: "hub-vnet"`). Child `azurerm_subnet.app` with `virtual_network_name: null`, `after_unknown: { "virtual_network_name": true }`.

**Expected:**
- Subnet renders as a standalone entry (not nested under the VNet block).
- Subnet attribute table contains row `virtual_network_name` = `` `(known after apply)` ``.

---

### TC-24: Computed Attributes on Create NOT Counted in Attribute-Level Change Count

**Type:** Integration  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`

**Description:** A create resource with computed attributes is counted once at resource level (➕ count). No per-attribute count appears on the summary line.

**Inputs:** Scenario 2 plan — `azuread_group_member.platform_admin_member` (create action, all IDs computed).

**Expected:** `ChangedAttributesSummary` is null/empty for create actions. Summary line does NOT contain a `🔧` change count.

---

### TC-25: Resources With Only Known Values Not Affected

**Type:** Regression  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`

**Description:** A resource with known (non-null) `after` values and no `after_unknown` entries does not generate any `(known after apply)` rows. The feature must not regress clean resources.

**Inputs:** A standard create resource with all attributes present and valued.

**Expected:** No attribute rows contain `(known after apply)`. Rendering is identical to pre-feature behavior.

---

### TC-26: Configuration Reference Strings Are Never Sensitive Values

**Type:** Invariant  
**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`

**Description:** References from `expressions.references` are Terraform expression paths (e.g., `"azuread_group.admins.object_id"`) — structural identifiers, not data values. The test asserts that a sensitive attribute whose before value is masked still shows a safe configuration reference path as the `(known after apply: ...)` label (never the actual secret before value, never an empty sensitive string).

**Inputs:** A resource with `before_sensitive: { "api_key": true }`, `after: { "api_key": null }`, `after_unknown: { "api_key": true }`, plus a configuration expression `references: ["var.api_secret"]`.

**Expected:**
- After column = `` `🔒(known after apply: var.api_secret)` ``
- Before column = `` `(sensitive)` ``

---

### TC-27: Existing Snapshot Tests Pass Unchanged (No Silent Regressions)

**Type:** Regression  
**File:** All existing snapshot test classes

**Description:** After implementing the feature, all existing snapshot tests that do NOT involve `after_unknown` fields continue to pass. Snapshot files that change because they contain `after_unknown` data must be reviewed and intentionally updated (see `scripts/update-test-snapshots.sh`).

**Expected:** Snapshot test suite passes. Any snapshot updates are verified to be intentional and correct.

---

## Test Data Requirements

New test data JSON files required (each matching one spec scenario):

| File | Contents |
|---|---|
| `TestData/known-after-apply-scenario-1.json` | Single `azuread_group_member.all_unknown` — all IDs null in `after` + all `after_unknown: true`, no `configuration` block |
| `TestData/known-after-apply-scenario-2.json` | `azuread_group_member.platform_admin_member` with static config refs for both IDs (mirrors spec Scenario 2) |
| `TestData/known-after-apply-scenario-3.json` | `azuread_group_member.user_groups["team-example - user@example.de"]` with `each.value.*` config refs |
| `TestData/known-after-apply-scenario-4.json` | `azuread_group_member.platform_admin_member` — `group_object_id` computed, `member_object_id` known `"user-200"` |
| `TestData/known-after-apply-scenario-5.json` | `azuread_group_member.members[0]` — static group ref, `count.index`+`var.users` member refs |
| `TestData/known-after-apply-scenario-6a.json` | `azurerm_resource_group.demo` — `id: null` in `after`, `after_unknown: { "id": true }` |
| `TestData/known-after-apply-scenario-6b.json` | `azurerm_resource_group.demo` — `id` absent from `after`, `after_unknown: { "id": true }` |
| `TestData/known-after-apply-scenario-7.json` | `azurerm_storage_account.data` update — `primary_access_key` sensitive+computed, `account_replication_type` changed |
| `TestData/known-after-apply-scenario-8.json` | `null_resource.app_config` — `after: null`, `after_unknown: true` (root boolean) |
| `TestData/known-after-apply-scenario-9.json` | `azurerm_virtual_network.hub` + `azurerm_subnet.app` — subnet's `virtual_network_name: null` + `after_unknown: true` |

> **Note:** `TestData/azuread-group-members-known-after-apply-plan.json` already exists and covers a subset of Scenarios 2/4. Evaluate whether it can serve as the primary test data for TC-13/TC-15 or whether separate scenario files are cleaner.

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|---|---|---|
| `afterUnknown` is `null` | `IsAttributeUnknownAfterApply` returns `false`, never throws | TC-03 |
| `afterUnknown` is a JSON `false` boolean | Returns `false` | TC-02 |
| Flattened key with array index: `"rules[0].protocol"` | Correctly navigates array + object path | TC-03 |
| Intermediate `true` node: `after_unknown = { "tags": true }` for key `"tags.env"` | Returns `true` (whole subtree unknown) | TC-03 |
| Empty references list | `SelectBestReference` returns `null` | TC-09 |
| Bare `each.value` (without attribute part) | Skipped as useless | TC-06 |
| Numeric instance key on child resource with no static ref | Numeric key is NOT used alone as a label | TC-16 |
| Child resource `ChildReferenceAttribute` is computed | Renders standalone, not nested | TC-23 |
| Sensitive+computed attribute — explicit before value never surfaced | Before = `(sensitive)`, After = `🔒(known after apply)` | TC-19, TC-20 |
| Whole-resource unknown (`after: null`, `after_unknown: true`) | No rows, no `_No attribute changes._` | TC-22 |

---

## Non-Functional Tests

### Architecture Compliance

No new dependencies from `MarkdownGeneration` to `Providers` are introduced. The new `AfterUnknownHelper` and `ReferenceSelector` classes live in `MarkdownGeneration/Helpers/`, consistent with `SensitivityHelper`. Architecture boundary tests must continue to pass.

### Never-Throw Contract

`AfterUnknownHelper` must never throw on unexpected or malformed `after_unknown` structures (e.g., missing properties, wrong types). This is covered by TC-03 and TC-04 sub-cases.

---

## Open Questions / Flagged Items

### OQ-01 — Scenario 8: What exactly is rendered when there are zero attribute rows and whole-resource unknown is true?

The specification states `_No attribute changes._` must NOT be shown. It also notes that "Future iterations may add a `(all values known after apply)` note". Currently, the spec says "render without an attribute table body" — no rows and no placeholder. 

**Impact on testing:** TC-22 currently asserts only the absence of `_No attribute changes._`. If the developer chooses to add a `(all values known after apply)` note, TC-22 must be extended to also verify its presence. This requires a decision before writing snapshot assertions.

**Recommended action:** Confirm with the Maintainer whether the current iteration should show any note for the whole-resource-unknown case, or simply render nothing.

### OQ-02 — Existing `azuread-group-members-known-after-apply-plan.json` Coverage

The existing test data file covers partial Scenarios 2 and 4 (computed `group_object_id`, known `member_object_id`). **Before creating new scenario-2 and scenario-4 test data files**, the Developer should evaluate whether to extend this file or create separate minimal scenario files. The test plan assumes separate files for clarity.
