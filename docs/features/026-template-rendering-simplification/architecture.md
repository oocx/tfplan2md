# Architecture: Template Rendering Simplification

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

The current template rendering system uses a **render-then-replace** pattern:
1. Render the entire report using `default.sbn`
2. For each resource, check if a resource-specific template exists
3. If so, render the resource again with that template
4. Use regex to find HTML anchor comments and replace the default-rendered section

This creates several problems:
- Template authors must emit matching anchor comments (`<!-- tfplan2md:resource-start -->` / `<!-- tfplan2md:resource-end -->`)
- Default content is rendered then discarded (wasted computation)
- Regex replacement is fragile with special characters in addresses
- Coordination between default and resource-specific templates is implicit and error-prone
- During feature 024 development, agents struggled with trial-and-error implementation

**Goal:** Eliminate the render-then-replace pattern while maintaining template flexibility and ensuring zero regressions in output.

## Options Considered

### Option A: Direct Template Dispatch (Single-Pass) ✅ Selected

Instead of render-then-replace, determine which template to use BEFORE rendering each resource and invoke it directly.

**How it works:**
```
1. Render report header/summary (non-resource content)
2. For each module:
   2a. Render module header
   2b. For each resource:
       - Resolve template: resource-specific → default resource partial
       - Render that template directly (no anchors needed)
3. Render report footer
```

**Implementation approach:**
- Split `default.sbn` into composable partials: `_header.sbn`, `_footer.sbn`, `_resource.sbn`
- Add Scriban `include` support with custom template loader
- Main template orchestrates by calling `include` for each resource with pre-resolved template name

**Template example:**
```scriban
{{ include "_header" }}

{{ for module in module_changes }}
### 📦 Module: {{ module.module_address }}

{{ for change in module.changes }}
{{ include (resolve_template change.type) change }}
{{ end }}
{{ end }}

{{ include "_footer" }}
```

**Pros:**
- No anchors or regex replacement
- Single rendering pass (no wasted computation)
- Template selection is explicit and debuggable
- Easier to understand control flow
- Scriban has built-in include support

**Cons:**
- Requires custom template loader for Scriban
- Partial templates get `change` context, not full `model` context
- More templates to maintain (partials)

### Option B: Model-Driven Template Resolution

Move template selection INTO the model-building phase. Each `ResourceChangeModel` gets a `TemplateName` property that tells the renderer which template to use.

**How it works:**
- `ReportModelBuilder` resolves template for each resource during model construction
- `ResourceChangeModel.TemplateName` = "azurerm/role_assignment" or "resource-default"
- Renderer iterates changes and invokes the pre-resolved template
- No runtime template resolution in renderer

**Pros:**
- Template resolution testable at model-building time
- Very explicit - you can inspect which template each resource will use
- Renderer becomes a simple loop

**Cons:**
- Model knows about presentation concerns (template names)
- Template resolution split across two phases (model + render)
- Still needs template partials concept

### Option C: Scriban Custom Functions for Resource Rendering

Keep a single template but provide a custom Scriban function `render_resource(change)` that handles template resolution internally.

**How it works:**
- Single `default.sbn` template
- Template calls `{{ render_resource change }}` for each resource
- C# function resolves and invokes the appropriate template
- No anchors needed - output is inserted directly

**Pros:**
- Minimal template structure change
- Template authors just call `render_resource`
- Gradual migration possible

**Cons:**
- Recursive template rendering from within templates
- Template context management is tricky (global vars, helpers availability)
- Harder to debug template resolution
- Mixing C# logic with template logic in confusing ways

### Option D: Layout Template + Typed Resource Blocks

The default template is purely layout. Each resource type registers a "block renderer" in C#.

**How it works:**
- C# code handles outer structure (details/summary/div)
- Templates only define table content
- `IResourceBlockRenderer` interface for resource-specific rendering

**Pros:**
- Clear separation: C# handles structure, templates handle content
- Resource renderers are discoverable (all implement interface)
- Easy to add new resource types

**Cons:**
- Moves too much logic to C# (reduces template flexibility)
- Templates become very limited in scope
- May reduce custom template usefulness
- Largest refactoring effort

## Decision

Choose **Option A: Direct Template Dispatch (Single-Pass)**.

## Rationale

Option A provides the best balance of:
1. **Architectural clarity** - Single-pass rendering with explicit template selection
2. **Template flexibility** - Scriban templates remain powerful and customizable
3. **Maintainability** - No anchors, no regex, no implicit coordination
4. **Migration path** - Can be implemented incrementally

Option B was considered but rejected because it mixes presentation concerns (template names) into the data model. Option C was rejected due to complexity of recursive template rendering and context management. Option D was rejected as too restrictive for template authoring.

## Consequences

### Positive

