# ADR-003 (feature 122): Inline action rendering, deferred indicator, and orphan-action fallback

## Status

Proposed

## Context

Per the spec (§ In Scope — H1, FR-H1.1 – FR-H1.8) and the maintainer's locked decisions:

- Lifecycle-triggered actions render **inline beneath their triggering resource** via the existing parent-child registry pattern (`ParentChildRelationshipRegistry`, `ReportModelBuilder.ParentChildMerging`).
- A **single generic** action renderer must handle every action type from every provider.
- **Deferred** actions (from `deferred_action_invocations[]`) render in the same inline location and are clearly marked.
- Sensitive (`config_sensitive`) and unknown (`config_unknown`) action config values must use the existing `SensitivityHelper`.
- Orphan actions — invoke-mode actions (`invoke_action_trigger` present, no triggering resource) and lifecycle actions whose `triggering_resource_address` is not present in `resource_changes[]` — must render in a defined fallback location (FR-H1.7).

This ADR locks: (1) how actions attach to parent resources, (2) the visual form of the deferred indicator, (3) the layout of the generic action renderer, (4) how sensitivity is applied, and (5) where orphan actions go.

The existing parent-child registry was designed for **resource-to-resource** parent-child relationships (e.g. an Azure firewall rule child resource attaching to an Azure firewall parent). Action invocations are not resources — they are a separate top-level array. The registry pattern therefore cannot be reused verbatim; this ADR adapts the *spirit* of the pattern (attach-children-to-parents-during-build) without trying to cram actions into the resource-relationship registry.

## Options considered

### (1) How actions attach to parent resources

**Option 1A (chosen): Build-time attachment via a new `ActionInvocationModel` collection on `ResourceChangeModel`.**

After `ReportModelBuilder` has built its `List<ResourceChangeModel>`, a new partial method (`ReportModelBuilder.Actions.cs`) iterates `plan.ActionInvocations` ∪ `plan.DeferredActionInvocations`, looks up the parent `ResourceChangeModel` by matching `lifecycle_action_trigger.triggering_resource_address` against the resource's `Address`, and appends an `ActionInvocationModel` (carrying an `IsDeferred` bool) to a new `IReadOnlyList<ActionInvocationModel> Actions { get; }` property on `ResourceChangeModel`. Orphans (no parent match, or `invoke_action_trigger`-only) flow into a separate top-level `ReportModel.OtherActions` collection — see option (5).

- Pros: minimally invasive — does not touch `ParentChildRelationshipRegistry` (which is built around `ParentChildRelationship` instances created by provider-specific extensions); keeps actions strictly out of provider code; the resource renderer can opt into rendering an actions sub-section without coupling to plan-context concerns.
- Cons: adds a property to `ResourceChangeModel`. Acceptable — it defaults to an empty list, so existing snapshots and tests are unaffected.

**Option 1B (rejected): Synthesise `ParentChildRelationship` entries for every action type.** Would require the registry to learn about non-resource children, leak provider knowledge (action `type` ↔ resource `type` mapping), and pollute `ReportModelBuilder.ParentChildMerging.cs` with action-specific code. Rejected.

### (2) Deferred-indicator visual form

**Option 2A (chosen): A `⏳` prefix on the action address line plus a leading callout `> ⏳ **Deferred** — will run on a subsequent apply.`**

- Pros: visible in both rendered HTML and raw markdown; survives table-cell rendering; consistent with the existing `🔒 Yes` / `⚠️` iconography in the report.
- Cons: emoji prefix collides if Terraform later introduces a real `⏳` glyph in addresses (not a realistic concern).

**Option 2B (rejected): A separate "Deferred actions" sub-section under each parent resource.** Rejected because the spec (FR-H1.5) explicitly requires deferred actions to render *in the same inline location* as non-deferred actions.

**Option 2C (rejected): Render-target-conditional badge HTML (e.g. `<span class="badge">deferred</span>` for HTML targets).** Rejected — the report is markdown-first and the renderer should not branch on `RenderTarget` for a basic indicator. Plain emoji + text works everywhere.

