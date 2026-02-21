# UAT Test Plan: Sensitive Information Exposure Fix (Issue 098)

## Goal

Verify that after the security fix, tfplan2md renders `(sensitive)` in place of plaintext secret values
in GitHub PR comments and Azure DevOps PR descriptions — and that no non-sensitive values are
accidentally masked.

This fix affects output visually: values that previously appeared as `P@ssw0rd123!` will now appear
as `(sensitive)`. The UAT verifies this change renders legibly and consistently in both platforms.

---

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the six confirmed sensitive-data rendering paths in this issue.
Each affected path must produce a visible `(sensitive)` placeholder.

**Source Plan Path:** `docs/issues/098-sensitive-info-exposure/uat-plan.json`

**Rendered Output Path:** `docs/issues/098-sensitive-info-exposure/uat-plan.md`

**Plan Requirements:**

- **MUST be a real Terraform plan JSON** containing at least:
  - An `azapi_resource` **create** action with a sensitive body property (`after_sensitive.body.properties.<key> = true`)
  - An `azapi_resource` **update** action where `afterSensitive` marks a property sensitive
  - An `azapi_resource` **delete** action where `beforeSensitive` marks a property sensitive
  - An `azuredevops_variable_group` resource where a variable transitions from `is_secret: true` → `is_secret: false`
  - A resource with root-level `after_sensitive: true` (wraps the entire resource)
  - A resource with a top-level array attribute marked sensitive (e.g., `secrets: true` in `after_sensitive`)
- **Rationale:** Exercises all six exposure paths identified in the security analysis simultaneously.
- **Key Resources:**
  - `azapi_resource.sql_server` (create, sensitive `administratorLoginPassword`)
  - `azapi_resource.policy_assignment` (update, sensitive `clientSecret`)
  - `azuredevops_variable_group.pipeline_vars` (update, `is_secret` transition `true → false`)
- **Coverage:** Create, update, delete AzApi body sensitivity; Variable Group secret transition; root boolean sensitivity; array parent sensitivity.

**Example Generation Command (run after Developer implements fix):**
```bash
tfplan2md docs/issues/098-sensitive-info-exposure/uat-plan.json \
  > docs/issues/098-sensitive-info-exposure/uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure the security fix introduces no unintended side effects on unrelated resource types.

**Artifact Path:**
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** Generated automatically by the Developer using the `generate-demo-artifacts` skill.

---

## Test Steps

1. Developer creates `docs/issues/098-sensitive-info-exposure/uat-plan.json` based on the schema in `test-plan.md § Test Data Requirements`.
2. Developer generates `docs/issues/098-sensitive-info-exposure/uat-plan.md` from the plan after implementing the fix.
3. Code Reviewer validates both files exist and contain `(sensitive)` placeholders (not plaintext secrets).
4. UAT Tester uses `uat-plan.md` for the feature-specific test and `artifacts/comprehensive-demo-simple-diff.md` for the regression test.
5. UAT posts TWO separate PR comments:
   - **Feature-Specific Report** (🎯 Feature Test): Uses `uat-plan.md`
   - **Comprehensive Demo** (🔄 Regression Test): Uses `artifacts/comprehensive-demo-simple-diff.md`
6. Maintainer reviews both comments on GitHub and Azure DevOps.

---

## Validation Instructions (Test Description)

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

**Specific Resources to Check:**

1. **AzApi create — `azapi_resource.sql_server`**
   - In the Body table, look for the `administratorLoginPassword` row.
   - **Expected:** Value column shows `` `(sensitive)` ``
   - **NOT expected:** Any plaintext password (e.g., `P@ssw0rd123!` or similar)
   - Verify `administratorLogin` (non-sensitive) still shows its actual value (e.g., `sqladmin`)

2. **AzApi update — `azapi_resource.policy_assignment`** (or equivalent)
   - In the Body Changes table, look for the `clientSecret` row.
   - **Expected:** Both Before and After columns show `` `(sensitive)` ``
   - **NOT expected:** `old-secret` or `new-secret` in any form
   - Verify other non-sensitive changed properties (e.g., `name`) still show their values

3. **AzApi delete — `azapi_resource` with sensitive before-value**
   - In the Body table (showing pre-deletion state), look for the sensitive property.
   - **Expected:** Value column shows `` `(sensitive)` ``

4. **Variable Group — `azuredevops_variable_group.pipeline_vars`**
   - Find the variable that transitioned from `is_secret: true` → `is_secret: false`.
   - **Expected:** The Before-value column shows `` `(sensitive / hidden)` ``
   - **NOT expected:** The old plaintext secret value shown in the Before column.

5. **Root-boolean-sensitive resource**
   - All attribute rows should show `` `(sensitive)` ``

6. **Array-parent-sensitive resource**
   - Items like `secrets[0]`, `secrets[1]` should show `` `(sensitive)` ``

**Before/After Context:**
- **Before fix:** Passwords such as `P@ssw0rd123!`, API keys, and client secrets appeared in full
  plaintext in the Markdown output whenever an `azapi_resource` or `azuredevops_variable_group`
  was in a Terraform plan.
- **After fix:** Any value marked sensitive by Terraform (`before_sensitive`/`after_sensitive`)
  is replaced with the `(sensitive)` placeholder throughout all rendering paths.

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
- No `(sensitive)` placeholder appears for resources that have no sensitivity metadata.
- Standard Azure resources (VMs, storage accounts, VNets) still render as before.
- AzApi resources without sensitive fields still show all their body values.
- Summary counts (add/change/destroy) are correct.
- All sections (summaries, details, static analysis) render without layout issues.
- No extra whitespace, broken tables, or missing closing `</details>` tags.

---

## Success Criteria

| Check | Expected | Pass Condition |
|---|---|---|
| Sensitive password in AzApi create | `(sensitive)` shown | ✅ No plaintext password visible |
| Non-sensitive value in AzApi create | Plaintext shown | ✅ `sqladmin`, `12.0`, `Enabled`, etc. visible |
| Sensitive property in AzApi update | `(sensitive)` in both Before/After | ✅ No raw secret in either column |
| AzApi delete sensitive before-value | `(sensitive)` shown | ✅ No raw secret visible |
| Variable Group `IsSecret true → false` | Before column masked | ✅ `(sensitive / hidden)` or equiv. |
| Root-boolean-sensitive resource | All attrs masked | ✅ No attr values shown |
| Array parent sensitive (`secrets: true`) | `secrets[0]` etc. masked | ✅ All indexed items masked |
| Non-sensitive resources unchanged | All values present | ✅ No accidental over-masking |
| Render in GitHub Markdown | Layout correct | ✅ Tables, details, code blocks well-formed |
| Render in Azure DevOps Markdown | Layout correct | ✅ Same as GitHub; no layout degradation |

---

## Feedback Opportunities

- Is the `(sensitive)` placeholder visually distinct enough in the table?
- Should `(sensitive)` be formatted differently (e.g., italic, dim) vs. `(sensitive / hidden)` used in Variable Group? Consider normalizing across all paths.
- For root-boolean-sensitive resources where every attribute is masked, should a summary note explain why all values are hidden?
- Does revealing values via `--show-sensitive` feel discoverable enough with the current CLI help text?
