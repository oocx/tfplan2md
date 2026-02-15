# Architecture: Provider Code Separation

## Status

Proposed

## Context

The feature specification ([specification.md](specification.md)) requests restructuring the codebase to clearly separate:
1. **Provider-specific code** - Code tied to Terraform providers (azurerm, azapi, azuredevops)
2. **Output platform-specific code** - Code for rendering to specific platforms (GitHub PR vs Azure DevOps PR)

### Current State

Provider-specific code is currently scattered across multiple locations:

| Location | Description | Files |
|----------|-------------|-------|
| `MarkdownGeneration/Templates/azurerm/` | Templates for azurerm resources | 3 templates |
| `MarkdownGeneration/Templates/azapi/` | Templates for azapi resources | 1 template |
| `MarkdownGeneration/Templates/azuredevops/` | Templates for azuredevops resources | 1 template |
| `MarkdownGeneration/Helpers/ScribanHelpers.AzApi.*.cs` | AzApi-specific helper functions | 9 files |
| `MarkdownGeneration/Helpers/ScribanHelpers.Azure.cs` | Azure role/scope helpers | 1 file |
| `MarkdownGeneration/Models/*ViewModel*.cs` | Provider-specific view models | 8 files |
| `Azure/` | Azure principal/role mapping services | 12 files |
| `MarkdownGeneration/Models/ResourceViewModelFactoryRegistry.cs` | Explicit factory registration | 1 file |

### Constraints

From the specification and project requirements:
- **AOT Compatible**: Must use explicit registration, no reflection
- **Single Assembly**: All code remains in `Oocx.TfPlan2Md.csproj`
- **Namespace Alignment**: Namespaces must match folder structure
- **Behavioral Preservation**: Existing functionality must work identically

## Options Considered

### Option 1: Flat Provider Folders at Root Level

Create provider folders directly under `src/Oocx.TfPlan2Md/`:

```
src/Oocx.TfPlan2Md/
├── Providers/
│   ├── AzureRM/
│   │   ├── Templates/               # .sbn files
│   │   ├── Helpers/                 # ScribanHelpers partial classes
│   │   ├── ViewModels/              # ViewModel + Factory pairs
│   │   └── Registration.cs          # Provider registration
│   ├── AzApi/
│   │   ├── Templates/
│   │   ├── Helpers/
│   │   └── Registration.cs
│   └── AzureDevOps/
│       ├── Templates/
│       ├── ViewModels/
│       └── Registration.cs
├── RenderTargets/
│   ├── GitHub/
│   │   └── (currently empty - for future render target-specific code)
│   └── AzureDevOps/
│       └── (currently empty - for future render target-specific code)
├── Azure/                           # Shared Azure utilities (PrincipalMapper, etc.)
├── Core/
│   ├── Parsing/                     # Moved from Parsing/
│   ├── MarkdownGeneration/          # Core rendering logic
│   └── CLI/                         # Moved from CLI/
└── Program.cs
```

**Pros:**
- Clear visual separation of provider code
- Providers at top-level are easy to discover
- Natural place for future providers

**Cons:**
- Significant restructuring (many file moves)
- Changes existing folder patterns developers are familiar with
- Introduces "Core" folder adding another abstraction layer

### Option 2: Provider Subfolders Within MarkdownGeneration

Keep the current structure but consolidate provider code within `MarkdownGeneration/`:

```
src/Oocx.TfPlan2Md/
├── Azure/                           # Keep existing Azure utilities
├── CLI/                             # Keep existing CLI
├── Parsing/                         # Keep existing Parsing
├── MarkdownGeneration/
│   ├── Core/                        # Generic code
│   │   ├── ReportModelBuilder.*.cs
│   │   ├── MarkdownRenderer.cs
│   │   ├── TemplateLoader.cs
│   │   ├── TemplateResolver.cs
│   │   ├── Helpers/                 # Generic helpers only
│   │   └── Templates/               # Generic templates (_resource.sbn, etc.)
│   ├── Providers/
│   │   ├── IProviderRegistration.cs
│   │   ├── ProviderRegistry.cs
│   │   ├── AzureRM/
│   │   │   ├── Templates/
│   │   │   ├── Helpers/
│   │   │   ├── ViewModels/
│   │   │   └── AzureRMProvider.cs   # Registration
│   │   ├── AzApi/
│   │   │   ├── Templates/
│   │   │   ├── Helpers/
│   │   │   └── AzApiProvider.cs
│   │   └── AzureDevOps/
│   │       ├── Templates/
│   │       ├── ViewModels/
│   │       └── AzureDevOpsProvider.cs
│   └── RenderTargets/
│       ├── GitHub/
│       └── AzureDevOps/
└── Program.cs
```

**Pros:**
- Providers logically grouped within markdown generation (their main purpose)
- Minimal changes to top-level folder structure
- Clear Core vs Provider separation within MarkdownGeneration

