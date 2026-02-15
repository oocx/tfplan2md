# Tasks: Network Security Group Security Rules Template

## Overview

This feature implements a specialized Scriban template for `azurerm_network_security_group` resources. It provides a semantic diff of security rules, categorized by change type (Added, Modified, Removed, Unchanged) and sorted by priority.

Reference:
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)

## Tasks

### Task 1: Create Test Data for NSG Changes

**Priority:** High

**Description:**
Create a Terraform plan JSON file that includes various NSG rule changes to support testing.

**Acceptance Criteria:**
- [ ] File `src/tests/Oocx.TfPlan2Md.Tests/TestData/nsg-rule-changes.json` is created.
- [ ] Includes an `azurerm_network_security_group` resource with an `update` action.
- [ ] `before` state contains at least 4 rules.
- [ ] `after` state contains:
    - One new rule (Added).
    - One rule from `before` is missing (Removed).
    - One rule with the same name but changed port/description (Modified).
    - One rule with the same name and properties (Unchanged).
    - Rules are not in priority order in the JSON array.
    - Mix of singular (`source_address_prefix`) and plural (`source_address_prefixes`) fields.
- [ ] Includes an `azurerm_network_security_group` resource with a `create` action.
- [ ] Includes an `azurerm_network_security_group` resource with a `delete` action.

**Dependencies:** None

---

### Task 2: Implement NSG Security Rules Template

**Priority:** High

**Description:**
Create the Scriban template for `azurerm_network_security_group` that renders security rules in a semantic table.

**Acceptance Criteria:**
- [ ] File `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn` is created.
- [ ] Uses `diff_array` with `"name"` as the key to identify rule changes.
- [ ] Rules are sorted by ascending priority within the table.
- [ ] Table columns match the specification: Name, Priority, Direction, Access, Protocol, Source Addresses, Source Ports, Destination Addresses, Destination Ports, Description.
- [ ] Uses `format_diff` for modified rule attributes.
- [ ] Handles singular/plural address and port fields correctly (plural takes precedence if populated).
- [ ] Uses `escape_markdown` for all text values.

**Dependencies:** Task 1

---

### Task 3: Implement Unit Tests for NSG Template

**Priority:** High

**Description:**
Create unit tests to verify the NSG template rendering logic.

**Acceptance Criteria:**
- [ ] File `src/tests/Oocx.TfPlan2Md.Tests/MarkdownGeneration/MarkdownRendererNsgTemplateTests.cs` is created.
- [ ] **TC-01**: Verifies NSG creation renders a simple table.
- [ ] **TC-02**: Verifies NSG deletion renders a table of removed rules.
- [ ] **TC-03**: Verifies NSG update correctly categorizes rules (➕, 🔄, ❌, ⏺️) and shows diffs.
- [ ] **TC-04**: Verifies rules are sorted by priority (ascending).
- [ ] **TC-05**: Verifies singular/plural address and port field logic.
- [ ] All tests pass.

**Dependencies:** Task 2

---

### Task 4: Update Documentation

**Priority:** Medium

**Description:**
Update the project documentation to reflect the new supported resource template.

**Acceptance Criteria:**
- [ ] `docs/features.md` is updated to include `azurerm_network_security_group` in the "Supported Resources" section.
- [ ] `docs/features/001-resource-specific-templates/specification.md` is updated with details about the NSG template and an example of its output.

**Dependencies:** Task 3

## Implementation Order

1. **Task 1** - Foundational test data required for development and testing.
2. **Task 2** - Core implementation of the template.
3. **Task 3** - Verification of the implementation.
4. **Task 4** - Final documentation update.

## Open Questions

None.
