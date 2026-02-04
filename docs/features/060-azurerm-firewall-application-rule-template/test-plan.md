# Test Plan: Custom Template for azurerm_firewall_application_rule_collection

## Overview

This test plan defines how to verify the custom Scriban template and supporting infrastructure for `azurerm_firewall_application_rule_collection` resources. The implementation follows the Factory → ViewModel → Template pattern used by network rule collections, with semantic diffing of application firewall rules.

**Related Documents:**
- **Specification:** `docs/features/060-azurerm-firewall-application-rule-template/specification.md`
- **Architecture:** `docs/features/060-azurerm-firewall-application-rule-template/architecture.md`
- **Testing Strategy:** `docs/testing-strategy.md`

**Reference Implementation:** `azurerm_firewall_network_rule_collection` template and supporting classes

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Template renders create actions with all rules | TC-01, TC-02 | Snapshot |
| Template renders update actions with rule changes | TC-03, TC-04, TC-05, TC-06, TC-07 | Snapshot |
| Template renders delete actions with all rules | TC-08 | Snapshot |
| Template falls back to attribute changes when rules unavailable | TC-09 | Snapshot |
| View model factory correctly computes added rules | TC-10 | Unit |
| View model factory correctly computes modified rules | TC-11 | Unit |
| View model factory correctly computes removed rules | TC-12 | Unit |
| View model factory correctly computes unchanged rules | TC-13 | Unit |
| Change indicators display correctly (➕/🔄/❌/⏺️) | TC-03, TC-04, TC-05, TC-06 | Snapshot |
| Inline diffs show before/after for modified properties | TC-05, TC-11 | Snapshot, Unit |
| FQDN lists truncate when > 5 items | TC-14 | Unit |
| Protocol formatting preserves Terraform state format | TC-04 | Snapshot |
| Optional properties (source_ip_groups, fqdn_tags) render correctly | TC-15, TC-16 | Snapshot |
| Summary line shows count and sample changes | TC-17, TC-18, TC-19, TC-20 | Unit |
| Factory adapter correctly registers and applies view model | TC-21 | Integration |
| All markdown output passes markdownlint validation | TC-22 | Integration |
| Template produces no consecutive blank lines | TC-23 | Invariant |
| Table rows are consecutive without blank lines | TC-24 | Invariant |

## Test Cases

### TC-01: Create Action - New Application Rule Collection

**Type:** Snapshot Test

**Description:**
Verify that a new `azurerm_firewall_application_rule_collection` resource displays all rules being created with proper formatting.

**Preconditions:**
- Test data file with create action containing 2-3 application rules
- Rules include core properties: name, protocols, source_addresses, target_fqdns, description

**Test Steps:**
1. Parse plan JSON with create action for application rule collection
2. Build report model using `ReportModelBuilder`
3. Render markdown using `MarkdownRenderer`
4. Compare output against approved snapshot

**Expected Result:**
- Collapsible `<details>` section with ➕ Create in summary
- Collection metadata displayed: name, priority, action (with 🟢 Allow or 🔴 Deny icon)
- "Rules" heading
- Table with columns: Rule Name | Protocols | Source Addresses | Target FQDNs | Description
- All rules from `after` state displayed
- No change indicators (not applicable for create)
- Markdown passes lint validation

**Test Data:**
`TestData/firewall-application-rule-changes.json` (create scenario)

**Snapshot File:**
`TestData/Snapshots/firewall-application-rules.md`

---

### TC-02: Create Action - Application Rules with Multiple Protocols

**Type:** Snapshot Test

**Description:**
Verify that application rules with multiple protocols (e.g., `Http:80`, `Https:443`) display correctly in the protocols column.

**Preconditions:**
- Test data with create action
- At least one rule has multiple protocols with port numbers

**Test Steps:**
1. Parse plan JSON with multi-protocol rules
2. Build and render markdown
3. Verify protocols column displays comma-separated format

**Expected Result:**
- Protocols column shows: `Http:80, Https:443` (comma-separated)
- Format matches Terraform state representation
- No parsing or splitting into separate columns

**Test Data:**
`TestData/firewall-application-rule-changes.json` (create scenario with multi-protocol rule)

---

### TC-03: Update Action - Added Rules

**Type:** Snapshot Test

**Description:**
Verify that added rules display with ➕ change indicator and all property values.

