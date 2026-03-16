# UAT Test Plan: Azure DevOps User Entitlement Summary Fields

## Goal

Verify that `azuredevops_user_entitlement` resources render meaningful summary lines
(`principal_name`, `account_license_type`, `licensing_source`) in GitHub and Azure DevOps
PR comments instead of falling back to a bare resource address.

---

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific summary-line improvements for
`azuredevops_user_entitlement` resources.

**Source Plan Path:** `docs/features/048-azuredevops-user-entitlement-summary/uat-plan.json`

**Rendered Output Path:** `docs/features/048-azuredevops-user-entitlement-summary/uat-plan.md`

**Plan Requirements:**
- **MUST be a real Terraform plan JSON** (`format_version: "1.2"`) targeting the
  `registry.terraform.io/microsoft/azuredevops` provider.
- **MUST include at least three `azuredevops_user_entitlement` create changes** covering:
  1. All three fields populated (`principal_name`, `account_license_type`, `licensing_source`).
  2. `principal_name` and `account_license_type` populated; `licensing_source` absent.
  3. No mapped fields at all (empty `after`) — exercises the address-fallback path.
- **Rationale:** These three variants together prove that populated fields appear, that
  absent fields are silently skipped, and that the fallback remains intact — the three
  core success criteria from the specification.
- **Key Resources:**
  - `azuredevops_user_entitlement.full` — all three fields present.
  - `azuredevops_user_entitlement.partial` — two fields present, one absent.
  - `azuredevops_user_entitlement.empty` — no fields present (fallback case).
- **Coverage:**
  - `principal_name` display in summary line.
  - `account_license_type` display in summary line.
  - `licensing_source` display when populated.
  - Silent omission of `licensing_source` when absent/empty.
  - Address-only fallback when all mapped fields are absent.

**Example Creation Command:**
```bash
# Generate the rendered output from the plan
dotnet run --project src/Oocx.TfPlan2Md -- \
  docs/features/048-azuredevops-user-entitlement-summary/uat-plan.json \
  > docs/features/048-azuredevops-user-entitlement-summary/uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects on other resource types or existing
Azure DevOps resources.

**Artifact Path:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using the
`generate-demo-artifacts` skill.

---

## Test Steps

1. Developer creates `uat-plan.json` based on this specification.
2. Developer generates `uat-plan.md` from the plan using the command above.
3. Code Reviewer validates both files exist and are complete.
4. UAT Tester uses `uat-plan.md` for testing.
5. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific changes using `uat-plan.md`.
   - **Comprehensive Demo**: Regression test for side effects.
6. Verify both reports on GitHub and Azure DevOps.

---

## Validation Instructions (Test Description)

### Feature-Specific Validation

In the **feature-specific report** (first comment, labelled "🎯 Feature Test"):

**Specific Resources/Sections:**

1. `azuredevops_user_entitlement.full`
2. `azuredevops_user_entitlement.partial`
3. `azuredevops_user_entitlement.empty`

**Exact Attributes to check:**

For `azuredevops_user_entitlement.full`:
- `principal_name` (e.g., `john.doe@example.com`)
- `account_license_type` (e.g., `express`)
- `licensing_source` (e.g., `msdn`)

For `azuredevops_user_entitlement.partial`:
- `principal_name` (e.g., `jane.smith@example.com`)
- `account_license_type` (e.g., `stakeholder`)
- Confirm `licensing_source` does **not** appear in the summary line.

For `azuredevops_user_entitlement.empty`:
- Confirm only the resource address appears (no ` | ` separator).

**Expected Outcome:**

`azuredevops_user_entitlement.full` summary line should read approximately:
```
➕ azuredevops_user_entitlement full — john.doe@example.com | express | msdn
```

`azuredevops_user_entitlement.partial` summary line should read approximately:
```
➕ azuredevops_user_entitlement partial — jane.smith@example.com | stakeholder
```

`azuredevops_user_entitlement.empty` summary line should read approximately:
```
➕ azuredevops_user_entitlement empty
```
(resource address only, no extra fields)

**Before/After Context:**

*Before this change*, all three resources would show only the Terraform resource address
in their summary lines (e.g., `➕ azuredevops_user_entitlement john.doe@example.com`)
because the generic Azure DevOps provider fallback keys (`name`, `project_id`) are not
present on user entitlement resources.

*After this change*, populated fields give reviewers immediate visibility into who is
being granted access and under what license — without expanding the full diff.

---

### Regression Validation

In the **comprehensive demo** (second comment, labelled "🔄 Regression Test"):

**Verify:**
- Existing `azuredevops_project` summaries are unchanged (still show `name | visibility`).
- All other Azure DevOps resource summary lines are unaffected.
- All sections render correctly (summaries, details, static analysis).
- No unintended changes introduced to non-Azure DevOps resources.
