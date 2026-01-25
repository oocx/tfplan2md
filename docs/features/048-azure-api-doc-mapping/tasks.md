# Tasks: Azure API Documentation Mapping

## Overview

This feature replaces the heuristic Azure API documentation URL generation with reliable, curated mappings from authoritative Azure sources. The implementation introduces a mapper class that loads mappings from embedded JSON and provides O(1) lookups for resource types.

**Feature Specification:** `docs/features/048-azure-api-doc-mapping/specification.md`  
**Architecture Design:** `docs/features/048-azure-api-doc-mapping/architecture.md`  
**Test Plan:** `docs/features/048-azure-api-doc-mapping/test-plan.md`

## Tasks

### Task 1: Create Discovery Script for Mapping Generation

**Priority:** High

**Complexity:** Large (4-8 hours)

**Description:**
Create an automated script to generate comprehensive Azure API documentation mappings from official Azure sources. The script should discover resource types and generate documentation URLs following Microsoft Learn patterns.

**Acceptance Criteria:**
- [ ] Script exists at `scripts/generate-azure-api-mappings.py` (or equivalent language)
- [ ] Script can be executed manually by maintainer
- [ ] Script outputs valid JSON matching the required schema (mappings + metadata)
- [ ] Script generates mappings for 100+ Azure resource types
- [ ] Generated URLs follow pattern: `https://learn.microsoft.com/rest/api/{service}/{resource-type}`
- [ ] Script includes usage documentation (--help or inline comments)
- [ ] Script handles common Azure services: Compute, Storage, Network, KeyVault, Web, Database
- [ ] Output can be validated via spot-checking (no automated URL validation required)

**Dependencies:** None

**Files to Create:**
- `scripts/generate-azure-api-mappings.py` (or `.ps1`, `.sh`, `.cs`)
- Script documentation/README

**Notes:**
- Approach: Scrape Azure SDK Specs Inventory or parse azure-rest-api-specs repository structure
- Focus on comprehensive coverage over perfect accuracy (maintainer will spot-check)
- Use known patterns to construct URLs from resource types
- Reference architecture sections: "Discovery Script" and "Automation Level"
- Test cases: TC-14, TC-15, TC-16

---

### Task 2: Generate Initial Mappings JSON Data

**Priority:** High

**Complexity:** Small (1 hour)

**Description:**
Run the discovery script (from Task 1) to generate the initial `AzureApiDocumentationMappings.json` file with comprehensive mappings. Perform spot-checking to validate a sample of URLs.

**Acceptance Criteria:**
- [ ] File created: `src/Oocx.TfPlan2Md/Providers/AzApi/Data/AzureApiDocumentationMappings.json`
- [ ] JSON structure matches schema:
  ```json
  {
    "mappings": { "ResourceType": { "url": "..." } },
    "metadata": { "version": "1.0.0", "lastUpdated": "YYYY-MM-DD", "source": "..." }
  }
  ```
- [ ] Contains mappings for at least 20 common resource types initially
- [ ] Spot-check 10 URLs to verify they point to valid Azure REST API documentation
- [ ] File is marked as embedded resource in `.csproj`
- [ ] Metadata fields are populated correctly

**Dependencies:** Task 1 (discovery script must exist)

**Files to Create:**
- `src/Oocx.TfPlan2Md/Providers/AzApi/Data/AzureApiDocumentationMappings.json`

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj` (add embedded resource)

**Notes:**
- Start with 20-30 common types for initial testing, then expand with script
- Ensure resource type keys are version-agnostic (no `@YYYY-MM-DD`)
- Reference architecture section: "Data Model"
- Test cases: TC-01, TC-02, TC-03

---

### Task 3: Create Mapper Class with JSON Loading

**Priority:** High

**Complexity:** Medium (2-3 hours)

**Description:**
Implement `AzureApiDocumentationMapper` class that loads mappings from embedded JSON and provides O(1) lookup. Follow the pattern established by `AzureRoleDefinitionMapper`.

**Acceptance Criteria:**
- [ ] Class created: `src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.cs`
- [ ] Class follows `AzureRoleDefinitionMapper` pattern (partial class with static FrozenDictionary)
- [ ] `GetDocumentationUrl(string? resourceType)` method implemented
- [ ] Method strips API version suffix (`@YYYY-MM-DD`) before lookup
- [ ] Lookup uses case-insensitive comparison (`StringComparer.OrdinalIgnoreCase`)
- [ ] Returns `string?` (null if no mapping exists)
- [ ] Handles null/empty/whitespace input gracefully (returns null)
- [ ] Throws `InvalidOperationException` if JSON fails to load (fail-fast)
- [ ] XML documentation comments are comprehensive
- [ ] References feature specification in class comment: `docs/features/048-azure-api-doc-mapping/specification.md`

**Dependencies:** Task 2 (JSON file must exist)

**Files to Create:**
- `src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.cs`
- `src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMappingsJsonContext.cs` (if using source-generated JSON)

**Notes:**
- Follow architecture section: "Mapping Loader"
- Use `FrozenDictionary` for O(1) lookup performance
- Split into two partial classes if following role mapper pattern (main + loading logic)
- Test cases: TC-04, TC-05, TC-06, TC-07, TC-17

---

### Task 4: Update AzureApiDocLink Helper to Use Mapper

**Priority:** High

**Complexity:** Small (1 hour)

**Description:**
Replace the heuristic URL construction in `AzureApiDocLink()` with a call to the new mapper. Remove the kebab-case conversion logic as it's no longer needed.

**Acceptance Criteria:**
- [ ] `AzureApiDocLink()` calls `AzureApiDocumentationMapper.GetDocumentationUrl()`
- [ ] Heuristic URL construction logic removed (lines 104-121 in current implementation)
- [ ] Kebab-case utility function removed (lines 129-147) or marked as unused
- [ ] Method signature unchanged: `public static string? AzureApiDocLink(string? resourceType)`
- [ ] XML documentation updated to reflect mapping-based approach (remove "best-effort" language)
- [ ] Backward compatibility maintained (Scriban helper name unchanged)

**Dependencies:** Task 3 (mapper must exist)

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Resources.cs`

