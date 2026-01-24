# Test Plan: Consistent Value Formatting

## Overview

This test plan covers the "Consistent Value Formatting" feature, which aims to improve readability by code-formatting actual data values while keeping labels and attribute names as plain text. It also includes enhanced diff formatting for small values in firewall and NSG rules.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Attribute names in change tables are plain text | TC-01, TC-02 | Unit (Template) |
| Attribute values in change tables are code-formatted | TC-01, TC-02 | Unit (Template) |
| Firewall collection headers have all values code-formatted | TC-03 | Unit (Template) |
| NSG headers have names code-formatted | TC-04 | Unit (Template) |
| Firewall rule table values are code-formatted | TC-03 | Unit (Template) |
| NSG rule table values are code-formatted | TC-04 | Unit (Template) |
| `format_diff` produces styled output (inline-diff) | TC-05 | Unit (Helper) |
| `format_diff` produces styled output (standard-diff) | TC-06 | Unit (Helper) |
| `format_diff` respects `--large-value-format` | TC-07 | Integration |
| Role assignment summaries only code-format data values | TC-08 | Unit (Template) |

## User Acceptance Scenarios

### Scenario 1: Verify Consistent Formatting in Default Report

**User Goal**: View a standard plan report and see that values are highlighted while attribute names are not.

**Steps**:
1. Setup: Use `src/tests/Oocx.TfPlan2Md.Tests/TestData/azurerm-azuredevops-plan.json`
2. Execute: `tfplan2md -i plan.json`
3. Inspect: The generated Markdown report.

**Expected Output**:
- Attribute tables show names like `location` (plain) and values like `` `eastus` `` (code).
- Resource addresses in headings remain as they were.

**Success Criteria**:
- [ ] Output renders correctly in Markdown.
- [ ] Visual emphasis is on the values.

---

### Scenario 2: Verify Enhanced Diff in Firewall Rules

**User Goal**: See detailed diffs for firewall rule changes using the preferred format.

**Steps**:
1. Setup: Use `src/tests/Oocx.TfPlan2Md.Tests/TestData/firewall-rule-changes.json`
2. Execute: `tfplan2md -i plan.json --large-value-format inline-diff`
3. Inspect: The "Rule Changes" table for modified rules.
4. Execute: `tfplan2md -i plan.json --large-value-format standard-diff`
5. Inspect: The "Rule Changes" table again.

**Expected Output**:
- With `inline-diff`: Modified cells show HTML `<span style="background-color: ...">` for changes.
- With `standard-diff`: Modified cells show `` ` ```diff ... ``` `` blocks (or table-compatible equivalent).

**Success Criteria**:
- [ ] Diff format matches the CLI option.
- [ ] Diffs are readable within table cells.

---

## Test Cases

### TC-01: DefaultTemplate_AttributeTable_ReversedFormatting

**Type:** Unit (Template)

**Description:**
Verify that the default template renders attribute tables with plain names and code-formatted values.

**Preconditions:**
- A resource change with small attributes.

**Test Steps:**
1. Render a resource change using `default.sbn`.
2. Check the attribute table rows.

**Expected Result:**
Rows should look like `| location | `eastus` |`.

---

### TC-02: RoleAssignmentTemplate_AttributeTable_ReversedFormatting

**Type:** Unit (Template)

**Description:**
Verify that the role assignment template renders attribute tables with plain names and code-formatted values.

**Preconditions:**
- A role assignment change.

**Test Steps:**
1. Render a role assignment using `role_assignment.sbn`.
2. Check the attribute table rows.

**Expected Result:**
Rows should look like `| role_definition_id | `/subscriptions/...` |`.

---

### TC-03: FirewallTemplate_ConsistentFormatting

**Type:** Unit (Template)

**Description:**
Verify that firewall rule collections use code formatting for all data values in headers and tables.

**Preconditions:**
- A firewall network rule collection change.

**Test Steps:**
1. Render using `firewall_network_rule_collection.sbn`.
2. Check the header line.
3. Check the rule table rows.

**Expected Result:**
- Header: `**Collection:** `public-egress` | **Priority:** `110` | **Action:** `Allow` `
- Table: `| `allow-http` | `TCP` | `10.1.1.0/24` | `*` | `80` | `Allow outbound HTTP` |`

---

### TC-04: NsgTemplate_ConsistentFormatting

**Type:** Unit (Template)

**Description:**
Verify that NSG templates use code formatting for NSG names in headers and code formatting for all data values in rule tables.

**Preconditions:**
- An NSG change.

**Test Steps:**
1. Render using `network_security_group.sbn`.
2. Check the header line.
3. Check the security rule table rows.

**Expected Result:**
- Header: `**Network Security Group:** `nsg-app``
- Table: `| `AllowHTTP` | `100` | `Inbound` | `Allow` | `TCP` | `*` | `80` | `Allow HTTP` |`

---

### TC-05: ScribanHelpers_FormatDiff_InlineDiff

**Type:** Unit (Helper)

**Description:**
Verify that `FormatDiff` produces table-compatible HTML for inline diffs.

**Preconditions:**
- Two different strings.

**Test Steps:**
1. Call `ScribanHelpers.FormatDiff("old", "new", "inline-diff")`.

**Expected Result:**
Output should contain `<span style="background-color: ...">` and be compatible with markdown tables (no newlines that break the table).

---

### TC-06: ScribanHelpers_FormatDiff_StandardDiff

**Type:** Unit (Helper)

**Description:**
Verify that `FormatDiff` produces table-compatible text for standard diffs.

**Preconditions:**
- Two different strings.

**Test Steps:**
1. Call `ScribanHelpers.FormatDiff("old", "new", "standard-diff")`.

**Expected Result:**
Output should contain `- old<br>+ new` (or similar) and be wrapped in backticks if appropriate for the design.

---

### TC-07: MarkdownRenderer_HelperRegistration_RespectsConfig

**Type:** Integration

**Description:**
Verify that `MarkdownRenderer` correctly passes the `LargeValueFormat` from the model to the `format_diff` helper.

**Preconditions:**
- A plan with a change that uses `format_diff`.

**Test Steps:**
1. Create a `ReportModel` with `LargeValueFormat.StandardDiff`.
2. Render using `MarkdownRenderer`.
3. Verify the output uses standard diff markers.
4. Repeat with `LargeValueFormat.InlineDiff`.

**Expected Result:**
The output format changes based on the model configuration.

---

### TC-08: RoleAssignmentTemplate_Summary_RefinedFormatting

**Type:** Unit (Template)

**Description:**
Verify that role assignment summaries only code-format data values, not connecting text.

**Preconditions:**
- A role assignment change.

**Test Steps:**
1. Render a role assignment using `role_assignment.sbn`.
2. Check the summary line.

**Expected Result:**
Summary should look like: `` `my-user` (User) → `Contributor` on Subscription `my-sub` ``.
(Labels like "on Subscription" should be plain text).

## Test Data Requirements

List any new test data files needed:
- No new files needed; existing `azurerm-azuredevops-plan.json` and `firewall-rule-changes.json` are sufficient.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty values | Render as empty or `(known after apply)` without breaking formatting | TC-01 |
| Values with backticks | Correctly escaped by `EscapeMarkdown` before being wrapped in backticks | TC-05 |
| Values with pipes | Correctly escaped to not break markdown tables | TC-05 |
| Multi-line values in `format_diff` | Use `<br>` for line breaks to remain table-compatible | TC-05, TC-06 |

## Non-Functional Tests

- **Markdown Quality**: Ensure that the generated tables remain valid and render correctly in common Markdown viewers (GitHub, Azure DevOps).

## Open Questions

None.
