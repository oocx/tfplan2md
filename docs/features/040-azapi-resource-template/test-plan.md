# Test Plan: Custom Template for azapi_resource

## Overview

This test plan defines the testing strategy for the custom Scriban template for `azapi_resource` resources from the `azapi` Terraform provider. The feature transforms JSON body content into human-readable markdown, making it easy for developers to review azapi resources with confidence.

**Related Documents:**
- Feature Specification: `docs/features/040-azapi-resource-template/specification.md`
- Architecture Design: `docs/features/040-azapi-resource-template/architecture.md`

**Testing Framework:** TUnit 1.9.26

**Test Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Custom template for azapi_resource exists | TC-01 | Unit |
| Standard attributes displayed clearly | TC-02, TC-03, TC-04 | Unit, Integration |
| JSON body transformed to readable format | TC-05, TC-06, TC-07, TC-08, TC-09 | Unit |
| Change detection for updates | TC-10, TC-11, TC-12, TC-13 | Unit |
| Documentation links generated | TC-14, TC-15, TC-16 | Unit |
| Resource summary line includes context | TC-17, TC-18, TC-19, TC-20 | Integration |
| Template follows report style guide | TC-21, TC-22 | Integration |
| Markdown validation passes | TC-23 | Integration |
| Handles all operation types | TC-24, TC-25, TC-26, TC-27 | Integration |
| Complex nested JSON handled gracefully | TC-28, TC-29, TC-30 | Unit, Integration |
| Long values handled appropriately | TC-31, TC-32, TC-33 | Unit |
| Sensitive values masked correctly | TC-34, TC-35, TC-36, TC-37 | Unit |
| Large body properties collapsed | TC-38, TC-39 | Integration |

## User Acceptance Scenarios

> **Purpose**: For user-facing features (especially rendering changes), define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: Create Automation Account with Basic Configuration

**User Goal**: Review a new Azure Automation Account created via azapi_resource to verify the body configuration is easy to understand.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- Resource type shown clearly: `Microsoft.Automation/automationAccounts@2021-06-22`
- Documentation link displayed with "best-effort" disclaimer
- Standard attributes table shows name, parent_id (resource group), location
- Body table shows flattened properties:
  - `properties.disableLocalAuth` → ✅ `true`
  - `properties.publicNetworkAccess` → ❌ `false`
  - `properties.sku.name` → `Basic`
- Inline code formatting for values
- Boolean values have checkmark/cross icons

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Information is accurate and complete
- [ ] Body configuration is immediately understandable without parsing JSON
- [ ] Property paths use dot notation (e.g., `properties.sku.name`)

**Feedback Opportunities**:
- Is the flattened table format easy to scan?
- Are property paths clear and unambiguous?
- Is the documentation link helpful?
- Should any values be formatted differently?

---

### Scenario 2: Update Automation Account with Changed Properties

**User Goal**: Review changes to an existing azapi_resource to see exactly which body properties changed and what their before/after values are.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- Summary line shows: "🔄 azapi_resource myAccount — Microsoft.Automation/automationAccounts | 2 properties changed"
- Body Changes table shows only changed properties:
  - `properties.sku.name`: `Basic` → `Standard`
  - `properties.disableLocalAuth`: ❌ `false` → ✅ `true`
- Unchanged properties are NOT shown (unless `--show-unchanged-values` flag used)
- Before/After columns clearly distinguish old vs new values
- Large property changes moved to collapsible section

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Only changed properties are displayed (focused diff)
- [ ] Before and After values are clearly distinguishable
- [ ] Change is immediately understandable

**Feedback Opportunities**:
- Is it easy to identify what changed?
- Should unchanged properties be shown by default?
- Are before/after values easy to compare?
- Is the "2 properties changed" count helpful in the summary?

---

### Scenario 3: Complex Nested JSON with Arrays and Deep Nesting

**User Goal**: Review an azapi_resource with complex nested configuration including arrays and 5+ levels of nesting to verify it's readable and doesn't break the template.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- Deep nested properties flattened correctly (e.g., `properties.config.advanced.settings.option1`)
- Array elements shown with index notation (e.g., `properties.tags[0]`, `properties.tags[1]`)
- Large property values (>200 chars) moved to collapsible "Large body properties" section
- Small properties displayed in main table
- No template errors or rendering failures

**Success Criteria**:
- [ ] Output renders correctly in GitHub Markdown
- [ ] Output renders correctly in Azure DevOps Markdown
- [ ] Deep nesting doesn't break the table format
- [ ] Arrays are flattened with clear indexing
- [ ] Large values don't clutter the main view
- [ ] Template handles complexity without errors

**Feedback Opportunities**:
- Is deep nesting still understandable in flattened format?
- Are array representations clear?
- Is the large value threshold (200 chars) appropriate?
- Should arrays be summarized differently (e.g., `[3 items]`)?

---

### Scenario 4: Sensitive Values Masked and Unmasked

**User Goal**: Verify that sensitive properties in the body are masked by default and revealed with `--show-sensitive` flag.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description (two PRs: one masked, one unmasked).
- **Azure DevOps**: Verify rendering in Azure DevOps PR description (two PRs: one masked, one unmasked).

**Expected Output (Masked)**:
- Sensitive properties show `(sensitive)` instead of actual value
- Non-sensitive properties display normally
- Message indicates use of `--show-sensitive` flag to view

**Expected Output (Unmasked with --show-sensitive)**:
- All properties displayed including sensitive values
- No masking applied

