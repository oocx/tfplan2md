# Test Plan: Display Enhancements

## Overview

This test plan covers the four display improvements introduced in [051-display-enhancements](specification.md): syntax highlighting for large JSON/XML values, enriched API Management subresource summaries, the named values sensitivity fix, and the subscription attributes emoji.

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Large JSON values detected and pretty-printed with ```json | TC-01 | Unit |
| Large XML values detected and pretty-printed with ```xml | TC-02 | Unit |
| Already-formatted JSON/XML values are preserved | TC-03 | Unit |
| Pretty-printing works for updates (simple-diff/inline-diff) | TC-04 | Unit |
| APIM operations (AzureRM) display all required context in summary | TC-05 | Unit |
| APIM named values (AzureRM) display `api_management_name` in summary | TC-06 | Unit |
| Named values (AzureRM) with `secret=false` show actual values | TC-07 | Unit |
| Named values (AzureRM) with `secret=true` remain masked | TC-08 | Unit |
| `subscription_id` and `subscription` attributes get 🔑 emoji | TC-09 | Unit |
| `ReportModelBuilder` respects factory-provided `SummaryHtml` | TC-11 | Unit |
| Existing features and snapshots remain consistent | TC-10 | Integration |

## User Acceptance Scenarios

### Scenario 1: Enhanced APIM and Large Values Rendering

**User Goal**: Review a plan containing API Management operations with complex policies and non-secret named values.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR descriptions.

**Expected Output**:
- APIM operation summaries should be rich: `azurerm_api_management_api_operation "this" "Get Profile" — "get-profile" "apim-service" in "rg-name"`
- `policy_content` (if large) should be pretty-printed and syntax-highlighted.
- `azurerm_api_management_named_value` with `secret=false` should show its value instead of "(sensitive)".
- Subscription IDs in any resource should be prefixed with 🔑.

**Success Criteria**:
- [ ] APIM summaries match the specified "After" format.
- [ ] JSON/XML policies are readable and highlighted.
- [ ] No accidentally exposed secrets.
- [ ] 🔑 emoji is visible and correctly aligned.

## Test Cases

### TC-01: FormatLargeValue_JsonContent_DetectedAndFormatted

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersLargeValueTests.cs`

**Description:**
Verifies that a large unformatted JSON string is detected as JSON, pretty-printed, and wrapped in a ```json code fence.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue` with an unformatted JSON string (e.g., `{"a":1,"b":[1,2,3]}`).
2. Verify the output contains ```json.
3. Verify the output contains newlines and indentation (pretty-printed).

**Expected Result:**
The value is pretty-printed and has the `json` language marker.

---

### TC-02: FormatLargeValue_XmlContent_DetectedAndFormatted

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersLargeValueTests.cs`

**Description:**
Verifies that a large unformatted XML string is detected as XML, pretty-printed, and wrapped in a ```xml code fence.

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue` with an unformatted XML string.
2. Verify the output contains ```xml.
3. Verify the output is pretty-printed.

**Expected Result:**
The value is pretty-printed and has the `xml` language marker.

---

### TC-03: FormatLargeValue_AlreadyFormatted_Preserved

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersLargeValueTests.cs`

**Description:**
Verifies that if the JSON/XML content already contains newlines and indentation, it is preserved (to avoid over-formatting already good content).

**Preconditions:**
- None

**Test Steps:**
1. Call `ScribanHelpers.FormatLargeValue` with already formatted JSON.
2. Verify the output matches the input (except for the code fence wrapper).

**Expected Result:**
Original formatting is preserved.

---

### TC-04: FormatLargeValue_UpdatePath_PrettyPrintsBeforeDiff

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersLargeValueTests.cs`

**Description:**
Verifies that for resource updates, the pretty-printing happens before the diff logic so the diff itself is readable.

**Expected Result:**
The diff (inline or simple) operates on pretty-printed lines.

---

### TC-05: AzureRMApimOperationFactory_ApplyViewModel_SetsRichSummary

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/AzureRMApimOperationFactoryTests.cs`

**Description:**
Verifies that the factory for `azurerm_api_management_api_operation` includes `display_name`, `operation_id`, `api_name`, and `api_management_name` in `SummaryHtml`.

**Test Data:**
A `ResourceChange` for an APIM operation.

---

### TC-06: AzureRMApimSubresourceFactories_ApplyViewModel_IncludeApimName

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/AzureRMApimSummaryTests.cs`

**Description:**
Verifies that various APIM subresources (e.g., `azurerm_api_management_api_policy`, `azurerm_api_management_product`) include `api_management_name` in their `SummaryHtml` when the factory is applied.

---

### TC-07: AzureRMApimNamedValueFactory_ApplyViewModel_OverridesSensitivityWhenNotSecret

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/AzureRMApimNamedValueFactoryTests.cs`

**Description:**
Verifies that `azurerm_api_management_named_value.value` is NOT masked when `secret=false` even if the plan marks it as sensitive.

---

### TC-08: AzureRMApimNamedValueFactory_ApplyViewModel_RespectsTerraformMaskingWhenSecret

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/AzureRMApimNamedValueFactoryTests.cs`

**Description:**
Verifies that `azurerm_api_management_named_value.value` IS masked when `secret=true`.

---

### TC-09: SemanticFormatting_SubscriptionAttributes_AddEmoji

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersSemanticFormattingTests.cs`

**Description:**
Verifies that `subscription_id` and `subscription` attributes are prefixed with 🔑 in all formatting contexts (Table, Summary, Plain).

---

### TC-11: ReportModelBuilder_BuildResourceChange_RespectsFactorySummaryHtml

**Type:** Unit
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderTests.cs`

**Description:**
Verifies that if a `IResourceViewModelFactory` sets `SummaryHtml` on the model, `ReportModelBuilder` does not overwrite it with its default summary logic.

---

### TC-10: Regression_ComprehensiveDemo

**Type:** Integration
**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ComprehensiveDemoTests.cs`

**Description:**
Run the comprehensive demo and verify that there are no regressions in existing rendering, and new features appear where expected.

## Test Data Requirements

- `apim-display-enhancements.json`: A new test data file containing:
  - `azurerm_api_management_api_operation` with unformatted `policy_content`.
  - `azurerm_api_management_named_value` with `secret=false` and `value="https://example.com"`.
  - Various resources with `subscription_id`.

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Invalid JSON/XML | Fall back to plain text code fence | TC-XX |
| Empty JSON/XML | Handled gracefully, no empty fences | TC-XX |
| `secret` attribute missing | Default to Terraform's sensitivity marking | TC-08 |
| `subscription_id` is null | No emoji, no error | TC-09 |

## Open Questions

None.
