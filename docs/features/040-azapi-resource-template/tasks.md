# Tasks: Custom Template for azapi_resource

## Overview

This feature implements a custom Scriban template for `azapi_resource` resources in the `azapi` Terraform provider. The template will transform the JSON `body` attribute into human-readable markdown tables using dot-notation property paths, making it easy for developers to review Azure API resource configurations in pull requests.

**Related Documents:**
- Feature Specification: `docs/features/040-azapi-resource-template/specification.md`
- Architecture Design: `docs/features/040-azapi-resource-template/architecture.md`
- Test Plan: `docs/features/040-azapi-resource-template/test-plan.md`
- UAT Test Plan: `docs/features/040-azapi-resource-template/uat-test-plan.md`

## Tasks

### Task 1: Create Test Data Files for Integration Tests

**Priority:** High

**Description:**
Create 18 JSON test plan files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` to support integration and snapshot tests. These files represent various scenarios for azapi_resource operations (create, update, delete, replace) with different complexity levels (simple, nested, large values, sensitive values).

**Acceptance Criteria:**
- [ ] All 18 test plan JSON files created in correct format (Terraform plan JSON structure)
- [ ] Each file includes `terraform_version`, `format_version`, and `resource_changes` array
- [ ] Files cover all required scenarios from test plan (TC-01 to TC-39)
- [ ] Test files are valid JSON and can be loaded without errors
- [ ] Files include appropriate `before_sensitive` and `after_sensitive` structures for sensitivity tests
- [ ] Large value test files include properties exceeding 200 character threshold
- [ ] Complex nested test files include 5+ levels of nesting and arrays

**Files to Create:**
1. `azapi-create-plan.json` - Simple create with basic body (TC-17, TC-24)
2. `azapi-update-plan.json` - Update with 2-3 changed properties (TC-18, TC-25)
3. `azapi-delete-plan.json` - Delete operation (TC-19, TC-26)
4. `azapi-replace-plan.json` - Replace operation (TC-20, TC-27)
5. `azapi-special-chars-plan.json` - Special markdown characters in values (TC-21)
6. `azapi-create-complete-plan.json` - Complete create with all attributes (TC-24)
7. `azapi-update-complete-plan.json` - Complete update with multiple changes (TC-25)
8. `azapi-delete-complete-plan.json` - Complete delete (TC-26)
9. `azapi-replace-complete-plan.json` - Complete replace (TC-27)
10. `azapi-empty-body-plan.json` - Empty body `{}` (TC-28)
11. `azapi-large-value-plan.json` - One large property >200 chars (TC-31)
12. `azapi-multiple-large-values-plan.json` - Multiple large properties (TC-32)
13. `azapi-update-large-values-plan.json` - Update with large values (TC-33)
14. `azapi-sensitive-plan.json` - Per-property sensitivity (TC-34, TC-35, TC-36)
15. `azapi-body-sensitive-plan.json` - Entire body marked sensitive (TC-37)
16. `azapi-all-large-body-plan.json` - All properties are large (TC-38)
17. `azapi-deep-nested-large-plan.json` - Deep nesting with large values (TC-39)
18. `azapi-complex-nested-plan.json` - Complex nesting with arrays (TC-28, TC-29, TC-30)

**Dependencies:** None

**Notes:**
- Follow existing test data file structure (e.g., `firewall-network-rule-collection-plan.json`)
- Resource type examples: `Microsoft.Automation/automationAccounts@2021-06-22`, `Microsoft.Web/sites@2022-03-01`
- Include realistic Azure resource configurations for body content
- Refer to UAT test plan for realistic body structures

**Estimated Complexity:** Medium (M)

---

### Task 2: Implement FlattenJson Scriban Helper

**Priority:** High

**Description:**
Implement the `FlattenJson` Scriban helper function that recursively flattens a JSON object into dot-notation key-value pairs. This is the core function for transforming the azapi_resource body into a scannable table format.

**Acceptance Criteria:**
- [ ] Function signature: `public static List<object> FlattenJson(object? jsonObject, string prefix = "")`
- [ ] Recursively traverses nested objects using dot notation (e.g., `properties.sku.name`)
- [ ] Handles arrays with index notation (e.g., `tags[0].key`, `tags[1].value`)
- [ ] Handles primitive types (string, number, boolean, null) as leaf values
- [ ] Marks properties as large (`is_large: true`) when serialized value length >200 characters
- [ ] Handles empty objects `{}` (omits from output)
- [ ] Handles null values (includes with `null` value)
- [ ] Handles deep nesting (5+ levels) without stack overflow or performance issues
- [ ] Handles mixed-type arrays (primitives, objects, nulls)
- [ ] Returns list of objects with properties: `path` (string), `value` (object), `is_large` (bool)
- [ ] Performance: <100ms for JSON with 100+ properties
- [ ] Unit tests pass for TC-05, TC-06, TC-07, TC-08, TC-09, TC-30

**Dependencies:** Task 1 (for test data)

**Notes:**
- Create new file: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.AzApi.cs`
- Use `System.Text.Json` for JSON parsing (consistent with codebase)
- Handle `JsonElement` types from parsed JSON
- Large value threshold: 200 characters (same as existing large value handling)
- Refer to existing helper patterns in `ScribanHelpers.Json.cs`

