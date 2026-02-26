# Test Plan: Case-Insensitive Attribute Change Filter

## Overview

This test plan covers the `--ignore-azure-id-case-changes` CLI flag introduced by feature 103. When enabled, the flag suppresses attribute change rows where the before **and** after values are **Azure resource IDs** (detected by `AzureScopeParser.IsAzureResourceId()`) that are equal under case-insensitive (ordinal) comparison. The filter is disabled by default and takes precedence over `--show-unchanged-values` for casing-only Azure ID rows.

**Reference:** [specification.md](./specification.md) | [architecture.md](./architecture.md)

**Architecture summary (revised 2025-07-14):** The filter is implemented as a new `IAttributeChangeFilter` / `AttributeChangeFilterRegistry` extension point in `MarkdownGeneration/Services/`, mirroring the existing `IValueFormatter` / `ValueFormatterRegistry` pattern. The Azure-specific implementation lives entirely in `Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs`. The core pipeline (`BuildAttributeChanges()`) gains only a single delegate call to the registry — **no Azure-specific logic is present in `MarkdownGeneration/`**. Non-Azure-ID strings and non-azurerm provider resources are **never** filtered.

The integration tests follow the pattern established by `--show-unchanged-values` (feature 014) and mirror `ReportModelBuilderUnchangedValuesTests.cs` and `CliParserTests.cs`. The unit tests for the filter class and registry are new, following the `AzureValueFormatterTests.cs` isolation pattern.

---

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| CLI flag `--ignore-azure-id-case-changes` appears in help text | TC-10 | Unit |
| Flag absent → no regression; output identical to current behavior | TC-01, TC-11 | Unit |
| Flag present → Azure ID casing-only rows suppressed | TC-02, TC-03 | Unit |
| All Azure ID rows casing-only → attribute table has no rows | TC-02 | Unit |
| Mixed changes → only Azure ID casing-only rows suppressed; genuine changes remain | TC-03 | Unit |
| Non-Azure-ID string values (display names, descriptions) are NOT suppressed | TC-15 | Unit |
| Non-azurerm provider resources are NOT filtered | TC-16 | Unit |
| Non-string values (numbers, booleans) unaffected | TC-06 | Unit |
| Null before value → row shown normally (no case comparison) | TC-04 | Unit |
| Null after value → row shown normally (no case comparison) | TC-05 | Unit |
| `--ignore-azure-id-case-changes` takes precedence over `--show-unchanged-values` | TC-07 | Unit |
| `ReportModel.IgnoreAzureIdCaseChanges` reflects the flag value | TC-12, TC-13 | Unit |
| `CliOptions.IgnoreAzureIdCaseChanges` defaults to `false` | TC-09 | Unit |
| `--ignore-azure-id-case-changes` flag sets `IgnoreAzureIdCaseChanges = true` in parsed options | TC-08 | Unit |
| Filter is consistent across all attribute change tables | TC-03 (multi-resource plan) | Unit |
| `ignore_azure_id_case_changes` Scriban variable accessible in templates | TC-14 | Unit |
| `AzureResourceIdCaseChangeFilter` suppresses Azure ID casing changes | TC-17 | Unit |
| `AzureResourceIdCaseChangeFilter` does NOT suppress non-Azure-ID string changes | TC-18 | Unit |
| `AzureResourceIdCaseChangeFilter` does NOT suppress non-azurerm provider values | TC-19 | Unit |
| `AzureResourceIdCaseChangeFilter` does NOT suppress when BeforeValue is null | TC-20 | Unit |
| `AzureResourceIdCaseChangeFilter` does NOT suppress when AfterValue is null | TC-21 | Unit |
| `AttributeChangeFilterRegistry` empty → returns false | TC-22 | Unit |
| `AttributeChangeFilterRegistry` returns true when any filter returns true | TC-23 | Unit |
| `AttributeChangeFilterRegistry` returns false when all filters return false | TC-24 | Unit |

---

## Test Cases

### New file: `ReportModelBuilderIgnoreAzureIdCaseChangesTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderIgnoreAzureIdCaseChangesTests.cs`

