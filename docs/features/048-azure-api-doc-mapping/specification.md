# Feature: Official Azure API Documentation Mappings

## Overview

Currently, `tfplan2md` generates Azure REST API documentation links for `azapi_resource` by guessing the URL structure based on common patterns and conventions. The `AzureApiDocLink` helper function converts resource types like `Microsoft.Automation/automationAccounts@2021-06-22` into documentation URLs by:
1. Extracting the service name from the `Microsoft.{Service}` prefix
2. Converting it to lowercase
3. Converting the resource type to kebab-case
4. Constructing a URL: `https://learn.microsoft.com/rest/api/{service}/{resource}/`

This heuristic approach often produces incorrect or broken links because Azure documentation URLs don't follow a perfectly predictable pattern across all services.

This feature will replace URL guessing with reliable, official mappings from authoritative Azure sources, ensuring users always get accurate documentation links.

## User Goals

- **Access correct documentation reliably**: Users need documentation links that actually work, not best-effort guesses
- **Discover Azure API details quickly**: When reviewing azapi resources, users want immediate access to the official REST API reference
- **Build confidence in tooling**: Broken documentation links erode trust in the tool; reliable links build confidence
- **Reduce manual lookup effort**: Users shouldn't need to manually search for Azure API documentation when the tool can provide accurate links

## Scope

### In Scope

