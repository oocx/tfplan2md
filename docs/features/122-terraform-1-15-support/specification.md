# Feature: Terraform 1.14 / 1.15 Plan-JSON Support

## Overview

Terraform 1.14 and 1.15 introduced new content in the `terraform show -json` plan format that `tfplan2md` does not currently parse or render. As a result, plan reports generated for users on Terraform 1.14+ silently omit information that is essential for reviewing infrastructure changes — most notably the new top-level `action_invocations[]` produced by `lifecycle { action_trigger { … } }` blocks and provider-shipped Actions (e.g. `aws_lambda_invoke`, `aws_cloudfront_create_invalidation`). Pre-existing top-level fields that have always been silently dropped (`resource_drift[]`, `relevant_attributes[]`, plan status booleans `applyable` / `complete` / `errored`) and the new `deprecated` flag on variables/outputs introduced in 1.15 are also out of reach for reviewers today.

This feature bundles three related plan-JSON gaps into a single deliverable so that tfplan2md can be confidently described as supporting Terraform 1.14 and 1.15 plan output:

- **H1 — Action invocations** (new in 1.14): `action_invocations[]` and `deferred_action_invocations[]`.
- **H2 — Plan-context awareness**: `resource_drift[]`, `relevant_attributes[]`, and the plan status booleans `applyable` / `complete` / `errored`.
- **M2 — Deprecations** (new in 1.15): `deprecated` on `configuration.root_module.variables[*]` and `configuration.root_module.outputs[*]`, plus explicit `outputs[*].type` if useful in rendering.

The full background, schema references, and gap analysis are in [`analysis.md`](./analysis.md). The format version of the plan JSON remains `"1.2"` across 1.13 → 1.14 → 1.15, so all additions are purely additive and tfplan2md must continue to work unchanged for plans produced by Terraform 1.13 (and earlier supported versions) where these fields are absent.

## User Goals

- **Reviewers running Terraform 1.14+ workflows** need to see every effect of a plan in the rendered markdown report — including provider Actions triggered by resource lifecycle events and Actions invoked via `terraform plan -invoke=…`. Today these are silently omitted.
- **Reviewers of plans run against drifted state** need a clear callout that the plan exists in part because reality drifted, and need to understand which attributes from other resources triggered cascading changes.
- **Reviewers of erroring or no-op plans** need the report header to honestly reflect whether the plan is applyable, complete, or errored, instead of looking like an ordinary change set.
- **Module authors and consumers adopting Terraform 1.15** need migration warnings surfaced when a plan references variables or outputs flagged `deprecated`, so they can plan migrations alongside the change.
- **All users** need this to work without changing their CLI invocation, opting into a flag, or maintaining hand-crafted templates per Terraform version.

## Scope

### In Scope — H1: Action invocations (Terraform 1.14)

- Parse the new top-level `action_invocations[]` field on the plan JSON.
- Parse the new top-level `deferred_action_invocations[]` field on the plan JSON.
- Render every action **inline under the resource that triggers it**, using the existing parent-child resource registry pattern in tfplan2md (the same pattern already used for inline children such as Azure firewall rules and Azure AD group members; see `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ParentChildRelationshipRegistry.cs` and `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`). The `lifecycle_action_trigger.triggering_resource_address` field on each action provides the linkage to the parent resource.
- Provide a **single generic action renderer** that can render any action type from any provider. The renderer must, at minimum, surface:
  - The action's address (`address`), action type (`type`), name (`name`), and provider (`provider_name`).
  - The trigger event (`lifecycle_action_trigger.action_trigger_event` — one of `before_create`, `after_create`, `before_update`, `after_update`, etc.) and a clear "invoke" indicator when the action carries `invoke_action_trigger` instead.
  - The action's `config_values`, with sensitivity-aware redaction reusing the existing `SensitivityHelper` (`src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/SensitivityHelper.cs`) for `config_sensitive` / `config_unknown`.
  - Any status, error, or diagnostic field present in the JSON for the action invocation (rendered when present, omitted gracefully when absent).
- For `deferred_action_invocations[]`, render the action in the same inline location under the triggering resource and clearly indicate that it is **deferred** (e.g. a "deferred" badge or label).
- Plans that contain actions for resources which are themselves not in `resource_changes[]` (e.g. invoke-mode actions with no associated resource change, or actions whose triggering resource is in a module that yields no instance changes) MUST still render those actions. The renderer must define a sensible fallback location when no parent resource section exists.
- Provider-specific action rendering is **not** part of this feature (see Out of Scope).

### In Scope — H2: Plan-context awareness

