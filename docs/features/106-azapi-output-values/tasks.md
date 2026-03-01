# Tasks: Separate Table for azapi Output Values

## Overview

Feature 106 adds a dedicated **Output Values** section to `azapi_resource` and
`azapi_update_resource` rendered reports, showing the Azure REST API response values
returned via the `output` attribute. Implementation is template-only — two Scriban
templates require additions; no C# changes are needed.

Reference: `docs/features/106-azapi-output-values/specification.md`  
Architecture: `docs/features/106-azapi-output-values/architecture.md`  
Test plan: `docs/features/106-azapi-output-values/test-plan.md`

---

## Tasks

### Task 1: Add output rendering block to `resource.sbn`

**Priority:** High

**Description:**  
Add the output values rendering block to the `azapi_resource` Scriban template,
immediately after the closing `{{~ end ~}}` of the existing body rendering block and
before the `{{ include "/_code_analysis_findings.sbn" }}` line.

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`

The complete Scriban snippet to add is documented verbatim in
`docs/features/106-azapi-output-values/architecture.md` under
**"Scriban Snippet — resource.sbn Output Block"**. Copy it exactly.

**Acceptance Criteria:**
- [ ] The output rendering block is inserted after the closing `{{~ end ~}}` of the body
      rendering block (the `{{~ else if change.action == "delete" ~}}` / `{{~ end ~}}` chain)
      and before the `{{ include "/_code_analysis_findings.sbn" }}` line.
- [ ] When `after_unknown.output = true`, the template emits `#### Output Values` heading
      followed by `*Output values are not known until after apply.*`.
- [ ] When `output` is present in both before and after, `render_azapi_body` is called in
      `"update"` mode with `"Output Values"` as the heading parameter.
- [ ] When `output` is absent in both before and after, no `#### Output Values` heading or
      content is emitted (visibility guard works).
- [ ] All existing azapi snapshot tests continue to pass with no snapshot changes (TC-07
      regression check).

**Dependencies:** None

**Notes:**  
The architecture document provides the exact Scriban text — do not paraphrase or
restructure it. The key guard condition is:
`has_before_output || has_after_output || output_unknown`.

---

### Task 2: Add output rendering block to `update_resource.sbn`

**Priority:** High

**Description:**  
Add the output values rendering block to the `azapi_update_resource` Scriban template,
immediately after the closing `{{~ end ~}}` of the existing body rendering block and
before the `{{ include "/_code_analysis_findings.sbn" }}` line.

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/update_resource.sbn`

The complete Scriban snippet is documented verbatim in
`docs/features/106-azapi-output-values/architecture.md` under
**"Scriban Snippet — update_resource.sbn Output Block"**. Copy it exactly.

**Acceptance Criteria:**
- [ ] The output rendering block is inserted after the closing `{{~ end ~}}` of the body
      rendering block (the `{{~ else if change.action == "delete" ~}}` / `{{~ end ~}}` chain)
      and before the `{{ include "/_code_analysis_findings.sbn" }}` line.
- [ ] No `output_unknown` guard is present (this template does not support create/replace).
- [ ] Guard condition is `has_before_output || has_after_output` (no `output_unknown`).
- [ ] When `output` is absent in both before and after, no `#### Output Values` heading or
      content is emitted.
- [ ] All existing `azapi_update_resource` snapshot tests continue to pass (TC-07 regression).

**Dependencies:** None (can be done in parallel with Task 1)

**Notes:**  
`azapi_update_resource` only supports `update` and `delete` actions, so there is no
`after_unknown.output` guard in this template, unlike `resource.sbn`.

---

### Task 3: Create test data JSON files

**Priority:** High

**Description:**  
Create JSON plan files for the six new test scenarios. Each file goes in
`src/tests/Oocx.TfPlan2Md.TUnit/TestData/` and follows the same structure as existing
files in that directory (e.g. `azapi-update-plan.json`, `azapi-create-complete-plan.json`).

**Files to create:**

