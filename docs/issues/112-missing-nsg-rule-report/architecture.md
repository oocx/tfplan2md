# Architecture: Missing Separate NSG Rule In Generated Report

## Status

No architectural changes required.

## Context

The work item folder does not currently contain a `specification.md` artifact, so this architecture review is based on [analysis.md](analysis.md).

The issue analysis shows that the current architecture already has the right high-level separation of concerns:

- Core parent-child merging happens in `MarkdownGeneration` via `ReportModelBuilder.MergeParentChildRelationships()`.
- Provider-specific rendering happens under `Providers/AzureRM/Renderers/`.
- The default renderer already treats `ChildResourceGroups` as the canonical source for rendering merged child resources.

The defect is that the specialized AzureRM NSG renderer bypasses that canonical merged model and rebuilds its security-rule table directly from the parent resource state.

## Analysis

This bug is an implementation-level contract violation against the existing architecture, not a missing architecture capability.

The current architecture already establishes these responsibilities:

- `ReportModelBuilder.ParentChildMerging` is responsible for combining inline and separate child resources, attaching them to the parent as `ChildResourceGroups`, updating parent summaries, and removing separate child resources from the top-level display list.
- `DefaultResourceRenderer` is responsible for rendering `ChildResourceGroups` in the final report body.
- Specialized provider renderers may improve presentation, but they must not replace the core merged model with a second, competing source of truth.

For `azurerm_network_security_group`, the specialized `NsgRenderer` currently breaks that contract by rendering from `NetworkSecurityGroupViewModelFactory.Build(change.ResourceChange, ...)` only. That works for inline rule deltas present in the parent state, but it loses separate `azurerm_network_security_rule` children that were already merged into `change.ChildResourceGroups`.

## Options Considered

### Option 1: Make `ChildResourceGroups` the authoritative render input for specialized NSG output

Update `NsgRenderer` so that when the parent change already contains a `Security Rules` child group, the renderer uses that merged group as the source for the rendered table. The existing view-model path remains only as a fallback when no merged child group exists.

- Pros
  - Preserves a single source of truth for summaries, filtering, and body rendering.
  - Aligns NSG rendering with the existing parent-child architecture already used by the default renderer.
  - Fixes separate child `create`, `update`, `delete`, and mixed inline/separate scenarios without duplicating merge logic.
  - Keeps provider-specific presentation inside `Providers/AzureRM/`.
- Cons
  - Requires a small adaptation in the specialized renderer to consume child-group rows instead of only its current view model.

### Option 2: Extend the NSG-specific view model to merge separate child resources itself

Keep the current renderer shape, but teach the NSG-specific model-building path to discover and merge separate `azurerm_network_security_rule` children.

- Pros
  - Keeps the current renderer and view-model structure mostly intact.
  - Limits the code change to the AzureRM NSG-specific path.
- Cons
  - Duplicates core parent-child merge behavior in a provider-specific code path.
  - Creates two sources of truth for the same NSG child rows.
  - Increases the risk that summaries, filtering, and body rendering diverge again later.

### Option 3: Remove the specialized NSG renderer and rely entirely on the default renderer

Stop using `NsgRenderer` for NSGs and let the default renderer render the merged child-resource table.

- Pros
  - Fully reuses the existing canonical parent-child rendering path.
  - Minimizes special-case logic.
- Cons
  - Loses NSG-specific formatting and presentation already established by the AzureRM provider.
  - Is broader than necessary for this bug fix.

## Decision

Adopt **Option 1**.

`ChildResourceGroups` should remain the authoritative rendering input when parent-child merging has already produced a `Security Rules` group for an NSG. The specialized NSG renderer should format that merged data, not reconstruct an alternate rule set from parent state alone.

## Rationale

The strongest architectural property already present in this codebase is a staged pipeline with a canonical merged report model.

Once `MergeParentChildRelationships()` has:

- attached child rows to the parent,
- updated the parent summary, and
- removed the separate child resource from the top-level list,

the rendering layer must treat the resulting `ChildResourceGroups` as authoritative. Recomputing child rows later in a specialized renderer breaks that contract and reintroduces the same class of visibility bug this pipeline was designed to prevent.

Option 1 restores consistency without changing architectural boundaries:

- Core merge semantics stay in `MarkdownGeneration`.
- AzureRM-specific presentation stays in `Providers/AzureRM`.
- No new cross-cutting abstraction or ADR is needed.

## Implementation Guidance

This issue can be fixed using the existing architecture and patterns:

- In `NsgRenderer`, prefer the merged `Security Rules` entry from `change.ChildResourceGroups` when present.
- Keep `NetworkSecurityGroupViewModelFactory` as a fallback for cases where the parent state itself contains the relevant rule information and no merged child group exists.
- Do not duplicate parent-child merge logic inside AzureRM model factories or renderer-specific helpers.
- Preserve provider-specific presentation concerns in `Providers/AzureRM/`; do not move NSG-specific formatting logic into core `MarkdownGeneration`.
- Ensure the rendered NSG table can represent all child actions already encoded in merged rows: add, update, delete, unchanged, and mixed inline/separate sources.

## Testing Guidance

The Developer agent should treat this as a regression against the rendering contract and add tests before changing implementation:

- A no-op NSG parent with a separate created `azurerm_network_security_rule`
- A no-op NSG parent with a separate updated child rule
- A no-op NSG parent with a separate deleted child rule
- A mixed inline + separate child scenario to confirm all rows remain visible

## Components Affected

- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Renderers/AzureRmResourceRenderers.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/NetworkSecurityGroupViewModelFactory.cs`

## References

- [analysis.md](analysis.md)
- [../../architecture.md](../../architecture.md)
- [../../adr-010-scriban-removal-evaluation.md](../../adr-010-scriban-removal-evaluation.md)