**Preconditions:**
- Test data with update action
- `after.rule` array contains rules not present in `before.rule` array

**Test Steps:**
1. Parse plan JSON with update action
2. Build report model
3. Render markdown
4. Verify added rules appear with ➕ indicator

**Expected Result:**
- "Rule Changes" heading
- Added rules have ➕ in Change column
- All properties display formatted values (no diffs)
- Rule name comparison is case-insensitive

**Test Data:**
`TestData/firewall-application-rule-changes.json` (update scenario)

---

### TC-04: Update Action - Modified Rules

**Type:** Snapshot Test

**Description:**
Verify that modified rules display with 🔄 change indicator and inline diffs for changed properties.

**Preconditions:**
- Test data with update action
- At least one rule exists in both before/after with different property values

**Test Steps:**
1. Parse plan JSON with modified rule
2. Build report model
3. Render markdown
4. Verify inline diffs use strikethrough/insertion markup

**Expected Result:**
- Modified rules have 🔄 in Change column
- Changed properties show inline diff: `- before + after` format
- Unchanged properties show single value (no diff)
- Uses `FormatDiff` helper for consistent formatting

**Test Data:**
`TestData/firewall-application-rule-changes.json` (update scenario with modified rule)

**Example Modified Property:**
```markdown
| 🔄 | allow-microsoft | Http:80, Https:443 | <del>10.0.0.0/24</del> <ins>10.0.0.0/16</ins> | *.microsoft.com | Microsoft services |
```

---

### TC-05: Update Action - Removed Rules

**Type:** Snapshot Test

**Description:**
Verify that removed rules display with ❌ change indicator and before values.

**Preconditions:**
- Test data with update action
- `before.rule` array contains rules not present in `after.rule` array

**Test Steps:**
1. Parse plan JSON with removed rule
2. Build report model
3. Render markdown
4. Verify removed rules show before state

**Expected Result:**
- Removed rules have ❌ in Change column
- All properties show before values
- No diff markup (values are not changing, entire rule is removed)

**Test Data:**
`TestData/firewall-application-rule-changes.json` (update scenario)

---

### TC-06: Update Action - Unchanged Rules

**Type:** Snapshot Test

**Description:**
Verify that unchanged rules display with ⏺️ change indicator for context.

**Preconditions:**
- Test data with update action
- At least one rule exists in both before/after with identical property values

**Test Steps:**
1. Parse plan JSON with unchanged rule
2. Build report model
3. Render markdown
4. Verify unchanged rules included for context

**Expected Result:**
- Unchanged rules have ⏺️ in Change column
- All properties show current values
- No diff markup (values identical)
- Provides context for what's NOT changing

**Test Data:**
`TestData/firewall-application-rule-changes.json` (update scenario)

---

### TC-07: Update Action - Mixed Changes (All Change Types)

**Type:** Snapshot Test

**Description:**
Verify that an update with added, modified, removed, and unchanged rules displays all change types correctly in a single table.

**Preconditions:**
- Test data with update action
- Rules array contains at least one of each change type: added, modified, removed, unchanged

**Test Steps:**
1. Parse plan JSON with all change types
2. Build report model
3. Render markdown
4. Verify all rows appear in correct order

**Expected Result:**
- Single "Rule Changes" table with all rules
- Each rule has correct change indicator
- Modified rules show inline diffs
- Unchanged rules provide context
- Table is sorted logically (e.g., unchanged first, then modified, added, removed)

**Test Data:**
`TestData/firewall-application-rule-changes.json` (comprehensive update scenario)

---

### TC-08: Delete Action - Deleting Application Rule Collection

**Type:** Snapshot Test

**Description:**
Verify that deleted application rule collections display all rules being removed.

**Preconditions:**
- Test data with delete action
- `before` state contains rules, `after` state is null

**Test Steps:**
1. Parse plan JSON with delete action
2. Build report model
3. Render markdown
4. Verify all before rules displayed

**Expected Result:**
- Collapsible `<details>` section with ❌ Delete in summary
- Collection metadata from before state
- "Rules (being deleted)" heading
- Table shows all rules from before state
- No change indicators (entire collection being deleted)

**Test Data:**
`TestData/firewall-application-rule-changes.json` (delete scenario)

---

### TC-09: Update Action - Fallback to Attribute Changes

**Type:** Snapshot Test

