# Test Plan: Inline Relevant Attributes (Feature 660)

## Overview

This plan covers testing for the redesign of `relevant_attributes[]` rendering: replacing the flat `## Relevant Attributes` H2 table with per-resource inline annotations (forced-replacement callouts and depends-on lines) and a collapsible fallback section for uncorrelated attributes.

Reference: `docs/features/660-inline-relevant-attributes/specification.md`  
Architecture: `docs/features/660-inline-relevant-attributes/architecture.md`

---

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---|---|---|
| SC-1: Forced-replacement callout rendered when replace_paths trace to upstream relevant attribute | TC-01, TC-30, TC-31, TC-32 | Unit + Snapshot |
| SC-2: Callout names local attribute, upstream resource, and upstream attribute path | TC-01, TC-02, TC-30 | Unit + Snapshot |
| SC-3: "changing in this plan" bold phrase when upstream is also replaced/destroyed | TC-03, TC-04, TC-31 | Unit + Snapshot |
| SC-3 (negative): No phrase when upstream is NOT replaced/destroyed | TC-05, TC-32 | Unit + Snapshot |
| SC-4: 🔗 Depends on lists ALL correlated attributes for replaced/destroyed resources | TC-06, TC-07, TC-33, TC-34 | Unit + Snapshot |
| SC-4: Also depends on label when forced-replacement line also present | TC-08, TC-34 | Unit + Snapshot |
| SC-5: In-place update resources receive no callout lines | TC-09, TC-37 | Unit + Snapshot |
| SC-6: Drift-section resources receive no callout lines | TC-10, TC-38 | Unit + Snapshot |
| SC-7: Uncorrelated attributes appear in collapsible fallback section | TC-11, TC-35 | Unit + Snapshot |
| SC-8: Fallback section omitted when all attributes were correlated | TC-12, TC-36 | Unit + Snapshot |
| SC-9: ## Relevant Attributes H2 table removed | TC-13, TC-30–TC-38 | Unit + Snapshot |
| SC-10: Plans without relevant_attributes produce identical output | TC-14, TC-39 | Unit + Snapshot |
| Reference normalization (data source vs managed resource) | TC-15, TC-16 | Unit |
| Case-insensitive correlation matching | TC-17 | Unit |
| Multiple forced-replacement paths on one resource | TC-18 | Unit |
| Delete ("delete") action treated same as replace | TC-19 | Unit + Snapshot |
| Existing snapshot updates: relevant-attributes-present.md | EX-01 | Snapshot |

---

## Test Cases

