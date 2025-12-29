# Test Plan: Firewall Rule Before/After Attributes Display

## Overview

This test plan defines how the firewall rule before/after display feature will be tested. The feature adds a `format_diff` Scriban helper function and updates the firewall rule template to show before/after values for changed attributes in modified rules.

Reference: [specification.md](specification.md) | [architecture.md](architecture.md) | [tasks.md](tasks.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Modified rules show before/after values for changed attributes | TC-01, TC-05 | Unit, Integration |
| Unchanged attributes show only single value without prefix | TC-02, TC-05 | Unit, Integration |
| Before values are prefixed with `-` | TC-01, TC-05 | Unit, Integration |
| After values are prefixed with `+` | TC-01, TC-05 | Unit, Integration |
| Before and after values are visually separated | TC-01, TC-05 | Unit, Integration |
| Added, removed, unchanged rules continue to display as before | TC-06 | Integration |
| All existing tests for firewall rule rendering pass | TC-07 | Integration |
| `FormatDiff` handles equal strings correctly | TC-01 | Unit |
| `FormatDiff` handles different strings correctly | TC-01 | Unit |
| `FormatDiff` handles null values appropriately | TC-03, TC-04 | Unit |

## Test Cases

### TC-01: FormatDiff Returns Single Value for Equal Strings

**Type:** Unit

**Description:**
Verifies that the `format_diff` helper returns a single value without any prefix when before and after strings are identical.

**Preconditions:**
- `FormatDiff` method is implemented in `ScribanHelpers`
- Method is properly registered

**Test Steps:**
1. Call `FormatDiff("TCP", "TCP")`
2. Verify the result is `"TCP"`
3. Call `FormatDiff("10.0.1.0/24", "10.0.1.0/24")`
4. Verify the result is `"10.0.1.0/24"`

**Expected Result:**
The method returns the string value as-is without any prefix or `<br>` separator.

**Test Data:**
- Inline test data: equal string pairs

**Test Name:** `FormatDiff_EqualStrings_ReturnsSingleValue`

---

### TC-02: FormatDiff Returns Diff Format for Different Strings

**Type:** Unit

**Description:**
Verifies that the `format_diff` helper returns a diff-formatted string with `-` and `+` prefixes and `<br>` separator when before and after strings differ.

**Preconditions:**
- `FormatDiff` method is implemented in `ScribanHelpers`
- Method is properly registered

**Test Steps:**
1. Call `FormatDiff("TCP", "UDP")`
2. Verify the result is `"- TCP<br>+ UDP"`
3. Call `FormatDiff("10.0.1.0/24", "10.0.1.0/24, 10.0.3.0/24")`
4. Verify the result is `"- 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24"`

**Expected Result:**
The method returns a string in the format `"- {before}<br>+ {after}"`.

**Test Data:**
- Inline test data: different string pairs

**Test Name:** `FormatDiff_DifferentStrings_ReturnsDiffFormat`

---

### TC-03: FormatDiff Handles Null Before Value

**Type:** Unit

**Description:**
Verifies that the `format_diff` helper handles null before values correctly.

**Preconditions:**
- `FormatDiff` method is implemented in `ScribanHelpers`
- Method is properly registered

**Test Steps:**
1. Call `FormatDiff(null, "value")`
2. Verify the result matches expected behavior (likely `"- <br>+ value"` or `"+ value"`)

**Expected Result:**
The method handles null gracefully and returns appropriate diff format or just the after value.

**Test Data:**
- Inline test data: null before, non-null after

**Test Name:** `FormatDiff_NullBefore_ReturnsExpectedFormat`

---

### TC-04: FormatDiff Handles Null After Value

**Type:** Unit

**Description:**
Verifies that the `format_diff` helper handles null after values correctly.

**Preconditions:**
- `FormatDiff` method is implemented in `ScribanHelpers`
- Method is properly registered

**Test Steps:**
1. Call `FormatDiff("value", null)`
2. Verify the result matches expected behavior (likely `"- value<br>+ "` or `"- value"`)

**Expected Result:**
The method handles null gracefully and returns appropriate diff format or just the before value.

**Test Data:**
- Inline test data: non-null before, null after

**Test Name:** `FormatDiff_NullAfter_ReturnsExpectedFormat`

---

### TC-05: FormatDiff Handles Both Null Values

**Type:** Unit

**Description:**
Verifies that the `format_diff` helper handles both values being null.

**Preconditions:**
- `FormatDiff` method is implemented in `ScribanHelpers`
- Method is properly registered

**Test Steps:**
1. Call `FormatDiff(null, null)`
2. Verify the result is empty string or appropriate representation

**Expected Result:**
The method returns empty string or appropriate null representation without throwing an exception.

**Test Data:**
- Inline test data: both values null

**Test Name:** `FormatDiff_BothNull_ReturnsEmptyOrAppropriate`

---

### TC-06: FormatDiff Handles Empty Strings

**Type:** Unit

**Description:**
Verifies that the `format_diff` helper distinguishes between empty strings and null values if treated differently.

**Preconditions:**
- `FormatDiff` method is implemented in `ScribanHelpers`
- Method is properly registered

**Test Steps:**
1. Call `FormatDiff("", "")`
2. Verify the result is `""`
3. Call `FormatDiff("value", "")`
4. Verify the result shows diff format
5. Call `FormatDiff("", "value")`
6. Verify the result shows diff format

**Expected Result:**
The method correctly handles empty strings and distinguishes them from null if applicable.

**Test Data:**
- Inline test data: empty string combinations

**Test Name:** `FormatDiff_EmptyStrings_HandledCorrectly`

---

### TC-07: Modified Firewall Rules Show Diff Format for Changed Attributes

**Type:** Integration

**Description:**
Verifies that the firewall rule template correctly uses `format_diff` to display before/after values for changed attributes in modified rules.

**Preconditions:**
- `FormatDiff` helper is implemented and registered
- Template is updated to use `format_diff`
- Test data file `firewall-rule-changes.json` exists with modified rules

**Test Steps:**
1. Load test data from `firewall-rule-changes.json`
2. Parse the plan
3. Build the report model
4. Render the markdown
5. Locate the modified rule `allow-http` in the output
6. Verify source addresses show: `"- 10.0.1.0/24<br>+ 10.0.1.0/24, 10.0.3.0/24"`
7. Verify description shows: `"- Allow HTTP traffic<br>+ Allow HTTP traffic from web and API tiers"`
8. Verify protocols, destination addresses, and ports show single values (unchanged)

**Expected Result:**
- Changed attributes display diff format with `-` and `+` prefixes and `<br>` separator
- Unchanged attributes display single values without prefixes
- The output is valid markdown

**Test Data:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/firewall-rule-changes.json`

**Test Name:** `Render_FirewallModifiedRules_ShowsDiffForChangedAttributes`

---

### TC-08: Modified Firewall Rules Show Single Value for Unchanged Attributes

**Type:** Integration

**Description:**
Verifies that unchanged attributes in modified firewall rules show only a single value without any diff prefix.

**Preconditions:**
- `FormatDiff` helper is implemented and registered
- Template is updated to use `format_diff`
- Test data file `firewall-rule-changes.json` exists with modified rules

**Test Steps:**
1. Load test data from `firewall-rule-changes.json`
2. Parse the plan
3. Build the report model
4. Render the markdown
5. Locate the modified rule `allow-http` in the output
6. Verify protocols column shows: `"TCP"` (no prefix or `<br>`)
7. Verify destination addresses column shows: `"*"` (no prefix or `<br>`)
8. Verify destination ports column shows: `"80"` (no prefix or `<br>`)
9. Ensure no `-` or `+` prefixes appear for these unchanged attributes

**Expected Result:**
Unchanged attributes display single values without diff formatting.

**Test Data:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/firewall-rule-changes.json`

**Test Name:** `Render_FirewallModifiedRules_ShowsSingleValueForUnchangedAttributes`

---

### TC-09: Added, Removed, and Unchanged Rules Display Correctly

**Type:** Integration

**Description:**
Verifies that the template changes do not affect the display of added, removed, and unchanged firewall rules.

**Preconditions:**
- `FormatDiff` helper is implemented and registered
- Template is updated to use `format_diff`
- Test data file `firewall-rule-changes.json` exists

**Test Steps:**
1. Load test data from `firewall-rule-changes.json`
2. Parse the plan
3. Build the report model
4. Render the markdown
5. Verify added rule `allow-dns` shows ➕ indicator and after values only
6. Verify removed rule `allow-ssh-old` shows ❌ indicator and before values only
7. Verify unchanged rule `allow-https` shows ⏺️ indicator and single values
8. Ensure no diff formatting (`-`/`+` prefixes) appears for these rule types

**Expected Result:**
Added, removed, and unchanged rules continue to display as they did before the feature implementation.

**Test Data:**
- `tests/Oocx.TfPlan2Md.Tests/TestData/firewall-rule-changes.json`

**Test Name:** `Render_FirewallNonModifiedRules_DisplayAsExpected`

---

### TC-10: Existing Firewall Rule Tests Pass

**Type:** Integration

**Description:**
Verifies that all existing tests for firewall rule rendering continue to pass after implementing the feature.

**Preconditions:**
- Feature implementation is complete
- All existing test files are unchanged except for expected updates

**Test Steps:**
1. Run all tests in `MarkdownRendererResourceTemplateTests.cs`
2. Run all tests in `MarkdownRendererTests.cs` that involve firewall rules
3. Verify all tests pass

**Expected Result:**
All existing tests pass without modification (except where test expectations need to be updated to match new diff format behavior).

**Test Data:**
- Existing test data files

**Test Name:** (Existing test names in test catalog)

---

## Test Data Requirements

### Existing Test Data

The feature primarily uses existing test data:

- **`firewall-rule-changes.json`** - Contains:
  - Modified rule collection `web_tier` with:
    - Unchanged rule: `allow-https` (no attribute changes)
    - Added rule: `allow-dns`
    - Modified rule: `allow-http` (changes to `source_addresses` and `description`)
    - Removed rule: `allow-ssh-old`
  - Created rule collection `database_tier`
  - Deleted rule collection `legacy`

### Test Data Analysis for `allow-http` Rule

**Before state:**
```json
{
  "name": "allow-http",
  "description": "Allow HTTP traffic",
  "protocols": ["TCP"],
  "source_addresses": ["10.0.1.0/24"],
  "destination_addresses": ["*"],
  "destination_ports": ["80"]
}
```

**After state:**
```json
{
  "name": "allow-http",
  "description": "Allow HTTP traffic from web and API tiers",
  "protocols": ["TCP"],
  "source_addresses": ["10.0.1.0/24", "10.0.3.0/24"],
  "destination_addresses": ["*"],
  "destination_ports": ["80"]
}
```

**Changed attributes:**
- `source_addresses`: `"10.0.1.0/24"` → `"10.0.1.0/24, 10.0.3.0/24"`
- `description`: `"Allow HTTP traffic"` → `"Allow HTTP traffic from web and API tiers"`

**Unchanged attributes:**
- `protocols`: `"TCP"`
- `destination_addresses`: `"*"`
- `destination_ports`: `"80"`

### New Test Data

No new test data files are required. The existing `firewall-rule-changes.json` provides sufficient coverage.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Equal strings | Return single value without prefix | TC-01 |
| Different strings | Return diff format with `-` and `+` | TC-02 |
| Null before value | Handle gracefully, show after value | TC-03 |
| Null after value | Handle gracefully, show before value | TC-04 |
| Both null values | Return empty or appropriate representation | TC-05 |
| Empty strings | Treat as valid values, not null | TC-06 |
| Unchanged attributes in modified rules | Show single value without diff | TC-08 |
| Modified rules with all attributes changed | Show diff for all attributes | TC-07 |
| Whitespace differences | Treat as different values | (Covered by TC-02) |
| Long values with line breaks | Handle `<br>` correctly | (Covered by TC-07) |

## Non-Functional Tests

### Performance

The `format_diff` helper is a simple string comparison and concatenation operation. No specific performance tests are required, as the overhead is negligible compared to template rendering.

### Error Handling

- **Invalid template syntax**: Existing test `Render_WithInvalidTemplate_ThrowsMarkdownRenderException` covers this
- **Null handling**: TC-03, TC-04, TC-05 cover null scenarios

### Compatibility

- **Scriban version compatibility**: Existing template tests verify Scriban rendering works correctly
- **Markdown rendering**: Integration tests verify the output is valid markdown that renders correctly

## Open Questions

None - all test scenarios are clearly defined.

## Definition of Done

Testing is complete when:
- [ ] All unit tests for `FormatDiff` are implemented and passing (TC-01 through TC-06)
- [ ] All integration tests for template rendering are implemented and passing (TC-07 through TC-09)
- [ ] All existing firewall rule tests continue to pass (TC-10)
- [ ] Code coverage for new code is at least as high as project average
- [ ] No regressions in existing functionality
- [ ] Test names follow project naming conventions (`MethodName_Scenario_ExpectedResult`)
- [ ] All tests are fully automated and executable via `dotnet test`