**Estimated Complexity:** Medium (M)

---

### Task 3: Implement CompareJsonProperties Scriban Helper

**Priority:** High

**Description:**
Implement the `CompareJsonProperties` Scriban helper function that compares before/after JSON objects and returns only changed properties with their before/after values. This enables focused diff views for update operations.

**Acceptance Criteria:**
- [ ] Function signature: `public static List<object> CompareJsonProperties(object? beforeJson, object? afterJson, object? beforeSensitive, object? afterSensitive, bool showUnchanged, bool showSensitive)`
- [ ] Flattens both before and after JSON using dot notation
- [ ] Compares flattened properties to detect changes
- [ ] Detects added properties (only in after)
- [ ] Detects removed properties (only in before)
- [ ] Detects modified properties (different values)
- [ ] Returns only changed properties by default (`showUnchanged: false`)
- [ ] Returns all properties when `showUnchanged: true`
- [ ] Navigates `before_sensitive` and `after_sensitive` structures to identify per-property sensitivity
- [ ] Marks sensitive properties (`is_sensitive: true`) based on sensitivity structures
- [ ] Applies large value detection (>200 chars) to before/after values
- [ ] Returns list of objects with properties: `path`, `before`, `after`, `is_large`, `is_sensitive`, `is_changed`
- [ ] Handles null/missing JSON gracefully
- [ ] Unit tests pass for TC-10, TC-11, TC-12, TC-13, TC-34

**Dependencies:** Task 2 (uses FlattenJson internally)

**Notes:**
- Reuse `FlattenJson` helper for flattening before/after JSON
- Sensitivity navigation may need recursive traversal similar to flattening
- Consider edge case: entire body marked sensitive (`before_sensitive.body: true`) vs per-property sensitivity
- Changed properties: before ≠ after (value comparison)
- Added properties: before is null, after has value
- Removed properties: before has value, after is null

**Estimated Complexity:** Large (L)

---

### Task 4: Implement ParseAzureResourceType Scriban Helper

**Priority:** Medium

**Description:**
Implement the `ParseAzureResourceType` Scriban helper that parses an Azure resource type string into its components (provider, service, resource type, API version). This enables clear display and documentation link generation.

**Acceptance Criteria:**
- [ ] Function signature: `public static object ParseAzureResourceType(string resourceType)`
- [ ] Parses format: `Microsoft.{Service}/{ResourceType}@{ApiVersion}`
- [ ] Returns object with properties: `provider`, `service`, `resource_type`, `api_version`
- [ ] Example: `Microsoft.Automation/automationAccounts@2021-06-22` → `{ provider: "Microsoft.Automation", service: "Automation", resource_type: "automationAccounts", api_version: "2021-06-22" }`
- [ ] Handles missing API version (no `@` separator)
- [ ] Handles non-Microsoft providers gracefully
- [ ] Unit tests pass for TC-16

**Dependencies:** None

**Notes:**
- Simple string parsing with `Split` operations
- Consider edge cases: missing `@`, no `/` separator, empty string
- Return null or empty object for invalid formats

**Estimated Complexity:** Small (S)

---

### Task 5: Implement AzureApiDocLink Scriban Helper