### TC-01: Single forced-replacement annotation populated on replace resource

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests` (new file, extends pattern from `ReportModelBuilderActionsTests`)

**Description:**  
When a resource is being replaced, has a `replace_paths` entry whose top-level attribute matches a `ConfigurationReferences` key that references an upstream resource, and that upstream resource appears in `relevant_attributes`, then `ForcedReplacementAnnotations` is populated on that resource.

**Preconditions:**
- Resource `azurerm_virtual_machine.web` with action `replace`
- `ReplacePaths`: `[["network_interface_ids", 0]]`
- `ConfigurationReferences["network_interface_ids"]` = `["azurerm_network_interface.web.id", "azurerm_network_interface.web"]`
- `relevant_attributes`: `[{resource: "azurerm_network_interface.web", attribute: ["id"]}]`
- `azurerm_network_interface.web` is NOT in the plan (not changing in this plan)

**Test Steps:**
1. Construct `TerraformPlan` with the above parameters
2. Call `new ReportModelBuilder().Build(plan)`
3. Inspect `model.Changes[0].ForcedReplacementAnnotations`

**Expected Result:**
- `ForcedReplacementAnnotations.Count` == 1
- `ForcedReplacementAnnotations[0].LocalAttribute` == `"network_interface_ids"`
- `ForcedReplacementAnnotations[0].UpstreamResource` == `"azurerm_network_interface.web"`
- `ForcedReplacementAnnotations[0].UpstreamAttributePath` == `"id"`
- `ForcedReplacementAnnotations[0].IsChangingInThisPlan` == `false`

**Test Data:** Inline construction (no JSON fixture needed)

---

### TC-02: Forced-replacement annotation names all three fields correctly

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Verifies the annotation carries the correct local attribute name (top-level replace_path attribute), upstream resource address, and upstream attribute path.

**Preconditions:**
- Resource `azurerm_app_service.api` with action `replace`
- `ReplacePaths`: `[["app_settings"]]`
- `ConfigurationReferences["app_settings"]` = `["azurerm_key_vault.main.vault_uri"]`
- `relevant_attributes`: `[{resource: "azurerm_key_vault.main", attribute: ["vault_uri"]}]`

**Expected Result:**
- `ForcedReplacementAnnotations[0].LocalAttribute` == `"app_settings"`
- `ForcedReplacementAnnotations[0].UpstreamResource` == `"azurerm_key_vault.main"`
- `ForcedReplacementAnnotations[0].UpstreamAttributePath` == `"vault_uri"`

---

### TC-03: IsChangingInThisPlan is true when upstream is also being replaced

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
When the upstream resource referenced in a relevant attribute is itself being replaced in the same plan, `IsChangingInThisPlan` must be `true`.

**Preconditions:**
- Resource `azurerm_virtual_machine.web` with action `replace` referencing `azurerm_network_interface.web`
- Resource `azurerm_network_interface.web` with action `replace` also present in `resource_changes`
- `relevant_attributes`: `[{resource: "azurerm_network_interface.web", attribute: ["id"]}]`

**Expected Result:**
- `ForcedReplacementAnnotations[0].IsChangingInThisPlan` == `true`

---

### TC-04: IsChangingInThisPlan is true when upstream is being deleted

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
`delete` action on upstream also triggers `IsChangingInThisPlan = true`.

**Preconditions:**
- Upstream resource has action `delete`

**Expected Result:**
- `IsChangingInThisPlan` == `true`

---

### TC-05: IsChangingInThisPlan is false when upstream resource is in-place update

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
An `update` action on the upstream resource does NOT set `IsChangingInThisPlan` — the flag is only set for replace/delete.

**Preconditions:**
- Same as TC-03 except `azurerm_network_interface.web` has action `update`

**Expected Result:**
- `ForcedReplacementAnnotations[0].IsChangingInThisPlan` == `false`

---

### TC-06: DependsOnAnnotations populated for correlated attributes not in replace_paths

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Relevant attributes correlated to a replaced resource via `ConfigurationReferences` but NOT tracing to a `replace_paths` entry go into `DependsOnAnnotations` instead of `ForcedReplacementAnnotations`.

**Preconditions:**
- Resource `azurerm_app_service.api` with action `replace`
- `ReplacePaths`: `[["app_settings"]]`
- `ConfigurationReferences`:
  - `"app_settings"` → `["azurerm_key_vault.main.vault_uri"]`
  - `"identity"` → `["data.azurerm_client_config.current.tenant_id"]`
- `relevant_attributes`:
  - `{resource: "azurerm_key_vault.main", attribute: ["vault_uri"]}` (forced)
  - `{resource: "data.azurerm_client_config.current", attribute: ["tenant_id"]}` (depends-on)

**Expected Result:**
- `ForcedReplacementAnnotations.Count` == 1 (key_vault.main)
- `DependsOnAnnotations.Count` == 1 (client_config.current)
- `DependsOnAnnotations[0].UpstreamResource` == `"data.azurerm_client_config.current"`
- `DependsOnAnnotations[0].UpstreamAttributePath` == `"tenant_id"`

---

### TC-07: All correlated entries returned as empty uncorrelated list

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
When all `relevant_attributes` entries are correlated to changed resources, the method returns an empty uncorrelated list and `model.RelevantAttributes` is empty.

**Preconditions:**
- Both relevant attributes are correlated (as in TC-06 above)

**Expected Result:**
- `model.RelevantAttributes` is empty (uncorrelated list is empty)
- `model.Changes[0].ForcedReplacementAnnotations.Count` + `model.Changes[0].DependsOnAnnotations.Count` == 2

---

### TC-08: DependsOn label becomes "Also depends on" when forced-replacement line is also present

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
When a resource has both `ForcedReplacementAnnotations` and `DependsOnAnnotations`, the combined state is present on the model, enabling the renderer to use "Also depends on" label. (The label selection is a renderer concern, but the model must carry both lists.)

**Preconditions:** Same as TC-06

**Expected Result:**
- `change.ForcedReplacementAnnotations.Count > 0` AND `change.DependsOnAnnotations.Count > 0` simultaneously

---

### TC-09: In-place update resource receives no annotations

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
A resource with action `update` (not replace/delete) must have empty `ForcedReplacementAnnotations` and `DependsOnAnnotations`, even if its `ConfigurationReferences` reference upstream resources listed in `relevant_attributes`.

**Preconditions:**
- Resource `azurerm_app_service.api` with action `update`
- `ConfigurationReferences["identity"]` = `["data.azurerm_client_config.current.tenant_id"]`
- `relevant_attributes`: `[{resource: "data.azurerm_client_config.current", attribute: ["tenant_id"]}]`

**Expected Result:**
- `ForcedReplacementAnnotations.Count` == 0
- `DependsOnAnnotations.Count` == 0
- `model.RelevantAttributes.Count` == 1 (the attribute stays in the uncorrelated fallback list)

---

### TC-10: Drift resources receive no annotations

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Drift resources (from `resource_drift`) should not receive inline annotations, because `BuildInlineRelevantAttributeAnnotations` operates on planned changes only.

**Preconditions:**
- A drift resource (in `resource_drift`, not `resource_changes`) with action `update` referencing an upstream relevant attribute
- The upstream appears in `relevant_attributes`

**Expected Result:**
- `model.Drift[0].ForcedReplacementAnnotations.Count` == 0
- `model.Drift[0].DependsOnAnnotations.Count` == 0
- `model.RelevantAttributes` still contains the attribute (in fallback)

---

### TC-11: Uncorrelated attributes appear in model.RelevantAttributes (fallback list)

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Relevant attributes that do not correlate to any changed resource remain in `model.RelevantAttributes` for the fallback section renderer.

**Preconditions:**
- Resource `example_resource.a` with action `update` (no ConfigurationReferences)
- `relevant_attributes`: two entries with unrelated upstream resources

**Expected Result:**
- `model.RelevantAttributes.Count` == 2 (both uncorrelated, kept for fallback)

---

### TC-12: Fallback list empty when all attributes correlated

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Mirrors TC-07 from the model perspective — directly asserts the uncorrelated list returned by `BuildInlineRelevantAttributeAnnotations` is empty.

**Preconditions:** All relevant attributes are matched via ConfigurationReferences on a replace resource

**Expected Result:**
- `model.RelevantAttributes.Count` == 0

---

### TC-13: model.RelevantAttributes now semantically means uncorrelated attributes

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Ensures `model.RelevantAttributes` is the list passed to the fallback section renderer (only uncorrelated entries), not the full list. This documents the semantic change from feature 122.

**Preconditions:**
- 3 relevant attributes: 2 correlated to a replace resource, 1 not correlated

**Expected Result:**
- `model.RelevantAttributes.Count` == 1

---

### TC-14: Empty relevant_attributes list → no annotations, no fallback

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
When `relevant_attributes` is absent or empty, no annotations are set and the uncorrelated list is empty — preserving existing behaviour for pre-1.14 plans.

**Preconditions:**
- Plan with no `relevant_attributes` field
- Resource with `replace` action

**Expected Result:**
- `ForcedReplacementAnnotations.Count` == 0
- `DependsOnAnnotations.Count` == 0
- `model.RelevantAttributes.Count` == 0

---

### TC-15: Reference with .attribute suffix normalizes to resource address (managed resource)

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
`ConfigurationReferences` values like `"azurerm_network_interface.web.id"` must be normalized to `"azurerm_network_interface.web"` to match against `relevant_attributes[].resource`.

**Preconditions:**
- `ConfigurationReferences["network_interface_ids"]` = `["azurerm_network_interface.web.id"]`
- `relevant_attributes`: `[{resource: "azurerm_network_interface.web", attribute: ["id"]}]`
- Replace resource

**Expected Result:**
- Correlation succeeds: `ForcedReplacementAnnotations.Count` == 1

---

### TC-16: Data source reference normalizes correctly (3-segment address)

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Data source references like `"data.azurerm_client_config.current.tenant_id"` must normalize to `"data.azurerm_client_config.current"`.

**Preconditions:**
- `ConfigurationReferences["identity"]` = `["data.azurerm_client_config.current.tenant_id"]`
- `relevant_attributes`: `[{resource: "data.azurerm_client_config.current", attribute: ["tenant_id"]}]`
- Replace resource

**Expected Result:**
- Correlation succeeds: `DependsOnAnnotations.Count` == 1

---

### TC-17: Case-insensitive resource address matching

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
Correlation must be case-insensitive; `"AzureRM_Network_Interface.Web"` in ConfigurationReferences should match `"azurerm_network_interface.web"` in relevant_attributes.

**Preconditions:**
- `ConfigurationReferences` contains an upper-cased reference string
- `relevant_attributes` contains the same address in lower case

**Expected Result:**
- Correlation succeeds

---

### TC-18: Multiple replace_paths produces multiple ForcedReplacementAnnotations

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
When a resource has two replace_paths entries that both correlate to upstream relevant attributes, two `ForcedReplacementAnnotation` records are produced.

**Preconditions:**
- Resource with two `replace_paths` entries: `["network_interface_ids", 0]` and `["os_disk"]`
- Both top-level attributes have `ConfigurationReferences` pointing to different upstream resources
- Both upstream resources appear in `relevant_attributes`

**Expected Result:**
- `ForcedReplacementAnnotations.Count` == 2

---

### TC-19: Delete resource treated identically to replace for annotations

**Type:** Unit  
**Class:** `ReportModelBuilderInlineRelevantAttributeTests`

**Description:**  
A resource with action `delete` must receive the same inline annotation treatment as `replace`.

**Preconditions:**
- Resource with action `delete` and `ConfigurationReferences` referencing an upstream in `relevant_attributes`

**Expected Result:**
- `DependsOnAnnotations.Count` == 1 (or `ForcedReplacementAnnotations` if replace_paths also present)

---

## Snapshot Tests

All snapshot tests follow the pattern established in `Terraform114SnapshotTests`:
- Plan JSON fixtures in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/tf114/`
- Expected markdown baselines in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`
- Test class: `InlineRelevantAttributeSnapshotTests` (new file in `MarkdownGeneration/`)

### TC-30: Forced-replacement card, upstream changing in this plan

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-forced-replacement-upstream-changing-plan.json`  
**Snapshot:** `relevant-attrs-forced-replacement-upstream-changing.md`

