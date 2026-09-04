# Analysis: Terraform 1.14.x and 1.15.0 support in tfplan2md

> **Scope note:** This document is an *analysis + enhancement suggestions* artifact, **not** a Feature Specification. The maintainer must choose which suggestion(s) to scope before the Requirements Engineer produces `specification.md`. Per maintainer instruction, no `work-protocol.md`, ADR, or code has been created at this stage.

## 1. Background

`tfplan2md` consumes the JSON produced by `terraform show -json <planfile>`. The shape of that JSON is defined in HashiCorp's [`internal/command/jsonplan`](https://github.com/hashicorp/terraform/tree/v1.15/internal/command/jsonplan) package, in particular `plan.go`, `resource.go` and (newly) `action_invocations.go`. This analysis enumerates the changes between Terraform 1.13 → 1.14 → 1.15 that affect that JSON, compares them against what tfplan2md currently parses and renders, and proposes prioritized enhancements.

**Sources consulted:**
- `https://raw.githubusercontent.com/hashicorp/terraform/v1.15/CHANGELOG.md`
- `https://raw.githubusercontent.com/hashicorp/terraform/v1.14/CHANGELOG.md`
- `internal/command/jsonplan/plan.go`, `resource.go`, `action_invocations.go` on the `v1.13`, `v1.14`, `v1.15` branches (raw GitHub).

**Plan format version:** `format_version` is **`"1.2"`** in both 1.14 and 1.15 (unchanged from 1.13). New top-level fields are *additive*; older consumers keep working.

## 2. Summary of relevant changes

### 2.1 Terraform 1.14.x

#### 1.14.0 (2025‑11‑19) — biggest change for tfplan2md

**JSON plan additions** (from diffing `jsonplan/plan.go` v1.13 → v1.14):

| New top‑level field | Type | Source |
|---|---|---|
| `action_invocations` | `[]ActionInvocation` | new `action_invocations.go` |
| `deferred_action_invocations` | `[]DeferredActionInvocation` | same |

**`ActionInvocation` schema** (`action_invocations.go`):
```
{
  "address": "...", "type": "...", "name": "...",
  "config_values": {...}, "config_sensitive": {...}, "config_unknown": {...},
  "provider_name": "...",
  "lifecycle_action_trigger": {
    "triggering_resource_address": "...",
    "action_trigger_event": "before_create|after_create|before_update|after_update|...",
    "action_trigger_block_index": 0,
    "actions_list_index": 0
  },
  "invoke_action_trigger": {}    // mutually exclusive with lifecycle_action_trigger
}
```

**Conceptual change:** Actions are a **new top-level configuration block** (alongside `resource`, `data`, `module`). Providers can ship Actions like `aws_lambda_invoke` or `aws_cloudfront_create_invalidation`. Two trigger modes:
- **Lifecycle trigger** — the action is wired to a resource lifecycle event via the resource's `lifecycle { action_trigger { events = [...] actions = [action.<addr>] } }` block.
- **Invoke trigger** — the action is invoked via `terraform plan -invoke=action.<addr>`.

**Other 1.14 items:** `terraform query` / `*.tfquery.hcl`, `GenerateResourceConfiguration` RPC. These are CLI- and provider-side; they do **not** appear in `terraform show -json` of a normal `plan` file. Out of scope for tfplan2md.

#### 1.14.1 – 1.14.9 (patch releases)

Reviewed each CHANGELOG entry. Plan-JSON-relevant items:

- **1.14.4** — bug: actions in modules without instances failed plan graph. Confirms actions surface in `action_invocations` for sub-modules; we should test that case.
- **1.14.8** — bug: crash in display of `relevant_attributes` after provider upgrades. Reminder that `relevant_attributes` (top-level field, present since 1.x) is something tfplan2md does not yet render.
- All other patch entries (1.14.1, 1.14.2, 1.14.3, 1.14.5, 1.14.6, 1.14.7, 1.14.9) are bug fixes in `terraform test`, stacks, S3 backend, init, etc. — **no impact** on plan JSON consumed by tfplan2md.

### 2.2 Terraform 1.15.0 (2026‑04‑29)

`jsonplan/plan.go` is **unchanged structurally** between v1.14 and v1.15 (the only diff is the file copyright header). The new functionality is exposed via **`configuration` block content** rather than new top-level fields:

| Feature | Where it appears in plan JSON |
|---|---|
| `deprecated` attribute on `variable` blocks | `configuration.root_module.variables[<name>].deprecated` (string) |
| `deprecated` attribute on `output` blocks | `configuration.root_module.outputs[<name>].deprecated` (string) |
| Explicit `type` constraints on `output` blocks | `configuration.root_module.outputs[<name>].type` (cty type, JSON-encoded) |
| Variables/locals in `module source` and `version` | `configuration.root_module.module_calls[<name>].source` may now be a non-literal expression reference. Already JSON-encoded as expression. |
| `convert()` function | runtime only, not visible in plan JSON |
| `terraform validate` checks `backend` | not in plan JSON |
| Bug fix #37834: `terraform show -json` no longer crashes on ephemeral resources with pre/postconditions | enables more reliable plan JSON when ephemeral resources are in use |

The `EXPERIMENTS` section (alpha-only) mentions deferred actions and `terraform test cleanup`; these are **not in stable 1.15 plan JSON**.

## 3. Current tfplan2md support (gap analysis)

Source of truth scanned:
- `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`
- `src/Oocx.TfPlan2Md/Parsing/TerraformPlanParser.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/**` (grep for field names)

| Plan JSON element | First in TF version | Supported in tfplan2md? | Notes |
|---|---|---|---|
| `format_version`, `terraform_version` | 0.12 | ✅ | `TerraformPlan` record |
| `resource_changes[]` | 0.12 | ✅ | full pipeline |
| `resource_changes[].change.actions` (`no-op`,`create`,`read`,`update`,`delete`,`delete`+`create`,`create`+`delete`) | 0.12 | ✅ | `TerraformActions` |
| `resource_changes[].change.actions` (`open`) — ephemeral | 1.10 | ✅ partial | constant exists (`TerraformActions.Open`); rendering is minimal — see snapshot tests. |
| `resource_changes[].previous_address` (moved blocks) | 1.5 | ✅ | feature 038 |
| `resource_changes[].change.importing.id` (import blocks) | 1.5 | ✅ | feature 038 |
| `resource_changes[].action_reason` | 1.2+ | ✅ | feature 011 |
| `resource_changes[].change.replace_paths` | 1.2 | ✅ | feature 011 |
| `resource_changes[].deposed` | 0.12 | ❌ | not parsed; would only matter for plans that touch deposed objects (rare in modern usage) |
| `resource_changes[].index_unknown` | 1.8 | ❌ | not parsed; appears when count/for_each is unknown |
| `resource_drift[]` | 1.0 | ❌ | top-level field never parsed; user has no visibility into drift detected during refresh |
| `relevant_attributes[]` | 1.2 | ❌ | top-level field never parsed; would explain why a resource was re-planned |
| `deferred_changes[]` | 1.9 (alpha) / 1.13 stable | ❌ | not parsed |
| `output_changes` | 0.13 | ✅ | feature 097 |
| `output_changes[<n>].after_unknown` (object form) | 1.x | ✅ | feature 102 |
| `checks[]` | 1.5 | ❌ | not parsed |
| `applyable`, `complete`, `errored` (top‑level booleans) | 1.6 | ❌ | not parsed; tool cannot mark a report as "no changes apply" or "errored plan" |
| `prior_state.values.root_module.resources[].identity` / `identity_schema_version` | 1.12 | ❌ | not rendered; matters for `import` planning previews |
| `configuration.root_module.variables[].deprecated` | **1.15** | ❌ | not parsed; could surface deprecation warnings in report |
| `configuration.root_module.outputs[].deprecated` | **1.15** | ❌ | not parsed |
| `configuration.root_module.outputs[].type` | **1.15** | ❌ | not parsed |
| **`action_invocations[]`** | **1.14** | ❌ | **not parsed** — entire new top-level concept invisible to the report |
| **`deferred_action_invocations[]`** | **1.14** | ❌ | not parsed |

> Verification: grep for `resource_drift`, `relevant_attributes`, `action_invocations`, `applyable`, `complete`, `errored`, `index_unknown`, `deferred`, `identity_schema_version` in `src/Oocx.TfPlan2Md/Parsing/` returns no results.

## 4. Suggested enhancements (prioritized)

