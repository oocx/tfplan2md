# Architecture: Azure API Documentation Mapping

## Status

Proposed

## Context

The current implementation in `AzApi.Resources.cs` generates Azure REST API documentation URLs using a heuristic that often produces broken links. This feature replaces URL guessing with reliable, curated mappings from authoritative Azure sources.

After reviewing:
- Microsoft Learn REST API documentation structure
- Azure SDK Specs Inventory organization
- Existing project patterns (AzureRoleDefinitions.json mapping)
- Project architecture and quality goals
- The 8 open questions in the specification

This document provides concrete architecture decisions for all design questions.

## Architecture Decisions

### 1. Storage Format: Embedded JSON

**Decision:** Use embedded JSON file with version-agnostic mappings.

**Format:**
```json
{
  "mappings": {
    "Microsoft.Compute/virtualMachines": {
      "url": "https://learn.microsoft.com/rest/api/compute/virtual-machines"
    },
    "Microsoft.Storage/storageAccounts": {
      "url": "https://learn.microsoft.com/rest/api/storagerp/storage-accounts"
    },
    "Microsoft.Storage/storageAccounts/blobServices": {
      "url": "https://learn.microsoft.com/rest/api/storagerp/blob-services"
    }
  },
  "metadata": {
    "version": "1.0.0",
    "lastUpdated": "2025-01-15",
    "source": "Microsoft Learn REST API Documentation"
  }
}
```

**Rationale:**
- **Consistency with existing patterns**: Project already uses embedded JSON for AzureRoleDefinitions.json
- **Fast lookup**: FrozenDictionary provides O(1) lookup performance
- **No external dependencies**: Works offline, no file I/O at runtime
- **Compile-time inclusion**: Guaranteed availability, no deployment concerns
- **Human-maintainable**: JSON is easy to read, edit, and review in PRs
- **Diffable**: Git diffs clearly show mapping additions/changes

**File location:** `src/Oocx.TfPlan2Md/Providers/AzApi/Data/AzureApiDocumentationMappings.json`

**Loading pattern:** Follow `AzureRoleDefinitionMapper.Roles.cs` pattern:
- Static `FrozenDictionary<string, string>` loaded at startup
- Case-insensitive key comparison (resource types may vary in casing)
- Exception on load failure (fail-fast for corrupted data)

### 2. API Version Handling: Version-Agnostic

**Decision:** Map resource types without API versions; strip `@YYYY-MM-DD` suffix before lookup.

**Rationale:**
- **Microsoft Learn URLs are version-agnostic**: Examined documentation shows URLs like `/rest/api/compute/virtual-machines` without version segments
- **API versions in query parameters**: Azure uses `?view=rest-compute-2025-04-01` query parameters, not path segments
- **Fewer mappings**: One entry per resource type instead of one per version
- **Simpler maintenance**: No need to add mappings for every new API version
- **User expectation**: Users want resource type documentation, not version-specific minutiae

**Lookup algorithm:**
```
Input: "Microsoft.Compute/virtualMachines@2023-03-01"
Step 1: Strip version → "Microsoft.Compute/virtualMachines"
Step 2: Lookup in mappings dictionary
Step 3: Return URL or null
```

**Edge case:** If version-specific documentation is needed in the future, add optional version-specific overrides:
```json
"Microsoft.Compute/virtualMachines@2019-03-01": {
  "url": "...",
  "note": "Legacy API version with different behavior"
}
```

### 3. Coverage Strategy: Comprehensive (All Azure Resources)

**Decision:** Include mappings for all Azure resources if possible, rather than incremental batches.

**Approach:**
- Generate mappings for all available Azure resource types from authoritative sources
- Use automated discovery script to maximize coverage
- Validate generated mappings through spot-checking
- Focus on quality and accuracy over manual curation of subsets

