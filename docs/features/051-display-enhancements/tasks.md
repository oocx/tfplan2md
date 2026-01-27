# Tasks: Display Enhancements

## Overview

This feature introduces focused display improvements to enhance readability and context:
1. Syntax highlighting for large JSON/XML values (with pretty-printing)
2. Enriched API Management subresource summaries
3. Named values sensitivity override when `secret=false`
4. Subscription attributes emoji (🔑)

Reference documents:
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)
- [UAT Test Plan](uat-test-plan.md)

## Tasks

### Task 1: Subscription Attribute Emoji (🔑)

**Priority:** High

**Description:**
Add the 🔑 emoji prefix to `subscription_id` and `subscription` attribute values globally.

**Acceptance Criteria:**
- [ ] `ScribanHelpers.FormatAttributeValueTable`, `FormatAttributeValueSummary`, and `FormatAttributeValuePlain` are updated to detect `subscription_id` and `subscription` (case-insensitive).
- [ ] Detected attributes are prefixed with `🔑` and a non-breaking space.
- [ ] Unit tests (TC-09) verify the emoji prefix in all three formatting contexts.

**Dependencies:** None

---

### Task 2: API Management Summary Enrichment

**Priority:** High

**Description:**
Update the resource summary builder to include more context for API Management subresources.

**Acceptance Criteria:**
- [ ] `ResourceSummaryHtmlBuilder.BuildSummaryHtml` is updated to include `api_management_name` generically for any resource that has it.
- [ ] Special handling for `azurerm_api_management_api_operation` to include `display_name`, `operation_id`, and `api_name`.
- [ ] Output format matches: `azurerm_api_management_api_operation \`this\` \`display_name\` — \`operation_id\` \`api_name\` \`api_management_name\` in \`📁 resource-group\``.
- [ ] Unit tests (TC-05, TC-06) verify the enriched summary for various APIM resources.

**Dependencies:** None

---

### Task 3: Named Values Sensitivity Override

**Priority:** High

**Description:**
Override Terraform's sensitivity marking for `azurerm_api_management_named_value.value` when the `secret` attribute is `false`.

**Acceptance Criteria:**
- [ ] `ReportModelBuilder.BuildAttributeChanges` is modified to detect `azurerm_api_management_named_value`.
- [ ] For these resources, if `secret` is `false`, the `value` attribute's `isSensitive` flag is forced to `false`.
- [ ] Sensitivity is preserved if `secret` is `true` or missing.
- [ ] Unit tests (TC-07, TC-08) verify that non-secret values are revealed while secret ones stay masked.

**Dependencies:** None

---

### Task 4: Syntax Highlighting for Large Values

**Priority:** Medium (Complex)

**Description:**
Implement JSON/XML detection and pretty-printing for large values, adding language-specific code fences.

**Acceptance Criteria:**
- [ ] `ScribanHelpers.FormatLargeValue` handles JSON/XML detection and formatting.
- [ ] JSON is detected via `System.Text.Json` and pretty-printed.
- [ ] XML is detected via `System.Xml.Linq` and pretty-printed.
- [ ] Large values are wrapped in fenced code blocks with language markers (```json, ```xml).
- [ ] Resource updates (simple-diff/inline-diff) pretty-print the content *before* diffing to ensure the diff is readable.
- [ ] Already-formatted content is preserved (TC-03).
- [ ] Unit tests (TC-01, TC-02, TC-03, TC-04) pass.

**Dependencies:** None

---

### Task 5: Integration & UAT

**Priority:** High

**Description:**
Create integrated test data and run the UAT workflow to verify rendering on GitHub and Azure DevOps.

**Acceptance Criteria:**
- [ ] `examples/apim-display-enhancements.json` is created with test data for all four improvements.
- [ ] `artifacts/apim-display-enhancements-demo.md` is generated and committed.
- [ ] UAT simulation is run and passes (TC-10).
- [ ] Documentation is updated if necessary.

**Dependencies:** Task 1, Task 2, Task 3, Task 4

## Implementation Order

1. **Task 1: Subscription Emoji** - Foundation and quick win.
2. **Task 3: Named Values Override** - Foundational change in model building.
3. **Task 2: APIM Summary Enrichment** - High value, independent logic.
4. **Task 4: Syntax Highlighting** - More complex logic in helpers.
5. **Task 5: UAT** - Final verification.

## Open Questions

None.
