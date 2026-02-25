# Tasks: Known-After-Apply Rendering

## Overview

This feature ensures that tfplan2md correctly surfaces computed Terraform attributes (`after_unknown: true`) in rendered reports instead of silently dropping them. This covers both attribute table rows and AzureAD group member summary lines.

See [specification.md](specification.md), [architecture.md](architecture.md), [test-plan.md](test-plan.md), and [uat-test-plan.md](uat-test-plan.md).

## Tasks

### Task 1: Unknown-After-Apply Detection Helper

**Priority:** High

**Description:**
Implement a static helper that navigates the `after_unknown` tree in the plan JSON to detect if a specific attribute or the whole resource is unknown after apply.

**Acceptance Criteria:**
- [x] Create `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/AfterUnknownHelper.cs`.
- [x] Implement `IsWholeResourceUnknownAfterApply(object? afterUnknown)`.
- [x] Implement `IsAttributeUnknownAfterApply(object? afterUnknown, string flattenedKey)`.
- [x] Path navigation supports both objects and arrays.
- [x] Create `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AfterUnknownHelperTests.cs`.
- [x] All unit tests pass (TC-01, TC-02, TC-03, TC-04).

**Dependencies:** None

---

### Task 2: Configuration Reference Selection Helper

**Priority:** High

**Description:**
Implement a static helper that selects the best reference label from a list of Terraform references, following the priority order: Static > EachValueAttr > Var/Local > fallback.

**Acceptance Criteria:**
- [x] Create `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ReferenceSelector.cs`.
- [x] Implement `SelectBestReference(IReadOnlyList<string> references)`.
- [x] Implement `SelectResourceLevelReference(IReadOnlyList<string> references)`.
- [x] Correctly strips trailing `.attribute` for resource-level labels.
- [x] Create `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReferenceSelectorTests.cs`.
- [x] All unit tests pass (TC-05 to TC-11).

**Dependencies:** None

---

### Task 3: Enhance ResourceChangeModel and ReportModelBuilder

**Priority:** High

**Description:**
Update the model-building layer to detect unknown attributes and populate them with a display label.

**Acceptance Criteria:**
- [x] Add `HasWholeResourceUnknownAfterApply` (bool) and `ConfigurationReferences` (internal map) to `ResourceChangeModel`.
- [x] Update `ReportModelBuilder.ResourceChanges.cs:BuildAttributeChanges` to detect unknown attributes (using `AfterUnknownHelper`).
- [x] Set `After` to `(known after apply)` or `(known after apply: reference)` (using `ReferenceSelector`) for unknown attributes.
- [x] Special handling for sensitive+computed attributes: `🔒(known after apply)`.
- [x] Ensure unknown attributes on updates are included in the change count (`ChangedAttributesSummary`).

**Dependencies:** Task 1, Task 2

---

### Task 4: Fix AzureAD Group Member Summary

**Priority:** High

**Description:**
Update the AzureAD provider logic to handle computed IDs in the summary line using configuration references.

**Acceptance Criteria:**
- [x] Update `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.Groups.cs`.
- [x] If `group_object_id` or `member_object_id` is null/empty, check `ConfigurationReferences` for a resource-level label.
- [x] Log instance keys for `for_each` resources when a static reference label is also found.
- [x] Fall back to string instance keys if no static reference exists.

**Dependencies:** Task 3

---

### Task 5: Template Update: Whole-Resource Unknown

**Priority:** Medium

**Description:**
Modify the resource template to show a specific note for whole-resource unknown instead of the default placeholder.

**Acceptance Criteria:**
- [x] Update `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn`.
- [x] If `change.has_whole_resource_unknown_after_apply`, suppress `_No attribute changes._`.
- [x] Show `_(all values known after apply)_` note for whole-resource unknown.

**Dependencies:** Task 3

---

### Task 6: Integration Tests for Scenario Coverage

**Priority:** High

**Description:**
Implement the full suite of integration tests defined in the test plan to ensure all scenarios are covered.

**Acceptance Criteria:**
- [x] Create `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderComputedAttributeTests.cs`.
- [x] Implement TC-17 to TC-26.
- [x] Create `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdGroupMemberComputedTests.cs`.
- [x] Implement TC-12 to TC-16.
- [x] Verify TC-27: Existing snapshots pass (with intentional updates if needed).

**Dependencies:** Task 4, Task 5

## Implementation Order

1. **Task 1 & Task 2** (Foundational helpers) - These are pure logic and easy to test in isolation.
2. **Task 3** (ReportModelBuilder) - Core logic that enables the feature for all resources.
3. **Task 4** (AzureAD provider) - Resolves the blank summary line for AzureAD group members.
4. **Task 5** (Template) - Visual final touches for the edge cases.
5. **Task 6** (Integration Tests) - Verification of all scenarios.

## Open Questions

None at this stage. All previously noted Open Questions in the test plan were resolved by the Maintainer to use a specific note for whole-resource unknown.