**Cons:**
- Azure/ folder remains separate (it serves both providers and core)
- Deeper nesting may be harder to navigate
- RenderTargets under MarkdownGeneration may be confusing

### Option 3: Hybrid Approach with Provider Folders at Top Level (Recommended)

Create `Providers/` and `RenderTargets/` at the top level, but keep shared infrastructure in existing locations:

```
src/Oocx.TfPlan2Md/
├── Platforms/                       # NEW: Cloud platform utilities (shared across providers)
│   └── Azure/                       # Azure platform utilities
│       ├── AzureRoleDefinitionMapper.cs
│       ├── AzureScopeParser.cs
│       ├── PrincipalMapper.cs
│       └── ...
├── CLI/                             # KEEP: CLI code
├── Parsing/                         # KEEP: Parsing code
├── MarkdownGeneration/              # KEEP: Core rendering logic
│   ├── Helpers/                     # Generic helpers only (refactored)
│   │   ├── ScribanHelpers.Markdown.cs
│   │   ├── ScribanHelpers.DiffFormatting.cs
│   │   ├── ScribanHelpers.Json.cs
│   │   └── ...
│   ├── Models/                      # Generic models only
│   │   ├── IResourceViewModelFactory.cs
│   │   ├── FormattedValue.cs
│   │   └── ...
│   ├── Templates/                   # Generic templates only
│   │   ├── default.sbn
│   │   ├── summary.sbn
│   │   ├── _resource.sbn
│   │   ├── _header.sbn
│   │   └── _summary.sbn
│   ├── ReportModelBuilder.*.cs
│   ├── MarkdownRenderer.cs
│   └── ...
├── Providers/                       # NEW: All provider-specific code
│   ├── IProviderModule.cs           # Interface for provider registration
│   ├── ProviderRegistry.cs          # Explicit registration of all providers
│   ├── AzureRM/
│   │   ├── Templates/
│   │   │   ├── firewall_network_rule_collection.sbn
│   │   │   ├── network_security_group.sbn
│   │   │   └── role_assignment.sbn
│   │   ├── Helpers/
│   │   │   └── AzureRMHelpers.cs    # Azure-specific Scriban helpers
│   │   ├── ViewModels/
│   │   │   ├── FirewallNetworkRuleCollectionViewModel.cs
│   │   │   ├── NetworkSecurityGroupViewModel.cs
│   │   │   └── RoleAssignmentViewModel.cs
│   │   └── AzureRMModule.cs         # Provider registration
│   ├── AzApi/
│   │   ├── Templates/
│   │   │   └── resource.sbn
│   │   ├── Helpers/
│   │   │   ├── AzApiHelpers.cs      # Consolidated from ScribanHelpers.AzApi.*.cs
│   │   │   ├── AzApiMetadata.cs
│   │   │   ├── AzApiRendering.cs
│   │   │   └── JsonFlattener.cs
│   │   └── AzApiModule.cs
│   └── AzureDevOps/
│       ├── Templates/
│       │   └── variable_group.sbn
│       ├── ViewModels/
│       │   ├── VariableGroupViewModel.cs
│       │   └── VariableGroupViewModelFactory.cs
│       └── AzureDevOpsModule.cs
├── RenderTargets/                   # NEW: Rendering target-specific code
│   ├── GitHub/
│   │   └── (placeholder for future GitHub-specific rendering)
│   └── AzureDevOps/
│       └── (placeholder for future Azure DevOps-specific rendering)
└── Program.cs
```

**Pros:**
- Clean separation at top level without excessive nesting
- Existing infrastructure (CLI, Parsing, MarkdownGeneration core) remains in place
- Providers are clearly visible and easy to navigate
- Platforms folder enables future cloud platform support (AWS, GCP)
- RenderTargets ready for future rendering target-specific code
- Minimal disruption to existing patterns

**Cons:**
- Templates move from embedded location in MarkdownGeneration to Providers
- Need to update template loading to check both core and provider locations
- Azure/ folder moves to Platforms/Azure/ (namespace change)

## Decision

**Option 3: Hybrid Approach with Provider Folders at Top Level**

This option provides the best balance of:
- Clear code organization for developers
- Minimal disruption to existing patterns
- Scalability for adding new providers
- Separation of concerns without over-engineering

## Rationale

### Why Option 3 over Option 1?

Option 1 introduces a "Core" folder that would require moving CLI and Parsing code, which adds unnecessary churn. The CLI and Parsing components are already well-organized and don't need restructuring.

### Why Option 3 over Option 2?

Option 2 buries providers deep in the folder hierarchy. Since providers are a key organizational concept (the primary goal of this feature), they deserve top-level visibility.

### Platforms vs Azure Folder