**Success Criteria**:
- [ ] Masked output renders correctly in GitHub Markdown
- [ ] Masked output renders correctly in Azure DevOps Markdown
- [ ] Unmasked output renders correctly in GitHub Markdown
- [ ] Unmasked output renders correctly in Azure DevOps Markdown
- [ ] Per-property sensitivity is respected (not entire body masked)
- [ ] Flag behavior is clear to users

**Feedback Opportunities**:
- Is the `(sensitive)` marker clear enough?
- Should there be a visual indicator (icon) for sensitive values?
- Is per-property masking more useful than whole-body masking?

---

## Test Cases

### TC-01: Template File Exists and Resolves

**Type:** Unit

**Description:**
Verify that the template file `azapi/resource.sbn` exists in the Templates directory and is correctly resolved for `azapi_resource` resources.

**Preconditions:**
- Template file created at `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/azapi/resource.sbn`
- Template resolver supports `{provider}/{resource}.sbn` pattern

**Test Steps:**
1. Call `TemplateResolver.ResolveResourceTemplate("azapi", "azapi_resource")`
2. Verify the resolved template path matches `azapi/resource.sbn`
3. Verify the template file can be loaded without errors

**Expected Result:**
- Template resolver returns correct path for azapi_resource
- Template loads successfully
- No exceptions thrown

**Test Data:**
N/A (uses template resolver and file system)

---

### TC-02: ExtractAzapiMetadata - Complete Attributes

**Type:** Unit

**Description:**
Verify that `extract_azapi_metadata` helper extracts and formats all standard attributes (name, type, parent_id, location, tags) correctly.

**Preconditions:**
- `ExtractAzapiMetadata` Scriban helper implemented

**Test Steps:**
1. Create a mock `ResourceChangeModel` with all azapi_resource attributes populated
2. Call `ScribanHelpers.ExtractAzapiMetadata(change)`
3. Verify returned object contains: `name`, `type`, `parent_id`, `location`, `tags`
4. Verify values are correctly formatted (e.g., location has globe emoji, parent_id shows resource group name)

**Expected Result:**
- All attributes extracted correctly
- Formatting applied consistently (icons, inline code)
- No null reference exceptions

**Test Data:**
```json
{
  "type": "Microsoft.Automation/automationAccounts@2021-06-22",
  "name": "myAccount",
  "parent_id": "/subscriptions/.../resourceGroups/example-resources",
  "location": "westeurope",
  "tags": {"env": "prod", "team": "platform"}
}
```

---

### TC-03: ExtractAzapiMetadata - Missing Optional Attributes

**Type:** Unit

**Description:**
Verify that `extract_azapi_metadata` gracefully handles missing optional attributes (tags, location).

**Preconditions:**
- `ExtractAzapiMetadata` Scriban helper implemented

**Test Steps:**
1. Create a mock `ResourceChangeModel` with only required attributes (type, name)
2. Call `ScribanHelpers.ExtractAzapiMetadata(change)`
3. Verify returned object omits missing attributes or shows appropriate defaults

**Expected Result:**
- No exceptions thrown
- Missing attributes are null or omitted from returned object
- Template can handle missing attributes gracefully

**Test Data:**
```json
{
  "type": "Microsoft.Automation/automationAccounts@2021-06-22",
  "name": "myAccount"
}
```

---

### TC-04: ExtractAzapiMetadata - Parent ID Formatting

**Type:** Unit

**Description:**
Verify that `extract_azapi_metadata` formats `parent_id` as "Resource Group `{name}`" when it's a resource group scope.

**Preconditions:**
- `ExtractAzapiMetadata` Scriban helper implemented

**Test Steps:**
1. Create a mock `ResourceChangeModel` with resource group parent_id
2. Call `ScribanHelpers.ExtractAzapiMetadata(change)`
3. Verify `parent_id` is formatted as "Resource Group `example-resources`"

**Expected Result:**
- Parent ID displays as "Resource Group `{name}`" in inline code
- Uses existing Azure scope parsing logic

**Test Data:**
```json
{
  "parent_id": "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/example-resources"
}
```

---

### TC-05: FlattenJson - Simple Nested Object

**Type:** Unit

**Description:**
Verify that `flatten_json` helper correctly flattens a simple nested JSON object into dot-notation key-value pairs.

**Preconditions:**
- `FlattenJson` Scriban helper implemented

**Test Steps:**
1. Create a JSON object with 2-3 levels of nesting
2. Call `ScribanHelpers.FlattenJson(jsonObject)`
3. Verify returned list contains objects with `path` and `value` properties
4. Verify paths use dot notation (e.g., `properties.sku.name`)
5. Verify leaf values are correctly extracted

**Expected Result:**
- All nested properties flattened to dot notation
- Paths are correct and unambiguous
- Values are leaf values (not objects)
- Order is preserved or consistent

**Test Data:**
```json
{
  "properties": {
    "sku": {
      "name": "Basic"
    },
    "disableLocalAuth": true,
    "publicNetworkAccess": false
  }
}
```

**Expected Output:**
```
[
  { path: "properties.sku.name", value: "Basic", is_large: false },
  { path: "properties.disableLocalAuth", value: true, is_large: false },
  { path: "properties.publicNetworkAccess", value: false, is_large: false }
]
```

---

### TC-06: FlattenJson - Arrays with Elements

**Type:** Unit

**Description:**
Verify that `flatten_json` correctly handles arrays by using index notation (e.g., `tags[0]`, `tags[1]`).

**Preconditions:**
- `FlattenJson` Scriban helper implemented