**Rationale:**
- **Maximum value**: Users get documentation links for all resources from day one
- **No artificial limitations**: Avoid prioritizing certain resources over others
- **Simpler maintenance**: One comprehensive generation run instead of multiple incremental batches
- **Automation-friendly**: Discovery script can process all resources as easily as a subset
- **Better user experience**: Consistent documentation link availability across all resource types

**Fallback for unmappable resources:**
- If a resource type cannot be reliably mapped, it will be omitted from the mappings
- Template will gracefully handle missing mappings (no link shown)

### 4. Automation Level: Semi-Automated with Update Script

**Decision:** Semi-automated generation with manual/on-demand execution.

**Approach:**
1. **Automated discovery script** (required component):
   - Scrape Azure SDK Specs Inventory page or parse azure-rest-api-specs repository
   - Parse specification folders to discover resource types
   - Generate candidate mappings based on discovered patterns
   - Output as JSON file with comprehensive mappings

2. **Manual execution**:
   - Script is run manually by maintainer on-demand
   - No automated CI/CD integration
   - Maintainer reviews generated output for obvious errors
   - Validates mappings through spot-checking (sample URLs)
   - Commits validated mappings to the repository

3. **Update workflow**:
   - Run discovery script when needed (new Azure services, maintainer discretion)
   - Review and validate output
   - Update `AzureApiDocumentationMappings.json`
   - Release with next version of tfplan2md

**Rationale:**
- **Script required**: Automated generation is necessary for comprehensive coverage
- **Manual execution**: Maintainer controls when updates occur; no CI/CD overhead
- **On-demand updates**: Run script as needed rather than on fixed schedule
- **Quality validation**: Spot-checking catches major issues without full manual review
- **Low maintenance burden**: Script does heavy lifting; maintainer validates and commits

**Out of scope:**
- Automated CI/CD pipeline to refresh mappings on every build
- Real-time scraping or API calls at runtime
- Comprehensive URL validation (checking all generated URLs)

### 5. Fallback Behavior: No Link (Clean Degradation)

**Decision:** When no mapping exists, omit the documentation link entirely. Do not show heuristic fallback.

**Template change:**
```scriban
{{~ # Generate documentation link if available ~}}
{{~ if change.after_json && change.after_json.type ~}}
{{~ doc_link = azure_api_doc_link change.after_json.type ~}}
{{~ else if change.before_json && change.before_json.type ~}}
{{~ doc_link = azure_api_doc_link change.before_json.type ~}}
{{~ end ~}}

{{~ if doc_link ~}}

📚 [View API Documentation]({{ doc_link | escape_markdown }})
{{~ end ~}}
```

**Rationale:**
- **No broken links**: The entire point of this feature is to eliminate unreliable links
- **Clean UX**: Absence of link is better than a broken link
- **Signals to users**: Missing link can prompt them to contribute the mapping
- **Remove "(best-effort)" disclaimer**: When link is present, it's reliable

**Alternative considered (rejected):**
- **Fallback to heuristic**: Defeats the purpose; still produces broken links
- **"Documentation not available" message**: Verbose and looks unpolished
- **Link to generic Azure docs**: Not helpful; adds noise without value

**Future enhancement:**
If users request it, add a configuration flag to enable heuristic fallback for advanced users who understand the limitations.

### 6. Nested Resources: Individual Mappings

**Decision:** Map each nested resource level individually; no automatic inference.

**Example:**
```json
{
  "Microsoft.Storage/storageAccounts": {
    "url": "https://learn.microsoft.com/rest/api/storagerp/storage-accounts"
  },
  "Microsoft.Storage/storageAccounts/blobServices": {
    "url": "https://learn.microsoft.com/rest/api/storagerp/blob-services"
  },
  "Microsoft.Storage/storageAccounts/blobServices/containers": {
    "url": "https://learn.microsoft.com/rest/api/storagerp/blob-containers"
  }
}
```

