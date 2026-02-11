# Architecture: Parent-Child Resource Grouping and Inline Rendering

## Status

Implemented

## Context

Feature 068 introduces parent-child resource grouping, where child Terraform resources (e.g., `azuread_group_member`) are rendered inline as table rows within their parent's section (e.g., `azuread_group`) rather than as separate collapsible sections. This reduces scrolling and makes relationships between resources immediately visible.

The key architectural challenge is that this pattern will be implemented for **many resources across different providers** (the catalog documents 15+ patterns across azurerm, azuread, and azuredevops). The architecture must provide common base functionality that makes adding new parent-child relationships easy (<50 lines per pattern) and consistent.

References:
- [specification.md](specification.md)
- [parent-child-resource-catalog.md](parent-child-resource-catalog.md)
- [rendering-examples.md](rendering-examples.md)

### Existing Patterns

The codebase has two relevant existing patterns:

1. **Firewall rule collections** (Features 026/060): Already render inline children as tables using typed view models (`FirewallNetworkRuleCollectionViewModel`) and resource-specific factories. These parse the `rule` array attribute from a `azurerm_firewall_network_rule_collection` resource and render rules as table rows — structurally the **same pattern** as parsing `members` from an `azuread_group`. The firewall implementation additionally includes complex semantic diffing (matching rules by name between before/after states to compute per-rule changes across 6+ columns).

2. **Provider module registration**: `IProviderModule` → `ProviderRegistry` pattern provides clean per-provider extensibility for helpers, factories, templates, value formatters, and icon providers.

Feature 068 addresses two capabilities:

- **Inline child rendering** — parsing array/set attributes from a parent resource and rendering them as table rows. This is the same structural problem the firewall implementation already solves for rules, but with a bespoke, non-reusable implementation.
- **Cross-resource merging** — detecting separate child Terraform resources (e.g., `azuread_group_member`), removing them from the main change list, and rendering them within the parent's section. This is entirely new.

Note: The firewall catalog entry shows a parent-child relationship that is NOT currently handled: `azurerm_firewall` → `azurerm_firewall_network_rule_collection` / `azurerm_firewall_application_rule_collection`. Each collection currently renders as its own section rather than being grouped under its parent firewall.

## Options Considered

### Option 1: Provider-Specific Typed View Models (Extend Current Pattern)

Each parent-child relationship gets its own typed view model (like `FirewallNetworkRuleCollectionViewModel`), factory, and template. For example: `AzureAdGroupWithMembersViewModel`, `AzureDevOpsTeamWithMembersViewModel`, etc.

**How it works:**
- Each provider adds a new property to `ResourceChangeModel` (e.g., `AzureAdGroupMembers`)
- Each provider implements a factory that detects children and populates the typed model
- Each provider has a fully custom Scriban template
- Merging logic is duplicated per factory

**Pros:**
- Follows the proven pattern used by firewall rule collections
- Maximum flexibility per resource type
- No new abstractions needed

**Cons:**
- ~150–200 lines per new relationship (view model + factory + template)
- Typed properties accumulate on `ResourceChangeModel` (already 6 typed properties; this adds 3+ more)
- Cross-resource merging logic (detect children, remove from change list, resolve references) must be duplicated in each factory
- No consistency guarantee — each implementation can diverge in rendering
- Does not achieve the "<50 lines per pattern" NFR from the spec
- `ResourceChangeModel` becomes increasingly coupled to provider-specific types

### Option 2: Generic Parent-Child Framework

Define a common framework in `MarkdownGeneration/` that handles the cross-resource merging generically. Providers register relationship definitions as data, and the framework handles detection, merging, and rendering.

**How it works:**
- New `IParentChildRelationship` interface defines a relationship declaratively (parent type, child type, reference attribute, inline attribute, table columns, row extraction)
- New `IParentChildRelationshipRegistry` collects all registered relationships
- `IProviderModule` gains a `RegisterParentChildRelationships()` method
- `ReportModelBuilder.Build()` uses the registry to merge children into parents after building all models
- A generic `ChildResourceGroup` model on `ResourceChangeModel` holds merged child data
- A shared Scriban template partial renders any `ChildResourceGroup`

