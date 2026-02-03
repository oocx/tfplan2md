# Test Plan: Azure API Documentation Mapping

## Overview

This test plan validates the Azure API documentation mapping feature that replaces heuristic URL guessing with reliable, curated mappings from authoritative Azure sources. The feature introduces:

1. `AzureApiDocumentationMapper.cs` - Mapping loader with O(1) lookups
2. `AzureApiDocumentationMappings.json` - Embedded JSON with resource-to-URL mappings
3. Updated `AzApi.Resources.cs` - Uses mapper instead of heuristic
4. Update script - Generates mappings from official Azure sources

**Related Documents:**
- Feature Specification: `docs/features/048-azure-api-doc-mapping/specification.md`
- Architecture Design: `docs/features/048-azure-api-doc-mapping/architecture.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Mapping data structure exists with verified mappings | TC-01, TC-02 | Unit |
| Mapping data source is authoritative | TC-03, TC-19 | Integration |
| Lookup logic replaces heuristic URL construction | TC-04, TC-05, TC-06, TC-07 | Unit |
| Documentation links use mapping-based approach | TC-08, TC-09, TC-10 | Integration |
| Existing tests updated for mapping-based behavior | TC-11, TC-12 | Regression |
| Known resource types with mappings handled | TC-04, TC-08 | Unit/Integration |
| Unknown resource types without mappings handled | TC-05, TC-09 | Unit/Integration |
| Edge cases handled (malformed types, non-Microsoft) | TC-06, TC-07, TC-10 | Unit/Integration |
| Template no longer shows "(best-effort)" disclaimer | TC-13 | Integration |
| Update script exists for maintaining mappings | TC-14, TC-15, TC-16 | Integration |
| JSON loading and parsing works correctly | TC-01, TC-02 | Unit |
| Version stripping logic works correctly | TC-04 | Unit |
| Error handling for malformed JSON | TC-17 | Unit |
| Performance characteristics acceptable | TC-18 | Performance |
| Nested resources resolve individually | TC-20 | Unit |
| Template rendering shows/omits links correctly | TC-13, TC-21, TC-22 | Integration |

## Test Cases

### TC-01: JSON Mappings File Structure Valid

**Type:** Unit

**Description:**
Verify that the embedded `AzureApiDocumentationMappings.json` file has valid structure and can be parsed.

**Preconditions:**
- `AzureApiDocumentationMappings.json` exists in `src/Oocx.TfPlan2Md/Providers/AzApi/Data/`
- File is marked as embedded resource

**Test Steps:**
1. Load the JSON file from embedded resources
2. Deserialize into expected data structure
3. Verify `mappings` dictionary exists
4. Verify `metadata` section exists with required fields: `version`, `lastUpdated`, `source`

**Expected Result:**
- JSON parses successfully without exceptions
- Root object contains `mappings` and `metadata` properties
- All required metadata fields are present

**Test Data:**
Use the actual embedded `AzureApiDocumentationMappings.json` file.

---

### TC-02: Mappings Dictionary Contains Expected Entries

**Type:** Unit

**Description:**
Verify that the mappings dictionary contains at least a representative set of common Azure resource types.

**Preconditions:**
- JSON file loads successfully (TC-01)

**Test Steps:**
1. Load mappings dictionary from JSON
2. Check for presence of common resource types:
   - `Microsoft.Compute/virtualMachines`
   - `Microsoft.Storage/storageAccounts`
   - `Microsoft.Network/virtualNetworks`
   - `Microsoft.KeyVault/vaults`

**Expected Result:**
- Mappings dictionary contains expected resource types
- Each mapping has a valid `url` property with `https://learn.microsoft.com` prefix

**Test Data:**
Inline test with known resource types.

---

### TC-03: Mapping URLs Follow Expected Patterns

**Type:** Integration

**Description:**
Verify that documentation URLs in the mappings follow the expected Microsoft Learn URL structure.

**Preconditions:**
- Mappings loaded successfully (TC-01)