The `Platforms/` folder contains shared cloud platform utilities (not Terraform provider code):

| Folder | Contains | Used By |
|--------|----------|---------|
| `Platforms/Azure/` | Principal mapping, role definitions, scope parsing | azurerm provider, azapi provider, core rendering |
| `Providers/AzureRM/` | Templates, view models, helpers for `azurerm_*` resources | azurerm templates only |

This distinction matters because:
- Multiple Terraform providers may target the same cloud platform (azurerm and azapi both target Azure)
- Platform utilities like `PrincipalMapper` don't know which Terraform provider triggered the lookup
- Future cloud platforms (AWS, GCP) would follow the same pattern: `Platforms/AWS/`, `Providers/AWS/`

### Provider Registration Design

The specification requires "explicit registration of providers in core startup code (no reflection)." The current `ResourceViewModelFactoryRegistry` already uses this pattern. The new design extends this:

```csharp
// IProviderModule.cs
internal interface IProviderModule
{
    void RegisterViewModelFactories(ResourceViewModelFactoryRegistry registry);
    void RegisterScribanHelpers(ScriptObject scriptObject);
    IEnumerable<string> GetTemplateProviders(); // Returns ["azurerm", "azapi", etc.]
}

// ProviderRegistry.cs
internal sealed class ProviderRegistry
{
    private readonly List<IProviderModule> _modules =
    [
        new AzureRMModule(),
        new AzApiModule(),
        new AzureDevOpsModule()
    ];
    
    // ... registration methods
}
```

This design:
- Keeps AOT compatibility (no reflection)
- Makes provider addition explicit and discoverable
- Groups all provider components under a single registration point

### Template Resolution

Templates will be loaded from provider-specific embedded resources:
- Core templates: `Oocx.TfPlan2Md.MarkdownGeneration.Templates.{name}.sbn`
- Provider templates: `Oocx.TfPlan2Md.Providers.{Provider}.Templates.{resource}.sbn`

The `TemplateLoader` will be updated to check both locations.

### RenderTargets Structure

Render target-specific code **already exists** in the codebase, embedded within the diff formatting helpers:

| Current Location | Purpose | Render Target |
|-----------------|---------|---------------|
| `ScribanHelpers.DiffFormatting.cs` → `WrapInlineDiffCode()` | Inline styles for diffs | Azure DevOps |
| `ScribanHelpers.DiffFormatting.cs` → `BuildSimpleDiffTable()` | Simple markdown diffs | GitHub |
| `LargeValueFormat` enum | Encodes the platform choice | Both |

GitHub does not support inline `style` attributes in markdown, so the `SimpleDiff` format uses basic markdown (`- \`old\`<br>+ \`new\``) while Azure DevOps can use richer HTML styling.

The `RenderTargets/` folder will consolidate this logic:

```
RenderTargets/
├── GitHub/
│   └── GitHubDiffFormatter.cs       # SimpleDiff implementation
├── AzureDevOps/
│   └── AzureDevOpsDiffFormatter.cs  # InlineDiff implementation
└── IDiffFormatter.cs                # Shared interface
```

This prepares the codebase for additional render target differences:
- Collapsible section syntax variations
- Emoji rendering differences
- HTML element support differences

### CLI Change: `--render-target` Instead of `--large-value-format`

As part of this feature, the CLI will be updated to let users specify the render target directly instead of the technical diff format:

**Current CLI (deprecated):**
```bash
tfplan2md plan.json --large-value-format simple-diff
tfplan2md plan.json --large-value-format inline-diff
```

**New CLI:**
```bash
tfplan2md plan.json --render-target github
tfplan2md plan.json --render-target azuredevops   # or: azdo
```

**Design:**
- The `--render-target` option selects the appropriate rendering strategy for the target platform
- Default remains Azure DevOps (current behavior: `InlineDiff`)
- `--large-value-format` is removed (breaking change permitted as product is not yet released)
- Each render target can define multiple rendering behaviors (diff format, collapsible sections, etc.)

**Benefits:**
- Users specify *where* output renders, not *how* (better UX)
- Easier to add future platform-specific behaviors without new CLI flags
- Clearer mental model for users

## Consequences

### Positive

- Clear code organization improves developer experience
- Adding new providers follows an obvious pattern
- Provider boundaries are explicit, reducing unintended coupling
- Existing tests continue to work with updated namespaces
- AOT compatibility maintained

### Negative

- One-time migration effort for moving files
- Namespace changes require updating `InternalsVisibleTo` if new namespaces added
- Template resolution logic becomes slightly more complex
- Some Scriban helper functions may need refactoring if they combine provider-specific and generic logic

### Risks

| Risk | Mitigation |
|------|------------|
| Breaking embedded resource paths | Comprehensive integration tests verify template loading |
| Namespace refactoring breaks tests | Update `InternalsVisibleTo` attributes as needed |
| Complex inter-provider dependencies | Platforms/ folder provides shared utilities; clear boundaries documented |