**Priority:** Medium

**Description:**
Implement the `AzureApiDocLink` Scriban helper that constructs a best-effort Azure REST API documentation URL from a resource type string. The link uses a heuristic pattern and may not always be accurate.

**Acceptance Criteria:**
- [ ] Function signature: `public static string? AzureApiDocLink(string resourceType)`
- [ ] Returns null for non-Microsoft resource types
- [ ] Constructs URL pattern: `https://learn.microsoft.com/rest/api/{service}/{resource}/`
- [ ] Extracts service name from `Microsoft.{Service}` (lowercase)
- [ ] Converts resource type to kebab-case (e.g., `automationAccounts` → `automation-accounts`)
- [ ] Example: `Microsoft.Automation/automationAccounts@2021-06-22` → `https://learn.microsoft.com/rest/api/automation/automation-accounts/`
- [ ] API version is NOT included in URL
- [ ] Returns null for invalid or custom resource types
- [ ] Unit tests pass for TC-14, TC-15

**Dependencies:** Task 4 (uses ParseAzureResourceType internally)

**Notes:**
- Best-effort approach: links may not always be correct (acceptable per architecture)
- Simple heuristic: lowercase service name + kebab-case resource type
- Consider reusing existing string formatting utilities
- Kebab-case conversion: split on capital letters, join with hyphens, lowercase

**Estimated Complexity:** Small (S)

---

### Task 6: Implement ExtractAzapiMetadata Scriban Helper

**Priority:** Medium

**Description:**
Implement the `ExtractAzapiMetadata` Scriban helper that extracts and formats key azapi_resource attributes (type, name, parent_id, location, tags) for display in the standard attributes table.

**Acceptance Criteria:**
- [ ] Function signature: `public static object ExtractAzapiMetadata(object change)`
- [ ] Extracts `type`, `name`, `parent_id`, `location`, `tags` from resource change model
- [ ] Returns object with formatted properties ready for template rendering
- [ ] Formats `location` with globe emoji: `🌍 {location}`
- [ ] Formats `parent_id` as "Resource Group `{name}`" when it's a resource group scope
- [ ] Formats `name` with inline code: `` `{name}` ``
- [ ] Handles missing optional attributes (returns null or omits from object)
- [ ] Reuses existing Azure scope parsing logic for `parent_id` formatting
- [ ] Unit tests pass for TC-02, TC-03, TC-04

**Dependencies:** None (may use existing Azure helper functions)

**Notes:**
- Refer to existing `ScribanHelpers.Azure.cs` for Azure scope parsing
- Use existing formatters: `WrapInlineCode`, `FormatLocationBadge` (if available)
- Handle both `before` and `after` states (likely extract from `after` for create, `before` for delete)
- Consider using existing `ReportModel` or `ResourceChangeModel` structure

**Estimated Complexity:** Small (S)

---

### Task 7: Register Scriban Helpers in Helper Registry

**Priority:** High

**Description:**
Register the five new Scriban helper functions in `ScribanHelpers.Registry.cs` so they are available in templates.

**Acceptance Criteria:**
- [ ] All five helpers registered with correct function signatures
- [ ] Function names match template usage: `flatten_json`, `compare_json_properties`, `azure_api_doc_link`, `parse_azure_resource_type`, `extract_azapi_metadata`
- [ ] Registration follows existing pattern using `scriptObject.Import(...)`
- [ ] Helpers are accessible in templates after registration
- [ ] No compilation errors
- [ ] Integration tests can use helpers in templates

**Dependencies:** Tasks 2, 3, 4, 5, 6 (all helpers implemented)