**Pros:**
- Adding a new relationship is ~20–40 lines (a data definition + row extractor)
- Consistent rendering across all providers via shared template
- Cross-resource merging logic written once, tested once
- `ResourceChangeModel` gains one generic property instead of many typed ones
- Meets the "<50 lines per pattern" NFR
- Clear separation: relationship definition (provider) vs. merging logic (core) vs. rendering (template)

**Cons:**
- New abstraction to learn and maintain
- Less flexibility for highly specialized rendering (all child tables follow the same structure)
- Existing firewall implementations use a different pattern (two coexisting approaches until potential future migration)

### Option 3: Generic Framework with Provider-Specific Overrides (Hybrid)

Same as Option 2 but with an explicit escape hatch: providers can optionally override the generic template with a resource-specific template and provide a custom `IChildRowExtractor` for complex formatting.

**How it works:**
- All of Option 2's infrastructure
- `IParentChildRelationship` can optionally specify a custom row extractor for complex value formatting
- Template resolution falls through: resource-specific template → shared `_child_resources.sbn` partial
- Providers can provide a fully custom template for complex cases (e.g., future `azurerm_virtual_network` with complex subnet attributes)

**Pros:**
- All pros of Option 2
- Handles both simple cases (group members = just IDs) and complex cases (subnets with delegations, service endpoints)
- Existing template resolution mechanism already supports this fallthrough pattern
- Future-proof for the out-of-scope resource types documented in the catalog

**Cons:**
- Slightly more complex initial design than Option 2
- Two rendering paths (generic + override) must both be tested
- Risk of over-engineering for the initial 3 resource types (azuread_group, azuredevops_group, azuredevops_team)

## Decision

**Option 2: Generic Parent-Child Framework.**

Rationale: The initial implementation targets (azuread_group + members, azuredevops_group + membership, azuredevops_team + members/administrators) are all structurally simple — child resources have 1–2 display attributes. The generic approach handles them cleanly without needing per-resource overrides. The template resolution mechanism already supports future resource-specific template overrides, so Option 3's escape hatch is available implicitly without additional framework code.

### Relationship to Existing Firewall Implementation

The existing firewall rule collection implementation solves the same structural problem (inline children rendered as table rows) but with a bespoke approach: typed view models, typed properties on `ResourceChangeModel`, and custom templates. The new generic framework handles this same case plus cross-resource merging.

For this feature, the existing firewall implementation is **not migrated** — it works well and has complex semantic diffing logic (per-rule before/after comparison) that goes beyond what the generic `IChildRowExtractor` interface provides. Migrating it would introduce risk for no user-visible benefit.

However, the generic framework is explicitly designed so that future firewall-like implementations (e.g., NSG rules, route table routes) use the generic framework instead of creating new bespoke view models. Additionally, the unhandled `azurerm_firewall` → collection relationship could be registered as a generic parent-child relationship in a future feature.

## Rationale

1. **Scale**: With 15+ documented parent-child patterns, a generic approach avoids accumulating hundreds of lines of duplicated merging logic.

2. **Consistency**: Users expect the same table structure, change indicators, and "Terraform Resource" column across all parent-child renderings. A shared template guarantees this.

3. **Maintainability**: The merging logic (detect children → match to parent → remove from list → attach) is the same for every relationship. Writing and testing it once in the core reduces bugs.

4. **Low overhead per pattern**: A new relationship is defined by:
   - A `ParentChildRelationship` record (~10 lines)
   - A `IChildRowExtractor` implementation (~15–30 lines) for formatting child values
   - Registration in the provider module (~3 lines)
   
   Total: ~25–40 lines, well within the <50 line NFR.

