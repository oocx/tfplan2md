# Test Plan: Parent-Child Resource Grouping and Inline Rendering

## Overview

This test plan covers the implementation of parent-child resource grouping, where specified child resources are rendered inline as tables within their parent sections. This feature involves generic merging-logic in the report model builder and standardized rendering via Scriban templates.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| **Registry Complete**: Cataloged patterns with status | TC-01 | Unit (Registry) |
| **Inline Rendering**: Initial targets (azuread, azuredevops) render as tables | TC-02, TC-03, TC-04 | Integration (Snapshot) |
| **Change Indicators**: Tables show change emojis (➕, 🔄, etc.) | TC-05 | Unit (Extractor) |
| **Resource Address**: Separate children show their original address | TC-06 | Unit (Merging) |
| **Inline Source**: Inline children show attribute name (e.g., `members`) | TC-07 | Unit (Merging) |
| **Mixed Handling**: Handle both inline and separate children for one parent | TC-08 | Unit (Merging) |
| **Formatting**: Use existing value formatters for table cells | TC-09 | Unit (Extractor) |
| **Summary Line**: Parent summary includes child change counts | TC-10 | Unit (Model Builder) |
| **Merged-Child Findings**: Findings on inlined children appear in parent section | TC-11 | Unit (Model Builder) |

## User Acceptance Scenarios

> **Purpose**: Verify visual rendering and cross-platform compatibility (GitHub vs. Azure DevOps) for real-world parent-child patterns.

### Scenario 1: Azure AD Group with Separate and Inline Members

**User Goal**: Review a plan that manages an Azure AD group with some members defined inline (members attribute) and others as separate `azuread_group_member` resources.

**Test PR Context**:
- **GitHub**: Verify table layout, change indicators, and "Terraform Resource" column labels.
- **Azure DevOps**: Verify table layout (ADOs markdown sometimes behaves differently with tables).

**Expected Output**:
- A single section for `azuread_group.my_group`.
- A "Members" table containing both types of members.
- Separate members show their address (e.g., `azuread_group_member.user1`).
- Inline members show `members` in the resource column.
- Warning message about mixed management.

**Success Criteria**:
- [ ] No separate sections for `azuread_group_member` resources.
- [ ] Mixed management warning is visible.
- [ ] Correct change indicators for additions/removals.

---

### Scenario 2: Azure DevOps Team with Administrators and Members

**User Goal**: Review changes to an Azure DevOps team where both administrators and members are being updated.

**Test PR Context**:
- **GitHub/Azure DevOps**: Verify that two separate tables are rendered if both relationships are present.

**Expected Outcome**:
- `azuredevops_team.my_team` section.
- "Administrators" table.
- "Members" table.
- Values correctly formatted (descriptors or readable names).

---

### Scenario 3: Static Analysis Findings on Inlined Resources

**User Goal**: Ensure that security findings mapped to a child resource are not lost when that resource is moved inline.

**Test PR Context**:
- **GitHub/Azure DevOps**: Verify finding display.

**Expected Outcome**:
- Finding appears within the parent resource section.
- Finding clearly mentions the original child resource address it applies to.

## Test Cases

### TC-01: Relationship Registry Validation

**Type:** Unit

**Description:**
Verify that the `ParentChildRelationshipRegistry` correctly registers and retrieves relationships for the initial targets.

**Preconditions:**
- Provider modules registered.

**Test Steps:**
1. Query registry for `azuread_group`.
2. Query registry for `azuredevops_team`.

**Expected Result:**
- `azuread_group` has 1 relationship (members).
- `azuredevops_team` has 2 relationships (administrators, members).

---

### TC-02: Azure AD Group Integration (Snapshot)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end rendering of `azuread_group` with `azuread_group_member` resources using a real plan snippet.

---

### TC-06: Separately Defined Child Merging Logic

**Type:** Unit

**Description:**
Verify that `ReportModelBuilder` correctly identifies separate child resources, removes them from the main change list, and attaches them to the `ChildResourceGroup` of the parent.

**Test Data:**
Plan with 1 `azuread_group` and 2 `azuread_group_member` resources referencing it.

**Expected Result:**
- `model.Changes` contains 1 item (the group).
- Group model has a `ChildResourceGroup` with 2 entries.

---

### TC-08: Mixed Inline and Separate Management Warning

**Type:** Unit

**Description:**
Verify that a warning is added to the `ChildResourceGroup` when children are detected both in the parent's inline attributes and as separate resources.

**Expected Result:**
- `ChildResourceGroup.HasMixedManagement` is true.
- Rendering includes the mixed management warning message.

---

### TC-10: Child Counts in Parent Summary

**Type:** Unit

**Description:**
Verify that the parent resource's summary line (e.g. `➕ azuread_group.test | ➕ 2 members`) correctly aggregates changes from all children in the group.

---

### TC-11: Merging Findings for Inlined Children

**Type:** Unit

**Description:**
Verify that findings mapped to a child resource address that gets inlined are moved to the parent's `Findings` collection.

**Test Steps:**
1. Create a plan with a child resource.
2. Add a static analysis finding mapped to that child resource address.
3. Build report model.

**Expected Result:**
- Child resource is inlined.
- Finding is present in the parent resource section.

## Test Data Requirements

- `azuread-group-members.json`: Plan with `azuread_group` and `azuread_group_member`.
- `azuredevops-team-members.json`: Plan with `azuredevops_team` and members.
- `mixed-management.json`: Plan with mixed inline/separate members for a single group.
- `child-findings.json`: Plan + findings mapped to a child resource.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Child references non-existent parent | Child remains a separate section (no merge) | TC-E1 |
| Circular parent-child | Prevent infinite loops or crashes | TC-E2 |
| Parent has no children | No table rendered, no summary change | TC-E3 |
| Inline attribute is empty | No table rendered for that attribute | TC-E4 |
| Child has null attributes | ExtractRow handles nulls gracefully | TC-E5 |

## Non-Functional Tests

- **Performance**: Building a report with 100+ separate child resources should not significantly increase processing time (<500ms overhead).

## Open Questions

- Should we support "grandchildren" in this implementation? (Currently out of scope per spec).
- How should we handle cases where a child resource references multiple parents? (Usually not possible in targeted TF resources, but should be considered if found).