**Test Steps:**
1. Create a JSON object with an array property
2. Call `ScribanHelpers.FlattenJson(jsonObject)`
3. Verify array elements are flattened with index notation
4. Verify nested objects within arrays are also flattened

**Expected Result:**
- Array elements shown as `property[0]`, `property[1]`, etc.
- Nested properties within array elements use notation like `tags[0].key`, `tags[1].value`

**Test Data:**
```json
{
  "tags": [
    {"key": "env", "value": "prod"},
    {"key": "team", "value": "platform"}
  ]
}
```

**Expected Output:**
```
[
  { path: "tags[0].key", value: "env", is_large: false },
  { path: "tags[0].value", value: "prod", is_large: false },
  { path: "tags[1].key", value: "team", is_large: false },
  { path: "tags[1].value", value: "platform", is_large: false }
]
```

---

### TC-07: FlattenJson - Deep Nesting (5+ Levels)

**Type:** Unit

**Description:**
Verify that `flatten_json` handles deeply nested JSON structures (5+ levels) without errors.

**Preconditions:**
- `FlattenJson` Scriban helper implemented

**Test Steps:**
1. Create a JSON object with 5+ levels of nesting
2. Call `ScribanHelpers.FlattenJson(jsonObject)`
3. Verify all properties are flattened correctly
4. Verify no stack overflow or performance issues

**Expected Result:**
- Deep paths correctly represented (e.g., `a.b.c.d.e.f`)
- No exceptions or errors
- Performance acceptable for typical use cases (<100ms)

**Test Data:**
```json
{
  "properties": {
    "config": {
      "advanced": {
        "settings": {
          "option1": {
            "enabled": true
          }
        }
      }
    }
  }
}
```

**Expected Output:**
```
[
  { path: "properties.config.advanced.settings.option1.enabled", value: true, is_large: false }
]
```

---

### TC-08: FlattenJson - Empty Objects and Null Values

**Type:** Unit

**Description:**
Verify that `flatten_json` correctly handles empty objects `{}`, null values, and missing properties.

**Preconditions:**
- `FlattenJson` Scriban helper implemented

**Test Steps:**
1. Create a JSON object with empty nested objects, null values, and mixed content
2. Call `ScribanHelpers.FlattenJson(jsonObject)`
3. Verify empty objects are omitted or shown as appropriate
4. Verify null values are included with `null` marker

**Expected Result:**
- Empty objects `{}` are omitted (no properties to flatten)
- Null values are included with path and `null` value
- No exceptions thrown

**Test Data:**
```json
{
  "properties": {
    "emptyObject": {},
    "nullValue": null,
    "validValue": "test"
  }
}
```

**Expected Output:**
```
[
  { path: "properties.nullValue", value: null, is_large: false },
  { path: "properties.validValue", value: "test", is_large: false }
]
```

---

### TC-09: FlattenJson - Large Value Detection

**Type:** Unit

**Description:**
Verify that `flatten_json` correctly marks properties as large when their serialized value exceeds 200 characters.

**Preconditions:**
- `FlattenJson` Scriban helper implemented
- Large value threshold = 200 characters

**Test Steps:**
1. Create a JSON object with one property having a value >200 chars and one <200 chars
2. Call `ScribanHelpers.FlattenJson(jsonObject)`
3. Verify large property has `is_large: true`
4. Verify small property has `is_large: false`

**Expected Result:**
- Properties >200 chars marked as `is_large: true`
- Properties ≤200 chars marked as `is_large: false`
- Threshold calculation uses serialized value length

**Test Data:**
```json
{
  "small": "short value",
  "large": "<string with 250+ characters...>"
}
```

---

### TC-10: CompareJsonProperties - Detects Added Properties

**Type:** Unit

**Description:**
Verify that `compare_json_properties` detects properties that exist only in `after` JSON (added properties).

**Preconditions:**
- `CompareJsonProperties` Scriban helper implemented

**Test Steps:**
1. Create `before` JSON with 2 properties
2. Create `after` JSON with 3 properties (2 from before + 1 new)
3. Call `ScribanHelpers.CompareJsonProperties(before, after, ...)`
4. Verify the new property is returned with `before: null` and `after: <value>`

**Expected Result:**
- Added property detected
- Before value shown as `(none)` or null
- After value shown correctly
- Change marked appropriately

**Test Data:**
```json
// before
{
  "properties": {
    "sku": { "name": "Basic" }
  }
}

// after
{
  "properties": {
    "sku": { "name": "Basic" },
    "disableLocalAuth": true
  }
}
```

**Expected Output:**
```
[
  { path: "properties.disableLocalAuth", before: null, after: true, is_changed: true }
]
```

---

### TC-11: CompareJsonProperties - Detects Removed Properties

**Type:** Unit

**Description:**
Verify that `compare_json_properties` detects properties that exist only in `before` JSON (removed properties).

**Preconditions:**
- `CompareJsonProperties` Scriban helper implemented

**Test Steps:**
1. Create `before` JSON with 3 properties
2. Create `after` JSON with 2 properties (1 removed)
3. Call `ScribanHelpers.CompareJsonProperties(before, after, ...)`
4. Verify the removed property is returned with `before: <value>` and `after: null`

**Expected Result:**
- Removed property detected
- Before value shown correctly
- After value shown as `(none)` or null

**Test Data:**
```json
// before
{
  "properties": {
    "sku": { "name": "Basic" },
    "disableLocalAuth": true
  }
}

// after
{
  "properties": {
    "sku": { "name": "Basic" }
  }
}
```

**Expected Output:**
```
[
  { path: "properties.disableLocalAuth", before: true, after: null, is_changed: true }
]
```

