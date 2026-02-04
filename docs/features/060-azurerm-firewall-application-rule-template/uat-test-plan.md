# UAT Test Plan: Custom Template for azurerm_firewall_application_rule_collection

## Goal

Verify that the Azure Firewall application rule collection template renders correctly in GitHub and Azure DevOps PR comments, showing semantic rule-by-rule changes instead of index-based attribute diffs.

## Artifacts

### Feature-Specific Test Artifact (Required)

**Purpose:** Focus testing on the specific changes in this feature (application rule template).

**Artifact Path:** `artifacts/firewall-application-rules-uat.md`

**Creation Instructions:**

**Source Plan:** `examples/firewall-application-rules-demo/plan.json` (create a minimal plan that exercises the feature)

**Command:**
```bash
tfplan2md examples/firewall-application-rules-demo/plan.json > artifacts/firewall-application-rules-uat.md
```

**Rationale:** This plan contains only `azurerm_firewall_application_rule_collection` resources with create, update, and delete scenarios. It demonstrates the semantic diffing capability for application rules without the noise of other resources.

**Key Resources:**
1. `azurerm_firewall_application_rule_collection.new_app_rules` (create)
2. `azurerm_firewall_application_rule_collection.web_rules` (update with all change types)
3. `azurerm_firewall_application_rule_collection.old_rules` (delete)

---

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other resource types or templates.

**Artifact Path:**
- **GitHub:** `artifacts/comprehensive-demo-simple-diff.md`
- **Azure DevOps:** `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using the `generate-demo-artifacts` skill.

**Rationale:** The comprehensive demo includes a wide variety of Azure resources (network rules, role assignments, key vaults, etc.) to catch regressions where the new application rule template accidentally affects other templates or breaks existing functionality.

---

## Test Steps

1. **Generate Feature-Specific Artifact:**
   - Developer creates `examples/firewall-application-rules-demo/plan.json` with application rule scenarios
   - Run: `tfplan2md examples/firewall-application-rules-demo/plan.json > artifacts/firewall-application-rules-uat.md`

2. **Generate Comprehensive Demo (Regression):**
   - Developer invokes `generate-demo-artifacts` skill
   - This generates both GitHub and Azure DevOps versions of the comprehensive demo

3. **Run UAT using the `UAT Tester` agent:**
   - UAT Tester agent uses the `run-uat` skill
   - Posts TWO separate PR comments:
     - **Feature-Specific Report** (first comment, labeled "🎯 Feature Test")
     - **Comprehensive Demo** (second comment, labeled "🔄 Regression Test")

4. **Maintainer Verification:**
   - Review both reports on **GitHub** (in `oocx/tfplan2md-uat` repository)
   - Review both reports on **Azure DevOps** (`https://dev.azure.com/oocx`, project `test`, repository `test`)
   - Follow validation instructions below

5. **Approval/Feedback:**
   - Maintainer comments "approved" or "passed" when rendering is correct
   - Maintainer comments with issues if bugs are found (UAT agent detects and reports back)

---

## Validation Instructions (Test Description)

_These instructions will be used verbatim by the UAT Tester agent as the PR description._

### Feature-Specific Validation

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

#### Specific Resources to Verify

**1. Create Scenario: `azurerm_firewall_application_rule_collection.new_app_rules`**

**Expected Outcome:**
- Collapsible `<details>` section with ➕ Create in the summary line
- Collection metadata displayed: name, priority, action with icon (🟢 Allow or 🔴 Deny)
- "Rules" heading (NOT "Rule Changes")
- Table with columns: Rule Name | Protocols | Source Addresses | Target FQDNs | Description
- All rules listed WITHOUT change indicators (no ➕/🔄/❌/⏺️ in the table)
- Protocols displayed as provided (e.g., `Https:443`, `Http:80,Https:443`)
- Target FQDNs displayed as comma-separated list (e.g., `github.com, *.github.io`)

**Before/After Context:**
- **Before:** Application rule collections used the default template, showing index-based array changes like `rule[0].name`, `rule[1].target_fqdns[0]`
- **After:** Application rule collections now show semantic rule-by-rule display with clear property names

---

**2. Update Scenario: `azurerm_firewall_application_rule_collection.web_rules`**

