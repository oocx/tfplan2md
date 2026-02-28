# UAT Test Plan: Separate Table for azapi Output Values

## Goal

Verify that the **Output Values** section for `azapi_resource` and `azapi_update_resource`
renders correctly in GitHub and Azure DevOps PR comments, clearly separating API response
values from body (input) attributes.

---

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific Output Values rendering introduced in this feature.
This artifact MUST be real tfplan2md output, not synthetic or simulated.

**Source Plan Path:** `docs/features/106-azapi-output-values/uat-plan.json`

**Rendered Output Path:** `docs/features/106-azapi-output-values/uat-plan.md`

**Plan Requirements:**
- **MUST be a real Terraform plan JSON** that exercises the Output Values feature
- **MUST include all key output rendering scenarios:** create (unknown), update (changed),
  delete (before only)
- **MUST include a grouped output example** (output with a `properties` sub-object having ≥3
  fields to trigger Feature 034 grouping)
- **MUST include a sensitive output field** in at least one resource
- **Rationale:** A single multi-resource plan allows the UAT Tester to verify all key
  output scenarios in one PR comment, reducing review effort while ensuring completeness.
- **Key Resources to include:**
  1. `azapi_resource.automation_create` — create action, `after_unknown.output = true`
     (shows "known after apply" notice)
  2. `azapi_resource.automation_update` — update action, before/after output with grouped
     `properties` sub-object (shows grouped output table)
  3. `azapi_resource.sql_delete` — delete action, before output with a sensitive field
     (shows delete-mode table with sensitivity masking)
- **Coverage:**
  - "Known after apply" notice for creates
  - Before/After update table with Feature 034 grouping
  - Before-only delete table
  - Sensitivity masking in output values
  - Section heading "Output Values" clearly distinct from "Body Changes"

**Example Creation Command:**
```bash
# After the Developer implements the feature and builds the binary:
tfplan2md docs/features/106-azapi-output-values/uat-plan.json \
  > docs/features/106-azapi-output-values/uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas.

**Artifact Path:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using the
`generate-demo-artifacts` skill.

---

## Test Steps

1. Developer creates `uat-plan.json` based on this specification
2. Developer generates `uat-plan.md` from the plan using the built binary
3. Code Reviewer validates both files exist and are complete
4. UAT Tester uses `uat-plan.md` for testing via the `run-uat` skill
5. UAT posts TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific Output Values changes using
     `docs/features/106-azapi-output-values/uat-plan.md`
   - **Comprehensive Demo**: Regression test for side effects using
     `artifacts/comprehensive-demo-simple-diff.md` (GitHub) /
     `artifacts/comprehensive-demo.md` (Azure DevOps)
6. Verify both reports on GitHub and Azure DevOps

---

## Validation Instructions (Test Description)

**Feature-Specific Validation:**

In the **feature-specific report** (first comment, labelled "🎯 Feature Test"):

### Resource 1 — `azapi_resource.automation_create` (create, output unknown)

**Expected:** An `#### Output Values` heading appears after the `#### Body Changes` section
(or after the top-level attributes table), followed by the italic text:

> *Output values are not known until after apply.*

**Verify:**
- The heading `#### Output Values` is present and distinct from `#### Body Changes`
- No table rows appear under it — only the notice text
- The notice text is in italics

---

### Resource 2 — `azapi_resource.automation_update` (update, grouped output)

**Expected:** An `#### Output Values` heading appears after the body section, followed by
a `###### \`properties\`` sub-section (from Feature 034 grouping), containing a Before/After
table with 3+ rows.

**Verify:**
- The `#### Output Values` heading appears after `#### Body Changes`
- A `###### \`properties\`` H6 sub-heading is rendered inside the Output Values section
- The table under it has `| Property | Before | After |` columns
- Values from `automationHybridServiceUrl`, `state`, and `sku.name` appear as rows
- Data values are formatted as inline code (e.g., `` `Ok` ``, not plain `Ok`)
- No output values appear in the `#### Body Changes` section (clear separation)

**Before/After Context:**
Previously, the `output` attribute was completely invisible in the report. Users had to
read the raw Terraform plan JSON to find API response values. After this feature, the
rendered report shows these values in a clearly labelled, grouped table.

---

### Resource 3 — `azapi_resource.sql_delete` (delete, sensitive field)

**Expected:** An `#### Output Values` heading with a Before-only table. One row contains
`(sensitive)` because the field is marked sensitive in `before_sensitive.output`.

**Verify:**
- The `#### Output Values` heading appears
- The table has only `| Property | Before |` columns (single-column delete mode)
- The sensitive field (e.g. `apiKey`) displays `(sensitive)` rather than the actual value
- The non-sensitive field (e.g. `state`) shows its actual value in code formatting

---

**Regression Validation:**

In the **comprehensive demo** (second comment, labelled "🔄 Regression Test"):

**Verify:**
- Resources without an `output` attribute do not show an "Output Values" section —
  the section heading must be completely absent for those resources
- Existing body rendering (grouping, sensitivity, large values) is unchanged
- All report sections (summary table, module headings, code analysis findings) render
  correctly with no unintended changes
