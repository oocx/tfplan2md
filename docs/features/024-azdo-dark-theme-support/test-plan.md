# Test Plan: Azure DevOps Dark Theme Support

## Overview

This test plan covers the introduction of theme-adaptive border styling for Terraform resource containers in Azure DevOps reports. The goal is to ensure that `<details>` elements use Azure DevOps CSS variables to adjust their appearance based on the active theme (light or dark), while maintaining a safe fallback for other environments.

Reference: [Specification](specification.md), [Architecture](architecture.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Azure DevOps HTML reports use theme-aware CSS variable for `<details>` | TC-01, TC-02 | Unit / Integration |
| Borders appear subtle in Azure DevOps dark theme | TC-03 | UAT |
| Borders remain appropriate in Azure DevOps light theme | TC-03 | UAT |
| Fallback gray color works in non-Azure DevOps environments | TC-04 | Unit |
| Resource-specific templates are updated consistently | TC-02 | Unit |

## User Acceptance Scenarios

### Scenario 1: Azure DevOps Theme Adaptation

**User Goal**: View the Terraform plan report in Azure DevOps and have it look native in both Light and Dark modes.

**Test PR Context**:
- **Azure DevOps**: Verify rendering in Azure DevOps PR comment.
- **GitHub**: Verify that the changes do not break or negatively impact GitHub rendering (expected to be a no-op as GitHub strips inline styles on `<details>`).

**Expected Output**:
- In **Azure DevOps Light Theme**: The border around resource segments is a subtle light gray.
- In **Azure DevOps Dark Theme**: The border around resource segments is a subtle dark gray, not high-contrast white/light-gray.
- In **Preview HTML** (via `HtmlRenderer`): If the wrapper defines the variable, it should also show the adaptation.

**Success Criteria**:
- [ ] Output renders correctly in Azure DevOps (both themes).
- [ ] Information remains readable and "boxed" appropriately.
- [ ] No visual "flashing" or excessive contrast in dark mode.

---

## Test Cases

### TC-01: Default Resource Template Border Update

**Type:** Unit

**Description:**
Verify that the default resource template (`_resource.sbn`) uses the new theme-aware border style instead of a hardcoded hex color.

**Preconditions:**
- None

**Test Steps:**
1. Render a standard resource change using the default template.
2. Inspect the generated HTML for the `<details>` tag.

**Expected Result:**
The `<details>` tag contains `style="... border: 1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); ..."`

**Test Data:**
`azurerm-azuredevops-plan.json`

---

### TC-02: Resource-Specific Templates Consistency

**Type:** Unit

**Description:**
Verify that all resource-specific templates that bypass the default wrapper also use the new theme-aware border style.

**Preconditions:**
- Templates to check: `role_assignment.sbn`, `network_security_group.sbn`, `firewall_network_rule_collection.sbn`.

**Test Steps:**
1. Render each of the specified resources.
2. Inspect the generated HTML for the `<details>` tag.

**Expected Result:**
Each `<details>` tag contains `style="... border: 1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); ..."`

**Test Data:**
- `role-assignment-plan.json` (if exists, or similar)
- `firewall-rule-changes.json`

---

### TC-03: Visual Verification in Azure DevOps (UAT)

**Type:** UAT

**Description:**
Verify real-world rendering in Azure DevOps Services.

**Preconditions:**
- Acccess to Azure DevOps test organization.

**Test Steps:**
1. Generate a demo report using the updated code.
2. Post as a comment in an Azure DevOps PR.
3. Switch between Light and Dark themes in Azure DevOps settings.

**Expected Result:**
The border color changes automatically or matches the theme's neutral palette without visual jarring.

---

### TC-04: Fallback and Non-Inline Style Environments

**Type:** Unit / Integration

**Description:**
Verify that in environments without the CSS variable (like a local browser or GitHub), the fallback color is used or the style is ignored gracefully.

**Test Steps:**
1. Open the generated Markdown in a GitHub preview or a plain browser without the Azure DevOps wrapper.
2. Inspect rendering.

**Expected Result:**
- In browser: Border is `#999` (fallback).
- In GitHub: Style attribute is stripped (standard GitHub behavior), meaning no border (as before).

---

## Test Data Requirements

- Existing `TestData/azurerm-azuredevops-plan.json` is sufficient for general testing.
- Existing `TestData/firewall-rule-changes.json` is sufficient for resource-specific template testing.
- No new JSON data files are required.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Multiple nested `<details>` | Borders should all use the same variable and look consistent | TC-02 |
| Variable missing in host | Fallback `153, 153, 153` (#999) is used | TC-04 |

## Non-Functional Tests

- **Performance**: No impact expected as this is a tiny CSS change.
- **Compatibility**: Verified that GitHub strips these styles, so no regression there.

## Open Questions

None.