**Notes:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers.Registry.cs`
- Follow existing registration pattern (see how other helpers are registered)
- Use `Func<...>` delegates for registration
- Example: `scriptObject.Import("flatten_json", new Func<object?, string, List<object>>(FlattenJson));`

**Estimated Complexity:** Small (S)

---

### Task 8: Create azapi/resource.sbn Template File

**Priority:** High

**Description:**
Create the Scriban template file `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn` that renders azapi_resource resources using the new Scriban helpers. The template must handle all operation types (create, update, delete, replace) and follow the project's report style guide.

**Acceptance Criteria:**
- [ ] Template file created at correct path: `azapi/resource.sbn`
- [ ] Template uses collapsible `<details>` section with `change.summary_html`
- [ ] Displays resource type with API version in inline code
- [ ] Generates documentation link using `azure_api_doc_link` helper
- [ ] Shows standard attributes table (name, parent_id, location) using `extract_azapi_metadata`
- [ ] Displays tags badges (if present)
- [ ] For **create** operations: shows body configuration using `flatten_json`
- [ ] For **update** operations: shows body changes using `compare_json_properties`
- [ ] For **delete** operations: shows body configuration from before state
- [ ] For **replace** operations: handles as create with special indicator
- [ ] Separates small properties (main table) from large properties (collapsible section)
- [ ] Applies markdown escaping using `escape_markdown` helper
- [ ] Uses existing formatters: `format_attribute_value_table`, `format_large_value`
- [ ] Respects `show_unchanged_values` flag for updates
- [ ] Respects `show_sensitive` flag for sensitive values
- [ ] Respects `large_value_format` setting
- [ ] Follows project report style guide: icons (✅, ❌, 🌍, 📚), inline code, semantic formatting
- [ ] Template renders without errors for all test scenarios
- [ ] Integration tests pass for TC-17 to TC-27

**Dependencies:** Tasks 1, 2, 3, 4, 5, 6, 7 (helpers and test data)

**Notes:**
- Refer to existing templates: `azurerm/firewall_network_rule_collection.sbn`, `azurerm/network_security_group.sbn`
- Use Scriban syntax: `{{~ ~}}` for logic, `{{ }}` for output
- Template receives `change` object with `before_json`, `after_json`, `action`, etc.
- Consider template structure from architecture design (Appendix: Example Output)
- Large properties: use existing pattern with `<details>` section titled "Large body properties"

**Estimated Complexity:** Large (L)

---

### Task 9: Write Unit Tests for FlattenJson Helper

**Priority:** High

**Description:**
Create unit tests for the `FlattenJson` Scriban helper covering all specified test cases from the test plan.

**Acceptance Criteria:**
- [ ] Test class created: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ScribanHelpersAzApiTests.cs`
- [ ] Test methods follow TUnit conventions and project patterns
- [ ] All test cases implemented: TC-05, TC-06, TC-07, TC-08, TC-09, TC-30
- [ ] Tests verify correct dot-notation path generation
- [ ] Tests verify array index notation
- [ ] Tests verify deep nesting handling
- [ ] Tests verify empty object and null value handling
- [ ] Tests verify large value detection (is_large flag)
- [ ] Tests verify mixed-type array handling
- [ ] All tests pass
- [ ] Test data is clear and representative

**Dependencies:** Task 2 (FlattenJson implementation)

**Notes:**
- Use TUnit 1.9.26 framework (not xUnit or NUnit)
- Follow existing test patterns in the project
- Use `[Test]` attribute for test methods
- Use `Assert.That(...)` for assertions
- Test data can be inline or use helper methods to create test JSON

**Estimated Complexity:** Medium (M)

---

### Task 10: Write Unit Tests for CompareJsonProperties Helper

**Priority:** High

**Description:**
Create unit tests for the `CompareJsonProperties` Scriban helper covering all specified test cases from the test plan.

**Acceptance Criteria:**
- [ ] Test methods added to `ScribanHelpersAzApiTests.cs`
- [ ] All test cases implemented: TC-10, TC-11, TC-12, TC-13, TC-34
- [ ] Tests verify detection of added properties
- [ ] Tests verify detection of removed properties
- [ ] Tests verify detection of modified properties
- [ ] Tests verify `showUnchanged` flag behavior
- [ ] Tests verify per-property sensitivity detection
- [ ] Tests verify `is_sensitive` flag is set correctly
- [ ] All tests pass

**Dependencies:** Task 3 (CompareJsonProperties implementation)

**Notes:**
- Reuse test data creation patterns from FlattenJson tests
- Test both changed and unchanged scenarios
- Verify before/after values are correctly captured

**Estimated Complexity:** Medium (M)

---

### Task 11: Write Unit Tests for ParseAzureResourceType and AzureApiDocLink Helpers

**Priority:** Medium

**Description:**
Create unit tests for the `ParseAzureResourceType` and `AzureApiDocLink` Scriban helpers covering all specified test cases from the test plan.