---

### TC-12: CompareJsonProperties - Detects Modified Properties

**Type:** Unit

**Description:**
Verify that `compare_json_properties` detects properties that have different values in `before` and `after` JSON.

**Preconditions:**
- `CompareJsonProperties` Scriban helper implemented

**Test Steps:**
1. Create `before` JSON with property set to value A
2. Create `after` JSON with same property set to value B
3. Call `ScribanHelpers.CompareJsonProperties(before, after, ...)`
4. Verify the property is returned with `before: A` and `after: B`

**Expected Result:**
- Modified property detected
- Both before and after values shown correctly
- Change marked appropriately

**Test Data:**
```json
// before
{
  "properties": {
    "sku": { "name": "Basic" }
  }
}

// after
{
  "properties": {
    "sku": { "name": "Standard" }
  }
}
```

**Expected Output:**
```
[
  { path: "properties.sku.name", before: "Basic", after: "Standard", is_changed: true }
]
```

---

### TC-13: CompareJsonProperties - Show Unchanged Values Flag

**Type:** Unit

**Description:**
Verify that `compare_json_properties` respects the `showUnchanged` flag to include or exclude unchanged properties.

**Preconditions:**
- `CompareJsonProperties` Scriban helper implemented

**Test Steps:**
1. Create `before` and `after` JSON with 2 unchanged properties and 1 changed property
2. Call `ScribanHelpers.CompareJsonProperties(before, after, ..., showUnchanged: false)`
3. Verify only changed property is returned
4. Call again with `showUnchanged: true`
5. Verify all 3 properties are returned

**Expected Result:**
- With `showUnchanged: false`, only changed properties returned
- With `showUnchanged: true`, all properties returned (changed and unchanged)
- Unchanged properties marked with `is_changed: false`

**Test Data:**
```json
// before
{
  "properties": {
    "sku": { "name": "Basic" },
    "location": "westeurope",
    "disableLocalAuth": false
  }
}

// after
{
  "properties": {
    "sku": { "name": "Standard" },
    "location": "westeurope",
    "disableLocalAuth": false
  }
}
```

---

### TC-14: AzureApiDocLink - Microsoft Resource Types

**Type:** Unit

**Description:**
Verify that `azure_api_doc_link` constructs best-effort documentation URLs for common Microsoft resource types.

**Preconditions:**
- `AzureApiDocLink` Scriban helper implemented

**Test Steps:**
1. Call `ScribanHelpers.AzureApiDocLink("Microsoft.Automation/automationAccounts@2021-06-22")`
2. Verify returned URL matches pattern: `https://learn.microsoft.com/rest/api/automation/automation-accounts/`
3. Test with 5+ different Microsoft resource types
4. Verify URL structure is consistent

**Expected Result:**
- URL follows pattern: `https://learn.microsoft.com/rest/api/{service}/{resource}/`
- Service name extracted and lowercased from `Microsoft.{Service}`
- Resource type converted to kebab-case
- API version not included in URL

**Test Data:**
| Resource Type | Expected URL |
|--------------|-------------|
| `Microsoft.Automation/automationAccounts@2021-06-22` | `https://learn.microsoft.com/rest/api/automation/automation-accounts/` |
| `Microsoft.Compute/virtualMachines@2023-03-01` | `https://learn.microsoft.com/rest/api/compute/virtual-machines/` |
| `Microsoft.Network/virtualNetworks@2022-01-01` | `https://learn.microsoft.com/rest/api/network/virtual-networks/` |

---

### TC-15: AzureApiDocLink - Non-Microsoft Resource Types

**Type:** Unit

**Description:**
Verify that `azure_api_doc_link` returns null for non-Microsoft resource types (custom providers, third-party).

**Preconditions:**
- `AzureApiDocLink` Scriban helper implemented

**Test Steps:**
1. Call `ScribanHelpers.AzureApiDocLink("CustomProvider/customResource@1.0.0")`
2. Verify returned value is null
3. Test with multiple non-Microsoft providers

**Expected Result:**
- Returns null for non-Microsoft providers
- No exceptions thrown
- Template handles null gracefully (omits link section)

**Test Data:**
- `CustomProvider/customResource@1.0.0`
- `ThirdParty.Provider/resource@2.0.0`

---

### TC-16: ParseAzureResourceType - Type Parsing

**Type:** Unit

**Description:**
Verify that `parse_azure_resource_type` correctly parses a resource type string into components.

**Preconditions:**
- `ParseAzureResourceType` Scriban helper implemented

**Test Steps:**
1. Call `ScribanHelpers.ParseAzureResourceType("Microsoft.Automation/automationAccounts@2021-06-22")`
2. Verify returned object contains: `provider`, `service`, `resource_type`, `api_version`
3. Verify each component is correctly extracted

**Expected Result:**
- Returns object with all components
- Components parsed correctly:
  - `provider`: `Microsoft.Automation`
  - `service`: `Automation`
  - `resource_type`: `automationAccounts`
  - `api_version`: `2021-06-22`

**Test Data:**
```
Input: "Microsoft.Automation/automationAccounts@2021-06-22"
Expected Output:
{
  provider: "Microsoft.Automation",
  service: "Automation",
  resource_type: "automationAccounts",
  api_version: "2021-06-22"
}
```

---

### TC-17: Template Rendering - Create Action Summary

**Type:** Integration

**Description:**
Verify that azapi_resource create operations render a clear summary line with resource name, type, resource group, and location.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Test data with azapi_resource create action

