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

### 3. Coverage Strategy: Incremental with Top Resources

**Decision:** Start with top 50-100 most common Azure resources, expand incrementally based on usage data and community contributions.

**Initial Coverage Priority:**
1. **Core compute**: Virtual Machines, VM Scale Sets, App Services, Container Instances, AKS
2. **Core networking**: Virtual Networks, Subnets, NSGs, Load Balancers, Application Gateways
3. **Core storage**: Storage Accounts, Blob/Table/Queue/File Services
4. **Identity & security**: Key Vaults, Managed Identities, Role Assignments
5. **Databases**: SQL Databases, Cosmos DB, PostgreSQL, MySQL
6. **Monitoring**: Log Analytics, Application Insights, Monitor Action Groups

**Rationale:**
- **Pragmatic**: Comprehensive mapping of 1000+ resources is high-effort and error-prone
- **Fast initial value**: Users see improvements immediately for common resources
- **Quality over quantity**: Better to have 50 accurate mappings than 500 unverified ones
- **Incremental validation**: Each batch can be tested and validated
- **Usage-driven expansion**: Community can report missing mappings via issues

**Success metrics:**
- Target 80% coverage of resources seen in real-world Terraform plans within 6 months
- Track "mapping found" vs "mapping missing" in telemetry (if added later)

### 4. Automation Level: Hybrid (Semi-Automated)

**Decision:** Semi-automated generation with manual curation and review.

**Approach:**
1. **Automated discovery script** (one-time or on-demand):
   - Scrape Azure SDK Specs Inventory page
   - Parse specification folders from azure-rest-api-specs repo
   - Generate candidate mappings based on discovered patterns
   - Output as JSON for review

2. **Manual curation process**:
   - Maintainer reviews generated mappings
   - Validates URLs by spot-checking (curl/browser)
   - Adds missing mappings for resources not auto-discovered
   - Commits validated mappings to the repository

3. **Update workflow**:
   - Quarterly or semi-annual mapping refresh
   - Run discovery script to find new resources
   - Review and merge updates
   - No automated CI/CD integration initially (avoid false positives)

**Rationale:**
- **Quality over automation**: Manual review prevents broken links
- **One-time cost**: Mapping generation script is written once, run rarely
- **Human judgment needed**: Azure documentation has inconsistencies that require manual intervention
- **Low maintenance burden**: Quarterly updates are manageable for maintainer
- **Script provides scaffolding**: Automation does the tedious work, human does validation

**Out of scope (for now):**
- Automated CI/CD pipeline to refresh mappings on every build
- Real-time scraping or API calls at runtime
- Automated validation of URLs (link checking)

These can be added later if maintenance burden increases.

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

### 7. Update Cadence: Quarterly Manual Updates

**Decision:** Manually update mappings quarterly or when new Azure services are released.

**Update process:**
1. Maintainer runs the discovery script (if available) or manually reviews Azure SDK releases
2. Identifies new resource types or changed documentation URLs
3. Updates `AzureApiDocumentationMappings.json`
4. Commits to main branch or feature branch
5. Releases with next version of tfplan2md

**Rationale:**
- **Azure release cadence**: Azure services don't change URLs frequently
- **Manageable burden**: Quarterly updates take ~1-2 hours, acceptable for maintainer
- **User-driven updates**: Users can contribute mappings via PRs between scheduled updates
- **No automation complexity**: Avoids CI/CD overhead, false positives, and link validation infrastructure

**Trigger for ad-hoc updates:**
- User reports broken or missing mapping
- Major Azure service launch (e.g., new compute service)
- Microsoft restructures documentation URLs (rare but possible)

### 8. Community Contributions: Encouraged with Guidelines

**Decision:** Accept community contributions for new mappings with clear validation guidelines.

**Contribution process:**
1. User opens issue or PR to add missing mapping
2. User provides:
   - Resource type (e.g., `Microsoft.Network/privateEndpoints`)
   - Documentation URL
   - Verification: Screenshot or confirmation that URL is correct