**Scenario:**  
`azurerm_network_interface.web` is being replaced AND `azurerm_virtual_machine.web` is also being replaced. NIC ID is in `relevant_attributes`, and the VM's `network_interface_ids` is in `replace_paths`.

**Fixture Requirements:**
```json
{
  "format_version": "1.2",
  "terraform_version": "1.14.0",
  "resource_changes": [
    { "address": "azurerm_network_interface.web", "mode": "managed", "type": "azurerm_network_interface", "name": "web",
      "change": { "actions": ["create", "delete"], "replace_paths": [["location"]], ... } },
    { "address": "azurerm_virtual_machine.web", "mode": "managed", "type": "azurerm_virtual_machine", "name": "web",
      "change": { "actions": ["create", "delete"], "replace_paths": [["network_interface_ids", 0]], ... } }
  ],
  "relevant_attributes": [
    { "resource": "azurerm_network_interface.web", "attribute": ["id"] }
  ],
  "configuration": {
    "root_module": {
      "resources": [
        {
          "address": "azurerm_virtual_machine.web",
          "expressions": {
            "network_interface_ids": { "references": ["azurerm_network_interface.web.id", "azurerm_network_interface.web"] }
          }
        }
      ]
    }
  }
}
```

**Expected markdown in `azurerm_virtual_machine.web` card:**
```
> ⚠️ **Forced replacement** — `network_interface_ids` reads `azurerm_network_interface.web.id`, which is **changing in this plan**.
```

