# ADR-001 (feature 122): Plan-JSON model extensions for Terraform 1.14 / 1.15

## Status

Proposed

## Context

Terraform 1.14 / 1.15 added several optional top-level (and configuration-level) fields to the `terraform show -json` plan format while leaving `format_version` at `"1.2"`. The current parsed model in `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` (a `record` with positional optional parameters) silently discards every one of them:

- `action_invocations[]` and `deferred_action_invocations[]` (1.14)
- `resource_drift[]`, `relevant_attributes[]`, top-level `applyable` / `complete` / `errored` (pre-existing, never modeled)
- `configuration.root_module.variables[<name>].deprecated`, `configuration.root_module.outputs[<name>].{deprecated,type}` (1.15)

This ADR locks the parsing-model layout that downstream ADRs (002, 003, 004) build on. Constraints:

- All new fields are additive and must be optional (NFR-1, NFR-5, AC-9).
- The plan deserializer is source-generated through `TfPlanJsonContext` (`src/Oocx.TfPlan2Md/Parsing/TfPlanJsonContext.cs`) — every new record type must be registered there.
- `configuration` is currently parsed as `JsonElement?` (a raw blob queried later by `ConfigurationReferenceResolver`). The 1.15 deprecation fields live deep inside that blob.
- Provider-specific logic must stay out of the core plan model (architecture boundary; see [docs/architecture.md](../../architecture.md) and ADR-007).

## Options considered

### Option A (chosen): Extend `TerraformPlan` with optional fields; introduce one new record per new top-level shape; query `configuration` for deprecations via a helper

- Add optional properties to the `TerraformPlan` record:
  - `IReadOnlyList<ActionInvocation>? ActionInvocations`
  - `IReadOnlyList<ActionInvocation>? DeferredActionInvocations`
  - `IReadOnlyList<ResourceChange>? ResourceDrift` (spec confirms drift has the same shape as `resource_changes[]`, so the existing `ResourceChange` record is reused)
  - `IReadOnlyList<RelevantAttribute>? RelevantAttributes`
  - `bool? Applyable`, `bool? Complete`, `bool? Errored`
- Introduce three new records in `Parsing/`:
  - `ActionInvocation` — `address`, `type`, `name`, `provider_name`, `config_values` (JsonElement?), `config_sensitive` (JsonElement?), `config_unknown` (JsonElement?), `lifecycle_action_trigger` (`LifecycleActionTrigger?`), `invoke_action_trigger` (`InvokeActionTrigger?`), plus an optional `JsonElement? Status` and `JsonElement? Diagnostics` to absorb whatever status / error / diagnostic field shape Terraform emits without binding hard to it (FR-H1.8).
  - `LifecycleActionTrigger` — `triggering_resource_address`, `action_trigger_event`, `action_trigger_block_index`, `actions_list_index`.
  - `InvokeActionTrigger` — minimal record (one nullable `string? Reason` placeholder is sufficient; presence of the property in JSON is what matters per FR-H1.1).
  - `RelevantAttribute` — `resource` (string), `attribute` (`IReadOnlyList<JsonElement>` — Terraform emits the path as a heterogeneous array of strings and ints, identical in shape to `replace_paths` items; reuse the same parsing approach as `ReplacePathsConverter`).
- The "deferred" flag is **not** stored on `ActionInvocation` itself. Instead, `ReportModelBuilder` carries deferred-ness as a separate boolean attached to the in-memory action model it builds (see ADR-003). Rationale: the wire-level shape of an entry in `deferred_action_invocations[]` is identical to `action_invocations[]`; deferred-ness is purely positional, so encoding it on the wire record would be lying about the JSON.
- For `configuration.root_module.{variables,outputs}[<name>].{deprecated,type}`, do **not** introduce typed configuration records. Instead, add a small `ConfigurationDeprecationReader` helper next to `ConfigurationReferenceResolver` that walks the existing `JsonElement` and yields `(name, kind, deprecationMessage, optionalCtyType)` tuples on demand. This keeps the configuration blob untyped (consistent with how it is treated today) and isolates the deprecation lookup behind a single API used by ADR-004.