**Acceptance Criteria:**
- [ ] Test methods added to `ScribanHelpersAzApiTests.cs`
- [ ] ParseAzureResourceType tests: TC-16
- [ ] AzureApiDocLink tests: TC-14, TC-15
- [ ] Tests verify correct parsing of resource type components
- [ ] Tests verify URL construction for Microsoft resource types
- [ ] Tests verify null return for non-Microsoft resource types
- [ ] Tests verify kebab-case conversion
- [ ] All tests pass

**Dependencies:** Tasks 4, 5 (helper implementations)

**Notes:**
- Test with various resource type strings
- Verify URL pattern correctness (even if best-effort)
- Test edge cases: missing API version, invalid format

**Estimated Complexity:** Small (S)

---

### Task 12: Write Unit Tests for ExtractAzapiMetadata Helper

**Priority:** Medium

**Description:**
Create unit tests for the `ExtractAzapiMetadata` Scriban helper covering all specified test cases from the test plan.

**Acceptance Criteria:**
- [ ] Test methods added to `ScribanHelpersAzApiTests.cs`
- [ ] All test cases implemented: TC-02, TC-03, TC-04
- [ ] Tests verify extraction of all standard attributes
- [ ] Tests verify formatting (emoji, inline code, resource group name)
- [ ] Tests verify handling of missing optional attributes
- [ ] Tests verify parent_id formatting for resource group scopes
- [ ] All tests pass

**Dependencies:** Task 6 (ExtractAzapiMetadata implementation)

**Notes:**
- Create mock `ResourceChangeModel` objects for testing
- Verify returned object structure and property values
- Test with complete and minimal attribute sets

**Estimated Complexity:** Small (S)

---

### Task 13: Write Integration Tests for Template Rendering

**Priority:** High

**Description:**
Create integration tests that verify end-to-end rendering of azapi_resource resources using the new template. These tests load test plan JSON files, build report models, render to markdown, and validate output.

**Acceptance Criteria:**
- [ ] Test class created: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AzapiResourceTemplateTests.cs`
- [ ] Integration tests for all operation types: TC-24, TC-25, TC-26, TC-27
- [ ] Tests verify complete rendering (all sections present)
- [ ] Tests verify correct template resolution (azapi/resource.sbn)
- [ ] Tests verify markdown structure is valid
- [ ] Tests verify collapsible sections render correctly
- [ ] Tests verify tables are properly formatted
- [ ] Tests verify sensitive value masking (TC-35, TC-36, TC-37)
- [ ] Tests verify large value handling (TC-31, TC-32, TC-33, TC-38, TC-39)
- [ ] Tests verify edge cases: empty body (TC-28), body as string (TC-29)
- [ ] All tests pass

**Dependencies:** Tasks 1, 7, 8 (test data, helpers, template)

**Notes:**
- Follow existing integration test patterns (e.g., firewall template tests)
- Load test plan JSON, build `ReportModel`, render using `MarkdownRenderer`
- Verify sections using string assertions or snapshot comparison
- Consider using snapshot tests for comprehensive validation

**Estimated Complexity:** Large (L)

---

### Task 14: Create Snapshot Tests for Markdown Output

**Priority:** High

**Description:**
Create snapshot tests that compare generated markdown output against baseline snapshots. This ensures markdown output remains consistent and any changes are intentional.

**Acceptance Criteria:**
- [ ] Snapshot test class created or extended: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownSnapshotTests.cs`
- [ ] Snapshot tests for all 18 test plan files
- [ ] Snapshot files stored in `src/tests/Oocx.TfPlan2Md.TUnit/Snapshots/` (or appropriate location)
- [ ] Tests verify complete markdown output matches snapshot
- [ ] Snapshots cover create, update, delete, replace operations
- [ ] Snapshots cover simple, complex, large, sensitive scenarios
- [ ] Initial snapshots reviewed and approved by Maintainer
- [ ] All snapshot tests pass
- [ ] Clear documentation on how to update snapshots (if needed)

**Dependencies:** Tasks 1, 8, 13 (test data, template, integration tests)

**Notes:**
- Use TUnit's snapshot testing capabilities (or a snapshot library if available)
- Generate initial snapshots after template implementation
- Maintainer must review and approve initial snapshots before committing
- Snapshot updates should be rare and intentional

