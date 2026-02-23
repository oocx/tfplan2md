# UAT Test Plan: Terraform Outputs

## Goal

Verify that Terraform outputs render correctly in GitHub and Azure DevOps PR comments, with proper table formatting, sensitivity masking, display name mappings, and intelligent positioning (module outputs within modules, global outputs at the end).

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific changes in this feature. This artifact MUST be real tfplan2md output, not synthetic or simulated.

**Source Plan Path:** `docs/features/097-terraform-outputs/uat-plan.json`

**Rendered Output Path:** `docs/features/097-terraform-outputs/uat-plan.md`

**Plan Requirements:**
- **MUST be a real Terraform plan JSON** that exercises the outputs feature
- **MUST cover all changes** that affect markdown output
- **MUST include edge cases** relevant to the feature

**Rationale:** This plan demonstrates the Terraform outputs rendering feature with:
- Global outputs at the end of the report
- Module outputs positioned within their module sections
- Mix of sensitive and non-sensitive outputs
- Computed values (`(known after apply)`)
- Azure resource IDs with display name mappings applied to output values
- Various output actions (create, update, delete, no-op)
- Outputs with and without descriptions

**Key Resources:**
1. **Global Output:** `repository_url` - Demonstrates create action output with Azure DevOps repository ID formatted with display name
2. **Global Output:** `storage_key` - Demonstrates sensitive value masking (default behavior shows `(sensitive value)`)
3. **Global Output:** `pipeline_id` - Demonstrates computed value (`(known after apply)`)
4. **Module Output:** `module.database.connection_string` - Demonstrates sensitive output within module section
5. **Module Output:** `module.database.database_id` - Demonstrates Azure resource ID formatting within output value
6. **Module Output:** `module.network.vnet_id` - Demonstrates no-op action output

**Coverage:**
- ✅ Global outputs positioned after all resource changes (2nd-level header `## Outputs`)
- ✅ Module outputs positioned within module sections (4th-level header `#### Outputs`)
- ✅ 4-column table format: Name, Description, Sensitive, Value
- ✅ Sensitive values masked as `(sensitive value)` by default
- ✅ Computed values shown as `(known after apply)`
- ✅ Display name mappings applied to output values (Azure resource IDs, repository IDs)
- ✅ Alphabetical ordering within each outputs section
- ✅ Outputs with descriptions vs outputs without (shown as `-`)
- ✅ Various output actions (create, update, delete, no-op)

**Example Creation Command:**
```bash
# Generate the rendered output from the plan
tfplan2md docs/features/097-terraform-outputs/uat-plan.json > docs/features/097-terraform-outputs/uat-plan.md
```

---

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas.

**Artifact Path:** 
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using `generate-demo-artifacts` skill.

## Test Steps

1. Developer creates `uat-plan.json` based on this specification
2. Developer generates `uat-plan.md` from the plan
3. Code Reviewer validates both files exist and are complete
4. UAT Tester uses `uat-plan.md` for testing
5. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific changes using `uat-plan.md`
   - **Comprehensive Demo**: Regression test for side effects
6. Verify both reports on GitHub and Azure DevOps

## Validation Instructions (Test Description)

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

#### 1. Global Outputs Section

**Location:** Scroll to the end of the report (after all resource changes and module sections).

**Verify:**
- Section header is `## Outputs` (2nd-level header)
- Table has exactly 4 columns: **Name**, **Description**, **Sensitive**, **Value**
- Table header row uses proper markdown syntax with pipes and dashes

**Specific Outputs to Check:**

**Output: `repository_url`**
- Name column: Shows `` `repository_url` `` (code-formatted with backticks)
- Description column: Shows plain text description or `-` if no description
- Sensitive column: Shows `-` (not sensitive)
- Value column: Shows the repository URL, code-formatted with backticks
- If the output value contains an Azure DevOps repository ID, verify display name mapping is applied (e.g., "Repository `repo-name` (80128bc2-...)")

**Output: `storage_key`**
- Name column: Shows `` `storage_key` `` (code-formatted)
- Description column: Shows "Primary access key for storage account" or similar
- Sensitive column: Shows `Yes` (plain text, not code-formatted)
- Value column: Shows `(sensitive value)` (plain text, not code-formatted, not in backticks)

**Output: `pipeline_id`**
- Name column: Shows `` `pipeline_id` `` (code-formatted)
- Description column: Shows description or `-`
- Sensitive column: Shows `-`
- Value column: Shows `(known after apply)` (plain text, not code-formatted, not in backticks)

**Alphabetical Ordering:**
- Verify outputs are listed alphabetically by name (case-sensitive ordinal sort)

---

#### 2. Module Outputs Section

**Location:** Within a module section (e.g., `### Module: \`module.database\``)