**Test Steps:**
1. Load all mappings from JSON
2. For each mapping URL:
   - Verify URL starts with `https://learn.microsoft.com/rest/api/`
   - Verify URL contains a service segment (e.g., `/compute/`, `/storagerp/`)
   - Verify URL does not contain obvious typos or malformed segments

**Expected Result:**
- All URLs follow `https://learn.microsoft.com/rest/api/{service}/{resource-type}` pattern
- No URLs contain placeholder text or obvious errors

**Test Data:**
All mappings in `AzureApiDocumentationMappings.json`.

---

### TC-04: GetDocumentationUrl Returns Correct URL for Known Resource Type

**Type:** Unit

**Description:**
Verify that `AzureApiDocumentationMapper.GetDocumentationUrl()` returns the correct URL for a known resource type.

**Preconditions:**
- Mapper initialized with mappings

**Test Steps:**
1. Call `GetDocumentationUrl("Microsoft.Compute/virtualMachines")`
2. Verify returned URL matches expected documentation URL

**Expected Result:**
- Returns non-null URL string
- URL matches the mapping in JSON for `Microsoft.Compute/virtualMachines`

**Test Data:**
```csharp
// Test with multiple known types
var testCases = new[]
{
    "Microsoft.Compute/virtualMachines",
    "Microsoft.Storage/storageAccounts",
    "Microsoft.Network/virtualNetworks"
};
```

---

### TC-05: GetDocumentationUrl Returns Null for Unknown Resource Type

**Type:** Unit

**Description:**
Verify that the mapper returns null when no mapping exists for a resource type.

**Preconditions:**
- Mapper initialized with mappings

**Test Steps:**
1. Call `GetDocumentationUrl("Microsoft.UnknownService/unknownResource")`
2. Verify result is null

**Expected Result:**
- Returns null (not empty string or exception)

**Test Data:**
```csharp
var unknownTypes = new[]
{
    "Microsoft.UnknownService/unknownResource",
    "Microsoft.FakeService/fakeResource@2023-01-01",
    null,
    "",
    "   "
};
```

---

### TC-06: GetDocumentationUrl Strips API Version Before Lookup

**Type:** Unit

**Description:**
Verify that API version suffixes (e.g., `@2023-03-01`) are stripped before looking up mappings.

**Preconditions:**
- Mapper initialized with mappings containing version-agnostic keys

**Test Steps:**
1. Call `GetDocumentationUrl("Microsoft.Compute/virtualMachines@2023-03-01")`
2. Verify returned URL matches mapping for `Microsoft.Compute/virtualMachines` (without version)

**Expected Result:**
- Version suffix is stripped before lookup
- Returns same URL as version-agnostic lookup

**Test Data:**
```csharp
var testCases = new[]
{
    ("Microsoft.Compute/virtualMachines@2023-03-01", "Microsoft.Compute/virtualMachines"),
    ("Microsoft.Storage/storageAccounts@2021-06-01", "Microsoft.Storage/storageAccounts"),
    ("Microsoft.Network/virtualNetworks@2020-11-01", "Microsoft.Network/virtualNetworks")
};
// Each tuple: (inputWithVersion, expectedLookupKey)
```

---

### TC-07: GetDocumentationUrl Handles Non-Microsoft Providers

**Type:** Unit

**Description:**
Verify that non-Microsoft resource providers return null (no mapping).

**Preconditions:**
- Mapper initialized with mappings (only Microsoft resources)

**Test Steps:**
1. Call `GetDocumentationUrl("HashiCorp.RandomProvider/randomString")`
2. Verify result is null

**Expected Result:**
- Returns null for non-Microsoft providers

**Test Data:**
```csharp
var nonMicrosoftProviders = new[]
{
    "HashiCorp.RandomProvider/randomString",
    "Custom.Provider/customResource",
    "Terraform.LocalProvider/localFile"
};
```

---

### TC-08: AzureApiDocLink Helper Uses Mapper for Known Resource Type

