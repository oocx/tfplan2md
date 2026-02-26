# Test Plan: Case-Insensitive Attribute Change Filter

## Overview

This test plan covers the `--ignore-case-changes` CLI flag introduced by feature 103. When enabled, the flag suppresses attribute change rows where the before and after values are equal under case-insensitive (ordinal) comparison. The filter is disabled by default and takes precedence over `--show-unchanged-values` for casing-only rows.

**Reference:** [specification.md](./specification.md) | [architecture.md](./architecture.md)

The implementation follows the identical pattern established by `--show-unchanged-values` (feature 014); test cases mirror `ReportModelBuilderUnchangedValuesTests.cs` and `CliParserTests.cs`.

---

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| CLI flag `--ignore-case-changes` appears in help text | TC-10 | Unit |
| Flag absent → no regression; output identical to current behavior | TC-01, TC-11 | Unit |
| Flag present → casing-only rows suppressed | TC-02, TC-03 | Unit |
| All rows casing-only → attribute table has no rows | TC-02 | Unit |
| Mixed changes → only casing-only rows suppressed; genuine changes remain | TC-03 | Unit |
| Non-string values (numbers, booleans) unaffected | TC-06 | Unit |
| Null before value → row shown normally (no case comparison) | TC-04 | Unit |
| Null after value → row shown normally (no case comparison) | TC-05 | Unit |
| `--ignore-case-changes` takes precedence over `--show-unchanged-values` | TC-07 | Unit |
| `ReportModel.IgnoreCaseChanges` reflects the flag value | TC-12, TC-13 | Unit |
| `CliOptions.IgnoreCaseChanges` defaults to `false` | TC-09 | Unit |
| `--ignore-case-changes` flag sets `IgnoreCaseChanges = true` in parsed options | TC-08 | Unit |
| Filter is consistent across all attribute change tables | TC-03 (multi-resource plan) | Unit |
| `ignore_case_changes` Scriban variable accessible in templates | TC-14 | Unit |

---

## Test Cases

### New file: `ReportModelBuilderIgnoreCaseChangesTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderIgnoreCaseChangesTests.cs`

Test data dependency: `TestData/azurerm-case-only-ids-plan.json` (see [Test Data Requirements](#test-data-requirements) below).

---

#### TC-01: Flag absent — casing-only rows are included (no regression)

**Type:** Unit

**Maps to criterion:** "Flag absent → no regression; output identical to current behavior"

**Description:**
When `ignoreCaseChanges` is `false` (the default), attribute rows where before and after values differ only in casing are included in the model, not suppressed.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with at least one attribute where before and after values differ only in casing (e.g., `scope`: `/subscriptions/ABC123/…` vs `/subscriptions/abc123/…`).

**Test Method:** `Build_IgnoreCaseChangesFalse_IncludesCasingOnlyRows`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: false)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the resource with casing-only changes.

**Expected Result:**
- The casing-only attribute row(s) are present in `AttributeChanges` (count matches the known total changed attributes).

---

#### TC-02: All rows casing-only — attribute table is empty after suppression

**Type:** Unit

**Maps to criterion:** "Flag present → all rows casing-only → attribute table has no rows"

**Description:**
When `ignoreCaseChanges` is `true` and every attribute change in a resource is a casing-only difference, the resulting `AttributeChanges` collection is empty.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource whose only attribute changes are casing-only (e.g., two Azure resource IDs that differ only in capitalisation).

**Test Method:** `Build_IgnoreCaseChangesTrue_AllCasingOnly_AttributeChangesEmpty`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the all-casing-only resource.

**Expected Result:**
- `AttributeChanges` is empty (count = 0) for that resource.

---

#### TC-03: Mixed changes — only casing-only rows suppressed; genuine changes remain

**Type:** Unit

**Maps to criterion:** "Mixed changes → only casing-only rows suppressed; genuine changes remain"

**Description:**
When `ignoreCaseChanges` is `true` and a resource has both casing-only and genuine attribute changes, only the casing-only rows are suppressed. Genuine changes remain visible.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with at least one casing-only attribute change (e.g., `scope`) AND at least one genuine change (e.g., `display_name` changes from `"My App"` to `"My Application"`).

**Test Method:** `Build_IgnoreCaseChangesTrue_MixedChanges_RetainsGenuineChanges`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the mixed-changes resource.

**Expected Result:**
- The genuine change attribute (e.g., `display_name`) is present in `AttributeChanges`.
- The casing-only attribute (e.g., `scope`) is absent from `AttributeChanges`.
- `AttributeChanges.Count` equals the number of genuine changes only.

---

#### TC-04: Null before value — row is shown normally

**Type:** Unit

**Maps to criterion:** "Null before value → row shown normally (no case comparison)"

**Description:**
When a before value is `null`, `isCasingOnlyChange` must evaluate to `false` regardless of the after value, so the row is treated like any other changed attribute.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with an attribute whose before value is `null` and after value is a non-null string.

**Test Method:** `Build_IgnoreCaseChangesTrue_NullBeforeValue_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the attribute with a null before value.

**Expected Result:**
- The attribute row with null before is present in `AttributeChanges` (not suppressed).

---

#### TC-05: Null after value — row is shown normally

**Type:** Unit

**Maps to criterion:** "Null after value → row shown normally (no case comparison)"

**Description:**
When an after value is `null`, `isCasingOnlyChange` must evaluate to `false` regardless of the before value.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with an attribute whose before value is a non-null string and after value is `null`.

**Test Method:** `Build_IgnoreCaseChangesTrue_NullAfterValue_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the attribute with a null after value.

**Expected Result:**
- The attribute row with null after is present in `AttributeChanges` (not suppressed).

---

#### TC-06: Non-string values (numbers/booleans) — unaffected by filter

**Type:** Unit

**Maps to criterion:** "Non-string values unaffected"

**Description:**
Numbers and booleans in Terraform plan JSON are already lowercase and ordinal-equal when unchanged. When they do change (e.g., `42` → `43`), `valuesEqual` is `false` AND ordinal case-insensitive comparison of the string representations also differs (`"42" ≠ "43"`), so `isCasingOnlyChange` is `false` and the row is included normally.

This test verifies that a numeric attribute change (e.g., `soft_delete_retention_days`: `7` → `14`) is not suppressed when `ignoreCaseChanges: true`.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with a numeric attribute that genuinely changes value.

**Test Method:** `Build_IgnoreCaseChangesTrue_NumericAttributeChange_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the numeric attribute that changes value.

**Expected Result:**
- The numeric attribute change row is present in `AttributeChanges`.

---

#### TC-07: `--ignore-case-changes` takes precedence over `--show-unchanged-values`

**Type:** Unit

**Maps to criterion:** "Rows suppressed by `--ignore-case-changes` remain hidden even when `--show-unchanged-values` is also passed"

**Description:**
Even when `showUnchangedValues: true` is set (which would normally surface rows where before == after), casing-only rows are still hidden because `isCasingOnlyChange` guard executes before the `valuesEqual` guard and causes an unconditional `continue`.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with casing-only attribute changes.

**Test Method:** `Build_IgnoreCaseChangesTrue_AndShowUnchangedValues_CasingRowsStillSuppressed`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: true, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the casing-only resource.

**Expected Result:**
- Casing-only rows are absent from `AttributeChanges` (suppressed despite `showUnchangedValues: true`).
- Truly unchanged rows (before == after ordinal) ARE present (because `showUnchangedValues: true` affects those).
- Genuine change rows are present.

---

### Updates to `CliParserTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

---

#### TC-08: `--ignore-case-changes` flag sets `IgnoreCaseChanges = true`

**Type:** Unit

**Maps to criterion:** "CLI flag `--ignore-case-changes` is implemented"

**Test Method:** `Parse_IgnoreCaseChangesFlag_SetsIgnoreCaseChangesTrue`

**Test Steps:**
1. Call `CliParser.Parse(new[] { "--ignore-case-changes" })`.

**Expected Result:**
- `options.IgnoreCaseChanges.Should().BeTrue()`

---

#### TC-09: Default options — `IgnoreCaseChanges` defaults to `false`

**Type:** Unit

**Maps to criterion:** "Flag absent → no regression; flag is disabled by default"

**Description:**
Update the existing `Parse_NoArgs_ReturnsDefaultOptions` test to include an assertion that `options.IgnoreCaseChanges.Should().BeFalse()`.

**Test Method:** Update `Parse_NoArgs_ReturnsDefaultOptions` (add one assertion line)

**Expected Result:**
- `options.IgnoreCaseChanges.Should().BeFalse()`

---

### Updates to `HelpTextProviderTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/CLI/HelpTextProviderTests.cs`

---

#### TC-10: Help text includes `--ignore-case-changes` option

**Type:** Unit

**Maps to criterion:** "Help text documents the new flag"

**Test Method:** `GetHelpText_IncludesIgnoreCaseChangesOption`

**Test Steps:**
1. Call `HelpTextProvider.GetHelpText()`.

**Expected Result:**
- `help.Should().Contain("--ignore-case-changes")`
- `help.Should().Contain("casing")` (description references casing)

---

### Additional tests for `ReportModelBuilderIgnoreCaseChangesTests.cs`

---

#### TC-11: Default model — `IgnoreCaseChanges` is `false` when not specified

**Type:** Unit

**Maps to criterion:** "`ReportModel.IgnoreCaseChanges` reflects the flag value"

**Test Method:** `Build_Default_IgnoreCaseChangesFalseInModel`

**Test Steps:**
1. Parse any valid plan JSON.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false)` (no `ignoreCaseChanges` argument — uses default).
3. Call `Build(plan)`.

**Expected Result:**
- `model.IgnoreCaseChanges.Should().BeFalse()`

---

#### TC-12: Model reflects `IgnoreCaseChanges = true` when specified

**Type:** Unit

**Maps to criterion:** "`ReportModel.IgnoreCaseChanges` reflects the flag value"

**Test Method:** `Build_WithIgnoreCaseChangesTrue_ModelReflectsFlag`

**Test Steps:**
1. Parse any valid plan JSON.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.

**Expected Result:**
- `model.IgnoreCaseChanges.Should().BeTrue()`

---

#### TC-13: Identical values (ordinal equal) — unchanged-value logic unchanged by the new flag

**Type:** Unit

**Maps to criterion:** "Flag absent → no regression"

**Description:**
Verifies that the `isCasingOnlyChange` guard does NOT interfere with the existing `valuesEqual` guard. When before == after (ordinal), those rows are still subject to the existing `!_showUnchangedValues` filter exactly as before.

**Test Method:** `Build_IgnoreCaseChangesTrue_OrdinallyEqualValues_BehavesLikeUnchanged`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Check an attribute that is genuinely unchanged (before == after, ordinal).

**Expected Result:**
- The attribute with ordinal-equal before/after is absent from `AttributeChanges` (hidden by existing `valuesEqual` filter — unchanged by this feature).

---

#### TC-14: Scriban `ignore_case_changes` variable is accessible to templates

**Type:** Unit

**Maps to criterion:** "Template authors can access the flag value to implement the filter in custom templates"

**Description:**
`AotScriptObjectMapper` must expose `ignore_case_changes` as a Scriban variable when `IgnoreCaseChanges = true`. This can be verified by creating a minimal Scriban template that accesses `ignore_case_changes` and asserting the rendered output contains the expected value.

**Test Method:** `Render_IgnoreCaseChangesTrue_ScribanVariableIsTrue`

**Test Steps:**
1. Parse any valid plan JSON.
2. Build model with `ignoreCaseChanges: true`.
3. Render with a custom inline Scriban template: `"{{ ignore_case_changes }}"`.
4. Capture rendered output string.

**Expected Result:**
- Rendered output equals `"true"` (Scriban renders `true` as the string `"true"`).

**Note:** If a minimal renderer helper is not available, this can alternatively be verified by testing that `AotScriptObjectMapper` sets the expected key when mapping a `ReportModel` with `IgnoreCaseChanges = true`. Use whichever approach best matches existing Scriban mapper test patterns.

---

## Test Data Requirements

### New file: `azurerm-case-only-ids-plan.json`

**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-case-only-ids-plan.json`