**Notes:**
- Old logic replaced with single line: `return AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);`
- `ParseAzureResourceType()` helper can remain unchanged (may be used elsewhere)
- Reference architecture section: "Scriban Helper Update"
- Test cases: TC-08, TC-09, TC-10

---

### Task 5: Update Template to Remove "(best-effort)" Disclaimer

**Priority:** Medium

**Complexity:** Small (30 minutes)

**Description:**
Remove the "(best-effort)" disclaimer from the documentation link text in the azapi resource template, as links are now reliable and curated.

**Acceptance Criteria:**
- [ ] Line 27 in `resource.sbn` updated from `📚 [View API Documentation (best-effort)]` to `📚 [View API Documentation]`
- [ ] Conditional rendering logic unchanged (link only shown if `doc_link` exists)
- [ ] No other template changes required
- [ ] Template still renders correctly when `doc_link` is null (no link shown)

**Dependencies:** Task 4 (helper updated to use mapper)

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`

**Notes:**
- This is the user-facing change that removes the disclaimer
- Reference architecture section: "Template Update"
- Test cases: TC-13, TC-21

---

### Task 6: Create Unit Tests for Mapper

**Priority:** High

**Complexity:** Medium (2-3 hours)

**Description:**
Implement comprehensive unit tests for the `AzureApiDocumentationMapper` class, covering known types, unknown types, version stripping, edge cases, and error handling.

**Acceptance Criteria:**
- [ ] Test file created: `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/AzureApiDocumentationMapperTests.cs`
- [ ] Test: Known resource type returns correct URL (TC-04)
- [ ] Test: Unknown resource type returns null (TC-05)
- [ ] Test: API version suffix is stripped before lookup (TC-06)
- [ ] Test: Non-Microsoft providers return null (TC-07)
- [ ] Test: Nested resource types resolve individually (TC-20)
- [ ] Test: Edge cases (null, empty, whitespace, malformed) return null (TC-10)
- [ ] Test: JSON structure validation (TC-01)
- [ ] Test: Mappings contain expected entries (TC-02)
- [ ] Test: Case-insensitive lookup works
- [ ] All tests pass via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`

**Dependencies:** Task 3 (mapper must exist)

**Files to Create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/AzureApiDocumentationMapperTests.cs`

**Notes:**
- Use TUnit test framework (follow existing test patterns)
- Test data: Use resource types from generated JSON file
- Reference test plan: TC-01, TC-02, TC-04, TC-05, TC-06, TC-07, TC-10, TC-17, TC-20
- Test malformed JSON by testing the exception path (if possible)

---

### Task 7: Update Existing Scriban Helper Tests

**Priority:** High

**Complexity:** Medium (2 hours)

**Description:**
Update existing tests in `ScribanHelpersAzApiTests.cs` to validate mapping-based behavior instead of heuristic URL construction. Remove assertions that expect heuristic URLs.

**Acceptance Criteria:**
- [ ] Existing `AzureApiDocLink` tests updated (TC-14, TC-15 in existing file)
- [ ] Tests expect mapped URLs or null (not heuristic URLs)
- [ ] Test: Known types return correct mapped URLs (TC-08)
- [ ] Test: Unknown types return null (TC-09)
- [ ] Test: Edge cases handled gracefully (TC-10)
- [ ] Test: Non-Microsoft providers return null
- [ ] All existing tests pass after updates
- [ ] No tests rely on heuristic URL patterns

**Dependencies:** Task 4 (helper updated), Task 6 (mapper tests exist)

**Files to Modify:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/ScribanHelpersAzApiTests.cs`