3. Maintainer validates URL and merges PR
4. Mapping included in next release

**Validation guidelines (for contributors and maintainers):**
- URL must be on `learn.microsoft.com/rest/api/*`
- URL must return HTTP 200 (not 404)
- URL must document the exact resource type (not a parent or unrelated resource)
- Prefer stable documentation URLs over preview/versioned URLs

**Documentation:**
- Add `CONTRIBUTING.md` section: "Adding Azure API Documentation Mappings"
- Template PR description for mapping contributions
- Link to Azure SDK Specs Inventory for resource type discovery

**Rationale:**
- **Leverage community knowledge**: Users working with specific Azure resources know the correct URLs
- **Faster coverage expansion**: Don't wait for maintainer to discover every resource type
- **Simple validation**: URL verification is straightforward; low risk of incorrect contributions
- **Engagement**: Community contributions increase project engagement and quality

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

### 5. Discovery Script (Optional but Recommended)

**File:** `scripts/generate-azure-api-mappings.py` (or C#/PowerShell)

**Purpose:** Generate candidate mappings for manual review.

**Approach:**
1. Fetch Azure SDK Specs Inventory HTML page
2. Parse service names and resource types
3. Generate URLs using known patterns:
   - `Microsoft.Compute` → `/rest/api/compute/`
   - Resource type → kebab-case
4. Output JSON file with candidate mappings
5. Maintainer validates and merges

**Out of scope for MVP:** Can be added in a future enhancement if manual curation becomes too time-consuming.

## Migration Strategy

### Phase 1: Infrastructure (Developer)
1. Create `AzureApiDocumentationMappings.json` with 10-20 common resources
2. Implement `AzureApiDocumentationMapper` class
3. Update `AzureApiDocLink()` helper to use mapper
4. Update tests to validate mapping-based behavior
5. Remove "(best-effort)" from template

### Phase 2: Initial Mappings (Maintainer)
1. Curate 50-100 most common Azure resources
2. Manually verify each URL
3. Populate `AzureApiDocumentationMappings.json`
4. Document contribution process in CONTRIBUTING.md

### Phase 3: Community Expansion (Ongoing)
1. Accept community PRs for missing mappings
2. Quarterly reviews to add new Azure services
3. Monitor user feedback for broken or missing links

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
- **Initial curation effort**: Maintainer must populate initial 50-100 mappings (estimated 4-8 hours)
- **Incomplete coverage initially**: Not all 1000+ Azure resources will have mappings on day one
- **Quarterly maintenance**: Requires periodic updates to stay current with Azure releases
- **No automatic discovery**: Users must request missing mappings (or contribute them)

### Risks
- **Microsoft changes URL structure**: Low risk; Microsoft maintains stable documentation URLs. If it happens, mass-update mappings JSON.
- **Outdated mappings**: Quarterly updates may lag behind new Azure services. Mitigated by community contributions.
- **Broken links over time**: Azure may deprecate old services. Mitigated by maintainer reviews and user reports.

## Open Questions (Resolved)

All 8 open questions from the specification have been answered:

1. ✅ **Storage format**: Embedded JSON
2. ✅ **API versioning**: Version-agnostic (strip version suffix)
3. ✅ **Coverage strategy**: Incremental, starting with top 50-100 resources
4. ✅ **Automation level**: Hybrid (semi-automated generation with manual review)
5. ✅ **Fallback behavior**: No link when mapping missing (clean degradation)
6. ✅ **Nested resources**: Individual mappings per level
7. ✅ **Update cadence**: Quarterly manual updates
8. ✅ **Community contributions**: Encouraged with clear validation guidelines

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
2. Create CONTRIBUTING.md section for adding mappings
3. Document validation guidelines for contributors

**For Maintainer:**
1. Curate initial 50-100 common Azure resource mappings
2. Validate URLs manually or with script
3. Review and approve architecture decisions
