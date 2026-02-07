# Architecture: Azure Display Enhancements

## Status

Proposed

## Context

Feature 063 enhances the display of Azure resources and identities across all Azure providers by:
1. Extending the mapping file with subscription, management group, tenant, and custom role sections
2. Resolving subscription and management group display names throughout readable output
3. Supporting custom role definitions (with built-in override capability)
4. Adding resource-specific summaries for DNS records, PIM assignments, and role management policies
5. Enriching debug output with new mapping statistics and failed resolution tracking

The feature builds on existing infrastructure: `PrincipalMapper`, `AzureRoleDefinitionMapper`, `AzureScopeParser`, the `ValueFormatterRegistry` pattern, and `ResourceSummaryBuilder`/`ResourceSummaryMappings`.

Reference: [specification.md](specification.md)

## Decisions

### Decision 1: Role ID Detection Strategy

**Options Considered:**

1. **Attribute name matching** — Register a value formatter matching known attribute names (`role_definition_id`, `role_id`) via the existing `ValueFormatterRegistry` + `MatchPattern` system.
   - Pros: Explicit, predictable, no false positives, aligns with `AzureResourceIdFormatter` pattern
   - Cons: Must maintain a small list of known role attribute names

2. **GUID pattern matching** — Try to resolve any GUID-like value against all mapping dictionaries.
   - Pros: Catches roles in any attribute, future-proof
   - Cons: Many wasted lookups, false-positive risk (same GUID could match subscription, principal, or role), performance impact

3. **Hybrid** — Attribute name matching primary, GUID fallback for Azure provider resources.
   - Pros: Best coverage
   - Cons: More complexity, harder to reason about resolution triggers

**Decision:** Option 1 — attribute name matching.

**Rationale:** The existing `ValueFormatterRegistry` + `MatchPattern` infrastructure is designed for exactly this: match by provider + attribute name pattern + value pattern, then apply formatting. Registering a `RoleDefinitionFormatter` with an attribute name regex like `^role_definition_id$` follows the same pattern as `AzureResourceIdFormatter`. The list of role-related attributes is small and stable (`role_definition_id`, `role_definition_resource_id`). This avoids false positives and unnecessary lookups while being trivially extensible if new attribute names emerge.

### Decision 2: Mapping File Loading Architecture

**Options Considered:**

1. **Extend PrincipalMapper** to load all new sections alongside principals.
   - Pros: Minimal new classes, single file-loading path
   - Cons: PrincipalMapper is already ~400 lines; violates single responsibility

2. **Create a shared AzureMappingFileLoader** that loads the file once and distributes parsed sections to domain-specific mappers.
   - Pros: Single file read, clean separation of concerns, each mapper stays focused
   - Cons: One new coordinating class

3. **Separate mapper classes each loading the same file independently.**
   - Pros: Fully independent
   - Cons: Redundant file I/O and JSON parsing

**Decision:** Option 2 — shared `AzureMappingFileLoader`.

**Rationale:** The mapping file is loaded once at startup, parsed into its sections, and the parsed data is handed to the appropriate mappers. `PrincipalMapper` continues to own principal resolution. New mappers own their respective domains. This keeps all classes under the ~300-line guideline and avoids redundant file reads.

### Decision 3: Subscription/Management Group Name Injection into Scope Output

**Options Considered:**

1. **Make AzureScopeParser stateful** — pass mappers to it so it resolves display names inline.
   - Pros: Single-step resolution
   - Cons: Turns a pure static parser into a stateful service; breaks existing contract

2. **Create an EnrichedAzureScopeFormatter** that post-processes `ScopeInfo` with display names, using `AzureScopeParser.Parse()` for structure and entity mappers for names.
   - Pros: `AzureScopeParser` remains a pure parser; enrichment is a composable layer; testable in isolation
   - Cons: Two-step process (parse then enrich)

3. **Enhance AzureResourceIdFormatter** to enrich scope output after parsing.
   - Pros: Keeps changes inside the existing value formatter
   - Cons: Formatter becomes responsible for both detection and enrichment; harder to reuse enrichment elsewhere (e.g., in resource-specific summaries)

**Decision:** Option 2 — `EnrichedAzureScopeFormatter`.

**Rationale:** `AzureScopeParser` is a heavily-used pure static utility. Making it stateful would require propagating mapper dependencies everywhere it's called. A dedicated `EnrichedAzureScopeFormatter` composes the parser output with display name resolution and can be reused by both the value formatter pipeline and resource-specific summary builders (PIM assignments, role management policies). This aligns with the project's preference for composition over mutation.

