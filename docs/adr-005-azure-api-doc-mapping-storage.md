# ADR-005: Embedded JSON for Azure API Documentation Mappings

## Status

Proposed

## Context

Feature 048 introduces official Azure API documentation mappings to replace URL guessing with reliable, curated mappings. A key architectural decision is how to store and load these mappings.

We need to:
- Map Azure resource types (e.g., `Microsoft.Compute/virtualMachines`) to Microsoft Learn documentation URLs
- Support 50-100 mappings initially, potentially growing to several hundred
- Load mappings efficiently at application startup
- Keep mappings synchronized with code releases
- Enable maintainers and community to update mappings easily

## Options Considered

### Option 1: Embedded JSON Resource

Store mappings in a JSON file embedded as a .NET resource in the compiled assembly.

**Pros:**
- **Consistency**: Project already uses this pattern for `AzureRoleDefinitions.json`
- **No external dependencies**: Works offline, no file I/O at runtime
- **Guaranteed availability**: Mappings always present; no missing file errors
- **Fast startup**: Loaded once at startup into `FrozenDictionary<string, string>` for O(1) lookups
- **Versioned with code**: Mappings ship with the application version; no version mismatch
- **Human-readable**: JSON is easy to read and edit
- **Git-friendly**: Clear diffs when mappings change

**Cons:**
- **Requires recompilation**: Updates require releasing new version of tfplan2md
- **No runtime customization**: Users cannot override mappings without rebuilding

**Implementation:**
```csharp
// Follow AzureRoleDefinitionMapper.Roles.cs pattern
private static readonly FrozenDictionary<string, string> Mappings = LoadMappings();

private static FrozenDictionary<string, string> LoadMappings()
{
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = "Oocx.TfPlan2Md.Providers.AzApi.Data.AzureApiDocumentationMappings.json";
    using var stream = assembly.GetManifestResourceStream(resourceName);
    // Deserialize and return FrozenDictionary
}
```

**File:** `src/Oocx.TfPlan2Md/Providers/AzApi/Data/AzureApiDocumentationMappings.json`

### Option 2: External JSON File (Optional User Override)

Store default mappings as embedded resource, but allow users to provide an external JSON file to override or extend mappings.

**Pros:**
- **User customization**: Advanced users could add mappings without rebuilding
- **Flexibility**: Supports edge cases and private Azure services

**Cons:**
- **Added complexity**: Need to handle file loading, path resolution, merging logic
- **Deployment burden**: Users must manage external file alongside Docker image
- **Error-prone**: Missing file, wrong path, or malformed JSON causes runtime failures
- **Unlikely use case**: Users rarely need custom documentation URLs
- **Breaking offline guarantee**: External file may not be available in CI/CD environments

### Option 3: Embedded C# Dictionary (Compile-Time Data)

Store mappings directly in C# code as a static dictionary initializer.

**Pros:**
- **Compile-time checking**: Syntax errors caught at build time
- **No serialization overhead**: Slightly faster startup (negligible difference)
- **Type-safe**: No JSON parsing

**Cons:**
- **Poor maintainability**: Large C# initializers are hard to read and edit
- **Merge conflicts**: High risk of conflicts when multiple contributors add mappings
- **Not diff-friendly**: C# code diffs are noisier than JSON diffs
- **No metadata**: Can't easily track version, last updated, source, etc.
- **Pattern break**: Project uses JSON for AzureRoleDefinitions; C# would be inconsistent

**Example:**
```csharp
private static readonly Dictionary<string, string> Mappings = new()
{
    ["Microsoft.Compute/virtualMachines"] = "https://...",
    ["Microsoft.Storage/storageAccounts"] = "https://...",
    // 100+ more lines...
};
```

### Option 4: CSV File

Store mappings in a CSV file with columns: `ResourceType,DocumentationUrl`.

**Pros:**
- **Simple format**: Easy to edit in spreadsheets
- **Diff-friendly**: Line-based format works well with Git

**Cons:**
- **No structure**: Can't store metadata (version, last updated, source)
- **No future extensibility**: Hard to add additional fields (e.g., description, tags)
- **Limited tooling**: Less support for validation compared to JSON schema
- **Pattern break**: Project uses JSON for structured data

## Decision

**Use Option 1: Embedded JSON Resource.**

Follow the existing pattern established by `AzureRoleDefinitions.json` and `AzureRoleDefinitionMapper.Roles.cs`.

**File structure:**
```json
{
  "mappings": {
    "Microsoft.Compute/virtualMachines": {
      "url": "https://learn.microsoft.com/rest/api/compute/virtual-machines"
    },
    "Microsoft.Storage/storageAccounts": {
      "url": "https://learn.microsoft.com/rest/api/storagerp/storage-accounts"
    }
  },
  "metadata": {
    "version": "1.0.0",
    "lastUpdated": "2025-01-15",
    "source": "Microsoft Learn REST API Documentation",
    "contributors": []
  }
}
```