1. **Official Mapping Data Source**: Establish and integrate authoritative mappings between Azure resource types and their documentation URLs using:
   - Microsoft Learn REST API documentation index (https://learn.microsoft.com/en-us/rest/api/azure/)
   - Azure REST API Specs repository (https://github.com/Azure/azure-rest-api-specs)
   - Azure SDK Specs Inventory page (https://azure.github.io/azure-sdk/releases/latest/specs.html)

2. **Mapping Storage Format**: Design a maintainable format for storing resource type → documentation URL mappings, considering:
   - JSON, CSV, or other structured format
   - Embedded in the application vs. external file
   - Update strategy (manual curation, automated generation, or hybrid)
   - Resource type patterns (exact matches, wildcards, versioned vs. version-agnostic)

3. **Mapping Lookup Logic**: Implement reliable lookup logic that:
   - Matches resource types to documentation URLs
   - Handles API version variations (e.g., should `Microsoft.Compute/virtualMachines@2023-03-01` map to the same docs as other versions?)
   - Falls back gracefully when no mapping exists (return null or show resource type without link)
   - Maintains backward compatibility with existing behavior where appropriate

4. **Mapping Maintenance Strategy**: Establish how mappings will be kept up-to-date:
   - Manual updates as part of project maintenance
   - Automated script to regenerate mappings from official sources
   - Contribution guidelines for adding missing mappings
   - Versioning strategy for the mapping data

5. **Update Existing Template**: Modify the `azapi/resource.sbn` template to use the new mapping-based approach instead of the current `azure_api_doc_link` helper (or update the helper to use mappings)

6. **Testing Coverage**: Ensure test coverage for:
   - Known resource types with correct documentation links
   - Unknown resource types (graceful degradation)
   - Edge cases (non-Microsoft providers, malformed resource types)

### Out of Scope

- Real-time fetching of documentation URLs from Azure APIs (mappings will be static or periodically updated)
- Support for non-Azure providers (only `Microsoft.*` resource types are in scope)
- Automatic redirection or validation of documentation URLs (we trust the official sources)
- Documentation content rendering or preview within tfplan2md
- Support for data plane APIs (focus on ARM/management plane resources)
- Custom documentation URL overrides by users (may be a future enhancement)

## Current Behavior Analysis

### Implementation Details

The current URL generation is implemented in:
- **File**: `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Resources.cs`
- **Function**: `AzureApiDocLink(string? resourceType)` (lines 97-122)
- **Helper Function**: `ParseAzureResourceType(string? resourceType)` (lines 32-75)
- **Utility Function**: `ConvertToKebabCase(string input)` (lines 129-147)

### URL Generation Algorithm

```
Input: "Microsoft.Automation/automationAccounts@2021-06-22"

Step 1: Parse resource type
  - Provider: "Microsoft.Automation"
  - Service: "Automation" (extracted from Microsoft.{Service})
  - Resource Type: "automationAccounts"
  - API Version: "2021-06-22"

Step 2: Transform for URL
  - Service → lowercase: "automation"
  - Resource Type → kebab-case: "automation-accounts"

Step 3: Construct URL
  - Pattern: https://learn.microsoft.com/rest/api/{service}/{resource}/
  - Result: https://learn.microsoft.com/rest/api/automation/automation-accounts/
```

### Known Issues with Current Approach

1. **Inconsistent URL patterns**: Not all Azure services follow the `{service}/{resource}` pattern
2. **Service name mismatches**: The service name in the resource type doesn't always match the documentation URL segment
   - Example: `Microsoft.Network` might map to `/networkmanagement/` or `/network/` depending on the resource
3. **Kebab-case conversion limitations**: Some resource types don't kebab-case predictably
   - Example: `Microsoft.Storage/storageAccounts/blobServices` might not map cleanly
4. **API version impact**: Some documentation URLs include or depend on API versions; current approach ignores them
5. **Resource hierarchy**: Child resources (e.g., `Microsoft.Storage/storageAccounts/blobServices/containers`) may have different URL structures
6. **Preview APIs**: Preview resource types may have different documentation locations

### Template Usage

The `AzureApiDocLink` helper is used in:
- **Template**: `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn` (lines 18-28)
- **Display**: Rendered as `📚 [View API Documentation (best-effort)]({url})`
- **Note**: "(best-effort)" label acknowledges current limitations

### Test Coverage

Tests exist in `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzApi/ScribanHelpersAzApiTests.cs`:
- TC-14: Basic URL construction (line 369-381)
- TC-15: Non-Microsoft providers return null (line 405-417)
- Additional tests for different services (line 384-402)

These tests validate the current heuristic behavior; they'll need updating to validate mapping-based behavior.

## Official Sources Investigation

Based on research, Azure provides several authoritative sources for mapping resource types to documentation:

### 1. Azure REST API Specs Repository
- **URL**: https://github.com/Azure/azure-rest-api-specs
- **Structure**: Organized under `specification/` directory by service and resource provider
  - Resource Manager APIs: `specification/{service}/resource-manager/{ResourceProvider}/`
  - Data Plane APIs: `specification/{service}/data-plane/`
- **Content**: OpenAPI/Swagger definitions with README files that often contain documentation links
- **Pros**: Authoritative source, maintained by Azure teams, includes all services
- **Cons**: Not a simple lookup table; requires parsing repo structure and README files

### 2. Azure SDK Specs Inventory
- **URL**: https://azure.github.io/azure-sdk/releases/latest/specs.html
- **Content**: Auto-generated inventory of all Azure REST API specs with links to specs and documentation
- **Pros**: Centralized, machine-readable, includes documentation links where available
- **Cons**: May not cover 100% of resource types; requires web scraping or automated extraction

### 3. Microsoft Learn REST API Documentation
- **URL**: https://learn.microsoft.com/en-us/rest/api/azure/
- **Content**: Main hub for official Azure REST API documentation
- **Structure**: Service-based navigation, but URL patterns vary
- **Pros**: End-user documentation URLs (what we want to link to)
- **Cons**: No programmatic API for mapping; requires manual curation or scraping

### 4. Mapping Approach

The most reliable approach appears to be:
1. **Primary source**: Azure SDK Specs Inventory page for automated discovery of documentation links
2. **Secondary source**: Manual curation based on Microsoft Learn structure for edge cases
3. **Validation**: Cross-reference with azure-rest-api-specs repository for completeness

## User Experience

### Updated Documentation Link Display

The user-facing markdown output should change from:

**Before:**
```markdown
📚 [View API Documentation (best-effort)](https://learn.microsoft.com/rest/api/automation/automation-accounts/)
```

**After (when mapping exists):**
```markdown
📚 [View API Documentation](https://learn.microsoft.com/rest/api/automation/automation-accounts/)
```

**After (when no mapping exists):**
```markdown
**Type:** `Microsoft.UnknownService/unknownResource@2023-01-01`
```
*(No documentation link shown)*

### Benefits to Users

1. **Confidence**: No more "(best-effort)" disclaimer; links are reliable
2. **Fewer dead links**: Only display links when we have verified mappings
3. **Better discoverability**: Users can trust that clicking the link will provide accurate API documentation
4. **Consistent experience**: All supported resource types have correct links

## Success Criteria

- [ ] A mapping data structure exists containing verified Azure resource type → documentation URL mappings
- [ ] Mapping data source is authoritative (derived from official Azure sources)
- [ ] Lookup logic replaces the existing heuristic URL construction
- [ ] Documentation links in azapi_resource templates use the new mapping-based approach
- [ ] Existing tests are updated to validate mapping-based behavior
- [ ] New tests verify handling of:
  - Known resource types with mappings
  - Unknown resource types without mappings
  - Edge cases (malformed types, non-Microsoft providers)
- [ ] Template no longer displays "(best-effort)" disclaimer when mappings are used
- [ ] README or documentation explains how to update or extend the mappings
- [ ] A process or script exists for maintaining/refreshing mappings from official sources

## Open Questions

### 1. Mapping Storage Format

**Question:** What format should we use to store resource type → documentation URL mappings?

**Options:**
- **A) JSON file** (`azure-api-docs.json`)
  - Pros: Human-readable, easy to edit, widely supported
  - Cons: Requires file I/O, versioning complexity
  
- **B) Embedded C# data structure** (e.g., `Dictionary<string, string>`)
  - Pros: No external dependencies, compile-time checking, fast lookup
  - Cons: Requires recompilation for updates, harder to maintain
  
- **C) CSV file** with columns: `ResourceType`, `DocumentationUrl`
  - Pros: Simple, can be edited in spreadsheets, diff-friendly
  - Cons: Limited structure, no nesting or metadata
  