- Parse top-level `resource_drift[]` (same shape as `resource_changes[]`).
- Parse top-level booleans `applyable`, `complete`, and `errored` (default to absent / unknown when not present in older plan JSON).
- Parse top-level `relevant_attributes[]` (each entry is an object identifying the resource and attribute path that influenced changes in this plan).
- Render these as **summary / context information** in an appropriate top-level section of the report. Detailed layout is left to the Architect, but the proposed default is:
  - A **plan status banner / callout** placed near the top of the report (immediately after the report title and any existing summary line), showing the values of `applyable`, `complete`, and `errored` when any of them is false / true respectively in a way that matters for the reviewer (e.g. a clear red/orange callout when `errored` is true or `applyable` is false).
  - A **"Drift detected" section** rendered like a slimmed-down resource-changes section, listing each entry from `resource_drift[]`. Placement: after the existing "Summary" / "Resource changes" sections so it does not push primary changes below the fold, but before the Outputs section.
  - A **"Relevant attributes" disclosure** — either a top-level subsection summarising upstream dependencies, or per-resource footnotes attached to the resources whose changes were influenced. The Architect chooses the final placement; either approach is acceptable as long as relevant_attributes are surfaced somewhere reviewers will see them.

### In Scope — M2: Deprecations (Terraform 1.15)