### Decision 4: Resource-Specific Summary Implementation

**Options Considered:**

1. **Extend ResourceSummaryBuilder** with special-case logic for all three resource types.
   - Pros: Centralized, simple for DNS records
   - Cons: Summary builder grows with external dependencies (mappers); PIM and role management policy need role/principal/scope resolution

2. **Use the ViewModelFactory pattern** (like `RoleAssignmentFactory`) for all three resource types.
   - Pros: Keeps provider-specific summary logic out of the generic builder; matches the established `RoleAssignmentFactory` precedent for Azure-specific summaries
   - Cons: One more factory registration to maintain

3. **Create per-resource summary builders** implementing `IResourceSummaryBuilder`.
   - Pros: Maximum flexibility
   - Cons: Departure from existing patterns; more interfaces to manage

**Decision:** Option 2 — ViewModelFactory approach for all resource-specific summaries.

**Rationale:**
- **`azurerm_private_dns_a_record`**: Still a simple attribute concatenation (`name` + `zone_name`), but keeping it in a provider-specific factory avoids provider logic in the generic summary builder. The factory sets the summary text and summary HTML to the fully qualified name (`{name}.{zone_name}`).
- **`azurerm_pim_eligible_role_assignment`** and **`azurerm_role_management_policy`**: These need external dependencies (role mapper, principal mapper, scope formatter) to produce summaries like "Assign `Contributor` to `Jane Doe`". Following the `RoleAssignmentFactory` pattern, create `ViewModelFactory` classes registered in `AzureRMModule.RegisterFactories()` that pre-resolve display data and set the summary on the view model. This is the established pattern for Azure-specific summaries.

## Implementation Notes

### Component Changes

#### New Classes (in `Platforms/Azure/`)

| Class | Purpose |
|-------|---------|
| `AzureMappingFileLoader` | Loads the unified mapping JSON file once, distributes parsed sections. Returns a result record containing principal, subscription, management group, tenant, and role collections. |
| `AzureEntityMapper` | Resolves subscription IDs → display names, management group IDs → display names, tenant IDs → display names. Accepts parsed collections from the loader. |
| `EnrichedAzureScopeFormatter` | Composes `AzureScopeParser.Parse()` output with `AzureEntityMapper` to produce display-name-enriched scope strings. |

#### Modified Classes

| Class | Changes |
|-------|---------|
| `PrincipalMappingFile` | Add `Subscriptions`, `ManagementGroups`, `Tenants`, `Roles` properties. Use `List<MappingEntry>` (array of `{id, displayName}` objects) for new sections to match the specification's JSON format. Keep existing `Users`, `Groups`, `ServicePrincipals` as `Dictionary<string, string>` for backward compatibility. |
| `PrincipalMapper` | Extract file-loading logic to `AzureMappingFileLoader`. Constructor accepts pre-parsed principal data instead of a file path. |
| `AzureRoleDefinitionMapper` | Add a method to merge custom role definitions from the mapping file. Custom roles override built-in names when both exist for the same GUID. |
| `AzureResourceIdFormatter` | Use `EnrichedAzureScopeFormatter` (when available) instead of calling `AzureScopeParser.ParseScope()` directly. |
| `AzureRMModule` | Register new `ViewModelFactory` instances for `azurerm_private_dns_a_record`, `azurerm_pim_eligible_role_assignment`, and `azurerm_role_management_policy`. Register the `RoleDefinitionFormatter` as a value formatter. |
| `ReportModelBuilder` | Respect provider factory overrides for `Summary` before falling back to `ResourceSummaryBuilder`. |
| `DiagnosticContext` | Add counters for subscriptions, management groups, tenants, and custom roles. Extend `FailedResolution` tracking to cover all entity types (not just principals). |
| `ProgramEntry` | Wire up `AzureMappingFileLoader` → individual mappers → pass to modules. |

#### New Classes (in `Providers/AzureRM/`)