Pros:
- Strictly additive: existing call-sites compile unchanged because the new properties default to `null`.
- One-to-one mapping between wire shape and model record — easy to reason about and testable in isolation.
- Reuses `ResourceChange` for drift, which gives us correct sensitivity handling, replace-path handling, and import handling for free.
- Avoids inventing parallel typed configuration records (which would duplicate parsing for an already-untyped blob).

Cons:
- Adds five nullable properties to the public `TerraformPlan` record. The record is a public type (used by `TerraformPlanParser`); adding optional nullable properties is non-breaking for callers but the surface area grows.
- `ActionInvocation.Status` / `Diagnostics` remain raw `JsonElement?` until Terraform pins their shape — renderer (ADR-003) must handle the dynamic shape defensively.

### Option B: Dedicated `PlanContext` sub-record

Group `applyable`, `complete`, `errored`, `resource_drift`, `relevant_attributes` into a synthetic `PlanContext` record nested under `TerraformPlan`. Pros: cleaner top-level surface. Cons: doesn't match the wire format (these are sibling top-level fields), forces a custom `JsonConverter` to flatten them at parse time, and adds a second translation layer for no real benefit. Rejected.

### Option C: Strongly-type `configuration.root_module`

Replace the `JsonElement? Configuration` with a typed graph (`Configuration` → `RootModule` → `Variables` / `Outputs`). Pros: deprecation lookup becomes a property access. Cons: very large blast radius — `ConfigurationReferenceResolver` would need to be rewritten, and the configuration tree contains many other shapes (provider configs, module calls, expressions) we deliberately leave untyped today. Out of scope for this feature. Rejected.

## Decision

Adopt **Option A**.

## Consequences

### Positive

- Backwards-compatible parsing: a Terraform 1.13 plan deserializes into a `TerraformPlan` whose new properties are all `null` (NFR-1, AC-9).
- Each new wire field is captured in exactly one place; downstream ADRs reference well-defined types.
- Reusing `ResourceChange` for drift means drift entries automatically participate in the existing sensitivity, replace-path, and importing logic.
- Configuration parsing surface is unchanged for everything except deprecation lookup.

### Negative

- `TerraformPlan` and `TfPlanJsonContext` are touched. Every new record (`ActionInvocation`, `LifecycleActionTrigger`, `InvokeActionTrigger`, `RelevantAttribute`) must be registered in the source-generation context or System.Text.Json AOT-trimming will fail at runtime.
- `ActionInvocation.Status` / `Diagnostics` are intentionally weakly typed; the renderer must defensively format them (and tests must cover both presence and absence — see AC-11).

## Implementation notes

For the Developer:

1. Add the five optional properties to `TerraformPlan` (nullable, default `null`). Keep `record` positional syntax to remain consistent with the existing style; place new parameters at the end with default values to preserve compatibility for callers using positional construction.
2. Create new records in `src/Oocx.TfPlan2Md/Parsing/`: `ActionInvocation.cs`, `LifecycleActionTrigger.cs`, `InvokeActionTrigger.cs`, `RelevantAttribute.cs`. Use the same `[JsonPropertyName(...)]` attribute style already established in `TerraformPlan.cs`.
3. Register every new record in `TfPlanJsonContext` (and in `IReadOnlyList<>`/`Dictionary<>` wrappers as needed).
4. For `RelevantAttribute.attribute`, reuse the parsing pattern from `ReplacePathsConverter` — the path is emitted by Terraform as a heterogeneous array of strings (object keys) and ints (array indices).
5. Add a `ConfigurationDeprecationReader` static helper (location: `src/Oocx.TfPlan2Md/Parsing/`) that takes the plan's `Configuration` JsonElement and yields deprecated variables / outputs. Used by ADR-004.
6. Do **not** add a `Deferred` flag to `ActionInvocation` — see ADR-003 for how deferred-ness is carried.

## References

- Specification: [docs/features/122-terraform-1-15-support/specification.md](specification.md) §§ FR-H1.1, FR-H1.2, FR-H2.1–FR-H2.3, FR-M2.1–FR-M2.3, NFR-1, NFR-5
- Analysis: [docs/features/122-terraform-1-15-support/analysis.md](analysis.md)
- Existing pattern: `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`, `TfPlanJsonContext.cs`, `ReplacePathsConverter.cs`