**Verifications:**
- `⚠️ **Forced replacement**` callout is present in the VM card
- Phrase "**changing in this plan**" is bold
- No `## Relevant Attributes` H2 table present anywhere
- `azurerm_network_interface.web` card has no annotation (upstream, not the dependant)
- No fallback `<details>` section (all attributes correlated)

---

### TC-31: Forced-replacement card, upstream is changing in this plan (⚠️ threshold: replace or delete only)

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-forced-replacement-upstream-changing-plan.json` (reuses TC-30 fixture)  
**Snapshot:** `relevant-attrs-forced-replacement-upstream-changing.md`

**Coverage:** Same as TC-30 — included to specifically confirm the `⚠️ threshold` logic. A separate snapshot is not required; TC-30 covers it.

---

### TC-32: Forced-replacement card, upstream NOT changing in this plan

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-forced-replacement-upstream-static-plan.json`  
**Snapshot:** `relevant-attrs-forced-replacement-upstream-static.md`

**Scenario:**  
`azurerm_network_interface.web` is NOT in the plan (the NIC already exists; its ID is a relevant attribute). `azurerm_virtual_machine.web` is being replaced due to `network_interface_ids` change.

**Fixture Requirements:**  
Same as TC-30 fixture but WITHOUT the `azurerm_network_interface.web` resource change entry.