**Description:**
Verify that when rule data is unavailable (e.g., computed values), template falls back to displaying attribute changes table.

**Preconditions:**
- Test data with update action
- Rules property is null, computed, or malformed in both before/after states

**Test Steps:**
1. Parse plan JSON with computed rules
2. Build report model
3. Render markdown
4. Verify attribute changes table displayed instead of rule changes

**Expected Result:**
- Collection metadata displayed
- Nested collapsible "Attribute Changes" section
- Standard attribute changes table with Before | After columns
- No "Rule Changes" or "Rules" heading
- Shows properties like priority, rule (computed)

**Test Data:**
`TestData/firewall-application-rule-changes.json` (fallback scenario)

**Example Output:**
```markdown
**Collection:** `computed-rules` | **Priority:** `150` | **Action:** 🟢 Allow

<details>
<summary>Attribute Changes</summary>

| Attribute | Before | After |
| ----------- | -------- | ------- |
| priority | 150 | 200 |
| rule | (computed) | (computed) |

</details>
```

---

### TC-10: Factory - Extract and Compute Added Rules

**Type:** Unit Test

**Description:**
Verify that `FirewallApplicationRuleCollectionViewModelFactory` correctly identifies rules present in after state but not in before state.

**Preconditions:**
- Mock `ResourceChange` object with before/after rules
- After contains rule "allow-github" not present in before

**Test Steps:**
1. Create test `ResourceChange` with added rule
2. Call `FirewallApplicationRuleCollectionViewModelFactory.Build()`
3. Inspect `RuleChanges` collection
4. Verify added rule has ➕ change indicator