### (3) Generic action-renderer layout

**Option 3A (chosen): An `ActionInvocationSectionRenderer` that emits one H4 sub-section per parent resource, titled `🎬 Actions`, containing one entry per action.**

Each entry is rendered as:

1. An H5 (or bold paragraph if H5 is too noisy — see implementation notes) with the action address, action `type`, and `provider_name`.
2. A small two-column "Properties" table with rows: `Trigger` (one of `before_create`, `after_create`, `before_update`, `after_update`, …, or `invoke`), `Action block index` / `Actions list index` (only when present), and `Deferred` (`Yes` when applicable).
3. A `Config` block listing `config_values` with sensitivity-aware redaction (see option (4)).
4. A `Status` block (only when the action JSON carries any of the optional `status` / `error` / `diagnostics` fields parsed in ADR-001 — rendered as a fenced JSON code block when the shape is unknown, omitted otherwise).

For orphan actions in the "Other Actions" H2 section (see option (5)), the same per-action sub-renderer is used; only the framing heading differs.

- Pros: one renderer, used for both inline and orphan placements; provider-agnostic; status / diagnostic rendering degrades gracefully because the model fields are weakly-typed `JsonElement?` (per ADR-001).
- Cons: the H4 / H5 nesting under an existing H3 resource heading deepens the heading hierarchy. Implementation notes address this.

### (4) Sensitivity handling for `config_values`

**Option 4A (chosen): Reuse `JsonFlattener` + `SensitivityHelper.IsSensitiveAttribute` exactly as the resource renderer does.**

Flatten `config_values` to a `Dictionary<string, string?>`; flatten `config_sensitive` and `config_unknown` to the same shape; for each leaf, call `SensitivityHelper.IsSensitiveAttribute(key, configSensitive, configUnknown)` (re-purposing the second dictionary parameter for unknowns is *not* legitimate — instead, call `IsSensitiveAttribute` for the sensitive map and a parallel unknown-check for the unknown map; both helpers already exist in `SensitivityHelper`). When `--show-sensitive` is OFF, sensitive leaves render as `(sensitive)`; unknown leaves render as `(known after apply)`. When `--show-sensitive` is ON, sensitive leaves render verbatim (as they already do for resource attributes).

- Pros: single source of truth for sensitivity semantics; no parallel masking code (consistent with ADR-009); the existing AOT-trim-safe path is reused.
- Cons: requires the action renderer to depend on `JsonFlattener` and `SensitivityHelper`. Acceptable — both are already internal helpers in the same assembly.

**Option 4B (rejected): Mask in the model-builder, render verbatim.** Rejected because it would force the `IRenderContext.ShowSensitive` flag to be consulted at build time, which would break the existing pattern (sensitivity is applied at render time so the same `ReportModel` can be rendered with and without `--show-sensitive`).

### (5) Orphan-action fallback location

**Option 5A (chosen): A new top-level H2 section "Other Actions", rendered immediately after `Resource Changes` (and after Code Analysis "Other Findings"), and before the plan-context cluster from ADR-002 (Drift, Relevant Attributes).**

Section structure:

- H2: `🎬 Other Actions`
- Optional H3: `Invoke actions` — for actions with `invoke_action_trigger`.
- Optional H3: `Lifecycle actions without a matching resource change` — for lifecycle actions whose `triggering_resource_address` is not in `resource_changes[]` (e.g. resources in modules with no instance changes).
- Each H3 contains one entry per action, rendered by the same `ActionInvocationSectionRenderer` from option (3).
- The entire H2 is omitted when both sub-groups are empty.

- Pros: keeps "what will happen" (Resource Changes + Other Actions) contiguous; keeps "why this plan exists" (Drift, Relevant Attributes — see ADR-002) as a separate cluster; honours FR-H1.7's "clearly-labeled fallback location" requirement.
- Cons: introduces another H2. Acceptable — it's omitted on absence (NFR-1, AC-9).

