# Test Plan: Separate Table for azapi Output Values

## Overview

This plan covers automated snapshot tests and UAT for Feature 106, which introduces a dedicated
**Output Values** section in the `azapi_resource` and `azapi_update_resource` rendered reports.
Tests verify correct rendering for all change actions, the "known after apply" notice, sensitivity
masking, Feature 034 attribute grouping, large-value handling, and absence of the section when no
output is present.

Reference: `docs/features/106-azapi-output-values/specification.md`

All automated tests follow the existing snapshot pattern used in `AzapiSnapshotTests.cs`:
1. A JSON plan file in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`
2. An approved `.md` snapshot in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`
3. A test method in `AzapiSnapshotTests.cs` using `AssertAzapiSnapshot(planFile, snapshotFile)`

---

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| `output` rendered for `azapi_resource` | TC-01, TC-02, TC-03, TC-05, TC-06 | Snapshot |
| `output` rendered for `azapi_update_resource` | TC-11 | Snapshot |
| Section heading clearly labelled "Output Values" | TC-02, TC-03, TC-05 | Snapshot |
| `after_unknown.output = true` → Output Values section suppressed entirely | TC-01 | Snapshot |
| Output absent → section omitted entirely | TC-07 | Snapshot (regression) |
| Feature 034 grouping applies to output values | TC-09 | Snapshot |
| Sensitivity masking on output values | TC-08 | Snapshot |
| Large-value handling on output values | TC-10 | Snapshot |
| Create action — output present at plan time | TC-02 | Snapshot |
| Update action — output changed | TC-03 | Snapshot |
| Update action — output unchanged (no-diff message) | TC-04 | Snapshot |
| Delete action — output from before state | TC-05 | Snapshot |
| Replace action — after unknown + before present | TC-06 | Snapshot |
| Body rendering unaffected by output changes | TC-12 | Regression |

---

## Test Cases

### TC-01: Create — output unknown at plan time

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputCreateUnknown_MatchesBaseline`

**Description:**
An `azapi_resource` create action where `after_unknown.output = true` (normal for creates
before apply). No output data is available and none is expected after apply at plan time,
so the entire Output Values section must be absent from the rendered markdown — no heading
and no notice are rendered.

**Test Data File:** `azapi-output-create-unknown-plan.json`

**Plan Requirements:**
- `change.actions = ["create"]`
- `change.before = null`
- `change.after` includes a `body` with a couple of properties
- `change.after_unknown = { "id": true, "output": true }` — the key condition
- No `output` key in `change.after`

**Expected Snapshot Output:**
The rendered markdown contains no `#### Output Values` heading and no "known after apply"
notice. The Output Values section is suppressed entirely.

```markdown
(no Output Values section rendered)
```

**Snapshot File:** `azapi-output-create-unknown.md`

---

### TC-02: Create — output present at plan time

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputCreatePresent_MatchesBaseline`

**Description:**
An `azapi_resource` create action where `output` is already populated in `after` (less
common, but supported). The Output Values section renders in create-mode (single "Value" column).

**Test Data File:** `azapi-output-create-present-plan.json`

**Plan Requirements:**
- `change.actions = ["create"]`
- `change.before = null`
- `change.after.output` contains 2–3 scalar properties (e.g. `state`, `serviceUrl`)
- `change.after_unknown = { "id": true }` — output is NOT unknown
- `change.after_sensitive` does not mark output fields sensitive

**Expected Snapshot Output (excerpt):**
```markdown
#### Output Values

| Property | Value |
|----------|-------|
| state | `Ok` |
| serviceUrl | `https://example.azure.com` |
```

**Snapshot File:** `azapi-output-create-present.md`

---

### TC-03: Update — output changed

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputUpdate_MatchesBaseline`

**Description:**
An `azapi_resource` update action where both `before.output` and `after.output` are
present with different values. Output Values renders in update-mode (Before/After columns).

**Test Data File:** `azapi-output-update-plan.json`

**Plan Requirements:**
- `change.actions = ["update"]`
- `change.before.output` contains 2–3 scalar properties
- `change.after.output` contains same properties but with at least one value changed
- `change.after_unknown = {}`
- Sensitivity markers absent for output

**Expected Snapshot Output (excerpt):**
```markdown
#### Output Values

| Property | Before | After |
|----------|--------|-------|
| state | `Ok` | `Updating` |
| serviceUrl | `https://example.azure.com` | `https://example.azure.com` |
```

**Snapshot File:** `azapi-output-update.md`

---

### TC-04: Update — output unchanged

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputUpdateUnchanged_MatchesBaseline`

**Description:**
An `azapi_resource` update action where `before.output` and `after.output` are identical.
The Output Values section should render showing no differences (matching existing body
"no changes" rendering behaviour).

**Test Data File:** `azapi-output-update-unchanged-plan.json`

**Plan Requirements:**
- `change.actions = ["update"]`
- `change.before.output` and `change.after.output` are byte-for-byte identical (same keys,
  same values)
- Body has at least one changed property (so the resource still appears in the report)

