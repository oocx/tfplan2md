# Architecture: Azure AD App Role Assignment Support

## Status

No new architectural patterns required — this feature reuses established patterns.

## Analysis

This feature adds human-readable summary display and GUID-to-name resolution for three Azure AD resource types: `azuread_app_role_assignment`, `azuread_directory_role_assignment`, and `azuread_service_principal_delegated_permission_grant`. Every integration point has an existing, well-documented pattern that should be followed directly:

| Concern | Existing Pattern | What This Feature Does |
|---------|-----------------|----------------------|
| GUID → name mapping (static, built-in) | `AzureRoleDefinitionsRegistry` + `AzureRoleDefinitions.json` → `FrozenDictionary` | New `MicrosoftGraphAppRolesRegistry` + `MicrosoftGraphAppRoles.json` → `FrozenDictionary` |
| Resolver interface + implementation | `IRoleDefinitionResolver` / `AzureRoleDefinitionResolver` | New `IAppRoleResolver` / `MicrosoftGraphAppRoleResolver` |
| Resolver info record | `RoleDefinitionInfo(Name, Id, FullName)` | Reuse `RoleDefinitionInfo` directly (same semantics: Name, Id, FullName) |
| Summary builder for resource type | `AzureAdSummaryBuilder.Groups.cs` partial class | New `AzureAdSummaryBuilder.AppRoleAssignments.cs` partial class with 3 builder methods |
| Summary factory registration | `AzureADModule.RegisterFactories()` | Add `azuread_app_role_assignment`, `azuread_directory_role_assignment`, and `azuread_service_principal_delegated_permission_grant` registrations |
| Value formatter implementation | `RoleDefinitionFormatter`, `PrincipalIdFormatter` | New `AppRoleIdFormatter` + reuse `PrincipalIdFormatter` |
| Value formatter registration | `AzureRmValueFormatterRegistration` | New registration logic in `AzureADModule.RegisterValueFormatters()` scoped to all `azuread` resources |
| Embedded JSON source generation | `AdditionalFiles` + `EmbedAsJson="true"` in `.csproj` | Add new entry for `MicrosoftGraphAppRoles.json` |
| JSON serialization context | `AzureRoleDefinitionsJsonContext` | New `MicrosoftGraphAppRolesJsonContext` (identical pattern) |
| `EmbeddedJsonPayloads` accessor | `AzureRoleDefinitions` property | New `MicrosoftGraphAppRoles` property |
| Icon registration | `azuread-icons.json` rules | 6 icon rules: 🔑 `app_role_id`, 👤 `principal_object_id`, 🎯 `resource_object_id`, 💻 `service_principal_object_id`, 🛡️ `role_definition_id`, 📋 `claim_values` |
| Resource renderer | `AzureAdDelegatingRenderer` subclasses | New `AppRoleAssignmentRenderer`, `DirectoryRoleAssignmentRenderer`, `DelegatedPermissionGrantRenderer` subclasses |
| Provider module wiring | `AzureADModule` constructor + `CompositionRoot.CreateProviderRegistry()` | Pass `IAppRoleResolver` to `AzureADModule`, wire in `CompositionRoot` |
| Provider fallback summary keys | `ResourceSummaryMappings.ProviderFallbacks` | `azuread` provider fallback includes `display_name` and `principal_object_id` |
| Resource-specific summary keys | `ResourceSummaryMappings.ResourceMappings` | Entries for all 3 resource types with their relevant attribute keys |

Because **all** the infrastructure patterns already exist, this feature is a straightforward extension using the established architecture. No new architectural decisions are needed.

## Implementation Guidance

### 1. App Role Resolver (New Components)

**Location:** `src/Oocx.TfPlan2Md/Platforms/Azure/`

Follow the `IRoleDefinitionResolver` / `AzureRoleDefinitionResolver` pattern exactly:

#### 1a. Interface: `IAppRoleResolver`

```
File: src/Oocx.TfPlan2Md/Platforms/Azure/IAppRoleResolver.cs
```

- Define an `internal interface IAppRoleResolver`
- Method: `RoleDefinitionInfo GetAppRole(string? appRoleId)` — returns the same `RoleDefinitionInfo` record used by RBAC roles
- Method: `string GetAppRoleName(string? appRoleId)` — convenience method returning `FullName`
- Reusing `RoleDefinitionInfo` is intentional: the semantics (Name, Id, FullName) are identical for app roles

#### 1b. Implementation: `MicrosoftGraphAppRoleResolver`

```
File: src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoleResolver.cs
```