| File | Scenario | Key characteristics |
|------|----------|---------------------|
| `azapi-output-create-unknown-plan.json` | TC-01 | `actions=["create"]`, `after_unknown.output=true`, no `output` in `after` |
| `azapi-output-create-present-plan.json` | TC-02 | `actions=["create"]`, `after.output` has 2–3 scalar props, `after_unknown.output` absent |
| `azapi-output-update-plan.json` | TC-03 | `actions=["update"]`, both `before.output` and `after.output` present, ≥1 value changed |
| `azapi-output-sensitive-plan.json` | TC-08 | `actions=["update"]`, `before_sensitive.output.apiKey=true`, `after_sensitive.output.apiKey=true` |
| `azapi-output-grouped-plan.json` | TC-09 | `actions=["update"]`, `output.properties` sub-object has ≥3 scalar fields (triggers Feature 034 grouping) |
| `azapi-update-resource-output-plan.json` | TC-11 | `type=azapi_update_resource`, `actions=["update"]`, both `before.output` and `after.output` present |

**Acceptance Criteria:**
- [ ] Each JSON file is valid JSON parseable by `TerraformPlanParser`.
- [ ] Each file has exactly one entry in `resource_changes`.
- [ ] Files follow the `format_version: "1.2"`, `terraform_version: "1.9.0"` header convention
      (matching other test data files).
- [ ] TC-01 file: `change.after_unknown = { "id": true, "output": true }`, no `output` key
      in `change.after`.
- [ ] TC-02 file: `change.after.output` contains at least `state` and `serviceUrl` scalar
      properties; `after_unknown` does not include `output`.
- [ ] TC-03 file: `change.before.output` and `change.after.output` share the same keys but
      at least one value differs between before and after (e.g. `state: "Ok"` → `"Updating"`).
- [ ] TC-08 file: `change.before_sensitive.output = { "apiKey": true }` and
      `change.after_sensitive.output = { "apiKey": true }`; both before and after output
      contain `state` (non-sensitive) and `apiKey` (sensitive) fields.
- [ ] TC-09 file: `output.properties` has ≥3 scalar fields in both before and after to
      trigger attribute grouping (e.g. `state`, `automationHybridServiceUrl`, `sku.name`
      or a nested `sku` sub-object).
- [ ] TC-11 file: resource type is `azapi_update_resource` with `resource_id` attribute;
      both `before.output` and `after.output` present with ≥1 value changed.

**Dependencies:** None (JSON creation does not require template changes)

**Notes:**  
Model output fields on the Azure Automation Account API response (as used in existing test
files): `state`, `automationHybridServiceUrl`, and a nested `sku` object. Keep values
realistic but synthetic — no real subscription IDs or secrets. See
`azapi-update-resource-update-plan.json` for the `azapi_update_resource` JSON structure.

---

### Task 4: Add snapshot test methods

**Priority:** Medium