**Expected Result:**
- `RuleChanges` contains row with `Change = "➕"`
- `Name` matches rule name
- All properties formatted correctly
- Rule name comparison is case-insensitive

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs` or new factory test file

---

### TC-11: Factory - Extract and Compute Modified Rules

**Type:** Unit Test

**Description:**
Verify that factory correctly identifies rules with same name but different property values, and formats inline diffs.

**Preconditions:**
- Mock `ResourceChange` with rule "allow-microsoft" in both before/after
- Rule has changed source_addresses property

**Test Steps:**
1. Create test `ResourceChange` with modified rule
2. Call factory `Build()` method
3. Inspect modified rule row
4. Verify inline diff formatting

**Expected Result:**
- `RuleChanges` contains row with `Change = "🔄"`
- Changed property shows diff format: `- old + new`
- Unchanged properties show single value
- Uses `FormatDiff` helper for markup

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

---

### TC-12: Factory - Extract and Compute Removed Rules

**Type:** Unit Test

**Description:**
Verify that factory correctly identifies rules present in before state but not in after state.

**Preconditions:**
- Mock `ResourceChange` with rule in before, missing in after

**Test Steps:**
1. Create test `ResourceChange` with removed rule
2. Call factory `Build()` method
3. Verify removed rule has ❌ change indicator

**Expected Result:**
- `RuleChanges` contains row with `Change = "❌"`
- All properties show before values
- No diff formatting (entire rule removed)

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

---

### TC-13: Factory - Extract and Compute Unchanged Rules

**Type:** Unit Test

**Description:**
Verify that factory correctly identifies rules with identical property values in before/after states.

**Preconditions:**
- Mock `ResourceChange` with rule identical in before/after

**Test Steps:**
1. Create test `ResourceChange` with unchanged rule
2. Call factory `Build()` method
3. Verify unchanged rule has ⏺️ indicator

**Expected Result:**
- `RuleChanges` contains row with `Change = "⏺️"`
- All properties show current values
- No diff formatting

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

---

### TC-14: Factory - FQDN List Truncation

**Type:** Unit Test

**Description:**
Verify that FQDN lists exceeding 5 items are truncated to first 3 with "... +N more" suffix.

**Preconditions:**
- Mock rule with 7 target_fqdns in after state

**Test Steps:**
1. Create test rule with long FQDN list: `["*.microsoft.com", "*.azure.com", "*.windows.net", "*.office.com", "*.live.com", "*.msn.com", "*.bing.com"]`
2. Call factory `Build()` method
3. Inspect `TargetFqdns` property in view model

**Expected Result:**
- `TargetFqdns = "*.microsoft.com, *.azure.com, *.windows.net, ... +4 more"`
- Truncation threshold is 5 items
- First 3 items displayed

**Test Location:**
New unit test in `FirewallApplicationRuleCollectionViewModelFactoryTests.cs`

---

### TC-15: Optional Property - Source IP Groups

**Type:** Snapshot Test

**Description:**
Verify that `source_ip_groups` property displays correctly when present, and column remains empty when not used.

**Preconditions:**
- Test data with rule using `source_ip_groups` instead of `source_addresses`

**Test Steps:**
1. Parse plan JSON with source_ip_groups
2. Build and render markdown
3. Verify column displays values

**Expected Result:**
- Source IP Groups column contains formatted values (e.g., comma-separated resource IDs)
- Source Addresses column may be empty for that rule
- Optional columns always present in table (accept empty cells)

**Test Data:**
`TestData/firewall-application-rule-changes.json` (scenario with source_ip_groups)

---

### TC-16: Optional Property - FQDN Tags

**Type:** Snapshot Test

**Description:**
Verify that `fqdn_tags` property displays correctly when present (alternative to target_fqdns).

**Preconditions:**
- Test data with rule using `fqdn_tags` (e.g., "WindowsUpdate", "AppServiceEnvironment")

**Test Steps:**
1. Parse plan JSON with fqdn_tags
2. Build and render markdown
3. Verify FQDN Tags column displays values

**Expected Result:**
- FQDN Tags column shows comma-separated tags
- Target FQDNs column may be empty for that rule
- Tags formatted without semantic icons (plain text)

**Test Data:**
`TestData/firewall-application-rule-changes.json` (scenario with fqdn_tags)

---

### TC-17: Summary - Update Action with Changes

**Type:** Unit Test

**Description:**
Verify that `BuildChangedAttributesSummary` generates correct summary line for update actions with rule changes.

**Preconditions:**
- View model with 4 rule changes: 1 added, 1 modified, 1 removed, 1 unchanged
- Action = "update"

**Test Steps:**
1. Create `FirewallApplicationRuleCollectionViewModel` with rule changes
2. Call `FirewallApplicationRuleCollectionViewModelFactory.BuildChangedAttributesSummary(viewModel, "update")`
3. Verify summary format

**Expected Result:**
- Summary format: `"4🔧 ➕ allow-github, 🔄 allow-microsoft, ❌ allow-old-site, +1 more"`
- Unchanged rules not included in summary
- Truncates after first 3 changes
- Non-breaking space (U+00A0) after emoji and count

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

**Test Method Name:**
`BuildChangedAttributesSummary_WhenMoreThanThreeChanges_Truncates()`

---

### TC-18: Summary - Update Action with No Changes

**Type:** Unit Test

**Description:**
Verify that summary is empty when update action has only unchanged rules.

**Preconditions:**
- View model with only unchanged rules (⏺️ indicator)
- Action = "update"

**Test Steps:**
1. Create view model with unchanged rules only
2. Call `BuildChangedAttributesSummary()`
3. Verify empty string returned

**Expected Result:**
- Summary = `""`
- No count, no rule names displayed

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

**Test Method Name:**
`BuildChangedAttributesSummary_WhenNoChanges_ReturnsEmpty()`

---

### TC-19: Summary - Non-Update Action

**Type:** Unit Test

**Description:**
Verify that summary is empty for create and delete actions.

**Preconditions:**
- View model with rule changes
- Action = "create" or "delete"

**Test Steps:**
1. Create view model with rules
2. Call `BuildChangedAttributesSummary()` with action = "create"
3. Verify empty string returned

**Expected Result:**
- Summary = `""` for create action
- Summary = `""` for delete action
- Summary only applies to update actions

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

**Test Method Name:**
`BuildChangedAttributesSummary_WhenNotUpdate_ReturnsEmpty()`

---

### TC-20: Summary - Rule Name Formatting

**Type:** Unit Test

**Description:**
Verify that summary preserves rule names without backticks, then wraps in `<code>` tags for HTML output.

**Preconditions:**
- View model with rule name not wrapped in backticks

**Test Steps:**
1. Create view model with rule name = "allow-dns" (no backticks)
2. Call `BuildChangedAttributesSummary()`
3. Verify HTML `<code>` tags used

**Expected Result:**
- Summary wraps name in `<code>allow-dns</code>` tags
- Non-breaking spaces around emoji: `1🔧\u00A0➕\u00A0<code>allow-dns</code>`

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallApplicationRuleCollectionSummaryTests.cs`

