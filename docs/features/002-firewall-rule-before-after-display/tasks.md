# Tasks: Firewall Rule Before/After Attributes Display

## Overview

This feature enhances the Azure firewall rule collection template to show before and after values for modified rule attributes. The implementation involves adding a new Scriban helper function and updating the template to use it.

Reference: [specification.md](specification.md) | [architecture.md](architecture.md)

## Tasks

### Task 1: Implement `format_diff` Helper Function

**Priority:** High

**Description:**
Add a new helper function `format_diff` to `ScribanHelpers.cs` that compares two string values and returns either the single value (if equal) or a diff-formatted string with `-` and `+` prefixes separated by `<br>`.

**Acceptance Criteria:**
- [ ] `FormatDiff` method is added to `ScribanHelpers` class
- [ ] Method signature: `public static string FormatDiff(string? before, string? after)`
- [ ] When `before` equals `after`, method returns the value as-is
- [ ] When `before` differs from `after`, method returns `- {before}<br>+ {after}`
- [ ] Method handles `null` values appropriately (treating them as empty strings for comparison)
- [ ] Method is registered in `ScribanHelpers.RegisterHelpers` as `format_diff`
- [ ] Code follows existing patterns in `ScribanHelpers.cs`
- [ ] XML documentation comments are added to the method

**Dependencies:** None

**Notes:**
- The helper should be simple and focused on string comparison
- Consider how null values should be displayed (empty string vs. "(null)")
- Follow the existing pattern for registering helpers (see `diff_array` as example)

---

### Task 2: Add Unit Tests for `format_diff`

**Priority:** High

**Description:**
Create comprehensive unit tests for the new `format_diff` helper function in `ScribanHelpersTests.cs`.

**Acceptance Criteria:**
- [ ] Test case for equal strings returns single value without prefix
- [ ] Test case for different strings returns diff format with `-` and `+`
- [ ] Test case for `null` before and non-null after
- [ ] Test case for non-null before and `null` after
- [ ] Test case for both `null` values
- [ ] Test case for empty strings vs. null values (if treated differently)
- [ ] All tests pass
- [ ] Test names follow existing naming convention in `ScribanHelpersTests.cs`

**Dependencies:** Task 1

**Notes:**
- Use the existing test patterns in `ScribanHelpersTests.cs` as reference
- Consider edge cases like whitespace differences

---

### Task 3: Update Firewall Rule Template for Modified Rules

**Priority:** High

**Description:**
Update the `azurerm_firewall_network_rule_collection.sbn` template to use the `format_diff` helper for displaying modified rule attributes. Each column should show before/after values when they differ, or a single value when unchanged.

**Acceptance Criteria:**
- [ ] The template's modified rules section uses `format_diff` for all rule attributes
- [ ] Protocols column: `{{ format_diff (item.before.protocols | array.join ", ") (item.after.protocols | array.join ", ") }}`
- [ ] Source Addresses column: `{{ format_diff (item.before.source_addresses | array.join ", ") (item.after.source_addresses | array.join ", ") }}`
- [ ] Destination Addresses column: `{{ format_diff (item.before.destination_addresses | array.join ", ") (item.after.destination_addresses | array.join ", ") }}`
- [ ] Destination Ports column: `{{ format_diff (item.before.destination_ports | array.join ", ") (item.after.destination_ports | array.join ", ") }}`
- [ ] Description column: `{{ format_diff (item.before.description ?? "") (item.after.description ?? "") }}`
- [ ] Rule Name column continues to show `item.after.name` (no diff needed as it's the key)
- [ ] Template syntax is correct and renders without errors
- [ ] Template maintains proper alignment and formatting

**Dependencies:** Task 1

**Notes:**
- The `array.join ", "` should be applied before passing to `format_diff`
- Handle null descriptions with `?? ""` before comparison
- Test the template syntax carefully to ensure Scriban parses it correctly

---

### Task 4: Update Integration Tests with Expected Output

**Priority:** High

**Description:**
Update or add integration tests in `MarkdownRendererResourceTemplateTests.cs` to verify that the firewall rule template correctly renders before/after values for modified rules.

**Acceptance Criteria:**
- [ ] Test uses existing `firewall-rule-changes.json` test data
- [ ] Test verifies that modified rules show diff format for changed attributes
- [ ] Test verifies that unchanged attributes in modified rules show single values (no diff prefix)
- [ ] Test checks for presence of `-` prefix for before values
- [ ] Test checks for presence of `+` prefix for after values
- [ ] Test checks for `<br>` separator between before and after
- [ ] Specific assertions for the `allow-http` rule changes:
  - Source addresses should show diff: `- 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24`
  - Description should show diff: `- Allow HTTP traffic<br>+ Allow HTTP traffic from web and API tiers`
  - Protocols, destination addresses, and ports should show single values
- [ ] All tests pass

**Dependencies:** Task 3

**Notes:**
- Review the `firewall-rule-changes.json` to identify what actually changes
- The `allow-http` rule has changes to `source_addresses` and `description`
- The `allow-https` rule remains unchanged and should show single values
- Consider adding snapshot-style assertions if appropriate

---

### Task 5: Update Documentation

**Priority:** Medium

**Description:**
Update project documentation to reflect the new before/after display feature.

**Acceptance Criteria:**
- [ ] [docs/features.md](../../features.md) is updated with a description of the before/after display
- [ ] [docs/features/001-resource-specific-templates/specification.md](../resource-specific-templates.md) is updated with:
  - Description of `format_diff` helper function
  - Example usage in templates
  - Screenshot or code example of the output format
- [ ] Example output in the specification shows the new diff format
- [ ] All documentation references are accurate and consistent

**Dependencies:** Task 4

**Notes:**
- Include example output showing both changed and unchanged attributes
- Explain the `-` and `+` prefix convention clearly
- Reference standard diff notation to help users understand the format

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1** - Implement the core helper function first, as all other tasks depend on it
2. **Task 2** - Add unit tests immediately to validate the helper works correctly
3. **Task 3** - Update the template to use the new helper
4. **Task 4** - Add integration tests to verify end-to-end behavior
5. **Task 5** - Update documentation once the feature is complete and tested

## Open Questions

None - all requirements are clearly defined in the specification and architecture documents.