- **D) Hybrid** (embedded fallback + optional external file for overrides)
  - Pros: Best of both worlds, extensible
  - Cons: Added complexity

**Recommendation needed:** The Architect should evaluate trade-offs considering maintainability, deployment simplicity, and update frequency.

### 2. API Version Handling

**Question:** Should mappings include or ignore API versions?

**Considerations:**
- Resource type: `Microsoft.Compute/virtualMachines@2023-03-01`
- Does documentation URL change based on API version?
- Should we store one mapping per version, or version-agnostic mappings?

**Options:**
- **A) Version-agnostic**: Map `Microsoft.Compute/virtualMachines` → URL (ignore `@2023-03-01`)
  - Pros: Fewer mappings, simpler maintenance
  - Cons: May miss version-specific docs
  
- **B) Version-specific**: Map full type including version
  - Pros: Precise, handles version-specific API docs
  - Cons: Many more mappings, frequent updates needed
  
- **C) Hybrid**: Map version-agnostic with optional version-specific overrides
  - Pros: Covers most cases with minimal entries
  - Cons: More complex lookup logic

**Research needed:** Examine how Microsoft Learn structures documentation URLs—do they include API versions?

### 3. Mapping Coverage

**Question:** How many Azure resource types do we need to map initially?

**Considerations:**
- There are 100+ Azure resource providers with potentially 1000+ resource types
- Should we map all of them, or start with commonly-used ones?
- What's the user experience when a mapping is missing?

**Options:**
- **A) Comprehensive**: Map all known resource types upfront
  - Pros: Complete coverage from day one
  - Cons: Large initial effort, hard to verify all mappings
  
- **B) Incremental**: Start with top 20-50 most common resource types
  - Pros: Faster initial implementation, validated mappings
  - Cons: Incomplete coverage, users may encounter missing links
  
- **C) On-demand**: Add mappings as users request them
  - Pros: Minimal initial work, user-driven priorities
  - Cons: Poor initial experience, reactive approach

**Recommendation needed:** Architect and maintainer should decide coverage strategy based on effort vs. value.