**Test Method Name:**
`BuildChangedAttributesSummary_WhenNameNotCodeWrapped_PreservesText()`

---

### TC-21: Integration - Factory Registration and Application

**Type:** Integration Test

**Description:**
Verify that `FirewallApplicationRuleCollectionFactory` is correctly registered in `AzureRMModule` and applies view model to `ResourceChangeModel`.

**Preconditions:**
- Factory registered in `AzureRMModule.RegisterFactories()` for resource type `azurerm_firewall_application_rule_collection`
- Test plan JSON with application rule collection

**Test Steps:**
1. Parse full plan JSON containing `azurerm_firewall_application_rule_collection`
2. Create `ProviderRegistry` with `AzureRMModule`
3. Build report model using `ReportModelBuilder`
4. Verify `ResourceChangeModel.FirewallApplicationRuleCollection` property is populated

**Expected Result:**
- Factory correctly invoked for matching resource type
- `ResourceChangeModel.FirewallApplicationRuleCollection` is not null
- `ResourceChangeModel.ChangedAttributesSummary` populated for update actions
- Template resolves to `firewall_application_rule_collection.sbn`

**Test Location:**
Existing integration test or new test in `MarkdownRendererResourceTemplateTests.cs`

---

### TC-22: Integration - Markdown Lint Validation

**Type:** Integration Test (Markdown Lint)

**Description:**
Verify that all rendered markdown for application rule collections passes markdownlint-cli2 validation with no errors.

**Preconditions:**
- Test data with all scenarios (create, update, delete, fallback)
- Docker image `davidanson/markdownlint-cli2:v0.20.0` available

**Test Steps:**
1. Render markdown for all test scenarios
2. Run markdownlint-cli2 via Docker on each output
3. Verify exit code = 0 (no lint errors)

**Expected Result:**
- All markdown outputs pass lint validation
- No rule violations (MD001-MD999)
- Tables properly formatted
- No consecutive blank lines (MD012)
- Headings surrounded by blank lines

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownLintIntegrationTests.cs`

**Test Method Name:**
`Lint_FirewallApplicationRules_PassesAllRules()`

---

### TC-23: Invariant - No Consecutive Blank Lines

**Type:** Invariant Test

**Description:**
Verify that rendered markdown never contains more than one consecutive blank line (MD012 violation).

**Preconditions:**
- Test data with application rule collection scenarios

**Test Steps:**
1. Render markdown for test plan
2. Search for pattern `\n\n\n` (three newlines = two blank lines)
3. Assert no matches found

**Expected Result:**
- Markdown contains no consecutive blank lines
- All sections properly spaced with exactly one blank line between elements

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs`

**Test Method Name:**
`Invariant_NoConsecutiveBlankLines_FirewallApplicationRules()`

---

### TC-24: Invariant - Table Rows Consecutive

**Type:** Invariant Test

**Description:**
Verify that table rows in rule changes table are consecutive without blank lines between rows.

**Preconditions:**
- Test data with update action containing multiple rules

**Test Steps:**
1. Render markdown with rule changes table
2. Parse table using Markdig
3. Verify all table rows are consecutive
4. Assert no blank lines between `|` delimited rows

**Expected Result:**
- Rule changes table has consecutive rows
- No `\n\n` pattern between table rows
- Table structure valid per Markdown spec

**Test Location:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownInvariantTests.cs`

**Test Method Name:**
`Invariant_NoBlankLinesBetweenTableRows_FirewallApplicationRules()`

---

## Test Data Requirements

### Primary Test Data File

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/firewall-application-rule-changes.json`

**Contents:**
A Terraform plan JSON file generated from a real `terraform plan -json` output containing:

#### Scenario 1: Create Action
- Resource: `azurerm_firewall_application_rule_collection.new_app_rules`
- Collection: `new-app-rules`
- Priority: `100`
- Action: `Allow`
- Rules: 2-3 application rules with:
  - Varied protocols: `Https:443`, `Http:80`, `Http:80,Https:443`
  - Source addresses: CIDR ranges
  - Target FQDNs: 2-5 FQDNs per rule
  - Descriptions