| Class | Purpose |
|-------|---------|
| `RoleDefinitionFormatter` | `IValueFormatter` matching attribute names `role_definition_id` (and `role_definition_resource_id`). Calls `AzureRoleDefinitionMapper.GetRoleDefinition()` to produce readable names. |
| `AzureRMPrivateDnsARecordFactory` | `IResourceViewModelFactory` for `azurerm_private_dns_a_record`. Builds the FQDN summary (`name.zone_name`) and sets Summary/SummaryHtml. |
| `PimEligibleRoleAssignmentFactory` | `IResourceViewModelFactory` for `azurerm_pim_eligible_role_assignment`. Resolves role name and principal name to produce "Assign `<role>` to `<principal>`" summary. |
| `RoleManagementPolicyFactory` | `IResourceViewModelFactory` for `azurerm_role_management_policy`. Resolves role name and scope display name to produce "`<role>` in `<scope>`" summary. |

#### New Record (in `Platforms/Azure/`)

| Record | Purpose |
|--------|---------|
| `MappingEntry` | `record MappingEntry(string Id, string DisplayName)` — represents one entry in the new mapping file sections. |
| `AzureMappingFileResult` | Holds all parsed mapping sections for distribution to mappers. |

### Mapping File Schema Change

The existing `PrincipalMappingFile` gains four optional new sections. The new sections use an **array-of-objects** format (not dictionary) per the specification:

```json
{
  "users": { "guid": "name" },
  "groups": { "guid": "name" },
  "servicePrincipals": { "guid": "name" },
  "subscriptions": [
    { "id": "guid", "displayName": "Production" }
  ],
  "managementGroups": [
    { "id": "mg-prod", "displayName": "Production Workloads" }
  ],
  "tenants": [
    { "id": "guid", "displayName": "Contoso Corp" }
  ],
  "roles": [
    { "id": "guid", "displayName": "Custom Deployment Role" }
  ]
}
```

Backward compatibility: files with only `users`/`groups`/`servicePrincipals` (or flat `{"guid": "name"}` format) continue to work unchanged.

### Enriched Scope Formatting Flow

```
AzureResourceIdFormatter.TryFormat(context)
  → AzureScopeParser.Parse(value)          // pure structural parse
  → EnrichedAzureScopeFormatter.Format(scopeInfo, entityMapper)
      → Replace subscription ID with "DisplayName (ID)"
      → Replace management group ID with display name
      → Format root management group as "Tenant `<name>` root"
  → Return enriched markdown string
```

### Value Formatter Registration (for roles)

```
AzureRMModule.RegisterValueFormatters(registry):
  registry.Register(
    new MatchPattern("(^azurerm$|.*/azurerm$)", null, "^role_definition_id$|^role_definition_resource_id$", null),
    new RoleDefinitionFormatter());
```

### Dependency Wiring (in ProgramEntry)

```
1. AzureMappingFileLoader.Load(mappingFilePath, diagnosticContext)
   → AzureMappingFileResult { Principals, PrincipalTypes, Subscriptions, ManagementGroups, Tenants, Roles }

2. PrincipalMapper(result.Principals, result.PrincipalTypes, diagnosticContext)
3. AzureEntityMapper(result.Subscriptions, result.ManagementGroups, result.Tenants, diagnosticContext)
4. AzureRoleDefinitionMapper.MergeCustomRoles(result.Roles)
5. EnrichedAzureScopeFormatter(entityMapper)
6. Modules receive mappers via constructor injection as before
```

## Consequences

### Positive

- Clean separation: each mapper handles one domain (principals, entities, roles)
- Existing pure `AzureScopeParser` is untouched; enrichment is composable
- Role definition resolution uses the proven `ValueFormatterRegistry` pattern
- ViewModelFactory pattern reuse for PIM and role management policy prevents ad-hoc summary logic
- One file load, distributed to multiple consumers
- Fully backward-compatible mapping file format

### Negative

- Introduces ~6 new classes, adding to the class count in `Platforms/Azure/` and `Providers/AzureRM/`
- `PrincipalMapper` constructor changes (accepts pre-parsed data instead of file path) — affects test fixtures
- Two formatting mechanisms coexist: `ResourceSummaryMappings` for simple cases, `ViewModelFactory` for complex cases (but this is already the established pattern)

### Risks

- `EnrichedAzureScopeFormatter` must handle all `ScopeLevel` variants correctly, including edge cases like empty/partial subscription IDs
- Custom role overrides in `AzureRoleDefinitionMapper.MergeCustomRoles()` must be applied before any resolution occurs (load-time, not lazy) to avoid race conditions
- Root management group detection ("Tenant root") requires matching the tenant ID against the management group ID, which needs both tenant and management group mappings to be present