- `internal sealed class MicrosoftGraphAppRoleResolver : IAppRoleResolver`
- Constructor takes `FrozenDictionary<string, string>` built-in roles (loaded from `MicrosoftGraphAppRolesRegistry`)
- Uses `FrozenDictionary<string, string>` with `OrdinalIgnoreCase` comparer (same as `AzureRoleDefinitionResolver`)
- `GetAppRole()` looks up the GUID, returns `RoleDefinitionInfo(name, guid, $"{name} ({guid}")` when found, or `RoleDefinitionInfo(guid, guid, guid)` when not found
- Provide a static `CreateBuiltIn()` factory method for default construction (same pattern as `AzureRoleDefinitionResolver.CreateBuiltIn()`)
- **Simpler than `AzureRoleDefinitionResolver`**: no custom-role merging needed (app roles are globally stable for Microsoft Graph), no `/subscriptions/.../roleDefinitions/` GUID extraction needed (app role IDs are bare GUIDs)

#### 1c. Registry: `MicrosoftGraphAppRolesRegistry`

```
File: src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesRegistry.cs
```

- `internal static class MicrosoftGraphAppRolesRegistry`
- Single method `Load()` returning `FrozenDictionary<string, string>`
- Loads from `EmbeddedJsonPayloads.MicrosoftGraphAppRoles`
- Uses a new `MicrosoftGraphAppRolesJsonContext` for AOT-safe deserialization

#### 1d. JSON Context: `MicrosoftGraphAppRolesJsonContext`

```
File: src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesJsonContext.cs
```

- `[JsonSerializable(typeof(Dictionary<string, string>))]`
- `internal partial class MicrosoftGraphAppRolesJsonContext : JsonSerializerContext`

### 2. Embedded JSON Data File

**Location:** `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json`

- Simple `{ "guid": "PermissionName" }` structure (same as `AzureRoleDefinitions.json`)
- Include **all** Microsoft Graph app roles (comprehensive, sourced via `az ad sp show --id 00000003-0000-0000-c000-000000000000`)
- This is consistent with `AzureRoleDefinitions.json` which contains ~500 Azure RBAC roles

### 3. Build Configuration

**File:** `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj`

Add to the existing `AdditionalFiles` ItemGroup:

```xml
<AdditionalFiles Include="Platforms/Azure/MicrosoftGraphAppRoles.json" EmbedAsJson="true" />
```

### 4. EmbeddedJsonPayloads Update

**File:** `src/Oocx.TfPlan2Md/EmbeddedJsonPayloads.cs`

Add a new property:

```
internal static ReadOnlySpan<byte> MicrosoftGraphAppRoles => global::EmbeddedJsonResources.MicrosoftGraphAppRoles.GetBytes();
```

The source generator (`JsonEmbedGenerator`) will auto-generate the `EmbeddedJsonResources.MicrosoftGraphAppRoles` class from the `AdditionalFiles` entry.

### 5. Summary Builder Extension

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.AppRoleAssignments.cs` (new partial class file)

Contains three builder methods for the three resource types:

#### 5a. `BuildAppRoleAssignmentSummaryHtml`

- Constant `AppRoleAssignmentResourceType = "azuread_app_role_assignment"`
- Method `BuildAppRoleAssignmentSummaryHtml(ResourceChangeModel, object? state, IPrincipalMapper, IAppRoleResolver?, IconProviderRegistry?)`
- **Summary format:** `{principal} → {role} → {resource}`

**Resolution order** (per specification):

| Component | Attribute | Resolution |
|-----------|-----------|------------|
| `{role}` | `app_role_id` | 1. `IAppRoleResolver.GetAppRole()` → Name; 2. Raw GUID |
| `{principal}` | `principal_object_id` | 1. `IPrincipalMapper.GetName()` → display name; 2. Computed `principal_display_name` from state; 3. Raw GUID |
| `{resource}` | `resource_object_id` | 1. `IPrincipalMapper.GetName()` → display name; 2. Computed `resource_display_name` from state; 3. Raw GUID |

#### 5b. `BuildDirectoryRoleAssignmentSummaryHtml`

- Constant `DirectoryRoleAssignmentResourceType = "azuread_directory_role_assignment"`
- Method `BuildDirectoryRoleAssignmentSummaryHtml(ResourceChangeModel, object? state, IPrincipalMapper, IconProviderRegistry?)`
- **Summary format:** `{principal} → {role_definition_id}`

| Component | Attribute | Resolution |
|-----------|-----------|------------|
| `{principal}` | `principal_object_id` | 1. `IPrincipalMapper.GetName()` → display name; 2. Raw GUID |
| `{role_definition_id}` | `role_definition_id` | Raw value (no built-in resolution) |

#### 5c. `BuildDelegatedPermissionGrantSummaryHtml`

- Constant `DelegatedPermissionGrantResourceType = "azuread_service_principal_delegated_permission_grant"`
- Method `BuildDelegatedPermissionGrantSummaryHtml(ResourceChangeModel, object? state, IPrincipalMapper, IconProviderRegistry?)`
- **Summary format:** `{service_principal} → {claims} → {resource}`

| Component | Attribute | Resolution |
|-----------|-----------|------------|
| `{service_principal}` | `service_principal_object_id` | 1. `IPrincipalMapper.GetName()` → display name; 2. Raw GUID |
| `{claims}` | `claim_values` | Joined array values, or `(no claims)` when empty |
| `{resource}` | `resource_object_id` | 1. `IPrincipalMapper.GetName()` → display name; 2. Raw GUID |

### 6. Summary Factory Updates

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryFactory.cs`