**Expected markdown in VM card:**
```
> ⚠️ **Forced replacement** — `network_interface_ids` reads `azurerm_network_interface.web.id`.
```
(note: no "changing in this plan" phrase)

**Verifications:**
- "changing in this plan" phrase does NOT appear
- Callout still present with local attribute and upstream names

---

### TC-33: Depends-on line only (no forced-replacement path)

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-depends-on-only-plan.json`  
**Snapshot:** `relevant-attrs-depends-on-only.md`

**Scenario:**  
A resource is being replaced, and its `ConfigurationReferences` reference upstream resources listed in `relevant_attributes`, but no `replace_paths` entry traces to those upstream resources. Only the `🔗 Depends on:` line is rendered.

**Fixture Requirements:**
- Resource `azurerm_app_service.api` with action `replace`
- `replace_paths`: `[["site_config"]]` (traces to a non-upstream attribute)
- `ConfigurationReferences`:
  - `"site_config"` → `["some_other.resource"]` (no relevant attribute match)
  - `"identity"` → `["data.azurerm_client_config.current.tenant_id"]`
- `relevant_attributes`: `[{resource: "data.azurerm_client_config.current", attribute: ["tenant_id"]}]`

**Expected markdown in app service card:**
```
> 🔗 **Depends on:** `data.azurerm_client_config.current.tenant_id`
```

**Verifications:**
- No `⚠️ **Forced replacement**` line
- `🔗 **Depends on:**` line present (not "Also depends on")
- No `## Relevant Attributes` H2 table
- No fallback section (all attributes correlated)

---