#### Scenario 2: Update Action (Comprehensive)
- Resource: `azurerm_firewall_application_rule_collection.web_rules`
- Collection: `web-rules`
- Priority: `200`
- Action: `Allow`
- Before state: 4 rules
- After state: 4 rules (1 added, 1 modified, 1 removed, 1 unchanged)
- Changes:
  - **Added:** `allow-github` - New rule for GitHub access
  - **Modified:** `allow-microsoft` - Changed source_addresses from /24 to /16
  - **Removed:** `allow-old-site` - Deprecated legacy rule
  - **Unchanged:** `allow-azure` - No changes to Azure services rule

#### Scenario 3: Delete Action
- Resource: `azurerm_firewall_application_rule_collection.old_rules`
- Collection: `old-rules`
- Priority: `300`
- Action: `Deny`
- Before state: 2 rules
- After state: null (being deleted)

#### Scenario 4: Fallback (Computed Rules)
- Resource: `azurerm_firewall_application_rule_collection.computed_rules`
- Collection: `computed-rules`
- Priority: `150` → `200` (priority change)
- Rules: marked as `(known after apply)` or null in both before/after

#### Scenario 5: Optional Properties
- Resource: `azurerm_firewall_application_rule_collection.advanced_rules`
- Rules include:
  - One rule with `source_ip_groups` instead of `source_addresses`
  - One rule with `fqdn_tags` (e.g., `["WindowsUpdate"]`) instead of `target_fqdns`

#### Scenario 6: Edge Cases
- Rule with long FQDN list (7+ items) to test truncation
- Rule with empty description
- Rule with multiple protocols and ports

### Snapshot Files

**File:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/firewall-application-rules.md`

**Contents:**
Expected markdown output for all scenarios in the test data file, showing:
- Proper formatting of all change indicators
- Inline diffs for modified properties
- FQDN truncation
- Optional property handling
- Valid markdown structure

**Generation:**
1. Implement the feature completely
2. Generate markdown output: `dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj TestData/firewall-application-rule-changes.json > expected-output.md`
3. Review output manually to ensure correctness
4. Save as snapshot file

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty description | Display empty cell or "(no description)" placeholder | TC-02 |
| Long FQDN list (> 5 items) | Truncate to first 3 with "... +N more" | TC-14 |
| Multiple protocols | Display comma-separated: `Http:80, Https:443` | TC-02, TC-04 |
| Rule name case insensitivity | Match "Allow-HTTP" with "allow-http" | TC-10, TC-11, TC-12 |
| source_ip_groups without source_addresses | Display source_ip_groups, leave source_addresses empty | TC-15 |
| fqdn_tags without target_fqdns | Display fqdn_tags, leave target_fqdns empty | TC-16 |
| Computed rules (unknown after apply) | Fall back to attribute changes table | TC-09 |
| Null before state (create) | Display all rules from after state | TC-01 |
| Null after state (delete) | Display all rules from before state | TC-08 |
| Empty rule arrays | Display "No rules" message or empty table | TC-09 |
| Malformed JSON in rule property | Gracefully handle with error message or fallback | Implicit in TC-09 |

---

## Non-Functional Tests

### Performance
- **Requirement:** Template rendering should complete in < 100ms for typical plan (< 10 rule changes)
- **Test:** Measure rendering time in snapshot tests, log warning if > 100ms
- **Rationale:** Maintain responsiveness for large Terraform plans

### Error Handling
- **Requirement:** Factory should gracefully handle missing or malformed rule data
- **Test:** TC-09 validates fallback to attribute changes when rules unavailable
- **Rationale:** Terraform state may have computed values or unexpected formats

### Compatibility
- **Requirement:** Template must render correctly in GitHub and Azure DevOps markdown parsers
- **Test:** UAT test plan (separate document) defines visual verification steps
- **Rationale:** Different platforms have subtle markdown rendering differences

---

## User Acceptance Testing (UAT)

For comprehensive UAT covering visual rendering in GitHub and Azure DevOps, see:

**UAT Test Plan:** `docs/features/060-azurerm-firewall-application-rule-template/uat-test-plan.md`

The UAT plan defines:
- Feature-specific test artifact creation
- Validation steps for maintainer review
- Regression testing with comprehensive demo

UAT is executed after all automated tests pass.

---

## Open Questions

### Question 1: Snapshot Test Organization

Should firewall application rule tests:
1. Reuse the existing `Snapshot_FirewallRules_MatchesBaseline()` test (extend test data)?
2. Create a new separate snapshot test `Snapshot_FirewallApplicationRules_MatchesBaseline()`?
3. Create multiple snapshot tests (one per scenario: create, update, delete)?

**Recommendation:** Create a new separate snapshot test to keep test cases focused and easy to debug. Existing test covers network rules; new test covers application rules.

**Decision Needed From:** Developer

---

### Question 2: Factory Unit Test Coverage

Should factory unit tests:
1. Test only the summary generation logic (mirroring network rule collection tests)?
2. Also test rule extraction and diff computation logic?
3. Test both factory methods and internal helper methods?

**Recommendation:** Test only public API (summary generation) initially, mirroring the existing pattern. Internal methods are tested implicitly via snapshot tests. Add factory-specific tests only if bugs are found in production.

**Decision Needed From:** Developer or Maintainer

---

### Question 3: Test Execution Timeout

Should firewall application rule tests:
1. Use default timeout (no special handling)?
2. Use custom timeout via `scripts/test-with-timeout.sh --timeout-seconds <N>`?

**Recommendation:** Use default timeout. Only override if tests hang during development (unlikely for unit/snapshot tests).

**Decision Needed From:** Developer

---

## Test Execution

### Run All Tests

From repository root:

```bash
# Run all tests (recommended during development)
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx

# Run with detailed output (for debugging)
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx --output Detailed
```

### Run Specific Test Categories

Using TUnit's `--treenode-filter` syntax:

```bash
# Run only firewall application rule tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ \
  --treenode-filter /*/*/FirewallApplicationRuleCollectionSummaryTests/*

# Run only snapshot tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ \
  --treenode-filter /*/*/MarkdownSnapshotTests/*

# Run markdown lint integration tests
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ \
  --treenode-filter /*/*/MarkdownLintIntegrationTests/*
```

### Run Single Test

```bash
# Run specific test method
dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ \
  --treenode-filter /*/*/*/BuildChangedAttributesSummary_WhenMoreThanThreeChanges_Truncates
```

### Update Snapshots

When intentional changes are made to template output:

1. Run tests to see failures
2. Review diffs carefully to ensure changes are intentional
3. Update snapshot files manually or use snapshot update tooling (if available)
4. Re-run tests to verify snapshots match

**Important:** Never update snapshots without understanding why they changed. Unexpected snapshot changes may indicate bugs.

---

## Definition of Done

This feature's testing is complete when:

- [ ] All 24 test cases are implemented and passing
- [ ] Test data file created with all scenarios
- [ ] Snapshot file created and matches expected output
- [ ] All markdown passes markdownlint validation
- [ ] All invariant tests pass (no consecutive blank lines, proper table formatting)
- [ ] Factory unit tests mirror network rule collection tests
- [ ] Integration tests verify factory registration
- [ ] No new test infrastructure dependencies added
- [ ] Test execution completes in < 30 seconds (full test suite)
- [ ] Documentation updated with test locations and purposes
- [ ] UAT test plan created (separate document)
- [ ] All existing tests continue to pass (no regressions)

---

## Maintenance Notes

### When to Update Tests

- **Template Changes:** Update snapshot files when template output intentionally changes
- **New Scenarios:** Add test cases for newly discovered edge cases or bug fixes
- **Architecture Changes:** Update factory tests if internal implementation changes
- **Markdown Spec Changes:** Update lint tests if markdownlint rules are added/changed

### Test Data Maintenance

The test data file (`firewall-application-rule-changes.json`) should be regenerated from real Terraform output if:
- Azure provider schema changes (new properties added)
- Terraform plan JSON format changes
- New edge cases discovered in production

**Regeneration Process:**
1. Create Terraform configuration with `azurerm_firewall_application_rule_collection`
2. Run `terraform plan -out=plan.tfplan && terraform show -json plan.tfplan > plan.json`
3. Extract relevant `resource_changes` entries
4. Place in test data file
5. Update snapshot file with new expected output

---

## References

- **Testing Strategy:** `docs/testing-strategy.md`
- **Architecture:** `docs/features/060-azurerm-firewall-application-rule-template/architecture.md`
- **Specification:** `docs/features/060-azurerm-firewall-application-rule-template/specification.md`
- **Reference Tests:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/FirewallNetworkRuleCollectionSummaryTests.cs`
- **Snapshot Test Pattern:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownSnapshotTests.cs`
- **TUnit Documentation:** https://thomhurst.github.io/TUnit/