Test data dependency: `TestData/azurerm-case-only-ids-plan.json` (see [Test Data Requirements](#test-data-requirements) below).

---

#### TC-01: Flag absent — Azure ID casing-only rows are included (no regression)

**Type:** Unit

**Maps to criterion:** "Flag absent → no regression; output identical to current behavior"

**Description:**
When `ignoreCaseChanges` is `false` (the default), attribute rows for azurerm resources where before and after values are Azure resource IDs that differ only in casing are included in the model, not suppressed.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource with at least one Azure resource ID attribute where before and after differ only in casing (e.g., `scope`: `/subscriptions/ABC123/…` vs `/subscriptions/abc123/…`).

**Test Method:** `Build_IgnoreAzureIdCaseChangesFalse_IncludesCasingOnlyRows`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: false)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the resource with Azure ID casing-only changes.

**Expected Result:**
- The casing-only Azure ID attribute row(s) are present in `AttributeChanges` (count matches the known total changed attributes).

---

#### TC-02: All rows are Azure ID casing-only — attribute table is empty after suppression

**Type:** Unit

**Maps to criterion:** "Flag present → all Azure ID rows casing-only → attribute table has no rows"

**Description:**
When `ignoreCaseChanges` is `true` and every attribute change in a resource is a casing-only difference on Azure resource IDs, the resulting `AttributeChanges` collection is empty. The filter acts via `AzureResourceIdCaseChangeFilter` which checks `AzureScopeParser.IsAzureResourceId()` on each value.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource whose only attribute changes are Azure resource IDs differing only in casing (e.g., `scope` and `role_definition_id`).

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_AllAzureIdCasingOnly_AttributeChangesEmpty`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the all-Azure-ID-casing-only resource.

**Expected Result:**
- `AttributeChanges` is empty (count = 0) for that resource.

---

#### TC-03: Mixed changes — only Azure ID casing-only rows suppressed; genuine changes remain

**Type:** Unit

**Maps to criterion:** "Mixed changes → only Azure ID casing-only rows suppressed; genuine changes remain"

**Description:**
When `ignoreCaseChanges` is `true` and a resource has both Azure ID casing-only changes and genuine attribute changes, only the casing-only Azure ID rows are suppressed. Non-Azure-ID attribute changes (e.g., `display_name` changing from `"My App"` to `"My Application"`) are **not** Azure resource IDs, so `AzureScopeParser.IsAzureResourceId()` returns false for them, and they remain visible.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains a resource with an Azure resource ID attribute casing-only change (e.g., `scope`) AND a genuine non-ID change (e.g., `display_name` from `"My App"` to `"My Application"`).

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_MixedChanges_RetainsGenuineChanges`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the mixed-changes resource.

**Expected Result:**
- The genuine change attribute (e.g., `display_name`) is present in `AttributeChanges`.
- The Azure ID casing-only attribute (e.g., `scope`) is absent from `AttributeChanges`.
- `AttributeChanges.Count` equals the number of genuine (non-Azure-ID-casing) changes only.

---

#### TC-04: Null before value — row is shown normally

**Type:** Unit

**Maps to criterion:** "Null before value → row shown normally (no case comparison)"

**Description:**
`AzureResourceIdCaseChangeFilter.ShouldSuppress()` returns `false` immediately when `BeforeValue` is `null` (first guard in the filter implementation). The row is treated like any other changed attribute.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource with an attribute whose before value is `null` and after value is a non-null string.

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_NullBeforeValue_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the attribute with a null before value.

**Expected Result:**
- The attribute row with null before is present in `AttributeChanges` (not suppressed).

---

#### TC-05: Null after value — row is shown normally

**Type:** Unit

**Maps to criterion:** "Null after value → row shown normally (no case comparison)"

**Description:**
`AzureResourceIdCaseChangeFilter.ShouldSuppress()` returns `false` immediately when `AfterValue` is `null` (first guard in the filter implementation).

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource with an attribute whose before value is a non-null string and after value is `null`.

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_NullAfterValue_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the attribute with a null after value.

**Expected Result:**
- The attribute row with null after is present in `AttributeChanges` (not suppressed).

---

#### TC-06: Non-string values (numbers/booleans) — unaffected by filter

**Type:** Unit

**Maps to criterion:** "Non-string values unaffected"

**Description:**
Numeric attribute values (e.g., `7`, `14`) are not Azure resource IDs, so `AzureScopeParser.IsAzureResourceId()` returns false for their string representations. `AzureResourceIdCaseChangeFilter.ShouldSuppress()` returns false at the ID-detection guard, and the row is included normally.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource with a numeric attribute that genuinely changes value (e.g., `soft_delete_retention_days`: `7` → `14`).

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_NumericAttributeChange_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the numeric attribute that changes value.

**Expected Result:**
- The numeric attribute change row is present in `AttributeChanges`.

---

#### TC-07: `--ignore-azure-id-case-changes` takes precedence over `--show-unchanged-values`

**Type:** Unit

**Maps to criterion:** "Rows suppressed by `--ignore-azure-id-case-changes` remain hidden even when `--show-unchanged-values` is also passed"

**Description:**
The `AzureResourceIdCaseChangeFilter` guard in `BuildAttributeChanges()` executes before the `valuesEqual` guard and causes an unconditional `continue`. Even when `showUnchangedValues: true` is set (which would normally surface rows where before == after), Azure ID casing-only rows are still hidden.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource with Azure resource ID casing-only attribute changes.

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_AndShowUnchangedValues_CasingRowsStillSuppressed`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: true, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the casing-only resource.

**Expected Result:**
- Azure ID casing-only rows are absent from `AttributeChanges` (suppressed despite `showUnchangedValues: true`).
- Truly unchanged rows (before == after ordinal) ARE present (because `showUnchangedValues: true` affects those).
- Genuine change rows are present.

---

#### TC-15: Non-Azure-ID string casing-only change — NOT suppressed

**Type:** Unit

**Maps to criterion:** "Non-Azure-ID attribute values (plain names, numeric, boolean, null) are NOT suppressed by this filter regardless of the flag"

**Description:**
When an azurerm resource has an attribute whose before and after values differ only in casing, but the values are **not** Azure resource IDs (e.g., `display_name`: `"MyApp"` → `"myapp"`), `AzureScopeParser.IsAzureResourceId()` returns false for both values and `AzureResourceIdCaseChangeFilter.ShouldSuppress()` returns false. The row must appear in `AttributeChanges` even though it is a casing-only difference.

This test is critical to validate that the filter does **not** behave as a blanket string casing filter.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` contains an azurerm resource with a non-Azure-ID attribute that differs only in casing (e.g., `display_name`: before `"MyApp"`, after `"myapp"`).

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_NonAzureIdStringCasingChange_RowIsShown`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Find the resource with a non-Azure-ID display-name casing change.

**Expected Result:**
- The non-Azure-ID attribute row (e.g., `display_name`) is present in `AttributeChanges` (NOT suppressed).

---

#### TC-16: Non-azurerm provider resource — NOT filtered even with Azure ID-shaped values

**Type:** Unit

**Maps to criterion:** "Non-azurerm provider resources are NOT filtered"

**Description:**
`AzureResourceIdCaseChangeFilter` returns false when the provider name does not match the azurerm provider pattern (second guard in the filter implementation). A resource from a different provider (e.g., `aws_iam_role` with a value that happens to look like an Azure resource ID path) must not be filtered.

**Preconditions:**
- `azurerm-case-only-ids-plan.json` (or a separate plan file for non-azurerm providers) contains a non-azurerm resource with an attribute whose before and after values look like Azure resource ID paths but differ only in casing.

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_NonAzureRmProvider_RowIsShown`

**Test Steps:**
1. Parse the plan that contains a non-azurerm resource with Azure ID-shaped values differing only in casing.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Retrieve attribute changes for the non-azurerm resource.

**Expected Result:**
- The attribute row with Azure ID-shaped values differing only in casing is present in `AttributeChanges` (not filtered for non-azurerm provider).

---

### Updates to `CliParserTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`

---

#### TC-08: `--ignore-azure-id-case-changes` flag sets `IgnoreAzureIdCaseChanges = true`

**Type:** Unit

**Maps to criterion:** "CLI flag `--ignore-azure-id-case-changes` is implemented"

**Test Method:** `Parse_IgnoreAzureIdCaseChangesFlag_SetsIgnoreAzureIdCaseChangesTrue`

**Test Steps:**
1. Call `CliParser.Parse(new[] { "--ignore-azure-id-case-changes" })`.

**Expected Result:**
- `options.IgnoreAzureIdCaseChanges.Should().BeTrue()`

---

#### TC-09: Default options — `IgnoreAzureIdCaseChanges` defaults to `false`

**Type:** Unit

**Maps to criterion:** "Flag absent → no regression; flag is disabled by default"

**Description:**
Update the existing `Parse_NoArgs_ReturnsDefaultOptions` test to include an assertion that `options.IgnoreAzureIdCaseChanges.Should().BeFalse()`.

**Test Method:** Update `Parse_NoArgs_ReturnsDefaultOptions` (add one assertion line)

**Expected Result:**
- `options.IgnoreAzureIdCaseChanges.Should().BeFalse()`

---

### Updates to `HelpTextProviderTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/CLI/HelpTextProviderTests.cs`

---

#### TC-10: Help text includes `--ignore-azure-id-case-changes` option

**Type:** Unit

**Maps to criterion:** "Help text documents the new flag"

**Test Method:** `GetHelpText_IncludesIgnoreAzureIdCaseChangesOption`

**Test Steps:**
1. Call `HelpTextProvider.GetHelpText()`.

**Expected Result:**
- `help.Should().Contain("--ignore-azure-id-case-changes")`
- `help.Should().Contain("casing")` (description references casing)

---

### Additional tests for `ReportModelBuilderIgnoreAzureIdCaseChangesTests.cs`

---

#### TC-11: Default model — `IgnoreAzureIdCaseChanges` is `false` when not specified

**Type:** Unit

**Maps to criterion:** "`ReportModel.IgnoreAzureIdCaseChanges` reflects the flag value"

**Test Method:** `Build_Default_IgnoreAzureIdCaseChangesFalseInModel`

**Test Steps:**
1. Parse any valid plan JSON.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false)` (no `ignoreCaseChanges` argument — uses default).
3. Call `Build(plan)`.

**Expected Result:**
- `model.IgnoreAzureIdCaseChanges.Should().BeFalse()`

---

#### TC-12: Model reflects `IgnoreAzureIdCaseChanges = true` when specified

**Type:** Unit

**Maps to criterion:** "`ReportModel.IgnoreAzureIdCaseChanges` reflects the flag value"

**Test Method:** `Build_WithIgnoreAzureIdCaseChangesTrue_ModelReflectsFlag`

**Test Steps:**
1. Parse any valid plan JSON.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.

**Expected Result:**
- `model.IgnoreAzureIdCaseChanges.Should().BeTrue()`

---

#### TC-13: Identical values (ordinal equal) — unchanged-value logic unchanged by the new flag

**Type:** Unit

**Maps to criterion:** "Flag absent → no regression"

**Description:**
Verifies that the Azure ID filter guard does NOT interfere with the existing `valuesEqual` guard. When before == after (ordinal), those rows are still subject to the existing `!_showUnchangedValues` filter exactly as before. The filter registry is only consulted when `_ignoreCaseChanges` is `true` AND `valuesEqual` is `false`.

**Test Method:** `Build_IgnoreAzureIdCaseChangesTrue_OrdinallyEqualValues_BehavesLikeUnchanged`

**Test Steps:**
1. Parse `azurerm-case-only-ids-plan.json`.
2. Create `ReportModelBuilder(showSensitive: false, showUnchangedValues: false, ignoreCaseChanges: true)`.
3. Call `Build(plan)`.
4. Check an attribute that is genuinely unchanged (before == after, ordinal).

**Expected Result:**
- The attribute with ordinal-equal before/after is absent from `AttributeChanges` (hidden by existing `valuesEqual` filter — unchanged by this feature).

---

#### TC-14: Scriban `ignore_azure_id_case_changes` variable is accessible to templates

**Type:** Unit

**Maps to criterion:** "Template authors can access the flag value to implement the filter in custom templates"

**Description:**
`AotScriptObjectMapper` must expose `ignore_azure_id_case_changes` as a Scriban variable when `IgnoreAzureIdCaseChanges = true`. This can be verified by creating a minimal Scriban template that accesses `ignore_azure_id_case_changes` and asserting the rendered output contains the expected value.

**Test Method:** `Render_IgnoreAzureIdCaseChangesTrue_ScribanVariableIsTrue`

**Test Steps:**
1. Parse any valid plan JSON.
2. Build model with `ignoreCaseChanges: true`.
3. Render with a custom inline Scriban template: `"{{ ignore_azure_id_case_changes }}"`.
4. Capture rendered output string.

**Expected Result:**
- Rendered output equals `"true"` (Scriban renders `true` as the string `"true"`).

**Note:** If a minimal renderer helper is not available, this can alternatively be verified by testing that `AotScriptObjectMapper` sets the expected key when mapping a `ReportModel` with `IgnoreAzureIdCaseChanges = true`. Use whichever approach best matches existing Scriban mapper test patterns.

---

### New file: `AzureResourceIdCaseChangeFilterTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/AzureResourceIdCaseChangeFilterTests.cs`

**Purpose:** Unit tests for `AzureResourceIdCaseChangeFilter` in complete isolation — no `ReportModelBuilder` involved. These tests verify the filter's own `ShouldSuppress()` logic directly.

---

#### TC-17: `ShouldSuppress` returns `true` for Azure ID casing-only change with azurerm provider

**Type:** Unit

**Maps to criterion:** "`AzureResourceIdCaseChangeFilter` suppresses Azure ID casing changes"

**Description:**
The core happy-path: an azurerm resource attribute whose before and after values are Azure resource IDs that differ only in casing. `ShouldSuppress()` must return `true`.

**Test Method:** `ShouldSuppress_AzureIdCasingOnlyChange_ReturnsTrue`

**Test Steps:**
1. Construct `AzureResourceIdCaseChangeFilter filter = new()`.
2. Construct `AttributeChangeFilterContext context = new(ProviderName: "registry.terraform.io/hashicorp/azurerm", AttributeName: "scope", BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg", AfterValue: "/subscriptions/abc123/resourceGroups/my-rg")`.
3. Call `filter.ShouldSuppress(context)`.

**Expected Result:**
- Returns `true`.

**Variant tests (same method style, parameterised):**
- Short provider name `"azurerm"` also returns `true`.
- `role_definition_id`: before `/providers/Microsoft.Authorization/roleDefinitions/XYZ`, after `/providers/Microsoft.Authorization/roleDefinitions/xyz` → returns `true`.

---

#### TC-18: `ShouldSuppress` returns `false` for non-Azure-ID string casing change

**Type:** Unit

**Maps to criterion:** "`AzureResourceIdCaseChangeFilter` does NOT suppress non-Azure-ID string changes"

**Description:**
When the attribute values are plain strings that are NOT Azure resource IDs (e.g., `"MyApp"` vs `"myapp"`), `AzureScopeParser.IsAzureResourceId()` returns `false` for both. `ShouldSuppress()` must return `false`.

**Test Method:** `ShouldSuppress_NonAzureIdStringCasingChange_ReturnsFalse`

**Test Steps:**
1. Construct `AzureResourceIdCaseChangeFilter filter = new()`.
2. Construct `AttributeChangeFilterContext context = new(ProviderName: "registry.terraform.io/hashicorp/azurerm", AttributeName: "display_name", BeforeValue: "MyApp", AfterValue: "myapp")`.
3. Call `filter.ShouldSuppress(context)`.

**Expected Result:**
- Returns `false`.

---

#### TC-19: `ShouldSuppress` returns `false` for non-azurerm provider

**Type:** Unit

**Maps to criterion:** "`AzureResourceIdCaseChangeFilter` does NOT suppress non-azurerm provider values"

**Description:**
When the provider name does not match the azurerm pattern (second guard: `AzureRmProviderPattern.IsMatch()`), `ShouldSuppress()` returns `false` immediately, regardless of the values.

**Test Method:** `ShouldSuppress_NonAzureRmProvider_ReturnsFalse`

**Test Steps:**
1. Construct `AzureResourceIdCaseChangeFilter filter = new()`.
2. Construct `AttributeChangeFilterContext context = new(ProviderName: "registry.terraform.io/hashicorp/aws", AttributeName: "arn", BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg", AfterValue: "/subscriptions/abc123/resourceGroups/my-rg")`.
3. Call `filter.ShouldSuppress(context)`.

**Expected Result:**
- Returns `false`.

**Variant:**
- `ProviderName: "azapi"` also returns `false`.

---

#### TC-20: `ShouldSuppress` returns `false` when `BeforeValue` is `null`

**Type:** Unit

**Maps to criterion:** "`AzureResourceIdCaseChangeFilter` does NOT suppress when BeforeValue is null"

**Description:**
`ShouldSuppress()` has an explicit null guard as its first check. When `BeforeValue` is `null`, it returns `false` without further evaluation.

**Test Method:** `ShouldSuppress_NullBeforeValue_ReturnsFalse`

**Test Steps:**
1. Construct `AzureResourceIdCaseChangeFilter filter = new()`.
2. Construct `AttributeChangeFilterContext context = new(ProviderName: "azurerm", AttributeName: "scope", BeforeValue: null, AfterValue: "/subscriptions/abc123/resourceGroups/my-rg")`.
3. Call `filter.ShouldSuppress(context)`.

**Expected Result:**
- Returns `false`.

---

#### TC-21: `ShouldSuppress` returns `false` when `AfterValue` is `null`

**Type:** Unit

**Maps to criterion:** "`AzureResourceIdCaseChangeFilter` does NOT suppress when AfterValue is null"

**Description:**
`ShouldSuppress()` has an explicit null guard as its first check. When `AfterValue` is `null`, it returns `false` without further evaluation.

**Test Method:** `ShouldSuppress_NullAfterValue_ReturnsFalse`

**Test Steps:**
1. Construct `AzureResourceIdCaseChangeFilter filter = new()`.
2. Construct `AttributeChangeFilterContext context = new(ProviderName: "azurerm", AttributeName: "scope", BeforeValue: "/subscriptions/ABC123/resourceGroups/my-rg", AfterValue: null)`.
3. Call `filter.ShouldSuppress(context)`.

**Expected Result:**
- Returns `false`.

---

### New file: `AttributeChangeFilterRegistryTests.cs`

Location: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AttributeChangeFilterRegistryTests.cs`

**Purpose:** Unit tests for `AttributeChangeFilterRegistry` infrastructure — verifying the registry's dispatch behaviour independently of any concrete filter.

---

#### TC-22: Empty registry — `ShouldSuppress` returns `false`

**Type:** Unit

**Maps to criterion:** "`AttributeChangeFilterRegistry` empty → returns false"

**Description:**
A registry with no registered filters must return `false` for any context, ensuring the default (no filters active) never accidentally suppresses rows.

**Test Method:** `ShouldSuppress_EmptyRegistry_ReturnsFalse`

**Test Steps:**
1. Construct `AttributeChangeFilterRegistry registry = new()` (no filters registered).
2. Construct any `AttributeChangeFilterContext context`.
3. Call `registry.ShouldSuppress(context)`.

**Expected Result:**
- Returns `false`.

---

#### TC-23: Registry returns `true` when at least one registered filter returns `true`

**Type:** Unit

**Maps to criterion:** "`AttributeChangeFilterRegistry` returns true when any filter returns true"

**Description:**
The registry uses OR semantics: if any registered filter's `ShouldSuppress()` returns `true`, the registry returns `true`. This is tested using simple stub/lambda implementations of `IAttributeChangeFilter`.

**Test Method:** `ShouldSuppress_OneFilterReturnsTrue_ReturnsTrue`

**Test Steps:**
1. Construct `AttributeChangeFilterRegistry registry = new()`.
2. Register a stub filter that always returns `false`.
3. Register a stub filter that always returns `true`.
4. Call `registry.ShouldSuppress(anyContext)`.

**Expected Result:**
- Returns `true`.

---

#### TC-24: Registry returns `false` when all registered filters return `false`

**Type:** Unit

**Maps to criterion:** "`AttributeChangeFilterRegistry` returns false when all filters return false"

**Description:**
When all registered filters return `false`, the registry must also return `false`.

**Test Method:** `ShouldSuppress_AllFiltersReturnFalse_ReturnsFalse`

**Test Steps:**
1. Construct `AttributeChangeFilterRegistry registry = new()`.
2. Register two stub filters that always return `false`.
3. Call `registry.ShouldSuppress(anyContext)`.

**Expected Result:**
- Returns `false`.

---

## Test Data Requirements

### Updated file: `azurerm-case-only-ids-plan.json`

**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-case-only-ids-plan.json`

**Description:**
A minimal Terraform plan JSON exercising all integration test scenarios above. Must include:

| Resource | Provider | Purpose | Attributes Required |
|----------|----------|---------|---------------------|
| `azurerm_role_assignment.casing_only` | `registry.terraform.io/hashicorp/azurerm` | TC-01, TC-02 | `scope`: before `/subscriptions/ABC123/resourceGroups/my-rg`, after `/subscriptions/abc123/resourceGroups/my-rg`; `role_definition_id`: before `/providers/Microsoft.Authorization/roleDefinitions/XYZ`, after `/providers/Microsoft.Authorization/roleDefinitions/xyz` |
| `azurerm_role_assignment.mixed_changes` | `registry.terraform.io/hashicorp/azurerm` | TC-03, TC-07 | `scope` (casing-only Azure ID as above); `display_name`: before `"My App"`, after `"My Application"` (genuine non-ID change) |
| `azurerm_key_vault.null_before` | `registry.terraform.io/hashicorp/azurerm` | TC-04 | `tenant_id`: before `null`, after `"tenant-abc"` |
| `azurerm_key_vault.null_after` | `registry.terraform.io/hashicorp/azurerm` | TC-05 | `tenant_id`: before `"tenant-abc"`, after `null` |
| `azurerm_key_vault.numeric_change` | `registry.terraform.io/hashicorp/azurerm` | TC-06 | `soft_delete_retention_days`: before `7`, after `14` (numeric genuine change) |
| `azurerm_key_vault.unchanged` | `registry.terraform.io/hashicorp/azurerm` | TC-13 | `name`: before `"my-vault"`, after `"my-vault"` (ordinal equal, unchanged) |
| `azurerm_role_assignment.display_name_casing` | `registry.terraform.io/hashicorp/azurerm` | TC-15 | `display_name`: before `"MyApp"`, after `"myapp"` (casing-only but NOT an Azure ID — must NOT be suppressed) |
| `random_string.non_azurerm` | `registry.terraform.io/hashicorp/random` | TC-16 | `result`: before `/subscriptions/ABC123/resourceGroups/my-rg`, after `/subscriptions/abc123/resourceGroups/my-rg` (Azure-ID-shaped but non-azurerm provider — must NOT be suppressed) |

The plan must use action `["update"]` for all resources so they appear in the changes list.

**Note:** TC-17 through TC-24 (`AzureResourceIdCaseChangeFilterTests.cs` and `AttributeChangeFilterRegistryTests.cs`) use inline values in the test code itself and do not require additional JSON test data files.

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| All Azure ID attribute changes are casing-only | Attribute table has 0 rows | TC-02 |
| Mix of Azure ID casing-only and genuine changes | Only genuine changes in attribute table | TC-03 |
| Non-Azure-ID string with casing-only change | Row NOT suppressed (not an Azure ID) | TC-15 |
| Non-azurerm provider with Azure ID-shaped values | Row NOT suppressed (provider check fails) | TC-16 |
| Null before value | Row not suppressed (null guard in filter) | TC-04, TC-20 |
| Null after value | Row not suppressed (null guard in filter) | TC-05, TC-21 |
| Numeric attribute change | Row not suppressed (not an Azure ID) | TC-06 |
| Flag + `--show-unchanged-values` combined | Azure ID casing-only rows still hidden | TC-07 |
| Flag absent (default) | No change in existing behavior | TC-01, TC-09 |
| Ordinal-equal values (`valuesEqual = true`) | Still hidden by existing unchanged filter (independent of new flag) | TC-13 |
| `ignore_azure_id_case_changes` Scriban variable | Set to `true` when flag is active | TC-14 |
| Empty filter registry | Returns false (no rows suppressed) | TC-22 |
| Fully-qualified azurerm provider path | Treated as azurerm (provider pattern matches `*/azurerm$`) | TC-17 |

---

## Non-Functional Tests

None required for this feature. The filter is a simple boolean guard in a tight loop — no performance or compatibility concerns beyond existing test infrastructure.

---

## Open Questions

None. The revised architecture document resolves all implementation ambiguities.