**Test Steps:**
1. Load test plan with azapi_resource create action
2. Render to markdown
3. Verify summary line contains: action icon (➕), resource name, resource type, resource group, location

**Expected Result:**
- Summary line format: `➕ azapi_resource myAccount — Microsoft.Automation/automationAccounts | example-resources | westeurope`
- All components present and correctly formatted
- Unicode icons display correctly

**Test Data:**
New test file: `azapi-create-plan.json`

---

### TC-18: Template Rendering - Update Action Summary

**Type:** Integration

**Description:**
Verify that azapi_resource update operations render a summary line indicating the number of changed properties.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Test data with azapi_resource update action

**Test Steps:**
1. Load test plan with azapi_resource update action (2 changed body properties)
2. Render to markdown
3. Verify summary line contains: action icon (🔄), resource name, resource type, change count

**Expected Result:**
- Summary line format: `🔄 azapi_resource myAccount — Microsoft.Automation/automationAccounts | 2 properties changed`
- Change count is accurate
- Update icon (🔄) displayed

**Test Data:**
New test file: `azapi-update-plan.json`

---

### TC-19: Template Rendering - Delete Action Summary

**Type:** Integration

**Description:**
Verify that azapi_resource delete operations render a summary line with resource name, type, and resource group.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Test data with azapi_resource delete action

**Test Steps:**
1. Load test plan with azapi_resource delete action
2. Render to markdown
3. Verify summary line contains: action icon (❌), resource name, resource type, resource group

**Expected Result:**
- Summary line format: `❌ azapi_resource myAccount — Microsoft.Automation/automationAccounts | example-resources`
- Delete icon (❌) displayed
- No location shown (not relevant for delete)

**Test Data:**
New test file: `azapi-delete-plan.json`

---

### TC-20: Template Rendering - Replace Action Summary

**Type:** Integration

**Description:**
Verify that azapi_resource replace operations render appropriately (as create + delete).

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Test data with azapi_resource replace action

**Test Steps:**
1. Load test plan with azapi_resource replace action
2. Render to markdown
3. Verify replace is handled (likely as create with special marker)

**Expected Result:**
- Replace action shows as create-before-destroy or similar
- Icon (♻️) or appropriate marker displayed
- Both before and after states shown if applicable

**Test Data:**
New test file: `azapi-replace-plan.json`

---

### TC-21: Template Rendering - Markdown Escaping

**Type:** Integration

**Description:**
Verify that special markdown characters in body property values are correctly escaped.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Test data with special characters in body values (pipes, asterisks, underscores, brackets)

**Test Steps:**
1. Load test plan with azapi_resource containing special characters in body values
2. Render to markdown
3. Verify all special characters are correctly escaped
4. Verify markdown is valid (no broken tables)

**Expected Result:**
- Pipes (`|`) escaped in table cells
- Asterisks (`*`), underscores (`_`), brackets (`[]`) handled correctly
- Newlines converted to `<br/>` or handled appropriately
- Markdown validation passes

**Test Data:**
New test file: `azapi-special-chars-plan.json` with properties like:
```json
{
  "body": {
    "properties": {
      "description": "This | has | pipes",
      "name": "test_with_underscores",
      "config": "line1\nline2"
    }
  }
}
```

---

### TC-22: Template Rendering - Style Guide Compliance

**Type:** Integration

**Description:**
Verify that the template follows the project's report style guide (icons, inline code, emoji, formatting).

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Test data with azapi_resource

**Test Steps:**
1. Render azapi_resource to markdown
2. Verify style elements:
   - Boolean `true` → ✅ with inline code
   - Boolean `false` → ❌ with inline code
   - Location has globe emoji 🌍
   - Resource type in inline code
   - Property paths in inline code in table cells
   - Documentation link has book emoji 📚

**Expected Result:**
- All style guide elements applied consistently
- Output matches formatting of other resource templates
- Visual consistency with existing templates (firewall, NSG, role assignment)

**Test Data:**
Reuse existing test data files

---

### TC-23: Markdown Validation - markdownlint Passes

**Type:** Integration

**Description:**
Verify that rendered markdown for azapi_resource passes markdownlint validation with project rules.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- markdownlint configuration in `.markdownlint.json`

**Test Steps:**
1. Render multiple azapi_resource scenarios to markdown
2. Run markdownlint on generated markdown
3. Verify no linting errors

**Expected Result:**
- All generated markdown passes markdownlint
- No MD012 (consecutive blank lines) violations
- No table formatting issues
- No heading spacing issues

**Test Data:**
All azapi test plans

---

### TC-24: Integration - Create Operation Full Rendering

**Type:** Integration

**Description:**
Verify complete end-to-end rendering of azapi_resource create operation including all sections.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- All Scriban helpers implemented

**Test Steps:**
1. Load test plan with azapi_resource create
2. Build report model
3. Render to markdown
4. Verify all sections present:
   - Collapsible details
   - Resource type with API version
   - Documentation link (best-effort)
   - Standard attributes table
   - Body section
   - Property table with values

**Expected Result:**
- Complete markdown output generated
- All sections present and correctly formatted
- Output is valid markdown
- No template errors

**Test Data:**
New test file: `azapi-create-complete-plan.json`

---

### TC-25: Integration - Update Operation Full Rendering

**Type:** Integration

**Description:**
Verify complete end-to-end rendering of azapi_resource update operation with before/after comparison.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- All Scriban helpers implemented

**Test Steps:**
1. Load test plan with azapi_resource update (multiple changed properties)
2. Build report model
3. Render to markdown
4. Verify sections:
   - Resource type
   - Documentation link
   - Standard attributes
   - Body Changes section (not "Body")
   - Before/After columns in table

