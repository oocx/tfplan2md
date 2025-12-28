# Tasks: Visual Report Enhancements

## Overview

This feature improves the visual appearance of Terraform plan reports by introducing collapsible resource sections, semantic icons for data values (IPs, booleans, protocols), and better module separation. The implementation follows a "model-first" approach, keeping Scriban templates simple and moving complex formatting logic to C#.

Reference: [specification.md](specification.md), [architecture.md](architecture.md), [test-plan.md](test-plan.md)

## Tasks

### Task 1: Infrastructure - Stable Resource Block Anchors

**Priority:** High

**Description:**
Update the `MarkdownRenderer` to use HTML comment anchors for resource block replacement instead of relying on visible headings. This ensures that resource-specific templates can be replaced correctly even when the heading structure changes.

**Acceptance Criteria:**
- [ ] `MarkdownRenderer.cs` uses `<!-- tfplan2md:resource-start address=... -->` and `<!-- tfplan2md:resource-end address=... -->` for regex replacement.
- [ ] `default.sbn` emits these anchors for every resource.
- [ ] All resource-specific templates (`azurerm/network_security_group.sbn`, `azurerm/firewall_network_rule_collection.sbn`, `azurerm/role_assignment.sbn`) emit these anchors.

**Dependencies:** None

---

### Task 2: C# Helpers - Semantic Value Formatting

**Priority:** High

**Description:**
Implement context-aware formatting helpers in `ScribanHelpers.cs` to handle semantic icons and ensure Azure DevOps compatibility (HTML `<code>` in summaries).

**Acceptance Criteria:**
- [ ] `format_code_summary(text)` returns `<code>text</code>`.
- [ ] `format_code_table(text)` returns `` `text` ``.
- [ ] `format_attribute_value_summary(attr_name, value, provider)` applies semantic icons and uses `<code>`.
- [ ] `format_attribute_value_table(attr_name, value, provider)` applies semantic icons and uses backticks.
- [ ] Implement semantic mappers for:
    - Booleans: `✅ true`, `❌ false`
    - Access: `✅ Allow`, `⛔ Deny`
    - Direction: `⬇️ Inbound`, `⬆️ Outbound`
    - Protocol: `🔗 TCP`, `📨 UDP`, `📡 ICMP`, `✳️ *`
    - IP/CIDR: `🌐 10.0.0.0/16` (icon inside code)
    - Location: `🌍 eastus` (icon inside code)

**Dependencies:** None

---

### Task 3: Model Updates - Precomputed Summary Fields

**Priority:** High

**Description:**
Add precomputed fields to `ResourceChangeModel` to keep templates simple and layout-focused.

**Acceptance Criteria:**
- [ ] `ResourceChangeModel` has `SummaryHtml`, `ChangedAttributesSummary`, and `TagsBadges` properties.
- [ ] `ReportModelBuilder.cs` populates these fields.
- [ ] `SummaryHtml` contains the rich summary line content (Action, Type, Name, Context) using `<code>`.
- [ ] `ChangedAttributesSummary` contains `2 🔧 attr1, attr2` for updates.
- [ ] `TagsBadges` contains `**🏷️ Tags:** `key: value` `...`` for creates/deletes.

**Dependencies:** Task 2

---

### Task 4: Template Overhaul - Default Template

**Priority:** Medium

**Description:**
Update `default.sbn` to implement the new visual layout.

**Acceptance Criteria:**
- [ ] Module separators (`---`) and icons (📦) are implemented.
- [ ] Resources are wrapped in `<details>` and `<summary>`.
- [ ] `<br>` is added after `</summary>`.
- [ ] Uses `SummaryHtml`, `ChangedAttributesSummary`, and `TagsBadges` from the model.
- [ ] Uses new helpers for table values.

**Dependencies:** Task 1, Task 3

---

### Task 5: Template Alignment - Resource-Specific Templates

**Priority:** Medium

**Description:**
Update resource-specific templates to match the new visual style and use the new anchors.

**Acceptance Criteria:**
- [ ] NSG, Firewall, and Role Assignment templates use the new `<details>/<summary>` structure.
- [ ] They use the new semantic formatting helpers.

**Dependencies:** Task 1, Task 2

---

### Task 6: Verification and Regression

**Priority:** High

**Description:**
Update existing tests and add new ones to verify the enhancements.

**Acceptance Criteria:**
- [ ] Existing tests in `MarkdownRendererTests.cs` and `ScribanHelpersTests.cs` are updated.
- [ ] New tests from `test-plan.md` are implemented.
- [ ] UAT passes on GitHub and Azure DevOps using `uat-test-plan.md`.

**Dependencies:** Task 4, Task 5

## Implementation Order

Recommended sequence for implementation:
1. **Task 1 & Task 2** - Foundational infrastructure and formatting logic.
2. **Task 3** - Model precomputation (depends on Task 2).
3. **Task 4** - Main template overhaul (depends on Task 1 and Task 3).
4. **Task 5** - Resource-specific template alignment.
5. **Task 6** - Final verification.

## Open Questions

None.