**Expected Snapshot Output:**
The `#### Output Values` section renders with a table where Before and After values are
identical for every row (identical to how body attributes with no diff render). The section
must appear (it is not omitted for identical output values — omission only happens when
output is completely absent).

**Snapshot File:** `azapi-output-update-unchanged.md`

---

### TC-05: Delete — output from before state

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputDelete_MatchesBaseline`

**Description:**
An `azapi_resource` delete action where `before.output` is present. Output Values renders
in delete-mode (single "Before" column), mirroring how body is rendered for deletes.

**Test Data File:** `azapi-output-delete-plan.json`

**Plan Requirements:**
- `change.actions = ["delete"]`
- `change.before.output` contains 2–3 scalar properties
- `change.after = null`
- No output-related `after_unknown` or `after_sensitive` (both null/absent)

**Expected Snapshot Output (excerpt):**
```markdown
#### Output Values

| Property | Before |
|----------|--------|
| state | `Ok` |
| serviceUrl | `https://example.azure.com` |
```

**Snapshot File:** `azapi-output-delete.md`

---

### TC-06: Replace — before output present, after output unknown

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputReplaceUnknown_MatchesBaseline`

**Description:**
An `azapi_resource` replace action (`["delete", "create"]`) where `before.output` is
present but `after_unknown.output = true`. The Output Values section should render the
before output (showing what will be destroyed) and a notice that the new output is not
yet known.

**Test Data File:** `azapi-output-replace-unknown-plan.json`

**Plan Requirements:**
- `change.actions = ["delete", "create"]`
- `change.before.output` contains 2–3 properties
- `change.after_unknown = { "id": true, "output": true }`
- `change.after` does not include an `output` key

**Expected Snapshot Output:**
Per specification:
> replace, before output present, after unknown → render before output in delete mode + notice

The snapshot should include both the "Before" table from the before output and the
"known after apply" notice for the after state.

**Snapshot File:** `azapi-output-replace-unknown.md`

---

### TC-07: No output — section omitted

**Type:** Regression (Snapshot)

**Method name:** Covered by existing tests (e.g., `Snapshot_AzapiCreateMinimal_MatchesBaseline`,
`Snapshot_AzapiUpdateMinimal_MatchesBaseline`)

**Description:**
When neither `before.output` nor `after.output` is present, and `after_unknown.output`
is not set, the entire Output Values section (heading and any table) must be absent from
the rendered markdown. This is verified by re-running all existing azapi tests — they have
no `output` fields and must produce identical snapshots after the feature is implemented.

**Verification:**
All existing 20 azapi snapshot tests continue to pass without snapshot updates. The
existing snapshot files must not change.

---

### TC-08: Sensitive output values

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputSensitive_MatchesBaseline`

**Description:**
An `azapi_resource` update action where `after_sensitive.output` marks one field as
sensitive. The sensitive field must be masked with `(sensitive)` in the output; other
fields display normally.

**Test Data File:** `azapi-output-sensitive-plan.json`

**Plan Requirements:**
- `change.actions = ["update"]`
- `change.before.output` contains e.g. `{ "state": "Ok", "apiKey": "abc123" }`
- `change.after.output` contains e.g. `{ "state": "Active", "apiKey": "xyz789" }`
- `change.before_sensitive.output = { "apiKey": true }`
- `change.after_sensitive.output = { "apiKey": true }`
- `--show-sensitive` is NOT passed (default: mask)

**Expected Snapshot Output (excerpt):**
```markdown
#### Output Values

| Property | Before | After |
|----------|--------|-------|
| state | `Ok` | `Active` |
| apiKey | `(sensitive)` | `(sensitive)` |
```

**Snapshot File:** `azapi-output-sensitive.md`

---

### TC-09: Grouped output values (Feature 034)

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputGrouped_MatchesBaseline`

**Description:**
An `azapi_resource` update action where `output` contains a nested sub-object with ≥3
properties, triggering Feature 034 grouping. The grouped sub-section should appear as a
separate H6 table with the sub-object key as the heading (matching existing body grouping
behaviour).

**Test Data File:** `azapi-output-grouped-plan.json`

**Plan Requirements:**
- `change.actions = ["update"]`
- `before.output` and `after.output` contain a `properties` sub-object with ≥3 scalar
  fields (e.g. `state`, `automationHybridServiceUrl`, `sku.name`)
- The grouping threshold is met so `properties` is rendered as a separate sub-section