**Expected Result:**
- Update-specific rendering with before/after comparison
- Only changed properties shown (unless --show-unchanged-values)
- Before and after columns present
- Output is valid markdown

**Test Data:**
New test file: `azapi-update-complete-plan.json`

---

### TC-26: Integration - Delete Operation Full Rendering

**Type:** Integration

**Description:**
Verify complete end-to-end rendering of azapi_resource delete operation.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- All Scriban helpers implemented

**Test Steps:**
1. Load test plan with azapi_resource delete
2. Build report model
3. Render to markdown
4. Verify sections:
   - Delete summary
   - Resource type
   - Documentation link (if shown for deletes)
   - Standard attributes (before state)
   - Body (before state)

**Expected Result:**
- Delete operation shows before state
- Appropriate sections present
- Output is valid markdown

**Test Data:**
New test file: `azapi-delete-complete-plan.json`

---

### TC-27: Integration - Replace Operation Full Rendering

**Type:** Integration

**Description:**
Verify complete end-to-end rendering of azapi_resource replace operation.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- All Scriban helpers implemented

**Test Steps:**
1. Load test plan with azapi_resource replace (create_before_destroy)
2. Build report model
3. Render to markdown
4. Verify replace is handled (may be similar to create or show both states)

**Expected Result:**
- Replace operation handled without errors
- Appropriate state(s) shown
- Output is valid markdown

**Test Data:**
New test file: `azapi-replace-complete-plan.json`

---

### TC-28: Edge Case - Empty Body

**Type:** Integration

**Description:**
Verify that azapi_resource with an empty body (`{}` or `null`) is handled gracefully.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan with azapi_resource where body is `{}`
2. Render to markdown
3. Verify no errors and appropriate message shown

**Expected Result:**
- No template errors
- Message like "Body: (empty)" or similar
- Standard attributes still shown

**Test Data:**
New test file: `azapi-empty-body-plan.json`

---

### TC-29: Edge Case - Body as String (Not Parsed JSON)

**Type:** Integration

**Description:**
Verify that if the body is a string (not parsed JSON object), it's handled gracefully (fallback to code block).

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan where body is a JSON string (not parsed object)
2. Render to markdown
3. Verify fallback behavior (e.g., show as code block)

**Expected Result:**
- No template errors
- Body shown in code block or as-is with note
- Template doesn't crash on unexpected data type

**Test Data:**
Mock data with body as string value

---

### TC-30: Edge Case - Mixed Types in Arrays

**Type:** Unit

**Description:**
Verify that `flatten_json` handles arrays with mixed types (strings, numbers, objects) correctly.

**Preconditions:**
- `FlattenJson` Scriban helper implemented

**Test Steps:**
1. Create JSON with array containing mixed types: `[1, "string", {"key": "value"}, null]`
2. Call `ScribanHelpers.FlattenJson(jsonObject)`
3. Verify all elements flattened with correct types

**Expected Result:**
- Each element flattened correctly
- Primitive types shown directly
- Objects flattened recursively
- Null values included

**Test Data:**
```json
{
  "mixedArray": [
    1,
    "string",
    {"key": "value"},
    null,
    true
  ]
}
```

---

### TC-31: Large Values - Single Large Property

**Type:** Integration

**Description:**
Verify that a single large property value (>200 chars) is moved to a collapsible "Large body properties" section.

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Large value threshold = 200 characters

**Test Steps:**
1. Load test plan with azapi_resource having one property >200 chars and others <200 chars
2. Render to markdown
3. Verify small properties in main table
4. Verify large property in collapsible section

**Expected Result:**
- Small properties displayed in main "Body" table
- Large property in separate `<details>` section titled "Large body properties"
- Large property shows full value in collapsible section

**Test Data:**
New test file: `azapi-large-value-plan.json`

---

### TC-32: Large Values - Multiple Large Properties

**Type:** Integration

**Description:**
Verify that multiple large properties are all moved to the collapsible "Large body properties" section.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan with azapi_resource having 3 large properties (all >200 chars)
2. Render to markdown
3. Verify all 3 large properties in collapsible section

**Expected Result:**
- All large properties grouped together in collapsible section
- Each large property shown with its path as a heading
- Values displayed appropriately (code block or formatted)

**Test Data:**
New test file: `azapi-multiple-large-values-plan.json`

---

### TC-33: Large Values - Update with Large Values

**Type:** Integration

**Description:**
Verify that update operations with large property changes handle before/after large values correctly.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan with azapi_resource update where changed property has large before/after values
2. Render to markdown
3. Verify large property change in collapsible "Large body property changes" section

**Expected Result:**
- Small property changes in main "Body Changes" table
- Large property changes in separate collapsible section
- Before and after values shown for large properties (may use existing `format_large_value` helper)

**Test Data:**
New test file: `azapi-update-large-values-plan.json`

---

### TC-34: Sensitive Values - Per-Property Masking

**Type:** Unit

**Description:**
Verify that `compare_json_properties` respects per-property sensitivity from Terraform's `before_sensitive` and `after_sensitive` structures.

**Preconditions:**
- `CompareJsonProperties` Scriban helper implemented

**Test Steps:**
1. Create `before` and `after` JSON with nested body properties
2. Create `before_sensitive` and `after_sensitive` structures marking one property as sensitive
3. Call `ScribanHelpers.CompareJsonProperties(before, after, beforeSensitive, afterSensitive, showUnchanged: false, showSensitive: false)`
4. Verify sensitive property is marked `is_sensitive: true`
5. Verify non-sensitive properties are marked `is_sensitive: false`

