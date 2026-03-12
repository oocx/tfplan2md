# Test Plan: Missing Separate NSG Rule In Generated Report

## Overview

This test plan covers the fix for Issue 112, where `azurerm_network_security_rule` changes merged into a parent `azurerm_network_security_group` were counted in summaries but not rendered in the NSG `Security Rules` table.

There is no `specification.md` artifact in this work item folder. The requirements below are derived from `analysis.md` and `architecture.md`.

Primary regression risk: the specialized AzureRM NSG renderer bypasses the canonical merged report model in `ChildResourceGroups` and can therefore hide separate child-rule changes after the merge stage has already removed the child resource from the top-level display list.

## Requirements Derived From Analysis And Architecture

| Requirement ID | Requirement |
|----------------|-------------|
| RQ-01 | A no-op NSG parent with a separate created `azurerm_network_security_rule` must render the created rule row inside the parent `Security Rules` table. |
| RQ-02 | A no-op NSG parent with a separate updated `azurerm_network_security_rule` must render the updated rule row inside the parent `Security Rules` table with the correct action semantics. |
| RQ-03 | A no-op NSG parent with a separate deleted `azurerm_network_security_rule` must render the deleted rule row inside the parent `Security Rules` table. |
| RQ-04 | Mixed inline plus separate NSG rule scenarios must render all rule rows without losing either source. |
| RQ-05 | When merged `ChildResourceGroups` exist for NSG security rules, the specialized NSG renderer must treat them as the authoritative render input. |
| RQ-06 | Parent-child merging and display filtering must continue to remove separate NSG rule resources from the top-level display list while keeping the parent NSG visible. |
| RQ-07 | Parent summaries, resource-type summaries, and rendered table rows must stay consistent for separate NSG rule changes. |
| RQ-08 | Existing inline-only NSG rendering behavior, table formatting, sorting, and fallback behavior must not regress. |

## Test Coverage Matrix

| Requirement | Test Case(s) | Test Type |
|-------------|--------------|-----------|
| RQ-01 | TC-01, TC-05, TC-08 | Integration, Unit, UAT |
| RQ-02 | TC-02, TC-05, TC-08 | Integration, Unit, UAT |
| RQ-03 | TC-03, TC-05, TC-08 | Integration, Unit, UAT |
| RQ-04 | TC-04, TC-05, TC-09 | Integration, Unit, UAT |
| RQ-05 | TC-05, TC-06 | Unit |
| RQ-06 | TC-05, TC-07 | Unit |
| RQ-07 | TC-05, TC-07, TC-08 | Unit, UAT |
| RQ-08 | TC-06, TC-10 | Unit, Regression |

## Automated Test Strategy

The repository currently uses TUnit, not xUnit. All automated tests in this plan should be fully automated and runnable via:

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

Preferred implementation areas:

- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/MarkdownRendererNsgTemplateTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderNoOpParentWithChildrenTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderParentChildTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ParentChildUatSnapshotTests.cs`


Recommended TUnit naming style for new tests: `Method_Scenario_ExpectedResult`.

## User Acceptance Scenarios

### Scenario 1: Real Missing Rule Reproduction

**User Goal:** Review a plan where a no-op NSG gains a separate created rule and confirm the rule is visible in the final markdown instead of only being counted.

**Test PR Context:**

- **GitHub:** Verify the created rule appears in the NSG `Security Rules` table and not as a separate top-level resource section.
- **Azure DevOps:** Verify the same table row is visible and the markdown table remains readable.

**Expected Output:**

- The NSG `my-nsg` contains a `Security Rules` table row for `MyRuleName`.
- The row shows the expected action icon and key values: `Inbound`, `Allow`, `Tcp`, destination ports `5050-5051`, source prefix `127.1.0.0/22`.
- The parent summary still indicates `➕ 1 security rules`.
- No orphan top-level `azurerm_network_security_rule` section is rendered for the same rule.

**Success Criteria:**

- [ ] The created rule row is visible under the correct NSG.
- [ ] Summary counts and rendered rows agree.
- [ ] No duplicate or orphan rendering appears elsewhere in the report.

### Scenario 2: Full Separate-Rule Action Coverage

**User Goal:** Confirm that separate child `create`, `update`, and `delete` actions all stay visible after parent-child merging.

**Test PR Context:**

- **GitHub:** Verify three NSG sections or one focused artifact section showing all three action types.
- **Azure DevOps:** Verify action icons and table values still render correctly.

**Expected Output:**

- A created separate rule renders with an add icon.
- An updated separate rule renders with update semantics and changed values.
- A deleted separate rule renders with a delete icon.
- Each row is shown under its parent NSG `Security Rules` table rather than at the top level.

**Success Criteria:**

- [ ] All three separate-child action types are visible.
- [ ] Each row is attached to the correct NSG.
- [ ] No action type is counted without being shown.

### Scenario 3: Mixed Inline And Separate Rules

**User Goal:** Review an NSG where inline rule changes and separate child-rule changes coexist, and confirm that neither source is dropped.

**Test PR Context:**

- **GitHub:** Verify the `Security Rules` table contains both inline-derived and merged separate-child rows.
- **Azure DevOps:** Verify the mixed table remains readable and complete.

**Expected Output:**

- The NSG `Security Rules` table contains all expected rows from both inline and separate sources.
- Existing inline rule formatting is preserved.
- The separate-child row is not omitted because the NSG renderer rebuilt the table from parent state only.

**Success Criteria:**

- [ ] All expected rows are present.
- [ ] Existing inline-only formatting remains intact.
- [ ] Mixed scenarios do not duplicate or suppress rows.

## Test Cases

### TC-01: Render_NoOpParentWithSeparateCreatedRule_ShowsCreatedRowInSecurityRulesTable

**Type:** Integration

**Description:**
Verify end-to-end markdown rendering for a no-op NSG parent with a separate created `azurerm_network_security_rule`.

**Preconditions:**

- A plan JSON contains a no-op `azurerm_network_security_group` and a separate created `azurerm_network_security_rule` referencing that NSG.

**Test Steps:**

1. Parse the plan.
2. Build the report model.
3. Render markdown.
4. Assert the parent NSG section contains the created rule row.
5. Assert the separate child resource does not render as a top-level section.

**Expected Result:**

- The created rule is visible in the parent `Security Rules` table.
- The parent remains visible even though it is no-op.
- Summary counts and rendered rows are consistent.

**Suggested Implementation Area:**

- `MarkdownRendererNsgTemplateTests`

### TC-02: Render_NoOpParentWithSeparateUpdatedRule_ShowsUpdatedRowInSecurityRulesTable

**Type:** Integration

**Description:**
Verify end-to-end markdown rendering for a no-op NSG parent with a separate updated child rule.

**Test Steps:**

1. Parse a plan with a no-op NSG parent and a separate updated rule.
2. Build the report model.
3. Render markdown.
4. Assert the updated rule row appears under the parent NSG with the changed value content.

**Expected Result:**

- The updated rule is rendered under `Security Rules`.
- The row reflects update semantics and the changed values are visible.

**Suggested Implementation Area:**

- `MarkdownRendererNsgTemplateTests`

### TC-03: Render_NoOpParentWithSeparateDeletedRule_ShowsDeletedRowInSecurityRulesTable

**Type:** Integration

**Description:**
Verify end-to-end markdown rendering for a no-op NSG parent with a separate deleted child rule.

**Test Steps:**

1. Parse a plan with a no-op NSG parent and a separate deleted rule.
2. Build the report model.
3. Render markdown.
4. Assert the deleted rule row appears under the parent NSG.

**Expected Result:**

- The deleted rule is rendered under `Security Rules` with delete semantics.
- The child is not lost after top-level filtering.

**Suggested Implementation Area:**

- `MarkdownRendererNsgTemplateTests`

### TC-04: Render_MixedInlineAndSeparateRules_ShowsAllRows

**Type:** Integration

**Description:**
Verify that an NSG with inline rule changes and separate child-rule changes renders all expected rows.

**Test Steps:**

1. Parse a plan covering both inline and separate NSG rule changes.
2. Build the report model.
3. Render markdown.
4. Assert all expected rule rows are present.

**Expected Result:**

- Both inline-derived and merged separate-child rows are rendered.
- No expected rule row is lost.

**Suggested Implementation Area:**

- `MarkdownRendererNsgTemplateTests`

### TC-05: Build_NoOpNsgParentWithMergedSecurityRules_PopulatesChildResourceGroupsAndSummary

**Type:** Unit

**Description:**
Verify that report-model construction produces the merged NSG child group that the specialized renderer must consume.

**Test Steps:**

1. Build a report model from a plan with separate NSG rule changes.
2. Locate the parent NSG change.
3. Assert `ChildResourceGroups` contains a `Security Rules` group with the expected number of rows.
4. Assert the parent summary includes the child-rule change count.

**Expected Result:**

- `ChildResourceGroups` is populated correctly.
- The merged rows encode the expected actions and values.
- The parent summary is aligned with the merged group.

**Suggested Implementation Area:**

- `ReportModelBuilderNoOpParentWithChildrenTests`

### TC-06: Render_NsgWithNoMergedChildGroup_UsesExistingInlineFallbackBehavior

**Type:** Unit

**Description:**
Verify that existing inline-only NSG rendering continues to work when no merged `ChildResourceGroups` are present.

**Test Steps:**

1. Render an inline-only NSG scenario already covered by existing test data.
2. Assert table shape, sorting, and semantic formatting remain unchanged.

**Expected Result:**

- Existing NSG rendering tests continue to pass without behavior changes.
- The fix does not break the current fallback path.

**Suggested Implementation Area:**

- `MarkdownRendererNsgTemplateTests`
- `MarkdownRendererAzureRmTemplateRegressionTests`

### TC-07: Build_NoOpNsgParentWithSeparateRules_RemovesTopLevelChildResourcesButKeepsParentVisible

**Type:** Unit

**Description:**
Verify that display filtering still removes separate child resources from the top-level list while preserving the no-op parent NSG because it has merged children.

**Test Steps:**

1. Build the report model from a no-op NSG parent plus separate child rules.
2. Assert the parent NSG remains in `model.Changes`.
3. Assert the separate `azurerm_network_security_rule` resources are not present in the top-level list.

**Expected Result:**

- Only the parent NSG is shown at top level.
- The merged child rows are retained under the parent.

**Suggested Implementation Area:**

- `ReportModelBuilderNoOpParentWithChildrenTests`

### TC-08: Uat_Issue112FocusedArtifact_ShowsCreateUpdateDeleteAndNoOrphanRows

**Type:** UAT

**Description:**
Verify the feature-specific UAT artifact in GitHub and Azure DevOps using a repository-contained plan built specifically for this issue.

**Expected Result:**

- The focused artifact visibly covers separate create, update, delete, and mixed scenarios.
- Reviewers can confirm row visibility without inspecting raw JSON.

### TC-09: Uat_Issue112MixedScenario_ShowsInlineAndSeparateRowsTogether

**Type:** UAT

**Description:**
Verify the mixed inline plus separate scenario in the feature-specific artifact.

**Expected Result:**

- The rendered table contains all expected mixed-source rows.

### TC-10: Regression_ExistingNsgTemplateScenarios_StillPass

**Type:** Regression

**Description:**
Run the existing NSG renderer regression coverage to ensure the fix does not change unrelated NSG formatting behavior.

**Expected Result:**

- Existing create, update, delete, sorting, and field-formatting expectations still pass.

## Test Data Requirements

| Artifact | Purpose |
|----------|---------|
| `src/tests/Oocx.TfPlan2Md.TUnit/TestData/nsg-with-no-op-rules.json` | Baseline no-op parent plus separate child coverage for merge and visibility behavior. |
| `src/tests/Oocx.TfPlan2Md.TUnit/TestData/nsg-with-separate-rule-updates.json` | Separate child update coverage for parent-child merging and rendering. |
| `src/tests/Oocx.TfPlan2Md.TUnit/TestData/nsg-rule-changes.json` | Existing inline-only NSG regression coverage for fallback behavior. |
| `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azure-rm-batch-2-feature-test-plan.json` | Broad regression and mixed inline plus separate NSG rule coverage. |
| `docs/issues/112-missing-nsg-rule-report/uat-plan.json` | New focused UAT plan the Developer should create for this issue. |
| `docs/issues/112-missing-nsg-rule-report/uat-plan.md` | Rendered markdown generated from the focused UAT plan. |

## Edge Cases And Failure Modes

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| No-op parent plus only separate create | Parent stays visible and the created row renders under `Security Rules` | TC-01, TC-05, TC-07 |
| No-op parent plus only separate update | Updated row renders and changed values are visible | TC-02, TC-05 |
| No-op parent plus only separate delete | Deleted row renders and is not lost after merge/filtering | TC-03, TC-05 |
| Mixed inline plus separate rules | All rows render exactly once | TC-04, TC-09 |
| No merged child group present | Existing inline-only fallback path still works | TC-06, TC-10 |
| Summary counts diverge from rendered rows | Test fails; summaries and rows must agree | TC-01, TC-05, TC-08 |

## Non-Functional And Regression Checks

- Regression scope should include existing NSG table ordering, semantic formatting, and fallback table layout.
- No manual inspection is acceptable for automated tests; only UAT visual verification may be manual.
- The automated suite must stay runnable through the standard wrapper command without special setup.

## Gaps, Assumptions, And Questions

- **Document gap:** The work item does not include `specification.md`; this plan assumes `analysis.md` and `architecture.md` are the authoritative requirement sources.
- **Testability:** The requirements are testable with current repository test infrastructure and existing NSG/parent-child fixtures.
- **No blocking question at this stage:** The expected behavior is sufficiently defined by the issue analysis and architecture guidance.

## Definition Of Done For Testing

- Every derived requirement is mapped to at least one automated or UAT test case.
- Separate child `create`, `update`, `delete`, and mixed inline plus separate scenarios are covered.
- The plan preserves regression coverage for existing inline-only NSG rendering behavior.
- The Developer has a clear specification for the focused `uat-plan.json` and `uat-plan.md` artifacts.
