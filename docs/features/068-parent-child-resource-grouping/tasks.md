# Tasks: Parent-Child Resource Grouping and Inline Rendering

## Overview

This feature introduces a generic framework for grouping child Terraform resources (like group members) into a table within their parent resource's markdown section. This improves readability and reduces scrolling. Initial implementation targets include `azuread_group`, `azuredevops_group`, and `azuredevops_team`.

Reference: [specification.md](specification.md), [architecture.md](architecture.md).

## Tasks

### Task 1: Foundation - Core Abstractions and Registry

**Priority:** High

**Description:**
Implement the basic data structures and registry required for the parent-child framework as defined in the architecture.

**Acceptance Criteria:**
- [x] `ParentChildRelationship`, `ChildTableColumn`, and `IChildRowExtractor` defined in `MarkdownGeneration/Models/`.
- [x] `ChildResourceGroup` and `ChildResourceRow` added to `ResourceChangeModel`.
- [x] `IParentChildRelationshipRegistry` and its default implementation created.
- [x] Registry supports multiple relationships per parent type (required for `azuredevops_team` administrators + members).
- [x] Unit tests for `ParentChildRelationshipRegistry` (TC-01).

**Dependencies:** None

---

### Task 2: Provider Integration Hook

**Priority:** High

**Description:**
Add the necessary hooks to `IProviderModule` and `ProviderRegistry` to allow providers to register their relationships.

**Acceptance Criteria:**
- [x] `IProviderModule` has `RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)` with default no-op.
- [x] `ProviderRegistry` has `RegisterAllParentChildRelationships` method updated/added to invoke provider registrations.
- [x] Existing providers updated to support the new method (via default implementation or explicit override).

**Dependencies:** Task 1

---

### Task 3: Merging Logic in ReportModelBuilder

**Priority:** High

**Description:**
Implement the core merging logic in `ReportModelBuilder.Build()` that identifies children, merges them into parents, and removes them from the main list.

**Acceptance Criteria:**
- [x] Logic to detect inline children from parent attributes.
- [x] Logic to detect separate child resources by matching reference attributes to parent IDs (TC-06).
- [x] Inline children set the “Terraform Resource” label to the inline attribute name (e.g., `members`) (TC-07).
- [x] Separate children set the “Terraform Resource” label to the original Terraform address (TC-06).
- [x] Logic to detect mixed management and set `HasMixedSources` (TC-08).
- [x] Logic to re-attribute static analysis findings from inlined children to the parent resource (TC-11).
- [x] Re-attributed findings preserve the original child Terraform address in the rendered finding text (spec requirement + Scenario 3).
- [x] Logic to update parent summary line with child change counts (TC-10).
- [x] Unit tests for the merging logic covering separate, inline, mixed, and findings scenarios.

**Dependencies:** Task 1, Task 2

---

### Task 4: Scriban Rendering Infrastructure

**Priority:** Medium

**Description:**
Create the shared Scriban template partial for rendering the child resource table and update the main template to include it.

**Acceptance Criteria:**
- [x] `_child_resources.sbn` partial created with standardized table layout (Change, [Columns], Terraform Resource).
- [x] Mixed management warning included in the template.
- [x] Base/default template updated to include `_child_resources.sbn` if `ChildResourceGroups` is not empty.
- [x] Change indicators (emojis) correctly rendered in the first column (TC-05).
- [x] Table cell formatting goes through the existing formatting pipeline (icons/formatters/truncation) rather than bespoke string formatting (TC-09).
- [x] Findings rendered within the parent resource section still show the original child address they apply to (Scenario 3).

**Dependencies:** Task 1

---

### Task 5: Azure AD Group Implementation

**Priority:** Medium

**Description:**
Register the relationship for `azuread_group` and implement the `IChildRowExtractor` for group members.

**Acceptance Criteria:**
- [x] `azuread_group` relationship registered in `AzureAdProviderModule`.
- [x] `AzureAdGroupMemberExtractor` implemented to format member object IDs.
- [x] Test data JSON added for `azuread_group` scenarios (from Test Plan “Test Data Requirements”).
- [x] Snapshot test for `azuread_group` with separate and inline members (TC-02).

**Dependencies:** Task 3, Task 4

---

### Task 6: Azure DevOps Group and Team Implementation

**Priority:** Medium

**Description:**
Register relationships for `azuredevops_group` and `azuredevops_team` and implement the row extractors.

**Acceptance Criteria:**
- [x] `azuredevops_group` relationship registered in `AzureDevOpsProviderModule`.
- [x] `azuredevops_team` relationships (administrators and members) registered.
- [x] `AzureDevOpsMemberExtractor` implemented to format member descriptors.
- [x] Test data JSON added for `azuredevops_group` and `azuredevops_team` scenarios (from Test Plan “Test Data Requirements”).
- [x] Snapshot tests for group and team rendering (TC-03, TC-04).

---

### Task 7: Edge Cases and Performance Guardrails

**Priority:** Medium

**Description:**
Add explicit handling and test coverage for edge cases listed in the test plan and ensure the merging step does not introduce a noticeable slowdown.

**Acceptance Criteria:**
- [x] Child references non-existent parent → child remains a separate section / is not merged (TC-E1).
- [x] Circular parent-child / unexpected relationship graph → no crash, no infinite loop (TC-E2).
- [x] Parent has no children → no child tables rendered, summary line unchanged (TC-E3).
- [x] Inline attribute is empty → no child table rendered for that group (TC-E4).
- [x] Child has null / missing attributes → extractor handles nulls gracefully (TC-E5).
- [x] Merging implementation uses indexed lookups (no repeated full scans per parent) and includes a lightweight performance check consistent with the repo’s testing approach (NFR: no measurable degradation).

**Dependencies:** Task 3

**Dependencies:** Task 3, Task 4

## Implementation Order

Recommended sequence for implementation:
1. **Task 1 & 2** - Foundation for the registry and provider hooks.
2. **Task 3** - core logic for merging. This is the most complex part and needs thorough unit testing.
3. **Task 7** - Lock in edge case behavior and performance characteristics early.
4. **Task 4** - Rendering infrastructure to visualize the results.
5. **Task 5 & 6** - Provider-specific implementations and integration (snapshot) tests.

## Open Questions

- None at this stage. Logic for finding re-attribution needs to ensure the child resource address is preserved as requested in the specification.