## Implementation Notes

### Component Mapping

| Current Location | New Location | Namespace |
|-----------------|--------------|-----------|
| `Azure/*` | `Platforms/Azure/*` | `Oocx.TfPlan2Md.Platforms.Azure` |
| `MarkdownGeneration/Templates/azurerm/*` | `Providers/AzureRM/Templates/*` | N/A (embedded resources) |
| `MarkdownGeneration/Templates/azapi/*` | `Providers/AzApi/Templates/*` | N/A (embedded resources) |
| `MarkdownGeneration/Templates/azuredevops/*` | `Providers/AzureDevOps/Templates/*` | N/A (embedded resources) |
| `MarkdownGeneration/Helpers/ScribanHelpers.AzApi.*.cs` | `Providers/AzApi/Helpers/AzApi*.cs` | `Oocx.TfPlan2Md.Providers.AzApi` |
| `MarkdownGeneration/Helpers/ScribanHelpers.Azure.cs` | `Providers/AzureRM/Helpers/AzureRMHelpers.cs` | `Oocx.TfPlan2Md.Providers.AzureRM` |
| `MarkdownGeneration/Models/FirewallNetworkRuleCollection*.cs` | `Providers/AzureRM/ViewModels/*` | `Oocx.TfPlan2Md.Providers.AzureRM.ViewModels` |
| `MarkdownGeneration/Models/NetworkSecurityGroup*.cs` | `Providers/AzureRM/ViewModels/*` | `Oocx.TfPlan2Md.Providers.AzureRM.ViewModels` |
| `MarkdownGeneration/Models/RoleAssignment*.cs` | `Providers/AzureRM/ViewModels/*` | `Oocx.TfPlan2Md.Providers.AzureRM.ViewModels` |
| `MarkdownGeneration/Models/VariableGroup*.cs` | `Providers/AzureDevOps/ViewModels/*` | `Oocx.TfPlan2Md.Providers.AzureDevOps.ViewModels` |
| `MarkdownGeneration/Models/ResourceViewModelFactoryRegistry.cs` | `Providers/ProviderRegistry.cs` | `Oocx.TfPlan2Md.Providers` |
| `MarkdownGeneration/Helpers/ScribanHelpers.DiffFormatting.cs` (partial) | `RenderTargets/GitHub/GitHubDiffFormatter.cs` | `Oocx.TfPlan2Md.RenderTargets.GitHub` |
| `MarkdownGeneration/Helpers/ScribanHelpers.DiffFormatting.cs` (partial) | `RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs` | `Oocx.TfPlan2Md.RenderTargets.AzureDevOps` |
| `MarkdownGeneration/LargeValueFormat.cs` | `RenderTargets/LargeValueFormat.cs` | `Oocx.TfPlan2Md.RenderTargets` |

### Files to Keep in Current Location (Generic/Shared)

- `Platforms/Azure/*` - Shared Azure platform utilities (used by both core rendering and Azure providers)
- `MarkdownGeneration/Helpers/ScribanHelpers.Markdown.cs` - Generic markdown escaping
- `MarkdownGeneration/Helpers/ScribanHelpers.DiffFormatting.cs` - Generic diff formatting
- `MarkdownGeneration/Helpers/ScribanHelpers.CodeFormatting.cs` - Generic code formatting
- `MarkdownGeneration/Helpers/ScribanHelpers.Json.cs` - Generic JSON utilities
- `MarkdownGeneration/Helpers/ScribanHelpers.LargeValues.cs` - Generic large value handling
- `MarkdownGeneration/Templates/default.sbn` - Main report template
- `MarkdownGeneration/Templates/_resource.sbn` - Fallback resource template
- All `ReportModelBuilder.*.cs` files - Core model building

### Updating TemplateLoader

The `TemplateLoader` needs to support multiple embedded resource prefixes:

```csharp
// Template resolution order:
// 1. User-provided custom template directory (if specified)
// 2. Provider-specific embedded resource (e.g., Providers.AzureRM.Templates)
// 3. Core embedded resource (e.g., MarkdownGeneration.Templates._resource)
```

### Testing Approach

1. All existing tests must pass after migration
2. Add tests verifying template resolution from new locations
3. Verify embedded resource names are correct via integration tests
4. AOT publish test must continue to succeed

### Documentation Updates

- Update `docs/architecture.md` with new folder structure
- Create `Providers/README.md` explaining how to add a new provider
- Update developer onboarding documentation

## References

- [Feature Specification](specification.md)
- [ADR-001: Scriban Templating](../../adr-001-scriban-templating.md)
- [ADR-003: Modern C# Patterns](../../adr-003-modern-csharp-patterns.md)
- [Project Architecture](../../architecture.md)