Each suggestion has a *title*, *rationale*, *scope sketch*, *recommended granularity*, and an estimated effort (T-shirt: S/M/L).

### 🔴 High priority

#### H1. Render `action_invocations` from Terraform 1.14+
- **Rationale:** Actions are the headline 1.14 feature. With Terraform 1.14 in production, plans for any user adopting `aws_lambda_invoke`, `aws_cloudfront_create_invalidation`, etc. silently omit a whole class of changes from tfplan2md reports. Reviewers can't see what side-effects a plan will trigger.
- **Scope sketch:**
  - Extend `TerraformPlan` with `IReadOnlyList<ActionInvocation>? ActionInvocations`.
  - Model `ActionInvocation` (Address, Type, Name, ProviderName, ConfigValues/Sensitive/Unknown, LifecycleActionTrigger, InvokeActionTrigger).
  - New ReportModel section "Actions" with two subsections: lifecycle-triggered (grouped by triggering resource) and invoke-triggered.
  - Default template renders address, action type, trigger event, config (sensitive-aware), and provider.
  - Optional: link lifecycle-triggered actions inline under the resource that triggers them in the parent-child grouping pipeline.
  - Ephemeral / sensitive handling reuses existing `SensitivityHelper` and unknown-value rendering.
  - Tests: snapshot-style fixtures with both invoke and lifecycle examples.
- **Granularity:** Own feature work-item — non-trivial model, rendering, and template work; user-visible.
- **Effort:** **L**.

#### H2. Surface `resource_drift` and top-level status (`applyable`/`complete`/`errored`)
- **Rationale:** When a plan is run against drifted state, Terraform places drift detection results in `resource_drift` (separate from `resource_changes`). Today these are invisible in tfplan2md output, so reviewers may miss "this plan exists because reality drifted" cases. The status booleans let the report header truthfully say "Plan has no applyable changes" or "Plan errored" instead of looking like a normal change set.
- **Scope sketch:**
  - Parse `resource_drift[]` (same `ResourceChange` shape) and `applyable`, `complete`, `errored`.
  - Add a "Drift detected" section, rendered like a slimmed-down resource changes table.
  - Add a status banner / callout near the report header.
  - No template-author breaking changes (purely additive sections).
- **Granularity:** Own feature work-item.
- **Effort:** **M**.

### 🟡 Medium priority

#### M1. Parse and render `relevant_attributes`
- **Rationale:** Helps reviewers understand *why* a resource shows up in the plan (which upstream attribute changed). Particularly valuable for large plans with cascading updates.
- **Scope sketch:** Add `RelevantAttribute { resource, attribute (path) }`. Render either as a "Why" column in the resource table or as a footnote per resource.
- **Granularity:** Own small feature, or fold into H2 if scope is kept tight.
- **Effort:** **S/M**.

#### M2. Surface `variable.deprecated` and `output.deprecated` (1.15)
- **Rationale:** Terraform 1.15 introduces a first-class deprecation mechanism. tfplan2md is well-placed to call out deprecated outputs in the Outputs section and deprecated variables in a dedicated callout. Helps teams plan migrations.
- **Scope sketch:**
  - Extend configuration parsing to read `configuration.root_module.{variables,outputs}[<n>].deprecated`.
  - In Outputs section (feature 097/102 territory), badge deprecated outputs with a ⚠️ icon and the deprecation message.
  - Optional: a small "Deprecations" section if any deprecated outputs/variables are referenced by the plan.
- **Granularity:** Own small feature.
- **Effort:** **S**.

#### M3. Render `checks[]` (precondition / postcondition / check block results)
- **Rationale:** Pre/postconditions and `check` blocks are commonly used for guardrails; their failures appear in plan JSON's `checks` field. Today users can't see them in the markdown report.
- **Scope sketch:** Parse `checks[]` (object structure, see `jsonchecks` package) and render a "Check results" section grouping by status (pass / fail / unknown / error).
- **Granularity:** Own feature.
- **Effort:** **M**.

### 🟢 Low priority / opportunistic

#### L1. Handle `index_unknown` on resource changes
- **Rationale:** Edge-case display improvement: when `count`/`for_each` is unknown, the address shown today (`module.x.aws_foo.bar[?]`) can be confusing. We could display it as `[unknown]` and add a note.
- **Effort:** **S**.