**Rationale:**
- **Accurate URLs**: Microsoft Learn has separate pages for nested resources; URLs don't follow parent/child patterns
- **No ambiguity**: Explicit mappings avoid incorrect inferences
- **Simple lookup logic**: No complex parent traversal or URL construction
- **Maintainable**: Clear what is mapped and what isn't

**Lookup algorithm:** Direct dictionary lookup; no fallback to parent resource.

### 7. Update Process: Manual/On-Demand Script Execution

**Decision:** Update mappings on-demand by manually running the discovery script.

**Update process:**
1. Maintainer runs the discovery script when updates are needed
2. Script generates updated `AzureApiDocumentationMappings.json`
3. Maintainer reviews output for obvious errors (spot-checking)
4. Commits updated mappings to repository
5. Releases with next version of tfplan2md

**Rationale:**
- **Maintainer-controlled**: Updates happen when maintainer determines they're needed
- **Script-driven**: Automation handles discovery and generation
- **No fixed schedule**: Avoids unnecessary updates when Azure documentation is stable
- **Simple workflow**: Run script, review, commit, release
- **Low overhead**: No CI/CD integration or complex automation

**Trigger for updates:**
- User reports broken or missing mapping
- Major Azure service launch (e.g., new compute service)
- Microsoft restructures documentation URLs (rare but possible)
- Maintainer discretion based on Azure release notes

### 8. Community Contributions: Out of Scope

**Decision:** Community contribution process for mappings is out of scope for this feature.

**Rationale:**
- **Comprehensive coverage**: With automated script generating all mappings, individual contributions are less critical
- **Simpler workflow**: Maintainer runs script on-demand instead of managing individual mapping PRs
- **Reduced maintenance burden**: No need to document contribution guidelines, review individual submissions, or validate contributor URLs
- **Script-first approach**: Updates come from running the discovery script rather than manual contributions
- **Focus on core feature**: Keep initial implementation focused on automated generation and core functionality

**Alternative for user-reported issues:**
- Users can report broken or missing mappings via GitHub issues
- Maintainer investigates and re-runs discovery script if needed
- Fixes included in next mapping update

## Component Design

### 1. Data Model

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Data/AzureApiDocumentationMappings.json`

```json
{
  "mappings": {
    "Microsoft.Service/resourceType": {
      "url": "https://learn.microsoft.com/rest/api/..."
    }
  },
  "metadata": {
    "version": "1.0.0",
    "lastUpdated": "YYYY-MM-DD",
    "source": "Microsoft Learn REST API Documentation",
    "contributors": []
  }
}
```

### 2. Mapping Loader

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/AzureApiDocumentationMapper.cs`

```csharp
/// <summary>
/// Maps Azure resource types to their official REST API documentation URLs.
/// Related feature: docs/features/048-azure-api-doc-mapping/specification.md.
/// </summary>
public static class AzureApiDocumentationMapper
{
    private static readonly FrozenDictionary<string, string> Mappings = LoadMappings();

    /// <summary>
    /// Gets the documentation URL for the specified Azure resource type.
    /// </summary>
    /// <param name="resourceType">
    /// Azure resource type (e.g., "Microsoft.Compute/virtualMachines" or 
    /// "Microsoft.Compute/virtualMachines@2023-03-01"). API version suffix is stripped.
    /// </param>
    /// <returns>
    /// Documentation URL if mapping exists; otherwise null.
    /// </returns>
    public static string? GetDocumentationUrl(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType)) return null;

        // Strip API version (e.g., "@2023-03-01")
        var typeWithoutVersion = resourceType.Split('@', 2)[0];

        return Mappings.TryGetValue(typeWithoutVersion, out var url) ? url : null;
    }

    private static FrozenDictionary<string, string> LoadMappings()
    {
        // Load from embedded JSON resource (follow AzureRoleDefinitionMapper pattern)
        // Parse JSON, extract mappings dictionary, return as FrozenDictionary
    }
}
```