- The factory needs access to `IAppRoleResolver` to pass it to the summary builder
- Add an `IAppRoleResolver` field to `AzureAdSummaryFactory` (constructor-injected)
- Pass it through to `AzureAdSummaryBuilder.BuildSummaryHtml()`

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.cs` (main partial)

- Add `IAppRoleResolver` parameter to `BuildSummaryHtml()` signature
- Add the `azuread_app_role_assignment` dispatch case calling `BuildAppRoleAssignmentSummaryHtml()`

### 7. AzureADModule Updates

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/AzureADModule.cs`

#### Constructor changes:
- Add `IAppRoleResolver?` and `IPrincipalMapper?` parameters (optional, defaulting to null/built-in)
- Store as fields: `_appRoleResolver`, `_principalMapper`

#### `RegisterFactories()`:
- Pass `_appRoleResolver` to `AzureAdSummaryFactory` constructor
- Register three resource types:
  - `azuread_app_role_assignment`
  - `azuread_directory_role_assignment`
  - `azuread_service_principal_delegated_permission_grant`

#### `RegisterValueFormatters()`:
- Register an `AppRoleIdFormatter` for `app_role_id` attribute scoped to **all `azuread` resources** (provider pattern `(^azuread$|.*/azuread$)`)
- Register a `PrincipalIdFormatter` for `principal_object_id` and `resource_object_id` attributes scoped to all `azuread` resources
- Use match patterns scoped to `azuread` provider (not individual resource types) for broader coverage

#### `RegisterResourceRenderers()`:
- Add three renderers:
  - `registry.Register(new AppRoleAssignmentRenderer());`
  - `registry.Register(new DirectoryRoleAssignmentRenderer());`
  - `registry.Register(new DelegatedPermissionGrantRenderer());`

### 8. Value Formatter: `AppRoleIdFormatter`

**File:** `src/Oocx.TfPlan2Md/Platforms/Azure/AppRoleIdFormatter.cs`

- `internal sealed class AppRoleIdFormatter : IValueFormatter`
- Constructor takes `IAppRoleResolver?` (defaults to `MicrosoftGraphAppRoleResolver.CreateBuiltIn()`)
- `TryFormat()` resolves the GUID via `IAppRoleResolver.GetAppRole()`, formats as `🔑 {Name} ({GUID})` (matching `RoleDefinitionFormatter` format: icon + FullName)
- Returns `null` if the GUID cannot be resolved (raw value displayed)

### 9. Resource Renderers

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Renderers/AzureAdResourceRenderers.cs`

Add three renderer classes (following the established pattern):

```
internal sealed class AppRoleAssignmentRenderer : AzureAdDelegatingRenderer
{
    public AppRoleAssignmentRenderer() : base("azuread_app_role_assignment") { }
}

internal sealed class DirectoryRoleAssignmentRenderer : AzureAdDelegatingRenderer
{
    public DirectoryRoleAssignmentRenderer() : base("azuread_directory_role_assignment") { }
}

internal sealed class DelegatedPermissionGrantRenderer : AzureAdDelegatingRenderer
{
    public DelegatedPermissionGrantRenderer() : base("azuread_service_principal_delegated_permission_grant") { }
}
```

### 10. Icon Registration

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Icons/azuread-icons.json`

Add 6 rules for assignment-related attributes:

| Attribute Pattern | Icon | Description |
|-------------------|------|-------------|
| `^app_role_id$` | 🔑 | App role permission being granted |
| `^principal_object_id$` | 👤 | Principal receiving the assignment |
| `^resource_object_id$` | 🎯 | Target resource API |
| `^service_principal_object_id$` | 💻 | Service principal (delegated permission grants) |
| `^role_definition_id$` | 🛡️ | Directory role being assigned |
| `^claim_values$` | 📋 | Delegated permission scopes/claims |

These are attribute-level rules (no `resourceTypePattern`), so they apply across all Azure AD resource types.

### 11. CompositionRoot Wiring

**File:** `src/Oocx.TfPlan2Md/CompositionRoot.cs`

#### Add `CreateAppRoleResolver()` method:
- Returns `IAppRoleResolver` (new `MicrosoftGraphAppRoleResolver`)
- No mapping file needed — app role IDs are globally stable for Microsoft Graph