#### L2. Handle `deposed` resource changes
- **Rationale:** Rare in modern usage; affects display of partial-failure recovery plans.
- **Effort:** **S**.

#### L3. Render resource `identity` / `identity_schema_version` (1.12+) for `import` previews
- **Rationale:** Improves clarity of `import` block plans. Lower priority because feature 038 already surfaces basic import info via `change.importing.id`.
- **Effort:** **S/M**.

#### L4. `deferred_changes[]` and `deferred_action_invocations[]`
- **Rationale:** Stable in 1.13+/1.14 but mostly relevant when users opt into experimental `-allow-deferral`. Limited audience today.
- **Effort:** **M**.

#### L5. Stacks JSON
- **Rationale:** Stacks (`tfstacks`) have their own JSON output, distinct from `terraform show -json`. Not a `tfplan2md` audience today.
- **Recommendation:** **Out of scope.** Document explicitly in a future ADR if/when demand appears.

#### L6. Tests/parsing for ephemeral pre/postcondition crash fix (1.15.0 #37834)
- **Rationale:** Defensive: add a fixture with ephemeral resource + condition to confirm tfplan2md handles the JSON Terraform 1.15 now produces correctly. Probably already works; just needs a test.
- **Effort:** **S**.

## 5. Recommended first step

**Do H1 first** ("Render `action_invocations`"). Reasoning:

1. **Highest user impact.** It is the only change in the 1.14/1.15 window where a *current, real* plan will silently lose information when fed into tfplan2md. The other gaps (drift, relevant_attributes, checks) have existed for several Terraform releases without complaint, so they are known-and-tolerated; actions are brand new and surprising.
2. **Forcing function for model patterns.** Implementing actions cleanly will require deciding how non-resource items appear in the report (sectioning, sensitivity handling, parent/child linkage). Those patterns are reusable for H2/M1/M3.
3. **Independently shippable** with no breaking changes to existing templates.

**Suggested second step: H2 + M1 bundled.** Drift detection + status banner + relevant_attributes are conceptually one "plan-context awareness" upgrade and share parsing/rendering plumbing.

**Defer to a third feature:** M2 (deprecations) — small, clean, can wait until at least one user adopts 1.15's `deprecated`.

## 6. Open questions for the maintainer

1. **Action UX placement.** Should lifecycle-triggered actions be rendered (a) as their own top-level section, (b) inline under the triggering resource (parent/child style), or (c) both with a setting? The parent-child registry already exists (features 045/046) and could host them.
2. **Provider-specific action templates.** Should we plan for provider-specific action renderers (analogous to existing AzureRM/AzApi resource renderers) from day one, or ship a generic renderer first and add provider plugins later? Ecosystem today is mostly AWS — we may not need an Azure-specific path immediately.
3. **Template back-compat.** New report sections (Actions, Drift, Status) — should they be opt-in via CLI flag for one release to give template authors time to react, or always-on?
4. **Scope of the first feature.** Confirm: implement H1 only, or H1 + H2 in one feature? My recommendation is H1-only to keep the change reviewable, but they are independent enough to combine if you prefer one release.
5. **Stacks.** Confirm Stacks JSON is out of scope (suggestion L5). If not, a separate analysis is warranted.
6. **Test fixtures.** We currently lack a real Terraform 1.14 plan with `action_invocations`. Acceptable to hand-craft a JSON fixture, or should we generate one against a provider that ships Actions (e.g., AWS provider ≥ Actions support)?

---

## Appendix A — JSON field reference (1.14/1.15 deltas)

### Top-level Plan fields added since 1.13

```jsonc
{
  // ... existing fields ...
  "action_invocations": [ /* ActionInvocation, see schema in §2.1 */ ],          // 1.14
  "deferred_action_invocations": [ /* DeferredActionInvocation */ ]              // 1.14
}
```

### Configuration fields added in 1.15

```jsonc
"configuration": {
  "root_module": {
    "variables": {
      "<name>": { "default": ..., "type": ..., "deprecated": "message" }        // 1.15
    },
    "outputs": {
      "<name>": { "expression": ..., "type": ..., "deprecated": "message" }     // 1.15 (deprecated, type)
    }
  }
}
```

### Verified-unchanged fields

`format_version` remains `"1.2"` in both 1.14 and 1.15. No breaking changes for existing tfplan2md parsing.
