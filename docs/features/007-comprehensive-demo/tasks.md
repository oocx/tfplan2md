# Tasks: Comprehensive Demo

## Overview

This feature provides a comprehensive Terraform plan JSON example that demonstrates all supported features of `tfplan2md`. It serves as documentation, a test fixture, and a "try it now" experience for users.

Reference:
- [Specification](../../features/comprehensive-demo-specification.md)
- [Architecture](../../features/comprehensive-demo/architecture.md)

## Tasks

### Task 1: Setup Directory and Initial Files

**Priority:** High

**Description:**
Create the `examples/comprehensive-demo/` directory and initialize the `plan.json`, `demo-principals.json`, and `README.md` files.

**Acceptance Criteria:**
- [ ] Directory `examples/comprehensive-demo/` exists.
- [ ] `plan.json` exists with basic Terraform JSON metadata (`format_version`, `terraform_version`, `timestamp`).
- [ ] `demo-principals.json` exists with a valid JSON object structure.
- [ ] `README.md` exists with a title and introduction.

**Dependencies:** None

---

### Task 2: Implement Core Resource Changes in `plan.json`

**Priority:** High

**Description:**
Craft the `resource_changes` array in `plan.json` to cover action types, module grouping, and resource type breakdown.

**Acceptance Criteria:**
- [ ] Includes all action types: `create`, `update`, `delete`, `replace`, `no-op`.
- [ ] Implements module grouping: root, `module.network`, `module.security`, and nested `module.network.module.monitoring`.
- [ ] Includes at least 8 different resource types for `create` actions.
- [ ] Includes at least 4 different resource types for `update` actions.
- [ ] Includes at least 2 different resource types for `replace` actions.
- [ ] Includes at least 3 different resource types for `delete` actions.
- [ ] Resource addresses correctly reflect the module hierarchy.

**Dependencies:** Task 1

---

### Task 3: Implement Advanced Template Features in `plan.json`

**Priority:** High

**Description:**
Add complex attributes, sensitive values, and resource-specific data (Azure roles, Firewall rules) to `plan.json`.

**Acceptance Criteria:**
- [ ] Includes sensitive attributes in at least two resources (e.g., `azurerm_key_vault_secret`, `azurerm_storage_account`).
- [ ] Includes complex attribute types: lists, maps, nested objects, and computed values (`(known after apply)`).
- [ ] Includes `azurerm_role_assignment` resources with various scope types (Management Group, Subscription, Resource Group, Resource).
- [ ] Includes `azurerm_firewall_network_rule_collection` with an update action showing added, removed, modified, and unchanged rules.
- [ ] Attribute tables for different actions show correct columns (2-column for create/delete, 3-column for update/replace).

**Dependencies:** Task 2

---

### Task 4: Configure Principal Mappings

**Priority:** Medium

**Description:**
Populate `demo-principals.json` with mappings that correspond to the principal IDs used in the `azurerm_role_assignment` resources in `plan.json`.

**Acceptance Criteria:**
- [ ] `demo-principals.json` contains mappings for at least one user, one group, and one service principal.
- [ ] At least one role assignment in `plan.json` uses a principal ID NOT in the mapping file (to test fallback).

**Dependencies:** Task 3

---

### Task 5: Update Dockerfile for Demo Inclusion

**Priority:** High

**Description:**
Modify the `Dockerfile` to copy the `examples/comprehensive-demo/` directory into the runtime image.

**Acceptance Criteria:**
- [ ] `Dockerfile` has a `COPY` instruction in the runtime stage.
- [ ] Files are copied to `/examples/comprehensive-demo/` in the image.
- [ ] Image build succeeds.

**Dependencies:** Task 1

---

### Task 6: Complete README and Generate Sample Reports

**Priority:** Medium

**Description:**
Finalize the `README.md` with feature coverage and usage instructions. Generate the sample report files using the local build of `tfplan2md`.

**Acceptance Criteria:**
- [ ] `README.md` includes a feature coverage matrix.
- [ ] `README.md` includes Docker usage examples.
- [ ] `report.md` is generated and shows all features correctly rendered.
- [ ] `report-with-sensitive.md` is generated and shows sensitive values revealed.
- [ ] `report-summary.md` is generated using the summary template.

**Dependencies:** Task 3, Task 4

---

### Task 7: Final Verification and Documentation

**Priority:** Low

**Description:**
Perform a final check against all success criteria in the specification.

**Acceptance Criteria:**
- [ ] All 10 required features from the spec are verified in the generated reports.
- [ ] Docker image can be used to generate the report using the internal demo files.
- [ ] Documentation is consistent and complete.

**Dependencies:** Task 5, Task 6

## Implementation Order

1. **Task 1** - Foundation for all other tasks.
2. **Task 2 & 3** - Core content of the demo.
3. **Task 4** - Required for role assignment feature.
4. **Task 5** - Required for Docker distribution.
5. **Task 6** - Finalizes the user-facing artifacts.
6. **Task 7** - Quality assurance.

## Open Questions

- None at this time.