**Estimated Complexity:** Medium (M)

---

### Task 15: Validate Markdown Output with markdownlint

**Priority:** Medium

**Description:**
Create tests that validate generated markdown output passes markdownlint validation with project rules. This ensures all generated markdown is compliant and renders correctly.

**Acceptance Criteria:**
- [ ] Test class created or extended: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/MarkdownLintIntegrationTests.cs`
- [ ] Tests validate markdown for all azapi test scenarios (TC-23)
- [ ] Tests use project's `.markdownlint.json` configuration
- [ ] Tests verify no linting errors (MD012, table formatting, heading spacing, etc.)
- [ ] Tests handle edge cases: special characters, inline HTML, nested lists
- [ ] All markdownlint tests pass

**Dependencies:** Tasks 1, 8, 13 (test data, template, rendered output)

**Notes:**
- Refer to existing markdownlint integration tests (if present)
- Use markdownlint CLI or library to validate generated markdown
- Consider running markdownlint as part of test execution or as a separate validation step

**Estimated Complexity:** Small (S)

---

### Task 16: Verify Template Resolution Integration

**Priority:** Medium

**Description:**
Verify that the `TemplateResolver` correctly resolves the `azapi/resource.sbn` template for azapi_resource resources without requiring code changes to the template resolution logic.

**Acceptance Criteria:**
- [ ] Test added to verify template resolution: TC-01
- [ ] `TemplateResolver.ResolveResourceTemplate("azapi", "azapi_resource")` returns correct path
- [ ] Template file loads without errors
- [ ] No changes required to `TemplateResolver` class (works with existing `{provider}/{resource}.sbn` pattern)
- [ ] Integration tests confirm template is used for azapi_resource rendering
- [ ] Test passes

**Dependencies:** Task 8 (template file creation)

**Notes:**
- File: `src/Oocx.TfPlan2Md/MarkdownGeneration/TemplateResolver.cs`
- Verify existing template resolution logic supports `azapi/resource.sbn` pattern
- No implementation changes should be needed (architecture states it works automatically)

**Estimated Complexity:** Small (S)

---

### Task 17: Handle Edge Cases and Error Scenarios

**Priority:** Medium

**Description:**
Implement robust error handling and edge case support in the template and helpers to ensure graceful degradation when unexpected data is encountered.

**Acceptance Criteria:**
- [ ] Template handles null or missing body gracefully (shows appropriate message)
- [ ] Template handles body as string (not parsed JSON) with fallback rendering
- [ ] Template handles entire body marked sensitive (shows masked message)
- [ ] Helpers handle null inputs without throwing exceptions
- [ ] Helpers handle malformed JSON gracefully
- [ ] No unhandled exceptions in template rendering
- [ ] Clear error messages or fallback rendering for edge cases
- [ ] Error handling tests pass

**Dependencies:** Tasks 2, 3, 4, 5, 6, 8 (all helpers and template)

**Notes:**
- Refer to edge cases documented in test plan
- Use defensive programming: null checks, try-catch where appropriate
- Follow existing error handling patterns in the codebase
- Consider adding dedicated error handling tests

**Estimated Complexity:** Medium (M)

---

### Task 18: Add XML Documentation Comments

**Priority:** High

**Description:**
Add comprehensive XML documentation comments to all new code (helper functions, template, test classes) following the project's commenting guidelines.

**Acceptance Criteria:**
- [ ] All public/internal class members have XML documentation comments
- [ ] All private members have XML documentation comments (per project standards)
- [ ] Comments include `<summary>`, `<param>`, `<returns>`, `<remarks>` tags as appropriate
- [ ] Comments explain "why" not just "what"
- [ ] Comments reference related features/specifications for traceability
- [ ] Comments follow guidelines in `docs/commenting-guidelines.md`
- [ ] No compiler warnings about missing documentation
- [ ] Maintainer approves comment quality during code review

**Dependencies:** All implementation tasks (Tasks 2-8, 17)

**Notes:**
- Reference: `docs/commenting-guidelines.md`
- Reference related feature: `docs/features/040-azapi-resource-template/specification.md`
- Include examples for complex methods using `<example>` and `<code>` tags
- Keep comments synchronized with code changes

**Estimated Complexity:** Small (S)

---

### Task 19: Create Example Plan Files for UAT

**Priority:** Low

**Description:**
Create three example Terraform plan JSON files in the `examples/` directory that will be used to generate UAT artifacts for Maintainer review on GitHub and Azure DevOps.

**Acceptance Criteria:**
- [ ] Three example plan files created in `examples/` directory:
  - `examples/azapi-create.json` - Simple create operation (Automation Account)
  - `examples/azapi-update.json` - Update operation with changed properties
  - `examples/azapi-complex.json` - Complex nested JSON (Web App)
- [ ] Files match structures defined in UAT test plan
- [ ] Files represent realistic Azure resource configurations
- [ ] Files can be processed by tfplan2md without errors
- [ ] Generated markdown is suitable for UAT review

**Dependencies:** Task 8 (template implementation)

**Notes:**
- Refer to UAT test plan for exact plan structures
- These are separate from test data files (different purpose: UAT vs automated tests)
- Files should be representative of real-world usage

**Estimated Complexity:** Small (S)

---

### Task 20: Performance Validation

**Priority:** Low

**Description:**
Validate that template rendering performance meets the requirement of <100ms for azapi_resource bodies with 100+ properties.

**Acceptance Criteria:**
- [ ] Performance test created: `Performance_LargeAzapiBody_RendersUnder100ms`
- [ ] Test creates azapi_resource with 100+ flattened body properties
- [ ] Test measures rendering time using `Stopwatch`
- [ ] Test asserts rendering time < 100ms threshold
- [ ] Performance test passes on typical hardware
- [ ] Performance is acceptable for real-world usage

**Dependencies:** Tasks 1, 8, 13 (test data, template, integration tests)

**Notes:**
- Create dedicated performance test or add to integration tests
- Consider running performance tests separately or marking appropriately
- Threshold: 100ms for single resource rendering

**Estimated Complexity:** Small (S)

---

## Implementation Order

The recommended sequence for implementation prioritizes foundational components (test data and helpers) before template development, and follows a test-first approach:

### Phase 1: Test Data and Infrastructure (Tasks 1)
1. **Task 1** - Create all 18 test data JSON files
   - **Reason:** Test data is required for all subsequent test development and enables test-first approach

### Phase 2: Core Helper Functions with Unit Tests (Tasks 2, 4, 9, 11)
2. **Task 2** - Implement FlattenJson helper
   - **Reason:** Core functionality for body flattening, used by other helpers
3. **Task 9** - Write unit tests for FlattenJson
   - **Reason:** Test-first approach, validates FlattenJson immediately
4. **Task 4** - Implement ParseAzureResourceType helper
   - **Reason:** Simple helper, no dependencies, used by AzureApiDocLink
5. **Task 11** - Write unit tests for ParseAzureResourceType and AzureApiDocLink
   - **Reason:** Test-first for remaining simple helpers

### Phase 3: Advanced Helper Functions with Unit Tests (Tasks 5, 6, 3, 10, 12)
6. **Task 5** - Implement AzureApiDocLink helper
   - **Reason:** Depends on ParseAzureResourceType
7. **Task 6** - Implement ExtractAzapiMetadata helper
   - **Reason:** Independent helper for metadata extraction
8. **Task 12** - Write unit tests for ExtractAzapiMetadata
   - **Reason:** Test immediately after implementation
9. **Task 3** - Implement CompareJsonProperties helper
   - **Reason:** Most complex helper, depends on FlattenJson, needed for template update logic
10. **Task 10** - Write unit tests for CompareJsonProperties
    - **Reason:** Test complex helper immediately

### Phase 4: Template Development (Tasks 7, 8, 17, 18)
11. **Task 7** - Register all Scriban helpers
    - **Reason:** Makes helpers available to templates
12. **Task 8** - Create azapi/resource.sbn template
    - **Reason:** Main template implementation, depends on all helpers
13. **Task 17** - Handle edge cases and error scenarios
    - **Reason:** Robust error handling after main implementation
14. **Task 18** - Add XML documentation comments
    - **Reason:** Document code after implementation is complete

### Phase 5: Integration and Validation (Tasks 13, 14, 15, 16, 20)
15. **Task 13** - Write integration tests for template rendering
    - **Reason:** End-to-end validation of template with all helpers
16. **Task 16** - Verify template resolution integration
    - **Reason:** Ensure template is correctly resolved by existing infrastructure
17. **Task 14** - Create snapshot tests
    - **Reason:** Baseline validation after integration tests pass
18. **Task 15** - Validate markdown with markdownlint
    - **Reason:** Ensure generated markdown is compliant
19. **Task 20** - Performance validation
    - **Reason:** Validate non-functional requirements

### Phase 6: UAT Preparation (Task 19)
20. **Task 19** - Create example plan files for UAT
    - **Reason:** Generate artifacts for UAT Tester after all implementation is complete

---

## Open Questions

1. **Array Summarization**: Should very large arrays (50+ items) be summarized as `[N items]` with collapsible details instead of flattening all items individually?
   - **Impact:** Tasks 2, 8
   - **Decision needed by:** Architect/Maintainer

2. **Empty Array Representation**: How should empty arrays `[]` be displayed? Options: omit entirely, show as `(empty)`, show path with empty value
   - **Impact:** Tasks 2, 8
   - **Decision needed by:** Architect/Maintainer

3. **Documentation Link Disclaimer**: Should the "best-effort" disclaimer always be shown, or only for resource types that don't match common patterns?
   - **Impact:** Task 8 (template text)
   - **Decision needed by:** Maintainer

4. **Change Count in Summary**: For update operations, should the summary count all changed properties (including deeply nested), or only top-level changes?
   - **Impact:** Task 8 (summary line generation)
   - **Decision needed by:** Architect

5. **Test Framework for Markdownlint**: What is the preferred approach for running markdownlint in tests? CLI invocation, library, or separate validation script?
   - **Impact:** Task 15
   - **Decision needed by:** Developer/Maintainer

---

## Definition of Done

This task plan is complete and implementation can begin when:

- [x] All tasks clearly defined with acceptance criteria
- [x] Dependencies between tasks are identified
- [x] Implementation order is prioritized
- [x] Test-first approach is followed (tests before implementation where appropriate)
- [x] Complexity estimates provided (S/M/L)
- [x] Open questions documented for clarification
- [x] Tasks map to test cases from test plan
- [x] All acceptance criteria are measurable and testable
- [x] Maintainer has reviewed and approved this task plan

---

## Post-Implementation Checklist

After all tasks are completed, verify:

- [ ] All 39 test cases from test plan pass
- [ ] All snapshot tests pass
- [ ] Markdown output passes markdownlint validation
- [ ] Performance requirements met (<100ms for 100+ properties)
- [ ] Documentation comments are complete and accurate
- [ ] Template follows project report style guide
- [ ] Edge cases are handled gracefully
- [ ] No compiler warnings
- [ ] Code review feedback addressed
- [ ] Ready for UAT (examples generated, artifacts ready)

---

## Notes for Developer

- **Test-First Approach**: Implement tests before or immediately after each helper/template component
- **Incremental Development**: Complete each task fully (including tests) before moving to the next
- **Code Review**: Request code review after Phase 2 and Phase 4 for early feedback
- **Snapshot Baseline**: Generate initial snapshots after Task 14 and request Maintainer review before committing
- **Performance**: Profile rendering if Task 20 fails; optimize FlattenJson or CompareJsonProperties as needed
- **Style Consistency**: Refer to existing templates (firewall, NSG, role assignment) for formatting patterns
- **Scriban Syntax**: Use `{{~ ~}}` for logic blocks (whitespace control), `{{ }}` for output
- **AOT Compatibility**: Ensure all helpers are AOT-compatible (no reflection)

---

## Handoff to Developer

This task plan is ready for implementation. The Developer should:

1. Review all tasks and acceptance criteria
2. Ask any clarifying questions about task scope or open questions
3. Begin implementation with Task 1 (test data creation)
4. Follow the implementation order specified above
5. Request code review after major milestones (Phase 2, Phase 4)
6. Notify UAT Tester when Task 19 is complete (example files ready)
7. Notify Technical Writer when all implementation is complete (for documentation updates)

**Estimated Total Effort:** 15-20 development hours across 20 tasks