**Type:** Integration

**Description:**
Verify that the updated `AzureApiDocLink()` Scriban helper function uses the mapper and returns correct URLs.

**Preconditions:**
- Mapper initialized
- Scriban helpers registered

**Test Steps:**
1. Call `AzApiHelpers.AzureApiDocLink("Microsoft.Compute/virtualMachines@2023-03-01")`
2. Verify returned URL matches expected mapping

**Expected Result:**
- Returns mapped URL from `AzureApiDocumentationMapper`
- URL is correct for the resource type (version stripped)

**Test Data:**
Use test data from TC-04, TC-06.

---

### TC-09: AzureApiDocLink Helper Returns Null for Unknown Resource Type

**Type:** Integration

**Description:**
Verify that the helper returns null when no mapping exists.

**Preconditions:**
- Mapper initialized
- Scriban helpers registered

**Test Steps:**
1. Call `AzApiHelpers.AzureApiDocLink("Microsoft.UnknownService/unknownResource")`
2. Verify result is null

**Expected Result:**
- Returns null (not a heuristic URL)

**Test Data:**
Use test data from TC-05.

---

### TC-10: AzureApiDocLink Helper Handles Edge Cases

**Type:** Integration

**Description:**
Verify that the helper handles malformed or invalid inputs gracefully.

**Preconditions:**
- Mapper initialized

**Test Steps:**
1. Call `AzureApiDocLink(null)`
2. Call `AzureApiDocLink("")`
3. Call `AzureApiDocLink("   ")`
4. Call `AzureApiDocLink("invalid-format")`

**Expected Result:**
- All calls return null without throwing exceptions

**Test Data:**
```csharp
var edgeCases = new[]
{
    null,
    "",
    "   ",
    "invalid-format",
    "no-slash-format",
    "@version-only",
    "Microsoft.Only"
};
```

---

### TC-11: Existing ScribanHelpersAzApiTests Updated

**Type:** Regression

**Description:**
Verify that existing tests in `ScribanHelpersAzApiTests.cs` are updated to reflect mapping-based behavior.

