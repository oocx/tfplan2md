# Tasks: Missing Separate NSG Rule In Generated Report

## Overview

This bug-fix plan translates the requirements from [analysis.md](/home/re/git/private/tfplan2md/docs/issues/112-missing-nsg-rule-report/analysis.md), [architecture.md](/home/re/git/private/tfplan2md/docs/issues/112-missing-nsg-rule-report/architecture.md), [test-plan.md](/home/re/git/private/tfplan2md/docs/issues/112-missing-nsg-rule-report/test-plan.md), and [uat-test-plan.md](/home/re/git/private/tfplan2md/docs/issues/112-missing-nsg-rule-report/uat-test-plan.md) into implementation-ready user stories.

There is no `specification.md` artifact for this work item. For this issue, `analysis.md` is the requirements source and `architecture.md` defines the intended implementation boundary: keep parent-child merging as the canonical source of truth and make the specialized NSG renderer consume the merged `ChildResourceGroups` output.

## User Stories

### Story 1: Lock In The Regression With Automated Coverage

**Priority:** High

**User Story:**
As a maintainer, I want failing automated coverage for invisible separate NSG child-rule changes so the fix is driven by the real regression and protected from recurrence.

**Description:**
Add or extend test fixtures and automated tests for no-op NSG parents with separate child `create`, `update`, `delete`, and mixed inline plus separate scenarios before changing renderer behavior.

**Acceptance Criteria:**
- [ ] Automated tests cover separate child `create`, `update`, `delete`, and mixed inline plus separate rule scenarios.
- [ ] Coverage includes both report-model assertions and end-to-end markdown rendering assertions.
- [ ] Tests assert that separate `azurerm_network_security_rule` resources are not rendered as orphan top-level sections once merged.
- [ ] Tests assert that parent summaries and rendered table rows stay aligned.
- [ ] Existing inline-only NSG regression coverage remains present and runnable through the standard TUnit wrapper command.

**Dependencies:** None

**Scenario Coverage:**
- User Acceptance Scenario 1
- User Acceptance Scenario 2
- User Acceptance Scenario 3
- TC-01, TC-02, TC-03, TC-04, TC-05, TC-06, TC-07, TC-10

**Notes:**
Preferred implementation areas are the issue test plan targets in the TUnit suites. This story should create the safety net first, not after the renderer change.

---

### Story 2: Make Merged Security Rules The Authoritative NSG Render Input

**Priority:** High

**User Story:**
As a report reviewer, I want the NSG `Security Rules` table to render merged child-rule rows from the canonical report model so counted changes are always visible in the report body.

**Description:**
Update the specialized AzureRM NSG renderer to consume the merged `Security Rules` child group from `ChildResourceGroups` when it exists, while preserving the existing inline-only fallback path when no merged group is present.

**Acceptance Criteria:**
- [ ] The specialized NSG renderer uses merged `ChildResourceGroups` as the primary data source when a `Security Rules` child group exists.
- [ ] Separate child `create`, `update`, and `delete` rows render under the correct parent NSG instead of disappearing.
- [ ] Mixed inline plus separate rule scenarios render all expected rows exactly once.
- [ ] The existing inline-only NSG rendering path remains available when no merged child group exists.
- [ ] Provider-specific NSG formatting remains in the AzureRM renderer path rather than moving merge logic into unrelated layers.

**Dependencies:** Story 1

**Scenario Coverage:**
- User Acceptance Scenario 1
- User Acceptance Scenario 2
- User Acceptance Scenario 3
- TC-01, TC-02, TC-03, TC-04, TC-05, TC-06

**Notes:**
This is the core fix. It should follow the architecture decision to restore a single source of truth instead of duplicating merge behavior inside NSG-specific model building.

---

### Story 3: Preserve Merge, Filtering, And Summary Consistency For No-Op NSG Parents

**Priority:** High

**User Story:**
As a reviewer, I want no-op NSG parents, merged child rows, and summaries to stay consistent so the report does not show hidden, duplicated, or contradictory NSG rule changes.

**Description:**
Verify and adjust the rendering pipeline as needed so no-op parents remain visible when they have merged children, separate child resources stay removed from the top-level list, and summary counts match the rendered table rows after the renderer change.