5. **Template reuse**: A shared `_child_resources.sbn` partial handles the common table structure. Resource-specific templates can still include this partial or override entirely via the existing template resolution order.

## Detailed Design

### 1. Core Abstractions (in `MarkdownGeneration/Models/`)

#### ParentChildRelationship

```csharp
/// Describes a parent-child resource relationship for inline rendering.
internal record ParentChildRelationship
{
    /// Parent resource type (e.g., "azuread_group").
    required string ParentResourceType { get; init; }

    /// Child resource type (e.g., "azuread_group_member").
    required string ChildResourceType { get; init; }

    /// Inline attribute name on parent that contains children (e.g., "members").
    /// Null when children only exist as separate resources.
    string? InlineAttributeName { get; init; }

    /// Attribute on CHILD resource that references the parent's ID
    /// (e.g., "group_object_id"). Used to match separate child resources to parents.
    string? ChildReferenceAttribute { get; init; }

    /// Attribute on PARENT resource that contains the ID that children reference
    /// (e.g., "id"). Defaults to "id".
    string ParentIdAttribute { get; init; } = "id";

    /// Display label for this child group (e.g., "Members", "Administrators").
    required string ChildGroupLabel { get; init; }

    /// Column definitions for the child table.
    required IReadOnlyList<ChildTableColumn> TableColumns { get; init; }

    /// Row extractor that produces display values from child state.
    required IChildRowExtractor RowExtractor { get; init; }
}
```

#### ChildTableColumn

```csharp
/// Defines a column in the child resource table.
internal record ChildTableColumn(string Header, string PropertyName);
```

#### IChildRowExtractor

```csharp
/// Extracts a display row from a child resource's or inline attribute's JSON state.
internal interface IChildRowExtractor
{
    /// Extracts column values for a single child entry.
    /// Returns a dictionary mapping column PropertyName → formatted display value.
    IReadOnlyDictionary<string, string> ExtractRow(
        object? childState,
        string providerName,
        IconProviderRegistry? iconProviderRegistry);
}
```