- Parse `configuration.root_module.variables[<name>].deprecated` (string) and `configuration.root_module.outputs[<name>].deprecated` (string).
- Parse `configuration.root_module.outputs[<name>].type` (cty type, JSON-encoded) when present, and surface it in the rendered output if it improves clarity (e.g. alongside the output's value).
- **Surface deprecations through the existing code-analysis warnings mechanism.** The maintainer's instruction is explicit: do not invent a new warnings system. Concretely, deprecations should flow through the same model and renderer that today handle SARIF code-analysis warnings:
  - Production model: `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/CodeAnalysisWarningModel.cs`.
  - Source model: `src/Oocx.TfPlan2Md/CodeAnalysis/CodeAnalysisWarning.cs`.
  - Builder: `BuildWarningModels` in `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.CodeAnalysis.cs`.
  - Renderer: the "Code Analysis Warnings" section in `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/CodeAnalysisSectionRenderer.cs` (around the `Heading("Code Analysis Warnings", 3)` / `⚠️ Warning:` block).
- Each deprecated variable or output that appears in the plan should produce one warning entry whose message includes the variable/output name, the deprecation message from Terraform, and an indication of whether it is a variable or an output. The Architect may decide the exact wording and whether to relabel the existing "Code Analysis Warnings" heading to a more general "Warnings" heading once it carries entries from multiple sources.

### In Scope — Cross-cutting

- **Backwards-compatible parsing.** Plan JSON from Terraform 1.13 (and earlier supported versions) that lacks any of these fields MUST continue to parse and render exactly as it does today. All new fields are optional in the parsed model.
- **Always-on rendering.** No CLI flag, environment variable, or template-author opt-in is introduced for any of the new sections. Reviewers do not have to remember to enable anything.
- **Hand-crafted JSON test fixtures.** New, minimal JSON plan files are created by hand (placed alongside existing fixtures under `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`) to exercise each new field. There is no requirement to generate fixtures from a real Terraform 1.14 / 1.15 run.
- **Documentation.** The feature is recorded in `docs/features.md` and any user-facing capability lists, alongside this feature folder.

### Out of Scope

- **Stacks (`tfstacks`) JSON.** Stacks have a separate JSON format. Adding support is a distinct feature work-item if/when demand appears.
- **Provider-specific action renderers.** No AWS-, AzureRM-, or other provider-specific action rendering. A single generic renderer ships first; provider-specific renderers can be added in follow-up features once the generic renderer is in production.
- **Opt-in CLI flags or template-author migration windows.** All new sections render unconditionally.
- **Other items from the analysis still left for follow-up features:**
  - M1 (`relevant_attributes` as an explicit per-resource "why" column) — only the basic surfacing required by H2 is in scope; a richer "why" column is a separate feature.
  - M3 (`checks[]` results), L1 (`index_unknown`), L2 (`deposed`), L3 (`identity` / `identity_schema_version`), L4 (`deferred_changes[]` for non-action items), L5 (Stacks), L6 (defensive ephemeral pre/postcondition fixture).
- **New configuration semantics** beyond surfacing the data — tfplan2md does not validate, suppress, or fail on deprecations or errored plans; it only renders them.

## User Experience

### How reviewers will see the new information

- **Action invocations:** Within an existing resource section (e.g. an `aws_lambda_function` being updated), a new inline subsection or table lists every action triggered by that resource's lifecycle. Each row shows the action address, type, provider, trigger event, and (sensitivity-aware) `config_values`. Deferred invocations are visually marked. Invoke-mode actions and actions whose triggering resource has no change section appear in a fallback location designed by the Architect.
- **Plan status banner:** Immediately after the report title, a banner makes it visually obvious when a plan is not applyable, is incomplete, or has errored. For ordinary applyable + complete + not-errored plans, the banner is either omitted or shown as a quiet confirmation — the Architect decides.
- **Drift detected section:** A clearly titled section appears between the main change summary and the Outputs section whenever `resource_drift[]` is non-empty. When it is empty, the section is omitted entirely.
- **Relevant attributes:** Surfaced in whatever layout the Architect selects — but reviewers must be able to understand, for at least one realistic plan, which upstream attributes triggered downstream changes.
- **Deprecation warnings:** Each deprecated variable or output referenced by the plan appears as a warning entry under the existing code-analysis warnings section (or its renamed equivalent), using the same `⚠️` formatting reviewers already recognise.
- **Plans from Terraform 1.13:** No visible change — none of the new sections appear because the underlying fields are absent.

## Functional Requirements

### FR-H1 — Action invocations
- **FR-H1.1** Parse `action_invocations[]` from the plan JSON into a strongly-typed model that captures `address`, `type`, `name`, `provider_name`, `config_values`, `config_sensitive`, `config_unknown`, `lifecycle_action_trigger` (with `triggering_resource_address`, `action_trigger_event`, `action_trigger_block_index`, `actions_list_index`), and `invoke_action_trigger`.
- **FR-H1.2** Parse `deferred_action_invocations[]` into the same model with a "deferred" flag.
- **FR-H1.3** Render lifecycle-triggered actions inline beneath the triggering resource using the existing parent-child resource registry pattern (`ParentChildRelationshipRegistry` and `ReportModelBuilder.ParentChildMerging`).
- **FR-H1.4** Provide a single generic action renderer that handles any action type and any provider.
- **FR-H1.5** Deferred actions render in the same inline location as their non-deferred counterparts and carry a clear "deferred" indicator.
- **FR-H1.6** Sensitive and unknown config values must respect the existing sensitivity-handling pipeline (`SensitivityHelper`).
- **FR-H1.7** Actions with no resolvable triggering resource section in the report (invoke-mode actions; lifecycle actions whose target is not in `resource_changes[]`) still render in a clearly-labeled fallback location.
- **FR-H1.8** Status / error / diagnostic fields present on the action JSON are rendered when present, omitted when absent.

### FR-H2 — Plan-context awareness
- **FR-H2.1** Parse `resource_drift[]` using the same shape as `resource_changes[]`.
- **FR-H2.2** Parse the top-level booleans `applyable`, `complete`, and `errored` (each optional).
- **FR-H2.3** Parse `relevant_attributes[]`.
- **FR-H2.4** Render a plan status banner that visibly reflects non-default values of `applyable`, `complete`, and `errored`.
- **FR-H2.5** Render a "Drift detected" section listing entries from `resource_drift[]`. The section is omitted when the array is empty or absent.
- **FR-H2.6** Surface `relevant_attributes[]` in a location chosen by the Architect such that reviewers can see which upstream attributes influenced this plan.
- **FR-H2.7** All H2 sections are omitted when their underlying data is absent.

### FR-M2 — Deprecations
- **FR-M2.1** Parse `configuration.root_module.variables[<name>].deprecated`.
- **FR-M2.2** Parse `configuration.root_module.outputs[<name>].deprecated`.
- **FR-M2.3** Parse `configuration.root_module.outputs[<name>].type` and incorporate it into output rendering when it improves clarity.
- **FR-M2.4** For each deprecated variable or output that appears in the plan, emit a warning entry through the existing code-analysis warnings model (`CodeAnalysisWarningModel` / `BuildWarningModels` in `ReportModelBuilder.CodeAnalysis.cs`) so that it is rendered by the existing warnings renderer (`CodeAnalysisSectionRenderer`).
- **FR-M2.5** Do **not** introduce a parallel warnings model, renderer, or section. The Architect may relabel the existing heading from "Code Analysis Warnings" to a more general "Warnings" heading if appropriate.

## Non-Functional Requirements

- **NFR-1 — Backwards compatibility (Terraform 1.13 and earlier).** Plans that lack `action_invocations`, `deferred_action_invocations`, `resource_drift`, `relevant_attributes`, `applyable`, `complete`, `errored`, `variables[].deprecated`, `outputs[].deprecated`, and `outputs[].type` MUST parse and render byte-identically to today (verified by existing snapshot tests passing without modification, except where intentional global format changes such as a renamed warnings heading occur).
- **NFR-2 — Format-version stability.** No change to the supported `format_version` (`"1.2"`). The parser must not reject plans whose format_version differs only in patch level, and must not require a Terraform-version check before reading the new fields.
- **NFR-3 — Performance with large action sets.** Rendering plans with hundreds of `action_invocations[]` (e.g. modules that fan out lifecycle triggers across many resources) must not produce a noticeable slowdown relative to plans of comparable resource-change count. Concretely, plan-to-markdown rendering must remain O(N) in the total number of plan items (resources + actions + drift entries).
- **NFR-4 — Memory.** No materialisation of unbounded copies of the action list — actions are processed in the same streaming/iterative pattern used for resource changes.
- **NFR-5 — Graceful absence handling.** Every new field is optional in the parsed model; missing fields never cause parse failures, exceptions, or rendering errors.
- **NFR-6 — Test fixtures are minimal and hand-crafted.** Each new field has at least one focused JSON fixture under the existing `TestData` directory. No external Terraform binary is required to regenerate fixtures.
- **NFR-7 — Always-on, no opt-in.** No new CLI flags, environment variables, or configuration entries. The feature is observable purely by giving tfplan2md a plan that contains the relevant fields.

## Acceptance Criteria

The feature is complete when all of the following are demonstrably true:

- [ ] **AC-1.** Given a hand-crafted Terraform 1.14 plan JSON containing `action_invocations[]` with both lifecycle-triggered and invoke-triggered actions, the rendered markdown shows each action inline under its triggering resource (lifecycle), or in a defined fallback location (invoke), via the existing parent-child rendering pipeline.
- [ ] **AC-2.** Given a plan containing `deferred_action_invocations[]`, those actions render in the same inline location as non-deferred actions and are clearly marked as deferred.
- [ ] **AC-3.** A single generic action renderer is used; there are no provider-specific action renderers in this feature's source diff.
- [ ] **AC-4.** Sensitive (`config_sensitive`) and unknown (`config_unknown`) action config values are redacted / marked using the existing `SensitivityHelper`.
- [ ] **AC-5.** Given a plan containing a non-empty `resource_drift[]`, the rendered markdown contains a clearly titled "Drift detected" section listing every drifted resource. Given an empty or absent `resource_drift[]`, no such section is emitted.
- [ ] **AC-6.** Given a plan whose `applyable`, `complete`, or `errored` booleans signal a non-ordinary state, the report header contains a clearly visible status banner reflecting that state. Given an ordinary plan (or one without these fields), no misleading banner is rendered.
- [ ] **AC-7.** Given a plan containing `relevant_attributes[]`, the rendered markdown surfaces those attributes in a location reviewers can see.
- [ ] **AC-8.** Given a plan whose `configuration.root_module.variables[*]` or `configuration.root_module.outputs[*]` carry a `deprecated` string, each deprecated item that is referenced by the plan produces exactly one warning entry under the existing code-analysis warnings section (or its renamed equivalent), using `CodeAnalysisWarningModel` and `CodeAnalysisSectionRenderer`. No parallel warnings system is introduced.
- [ ] **AC-9.** Given a Terraform 1.13 plan JSON that lacks every field listed in this feature, the rendered markdown is identical to the output produced before this feature was implemented (verified by existing snapshot tests passing without diff, except for any intentional warnings-heading rename).
- [ ] **AC-10.** No new CLI flags, environment variables, or configuration options are introduced. `tfplan2md --help` output is unchanged with respect to feature toggles.
- [ ] **AC-11.** Hand-crafted fixtures exist under `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` exercising: lifecycle-triggered action, invoke-triggered action, deferred action, sensitive/unknown action config, `resource_drift`, `applyable`/`complete`/`errored` (each), `relevant_attributes`, deprecated variable, deprecated output. Each is covered by at least one snapshot or unit test.
- [ ] **AC-12.** Rendering performance for a plan with 200 resource changes and 200 action invocations stays within the same order of magnitude as a plan with 200 resource changes and zero actions (no quadratic blow-up).
- [ ] **AC-13.** `docs/features.md` lists this feature, and the feature folder contains the architect-, developer-, and reviewer-produced artifacts that follow the spec.

## Open Questions for the Architect

Most decisions are locked by the maintainer. The following layout choices remain genuinely open and are explicitly delegated to the Architect:

1. **H2 layout.** Choose the final placement of the plan status banner, the "Drift detected" section, and the rendering of `relevant_attributes[]` (top-level subsection vs. per-resource footnote vs. an extra column). The proposed default in this spec is a starting point, not a binding constraint.
2. **Warnings heading wording.** Decide whether to keep the heading "Code Analysis Warnings" (and accept that deprecation warnings appear there too) or rename it to a more general "Warnings" heading now that it carries entries from multiple sources. Either is acceptable as long as deprecations are emitted through the existing model and renderer.
3. **Fallback location for actions with no triggering resource section.** Decide where invoke-mode actions and lifecycle actions whose triggering resource is not in `resource_changes[]` render. (A small "Other actions" subsection at the end of the changes area is one reasonable option; another is to fold them into the plan-context section from H2. The Architect picks one.)

All other items raised in `analysis.md § 6` (action UX placement, generic vs provider-specific renderers, opt-in flags, scope of the first feature, Stacks, fixture sourcing) are already resolved by the maintainer's approved scope above.
