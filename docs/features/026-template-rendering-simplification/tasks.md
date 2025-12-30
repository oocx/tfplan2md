# Tasks: Template Rendering Simplification

## Overview

This feature refactors the template rendering system from a two-pass "render-then-replace" mechanism to a single-pass "direct dispatch" system. It also moves complex formatting logic from Scriban templates into C# ViewModels and split helper classes.

Reference: [Specification](specification.md), [Architecture](architecture.md), [Test Plan](test-plan.md)

## Tasks

### Task 1: Implement `ITemplateLoader` and `resolve_template`

**Priority:** High

**Description:**
Implement a custom Scriban `ITemplateLoader` that can load templates from the built-in resources and the custom template directory. Add a `resolve_template` helper to the Scriban context to map resource types to template paths.

**Acceptance Criteria:**
- [ ] `FileSystemTemplateLoader` (or similar) implemented and registered.
- [ ] Supports loading partials (prefixed with `_`).
- [ ] Supports loading resource-specific templates (e.g., `azurerm/role_assignment`).
- [ ] `resolve_template(type)` returns the correct path or `_resource` as fallback.
- [ ] Unit tests verify template resolution and loading.

**Dependencies:** None

---

### Task 2: Split `ScribanHelpers.cs` and Register Helpers

**Priority:** High

**Description:**
Split the 1700+ line `ScribanHelpers.cs` into focused classes (e.g., `SemanticIcons`, `MarkdownEscaping`, `AzureFormatting`). Create a registry to add these to the Scriban context.

**Acceptance Criteria:**
- [x] `ScribanHelpers.cs` is deleted.
- [x] New helper classes created in `MarkdownGeneration/Helpers/`.
- [x] All existing helper functionality is preserved.
- [x] `ScribanHelperRegistry` (or similar) handles registration.
- [x] No helper file exceeds 250 lines.

**Dependencies:** None

---

### Task 3: Implement `FormattedValue` and `FormattedList` Records

**Priority:** High

**Description:**
Create generic record types to wrap raw values with their pre-computed formatted strings. This allows templates to access both `.raw` and `.formatted` properties.

**Acceptance Criteria:**
- [x] `FormattedValue<T>` record implemented.
- [x] `FormattedList<T>` record implemented.
- [x] Records are serializable/accessible by Scriban.
- [x] Unit tests verify accessibility in templates.

**Dependencies:** None

---

### Task 4: Restructure `default.sbn` into Partials

**Priority:** Medium

**Description:**
Extract the header, summary, and default resource rendering from `default.sbn` into partial templates (`_header.sbn`, `_summary.sbn`, `_resource.sbn`). Update `default.sbn` to use `include` statements.

**Acceptance Criteria:**
- [x] `_header.sbn`, `_summary.sbn`, `_resource.sbn` created.
- [x] `default.sbn` uses `include` for all sections.
- [x] Anchor comments replaced with `data-tfplan-address` attributes.
- [x] Output remains equivalent for resources using the default template.

**Dependencies:** Task 1

---

### Task 5: Migrate Network Security Group Template

**Priority:** Medium

**Description:**
Migrate the NSG template to the new system. Move `func` definitions to a C# ViewModel and use `FormattedValue` for rule attributes.

**Acceptance Criteria:**
- [x] `NetworkSecurityGroupViewModel` implemented with pre-computed formatting.
- [x] `network_security_group.sbn` simplified (no `func` definitions, 53 lines).
- [x] Anchor comments replaced with `data-tfplan-address` attributes.
- [x] Output equivalence verified via snapshots (5 tests passing).

**Dependencies:** Task 1, Task 2, Task 3

---

### Task 6: Migrate Firewall Rule Template

**Priority:** Medium

**Description:**
Migrate the Firewall Rule Collection template. Move logic for before/after display and port formatting to C#.

**Acceptance Criteria:**
- [x] `FirewallNetworkRuleCollectionViewModel` implemented.
- [x] `firewall_network_rule_collection.sbn` simplified (47 lines).
- [x] Anchor comments replaced with `data-tfplan-address` attributes.
- [x] Output equivalence verified via snapshots (13 tests passing).

**Dependencies:** Task 1, Task 2, Task 3

---

### Task 7: Migrate Role Assignment Template

**Priority:** Medium

**Description:**
Migrate the Role Assignment template. This is the most complex template and will benefit most from moving logic to C#.

**Acceptance Criteria:**
- [x] `RoleAssignmentViewModel` implemented with Azure helper integration.
- [x] `role_assignment.sbn` simplified (63 lines, was 224 lines).
- [x] Anchor comments replaced with `data-tfplan-address` attributes.
- [x] Output equivalence verified via snapshots (13 tests passing).

**Dependencies:** Task 1, Task 2, Task 3

---

### Task 8: Update `MarkdownRenderer` and Remove Regex Hacks

**Priority:** High

**Description:**
Update `MarkdownRenderer.cs` to use the new single-pass rendering logic. Remove the anchor replacement code and the 6 identified Regex workarounds.

**Acceptance Criteria:**
- [x] `MarkdownRenderer.Render` uses a single Scriban `Render` call.
- [x] No `Regex.Replace` calls for anchors or whitespace "hacks".
- [x] All tests pass without post-processing (341/341 passing).
- [x] `NormalizeOutput` method handles deterministic trailing newlines.

**Dependencies:** Task 4, Task 5, Task 6, Task 7

---

### Task 9: Implement Automated Template Validation Tests

**Priority:** Low

**Description:**
Add unit tests that scan the `Templates/` directory to enforce architectural constraints (line counts, no `func` definitions, no anchors).

**Acceptance Criteria:**
- [ ] Test fails if any template contains `func`.
- [ ] Test fails if any template contains anchor comments.
- [ ] Test fails if any template exceeds 100 lines.
- [ ] Tests run as part of the standard test suite.

**Dependencies:** All migration tasks

## Implementation Order

1.  **Foundation (Tasks 1-3)**: Set up the new infrastructure and helper organization.
2.  **Core Structure (Task 4)**: Move to the `include`-based layout.
3.  **Resource Migration (Tasks 5-7)**: Migrate specific templates one by one, ensuring snapshots pass.
4.  **Cleanup (Task 8)**: Switch to single-pass rendering and remove hacks.
5.  **Enforcement (Task 9)**: Add the "guardrail" tests.

## Snapshot Consistency and Testing

To ensure a stable migration, we will follow these rules for snapshot testing:

| Task | Snapshot Status | Notes |
|------|-----------------|-------|
| **Tasks 1-3** | **Must Pass** | These are internal refactorings or new infrastructure with no output changes. |
| **Task 4** | **May Fail** | Removing anchors from `default.sbn` will break the old "render-then-replace" logic. Snapshots for NSG, Firewall, and Role Assignments will fail until their respective migration tasks (5-7) are complete OR until `MarkdownRenderer` is updated to use the new dispatch logic. |
| **Tasks 5-7** | **Must Pass (Incremental)** | As each resource is migrated, its specific snapshots must be verified and pass using the new rendering logic. |
| **Task 8** | **Must Pass (All)** | This is the final switchover. All snapshots must pass with the old regex-based code completely removed. |

**Recommendation for Developer:** To keep tests passing during Tasks 4-7, update `MarkdownRenderer` to use the new single-pass logic as soon as Task 4 is implemented. This will allow you to verify each template migration (Tasks 5-7) against the final rendering path immediately.

## Open Questions

None.