### TC-34: Combined card (⚠️ forced replacement + 🔗 also depends on)

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-combined-card-plan.json`  
**Snapshot:** `relevant-attrs-combined-card.md`

**Scenario:**  
A replaced resource has both a forced-replacement path tracing to upstream and additional upstream dependencies. Both lines appear.

**Fixture Requirements:**
- Resource `azurerm_app_service.api` with action `replace`
- `replace_paths`: `[["app_settings"]]`
- `ConfigurationReferences`:
  - `"app_settings"` → `["azurerm_key_vault.main.vault_uri"]` (forced replacement)
  - `"identity"` → `["data.azurerm_client_config.current.tenant_id"]` (depends-on only)
- `relevant_attributes`:
  - `{resource: "azurerm_key_vault.main", attribute: ["vault_uri"]}`
  - `{resource: "data.azurerm_client_config.current", attribute: ["tenant_id"]}`
- `azurerm_key_vault.main` is also being replaced in the plan (`IsChangingInThisPlan = true`)

**Expected markdown in app service card:**
```
> ⚠️ **Forced replacement** — `app_settings` reads `azurerm_key_vault.main.vault_uri`, which is **changing in this plan**.
> 🔗 **Also depends on:** `data.azurerm_client_config.current.tenant_id`
```

**Verifications:**
- Both lines present
- Label is "**Also depends on:**" (not just "Depends on:")
- Both lines appear above the diff table
- No `## Relevant Attributes` H2 table
- No fallback section (all attributes correlated)

---

### TC-35: Fallback section rendered for uncorrelated attributes

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-fallback-only-plan.json`  
**Snapshot:** `relevant-attrs-fallback-only.md`

**Scenario:**  
Relevant attributes exist but cannot be correlated to any changed resource (either no resource changes reference those upstreams, or all changes are in-place updates). The fallback `<details>` section is rendered.

**Fixture Requirements:**
- Resource `example_resource.a` with action `update`
- No `replace_paths`, no matching `ConfigurationReferences`
- `relevant_attributes`:
  - `{resource: "example_resource.upstream1", attribute: ["id"]}`
  - `{resource: "example_resource.upstream2", attribute: ["settings", "endpoint"]}`

**Expected markdown (end of report):**
```markdown
<details>
<summary>🔗 Other plan inputs (2) — read by this plan but not tied to a specific change</summary>

> These existing values were read to compute the plan. If they change before apply, the plan may be stale.

- `example_resource.upstream1.id`
- `example_resource.upstream2.settings.endpoint`

</details>
```

**Verifications:**
- `<details>` fallback section present at end of report
- Count in summary is `(2)`
- Both uncorrelated attributes listed as `resource.attribute_path`
- No inline annotations on the resource card
- No `## Relevant Attributes` H2 table

---

### TC-36: Fallback section omitted when all attributes correlated

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-all-correlated-plan.json`  
**Snapshot:** `relevant-attrs-all-correlated.md`

**Scenario:**  
All relevant attributes are correlated to changed resources. The fallback section is completely absent from the output.

**Fixture Requirements:** Same as TC-30 fixture (all attributes correlated via the NIC→VM dependency chain)

**Verifications:**
- No `🔗 Other plan inputs` `<details>` section in the output
- No `## Relevant Attributes` H2 table

---

### TC-37: In-place update resource receives no annotations

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attributes-present-plan.json` (EXISTING fixture — reused)  
**Snapshot:** `relevant-attributes-present.md` (EXISTING — must be updated)

**Scenario:**  
The existing `relevant-attributes-present-plan.json` fixture contains one `update` resource and two uncorrelated `relevant_attributes`. After the feature:
- No inline annotations on the update resource card
- The `## Relevant Attributes` H2 table is REMOVED
- A `<details>` fallback section appears at the end with both attributes

**This is an existing snapshot that REQUIRES UPDATE.** The current snapshot shows the H2 table. The updated snapshot must show the `<details>` fallback section instead.

**Expected change to snapshot:**
- Remove: `## Relevant Attributes` heading and table
- Add: `<details>` fallback section with the two uncorrelated attributes

---