**Acceptance Criteria:**
- [ ] No-op NSG parents with merged child rules remain visible in the final report.
- [ ] Separate child `azurerm_network_security_rule` resources remain removed from the top-level display list after merging.
- [ ] Parent-level summaries, resource-type summaries, and rendered table rows agree for separate child-rule changes.
- [ ] The fix does not introduce duplicate rule rows or duplicate logical rendering of the same change.
- [ ] Existing sort order and semantic row formatting remain stable for unchanged scenarios.

**Dependencies:** Story 2

**Scenario Coverage:**
- User Acceptance Scenario 1
- User Acceptance Scenario 2
- User Acceptance Scenario 3
- TC-01, TC-03, TC-04, TC-05, TC-07, TC-10

**Notes:**
The architecture review does not expect a redesign here. The goal is to keep the existing parent-child merge contract intact after the NSG renderer starts consuming merged groups.

---

### Story 4: Prepare Focused Reviewer Artifacts For UAT

**Priority:** Medium

**User Story:**
As a UAT reviewer, I want a compact issue-specific markdown artifact that clearly shows create, update, delete, and mixed NSG rule scenarios so I can validate the fix quickly in GitHub and Azure DevOps.

**Description:**
Create the focused issue-specific UAT plan artifact described in the UAT test plan and generate the corresponding markdown output so the UAT Tester can use it during review.

**Acceptance Criteria:**
- [ ] `docs/issues/112-missing-nsg-rule-report/uat-plan.json` is added with focused NSG scenarios for separate `create`, `update`, `delete`, and mixed inline plus separate rules.
- [ ] `docs/issues/112-missing-nsg-rule-report/uat-plan.md` is generated from that focused plan through the normal tfplan2md pipeline.
- [ ] The focused artifact makes the create scenario for `MyRuleName` visible with the expected key values.
- [ ] The focused artifact does not render orphan top-level `azurerm_network_security_rule` sections for merged child rows.
- [ ] The artifact remains small enough for reviewers to validate row visibility quickly in PR comments.

**Dependencies:** Story 2

**Scenario Coverage:**
- User Acceptance Scenario 1
- User Acceptance Scenario 2
- User Acceptance Scenario 3
- TC-08, TC-09

**Notes:**
This story prepares artifacts for the UAT Tester. It does not ask the Developer to execute UAT.

## Scenario Coverage Matrix

| Test Plan Scenario | Covered By |
|--------------------|------------|
| User Acceptance Scenario 1: Real Missing Rule Reproduction | Story 1, Story 2, Story 3, Story 4 |
| User Acceptance Scenario 2: Full Separate-Rule Action Coverage | Story 1, Story 2, Story 3, Story 4 |
| User Acceptance Scenario 3: Mixed Inline And Separate Rules | Story 1, Story 2, Story 3, Story 4 |
| TC-01 | Story 1, Story 2, Story 3 |
| TC-02 | Story 1, Story 2 |
| TC-03 | Story 1, Story 2, Story 3 |
| TC-04 | Story 1, Story 2, Story 3 |
| TC-05 | Story 1, Story 2, Story 3 |
| TC-06 | Story 1, Story 2 |
| TC-07 | Story 1, Story 3 |
| TC-08 | Story 4 |
| TC-09 | Story 4 |
| TC-10 | Story 1, Story 3 |

## Recommended Delivery Order

1. Story 1: Lock In The Regression With Automated Coverage
Reason: this bug should be reproduced and pinned down in tests before the renderer contract is changed.

2. Story 2: Make Merged Security Rules The Authoritative NSG Render Input
Reason: this is the minimal implementation change that addresses the root cause identified by analysis and architecture.

3. Story 3: Preserve Merge, Filtering, And Summary Consistency For No-Op NSG Parents
Reason: after the renderer fix, the surrounding contract still needs explicit validation so row visibility, filtering, and summaries remain aligned.

4. Story 4: Prepare Focused Reviewer Artifacts For UAT
Reason: generate the issue-specific artifact after the implementation stabilizes so reviewers see final behavior rather than an intermediate output.

## Open Questions

- None at this stage. The issue analysis, architecture review, test plan, and UAT plan are specific enough to proceed with implementation once this plan is approved.