### 4. Automated Mapping Generation

**Question:** Should we build automation to generate mappings from official sources?

**Considerations:**
- Manual curation is accurate but time-consuming
- Automated extraction could be error-prone but scalable
- Hybrid approach: automated generation + manual review

**Options:**
- **A) Fully manual**: Maintainers hand-craft all mappings
  - Pros: High quality, reviewed
  - Cons: Slow, doesn't scale
  
- **B) Fully automated**: Script scrapes Azure SDK inventory and generates mappings
  - Pros: Comprehensive, repeatable, stays current
  - Cons: May produce incorrect mappings, requires maintenance
  
- **C) Semi-automated**: Script generates candidate mappings, maintainer reviews and commits
  - Pros: Balances quality and efficiency
  - Cons: Still requires maintainer time

**Recommendation needed:** Architect should propose approach with script design if automation is chosen.

### 5. Fallback Behavior

**Question:** When a resource type has no mapping, what should the template display?

**Options:**
- **A) No link**: Simply don't show a documentation link
  - Pros: Honest, doesn't mislead users
  - Cons: Sudden absence of link may confuse users
  
- **B) Fallback to heuristic**: Use the current best-effort URL as fallback
  - Pros: Still provides a link that might work
  - Cons: Defeats the purpose of reliable mappings, still broken links
  
- **C) Show "Documentation not available" message**
  - Pros: Explicit communication
  - Cons: May look incomplete or unpolished
  
- **D) Link to generic Azure REST API docs page** (top-level index)
  - Pros: Always valid link, user can navigate manually
  - Cons: Not helpful, extra clicks required

**Recommendation needed:** Designer/Technical Writer should advise on user-friendly fallback UX.

### 6. Child Resources and Nested Types

**Question:** How do we handle nested resource types?

**Example:**
- `Microsoft.Storage/storageAccounts` (parent)
- `Microsoft.Storage/storageAccounts/blobServices` (child)
- `Microsoft.Storage/storageAccounts/blobServices/containers` (grandchild)

**Considerations:**
- Do these have separate documentation pages?
- Should we map each level individually?
- Or map parent and infer child URLs?

**Research needed:** Examine Microsoft Learn structure for nested resource documentation.

### 7. Update Cadence

**Question:** How often should mappings be updated?

**Options:**
- On-demand when users report issues
- Quarterly with Azure service updates
- Automated via CI/CD pipeline
- Manually as part of project releases

**Recommendation needed:** Maintainer should decide based on user needs and maintenance burden.

### 8. Contribution Process

**Question:** Should external users be able to contribute new mappings?

**Considerations:**
- Community contributions could expand coverage
- Need validation process to ensure accuracy
- Documentation for how to add mappings

**Recommendation needed:** Maintainer should define contribution guidelines if contributions are welcome.

## Next Steps

1. **Architect** to:
   - Review this specification and research Azure documentation URL patterns
   - Investigate the Azure SDK Specs Inventory and REST API specs repository structure
   - Design the mapping data structure and storage format
   - Propose lookup algorithm and API version handling strategy
   - Prototype mapping generation approach (manual, automated, or hybrid)
   - Estimate scope and coverage for initial implementation

2. **Quality Engineer** to:
   - Define test coverage requirements for mapping lookup
   - Identify representative resource types for test cases
   - Plan validation strategy for mapping accuracy
   
3. **Developer** to:
   - Implement mapping storage and lookup logic (based on Architect's design)
   - Update `AzureApiDocLink` function or create new mapping-based equivalent
   - Modify azapi/resource.sbn template to use new approach
   - Update existing tests and add new test coverage

4. **Technical Writer** to:
   - Document the new mapping-based approach
   - Create contribution guidelines for adding new mappings
   - Update README with improved documentation linking feature
   - Remove "(best-effort)" disclaimers from documentation

5. **UAT Tester** to:
   - Validate documentation links work correctly on real-world azapi resources
   - Test on both GitHub and Azure DevOps markdown rendering
   - Verify user experience with and without mappings