**Verify:**
- Section header is `#### Outputs` (4th-level header, nested under module's 3rd-level header)
- Table has same 4-column format as global outputs
- Module outputs appear immediately after the module's resource changes (before the next module or before global outputs)

**Specific Outputs to Check:**

**Output: `connection_string` (in `module.database`)**
- Name column: Shows `` `connection_string` `` (code-formatted)
- Description column: Shows description or `-`
- Sensitive column: Shows `Yes`
- Value column: Shows `(sensitive value)` (masked by default)

**Output: `database_id` (in `module.database`)**
- Name column: Shows `` `database_id` `` (code-formatted)
- Description column: Shows description or `-`
- Sensitive column: Shows `-`
- Value column: If value is an Azure resource ID (e.g., `/subscriptions/.../providers/Microsoft.DBforPostgreSQL/servers/...`), verify it's formatted with display name mapping (e.g., "PostgreSQL server `db-name` in resource group `rg-name`")

**Output: `vnet_id` (in `module.network`, no-op action)**
- Name column: Shows `` `vnet_id` `` (code-formatted)
- Description column: Shows description or `-`
- Sensitive column: Shows `-`
- Value column: Shows the current value (no-op outputs are included, not filtered out)

**Alphabetical Ordering:**
- Verify outputs within each module are listed alphabetically

---

#### 3. Table Formatting

**Verify for both global and module output tables:**

- Table header row: `| Name | Description | Sensitive | Value |`
- Separator row: `|------|-------------|-----------|-------|`
- Each output is exactly one table row
- Cells use proper markdown escaping (no broken table cells)
- Code-formatted items (names and non-masked values) use backticks
- Plain text items (descriptions, `Yes`, `-`, `(sensitive value)`, `(known after apply)`) do NOT use backticks

---

#### 4. Positioning Verification

**Verify:**
- Module outputs appear AFTER the module's resource changes (within the module's collapsible sections or immediately after)
- Module outputs appear BEFORE the next module section or the global outputs section
- Global outputs appear AFTER all module sections
- Global outputs appear BEFORE debug information (if present)

**Structure Example:**
```
## Summary
...

## Resource Changes

### Module: `module.network`
<resource changes>
#### Outputs
<module outputs table>

---

### Module: `module.database`
<resource changes>
#### Outputs
<module outputs table>

---

## Outputs
<global outputs table>
```

---

#### 5. Display Name Mappings

**Verify:**
- Azure resource IDs in output values are formatted with display names (e.g., "Key Vault `kv-name` in resource group `rg-name`")
- Azure DevOps repository IDs show repository names if available
- Principal IDs (user/group/service principal GUIDs) show mapped names if principal mappings are provided
- Subscription GUIDs show subscription display names if mappings are provided
- Formatting matches the style used for resource attributes (consistency)

**Before/After Context:**
- **Before (without feature):** Outputs section doesn't exist; users cannot see planned outputs in tfplan2md reports
- **After (with feature):** Outputs are clearly displayed with descriptions, sensitivity indicators, and display-enhanced values

---

#### 6. Sensitivity and Computed Values

**Verify:**
- Sensitive outputs show `(sensitive value)` in the Value column (plain text, no backticks)
- Sensitive outputs show `Yes` in the Sensitive column
- Computed outputs (unknown values) show `(known after apply)` in the Value column (plain text, no backticks)
- Non-sensitive, non-computed outputs show actual values in code-formatted text (backticks)

---

#### 7. Edge Cases

**Verify:**
- Outputs without descriptions show `-` in Description column (not empty cell, not "N/A", just `-`)
- Outputs with empty module address are treated as global outputs (appear in `## Outputs` section)
- If a module has only outputs (no resource changes), verify the module section still appears with just the outputs table
- If there are no outputs at all (neither global nor module), verify no `## Outputs` or `#### Outputs` sections appear (omitted entirely)

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
- No unintended changes to existing resource rendering
- If comprehensive demo includes outputs (it might not if the demo plan has no outputs), verify they render correctly
- All existing sections (Summary, Resource Changes, modules) render normally
- No broken table formatting elsewhere in the report
- No performance degradation (report generates quickly)

---

## Expected Outcome

**Feature-Specific Report:**
- Global outputs section clearly visible at end of report with 2nd-level header
- Module outputs sections visible within their respective module sections with 4th-level headers
- All output values correctly formatted:
  - Sensitive values masked as `(sensitive value)` (plain text)
  - Computed values shown as `(known after apply)` (plain text)
  - Normal values code-formatted with backticks
- Display name mappings applied to Azure resource IDs and other identifiers in output values
- Alphabetical ordering maintained within each outputs section
- Table format is clean, readable, and valid markdown

**Comprehensive Demo:**
- No unintended changes to resource rendering
- No broken formatting
- No regressions in existing features

---

## Success Criteria

- [ ] Global outputs render in dedicated section with 2nd-level header
- [ ] Module outputs render within module sections with 4th-level headers
- [ ] 4-column table format is correct and readable
- [ ] Sensitive values are masked by default
- [ ] Computed values show `(known after apply)`
- [ ] Display name mappings apply to output values
- [ ] Alphabetical ordering is maintained
- [ ] Outputs without descriptions show `-`
- [ ] Table markdown is valid in both GitHub and Azure DevOps
- [ ] No regressions in comprehensive demo

---

## Notes for UAT Tester

1. **Two Artifacts Required:** This feature requires both a feature-specific artifact (`uat-plan.md`) and the comprehensive demo for full validation.

2. **Focus Areas:**
   - Table formatting (4 columns, proper headers)
   - Positioning (module vs global)
   - Masking behavior (sensitive and computed values)
   - Display name mappings in output values
   - Alphabetical ordering

3. **Platform Differences:** Test on both GitHub and Azure DevOps to catch any markdown rendering differences between platforms.

4. **Optional Test:** If time permits, test with `--show-sensitive` flag to verify sensitive values are revealed (code-formatted) when flag is present. This would require generating a second version of `uat-plan.md` with the flag enabled.

5. **No Outputs Scenario:** If testing a plan with no outputs, verify the Outputs sections are completely omitted (no placeholder text).
