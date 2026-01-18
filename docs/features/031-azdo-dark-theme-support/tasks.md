# Tasks: Azure DevOps Dark Theme Support

## Overview

This feature implements theme-adaptive border styling for Terraform resource containers in Azure DevOps HTML reports. It replaces hard-coded light-theme borders with the Azure DevOps theme-aware CSS variable `--palette-neutral-10`.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Tasks

### Task 1: Update Default Resource Template

**Priority:** High

**Status:** ✅ Complete

**Description:**
Update the primary resource template to use the theme-adaptive border style.

**Acceptance Criteria:**
- [ ] [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn) is updated.
- [ ] Border color is changed from `#f0f0f0` to `rgb(var(--palette-neutral-10, 153, 153, 153))`.
- [ ] Existing spacing and style (1px solid) are preserved.

**Dependencies:** None

---

### Task 2: Update Resource-Specific Templates

**Priority:** High

**Status:** ✅ Complete

**Description:**
Update all resource-specific templates that define their own `<details>` border inline to ensure consistency across all resource types.

**Acceptance Criteria:**
- [ ] [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/role_assignment.sbn) updated.
- [ ] [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/network_security_group.sbn) updated.
- [ ] [src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn](src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azurerm/firewall_network_rule_collection.sbn) updated.
- [ ] All use `rgb(var(--palette-neutral-10, 153, 153, 153))` for the border color.

**Dependencies:** None

---

### Task 3: Update Azure DevOps Preview Wrapper

**Priority:** Medium

**Status:** ✅ Complete

**Description:**
Update the HTML preview wrapper to define the `--palette-neutral-10` variable for both light and dark themes. This ensures that demo artifacts and local previews correctly simulate the Azure DevOps environment.

**Acceptance Criteria:**
- [ ] [src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html](src/tools/Oocx.TfPlan2Md.HtmlRenderer/templates/azdo-wrapper.html) updated.
- [ ] CSS variable `--palette-neutral-10` is defined within `[data-theme="light"]` and `[data-theme="dark"]` scopes.
- [ ] Light theme value should be a light gray (e.g., `240, 240, 240` for `#f0f0f0`).
- [ ] Dark theme value should be a dark gray (e.g., `50, 50, 50`).

**Dependencies:** None

---

### Task 4: Verify with Unit Tests

**Priority:** High

**Status:** ✅ Complete

**Description:**
Run existing unit tests and verify the generated output contains the new border style.

**Acceptance Criteria:**
- [ ] `dotnet test` passes.
- [ ] Verified that output from `TC-01` and `TC-02` (from [test-plan.md](test-plan.md)) contains the expected `rgb(...)` string.

**Dependencies:** Task 1, Task 2

---

### Task 5: Generate and Verify Demo Artifacts (UAT)

**Priority:** Medium

**Status:** ✅ Complete

**Description:**
Regenerate repository demo artifacts and perform visual verification (UAT).

**Acceptance Criteria:**
- [ ] Run `scripts/generate-demo-artifacts.sh`.
- [ ] Manual verification of `artifacts/comprehensive-demo.azdo.html` (light) and `artifacts/comprehensive-demo.azdo-dark.html` (dark) shows appropriate borders.
- [ ] Verified no regression in GitHub artifacts (borders should simply be missing as before).

**Dependencies:** Task 1, Task 2, Task 3

## Implementation Order

1. **Task 1 & 2**: Core template updates (foundational).
2. **Task 3**: Update preview tooling (enables visual verification).
3. **Task 4**: Automated verification.
4. **Task 5**: Final demo artifact generation and UAT.

## Open Questions

None.