**Description:**  
Add six new test methods to `AzapiSnapshotTests.cs`, one per new test scenario. Each
method follows the exact same pattern as existing methods in that class.

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzapiSnapshotTests.cs`

**Methods to add:**

| Method name | Plan file | Snapshot file |
|-------------|-----------|---------------|
| `Snapshot_AzapiOutputCreateUnknown_MatchesBaseline` | `azapi-output-create-unknown-plan.json` | `azapi-output-create-unknown.md` |
| `Snapshot_AzapiOutputCreatePresent_MatchesBaseline` | `azapi-output-create-present-plan.json` | `azapi-output-create-present.md` |
| `Snapshot_AzapiOutputUpdate_MatchesBaseline` | `azapi-output-update-plan.json` | `azapi-output-update.md` |
| `Snapshot_AzapiOutputSensitive_MatchesBaseline` | `azapi-output-sensitive-plan.json` | `azapi-output-sensitive.md` |
| `Snapshot_AzapiOutputGrouped_MatchesBaseline` | `azapi-output-grouped-plan.json` | `azapi-output-grouped.md` |
| `Snapshot_AzapiUpdateResourceOutput_MatchesBaseline` | `azapi-update-resource-output-plan.json` | `azapi-update-resource-output.md` |

**Acceptance Criteria:**
- [ ] Each method is decorated with `[Test]`.
- [ ] Each method has an XML doc comment describing what it verifies (matching the style of
      existing methods in the class).
- [ ] Each method body is a single call to `AssertAzapiSnapshot(planFile, snapshotFile)`.
- [ ] No new helper methods or test infrastructure is added (reuse existing `AssertAzapiSnapshot`
      and `CreateProviderRegistry`).
- [ ] The file compiles without errors.

**Dependencies:** Task 3 (test data JSON files must exist for tests to run)

**Notes:**  
Add the new methods after the existing `Snapshot_AzapiUpdateResourceDelete_MatchesBaseline`
method (line 204), before the `RenderAzapiPlan` private method. Include a short XML doc
comment reference to feature 106 for the new group of methods.

---

### Task 5: Regenerate snapshots and verify all tests pass

**Priority:** Medium

**Description:**  
Run the full test suite after Tasks 1–4 are complete. For the six new test methods, the
snapshot files do not yet exist; `SnapshotTestAssertions` will auto-create them on first
run and fail with instructions. After auto-creation, re-run the tests to confirm all new
and existing tests pass.

**Steps:**
1. Run `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` from the
   repo root to trigger snapshot auto-creation.
2. Review the six newly generated snapshot files in
   `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/` to confirm they match expectations
   from the test plan (verify headings, table columns, sensitivity masking, grouping).
3. Re-run the tests; all should now pass (both new and existing).
4. Confirm the 20+ existing azapi snapshot files are **unchanged** (TC-07 regression).

**Acceptance Criteria:**
- [ ] Six new snapshot `.md` files are created and committed:
  - `azapi-output-create-unknown.md`
  - `azapi-output-create-present.md`
  - `azapi-output-update.md`
  - `azapi-output-sensitive.md`
  - `azapi-output-grouped.md`
  - `azapi-update-resource-output.md`
- [ ] `azapi-output-create-unknown.md` contains `#### Output Values` heading and the
      `*Output values are not known until after apply.*` notice (no table).
- [ ] `azapi-output-create-present.md` contains a `| Property | Value |` table under
      `#### Output Values`.
- [ ] `azapi-output-update.md` contains a `| Property | Before | After |` table.
- [ ] `azapi-output-sensitive.md` shows `(sensitive)` for the masked field.
- [ ] `azapi-output-grouped.md` contains a `###### \`properties\`` sub-section heading
      (Feature 034 grouping fired).
- [ ] `azapi-update-resource-output.md` contains an `#### Output Values` table from
      `azapi_update_resource`.
- [ ] All 20+ previously passing azapi snapshot tests still pass without snapshot changes.
- [ ] Full test suite passes: `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`

**Dependencies:** Tasks 1, 2, 3, 4

**Notes:**  
Use the `update-test-snapshots` skill if available, or follow the manual steps above.
The `SnapshotTestAssertions` helper auto-creates missing snapshots on first run, so
snapshot files do not need to be hand-authored — they are generated from the actual
rendered output. Review them carefully before committing.

---

## Implementation Order

1. **Task 1** (resource.sbn) and **Task 2** (update_resource.sbn) — Template changes first;
   these are independent and can be done in parallel. Template changes are the core
   implementation; nothing else works without them.
2. **Task 3** (test data JSONs) — Can start in parallel with Tasks 1 and 2 since JSON
   files are standalone; however tests cannot be run until templates are also updated.
3. **Task 4** (snapshot test methods) — Add after test data files exist; requires Tasks 1–3
   to be complete before the tests can produce useful output.
4. **Task 5** (snapshot generation and verification) — Final step; requires all prior tasks.

## Open Questions

None. The architecture document provides complete Scriban snippets and per-action logic
for both templates. The test plan specifies exact method names, file names, and expected
snapshot content for all scenarios.
