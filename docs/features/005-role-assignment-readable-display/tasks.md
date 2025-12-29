# Tasks: Enhanced Azure Role Assignment Display

## Overview

This document breaks down the "Enhanced Azure Role Assignment Display" feature into actionable tasks. The goal is to transform technical Azure IDs into human-readable information in the generated markdown reports.

Reference: [Specification](specification.md), [Architecture](architecture.md), [Test Plan](test-plan.md)

## Tasks

### Task 1: Implement Azure Role Definition Mapping

**Priority:** High

**Description:**
Create a static utility class `AzureRoleDefinitionMapper` that maps Azure built-in Role Definition GUIDs to friendly names.

**Acceptance Criteria:**
- [ ] `AzureRoleDefinitionMapper` class created in `Oocx.TfPlan2Md.Azure` namespace.
- [ ] Contains a `FrozenDictionary` with at least the most common built-in roles (Owner, Contributor, Reader, etc.).
- [ ] `GetRoleName(string roleDefinitionId)` method correctly extracts GUID and returns "Name (GUID)".
- [ ] Returns original string if GUID is not found or input is malformed.
- [ ] Unit tests verify mapping for known roles and fallback for unknown ones.

**Dependencies:** None

---

### Task 2: Implement Azure Scope Parsing

**Priority:** High

**Description:**
Create a static utility class `AzureScopeParser` to parse Azure resource IDs into readable hierarchical strings.

**Acceptance Criteria:**
- [ ] `AzureScopeParser` class created in `Oocx.TfPlan2Md.Azure` namespace.
- [ ] `ParseScope(string scope)` method handles:
    - Management Groups
    - Subscriptions
    - Resource Groups
    - Specific Resources (Key Vault, Storage, etc.)
- [ ] Output uses Markdown bolding for resource names.
- [ ] Unit tests cover all supported scope types and edge cases.

**Dependencies:** None

---

### Task 3: Implement Principal Mapping Logic

**Priority:** Medium

**Description:**
Implement the `PrincipalMapper` to resolve Principal IDs using a user-provided JSON file.

**Acceptance Criteria:**
- [ ] `IPrincipalMapper` interface defined.
- [ ] `PrincipalMapper` class implemented, loading JSON from a file path.
- [ ] `GetPrincipalName(string principalId)` returns "Name (Type) [ID]" if mapped, otherwise just ID.
- [ ] Gracefully handles missing files or malformed JSON (logs warning, continues).
- [ ] Unit tests verify mapping logic and error handling.

**Dependencies:** None

---

### Task 4: Update CLI to support Principal Mapping

**Priority:** Medium

**Description:**
Add the `--principal-mapping` (and `-p`) argument to the CLI.

**Acceptance Criteria:**
- [ ] `CliOptions` updated with `PrincipalMappingFile` property.
- [ ] `CliParser` updated to parse `--principal-mapping` and `-p`.
- [ ] Help text updated to describe the new option.
- [ ] Unit tests verify the new argument is parsed correctly.

**Dependencies:** Task 3

---

### Task 5: Register Scriban Helpers

**Priority:** High

**Description:**
Expose the Azure mapping and parsing logic to Scriban templates via custom helpers.

**Acceptance Criteria:**
- [ ] `ScribanHelpers.RegisterHelpers` updated to accept `IPrincipalMapper`.
- [ ] New helpers registered: `azure_role_name`, `azure_scope`, `azure_principal_name`.
- [ ] Helpers correctly delegate to the respective mapper/parser classes.

**Dependencies:** Task 1, Task 2, Task 3

---

### Task 6: Create Resource-Specific Template

**Priority:** High

**Description:**
Create the Scriban template for `azurerm_role_assignment` to use the new helpers.

**Acceptance Criteria:**
- [ ] Template created at `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn`.
- [ ] Template uses `azure_role_name` for `role_definition_id`.
- [ ] Template uses `azure_scope` for `scope`.
- [ ] Template uses `azure_principal_name` for `principal_id`.
- [ ] Integration tests verify the template is used and output is correctly formatted.

**Dependencies:** Task 5

---

### Task 7: Wire up components in Program.cs

**Priority:** High

**Description:**
Integrate all components in the main application entry point.

**Acceptance Criteria:**
- [ ] `Program.cs` instantiates `PrincipalMapper` using the CLI option.
- [ ] `MarkdownRenderer` updated to accept and use `IPrincipalMapper`.
- [ ] End-to-end flow works as expected.

**Dependencies:** Task 4, Task 6

---

## Implementation Order

1. **Task 1 & 2** - Foundational parsing logic (can be done in parallel).
2. **Task 3** - Principal mapping logic.
3. **Task 4** - CLI updates.
4. **Task 5** - Helper registration.
5. **Task 6** - Template creation.
6. **Task 7** - Final integration.

## Open Questions

None.