**Expected Result:**
- Sensitive property correctly identified from sensitivity structure
- Only marked properties are flagged as sensitive
- Template can use `is_sensitive` flag to mask values

**Test Data:**
```json
// after_sensitive
{
  "body": {
    "properties": {
      "administratorLoginPassword": true
    }
  }
}
```

---

### TC-35: Sensitive Values - Default Masking

**Type:** Integration

**Description:**
Verify that sensitive values in body are masked by default (without `--show-sensitive` flag).

**Preconditions:**
- Template `azapi/resource.sbn` implemented
- Sensitivity handling implemented

**Test Steps:**
1. Load test plan with azapi_resource containing sensitive body property
2. Build report model with `showSensitive: false`
3. Render to markdown
4. Verify sensitive property displays as `(sensitive)`

**Expected Result:**
- Sensitive property values masked
- Masked value shown as `(sensitive)` in table
- Non-sensitive properties display normally

**Test Data:**
New test file: `azapi-sensitive-plan.json`

---

### TC-36: Sensitive Values - Unmasked with Flag

**Type:** Integration

**Description:**
Verify that sensitive values are shown when `--show-sensitive` flag is used.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan with azapi_resource containing sensitive body property
2. Build report model with `showSensitive: true`
3. Render to markdown
4. Verify sensitive property displays actual value (not masked)

**Expected Result:**
- Sensitive values displayed (not masked)
- All properties shown normally

**Test Data:**
Reuse `azapi-sensitive-plan.json`

---

### TC-37: Sensitive Values - Entire Body Sensitive (Fallback)

**Type:** Integration

**Description:**
Verify that if the entire body is marked sensitive (not per-property), the whole body is masked with an appropriate message.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan where `before_sensitive.body` or `after_sensitive.body` is `true` (not an object)
2. Render to markdown
3. Verify entire body section shows masked message

**Expected Result:**
- Body/Changes section shows: `(sensitive - use --show-sensitive to view)`
- No individual properties displayed when body is entirely sensitive
- Standard attributes (name, type, location) still shown

**Test Data:**
New test file: `azapi-body-sensitive-plan.json`

---

### TC-38: Large Body - Entire Body Collapsed

**Type:** Integration

**Description:**
Verify that if all body properties are large (total body >2000 chars or all individual properties >200 chars), they are all grouped in the collapsible section.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan with azapi_resource where all body properties are large
2. Render to markdown
3. Verify no properties in main table (or empty main section)
4. Verify all properties in "Large body properties" collapsible section

**Expected Result:**
- Main table omitted or shows "(all properties are large)"
- Collapsible section contains all properties
- No duplication of data

**Test Data:**
New test file: `azapi-all-large-body-plan.json`

---

### TC-39: Large Body - Deep Nesting with Large Values

**Type:** Integration

**Description:**
Verify that deeply nested properties with large values are correctly flattened and moved to the large properties section.

**Preconditions:**
- Template `azapi/resource.sbn` implemented

**Test Steps:**
1. Load test plan with deeply nested body (5+ levels) where some nested properties are large
2. Render to markdown
3. Verify flattened paths are correct (e.g., `a.b.c.d.e.property`)
4. Verify large nested properties in collapsible section

**Expected Result:**
- Deep paths correctly flattened and escaped
- Large values correctly identified even when deeply nested
- No path truncation or errors

**Test Data:**
New test file: `azapi-deep-nested-large-plan.json`

---

## Test Data Requirements