### 3. Scriban Helper Update

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/ScribanHelpers/AzApi.Resources.cs`

**Change:** Replace `AzureApiDocLink()` implementation to use mapper:

```csharp
/// <summary>
/// Gets the official Azure REST API documentation URL for a resource type.
/// </summary>
/// <param name="resourceType">
/// Azure resource type string (e.g., "Microsoft.Automation/automationAccounts@2021-06-22").
/// </param>
/// <returns>
/// Official documentation URL from curated mappings, or null if no mapping exists.
/// </returns>
/// <remarks>
/// Uses curated mappings from Microsoft Learn. Only returns URLs for known resource types.
/// API version suffixes are ignored when looking up mappings.
/// </remarks>
public static string? AzureApiDocLink(string? resourceType)
{
    return AzureApiDocumentationMapper.GetDocumentationUrl(resourceType);
}
```

**Backward compatibility:** Existing template usage (`azure_api_doc_link`) continues to work; only internal implementation changes.

### 4. Template Update

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Templates/azapi/resource.sbn`

**Change (line 27):** Remove "(best-effort)" disclaimer:

```diff
- 📚 [View API Documentation (best-effort)]({{ doc_link | escape_markdown }})
+ 📚 [View API Documentation]({{ doc_link | escape_markdown }})
```

**No other template changes needed.**

### 5. Discovery Script (Required Component)