### TC-38: Drift resources receive no inline annotations

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attrs-drift-with-relevant-attrs-plan.json` (new fixture)  
**Snapshot:** `relevant-attrs-drift-with-relevant-attrs.md`

**Scenario:**  
A plan contains drift entries AND relevant attributes. The drift resource's card has no inline annotations; the relevant attributes appear only in the fallback section.

**Fixture Requirements:**
- `resource_drift`: one entry with `update` action referencing an upstream resource
- `resource_changes`: one ordinary `update` change (no correlation)
- `relevant_attributes`: one entry for the same upstream referenced in drift
- Configuration referencing the upstream in drift resource expressions

**Verifications:**
- Drift section resource card has no `⚠️` or `🔗` annotation lines
- Fallback section appears with the uncorrelated attribute (since drift resources don't trigger correlation)

---

### TC-39: Plan without relevant_attributes produces identical output (regression)

**Type:** Snapshot  
**Fixture:** `tf114/relevant-attributes-absent-plan.json` (EXISTING — unchanged)  
**Snapshot:** `relevant-attributes-absent.md` (EXISTING — must NOT change)

**Scenario:**  
A plan without `relevant_attributes[]` continues to produce identical output. This is the primary regression guard for pre-1.14 plans.

**Verifications:**
- Snapshot is byte-identical to the current approved baseline
- No extra lines, sections, or whitespace introduced

---

## Test Data Requirements

### New Fixtures to Create

| File | Location | Purpose |
|---|---|---|
| `relevant-attrs-forced-replacement-upstream-changing-plan.json` | `TestData/tf114/` | TC-30: NIC + VM both replaced |
| `relevant-attrs-forced-replacement-upstream-static-plan.json` | `TestData/tf114/` | TC-32: VM replaced, NIC not in plan |
| `relevant-attrs-depends-on-only-plan.json` | `TestData/tf114/` | TC-33: replace with depends-on only |
| `relevant-attrs-combined-card-plan.json` | `TestData/tf114/` | TC-34: forced + also depends on |
| `relevant-attrs-fallback-only-plan.json` | `TestData/tf114/` | TC-35: fallback section only |
| `relevant-attrs-all-correlated-plan.json` | `TestData/tf114/` | TC-36: no fallback section |
| `relevant-attrs-drift-with-relevant-attrs-plan.json` | `TestData/tf114/` | TC-38: drift + relevant attrs |

All fixtures must include a `configuration` block with `root_module.resources[].expressions` so that `ConfigurationReferenceResolver.BuildReferenceIndex` can populate `ConfigurationReferences` on the change models.

### New Snapshots to Create (Golden Files)

| File | Location | Created by |
|---|---|---|
| `relevant-attrs-forced-replacement-upstream-changing.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |
| `relevant-attrs-forced-replacement-upstream-static.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |
| `relevant-attrs-depends-on-only.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |
| `relevant-attrs-combined-card.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |
| `relevant-attrs-fallback-only.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |
| `relevant-attrs-all-correlated.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |
| `relevant-attrs-drift-with-relevant-attrs.md` | `TestData/Snapshots/` | Auto-generated on first run, then committed |

### Existing Snapshots to Update

| File | Location | Change |
|---|---|---|
| `relevant-attributes-present.md` | `TestData/Snapshots/` | Remove `## Relevant Attributes` H2 table; add `<details>` fallback section |

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|---|---|---|
| `relevant_attributes` absent | No annotations, no fallback, identical output | TC-14, TC-39 |
| `relevant_attributes` empty `[]` | Same as absent | TC-14 |
| In-place update with relevant attributes | No annotations; attributes go to fallback | TC-09, TC-37 |
| Drift resource references upstream in relevant_attributes | No annotation on drift resource; attribute goes to fallback | TC-10, TC-38 |
| ConfigurationReferences value includes deep suffix `.id` | Normalizes to `type.name` for matching | TC-15 |
| Data source reference `data.type.name.attribute` | Normalizes to `data.type.name` | TC-16 |
| Upstream resource address case mismatch | Case-insensitive match still succeeds | TC-17 |
| Multiple replace_paths on one resource | One ForcedReplacementAnnotation per matching path | TC-18 |
| Delete action (not just replace) | Treated same as replace for annotations | TC-04, TC-19 |
| All attributes correlated | Fallback section omitted entirely | TC-12, TC-36 |
| Partially correlated attributes | Uncorrelated subset appears in fallback | TC-13 |
| Combined card | Both lines render with correct labels | TC-08, TC-34 |

---

## Existing Tests Requiring Updates

### `ReportModelBuilderPlanContextTests.Build_RelevantAttributes_PopulatesModel`