- Eliminates anchor comments from all templates
- Single rendering pass (no wasted default rendering)
- Template selection is visible and debuggable
- Template authors don't need to understand replacement mechanics
- Easier to reason about template composition

### Negative

- Requires implementing custom Scriban template loader
- Partial templates have different context shape than current resource templates
- Migration requires updating all existing templates

## Regex Hacks to Eliminate

The current `MarkdownRenderer` contains several Regex.Replace calls that are workarounds for template output issues:

| Location | Pattern | Workaround For |
|----------|---------|----------------|
| Line 77 | Anchor replacement | Render-then-replace pattern |
| Line 293 | Blank lines in tables | Template whitespace control issues |
| Line 295 | Indented table rows | Template indentation leaking into output |
| Line 371 | Multiple blank lines | Template produces inconsistent spacing |
| Line 377 | Before headings | Template doesn't control spacing properly |
| Line 380 | After headings | Template doesn't control spacing properly |

**Root cause:** Scriban's whitespace handling combined with complex template logic (nested loops, conditionals, `func` definitions) makes output formatting unpredictable. The `{{~` and `~}}` whitespace modifiers are fragile.

**Goal:** The new implementation should produce clean output directly, without post-processing hacks. If any Regex post-processing remains, it must be:
1. Documented with clear justification
2. Minimal (ideally zero)
3. A conscious design choice, not a workaround for template complexity

**How the new approach helps:**
- Simpler templates = more predictable whitespace
- Pre-computed `FormattedValue<T>` won't contain embedded newlines that break tables
- No complex `func` definitions generating unexpected whitespace
- Layout-only templates are easier to control

## Success Criteria

### Baseline Metrics (Before Refactoring)

Measured on 2024-12-30:

#### C# Code Metrics

| File | Lines | Decision Points | Methods |
|------|-------|-----------------|---------|
| `ScribanHelpers.cs` | 1718 | 193 | 65 |
| `MarkdownRenderer.cs` | 386 | 28 | - |

#### Template Metrics

| Template | Lines | Decision Points | `func` Definitions |
|----------|-------|-----------------|-------------------|
| `default.sbn` | 110 | 33 | 0 |
| `summary.sbn` | 16 | 6 | 0 |
| `firewall_network_rule_collection.sbn` | 104 | 35 | 3 |
| `network_security_group.sbn` | 117 | 33 | 7 |
| `role_assignment.sbn` | 223 | 52 | 8 |
| **Total** | **570** | **159** | **18** |

#### Other Metrics

| Metric | Current Value |
|--------|---------------|
| Anchor comments in templates | 8 |

### Target Metrics (After Refactoring)

| Metric | Before | Target | Rationale |
|--------|--------|--------|-----------|
| `ScribanHelpers.cs` lines | 1718 | 0 (deleted) | Split into focused files |
| Largest helper file lines | 1718 | <250 | Manageable file size |
| Helper decision points (max per file) | 193 | <40 | Focused responsibility |
| Template `func` definitions | 18 | 0 | Logic moves to C# |
| Template max lines | 223 | <60 | Layout-only templates |
| Template max decision points | 52 | <15 | Minimal branching |
| Template total decision points | 159 | <50 | Reduced overall complexity |
| Anchor comments | 8 | 0 | No render-then-replace |
| Regex workarounds in renderer | 6 | 0 | Clean output without post-processing |

### Output Equivalence

All existing snapshot tests and UAT artifacts must produce **byte-identical or whitespace-equivalent** output after the refactoring. Any difference must be:
- Documented with a clear explanation
- Reviewed and approved as an intentional improvement (not a bug)
- Added to a "known differences" list in the PR description

### Change Locality

- Adding a new icon/format requires changes in ≤3 files
- New resource-specific templates require only 1 new file (no coordination with default.sbn)

## Validation Process

### Preventing Regressions

1. **Before refactoring**: Generate and commit baseline outputs
   ```bash
   scripts/generate-demo-artifacts.sh
   scripts/uat-run.sh
   git add artifacts/ tests/**/*.verified.*
   git commit -m "chore: capture baseline outputs before refactoring"
   ```

2. **After each incremental change**: Compare outputs
   ```bash
   git diff --stat artifacts/
   git diff tests/**/*.verified.*
   ```

3. **For any difference**:
   - Document the change and its cause
   - Classify as: **whitespace-only** | **intentional improvement** | **regression**
   - Regressions block the PR until fixed

4. **Final validation**:
   - All UAT tests pass
   - All unit/integration tests pass
   - Demo artifacts regenerated and committed
   - Metrics table updated with "After" values

### Parallel Rendering (Optional Safety Net)

During migration, optionally keep both code paths for comparison:

```csharp
public string Render(ReportModel model)
{
    var newResult = RenderWithNewApproach(model);
    
    #if DEBUG
    var oldResult = RenderWithOldApproach(model);
    if (Normalize(newResult) != Normalize(oldResult))
    {
        File.WriteAllText("/tmp/old.md", oldResult);
        File.WriteAllText("/tmp/new.md", newResult);
        throw new Exception("Output mismatch - compare /tmp/old.md vs /tmp/new.md");
    }
    #endif
    
    return newResult;
}
```

## Implementation Notes

### Phase 1: Infrastructure

1. Implement custom `ITemplateLoader` for Scriban that supports:
   - Loading partials by name (e.g., `_header`, `_resource`)
   - Resource-specific template resolution (e.g., `azurerm/role_assignment`)
   - Custom template directory support (existing feature)

2. Add `include` and `resolve_template` functions to template context

### Phase 2: Template Restructuring

1. Extract partials from `default.sbn`:
   - `_header.sbn` - Report header and summary table
   - `_footer.sbn` - Any closing content
   - `_resource.sbn` - Default resource block rendering

2. Update `default.sbn` to use includes instead of inline rendering

3. Remove anchor comments from all templates

### Phase 3: Resource Template Migration

1. Update each resource-specific template:
   - `azurerm/network_security_group.sbn`
   - `azurerm/firewall_network_rule_collection.sbn`
   - `azurerm/role_assignment.sbn`

2. Move `func` definitions to C# helpers

3. Simplify templates to layout-only

### Phase 4: Cleanup

1. Remove anchor-based replacement code from `MarkdownRenderer.cs`
2. Remove `NormalizeHeadingSpacing` workarounds if no longer needed
3. Update architecture documentation

## Migration Risk Mitigation

1. **Incremental migration**: Each phase can be completed and tested independently
2. **Feature flag**: Consider a temporary flag to switch between old/new rendering for comparison
3. **Parallel rendering test**: Render with both approaches and diff outputs during development
4. **Snapshot-driven development**: Update snapshots only after careful review of each change

---

## Decision 2: C# Code Organization

### Problem

The current `ScribanHelpers.cs` is a 1719-line "god class" mixing:
- Generic formatting (markdown escaping, diff formatting)
- Azure-specific formatting (scopes, roles, principals)
- Resource-specific logic (duplicated across templates as `func` definitions)

### Decision

**Split generic helpers into focused files + use rich ViewModels with FormattedValue pattern.**

### Helper File Structure

```
MarkdownGeneration/
├── Helpers/
│   ├── ScribanHelperRegistry.cs      # Registers all helpers with Scriban
│   ├── MarkdownEscaping.cs           # escape_markdown, escape_heading
│   ├── DiffFormatting.cs             # format_diff, diff_array
│   ├── CodeFormatting.cs             # format_code_table, format_code_summary
│   ├── ValueFormatting.cs            # format_value, format_large_value
│   ├── SemanticIcons.cs              # Icons for booleans, access, protocols, IPs, ports
│   └── AzureFormatting.cs            # azure_scope, azure_role_name, azure_principal
```

**Key principle:** All helpers are **generic** (not resource-specific). They format individual values based on semantic meaning (IP address, port, access level, etc.), not based on which template is using them.

### FormattedValue Pattern

For model properties that need both raw and formatted access, use wrapper types:

```csharp
/// <summary>
/// Wraps a value with its pre-computed formatted representation.
/// </summary>
public record FormattedValue<T>(T Raw, string Formatted);

/// <summary>
/// Wraps a list of values with a pre-computed formatted representation.
/// </summary>
public record FormattedList<T>(IReadOnlyList<T> Raw, string Formatted)
{
    public int Count => Raw.Count;
    public bool IsEmpty => Raw.Count == 0;
}
```

**Usage in ViewModels:**

```csharp
public class SecurityRuleViewModel
{
    public required string Name { get; init; }
    public required int Priority { get; init; }
    
    // FormattedValue provides both raw and formatted access
    public required FormattedValue<string> Access { get; init; }      
    public required FormattedValue<string> Protocol { get; init; }    
    public required FormattedList<string> SourceAddresses { get; init; }
    public required FormattedList<string> DestinationPorts { get; init; }
}
```

**Building the model:**

```csharp
new SecurityRuleViewModel
{
    Name = rule.Name,
    Priority = rule.Priority,
    Access = new FormattedValue<string>(
        Raw: rule.Access,
        Formatted: SemanticIcons.FormatAccess(rule.Access)  // "✅ Allow"
    ),
    SourceAddresses = new FormattedList<string>(
        Raw: rule.SourceAddressPrefixes,
        Formatted: string.Join(", ", rule.SourceAddressPrefixes.Select(SemanticIcons.FormatIpAddress))
    )
}
```

**Template usage (Scriban supports nested properties):**

```scriban
| {{ rule.access.formatted }} | {{ rule.protocol.formatted }} | {{ rule.source_addresses.formatted }} |
```

