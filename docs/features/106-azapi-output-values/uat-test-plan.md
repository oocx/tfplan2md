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
- **MUST include all key output rendering scenarios:** create (unknown → no section), update
  (changed), delete (before only)
- **MUST include a grouped output example** (output with a sub-object having ≥3 fields to
  trigger Feature 034 grouping)
- **MUST include a sensitive output field** in at least one resource
- **MUST include an Azure resource ID in output values** to demonstrate display name mapping
  (Azure resource ID → human-readable description)
- **Rationale:** A single multi-resource plan allows the UAT Tester to verify all key
  output scenarios in one PR comment, reducing review effort while ensuring completeness.
- **Key Resources to include:**
  1. `azapi_resource.automation_create` — create action, `after_unknown.output = true`
     (output section is **absent** — no section shown when all values are unknown)
  2. `azapi_resource.automation_update` — update action, before/after output with grouped
     `sku` sub-object (shows grouped output table) and `linkedWorkspaceId` Azure resource ID
     (shows display name mapping)
  3. `azapi_resource.sql_delete` — delete action, before output with a sensitive field
     (shows delete-mode table with sensitivity masking)
- **Coverage:**
  - No Output Values section for creates when output is unknown (section suppressed entirely)
  - Before/After update table with Feature 034 grouping (`sku` sub-section)
  - Azure resource ID display name mapping (`linkedWorkspaceId` formatted as workspace description)
  - Before-only delete table
  - Sensitivity masking in output values
  - Section heading "Output Values" clearly distinct from "Body Changes"

**Example Creation Command:**

```bash
# After the Developer implements the feature and builds the binary:
tfplan2md \
  --principals examples/comprehensive-demo/demo-principals.json \
  docs/features/106-azapi-output-values/uat-plan.json \
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

**Expected:** **No** `#### Output Values` section appears. When all output values are unknown
after apply, the section is suppressed entirely — there is no heading and no notice text.

**Verify:**

- The `#### Output Values` heading is **completely absent** for this resource
- The resource block ends after the `#### Body` section
- This behavior confirms that the section is only shown when there is actual data to display

---

### Resource 2 — `azapi_resource.automation_update` (update, grouped output + display names)

**Expected:** An `#### Output Values` heading appears after the body section, containing:

- A flat table row for `linkedWorkspaceId` showing the Azure resource ID formatted as a
  human-readable workspace description (display name mapping)
- A flat table row for `state` showing Before/After values
- A `###### \`sku\`` H6 sub-section (from Feature 034 grouping) with a Before/After table

**Verify:**

- The `#### Output Values` heading appears after `#### Body Changes`
- The `linkedWorkspaceId` row shows the Azure resource ID as a human-readable description
  (e.g., `Log Analytics Workspace` `🆔 log-workspace-old` in resource group...) — NOT the
  raw `/subscriptions/.../providers/...` path — demonstrating display name mapping
- A `###### \`sku\`` H6 sub-heading is rendered inside the Output Values section
- The table under `sku` has `| Property | Before | After |` columns with 3 rows
- Data values are formatted as inline code (e.g., `` `Running` ``, not plain `Running`)
- No output values appear in the `#### Body Changes` section (clear separation)

**Before/After Context:**
Previously, the `output` attribute was completely invisible in the report. Users had to
read the raw Terraform plan JSON to find API response values. After this feature, the
rendered report shows these values in a clearly labelled, grouped table with display name
formatting applied to Azure resource IDs.

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
