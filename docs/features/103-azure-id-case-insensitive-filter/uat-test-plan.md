# UAT Test Plan: Case-Insensitive Attribute Change Filter

## Goal

Verify that the `--ignore-azure-id-case-changes` flag correctly suppresses casing-only attribute rows in the rendered markdown output, as seen in GitHub and Azure DevOps PR comments.

---

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific changes in this feature. This artifact MUST be real tfplan2md output, not synthetic or simulated.

**Source Plan Path:** `docs/features/103-azure-id-case-insensitive-filter/uat-plan.json`

**Rendered Output Path:** `docs/features/103-azure-id-case-insensitive-filter/uat-plan.md`

**Plan Requirements:**

- **MUST be a real Terraform plan JSON** containing `azurerm_role_assignment` or similar resources with attributes where before/after values differ only in casing (uppercase vs lowercase Azure resource IDs).
- **MUST include a mixed-changes resource** — one where some attributes have casing-only differences and at least one has a genuine value change.
- **MUST be rendered with `--ignore-azure-id-case-changes`** so the output shows casing rows suppressed.
- **Rationale:** The plan must produce visible before/after casing differences to demonstrate the filter in action. A `--show-unchanged-values` baseline comparison (without the flag) is also useful to show what was previously visible.
- **Key Resources:**
  1. A resource with only casing-only attribute changes (to demonstrate complete suppression)
  2. A resource with mixed changes (to demonstrate selective suppression and genuine change retention)
- **Coverage:**
  - Casing-only rows are absent from the rendered table
  - Genuine change rows remain in the rendered table
  - Resource still appears in the plan (not completely hidden when all attributes suppressed)

**Example Creation Command:**
```bash
# Generate the rendered output from the plan (with the new flag)
tfplan2md docs/features/103-azure-id-case-insensitive-filter/uat-plan.json \
  --ignore-azure-id-case-changes \
  > docs/features/103-azure-id-case-insensitive-filter/uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas (other providers, resource types, summary counts).

**Artifact Path:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using the `generate-demo-artifacts` skill.

---

## Test Steps

1. Developer creates `uat-plan.json` following the requirements in this document.
2. Developer runs `tfplan2md` with `--ignore-azure-id-case-changes` to generate `uat-plan.md`.
3. Code Reviewer validates that both `uat-plan.json` and `uat-plan.md` exist and are complete.
4. UAT Tester uses `uat-plan.md` as the PR body / comment for testing.
5. UAT posts **two** separate PR comments:
   - **Feature-Specific Report** (labeled "🎯 Feature Test"): Tests the specific changes using `uat-plan.md`.
   - **Comprehensive Demo** (labeled "🔄 Regression Test"): Tests for side effects using the demo artifact.
6. Verify both reports on **GitHub** and **Azure DevOps**.

---

## Validation Instructions (Test Description)

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

**Specific Resources/Sections:**

1. The casing-only resource (e.g., `azurerm_role_assignment.casing_only` or similar):
   - Its attribute change table should be **empty** or contain **no rows** — all casing rows were suppressed.
   - The resource heading and summary line still appear in the report (the resource is not completely removed).

2. The mixed-changes resource (e.g., `azurerm_role_assignment.mixed_changes` or similar):
   - Its attribute change table should contain **only genuine changes** (e.g., `display_name` changing from `"My App"` to `"My Application"`).
   - Casing-only attributes (e.g., `scope`) should **not** appear in the table.

**Exact Attributes:**

- `scope` (or `role_definition_id`): should be **absent** from the mixed-changes resource's attribute table because before/after differ only in casing (e.g., `/subscriptions/ABC123/…` → `/subscriptions/abc123/…`).
- `display_name`: should be **present** in the mixed-changes resource's attribute table with before `"My App"` and after `"My Application"`.

**Expected Outcome:**

- When `--ignore-azure-id-case-changes` is active, the attribute table for the casing-only resource shows zero rows.
- When `--ignore-azure-id-case-changes` is active, the attribute table for the mixed-changes resource shows only non-casing changes.
- Both resources still appear in the resource change summary (plan-level counts are unaffected).

**Before/After Context:**

- **Before (without `--ignore-azure-id-case-changes`):** Both the casing-only and the genuine change rows appear side-by-side in the attribute table, making it hard for reviewers to identify real changes.
- **After (with `--ignore-azure-id-case-changes`):** Only genuine infrastructure changes appear in the attribute table; casing noise from the Azure ARM API is invisible.

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**

- No unintended changes to existing resources (azurerm, azuread, azapi, azuredevops resources all render as expected).
- Summary section (resource counts, change types) is unaffected.
- Static analysis (if present) sections are unaffected.
- No extra blank rows or broken table formatting.