**Notes:**
- Identify tests that validate heuristic URLs and update expectations
- May need to add test data for mapped resource types
- Reference test plan: TC-08, TC-09, TC-10, TC-11
- Tests should validate the integration between Scriban helper and mapper

---

### Task 8: Create Integration Tests for Template Rendering

**Priority:** High

**Complexity:** Medium (2-3 hours)

**Description:**
Create integration tests that verify end-to-end rendering of azapi resources with mapped and unmapped resource types. Test that links appear/disappear correctly based on mappings.

**Acceptance Criteria:**
- [ ] Test: Mapped resource shows documentation link with correct URL (TC-12)
- [ ] Test: Unmapped resource omits documentation link (TC-13)
- [ ] Test: Mixed mapped/unmapped resources render correctly
- [ ] Test: Nested resources show individual documentation links
- [ ] Test: No "(best-effort)" text appears in rendered output (TC-21)
- [ ] Test: Resource type is displayed even when link is omitted
- [ ] Tests use realistic test plan JSON files
- [ ] All tests pass

**Dependencies:** Task 5 (template updated), Task 7 (helper tests updated)

**Files to Create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/AzureApiDocLinkIntegrationTests.cs` (or add to existing integration test file)

**Test Data Files to Create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mapped-resources.json`
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-unmapped-resources.json`

**Notes:**
- Use `MarkdownRenderer` to render full markdown from test plans
- Assert on presence/absence of documentation links in rendered output
- Reference test plan: TC-12, TC-13, TC-21
- Test data structures defined in UAT plan

---

### Task 9: Update Snapshot Tests

**Priority:** Medium

**Complexity:** Medium (1-2 hours)

**Description:**
Update existing markdown snapshot tests to reflect new behavior: no "(best-effort)" disclaimers, mapping-based URLs only, and clean degradation for unmapped resources.

**Acceptance Criteria:**
- [ ] Identify all snapshot tests that include azapi resources
- [ ] Regenerate snapshots with new template/helper behavior
- [ ] Verify snapshots show mapped URLs (no heuristic URLs)
- [ ] Verify snapshots omit links for unmapped resources
- [ ] Verify no "(best-effort)" text in any snapshots
- [ ] All snapshot tests pass after regeneration
- [ ] Snapshot diffs reviewed and approved

**Dependencies:** Task 5 (template updated), Task 8 (integration tests pass)

**Files to Modify:**
- Snapshot files in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/` (multiple files)

**Notes:**
- Use existing snapshot test infrastructure to regenerate baselines
- Carefully review diffs to ensure changes are expected
- Reference test plan: TC-22
- Consider using the `update-test-snapshots` skill if available

---

### Task 10: Create Test Data for UAT

**Priority:** Medium

**Complexity:** Small (1 hour)

**Description:**
Create test plan JSON files that will be used to generate UAT artifacts. These files should cover mapped resources, unmapped resources, and mixed scenarios.

**Acceptance Criteria:**
- [ ] File created: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mapped-resources-demo.json`
- [ ] File created: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-unmapped-resources-demo.json`
- [ ] File created: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mixed-mappings-demo.json`
- [ ] Files match structures defined in UAT plan
- [ ] Mapped resources use types with known mappings (VM, Storage, VNet)
- [ ] Unmapped resources use fake/unknown types
- [ ] Mixed file includes nested resources (e.g., blobServices)
- [ ] Files can be processed by tfplan2md to generate markdown artifacts

**Dependencies:** Task 2 (mappings exist so we know which types are mapped)

**Files to Create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mapped-resources-demo.json`
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-unmapped-resources-demo.json`
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mixed-mappings-demo.json`

**Notes:**
- Files must be valid Terraform plan JSON format
- Reference UAT plan: "Artifacts" section
- These will be used by UAT Tester to validate rendering in GitHub/Azure DevOps

---

### Task 11: Generate UAT Artifacts

**Priority:** Low

**Complexity:** Small (30 minutes)

**Description:**
Generate markdown artifacts from UAT test data using tfplan2md. These artifacts will be used for user acceptance testing in GitHub and Azure DevOps PRs.

**Acceptance Criteria:**
- [ ] Artifact generated: `artifacts/azapi-mapped-resources-demo.md`
- [ ] Artifact generated: `artifacts/azapi-unmapped-resources-demo.md`
- [ ] Artifact generated: `artifacts/azapi-mixed-mappings-demo.md`
- [ ] Artifacts show correct documentation links for mapped resources
- [ ] Artifacts omit links for unmapped resources
- [ ] Artifacts contain no "(best-effort)" text
- [ ] Artifacts are valid markdown

**Dependencies:** Task 10 (test data files exist), All implementation tasks complete

