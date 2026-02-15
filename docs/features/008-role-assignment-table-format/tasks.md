# Tasks: Role Assignment Table Format

## Overview

This document breaks down the "Role Assignment Table Format" feature into actionable tasks. The goal is to transition `azurerm_role_assignment` resources from a bullet-list format to a structured table format with a summary line, while preserving and enhancing Azure-specific data parsing.

Reference:
- [Specification](specification.md)
- [Architecture](architecture.md)

## Tasks

### Task 1: Refactor Principal Mapping for Raw Data

**Priority:** High

**Description:**
Update the principal mapping infrastructure to provide raw names without Markdown formatting, allowing the Scriban template to control the styling.

**Acceptance Criteria:**
- [ ] `IPrincipalMapper` interface includes a `GetName(string principalId)` method.
- [ ] `PrincipalMapper` implements `GetName` returning the raw name from the mapping file (or null if not found).
- [ ] `NullPrincipalMapper` implements `GetName` returning null.
- [ ] Existing `GetPrincipalName` method is refactored to use `GetName` internally to avoid logic duplication.

**Dependencies:** None

---

### Task 2: Refactor Azure Scope Parsing for Structured Data

**Priority:** High

**Description:**
Refactor `AzureScopeParser` to separate the parsing logic from the string formatting logic.

**Acceptance Criteria:**
- [ ] Create a `ScopeInfo` record/class with properties: `Name`, `Type`, `SubscriptionId`, `ResourceGroup`, `Level`.
- [ ] `AzureScopeParser.Parse(string scope)` method is implemented and returns a `ScopeInfo` object.
- [ ] Existing `ParseScope` method (returning a formatted string) is refactored to use `Parse` internally.
- [ ] Unit tests verify `Parse` correctly identifies Subscription, Resource Group, Resource, and Management Group levels.

**Dependencies:** None

---

### Task 3: Implement Structured Azure Scriban Helpers

**Priority:** High

**Description:**
Register new Scriban helper functions that return structured objects (DTOs) instead of pre-formatted strings.

**Acceptance Criteria:**
- [ ] `azure_principal_info(id)` helper returns an object with `name`, `id`, and `type`.
- [ ] `azure_role_info(id)` helper returns an object with `name` and `id`.
- [ ] `azure_scope_info(id)` helper returns an object with `name`, `type`, `subscription_id`, and `resource_group`.
- [ ] `azure_role_info` correctly falls back to `role_definition_name` if `role_definition_id` is not provided in the plan.

**Dependencies:** Task 1, Task 2

---

### Task 4: Create Comprehensive Test Data

**Priority:** Medium

**Description:**
Create a new Terraform plan JSON file containing all scenarios required to verify the feature.

**Acceptance Criteria:**
- [ ] `src/tests/Oocx.TfPlan2Md.Tests/TestData/role-assignments.json` is created.
- [ ] Includes at least one resource for each action: `create`, `update`, `delete`, and `replace`.
- [ ] Includes a resource with a non-empty `description`.
- [ ] Includes a resource with optional attributes (e.g., `condition`) and one without.
- [ ] Includes resources with very long names to test truncation (or lack thereof).

**Dependencies:** None

---

### Task 5: Implement Role Assignment Scriban Template

**Priority:** High

**Description:**
Rewrite the `role_assignment.sbn` template to implement the new table-based layout and summary line logic.

**Acceptance Criteria:**
- [ ] Summary line matches the specification for all 4 actions (Create, Update, Replace, Delete).
- [ ] Summary line uses backticks for key identifiers as specified in "Style G".
- [ ] Description line appears below the summary if the attribute is present and non-empty.
- [ ] Details section is wrapped in a collapsible `<details>` tag.
- [ ] Details table uses 2 columns for Create/Delete and 3 columns for Update/Replace.
- [ ] Table includes all non-null attributes and omits attributes that are null in both before/after states.
- [ ] Table values are rendered as plain text (no markdown).

**Dependencies:** Task 3, Task 4

---

### Task 2: Update Comprehensive Demo

**Priority:** Low

**Description:**
Update the project's demo examples to reflect the new role assignment format.

**Acceptance Criteria:**
- [ ] `examples/comprehensive-demo/report.md` is updated with the new rendering.
- [ ] Any other relevant demo variants (e.g., `report-with-sensitive.md`) are updated.

**Dependencies:** Task 5

---

## Implementation Order

1.  **Task 1 & 2 (Foundational Logic)**: These provide the raw data needed for the new format.
2.  **Task 3 (Scriban Integration)**: Exposes the raw data to the templating engine.
3.  **Task 4 (Test Data)**: Provides the basis for verifying the implementation.
4.  **Task 5 (Core Implementation)**: The main work of rewriting the template.
5.  **Task 6 (Documentation)**: Finalizes the feature by updating the public-facing demo.

## Open Questions

None. All requirements and styling decisions have been finalized in the specification.