**Preconditions:**
- Test file exists: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/ScribanHelpersAzApiTests.cs`

**Test Steps:**
1. Review existing `AzureApiDocLink` tests (TC-14, TC-15 in that file)
2. Update assertions to expect mapped URLs or null (not heuristic URLs)
3. Run updated tests and verify they pass

**Expected Result:**
- Tests validate mapping-based behavior
- No tests rely on heuristic URL construction

**Test Data:**
Existing test data in `ScribanHelpersAzApiTests.cs`.

---

### TC-12: Template Rendering with Mapped Resources Shows Correct URLs

**Type:** Integration

**Description:**
Verify that the Scriban template renders documentation links correctly for resources with mappings.

**Preconditions:**
- Test plan file exists with azapi_resource changes
- Resource type has a mapping

**Test Steps:**
1. Create test plan JSON with azapi_resource of type `Microsoft.Compute/virtualMachines@2023-03-01`
2. Render markdown using `MarkdownRenderer`
3. Verify rendered output contains `📚 [View API Documentation](https://learn.microsoft.com/rest/api/compute/virtual-machines)`

**Expected Result:**
- Documentation link appears in rendered markdown
- URL is correct (from mapping)
- No "(best-effort)" disclaimer present

**Test Data:**
```json
{
  "resource_changes": [
    {
      "address": "azapi_resource.vm",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.Compute/virtualMachines@2023-03-01",
          "body": "{...}"
        }
      }
    }
  ]
}
```

---

### TC-13: Template Rendering with Unmapped Resources Omits Link

**Type:** Integration

**Description:**
Verify that the template omits the documentation link when no mapping exists.

**Preconditions:**
- Test plan file exists with azapi_resource changes
- Resource type has no mapping

**Test Steps:**
1. Create test plan JSON with azapi_resource of type `Microsoft.UnknownService/unknownResource@2023-01-01`
2. Render markdown using `MarkdownRenderer`
3. Verify rendered output does NOT contain `📚 [View API Documentation]`
4. Verify resource type is still displayed: `**Type:** \`Microsoft.UnknownService/unknownResource@2023-01-01\``

**Expected Result:**
- No documentation link in rendered output
- Resource type is displayed
- No error or placeholder text appears

**Test Data:**
```json
{
  "resource_changes": [
    {
      "address": "azapi_resource.unknown",
      "type": "azapi_resource",
      "change": {
        "actions": ["create"],
        "before": null,
        "after": {
          "type": "Microsoft.UnknownService/unknownResource@2023-01-01",
          "body": "{...}"
        }
      }
    }
  ]
}
```

---

### TC-14: Update Script Exists and is Executable

**Type:** Integration

**Description:**
Verify that the update script for generating mappings exists and can be executed.

**Preconditions:**
- Script file exists (e.g., `scripts/generate-azure-api-mappings.py` or similar)

**Test Steps:**
1. Check script file exists
2. Verify script has execute permissions (Unix) or is documented as runnable
3. Run script with `--help` or `--version` flag (if supported)

**Expected Result:**
- Script exists in `scripts/` directory
- Script documentation or help output is available
- Script can be executed without errors (preliminary check)

**Test Data:**
N/A (script existence check).

---

### TC-15: Update Script Generates Valid JSON

**Type:** Integration

**Description:**
Verify that running the update script produces valid JSON output matching the expected schema.

**Preconditions:**
- Update script exists (TC-14)

**Test Steps:**
1. Run update script to generate mappings
2. Parse generated JSON output
3. Verify structure matches expected schema (TC-01)
4. Verify generated URLs follow expected patterns (TC-03)

**Expected Result:**
- Script outputs valid JSON
- JSON structure matches `AzureApiDocumentationMappings.json` schema
- Generated URLs are well-formed

**Test Data:**
N/A (generated by script).

**Note:** This test may be manual or semi-automated depending on script design.

---

### TC-16: Update Script Coverage Metrics

**Type:** Integration

**Description:**
Verify that the update script generates mappings for a comprehensive set of Azure resource types.

**Preconditions:**
- Update script generates mappings (TC-15)

**Test Steps:**
1. Run update script
2. Count number of unique resource type mappings generated
3. Verify coverage includes common services:
   - Compute (VMs, Disks, etc.)
   - Storage (Storage Accounts, Blobs, etc.)
   - Network (VNets, Subnets, etc.)
   - KeyVault
   - Web (App Services)
   - Database (SQL, CosmosDB)

**Expected Result:**
- Script generates mappings for 100+ resource types (comprehensive coverage)
- Common Azure services are well-represented

**Test Data:**
N/A (generated by script).

**Note:** Exact coverage number depends on Azure REST API specs availability.

---

### TC-17: Mapper Handles Malformed JSON Gracefully

**Type:** Unit

**Description:**
Verify that the mapper fails fast with clear error messages when JSON is malformed.

**Preconditions:**
- Test with intentionally malformed JSON

**Test Steps:**
1. Create test JSON file with syntax errors (e.g., missing closing brace)
2. Attempt to load mappings
3. Verify appropriate exception is thrown

**Expected Result:**
- Throws `JsonException` or similar with descriptive message
- Application fails at startup (fail-fast behavior)

**Test Data:**
```json
// Malformed JSON examples
{ "mappings": { "incomplete": 
```

**Note:** This test validates the loading logic, not production usage (production file should always be valid).

---

### TC-18: Mapper Lookup Performance is O(1)

**Type:** Performance

**Description:**
Verify that lookups in the mapper are constant-time (O(1)) regardless of number of mappings.

**Preconditions:**
- Mapper initialized with full set of mappings

**Test Steps:**
1. Perform 1000 lookups for known resource types
2. Measure average lookup time
3. Verify lookup time is < 1ms on average

**Expected Result:**
- Lookup time is negligible (sub-millisecond)
- Performance is consistent across lookups
- Memory usage is acceptable (< 1 MB for 1000 mappings)

**Test Data:**
Use all resource types from `AzureApiDocumentationMappings.json`.

---

### TC-19: Mappings Derived from Authoritative Sources

**Type:** Integration (Manual Verification)

**Description:**
Verify that the mappings in the JSON file are derived from authoritative Azure sources.

**Preconditions:**
- Mappings file exists with metadata

**Test Steps:**
1. Review `metadata.source` field in JSON
2. Verify source is listed as authoritative (e.g., "Microsoft Learn REST API Documentation")
3. Spot-check 10-20 URLs by visiting them in a browser
4. Verify URLs point to actual Azure REST API documentation pages

**Expected Result:**
- Metadata indicates authoritative source
- Spot-checked URLs are valid and point to correct resource documentation
- No broken links (404 errors)

**Test Data:**
Sample URLs from various Azure services.

**Note:** This is partially a manual test; full URL validation is out of scope.

---

### TC-20: Nested Resource Types Resolve Individually

**Type:** Unit

**Description:**
Verify that nested resource types have individual mappings and don't fallback to parent resource URLs.

**Preconditions:**
- Mappings include nested resources (e.g., `Microsoft.Storage/storageAccounts/blobServices`)

**Test Steps:**
1. Call `GetDocumentationUrl("Microsoft.Storage/storageAccounts")`
2. Call `GetDocumentationUrl("Microsoft.Storage/storageAccounts/blobServices")`
3. Verify each returns a different, specific URL

**Expected Result:**
- Parent resource returns parent URL
- Child resource returns child URL (not inferred from parent)
- URLs are distinct

**Test Data:**
```csharp
var nestedTestCases = new[]
{
    ("Microsoft.Storage/storageAccounts", "https://learn.microsoft.com/rest/api/storagerp/storage-accounts"),
    ("Microsoft.Storage/storageAccounts/blobServices", "https://learn.microsoft.com/rest/api/storagerp/blob-services"),
    ("Microsoft.Storage/storageAccounts/blobServices/containers", "https://learn.microsoft.com/rest/api/storagerp/blob-containers")
};
```

---

### TC-21: Template No Longer Shows "(best-effort)" Disclaimer

**Type:** Integration

**Description:**
Verify that the rendered markdown does not contain "(best-effort)" text when documentation links are present.

**Preconditions:**
- Template updated to remove disclaimer (architecture decision #5)

**Test Steps:**
1. Render markdown for azapi_resource with known mapping
2. Search rendered output for text "(best-effort)"
3. Verify text is not present

**Expected Result:**
- Rendered output contains `📚 [View API Documentation](URL)` without "(best-effort)"
- Only resources with mappings show documentation links

**Test Data:**
Use test data from TC-12.

---

### TC-22: Template Rendering Snapshot Tests Updated

**Type:** Regression

**Description:**
Verify that markdown snapshot tests are updated to reflect new behavior.

**Preconditions:**
- Snapshot tests exist for azapi resources

**Test Steps:**
1. Run existing snapshot tests
2. Update snapshots to remove "(best-effort)" disclaimers
3. Update snapshots to reflect mapping-based URLs or omitted links
4. Re-run snapshot tests and verify they pass

**Expected Result:**
- All snapshot tests pass with updated baselines
- Snapshots reflect new behavior (no heuristic URLs, no best-effort disclaimers)

**Test Data:**
Existing snapshot test files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`.

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Null resource type | Returns null | TC-05, TC-10 |
| Empty string resource type | Returns null | TC-05, TC-10 |
| Whitespace-only resource type | Returns null | TC-05, TC-10 |
| Resource type with API version | Strips version, performs lookup | TC-06 |
| Non-Microsoft provider | Returns null | TC-07 |
| Malformed resource type | Returns null | TC-10 |
| Unknown resource type | Returns null | TC-05, TC-09 |
| Nested resource type | Resolves to specific URL (not parent) | TC-20 |
| Case variations in resource type | Handled via case-insensitive lookup | TC-04 (implicit) |
| Malformed JSON file | Throws exception at startup | TC-17 |
| Missing JSON file | Throws exception at startup | TC-17 |

## User Acceptance Scenarios

> **Purpose**: For this feature, UAT validates that documentation links render correctly in GitHub and Azure DevOps markdown, and that the links are functional and lead to correct Azure API documentation.

### Scenario 1: Mapped Resource Shows Documentation Link

**User Goal**: View official Azure API documentation for a known resource type.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR comments.

**Test Artifact**:
- **Artifact to use**: `artifacts/azapi-comprehensive-demo.md` (or create new artifact with azapi resources)
- **Source Plan**: Test plan with azapi_resource of type `Microsoft.Compute/virtualMachines@2023-03-01`

**Expected Output**:
- Rendered markdown contains: `📚 [View API Documentation](https://learn.microsoft.com/rest/api/compute/virtual-machines)`
- Link is clickable and styled as a hyperlink
- No "(best-effort)" disclaimer appears

**Success Criteria**:
- [ ] Documentation link renders correctly in GitHub markdown
- [ ] Documentation link renders correctly in Azure DevOps markdown
- [ ] Clicking link navigates to correct Azure REST API documentation page
- [ ] Page loads successfully (no 404 error)

**Feedback Opportunities**:
- Is the link text clear and actionable?
- Is the link placement intuitive?
- Does the documentation page help answer user questions about the resource type?

---

### Scenario 2: Unmapped Resource Omits Documentation Link

**User Goal**: Understand behavior when documentation link is not available for a resource type.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR comments.

**Test Artifact**:
- **Artifact to use**: Create new test artifact
- **Source Plan**: Test plan with azapi_resource of type `Microsoft.UnknownService/unknownResource@2023-01-01`
- **Command**: `tfplan2md < test-unmapped.json > artifacts/azapi-unmapped-test.md`

**Expected Output**:
- Resource type is displayed: `**Type:** \`Microsoft.UnknownService/unknownResource@2023-01-01\``
- No documentation link appears (no `📚 [View API Documentation]` line)
- No error message or placeholder text appears

**Success Criteria**:
- [ ] Output renders correctly in GitHub markdown (no broken formatting)
- [ ] Output renders correctly in Azure DevOps markdown
- [ ] Absence of link does not confuse or mislead users
- [ ] Resource type information is still useful without the link

**Feedback Opportunities**:
- Is the absence of a documentation link noticeable?
- Should there be an indicator explaining why no link is present?
- Does the user experience degrade significantly without the link?

---

### Scenario 3: Multiple Resources with Mixed Mappings

**User Goal**: Review a Terraform plan with multiple azapi resources, some with mappings and some without.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR comments.

**Test Artifact**:
- **Artifact to use**: Create new comprehensive artifact
- **Source Plan**: Test plan with:
  - `Microsoft.Compute/virtualMachines` (mapped)
  - `Microsoft.Storage/storageAccounts` (mapped)
  - `Microsoft.FakeService/fakeResource` (unmapped)
- **Command**: `tfplan2md < test-mixed.json > artifacts/azapi-mixed-mappings.md`

**Expected Output**:
- Mapped resources show documentation links with correct URLs
- Unmapped resources show type only (no link)
- Consistent formatting across all resources
- No "(best-effort)" disclaimers

**Success Criteria**:
- [ ] Mapped resources render correctly with links in GitHub
- [ ] Mapped resources render correctly with links in Azure DevOps
- [ ] Unmapped resources render correctly without links in both platforms
- [ ] Overall readability and consistency is maintained
- [ ] User can distinguish between mapped and unmapped resources easily

**Feedback Opportunities**:
- Is the mixed behavior (some links, some not) confusing?
- Should there be visual indicators to differentiate mapped vs. unmapped?
- Does the feature improve the review experience overall?

---

## Non-Functional Tests

### Performance Requirements

| Metric | Target | Test Case |
|--------|--------|-----------|
| Mapper initialization time | < 100ms | Measure time to load and freeze dictionary |
| Lookup latency | < 1ms | TC-18 |
| Memory footprint | < 1 MB for 1000 mappings | TC-18 |
| Rendering overhead | No measurable impact | Compare rendering time before/after feature |

### Error Handling

| Error Scenario | Expected Behavior | Test Case |
|----------------|-------------------|-----------|
| Malformed JSON | Fail at startup with exception | TC-17 |
| Missing JSON file | Fail at startup with exception | TC-17 |
| Null resource type input | Return null | TC-05, TC-10 |
| Invalid resource type format | Return null | TC-10 |

### Compatibility

| Aspect | Requirement | Test Case |
|--------|-------------|-----------|
| Backward compatibility | Existing templates still work | TC-11, TC-22 |
| Non-AzAPI functionality | No impact | TC-11 (regression tests) |
| Template syntax | No breaking changes | TC-12, TC-13 |

## Test Data Requirements

### New Test Data Files

1. **`azapi-mapped-resources.json`** - Plan with known resource types that have mappings:
   - `Microsoft.Compute/virtualMachines@2023-03-01`
   - `Microsoft.Storage/storageAccounts@2021-06-01`
   - `Microsoft.Network/virtualNetworks@2020-11-01`

2. **`azapi-unmapped-resources.json`** - Plan with unknown resource types without mappings:
   - `Microsoft.UnknownService/unknownResource@2023-01-01`
   - `Microsoft.FakeProvider/fakeResource@2022-12-01`

3. **`azapi-nested-resources.json`** - Plan with nested resource types:
   - `Microsoft.Storage/storageAccounts`
   - `Microsoft.Storage/storageAccounts/blobServices`
   - `Microsoft.Storage/storageAccounts/blobServices/containers`

4. **`azapi-version-variations.json`** - Same resource type with different API versions:
   - `Microsoft.Compute/virtualMachines@2023-03-01`
   - `Microsoft.Compute/virtualMachines@2022-11-01`
   - `Microsoft.Compute/virtualMachines@2021-07-01`

### Reuse Existing Test Data

- `azapi-create-plan.json` - Update expectations for TC-12
- `azapi-update-plan.json` - Update expectations for TC-13
- Existing test plans in `TestData/` - Update for regression tests (TC-11, TC-22)

## Open Questions

1. **URL Validation Scope**: Should the test plan include automated URL validation (HTTP requests to verify links are not broken)? Or is spot-checking sufficient?
   - **Recommendation**: Spot-checking is sufficient for initial implementation; full validation is out of scope due to network dependency and rate limiting.

2. **Case Sensitivity**: Should lookups be case-insensitive to handle variations in resource type casing?
   - **Architecture decision**: Use `StringComparer.OrdinalIgnoreCase` in `FrozenDictionary` for case-insensitive lookups.

3. **Test Data Volume**: How many mappings should be included in the initial JSON file for testing?
   - **Recommendation**: Include 20-30 common resource types for initial testing; comprehensive coverage comes from update script.

4. **Update Script Testing**: Should the update script have automated tests, or is manual execution sufficient?
   - **Recommendation**: Manual execution is sufficient; script output validation (TC-15, TC-16) ensures correctness.

## Definition of Done

- [ ] All unit tests (TC-01 to TC-07, TC-17, TC-18, TC-20) pass
- [ ] All integration tests (TC-03, TC-08 to TC-10, TC-12 to TC-16, TC-19, TC-21) pass
- [ ] All regression tests (TC-11, TC-22) pass
- [ ] UAT scenarios validated in GitHub and Azure DevOps PRs
- [ ] Test data files created and committed
- [ ] Snapshot tests updated with new baselines
- [ ] Performance tests meet target metrics
- [ ] All edge cases covered and passing
- [ ] Maintainer has approved test plan
- [ ] Tests run via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` without human intervention