**Option 5B (rejected): Fold orphan actions into the H2 plan-context section.** Rejected because invoke actions are imperative side-effects of the apply (active changes), not passive context like drift detection; mixing them with drift is misleading.

**Option 5C (rejected): Synthesise a fake "global" parent resource and attach orphan actions to it.** Rejected because there is no real parent and the synthesised entry would need to bypass every existing resource-renderer assumption (`Type`, `Address`, change-actions list, …).

## Decision

1. **Attachment**: build-time, via a new `Actions` property on `ResourceChangeModel` populated by a new `ReportModelBuilder.Actions.cs` partial. Orphans flow into a new `ReportModel.OtherActions` collection.
2. **Deferred indicator**: `⏳` prefix on the action address line plus a `> ⏳ **Deferred** — will run on a subsequent apply.` callout.
3. **Generic renderer**: `ActionInvocationSectionRenderer` rendering one `🎬 Actions` sub-section per parent (H4) and reused for the `🎬 Other Actions` H2 framing.
4. **Sensitivity**: reuse `JsonFlattener` + `SensitivityHelper` at render time; no parallel masking pipeline.
5. **Orphan fallback**: a new H2 "🎬 Other Actions" section between Resource Changes and the plan-context cluster, with two H3 sub-groups for invoke-mode and lifecycle-orphan actions.

## Consequences

### Positive

- A single generic renderer satisfies AC-3.
- Reusing `SensitivityHelper` satisfies AC-4 and keeps masking semantics consistent with ADR-009.
- Inline placement under the parent resource satisfies AC-1.
- Same-location rendering for deferred actions satisfies AC-2.
- Orphan actions are visible without crowding the primary changes section (FR-H1.7).

### Negative

- `ResourceChangeModel` gains a new property (`Actions`). Defaults to empty list; no observable effect on existing snapshots.
- Heading depth increases under deeply-nested module resources (H3 module → H3 resource → H4 actions → action entries). Implementation notes mitigate.
- The status / diagnostics field rendering is intentionally lenient (raw JSON code block when shape is unknown); QE should test both presence and absence.

## Implementation notes

For the Developer:

- Place the renderer in `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ActionInvocationSectionRenderer.cs`. Inject (or accept as parameter) the `IRenderContext` so `ShowSensitive` is honoured.
- For the per-action heading inside the inline `🎬 Actions` sub-section: prefer **bold paragraph + horizontal rule** rather than H5 to avoid heading-depth bloat. Use H4 only for the parent `🎬 Actions` group heading itself.
- `ResourceChangeModel.Actions` should be populated immediately after `MergeParentChildRelationships` in the build pipeline (so orphan detection runs against the post-merge resource list). Orphan detection: an action's parent address does not match any post-merge `ResourceChangeModel.Address`.
- Do **not** wire actions into `ParentChildRelationshipRegistry` — keep the action-attachment code in its own partial (`ReportModelBuilder.Actions.cs`).
- Provider-specific action rendering is out of scope (spec § Out of Scope). The renderer must not branch on `provider_name` for anything beyond display.
- Snapshot fixtures (per AC-11): one plan with a lifecycle-triggered action, one with a deferred action, one with an invoke-mode action, one with an orphan lifecycle action, one with a sensitive `config_values` leaf, one with an unknown `config_values` leaf.

## References

- Specification: [docs/features/122-terraform-1-15-support/specification.md](specification.md) §§ FR-H1.1–FR-H1.8, AC-1, AC-2, AC-3, AC-4
- Existing parent-child pattern: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ParentChildRelationshipRegistry.cs`, `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
- Existing sensitivity pipeline: `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs`, ADR-009
- Plan-JSON model extensions: [adr-001-plan-json-model-extensions.md](adr-001-plan-json-model-extensions.md)
- Layout interaction with H2 plan-context cluster: [adr-002-h2-report-layout.md](adr-002-h2-report-layout.md)