### Benefits

| Criterion | Before | After |
|-----------|--------|-------|
| God class | ❌ 1719 lines | ✅ ~100-200 lines per file |
| Helpers are generic | ⚠️ Mixed | ✅ All generic |
| Resource-specific logic | In templates | ✅ In ViewModels |
| Discoverability | ❌ Search huge file | ✅ One file per concern |
| Template simplicity | ⚠️ Call many helpers | ✅ Print `.formatted` |
| IntelliSense | ⚠️ Flat properties | ✅ Grouped (`.raw`, `.formatted`) |

### ViewModel Files

```
MarkdownGeneration/
├── Models/
│   ├── FormattedValue.cs             # Generic wrapper types
│   ├── ReportModel.cs                # Existing (unchanged)
│   ├── ResourceChangeModel.cs        # Enhanced with resource-specific ViewModels
│   ├── SecurityRuleViewModel.cs      # Pre-computed NSG rule display
│   ├── FirewallRuleViewModel.cs      # Pre-computed firewall rule display
│   └── RoleAssignmentViewModel.cs    # Pre-computed role assignment display
```

---

## Decision 3: Template Structure

### Problem

Current template organization:
- `default.sbn` - 120 lines with complex nested loops and inline logic
- Resource-specific templates (105-224 lines) duplicate outer structure and contain many `func` definitions
- No shared partials, leading to duplication

### Options Considered

#### Option 3A: All Partials
Split everything into small partials (~10+ files). Main template just orchestrates includes.

- Pros: Maximum DRY, small files
- Cons: Many files, must understand include pattern for custom templates

#### Option 3B: Self-Contained Templates
Fewer files, each template is complete and self-contained.

- Pros: Simple mental model, fewer files
- Cons: Duplication of header/summary across templates

#### Option 3C: Hybrid ✅ Selected
Shared partials for common layout, self-contained resource templates.

- Pros: DRY for common elements, simple resource template authoring
- Cons: Two patterns (minor)

### Decision

**Use hybrid approach: shared partials + self-contained resource templates.**

### Template File Structure

```
Templates/
├── default.sbn              # Main orchestrator (uses partials, loops resources)
├── _header.sbn              # Shared: report title, Terraform version
├── _summary.sbn             # Shared: summary table
├── _resource.sbn            # Default resource block rendering
├── summary.sbn              # Summary-only report (uses _header + _summary)
└── azurerm/
    ├── network_security_group.sbn
    ├── firewall_network_rule_collection.sbn
    └── role_assignment.sbn
```

### Main Template Example

```scriban
{{## default.sbn - Main report orchestrator ##}}

{{ include "_header" }}
{{ include "_summary" }}

## Resource Changes

{{ if module_changes.size == 0 }}
No changes.
{{ else }}
{{ for module in module_changes }}
{{ if for.index > 0 }}
---

{{ end }}
### 📦 Module: {{ if module.module_address }}{{ format_code_table(module.module_address) }}{{ else }}root{{ end }}

{{ for change in module.changes }}
{{ include (resolve_template change.type) change }}
{{ end }}
{{ end }}
{{ end }}
```

### Resource Template Example

Resource templates are **self-contained** - they render a complete resource block and receive a `ResourceChangeModel` (enhanced with ViewModels).

```scriban
{{## azurerm/network_security_group.sbn ##}}
{{## Receives: ResourceChangeModel with security_rules (list of SecurityRuleViewModel) ##}}

<details style="margin-bottom:12px;">
<summary>{{ summary_html }}</summary>
<br>

#### Security Rules

| Name | Priority | Access | Protocol | Source | Destination |
|------|----------|--------|----------|--------|-------------|
{{ for rule in security_rules }}
| {{ rule.name }} | {{ rule.priority }} | {{ rule.access.formatted }} | {{ rule.protocol.formatted }} | {{ rule.source_addresses.formatted }} | {{ rule.destination_ports.formatted }} |
{{ end }}

</details>
```

### Benefits

| Criterion | Before | After |
|-----------|--------|-------|
| Template lines | 105-224 | ~30-50 |
| `func` definitions | 5-10 per template | 0 |
| Anchor comments | Required | None |
| Adding resource template | Coordinate with default.sbn | Just add one file |
| Shared layout | Duplicated | Partials |

### Naming Conventions

- `_name.sbn` - Partial (included by other templates, not standalone)
- `name.sbn` - Standalone template (can be invoked directly)
- `provider/resource.sbn` - Resource-specific template

---

## References

- Current anchor-based approach: [docs/architecture.md](../../architecture.md#84-templating-architecture)
- Feature 024 experience (trial-and-error issues): [docs/features/024-visual-report-enhancements/](../024-visual-report-enhancements/)
- Scriban documentation: https://github.com/scriban/scriban