**Expected Outcome:**
- Collapsible `<details>` section with 🔄 Update in the summary line
- Collection metadata displayed
- "Rule Changes" heading (NOT "Rules")
- Table with "Change" column as the first column
- All four change types visible:
  - ➕ **Added rule** (e.g., `allow-github`) - shows rule properties without diff markup
  - 🔄 **Modified rule** (e.g., `allow-microsoft`) - shows inline diff for changed properties
  - ❌ **Removed rule** (e.g., `allow-old-site`) - shows before values
  - ⏺️ **Unchanged rule** (e.g., `allow-azure`) - shows current values for context

**Exact Attributes to Check:**

**Modified Rule (`allow-microsoft`):**
- **Source Addresses column:** Should show inline diff with strikethrough and insertion formatting
  - Example: `<del style="color:#E5534B;">10.0.0.0/24</del> <ins style="color:#46954A;">10.0.0.0/16</ins>`
  - Or GitHub markdown format: `~~10.0.0.0/24~~ **10.0.0.0/16**` (depending on template style)
- **Other columns:** Should show single values if unchanged (no diff markup)

**Before/After Context:**
- **Before:** Update diffs showed `rule[1].source_addresses[0] = "10.0.0.0/16"` (index-based, unclear which rule)
- **After:** Update diffs show rule name `allow-microsoft` with inline diff in the source_addresses column

---

**3. Delete Scenario: `azurerm_firewall_application_rule_collection.old_rules`**

**Expected Outcome:**
- Collapsible `<details>` section with ❌ Delete in the summary line
- Collection metadata from before state
- "Rules (being deleted)" heading
- Table shows all rules from before state WITHOUT change indicators
- All property values displayed (no diff markup, as entire collection is being deleted)

**Before/After Context:**
- **Before:** Delete operations showed generic attribute list
- **After:** Delete operations show clear table of which rules are being removed

---

#### Edge Cases to Verify

**4. FQDN List Truncation**

Find a rule with more than 5 target FQDNs (if present in test data).

**Expected Outcome:**
- Display format: `fqdn1.com, fqdn2.com, fqdn3.com, ... +N more`
- Only first 3 FQDNs displayed, rest indicated by count

---

**5. Multiple Protocols**

Find a rule with multiple protocols (e.g., `Http:80, Https:443`).

**Expected Outcome:**
- Protocols displayed as comma-separated list in single cell
- Format matches Terraform state (includes port numbers)

---

**6. Optional Properties (if present)**

Check if any rules use `source_ip_groups` or `fqdn_tags`.

**Expected Outcome:**
- If `source_ip_groups` present: Column shows IP group resource IDs
- If `fqdn_tags` present: Column shows tags like `WindowsUpdate`, `AppServiceEnvironment`
- Optional columns may be empty for rules that don't use them (acceptable)

---

**7. Empty Descriptions**

Find a rule with no description.

**Expected Outcome:**
- Description column is empty or shows placeholder (acceptable)
- No rendering errors or "null" text

---

### Regression Validation

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
1. **No Unintended Changes:**
   - Network firewall rules (`azurerm_firewall_network_rule_collection`) still render correctly
   - Role assignments still use role assignment template
   - Other Azure resources render without issues

2. **Application Rules in Context:**
   - If comprehensive demo includes application rule collections, verify they use the new template
   - Semantic rule display appears alongside other resources

3. **Overall Structure:**
   - Summary section displays correctly
   - Resource grouping (by module, if applicable) works
   - Action symbols (➕/🔄/❌/♻️) appear correctly

4. **No Rendering Errors:**
   - No broken tables
   - No extra blank lines or formatting issues
   - All collapsible sections open/close properly

---

## Success Criteria

### Feature-Specific Report

- [ ] Create scenario shows all rules in a clear table (no change indicators)
- [ ] Update scenario shows all four change types (added, modified, removed, unchanged)
- [ ] Modified rules show inline diffs for changed properties only
- [ ] Delete scenario shows rules being removed
- [ ] FQDN lists truncate after 5 items (if applicable)
- [ ] Multiple protocols display correctly
- [ ] Optional properties render without errors
- [ ] Collection metadata (name, priority, action) displays with icons
- [ ] Summary line (for update actions) shows change count and sample changes
- [ ] No markdown rendering errors in GitHub
- [ ] No markdown rendering errors in Azure DevOps