**Design note:** `IChildRowExtractor` handles the common case where each child row is extracted independently from its own state. For future resources that require semantic diffing across before/after collections (like the existing firewall rule implementation), a richer interface such as `IChildCollectionDiffExtractor` could be introduced, accepting both before and after parent state. This is explicitly out of scope for Feature 068 but documents the extension path for migrating the firewall implementation. See [GitHub issue #441](https://github.com/oocx/tfplan2md/issues/441) for the investigation tracking this potential refactoring.

#### ChildResourceGroup (on ResourceChangeModel)

```csharp
/// A group of child resources rendered as a table within a parent section.
internal record ChildResourceGroup
{
    /// Display label (e.g., "Members", "Administrators").
    required string Label { get; init; }

    /// Column definitions for the table header.
    required IReadOnlyList<ChildTableColumn> Columns { get; init; }

    /// Row data for each child.
    required IReadOnlyList<ChildResourceRow> Rows { get; init; }

    /// Whether both inline and separate children exist (conflict warning).
    bool HasMixedSources { get; init; }
}

/// A single row in the child resource table.
internal record ChildResourceRow
{
    /// Change indicator (➕, 🔄, ❌, ⏺️).
    required string ChangeIndicator { get; init; }

    /// Column values keyed by ChildTableColumn.PropertyName.
    required IReadOnlyDictionary<string, string> Values { get; init; }

    /// "Terraform Resource" column: resource address for separate children,
    /// or "{attribute} attribute" for inline children.
    required string TerraformResource { get; init; }

    /// The original Terraform resource address (for code analysis finding attribution).
    string? OriginalResourceAddress { get; init; }
}
```

#### IParentChildRelationshipRegistry

```csharp
/// Registry for parent-child resource relationships.
internal interface IParentChildRelationshipRegistry
{
    void Register(ParentChildRelationship relationship);

    /// Gets all relationships where the given type is a parent.
    IReadOnlyList<ParentChildRelationship> GetRelationshipsForParent(string parentResourceType);

    /// Gets all registered child resource types (for filtering from display list).
    IReadOnlySet<string> GetAllChildResourceTypes();

    /// Checks if a resource type is a registered child type.
    bool IsChildResourceType(string resourceType);
}
```

### 2. Provider Module Extension

`IProviderModule` gains a new default-implemented method:

```csharp
/// Registers parent-child resource relationships for inline rendering.
void RegisterParentChildRelationships(IParentChildRelationshipRegistry registry)
{
    // Default no-op keeps existing provider modules compatible.
}
```

`ProviderRegistry` gains:

```csharp
public void RegisterAllParentChildRelationships(IParentChildRelationshipRegistry registry)
{
    foreach (var provider in _providers)
    {
        provider.RegisterParentChildRelationships(registry);
    }
}
```

### 3. Merging Logic in ReportModelBuilder

The merging happens in `ReportModelBuilder.Build()`, after all `ResourceChangeModel`s are built but before filtering and grouping:

```
Build all ResourceChangeModels (existing)
    ↓
NEW: Run parent-child merging
    ├─ For each parent in the change list:
    │   ├─ Find registered relationships for parent type
    │   ├─ Collect inline children (from parent's own JSON attributes)
    │   ├─ Collect separate children (scan change list for matching child type + reference)
    │   ├─ Build ChildResourceGroup(s) and attach to parent model
    │   └─ Mark children for removal from main change list
    ├─ Remove merged children from change list
    └─ Re-attribute code analysis findings from merged children to parent
    ↓
Filter no-op resources, group by module (existing)
```

**Separate child matching strategy:**

For each registered `ParentChildRelationship`:
1. Identify all resources of `ChildResourceType` in the change list
2. For each child, read `ChildReferenceAttribute` from its `AfterJson` (or `BeforeJson` for deletes)
3. Match the reference value against each parent's `ParentIdAttribute` in its `AfterJson`/`BeforeJson`
4. **Fallback for `(known after apply)` — Configuration Reference Matching**: When the parent's ID is not yet known (the most common scenario when creating parent and children together), use the plan's `configuration` block to resolve the Terraform expression references. See **Section 3a** below for details.
5. **Graceful degradation**: If neither value-based matching (step 3) nor configuration reference matching (step 4) can identify the parent for a child resource, the child is **not merged** — it remains in the change list and renders as a standalone resource section, exactly as it would without Feature 068. Incorrect merging (false positives) is always worse than no merging, so the system must never guess.

**This merging logic is implemented as a new partial class file**: `ReportModelBuilder.ParentChildMerging.cs`.

### 3a. Configuration Reference Matching (Fallback for `(known after apply)`)

#### Problem

When a parent resource is being **created**, its `id` attribute is `(known after apply)` — its value won't exist until Terraform applies the plan. In the same scenario, a separate child resource that references the parent (e.g., `group_object_id = azuread_group.platform_engineers.id`) will also have its reference attribute marked as unknown in `after_unknown`. Neither value-based comparison can succeed.

A naive fallback using module-address heuristics (same module scope + matching type) **fails** when multiple parents of the same type exist in the same module. For example, two `azuread_group` resources with separate `azuread_group_member` children would be incorrectly cross-matched.

#### Solution: Parse `configuration` Block Expression References

The Terraform plan JSON always includes a `configuration` block (in plan output, not state output) containing the parsed Terraform configuration with unevaluated expressions. Each resource's attributes include a `references` array listing the Terraform resources they depend on:

```json
{
  "configuration": {
    "root_module": {
      "resources": [
        {
          "address": "azuread_group_member.platform_admin_member",
          "type": "azuread_group_member",
          "expressions": {
            "group_object_id": {
              "references": [
                "azuread_group.platform_engineers.id",
                "azuread_group.platform_engineers"
              ]
            },
            "member_object_id": {
              "constant_value": "user-100"
            }
          }
        }
      ]
    }
  }
}
```

The `references` array tells us that `azuread_group_member.platform_admin_member`'s `group_object_id` references `azuread_group.platform_engineers.id` — precisely identifying the parent resource even when neither value is concrete.

#### Design

**Data model change:** Add `Configuration` as an optional `JsonElement?` property to `TerraformPlan`:

```csharp
public record TerraformPlan(
    [property: JsonPropertyName("format_version")] string FormatVersion,
    [property: JsonPropertyName("terraform_version")] string TerraformVersion,
    [property: JsonPropertyName("resource_changes")] IReadOnlyList<ResourceChange> ResourceChanges,
    [property: JsonPropertyName("timestamp")] string? Timestamp = null,
    [property: JsonPropertyName("configuration")] JsonElement? Configuration = null
);
```

Using `JsonElement?` instead of strongly-typed models avoids the complexity of modeling the entire configuration tree. The source-generated JSON context (`TfPlanJsonContext`) already supports `JsonElement`. No new model classes are needed for the configuration block itself.

**New utility: `ConfigurationReferenceResolver`** (in `Parsing/` or `MarkdownGeneration/`):

```csharp
/// Resolves Terraform expression references from the plan's configuration block.
internal sealed class ConfigurationReferenceResolver
{
    /// Builds a reference index from the configuration block.
    /// Returns a map: (child_resource_address, attribute_name) → list of referenced resource addresses.
    public static IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>
        BuildReferenceIndex(JsonElement? configuration);
}
```

The resolver walks `configuration.root_module.resources[]` (and recursively `module_calls.*.module.resources[]` for modules) to extract expression references. For each resource, it:

1. Computes the absolute address by prepending the module path (e.g., a resource `azuread_group.example` inside `module_calls.network` becomes `module.network.azuread_group.example`)
2. For each expression attribute that has a `references` array, stores the mapping
3. References within `module_calls` are also prefixed with the module path to produce absolute addresses

**Integration in `BuildSeparateRows`:** When value-based matching fails (parent ID is null/empty), the method consults the reference index:

```
For each candidate child:
  1. Look up (child.Address, relationship.ChildReferenceAttribute) in the reference index
  2. Check if any reference matches the parent's address pattern:
     - parent.Address (exact match)
     - parent.Address + "." + relationship.ParentIdAttribute (attribute-qualified match)
  3. If matched → merge the child into the parent
```

**Data flow:** `TerraformPlan.Configuration` → `ConfigurationReferenceResolver.BuildReferenceIndex()` (called once per `Build()`) → stored as a field in `ReportModelBuilder` → passed to `MergeParentChildRelationships()` → used in `BuildSeparateRows()` as fallback.

#### Module Nesting

Configuration references are **module-local** — `azuread_group.example` inside module `network` maps to `module.network.azuread_group.example` in `resource_changes`. The resolver reconstructs absolute addresses by tracking the module path during tree traversal:

```
root_module.resources[] → address as-is
root_module.module_calls.<name>.module.resources[] → "module.<name>." + address
root_module.module_calls.<name>.module.module_calls.<inner>.module.resources[]
  → "module.<name>.module.<inner>." + address
```

#### Edge Cases

- **`configuration` block absent** (synthetic test data, `terraform show -json` of state files): Fallback returns empty, child resources appear as standalone sections. This is acceptable — the feature degrades gracefully to no merging rather than incorrect merging.
- **`for_each`/`count` resources**: Configuration has one entry per resource block, while `resource_changes` has entries per instance (`[key]` suffix). The resolver strips instance keys when looking up configuration references.
- **Dynamic blocks**: Terraform states that "expressions in `dynamic` blocks are not included in the configuration representation." This affects nested block attributes but NOT top-level attributes like `group_object_id`, `team_id`, etc. Parent-child reference attributes are always top-level, so dynamic blocks are not a concern.
- **`each.value`/`each.key` references**: These produce references like `each.value` rather than a specific resource address. These won't match any parent address and the child will remain unmerged — correct behavior since the actual parent depends on the iteration context.

#### Test Data Updates

Synthetic test plans that need to exercise the fallback must include a `configuration` block with appropriate expression references. Specifically:
- `azuread-group-members-plan.json` (or a new create-scenario variant): Add `configuration` with `azuread_group_member.*.expressions.group_object_id.references` pointing to the parent group
- `comprehensive-demo/plan.json`: Add a `configuration` block for the `azuread_group_member.platform_admin_member` resource
- New unit test: Verify `ConfigurationReferenceResolver.BuildReferenceIndex()` correctly handles root module, nested modules, and absent configuration

### 4. ResourceChangeModel Changes

Replace the proliferation of typed view model properties with a single generic collection:

```csharp
/// Gets or sets the child resource groups rendered inline within this parent.
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
public IReadOnlyList<ChildResourceGroup> ChildResourceGroups { get; set; } = [];
```

**Note:** Existing typed properties (`FirewallNetworkRuleCollection`, `NetworkSecurityGroup`, etc.) remain unchanged. They handle inline attribute rendering using bespoke view models with complex semantic diffing. Migration to the generic framework is not performed in this feature — see "Relationship to Existing Firewall Implementation" in the Decision section.

### 5. Template Design

#### Shared Partial: `_child_resources.sbn`

Located in core templates (`MarkdownGeneration/Templates/`):

```scriban
{{~ for group in change.child_resource_groups ~}}
#### {{ group.label }}{{ "\n" }}
{{~ if group.has_mixed_sources ~}}

⚠️ **Warning:** This resource has children managed both inline
and as separate resources. This configuration will cause conflicts.
{{~ end ~}}

{{~ if change.action == "create" || change.action == "delete" ~}}
{{~ ## No Change column for pure create/delete — all children share parent action ~}}
| {{ for col in group.columns }}{{ col.header }} | {{ end }}Terraform Resource |
| {{ for col in group.columns }}{{ repeat "-" col.header.size }} | {{ end }}{{ repeat "-" 20 }} |
{{~ for row in group.rows ~}}
| {{ for col in group.columns }}{{ row.values[col.property_name] }} | {{ end }}{{ row.terraform_resource }} |
{{~ end ~}}
{{~ else ~}}
{{~ ## Update with Change column ~}}
| Change | {{ for col in group.columns }}{{ col.header }} | {{ end }}Terraform Resource |
| -------- | {{ for col in group.columns }}{{ repeat "-" col.header.size }} | {{ end }}{{ repeat "-" 20 }} |
{{~ for row in group.rows ~}}
| {{ row.change_indicator }} | {{ for col in group.columns }}{{ row.values[col.property_name] }} | {{ end }}{{ row.terraform_resource }} |
{{~ end ~}}
{{~ end ~}}

{{~ end ~}}
```

Resource-specific templates (e.g., `azuread/azuread_group.sbn`) include this partial:

```scriban
{{~ include "/_child_resources.sbn" ~}}
```

### 6. Provider Registration Examples

#### AzureAD Provider

```csharp
// In AzureADModule.RegisterParentChildRelationships()
registry.Register(new ParentChildRelationship
{
    ParentResourceType = "azuread_group",
    ChildResourceType = "azuread_group_member",
    InlineAttributeName = "members",
    ChildReferenceAttribute = "group_object_id",
    ChildGroupLabel = "Members",
    TableColumns = [new("Member", "member")],
    RowExtractor = new AzureAdGroupMemberRowExtractor()
});
```

#### AzureDevOps Provider

```csharp
// In AzureDevOpsModule.RegisterParentChildRelationships()
registry.Register(new ParentChildRelationship
{
    ParentResourceType = "azuredevops_team",
    ChildResourceType = "azuredevops_team_members",
    InlineAttributeName = "members",
    ChildReferenceAttribute = "team_id",
    ChildGroupLabel = "Members",
    TableColumns = [new("Member", "member")],
    RowExtractor = new AzureDevOpsTeamMemberRowExtractor()
});

registry.Register(new ParentChildRelationship
{
    ParentResourceType = "azuredevops_team",
    ChildResourceType = "azuredevops_team_administrators",
    InlineAttributeName = "administrators",
    ChildReferenceAttribute = "team_id",
    ChildGroupLabel = "Administrators",
    TableColumns = [new("Administrator", "administrator")],
    RowExtractor = new AzureDevOpsTeamAdminRowExtractor()
});
```

### 7. Code Analysis Findings for Merged Children

When a child resource is merged into its parent's section, its code analysis findings must transfer to the parent while preserving the original resource address:

1. During merging, collect findings from each merged child's `CodeAnalysisFindings`
2. Attach them to the parent's `CodeAnalysisFindings` list with the original resource address preserved in the finding model
3. The template renders findings grouped by resource address (the existing `_code_analysis_findings.sbn` partial already supports per-resource finding headers)

This is handled inside `ReportModelBuilder.ParentChildMerging.cs` alongside the child detection logic.

### 8. Summary Line Updates

Parent resources with inlined children update their `ChangedAttributesSummary` and `SummaryHtml` to include child change counts. This is computed during the merging phase, after child groups are built:

- Format: `➕ 3 members` or `➕ 2 members, ❌ 1 member`
- Uses the same patterns as `FirewallNetworkRuleCollectionViewModelFactory.BuildChangedAttributesSummary()`

### 9. Component Location

| Component | Location |
|-----------|----------|
| `ParentChildRelationship` | `src/.../MarkdownGeneration/Models/ParentChildRelationship.cs` |
| `ChildResourceGroup`, `ChildResourceRow` | `src/.../MarkdownGeneration/Models/ChildResourceGroup.cs` |
| `ChildTableColumn` | `src/.../MarkdownGeneration/Models/ChildTableColumn.cs` |
| `IChildRowExtractor` | `src/.../MarkdownGeneration/Models/IChildRowExtractor.cs` |
| `IParentChildRelationshipRegistry` | `src/.../MarkdownGeneration/Models/IParentChildRelationshipRegistry.cs` |
| `ParentChildRelationshipRegistry` | `src/.../MarkdownGeneration/Models/ParentChildRelationshipRegistry.cs` |
| Merging logic | `src/.../MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs` |
| Shared template | `src/.../MarkdownGeneration/Templates/_child_resources.sbn` |
| AzureAD row extractors | `src/.../Providers/AzureAD/Models/AzureAdGroupMemberRowExtractor.cs` |
| AzureDevOps row extractors | `src/.../Providers/AzureDevOps/Models/AzureDevOps*RowExtractor.cs` |
| Registration | `AzureADModule.RegisterParentChildRelationships()`, `AzureDevOpsModule.RegisterParentChildRelationships()` |

All core abstractions live in `MarkdownGeneration/Models/` (core layer). All provider-specific extractors live in `Providers/{ProviderName}/Models/` (provider layer). This respects the architectural boundary: provider-specific logic MUST NOT appear in core modules.

## Consequences

### Positive

- **Extensibility**: Adding NSG rules, route table routes, subnets, etc. in future features requires only a relationship record + row extractor per provider — no core changes
- **Consistency**: All parent-child tables share the same structure (Change indicator, custom columns, Terraform Resource column)
- **Testability**: Core merging logic tested once with generic test data; each row extractor tested independently
- **Clean model**: `ResourceChangeModel` gains one generic property (`ChildResourceGroups`) instead of accumulating typed properties per resource type
- **Backward compatibility**: Existing firewall rule collection rendering is untouched

### Negative

- **Two patterns coexist**: Firewall rule collections use typed view models with complex semantic diffing; new parent-child resources use the generic framework. Both handle inline children rendered as tables, but the firewall implementation has additional complexity (per-rule before/after matching across 6+ columns) that exceeds the generic `IChildRowExtractor` interface. Future work could extend the framework to support semantic diffing and then migrate the firewall implementation, but that is not required or planned for this feature.
- **Configuration block dependency**: Matching separate child resources to parents when IDs are `(known after apply)` requires parsing the plan's `configuration` block for expression references. This adds a new data dependency (`TerraformPlan.Configuration`) and a reference resolver utility. Synthetic test data must include `configuration` blocks to exercise the fallback path. Plans without a `configuration` block (state files, minimal test data) degrade gracefully to no merging rather than incorrect merging.
- **New abstraction**: Developers must learn the `ParentChildRelationship` / `IChildRowExtractor` pattern. This is mitigated by clear documentation and the provider registration examples serving as templates for future additions.

## Implementation Notes

### For the Developer Agent

1. **Start with core abstractions**: Implement `ParentChildRelationship`, `ChildResourceGroup`, `IChildRowExtractor`, and `IParentChildRelationshipRegistry` in `MarkdownGeneration/Models/`.

2. **Implement merging in `ReportModelBuilder.ParentChildMerging.cs`**: This is the most complex piece. Handle:
   - Inline children (parse parent's JSON for the `InlineAttributeName`)
   - Separate children (scan change list for `ChildResourceType`, match via `ChildReferenceAttribute` → `ParentIdAttribute`)
   - `(known after apply)` fallback: use `ConfigurationReferenceResolver` to match children to parents via expression references when value-based matching fails (see Section 3a)
   - Mixed source detection (both inline and separate → set `HasMixedSources` flag)
   - Code analysis finding re-attribution

3. **Extend `IProviderModule`** with `RegisterParentChildRelationships()` (default no-op). Extend `ProviderRegistry` with `RegisterAllParentChildRelationships()`.

4. **Implement row extractors** in each provider:
   - `AzureAdGroupMemberRowExtractor`: Extract `member_object_id`, format with principal mapper + person icon
   - `AzureAdGroupOwnerRowExtractor`: Extract owner from `owners` inline attribute
   - `AzureDevOpsGroupMembershipRowExtractor`: Extract `member` descriptor
   - `AzureDevOpsTeamMemberRowExtractor`: Extract member descriptor
   - `AzureDevOpsTeamAdminRowExtractor`: Extract admin descriptor

5. **Create shared template partial** `_child_resources.sbn` and resource-specific templates that include it.

6. **Update summary logic**: After merging, update parent's `ChangedAttributesSummary` and `SummaryHtml` with child change counts.

7. **Wire up in `ReportModelBuilder.Build()`**: Call merging logic between building all models and the no-op filtering step.

8. **Integration order**: Core abstractions → merging logic → AzureAD provider → AzureDevOps provider → templates → summary updates → code analysis finding re-attribution.

### Guidelines for Adding New Parent-Child Relationships (Future)

To add a new parent-child relationship in a future feature:

1. **Create a row extractor** implementing `IChildRowExtractor` in the appropriate provider's `Models/` folder (~15–25 lines)

2. **Register the relationship** in the provider module's `RegisterParentChildRelationships()` method (~10 lines):
   ```csharp
   registry.Register(new ParentChildRelationship
   {
       ParentResourceType = "azurerm_network_security_group",
       ChildResourceType = "azurerm_network_security_rule",
       InlineAttributeName = "security_rule",
       ChildReferenceAttribute = "network_security_group_name",
       ChildGroupLabel = "Security Rules",
       TableColumns = [
           new("Name", "name"),
           new("Priority", "priority"),
           new("Direction", "direction"),
           new("Access", "access"),
           new("Protocol", "protocol"),
           new("Ports", "ports")
       ],
       RowExtractor = new NsgRuleRowExtractor()
   });
   ```

3. **Optionally create a resource-specific template** if the default rendering is insufficient.

4. **Add test data** (plan JSON fixture + snapshot test).

Total: ~25–40 lines of new code per relationship, plus test fixtures.