**Expected Snapshot Output (excerpt):**
```markdown
#### Output Values

###### `properties`

| Property | Before | After |
|----------|--------|-------|
| state | `Ok` | `Updating` |
| automationHybridServiceUrl | `https://...` | `https://...` |
| sku.name | `Basic` | `Standard` |
```

**Snapshot File:** `azapi-output-grouped.md`

---

### TC-10: Large output value

**Type:** Snapshot

**Method name:** `Snapshot_AzapiOutputLargeValue_MatchesBaseline`

**Description:**
An `azapi_resource` update action where one output field contains a string that exceeds
the large-value threshold (≥500 characters). The large value should be rendered using the
same collapsible/truncated format already used for large body values.

**Test Data File:** `azapi-output-large-value-plan.json`

**Plan Requirements:**
- `change.actions = ["update"]`
- `before.output` and `after.output` each contain a key (e.g. `certificateData`) whose
  value is a string ≥500 characters long (use a repeated or realistic-looking Base64 blob)
- At least one other normal-length property is present to confirm mixed rendering

**Expected Snapshot Output:**
The large field renders using the existing large-value format (`<details>`/`<summary>` or
truncation), matching the pattern already seen in `azapi-large-value.md`.

**Snapshot File:** `azapi-output-large-value.md`

---

### TC-11: azapi_update_resource — output rendered

**Type:** Snapshot

**Method name:** `Snapshot_AzapiUpdateResourceOutput_MatchesBaseline`

**Description:**
An `azapi_update_resource` update action where both `before.output` and `after.output`
are present. Verifies that the `update_resource.sbn` template renders Output Values
identically to `resource.sbn`.

**Test Data File:** `azapi-update-resource-output-plan.json`

**Plan Requirements:**
- `change.type = "azapi_update_resource"`
- `change.actions = ["update"]`
- `change.before.output` and `change.after.output` contain 2–3 scalar properties with at
  least one changed value
- `change.before_sensitive.output` and `change.after_sensitive.output` absent or empty

**Expected Snapshot Output:**
Equivalent to TC-03 format but using `azapi_update_resource` as the resource type.

**Snapshot File:** `azapi-update-resource-output.md`

---

### TC-12: Regression — body rendering unaffected

**Type:** Regression

**Description:**
All existing azapi snapshot tests (`azapi-create-complete`, `azapi-update-complete`,
`azapi-delete-complete`, `azapi-replace-complete`, `azapi-sensitive`, `azapi-body-sensitive`,
`azapi-complex-nested`, `azapi-large-value`, etc.) must continue to pass unchanged after
the output rendering block is added to both templates. No changes to existing snapshot
files are acceptable as part of this feature.

**Verification:**
Run `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`. All 20
existing azapi snapshot tests must pass with no snapshot regeneration required.

---

## Test Data Requirements

New JSON files required in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`:

| File | Description |
|------|-------------|
| `azapi-output-create-unknown-plan.json` | `azapi_resource` create, `after_unknown.output = true`, no output in after |
| `azapi-output-create-present-plan.json` | `azapi_resource` create, output present in after (output known at plan time) |
| `azapi-output-update-plan.json` | `azapi_resource` update, before/after output present, at least one value changed |
| `azapi-output-update-unchanged-plan.json` | `azapi_resource` update, before/after output identical, body has a change |
| `azapi-output-delete-plan.json` | `azapi_resource` delete, before output present, after null |
| `azapi-output-replace-unknown-plan.json` | `azapi_resource` replace, before output present, `after_unknown.output = true` |
| `azapi-output-sensitive-plan.json` | `azapi_resource` update, `before_sensitive.output` / `after_sensitive.output` marks a field |
| `azapi-output-grouped-plan.json` | `azapi_resource` update, output.properties has ≥3 fields (triggers grouping) |
| `azapi-output-large-value-plan.json` | `azapi_resource` update, one output field ≥500 chars long |
| `azapi-update-resource-output-plan.json` | `azapi_update_resource` update, before/after output present with a changed value |

Corresponding approved snapshots in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`:

| File | Corresponds To |
|------|----------------|
| `azapi-output-create-unknown.md` | TC-01 |
| `azapi-output-create-present.md` | TC-02 |
| `azapi-output-update.md` | TC-03 |
| `azapi-output-update-unchanged.md` | TC-04 |
| `azapi-output-delete.md` | TC-05 |
| `azapi-output-replace-unknown.md` | TC-06 |
| `azapi-output-sensitive.md` | TC-08 |
| `azapi-output-grouped.md` | TC-09 |
| `azapi-output-large-value.md` | TC-10 |
| `azapi-update-resource-output.md` | TC-11 |

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| `after_unknown.output = true` on create | Output Values section absent (suppressed entirely) | TC-01 |
| `after_unknown.output = true` on replace with before present | Before table (delete mode) + notice | TC-06 |
| Output absent (both before and after) | No `#### Output Values` heading rendered | TC-07 / TC-12 |
| Output identical before and after (no-diff) | Section renders; table shows identical Before/After | TC-04 |
| Sensitive field in output | Field masked as `(sensitive)` | TC-08 |
| Output with nested sub-object triggering grouping | Sub-section H6 table rendered | TC-09 |
| Large string in output field | Collapsible/truncated rendering | TC-10 |
| `azapi_update_resource` (no create/replace) | Output renders for update/delete only | TC-11 |

---

## Non-Functional Tests

- **No snapshot regressions:** All 20+ existing azapi snapshot tests pass unchanged.
- **Test execution:** All new tests run via
  `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` with no
  manual steps.

---

## Open Questions

None. The architecture document provides complete implementation guidance including exact
Scriban snippets for both templates and per-action output logic.