### Comprehensive Demo (Regression)

- [ ] Application rule collections use new template (if present)
- [ ] Network rule collections still render correctly (no regressions)
- [ ] Other resource templates unaffected
- [ ] Overall report structure and formatting correct
- [ ] No markdown rendering errors in GitHub
- [ ] No markdown rendering errors in Azure DevOps

---

## Feedback Opportunities

If the rendering is not as expected, provide feedback in PR comments addressing:

1. **Clarity:** Are the rule changes easy to understand? Is it clear which rules are added, modified, or removed?

2. **Completeness:** Are all relevant properties displayed? Any missing information?

3. **Formatting:** Is the table layout readable? Are inline diffs clear?

4. **Consistency:** Does the application rule template match the style of network rule template?

5. **Edge Cases:** Do edge cases (long FQDN lists, empty descriptions, optional properties) render acceptably?

6. **Regressions:** Any unintended changes to other resource types or templates?

---

## Platform-Specific Considerations

### GitHub

- **Markdown Dialect:** GitHub Flavored Markdown (GFM)
- **Collapsible Sections:** `<details>` and `<summary>` tags should work
- **Inline Styles:** `<del>` and `<ins>` tags with color styles should render
- **Tables:** Pipe-delimited tables should render correctly

### Azure DevOps

- **Markdown Dialect:** Azure DevOps Markdown (CommonMark with extensions)
- **Collapsible Sections:** May render differently than GitHub
- **Inline Styles:** May strip some CSS styles, verify colored diffs appear
- **Tables:** May have different column width handling

**Note:** If rendering differs significantly between platforms, document the differences in feedback.

---

## Approval Process

**Approved When:**
- Maintainer comments with keywords: `approved`, `passed`, `lgtm`, `accept`
- **OR** Maintainer closes the UAT PR (GitHub only)
- **OR** Maintainer marks UAT comment threads as "Resolved" (Azure DevOps)

**Rejected When:**
- Maintainer comments with keywords: `fail`, `reject`, `error`, `bug`, `issue`, `regression`
- UAT Tester agent will stop polling and report failure

**Cleanup:**
- UAT Tester agent automatically closes/abandons PRs and deletes branches after approval or abort

---

## Fallback Validation (Manual)

If UAT automation is unavailable, perform manual validation:

1. **Create GitHub PR:**
   ```bash
   scripts/uat-github.sh create artifacts/firewall-application-rules-uat.md "UAT: Azure Firewall application rule template"
   scripts/uat-github.sh comment <pr-number> artifacts/comprehensive-demo-simple-diff.md
   ```

2. **Create Azure DevOps PR:**
   ```bash
   scripts/uat-azdo.sh setup
   scripts/uat-azdo.sh create artifacts/firewall-application-rules-uat.md "UAT: Azure Firewall application rule template"
   scripts/uat-azdo.sh comment <pr-id> artifacts/comprehensive-demo.md
   ```

3. **Review PRs manually** using validation instructions above

4. **Cleanup after approval:**
   ```bash
   scripts/uat-github.sh cleanup <pr-number>
   scripts/uat-azdo.sh cleanup <pr-id>
   ```

---

## Notes

- **Test Data Creation:** The Developer agent is responsible for creating `examples/firewall-application-rules-demo/plan.json` with realistic application rule scenarios before UAT
- **Artifact Generation:** Must be done after all code changes are complete and tests pass
- **Maintainer Availability:** UAT requires Maintainer review; schedule accordingly
- **Iteration:** If UAT fails, fix issues, regenerate artifacts, and re-run UAT with new PR

---

## References

- **Feature Specification:** `docs/features/060-azurerm-firewall-application-rule-template/specification.md`
- **Architecture:** `docs/features/060-azurerm-firewall-application-rule-template/architecture.md`
- **Test Plan:** `docs/features/060-azurerm-firewall-application-rule-template/test-plan.md`
- **Testing Strategy:** `docs/testing-strategy.md`
- **UAT Wrapper Scripts:** `scripts/uat-run.sh`, `scripts/uat-github.sh`, `scripts/uat-azdo.sh`