#### Update `CreateProviderRegistry()`:
- Add `IAppRoleResolver appRoleResolver` parameter
- Pass `appRoleResolver` and `principalMapper` to `new AzureADModule(entityMapper, principalMapper, appRoleResolver)`

#### Update `ComposeServices()`:
- Create `appRoleResolver` via `CreateAppRoleResolver()`
- Pass to `CreateProviderRegistry()`

## Components Affected

### New Files

| File | Purpose |
|------|---------|
| `src/Oocx.TfPlan2Md/Platforms/Azure/IAppRoleResolver.cs` | Interface for app role GUID resolution |
| `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoleResolver.cs` | Built-in resolver using frozen dictionary |
| `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesRegistry.cs` | Loads embedded JSON into frozen dictionary |
| `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesJsonContext.cs` | AOT-safe JSON serialization context |
| `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json` | Embedded JSON mapping GUIDs to permission names |
| `src/Oocx.TfPlan2Md/Platforms/Azure/AppRoleIdFormatter.cs` | Value formatter for `app_role_id` detail table cells |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.AppRoleAssignments.cs` | Summary builder for app role, directory role, and delegated permission grant resources |

### Modified Files

| File | Changes |
|------|---------|
| `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj` | Add `AdditionalFiles` entry for `MicrosoftGraphAppRoles.json` |
| `src/Oocx.TfPlan2Md/EmbeddedJsonPayloads.cs` | Add `MicrosoftGraphAppRoles` property |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/AzureADModule.cs` | Add constructor params, register factory/formatters/renderer for 3 resource types |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryFactory.cs` | Accept `IAppRoleResolver` in constructor, pass to builder |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.cs` | Add `IAppRoleResolver` parameter to `BuildSummaryHtml()`, add 3 dispatch cases |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Renderers/AzureAdResourceRenderers.cs` | Add `AppRoleAssignmentRenderer`, `DirectoryRoleAssignmentRenderer`, `DelegatedPermissionGrantRenderer` classes |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Icons/azuread-icons.json` | Add 6 icon rules for assignment attributes |
| `src/Oocx.TfPlan2Md/CompositionRoot.cs` | Create and wire `IAppRoleResolver`, update `CreateProviderRegistry()` |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryMappings.cs` | Add `azuread` provider fallback and 3 resource-specific summary key entries |

### Test Files (to be created by Developer)

| File | Purpose |
|------|---------|
| Test for `MicrosoftGraphAppRoleResolver` | Resolution with known/unknown GUIDs |
| Test for `AppRoleIdFormatter` | Formatting with resolved/unresolved GUIDs |
| Test for `AzureAdSummaryBuilder` | Summary generation for all 3 resource types with mapped/unmapped principals and roles, computed attribute fallbacks |
| Snapshot test data | Terraform plan JSON containing `azuread_app_role_assignment` resources |

## Design Rationale

### Why reuse `RoleDefinitionInfo`?
The record has the same semantics for app roles as for RBAC roles: a human-readable `Name`, an `Id` (GUID), and a `FullName` combining both. Creating a separate record would add unnecessary type proliferation.

### Why a separate `IAppRoleResolver` interface (not reuse `IRoleDefinitionResolver`)?
Although the output type is the same, the input semantics differ:
- `IRoleDefinitionResolver.GetRoleDefinition()` handles Azure RBAC IDs that may contain `/subscriptions/.../roleDefinitions/` prefixes and supports custom role merging from mapping files
- `IAppRoleResolver.GetAppRole()` handles bare GUIDs only and needs no custom-role support (Microsoft Graph app roles are globally stable)

A simpler dedicated interface avoids confusion and keeps the implementations focused.

### Why place resolver in `Platforms/Azure/` not `Providers/AzureAD/`?
The `Platforms/Azure/` namespace holds cross-provider Azure infrastructure (`IRoleDefinitionResolver`, `IPrincipalMapper`, `AzureEntityMapper`). Microsoft Graph app roles could be referenced by future resources in other providers (e.g., `azurerm` resources). Placing the resolver in `Platforms/Azure/` follows the architectural boundary established by existing resolvers and keeps it reusable.

### Why comprehensive app role list?
The project values completeness — `AzureRoleDefinitions.json` already contains ~500 Azure RBAC roles. A comprehensive Microsoft Graph app roles list (sourced from `az ad sp show`) follows this precedent. The JSON file size is modest (~30KB) and has no runtime cost thanks to the frozen dictionary pattern.

### Why `IPrincipalMapper` for `resource_object_id`?
The `resource_object_id` identifies a service principal (the resource API's service principal, e.g., Microsoft Graph). Its object ID is tenant-specific and cannot be statically mapped. However, it is a principal-like object that users can include in their `--principal-mapping` file. Using the existing `IPrincipalMapper` infrastructure avoids creating a parallel mapping system. The Terraform plan's computed `resource_display_name` attribute serves as a fallback.