The following new test data files need to be created in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`:

1. **`azapi-create-plan.json`** - Simple azapi_resource create with basic body configuration
2. **`azapi-update-plan.json`** - azapi_resource update with 2-3 changed body properties
3. **`azapi-delete-plan.json`** - azapi_resource delete
4. **`azapi-replace-plan.json`** - azapi_resource replace (create_before_destroy)
5. **`azapi-special-chars-plan.json`** - Body values with markdown special characters (pipes, asterisks, underscores, newlines, brackets)
6. **`azapi-create-complete-plan.json`** - Comprehensive create with all attributes populated
7. **`azapi-update-complete-plan.json`** - Comprehensive update with multiple changes
8. **`azapi-delete-complete-plan.json`** - Comprehensive delete
9. **`azapi-replace-complete-plan.json`** - Comprehensive replace
10. **`azapi-empty-body-plan.json`** - azapi_resource with empty body (`{}`)
11. **`azapi-large-value-plan.json`** - One large property (>200 chars) and several small properties
12. **`azapi-multiple-large-values-plan.json`** - Multiple large properties
13. **`azapi-update-large-values-plan.json`** - Update with large before/after values
14. **`azapi-sensitive-plan.json`** - Body with per-property sensitivity markers
15. **`azapi-body-sensitive-plan.json`** - Entire body marked sensitive
16. **`azapi-all-large-body-plan.json`** - All body properties are large
17. **`azapi-deep-nested-large-plan.json`** - Deep nesting (5+ levels) with large values
18. **`azapi-complex-nested-plan.json`** - Complex nested JSON with arrays and 5+ levels (for UAT Scenario 3)

Each test data file should follow the structure of existing test files (Terraform plan JSON format) and include:
- `terraform_version`
- `format_version`
- `resource_changes` array with azapi_resource entries
- Appropriate `before`, `after`, `before_sensitive`, `after_sensitive` structures

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Empty body (`{}`) | Show "(empty)" message, no properties table | TC-28 |
| Null body | Show "(empty)" or handle gracefully | TC-28 |
| Body as string (not JSON object) | Fallback to code block display | TC-29 |
| Non-Microsoft resource type | Omit documentation link | TC-15 |
| Missing optional attributes (location, tags) | Omit from standard attributes table | TC-03 |
| Large array (50+ items) | Flatten with index notation, apply large value threshold | TC-06, TC-30 |
| Mixed types in arrays | Flatten correctly with type-appropriate formatting | TC-30 |
| Deep nesting (8+ levels) | Flatten without errors, long paths handled | TC-07, TC-39 |
| Entire body marked sensitive | Mask entire body with message | TC-37 |
| Per-property sensitivity | Mask only marked properties | TC-34, TC-35 |
| All properties large | All in collapsible section, main table omitted | TC-38 |
| Update with no body changes | Show standard attributes only, no body changes section | Integration test needed |
| Unknown/computed values in body | Handle gracefully, show as "(unknown)" | Integration test needed |

## Non-Functional Tests

### Performance Requirements

**Test:** Template rendering performance for large azapi_resource bodies

**Requirement:** Rendering a single azapi_resource with 100+ body properties should complete in <100ms on typical hardware.

**Test Approach:**
1. Create test data with azapi_resource containing 100 flattened properties
2. Measure rendering time using `Stopwatch`
3. Assert rendering time < 100ms threshold

**Test Case:** `Performance_LargeAzapiBody_RendersUnder100ms`

---

### Error Handling

**Test:** Template robustness with malformed or unexpected data

**Requirement:** Template should never throw exceptions; instead, show error messages or fallback rendering.

**Test Approach:**
1. Provide malformed JSON body (invalid structure)
2. Attempt to render
3. Verify either graceful fallback or clear error message (no unhandled exceptions)

**Test Case:** `ErrorHandling_MalformedBody_DoesNotThrowException`

---

### Markdown Compatibility

**Test:** Rendered markdown displays correctly in GitHub and Azure DevOps

**Requirement:** All markdown output must render correctly in both GitHub and Azure DevOps markdown engines.

**Test Approach:**
1. User Acceptance Testing (UAT) with real PRs in GitHub and Azure DevOps
2. Visual verification by Maintainer
3. Validate inline HTML elements (`<details>`, `<summary>`) work in both platforms

**UAT Scenarios:** See User Acceptance Scenarios section

---

## Test Execution

### Unit Tests

Run all unit tests for Scriban helpers:

```bash
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/ScribanHelpersAzApiTests/*
```

### Integration Tests

Run all integration tests for azapi_resource template:

```bash
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/AzapiResourceTemplateTests/*
```

### Snapshot Tests

Run snapshot tests to verify markdown output matches baseline:

```bash
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/MarkdownSnapshotTests/Snapshot_AzapiResource*
```

### Markdown Validation

Run markdownlint validation on all azapi test outputs:

```bash
scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/MarkdownLintIntegrationTests/Lint_AzapiPlans*
```

### Full Test Suite

Run all tests:

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

## Open Questions

1. **Array Representation Strategy**: Should large arrays (e.g., 50+ items) be summarized as `[N items]` with a collapsible detail section instead of flattening all items? This could reduce noise for resources with large array properties.

2. **API Version in Documentation Link**: Should we attempt to include the API version in the documentation link, or always link to the latest version? Research needed on Microsoft Learn URL patterns.

3. **Change Count in Summary**: For update operations, should the summary count all changed properties (including nested), or only top-level changes? Specification says "2 properties changed" but clarification needed on nesting.

4. **Empty Array Representation**: How should empty arrays `[]` be represented? Omit entirely, show as `(empty)`, or show path with empty value?

5. **Documentation Link Disclaimer**: Should the "best-effort" disclaimer always be shown, or only for resource types that don't match the common pattern? May add noise if shown for all links.

6. **Null vs. Missing Properties**: Should there be a visual distinction between `null` values and properties that don't exist? Currently both treated similarly.

7. **Large Value Threshold**: Is 200 characters the right threshold for body properties, or should azapi_resource use a different threshold? Some Azure configurations may have long but readable values.

## Definition of Done

This test plan is complete when:

- [ ] All acceptance criteria have at least one mapped test case
- [ ] Edge cases and error scenarios are covered
- [ ] Test cases follow TUnit conventions and project patterns
- [ ] Test data requirements are clearly documented
- [ ] Non-functional requirements (performance, error handling) are defined
- [ ] User Acceptance Scenarios are documented for UAT Tester agent
- [ ] Test execution commands are provided
- [ ] Open questions are documented for Architect/Developer clarification
- [ ] The Maintainer has approved this test plan

## Next Steps

After Maintainer approval:

1. **Developer** to implement:
   - Scriban helper functions (`flatten_json`, `compare_json_properties`, `azure_api_doc_link`, `parse_azure_resource_type`, `extract_azapi_metadata`)
   - Template file `azapi/resource.sbn`
   - Helper registration in `ScribanHelpers.Registry.cs`
   - Unit tests for all helpers
   - Integration tests for template rendering
   - Test data files

2. **Developer** to create snapshot baselines:
   - Generate initial markdown output for all test scenarios
   - Review output with Maintainer
   - Commit approved snapshots as baseline

3. **UAT Tester** to validate:
   - Create UAT test plan (`uat-test-plan.md`) based on User Acceptance Scenarios
   - Run UAT with real PRs in GitHub and Azure DevOps
   - Report rendering issues or improvements

4. **Technical Writer** to document:
   - Add azapi_resource template to features documentation
   - Update README with examples
   - Document body representation strategy and design decisions
