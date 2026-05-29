# ADR (feature 660): Inline Relevant Attributes — per-resource forced-replacement and depends-on annotations

## Status

Proposed

## Context

Feature 660 redesigns how `relevant_attributes[]` from Terraform 1.14+ plans are surfaced in the markdown report.

**Current behaviour:** `ReportRenderer.RenderRelevantAttributes` renders all `relevant_attributes[]` entries as a flat `## Relevant Attributes` H2 table at the bottom of the report (after the Drift section). This was the initial implementation (ADR-002, feature 122), which explicitly deferred per-resource correlation as out of scope.

**Required behaviour (feature 660):**
1. For each **replaced or destroyed** resource card, if any of its `replace_paths` trace back to an upstream `relevant_attributes` entry (via the resource's `ConfigurationReferences`), render a `⚠️ Forced replacement` blockquote line *inside* the card's `<details>` block, above the diff table.
2. For each replaced or destroyed resource card, a `🔗 Depends on:` (or `🔗 Also depends on:` when a forced-replacement line is already present) line lists all additional correlated `relevant_attributes` entries.
3. Attributes that could not be correlated to any specific resource are rendered in a collapsible `<details>` fallback section at the end of the report. The fallback section is omitted when all attributes were correlated.
4. The existing flat `## Relevant Attributes` H2 table is removed.
5. In-place updates and drift entries do not receive inline annotations.

### Relevant existing architecture

| Component | Location | Role |
|-----------|----------|------|
| `RelevantAttribute` | `Parsing/RelevantAttribute.cs` | Parsed wire record: `Resource` (string address) + `Attribute` (heterogeneous path array) |
| `RelevantAttributeModel` | `MarkdownGeneration/Models/RelevantAttributeModel.cs` | View model with pre-formatted `Resource` and `AttributePath` strings |
| `BuildRelevantAttributes` | `MarkdownGeneration/ReportModelBuilder.PlanContext.cs` | Converts `TerraformPlan.RelevantAttributes` → `List<RelevantAttributeModel>` |
| `ResourceChangeModel.ConfigurationReferences` | `MarkdownGeneration/ResourceChangeModel.cs` | `IReadOnlyDictionary<string, IReadOnlyList<string>>` — maps top-level attribute name → referenced resource addresses (populated by `ResourceChangeStage`) |
| `ResourceChangeModel.ReplacePaths` | `MarkdownGeneration/ResourceChangeModel.cs` | List of paths that forced a replacement |
| `ConfigurationReferenceResolver` | `Parsing/ConfigurationReferenceResolver.cs` | Builds the reference index from `configuration.root_module` JSON block |
| `ResourceSummaryPathFormatter.FormatReplacePath` | `MarkdownGeneration/Summaries/ResourceSummaryPathFormatter.cs` | Formats heterogeneous path array → dotted string (e.g. `["network_interface_ids",0]` → `"network_interface_ids[0]"`) |
| `ReportModel.RelevantAttributes` | `MarkdownGeneration/ReportModel.cs` | Currently holds all relevant attributes; passed to renderer |
| `RenderRelevantAttributes` | `MarkdownGeneration/Rendering/ReportRenderer.cs` (lines 118–141) | Current renderer: H2 heading + 2-column table |
| `DefaultResourceRenderer.Render` | `MarkdownGeneration/Rendering/DefaultResourceRenderer.cs` | Renders each resource card; already contains `RenderCodeAnalysisMetadata`, `RenderInlineActions` hooks above/below the diff table |

### Correlation algorithm (how to match)

`relevant_attributes[].resource` is the upstream resource address (e.g., `azurerm_network_interface.web`).

`ConfigurationReferences` maps a top-level attribute name (e.g., `network_interface_ids`) to reference strings extracted from `configuration.root_module.resources[].expressions[].references[]`. These reference strings may be exact resource addresses (`azurerm_network_interface.web`) or deeper paths (`azurerm_network_interface.web.id`).

A `RelevantAttributeModel` entry `ra` is **correlated** to a `ResourceChangeModel` `rc` when:

```
∃ refs in rc.ConfigurationReferences.Values : ∃ ref in refs :
    ref == ra.Resource (case-insensitive)  OR  ref.StartsWith(ra.Resource + ".")
```

A correlated entry specifically traces a **forced-replacement path** when the top-level attribute name (key in `ConfigurationReferences`) matches the top-level segment of one of the resource's `ReplacePaths`.

---

## Options Considered

### Option 1 (Rejected): Compute correlations inside the renderer at render time

The renderer (`ReportRenderer` or `DefaultResourceRenderer`) would receive both the `ResourceChangeModel` and the plan-level `relevant_attributes`, do the matching on every render pass, and emit the inline lines directly.

**Pros:** No model changes; renderer owns all logic.
**Cons:** Violates the model/renderer separation already established in this codebase — renderers are pure formatters, not computers. Correlating during render requires the renderer to scan all relevant attributes for every resource card, and to know which resources are being replaced/destroyed (to set the "changing in this plan" flag), which means the renderer needs a reference to `allChanges`. This is the same problem ADR-002 Option C2 rejected: "it forces every renderer (default and provider-specific) to know about relevant-attributes." Rejected for the same reason.

---

### Option 2 (Rejected): New dedicated pipeline stage `RelevantAttributeCorrelationStage`

A new `IRelevantAttributeCorrelationStage` interface and `RelevantAttributeCorrelationStage` class parallel to `ResourceChangeStage`. It takes `allChanges` + `relevantAttributes` and returns both annotated change models and uncorrelated attributes.

**Pros:** Fully isolated, independently unit-testable, consistent with existing staged pipeline design.
**Cons:** Adds a new interface and class for logic that is closely tied to the *existing* `BuildRelevantAttributes` method (already in `ReportModelBuilder.PlanContext.cs`). The correlation is lightweight (O(resources × attributes)); it does not require the infrastructure of a full stage (no service injection, no `Build()` contract). The staged pipeline was introduced to reduce `ReportModelBuilder.Build` complexity (feature 110); adding a stage for a small correlation step would be premature at this size.

---

### Option 3 (Chosen): Correlation in `ReportModelBuilder.PlanContext.cs`, annotations stored on `ResourceChangeModel`

Add a new `BuildInlineRelevantAttributeAnnotations` method in the existing `ReportModelBuilder.PlanContext.cs` partial class. Call it from `ReportModelBuilder.Build.cs` right after `BuildRelevantAttributes` and before the assembly stage. The method:
1. Builds a fast lookup: `Dictionary<string, List<RelevantAttributeModel>>` keyed by upstream resource address (normalized, case-insensitive) — from the full `relevant_attributes` list.
2. Builds the set of replaced/destroyed resource addresses from `allChanges` (for "changing in this plan" detection).
3. Iterates over `allChanges`; skips non-replace/delete actions (per spec).
4. For each eligible `ResourceChangeModel`:
   - Determines which `RelevantAttributeModel` entries are correlated via `ConfigurationReferences`.
   - Among correlated entries, identifies which are **forced-replacement correlated** (top-level `replace_path` attribute → `ConfigurationReferences[attr]` → upstream resource → `relevant_attributes` entry).
   - Marks each matched `RelevantAttributeModel` as correlated.
   - Stores two new properties on the model: `ForcedReplacementAnnotations` and `DependsOnAnnotations`.
5. Returns the uncorrelated `RelevantAttributeModel` entries (for the fallback section).

**Pros:**
- Consistent with existing model-build-time pre-computation (same pattern as `BuildCodeAnalysisReport`, `BuildActionInvocations`, etc.).
- No new interfaces, no new stages.
- Renderers remain pure formatters.
- The two new model properties are `internal` (like `ConfigurationReferences` and `Actions`), keeping the public surface minimal.
- Straightforward to unit-test via `ReportModelBuilder` tests with controlled plan inputs.

**Cons:**
- `ReportModelBuilder.PlanContext.cs` grows, but this is already the file for plan-context model building.
- Mutation of `ResourceChangeModel` instances after `ResourceChangeStage` completes (setting the new properties). This is already the pattern for `Summary`, `SummaryHtml`, `TagsBadges`, `Actions`, etc. in `ReportModelBuilder.Build.cs`.

**This option is chosen.**

---

## Decision

**Option 3** is the selected approach.

## Rationale

The codebase consistently computes model data in `ReportModelBuilder` partial classes and stores pre-computed values on `ResourceChangeModel` for pure rendering by the renderer layer. The correlation algorithm is a model-level concern (cross-referencing plan data structures), not a rendering concern. Storing results on `ResourceChangeModel` as `internal` properties is the established pattern for cross-cutting model enrichments.

A dedicated stage would be appropriate if the correlation were computationally expensive, required dependency injection, or needed to be replaceable — none of which applies here.

---

## Implementation Notes

High-level guidance for the Developer:

### 1. New model types (in `MarkdownGeneration/Models/`)

Add two new `internal sealed record` types:

```
ForcedReplacementAnnotation
{
    LocalAttribute         // e.g. "network_interface_ids" (top-level replace_path attr)
    UpstreamResource       // e.g. "azurerm_network_interface.web"
    UpstreamAttributePath  // e.g. "id" (pre-formatted via ResourceSummaryPathFormatter.FormatReplacePath)
    IsChangingInThisPlan   // true when upstream resource is itself replaced or destroyed
}

DependsOnAnnotation
{
    UpstreamResource       // e.g. "data.azurerm_client_config.current"
    UpstreamAttributePath  // e.g. "tenant_id"
    IsChangingInThisPlan   // true when upstream resource is itself replaced or destroyed
}
```

### 2. New properties on `ResourceChangeModel`

Add as `internal` properties (consistent with `ConfigurationReferences` and `Actions`):

```csharp
internal IReadOnlyList<ForcedReplacementAnnotation> ForcedReplacementAnnotations { get; set; } = [];
internal IReadOnlyList<DependsOnAnnotation> DependsOnAnnotations { get; set; } = [];
```

### 3. `BuildInlineRelevantAttributeAnnotations` in `ReportModelBuilder.PlanContext.cs`

```
private static IReadOnlyList<RelevantAttributeModel> BuildInlineRelevantAttributeAnnotations(
    IReadOnlyList<ResourceChangeModel> allChanges,
    IReadOnlyList<RelevantAttributeModel> allRelevantAttributes)
```

Algorithm:
1. If `allRelevantAttributes.Count == 0`, return empty list immediately (no-op, existing plans unaffected).
2. Build `byUpstream`: `Dictionary<string, List<RelevantAttributeModel>>` keyed by `ra.Resource`, case-insensitive.
3. Build `replacedOrDestroyedAddresses`: `HashSet<string>` of addresses where `action == "replace"` or `action == "delete"`.
4. Track a `HashSet<RelevantAttributeModel> correlated` set (by reference equality).
5. For each `ResourceChangeModel rc` where `rc.Action is "replace" or "delete"`:
   a. **Find all correlated `RelevantAttributeModel` entries:**  
      For each `(attr, refs)` in `rc.ConfigurationReferences`, for each `ref` in `refs`:  
      Normalize `ref` to a resource address (strip trailing `.attribute` suffix: first segment that is not part of a resource address pattern — heuristic: resource address is `provider_type.name` or `data.provider_type.name`; strip everything after the second or third dot).  
      Look up `byUpstream[normalizedRef]` to find matching `RelevantAttributeModel` entries.

   b. **Identify forced-replacement entries:**  
      For each `replacePath` in `rc.ReplacePaths`:  
      `topAttr = GetTopLevelAttributeName(FormatReplacePath(replacePath))`  
      `refs = rc.ConfigurationReferences[topAttr]`  
      Intersect with correlated entries from step (a) to produce `forcedEntries`.

   c. **Set `ForcedReplacementAnnotations`:**  
      For each forced `(topAttr, ra)` pair:  
      `isChanging = replacedOrDestroyedAddresses.Contains(ra.Resource)`  
      Append `ForcedReplacementAnnotation`.  
      Add `ra` to `correlated`.

   d. **Set `DependsOnAnnotations`:**  
      From all correlated entries in step (a) that are NOT in `forcedEntries`:  
      `isChanging = replacedOrDestroyedAddresses.Contains(ra.Resource)`  
      Append `DependsOnAnnotation`.  
      Add `ra` to `correlated`.

6. Return `allRelevantAttributes.Except(correlated).ToList()` as the uncorrelated fallback list.

> **Note on reference-address normalization:** `ConfigurationReferences` values are reference strings from Terraform's `configuration.root_module.resources[].expressions[].references[]`. These are typically the full Terraform address (`azurerm_network_interface.web`) or a deeper path (`azurerm_network_interface.web.id`). `relevant_attributes[].resource` is always the resource address (no attribute suffix). The normalization rule: strip trailing `.attribute` segments by finding the shortest prefix of the reference string that is present as a key in `byUpstream` — or, simpler and sufficient: the upstream resource address is the part up to and including the second dot component for normal resources (`type.name`) or third for data sources (`data.type.name`). Use a utility method `NormalizeReferenceToResourceAddress`.

### 4. Call site in `ReportModelBuilder.Build.cs`

```csharp
var relevantAttributes = BuildRelevantAttributes(plan);
var uncorrelatedRelevantAttributes = BuildInlineRelevantAttributeAnnotations(allChanges, relevantAttributes);
```

Pass `uncorrelatedRelevantAttributes` (not `relevantAttributes`) to `ReportAssemblyInput`.

### 5. `DefaultResourceRenderer` — render inline annotations

Add `RenderInlineRelevantAttributeAnnotations(writer, change)` in `DefaultResourceRenderer.Helpers.cs`.  
Call it from `DefaultResourceRenderer.Render`, immediately **after** `WriteDetailsHeader` and `RenderCodeAnalysisMetadata` and **before** the attribute table.

Rendering rules:
- If `ForcedReplacementAnnotations.Count > 0`:  
  For each annotation: emit a blockquote line:  
  `> ⚠️ **Forced replacement** — \`{localAttr}\` reads \`{upstreamResource}.{upstreamAttributePath}\`{changingPhrase}`  
  where `changingPhrase` is `, which is **changing in this plan**.` when `IsChangingInThisPlan`, else `.` (period only).
  
- If `DependsOnAnnotations.Count > 0`:  
  Determine label: if there are also forced replacement annotations, use `🔗 **Also depends on:**`; otherwise `🔗 **Depends on:**`.  
  Emit a single blockquote line listing all upstream references as comma-separated inline code:  
  `` > {label} `{ra.Resource}.{ra.AttributePath}` [⚠️] ``, where `[⚠️]` (the ⚠️ marker) is appended when `ra.IsChangingInThisPlan`.
  
- If both lists are empty, nothing is emitted (preserves existing snapshot output for plans without relevant attributes).

### 6. `ReportRenderer.RenderRelevantAttributes` — fallback section

Replace the current H2 table with a collapsible `<details>` block:

```markdown
<details>
<summary>🔗 Other plan inputs ({N}) — read by this plan but not tied to a specific change</summary>

> These existing values were read to compute the plan. If they change before apply, the plan may be stale.

- `{ra.Resource}.{ra.AttributePath}`
...

</details>
```

Omit the section entirely when `attributes.Count == 0`.

### 7. Snapshot test implications

- Existing snapshot tests that include `relevant_attributes` in the plan JSON will need to be updated: the H2 table is gone and replaced by inline annotations and/or a collapsible fallback.
- New snapshot fixtures (per spec Success Criteria) must cover:
  - A forced-replacement cascade (e.g., NIC ID forces VM replacement)
  - A combined card (forced replacement + additional depends-on)
  - An uncorrelated-only fallback (attributes that cannot be correlated)
  - An all-correlated plan (fallback section omitted entirely)
  - A plan without `relevant_attributes[]` (no regression)

### 8. Files to create or modify

| File | Change |
|------|--------|
| `MarkdownGeneration/Models/ForcedReplacementAnnotation.cs` | **New** — `internal sealed record` |
| `MarkdownGeneration/Models/DependsOnAnnotation.cs` | **New** — `internal sealed record` |
| `MarkdownGeneration/ResourceChangeModel.cs` | Add 2 internal properties |
| `MarkdownGeneration/ReportModelBuilder.PlanContext.cs` | Add `BuildInlineRelevantAttributeAnnotations` |
| `MarkdownGeneration/ReportModelBuilder.Build.cs` | Update call to pass uncorrelated list |
| `MarkdownGeneration/Rendering/DefaultResourceRenderer.cs` | Call new helper after `RenderCodeAnalysisMetadata` |
| `MarkdownGeneration/Rendering/DefaultResourceRenderer.Helpers.cs` | Add `RenderInlineRelevantAttributeAnnotations` |
| `MarkdownGeneration/Rendering/ReportRenderer.cs` | Replace H2 table with `<details>` fallback |
| Snapshot `.md` files referencing `relevant_attributes` plans | **Update** — existing snapshots change |
| New snapshot test plan JSON + expected `.md` files | **New** — see item 7 above |

---

## Consequences

### Positive

- Reviewers see the causal chain of a forced replacement directly on the resource card, without having to cross-reference a separate table.
- The "Also depends on" line gives full visibility into all upstream dependencies, including cross-resource blast radius.
- The fallback `<details>` section preserves all plan information (nothing is silently dropped), but collapses it so it does not dominate the report.
- Plans without `relevant_attributes[]` produce byte-identical output to today (no regression for pre-1.14 plans).
- Drift entries are excluded from inline annotations (consistent with spec intent: drift is observed state, not planned changes).
- The architecture boundary is maintained: all Terraform plan parsing and model correlation stays in `Parsing/` and `MarkdownGeneration/`, not in provider-specific code.

### Negative

- `DefaultResourceRenderer.Render` gains another conditional rendering step. Mitigated by extracting it as a named private helper (`RenderInlineRelevantAttributeAnnotations`), consistent with the existing `RenderInlineActions` pattern.
- All snapshot tests that exercise plans containing `relevant_attributes[]` will require updates (the H2 table disappears). This is expected and can be handled with the `update-test-snapshots` skill.
- The reference-address normalization heuristic (stripping trailing `.attribute` suffix from configuration reference strings) is slightly imprecise for unusual reference patterns. The Developer should use a conservative approach: check against `byUpstream` keys first, fall back to prefix matching. Add a dedicated unit test for this normalization in `ConfigurationReferenceResolverTests` or a new `RelevantAttributeCorrelatorTests`.
- `ForcedReplacementAnnotations` and `DependsOnAnnotations` on `ResourceChangeModel` break the existing pattern of `ResourceChangeModel` being populated entirely within `ResourceChangeStage` — these properties are set later in `ReportModelBuilder`. This is already an established exception for `Summary`, `SummaryHtml`, `TagsBadges`, `ChildResourceGroups`, `CodeAnalysisFindings`, and `Actions`.

## References

- Feature Specification: `docs/features/660-inline-relevant-attributes/specification.md`
- ADR-002 (feature 122): `docs/features/122-terraform-1-15-support/adr-002-h2-report-layout.md` — original relevant-attributes placement decision, Option C2 (per-resource footnotes) was rejected at the time.
- Relevant parsing: `src/Oocx.TfPlan2Md/Parsing/RelevantAttribute.cs`, `RelevantAttributePathConverter.cs`
- Existing model: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/RelevantAttributeModel.cs`
- Current renderer: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` (lines 110–141)
- Configuration reference resolver: `src/Oocx.TfPlan2Md/Parsing/ConfigurationReferenceResolver.cs`
- Path formatter: `src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryPathFormatter.cs`
- Inline actions pattern (model enrichment post-stage): `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs` (`RenderInlineActions`)
