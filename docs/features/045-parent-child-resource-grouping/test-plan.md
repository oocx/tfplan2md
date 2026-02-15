# Test Plan: Parent-Child Resource Grouping and Inline Rendering

## Overview

This test plan covers the implementation of parent-child resource grouping, where specified child resources are rendered inline as tables within their parent sections. This feature involves generic merging-logic in the report model builder, configuration reference matching for `(known after apply)` scenarios, and standardized rendering via Scriban templates.

Reference: [specification.md](specification.md), [architecture.md](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| **Registry Complete**: Cataloged patterns with status | TC-01 | Unit (Registry) |
| **Inline Rendering**: Initial targets (azuread, azuredevops) render as tables | TC-02, TC-03, TC-04, TC-15 | Integration (Snapshot) |
| **Change Indicators**: Tables show change emojis (➕, 🔄, etc.) | TC-05 | Unit (Extractor) |
| **Resource Address**: Separate children show their original address | TC-06 | Unit (Merging) |
| **Inline Source**: Inline children show attribute name (e.g., `members`) | TC-07 | Unit (Merging) |
| **Mixed Handling**: Handle both inline and separate children for one parent | TC-08 | Unit (Merging) |
| **Formatting**: Use existing value formatters for table cells | TC-09 | Unit (Extractor) |
| **Summary Line**: Parent summary includes child change counts | TC-10 | Unit (Model Builder) |
| **Merged-Child Findings**: Findings on inlined children appear in parent section | TC-11 | Unit (Model Builder) |
| **Configuration Parsing**: TerraformPlan parses Configuration block | TC-12 | Unit (Parsing) |
| **Reference Resolution**: ConfigurationReferenceResolver builds reference index | TC-13, TC-14, TC-16, TC-17 | Unit (Resolver) |
| **Known After Apply Matching**: Children merge when parent ID is unknown | TC-15, TC-18 | Integration (Merging) |
| **Module Nesting**: Reference matching handles nested modules | TC-16 | Unit (Resolver) |
| **For Each/Count**: Reference matching handles resource instances | TC-17 | Unit (Resolver) |
| **Graceful Degradation**: Missing configuration doesn't break merging | TC-19 | Integration (Merging) |

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

---

### TC-12: TerraformPlan Configuration Property

**Type:** Unit (Parsing)

**Description:**
Verify that `TerraformPlan` correctly parses the `configuration` block from a plan JSON.

**Test Steps:**
1. Create a plan JSON with a `configuration` block containing `root_module` with resources and expressions.
2. Deserialize using `JsonSerializer.Deserialize<TerraformPlan>`.

**Expected Result:**
- `TerraformPlan.Configuration` is not null.
- Configuration JSON element contains expected structure (`root_module`, `resources`, `expressions`).

---

### TC-13: ConfigurationReferenceResolver Root Module

**Type:** Unit (Reference Resolution)

**Description:**
Verify that `ConfigurationReferenceResolver.BuildReferenceIndex()` correctly builds a reference index for resources in the root module.

**Test Data:**
Configuration with:
- `azuread_group.platform_engineers` resource  
- `azuread_group_member.admin_member` resource with `group_object_id.references = ["azuread_group.platform_engineers.id", "azuread_group.platform_engineers"]`

**Expected Result:**
Reference index contains:
- Key: `("azuread_group_member.admin_member", "group_object_id")`
- Value: `["azuread_group.platform_engineers.id", "azuread_group.platform_engineers"]`

---

### TC-14: ConfigurationReferenceResolver No Configuration

**Type:** Unit (Reference Resolution)

**Description:**
Verify that `ConfigurationReferenceResolver.BuildReferenceIndex()` gracefully handles a null/absent `configuration` block.

**Test Steps:**
1. Call `BuildReferenceIndex(null)`.

**Expected Result:**
- Returns an empty dictionary (no exceptions).

---

### TC-15: Separate Children Merge with Known After Apply Parent ID (Integration)

**Type:** Integration (Snapshot)

**Description:**
Verify end-to-end merging of separate children when the parent's ID is `(known after apply)` using configuration reference matching.

**Test Data:**
Plan with:
- `azuread_group.platform_engineers` with `after_unknown.id = true` (creating the group)
- 2 `azuread_group_member` resources referencing `azuread_group.platform_engineers.id` in configuration
- Configuration block with proper expression references

**Expected Result:**
- Snapshot shows ONE section for `azuread_group.platform_engineers`.
- Members table contains 2 rows from the separate `azuread_group_member` resources.
- No separate sections for `azuread_group_member` resources.

**Snapshot File:** `azuread-group-members-known-after-apply.md`

---

### TC-16: ConfigurationReferenceResolver Nested Modules

**Type:** Unit (Reference Resolution)

**Description:**
Verify that `ConfigurationReferenceResolver.BuildReferenceIndex()` correctly handles nested modules, qualifying resource addresses with module prefixes.

**Test Data:**
Configuration with:
- `root_module.module_calls.security.module.resources[]` containing `azuread_group.admins`
- `root_module.module_calls.security.module.resources[]` containing `azuread_group_member.member1` with reference to `azuread_group.admins.id`

**Expected Result:**
Reference index contains:
- Key: `("module.security.azuread_group_member.member1", "group_object_id")`
- Value includes: `"module.security.azuread_group.admins.id"` or `"module.security.azuread_group.admins"`

---

### TC-17: ConfigurationReferenceResolver For Each/Count Instances

**Type:** Unit (Reference Resolution)

**Description:**
Verify that `ConfigurationReferenceResolver` handles `for_each`/`count` resource instances by stripping instance keys when looking up configuration references.

**Test Data:**
- Configuration has `azuread_group_member.members` (no instance key)
- ResourceChanges has `azuread_group_member.members["user-100"]` (with instance key)

**Expected Result:**
- Reference lookup strips `["user-100"]` and finds configuration for `azuread_group_member.members`.
- Reference index correctly maps the instance.

---

### TC-18: BuildSeparateRows Fallback with Known After Apply

**Type:** Unit (Merging Logic)

**Description:**
Verify that `BuildSeparateRows` falls back to configuration reference matching when parent ID is null/empty (known after apply).

**Test Steps:**
1. Create parent resource with `after.id` missing (known after apply).
2. Create child resource referencing parent.
3. Provide configuration with expression references.
4. Call `BuildSeparateRows`.

**Expected Result:**
- `BuildSeparateRows` returns child rows even though parent ID is null.
- Children are correctly matched via configuration references.

---

### TC-19: Graceful Degradation Without Configuration Block

**Type:** Integration (Merging)

**Description:**
Verify that when a plan has no `configuration` block, children with unknown parent IDs are NOT merged and remain as standalone sections.

**Test Data:**
Plan with:
- Parent with `after_unknown.id = true`
- Child referencing parent
- NO `configuration` block

**Expected Result:**
- Child remains in `model.Changes` (not merged).
- Child renders as standalone section (same as pre-Feature 068 behavior).
- No exceptions or errors.

---

### TC-20: Configuration Reference Matching with Multiple Parents

**Type:** Unit (Reference Resolution Precision)

**Description:**
Verify that configuration reference matching correctly distinguishes between multiple parents of the same type in the same module (the scenario that would fail with naive module-address heuristics).

**Test Data:**
Plan with:
- `azuread_group.team_a` with `after_unknown.id = true`
- `azuread_group.team_b` with `after_unknown.id = true`
- `azuread_group_member.member_a` referencing `azuread_group.team_a.id` in configuration
- `azuread_group_member.member_b` referencing `azuread_group.team_b.id` in configuration

**Expected Result:**
- `member_a` merges ONLY with `team_a` (not `team_b`).
- `member_b` merges ONLY with `team_b` (not `team_a`).

## Test Data Requirements

### Existing Test Data

- `azuread-group-members.json`: Plan with `azuread_group` and `azuread_group_member`.
- `azuredevops-team-members.json`: Plan with `azuredevops_team` and members.
- `mixed-management.json`: Plan with mixed inline/separate members for a single group.
- `child-findings.json`: Plan + findings mapped to a child resource.

### New Test Data for Configuration Reference Matching

- **`azuread-group-members-known-after-apply-plan.json`**: Plan with parent `after_unknown.id = true` AND `configuration` block with expression references. Required for TC-15, TC-18.

- **`configuration-with-nested-modules.json`**: Configuration block test data with nested `module_calls`. Required for TC-16.

- **`configuration-with-for-each.json`**: Configuration block test data with `for_each` resources. Required for TC-17.

- **`no-configuration-block-plan.json`**: Plan with `after_unknown.id = true` but NO `configuration` block. Required for TC-19.

- **`multiple-parents-same-type.json`**: Plan with 2+ parents of same type, each with different children, using configuration references. Required for TC-20.

- **Add `configuration` block to existing test data:**
  - Update `comprehensive-demo/plan.json` to include configuration for `azuread_group_member.platform_admin_member` linking to `azuread_group.platform_engineers.id`.
  - Update `azuread-group-members-plan.json` (if separate from comprehensive demo) to include configuration.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Child references non-existent parent | Child remains a separate section (no merge) | TC-E1 (existing) |
| Circular parent-child | Prevent infinite loops or crashes | TC-E2 (existing) |
| Parent has no children | No table rendered, no summary change | TC-E3 (existing) |
| Inline attribute is empty | No table rendered for that attribute | TC-E4 (existing) |
| Child has null attributes | ExtractRow handles nulls gracefully | TC-E5 (existing) |
| Parent ID is `(known after apply)` WITH configuration | Fallback to configuration reference matching | TC-15, TC-18 |
| Parent ID is `(known after apply)` WITHOUT configuration | Child remains standalone (graceful degradation) | TC-19 |
| Multiple parents same type, same module | Configuration precisely matches each child to correct parent | TC-20 |
| Nested modules | Configuration references qualified with module prefix | TC-16 |
| For each/count instances | Instance keys stripped when looking up configuration | TC-17 |
| Dynamic blocks with references | Top-level attributes unaffected (dynamic blocks out of scope) | Documentation only |
| `each.value`/`each.key` references | No match (correct, as parent depends on iteration) | Documentation + TC-19 |
| Extractor exceptions | Gracefully handle and log (do not crash) | TC-E6 (new) |
| Invalid JSON in child state | Gracefully handle parse errors | TC-E7 (new) |

### TC-E6: Extractor Exception Handling

**Type:** Unit (Error Handling)

**Description:**
Verify that if an `IChildRowExtractor.ExtractRow()` throws an exception, the merging logic handles it gracefully without crashing.

**Test Steps:**
1. Create a mock row extractor that throws an exception.
2. Attempt to merge children using this extractor.

**Expected Result:**
- Exception is caught and logged.
- Child resource remains in change list (not merged).
- No application crash.

---

### TC-E7: Invalid JSON Handling

**Type:** Unit (Error Handling)

**Description:**
Verify that invalid or malformed JSON in child resource state doesn't cause failures.

**Test Steps:**
1. Create child resource with malformed `after` JSON.
2. Attempt merging.

**Expected Result:**
- Gracefully handles parse errors.
- Child may remain unmerged or show empty values.
- No crashes.

## Non-Functional Tests

### Performance

**Requirement:** Building a report with 100+ separate child resources should not significantly increase processing time (<500ms overhead).

**Test Case:** TC-E3 (existing in `ReportModelBuilderParentChildEdgeCaseTests.Build_LargeChildSet_CompletesQuickly`)

**Acceptance Criteria:**
- 500 child resources complete in <5 seconds.
- Linear time complexity (no nested loops over children).

### Configuration Reference Resolution Performance

**Requirement:** Building the configuration reference index should scale linearly with the number of resources.

**Test Case:** TC-21 (new)

**Description:**
Verify that `ConfigurationReferenceResolver.BuildReferenceIndex()` completes quickly even with large configuration blocks.

**Test Data:**
- Configuration with 1000+ resources across multiple nested modules.

**Expected Result:**
- Index building completes in <100ms.
- Memory usage is reasonable (no duplicate data structures).

## Open Questions

### Resolved

- ~~Should we support "grandchildren" in this implementation?~~ → Out of scope per spec, catalog lists future patterns.
- ~~How should we handle cases where a child resource references multiple parents?~~ → Architecture states "usually not possible" in targeted TF resources. If found in future, add explicit validation.
- ~~How to handle `(known after apply)` parent IDs?~~ → **RESOLVED:** Use configuration reference matching (architecture Section 3a).

### Current

- **What happens if configuration block format changes in future Terraform versions?** → Monitor Terraform plan format version. Add version-specific handling if needed. Current implementation targets format_version 1.0+.

- **Should we add telemetry/logging for fallback usage?** → Consider adding debug-level logging when configuration fallback is used vs. value-based matching, to help diagnose issues in production.

## Definition of Done

Test plan is complete when:
- [ ] All acceptance criteria have comprehensive test coverage (unit + integration + snapshot).
- [ ] Configuration reference matching has dedicated test cases covering all scenarios.
- [ ] Edge cases include error handling and graceful degradation.
- [ ] Test data requirements are documented with specific file names and contents.
- [ ] Performance requirements have quantitative test cases.
- [ ] The Maintainer has approved the test plan.