**Files to Create:**
- `artifacts/azapi-mapped-resources-demo.md`
- `artifacts/azapi-unmapped-resources-demo.md`
- `artifacts/azapi-mixed-mappings-demo.md`

**Commands:**
```bash
tfplan2md < src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mapped-resources-demo.json > artifacts/azapi-mapped-resources-demo.md
tfplan2md < src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-unmapped-resources-demo.json > artifacts/azapi-unmapped-resources-demo.md
tfplan2md < src/tests/Oocx.TfPlan2Md.TUnit/TestData/azapi-mixed-mappings-demo.json > artifacts/azapi-mixed-mappings-demo.md
```

**Notes:**
- These artifacts are inputs for UAT (next agent: UAT Tester)
- Reference UAT plan: "Test Steps"

---

### Task 12: Performance Testing (Optional)

**Priority:** Low

**Complexity:** Small (1 hour)

**Description:**
Validate that mapper lookup performance meets requirements (< 1ms average lookup time, < 100ms initialization).

**Acceptance Criteria:**
- [ ] Benchmark test created for mapper initialization time
- [ ] Benchmark test created for lookup latency
- [ ] Initialization time < 100ms
- [ ] Average lookup time < 1ms
- [ ] Memory footprint < 1 MB for 1000 mappings
- [ ] Performance meets non-functional requirements

**Dependencies:** Task 3 (mapper exists), Task 6 (unit tests pass)

**Files to Create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/AzureApiDocumentationMapperPerformanceTests.cs` (if needed)

**Notes:**
- This is optional; performance should be acceptable by design (FrozenDictionary is fast)
- Can be deferred to post-implementation validation
- Reference test plan: TC-18

---

## Implementation Order

Recommended sequence for implementation (follow dependencies):

1. **Task 1** - Create Discovery Script (foundational; enables comprehensive mappings)
2. **Task 2** - Generate Initial Mappings JSON (provides data for all subsequent tasks)
3. **Task 3** - Create Mapper Class (core infrastructure)
4. **Task 4** - Update AzureApiDocLink Helper (integrate mapper with existing code)
5. **Task 5** - Update Template (user-facing change)
6. **Task 6** - Create Unit Tests for Mapper (validate Task 3)
7. **Task 7** - Update Existing Scriban Helper Tests (validate Task 4)
8. **Task 8** - Create Integration Tests (validate end-to-end behavior)
9. **Task 9** - Update Snapshot Tests (validate no regressions)
10. **Task 10** - Create Test Data for UAT (prepare for UAT)
11. **Task 11** - Generate UAT Artifacts (prepare for UAT)
12. **Task 12** - Performance Testing (optional validation)

**Critical Path:** Tasks 1 → 2 → 3 → 4 → 5 → 8 → 11 (minimum for working feature)

**Testing Path:** Tasks 6 → 7 → 8 → 9 (ensure quality and no regressions)

**UAT Path:** Tasks 10 → 11 (prepare for user acceptance testing)

## Open Questions

1. **Script Language Choice**: Should the discovery script be Python, PowerShell, or C#?
   - **Recommendation**: Python for portability and Azure SDK availability, but defer to Developer preference

2. **Mapping Coverage Target**: How many resource types should we aim for initially?
   - **Recommendation**: Start with 20-30 for testing, then expand to 100+ with script (per architecture)

3. **URL Validation**: Should we validate generated URLs programmatically or rely on spot-checking?
   - **Recommendation**: Spot-checking only (per architecture decision; no HTTP validation)

4. **Kebab-Case Function**: Should we remove `ConvertToKebabCase()` or keep it for potential future use?
   - **Recommendation**: Remove it (or mark as obsolete) since it's no longer needed after Task 4

## Definition of Done

- [ ] All 12 tasks completed and acceptance criteria met
- [ ] Discovery script exists and can generate comprehensive mappings
- [ ] Mapper class loads mappings from embedded JSON with O(1) lookup
- [ ] Helper function uses mapper instead of heuristic URL construction
- [ ] Template removes "(best-effort)" disclaimer
- [ ] All unit tests pass (mapper, helper, integration)
- [ ] All existing tests updated and passing
- [ ] Snapshot tests regenerated and approved
- [ ] UAT test data and artifacts created
- [ ] Code committed to feature branch: `copilot/add-api-documentation-mapping`
- [ ] Ready for UAT Tester to validate rendering in GitHub/Azure DevOps PRs
- [ ] All tests run without human intervention via `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`

## Handoff Notes

**Next Agent:** Developer (to implement tasks in order)

**UAT Preparation:** After Task 11 completes, hand off to UAT Tester with artifacts for validation in GitHub and Azure DevOps PRs.

**Documentation Updates:** After UAT passes, Technical Writer should update README and maintainer documentation for the discovery script usage.