**Current behaviour:** Asserts `model.RelevantAttributes[0].Resource == "example_resource.upstream"` with count 1.

**Post-feature semantics:** `model.RelevantAttributes` now holds only *uncorrelated* attributes (the fallback list). The existing test's plan has a single `update` resource with no `ConfigurationReferences`, so both existing `relevant_attributes` entries remain uncorrelated. The test assertions remain valid — no code change required.

**Note for Developer:** Add a comment clarifying that `model.RelevantAttributes` is now the uncorrelated fallback list.

### `Terraform114SnapshotTests.Snapshot_RelevantAttributesPresent_MatchesBaseline`

**Required action:** After feature implementation, delete `TestData/Snapshots/relevant-attributes-present.md` and re-run the test to regenerate the snapshot with the new `<details>` fallback section. Review and commit the updated snapshot. This test is handled by TC-37 in this plan.

---

## Non-Functional Tests

### Emoji spacing validation

All new snapshot files are automatically validated by `SnapshotTestAssertions.AssertNoEmojiFollowedByRegularSpace` (called before `AssertMatchesSnapshot` in the `AssertSnapshot` helper). The ⚠️ and 🔗 emojis in blockquote lines must be followed by a non-breaking space (U+00A0), not a regular space.

**Implication for renderer:** `RenderInlineRelevantAttributeAnnotations` must use `'\u00A0'` (non-breaking space) after each emoji, consistent with all other emoji uses in the renderer.

### Performance

No specific performance tests are required — the correlation algorithm is O(resources × attributes), and the existing `BuildReferenceIndex_LargeConfiguration_CompletesQuickly` test in `ConfigurationReferenceResolverTests` already covers the reference index building phase. If needed, a similar test can be added for `BuildInlineRelevantAttributeAnnotations` with a large number of resources and attributes.

---

## New Test Class: `InlineRelevantAttributeSnapshotTests`

**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/InlineRelevantAttributeSnapshotTests.cs`

**Pattern:** Mirrors `Terraform114SnapshotTests`. Each `[Test]` method calls a shared private `AssertSnapshot(string testDataFile, string snapshotName)` helper.

```csharp
/// <summary>
/// Snapshot tests for feature 660: Inline Relevant Attributes.
/// Covers forced-replacement callouts, depends-on lines, combined cards,
/// fallback section, and regression cases (in-place updates, drift, no relevant_attributes).
/// Related feature: docs/features/660-inline-relevant-attributes/specification.md.
/// </summary>
public class InlineRelevantAttributeSnapshotTests
{
    // TC-30 + TC-36
    [Test] Snapshot_ForcedReplacementUpstreamChanging_MatchesBaseline()
    // TC-32
    [Test] Snapshot_ForcedReplacementUpstreamStatic_MatchesBaseline()
    // TC-33
    [Test] Snapshot_DependsOnOnly_MatchesBaseline()
    // TC-34
    [Test] Snapshot_CombinedCard_ForcedAndDependsOn_MatchesBaseline()
    // TC-35
    [Test] Snapshot_FallbackSectionOnly_MatchesBaseline()
    // TC-38
    [Test] Snapshot_DriftWithRelevantAttributes_MatchesBaseline()
    // TC-37 (update to existing)
    [Test] Snapshot_InPlaceUpdateWithRelevantAttributes_MatchesBaseline()
    // TC-39 (no-change)
    [Test] Snapshot_NoRelevantAttributes_NoRegression_MatchesBaseline()
}
```

---

## New Test Class: `ReportModelBuilderInlineRelevantAttributeTests`

**Location:** `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderInlineRelevantAttributeTests.cs`

**Pattern:** Mirrors `ReportModelBuilderPlanContextTests` / `ReportModelBuilderActionsTests`. Uses inline JSON plan construction with `JsonDocument.Parse`, builds a `TerraformPlan`, calls `new ReportModelBuilder().Build(plan)`, and asserts on the resulting model.

Covers: TC-01 through TC-19.

---

## Open Questions

None — all design decisions have been finalised in the specification and architecture documents.