**Description:**
A minimal Terraform plan JSON exercising all test scenarios above. Must include:

| Resource | Purpose | Attributes Required |
|----------|---------|---------------------|
| `azurerm_role_assignment.casing_only` | TC-01, TC-02 | `scope`: before `/subscriptions/ABC123/resourceGroups/my-rg`, after `/subscriptions/abc123/resourceGroups/my-rg`; `role_definition_id`: before `/providers/Microsoft.Authorization/roleDefinitions/XYZ`, after `/providers/Microsoft.Authorization/roleDefinitions/xyz` |
| `azurerm_role_assignment.mixed_changes` | TC-03, TC-07 | `scope` (casing-only, same as above); `display_name`: before `"My App"`, after `"My Application"` (genuine change) |
| `azurerm_key_vault.null_before` | TC-04 | `tenant_id`: before `null`, after `"tenant-abc"` |
| `azurerm_key_vault.null_after` | TC-05 | `tenant_id`: before `"tenant-abc"`, after `null` |
| `azurerm_key_vault.numeric_change` | TC-06 | `soft_delete_retention_days`: before `7`, after `14` (numeric genuine change) |
| `azurerm_key_vault.unchanged` | TC-13 | `name`: before `"my-vault"`, after `"my-vault"` (ordinal equal, unchanged) |

The plan must use action `["update"]` for all resources so they appear in the changes list.

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| All attribute changes are casing-only | Attribute table has 0 rows | TC-02 |
| Mix of casing-only and genuine changes | Only genuine changes in attribute table | TC-03 |
| Null before value | Row not suppressed (case comparison skipped) | TC-04 |
| Null after value | Row not suppressed (case comparison skipped) | TC-05 |
| Numeric attribute change | Row not suppressed (values are not case-equivalent) | TC-06 |
| Flag + `--show-unchanged-values` combined | Casing-only rows still hidden | TC-07 |
| Flag absent (default) | No change in existing behavior | TC-01, TC-09 |
| Ordinal-equal values (`valuesEqual = true`) | Still hidden by existing unchanged filter (independent of new flag) | TC-13 |
| `ignore_case_changes` Scriban variable | Set to `true` when flag is active | TC-14 |

---

## Non-Functional Tests

None required for this feature. The filter is a simple boolean guard in a tight loop — no performance or compatibility concerns beyond existing test infrastructure.

---

## Open Questions

None. The architecture document resolves all implementation ambiguities.