**File:** `scripts/generate-azure-api-mappings.py` (or C#/PowerShell)

**Purpose:** Generate comprehensive mappings for all Azure resources.

**Approach:**
1. Fetch Azure SDK Specs Inventory page or parse azure-rest-api-specs repository
2. Discover all resource types from specifications
3. Generate documentation URLs using known patterns:
   - `Microsoft.Compute` → `/rest/api/compute/`
   - Resource type → kebab-case conversion
4. Output complete JSON file with all mappings
5. Maintainer runs script manually/on-demand
6. Maintainer validates via spot-checking and commits

**Requirements:**
- Must be part of the feature implementation (not optional)
- Should generate mappings for all discoverable Azure resource types
- Should be runnable manually by maintainer
- Should output valid JSON matching the expected schema

## Implementation Strategy

### Phase 1: Infrastructure (Developer)
1. Create discovery script to generate mappings from Azure sources
2. Run script to generate initial `AzureApiDocumentationMappings.json`
3. Implement `AzureApiDocumentationMapper` class
4. Update `AzureApiDocLink()` helper to use mapper
5. Update tests to validate mapping-based behavior
6. Remove "(best-effort)" from template

### Phase 2: Validation and Release (Maintainer)
1. Review generated mappings via spot-checking (sample URLs)
2. Verify script can be re-run on-demand
3. Document script usage for future updates
4. Release feature with comprehensive mappings

### Phase 3: Ongoing Maintenance (Maintainer)
1. Run discovery script on-demand when updates needed
2. Respond to user reports of broken/missing mappings
3. Update mappings and release new version

## Testing Strategy

### Unit Tests
- `AzureApiDocumentationMapper.GetDocumentationUrl()`:
  - Known resource type → returns correct URL
  - Resource type with version → strips version and returns URL
  - Unknown resource type → returns null
  - Nested resource type → returns specific URL (not parent)
  - Case variations → case-insensitive matching

### Integration Tests
- Scriban template rendering with:
  - azapi_resource with known type → link displayed
  - azapi_resource with unknown type → no link displayed
  - azapi_resource with versioned type → link displayed (version stripped)

### Snapshot Tests
- Update existing snapshots to reflect new behavior (no best-effort links)

## Non-Functional Requirements

### Performance
- **Startup time**: FrozenDictionary loaded at startup (once)
- **Lookup time**: O(1) dictionary lookup, negligible overhead
- **Memory**: ~10-50 KB for 100 mappings, acceptable

### Security
- **No external calls**: Embedded data only, no network requests
- **Trusted source**: Mappings curated by maintainer, not user-provided at runtime
- **No injection risk**: URLs are static strings from JSON, not constructed from user input

### Maintainability
- **Clear data format**: JSON is human-readable and reviewable
- **Git-friendly**: Diffs clearly show mapping changes
- **Documentation**: Contribution guidelines lower barrier for community help
- **Versioned mappings**: Metadata tracks when mappings were last updated

### Reliability
- **Fail-fast on load errors**: If JSON is corrupted, application fails at startup (not during rendering)
- **Graceful degradation**: Missing mappings don't break rendering; link is simply omitted
- **No cascading failures**: Mapper is isolated; failures don't affect other features

## Alternatives Considered

### Alternative 1: External JSON File (User-Provided)
**Rejected:** Adds deployment complexity; users unlikely to customize mappings; embedded approach is simpler.

### Alternative 2: Keep Heuristic as Default, Mappings as Override
**Rejected:** Heuristic produces too many broken links; better to omit link than show broken one.

### Alternative 3: Fully Automated URL Scraping at Runtime
**Rejected:** Requires network calls; unreliable; breaks offline usage; adds latency; violates project constraints.

### Alternative 4: CSV File Instead of JSON
**Rejected:** JSON supports nested structure (metadata, future extensions); CSV is flat and limiting.

### Alternative 5: Version-Specific Mappings
**Rejected:** Microsoft Learn URLs are version-agnostic; version-specific mappings add complexity without benefit.

## Consequences

### Positive
- **Reliable documentation links**: Users get correct URLs, not guesses
- **Clean user experience**: No more "(best-effort)" disclaimers
- **Maintainable**: JSON format is easy to update and review
- **Extensible**: Can add more mappings incrementally
- **Community-friendly**: Clear contribution process enables user participation
- **Consistent with existing patterns**: Follows AzureRoleDefinitions.json precedent

### Negative
- **Script development effort**: Initial time to build discovery script (estimated 4-8 hours)
- **Potential incomplete coverage**: Some Azure resources may not be discoverable via automated script
- **Manual update trigger**: Maintainer must remember to run script when Azure releases new services
- **No automated validation**: Generated URLs not automatically verified for correctness

### Risks
- **Microsoft changes URL structure**: Low risk; Microsoft maintains stable documentation URLs. If it happens, re-run script to regenerate mappings.
- **Outdated mappings**: Updates depend on maintainer running script. Mitigated by user reports triggering script re-runs.
- **Broken links over time**: Azure may deprecate old services. Mitigated by maintainer spot-checking and user reports.

## Open Questions (Resolved)

All 8 open questions from the specification have been answered:

1. ✅ **Storage format**: Embedded JSON
2. ✅ **API versioning**: Version-agnostic (strip version suffix)
3. ✅ **Coverage strategy**: Comprehensive (all Azure resources if possible)
4. ✅ **Automation level**: Semi-automated with required update script (manual/on-demand execution)
5. ✅ **Fallback behavior**: No link when mapping missing (clean degradation)
6. ✅ **Nested resources**: Individual mappings per level
7. ✅ **Update process**: Manual/on-demand script execution (no fixed schedule)
8. ✅ **Community contributions**: Out of scope

## Next Steps

**For Quality Engineer:**
1. Review architecture decisions
2. Define test cases for mapping lookup logic
3. Define test coverage for template rendering with/without mappings
4. Identify representative resource types for testing

**For Developer:**
1. Implement `AzureApiDocumentationMapper` class (follow `AzureRoleDefinitionMapper` pattern)
2. Create `AzureApiDocumentationMappings.json` with initial 10-20 resources for testing
3. Update `AzureApiDocLink()` helper to use mapper
4. Update template to remove "(best-effort)" disclaimer
5. Update and expand test coverage

**For Technical Writer:**
1. Document new mapping-based approach in README
2. Document discovery script usage for maintainer reference

**For Maintainer:**
1. Review generated mappings from discovery script
2. Spot-check sample URLs for accuracy
3. Validate script can be re-run for future updates