**Loading:**
```csharp
public static class AzureApiDocumentationMapper
{
    private static readonly FrozenDictionary<string, string> Mappings = LoadMappings();

    public static string? GetDocumentationUrl(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType)) return null;
        var typeWithoutVersion = resourceType.Split('@', 2)[0];
        return Mappings.TryGetValue(typeWithoutVersion, out var url) ? url : null;
    }

    private static FrozenDictionary<string, string> LoadMappings()
    {
        // Follow AzureRoleDefinitionMapper pattern
    }
}
```

## Rationale

### Consistency with Existing Patterns
The project already uses embedded JSON for `AzureRoleDefinitions.json` (Feature 025). Reusing this pattern:
- Reduces cognitive load for developers
- Proves the pattern works well for similar use cases
- Enables code reuse (similar loader implementation)
- Maintains architectural consistency

### Performance Characteristics
- **Startup**: One-time load at application startup (~10ms for 100 mappings)
- **Runtime**: O(1) lookup via `FrozenDictionary<string, string>`
- **Memory**: ~10-50 KB for 100-200 mappings (negligible for modern systems)

### Deployment Simplicity
- **No external files**: Docker image contains everything needed
- **No configuration**: Works out of the box, no environment variables or file paths
- **CI/CD friendly**: Runs in any environment without file system dependencies

### Maintainability
- **JSON is human-readable**: Easy to review in PRs
- **Git diffs are clear**: Line-by-line changes visible
- **Schema validation possible**: Can add JSON schema for IDE support
- **Metadata support**: Can track version, last updated date, contributors

### Extensibility
JSON structure allows future enhancements without breaking changes:
- Add `"description"` field for resource type explanations
- Add `"tags"` for categorization or filtering
- Add version-specific overrides if needed
- Add deprecation warnings or preview indicators

## Consequences

### Positive
- **Zero deployment complexity**: Mappings always available, no missing file errors
- **Consistent with project patterns**: Follows AzureRoleDefinitions precedent
- **Fast and efficient**: FrozenDictionary provides optimal lookup performance
- **Offline-capable**: No external dependencies or network calls required
- **Version-locked**: Mappings ship with code version; no mismatch issues
- **Community-friendly**: JSON is easy for contributors to edit

### Negative
- **Requires new release for updates**: Can't hotfix mappings without releasing new version
- **No user customization**: Users can't add private Azure service mappings (but this is an unlikely need)
- **Compilation dependency**: Mappings embedded at build time

### Mitigations
- **Regular releases**: Update mappings quarterly or when Azure releases major services
- **Community contributions**: Users can submit PRs for missing mappings
- **Fast release cycle**: CI/CD pipeline enables quick releases when needed
- **Future enhancement option**: Can add external file override in future if users request it

## Implementation Notes

### Project File Change
```xml
<ItemGroup>
  <EmbeddedResource Include="Providers/AzApi/Data/AzureApiDocumentationMappings.json" />
</ItemGroup>
```

### Resource Naming Convention
Follow .NET embedded resource naming: `{RootNamespace}.{RelativePath.ReplaceSlashWithDot}`

Example: `Oocx.TfPlan2Md.Providers.AzApi.Data.AzureApiDocumentationMappings.json`

### Error Handling
- **Missing resource**: Throw `InvalidOperationException` at startup (fail-fast)
- **Malformed JSON**: Throw `InvalidOperationException` at startup (fail-fast)
- **Empty mappings**: Throw `InvalidOperationException` (catch during development)

### Testing
- Unit test: `LoadMappings()` succeeds and returns non-empty dictionary
- Unit test: `GetDocumentationUrl()` returns correct URLs for known types
- Unit test: `GetDocumentationUrl()` returns null for unknown types
- Unit test: API version suffix is stripped before lookup

## Related Decisions
- **ADR-001**: Scriban for templating (affected by how we expose mappings to templates)
- **Feature 025**: Azure Role Definition Mapping (established the embedded JSON pattern)
- **Feature 048**: Azure API Documentation Mapping (this ADR implements storage for this feature)

## References
- Feature Specification: `docs/features/048-azure-api-doc-mapping/specification.md`
- Architecture Document: `docs/features/048-azure-api-doc-mapping/architecture.md`
- Existing Pattern: `src/Oocx.TfPlan2Md/Platforms/Azure/AzureRoleDefinitionMapper.Roles.cs`
- Microsoft Learn: https://learn.microsoft.com/rest/api/azure/
