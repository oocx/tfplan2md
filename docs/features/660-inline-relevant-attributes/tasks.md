# Tasks: Inline Relevant Attributes (Feature 660)

## Overview

Redesigns `relevant_attributes[]` rendering from a standalone `## Relevant Attributes` H2 table at the bottom of the report into per-resource inline annotations (forced-replacement callouts and depends-on lines) on each replaced/destroyed resource card, with a collapsible `<details>` fallback section for uncorrelated attributes.

Reference: `docs/features/660-inline-relevant-attributes/specification.md`  
Architecture: `docs/features/660-inline-relevant-attributes/architecture.md`  
Test Plan: `docs/features/660-inline-relevant-attributes/test-plan.md`

---

## Tasks

### Task 1: Add new annotation model types

**Priority:** High

**Description:**  
Create two new `internal sealed record` types in `MarkdownGeneration/Models/` to carry the computed correlation data. These records are the data contracts between the builder and the renderer — all other tasks depend on them existing.

**Files to create:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ForcedReplacementAnnotation.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/DependsOnAnnotation.cs`

**`ForcedReplacementAnnotation` fields:**
- `string LocalAttribute` — top-level attribute name from `replace_paths` (e.g. `"network_interface_ids"`)
- `string UpstreamResource` — upstream resource address (e.g. `"azurerm_network_interface.web"`)
- `string UpstreamAttributePath` — pre-formatted attribute path (e.g. `"id"`)
- `bool IsChangingInThisPlan` — true when the upstream resource is itself replaced or destroyed in the same plan

**`DependsOnAnnotation` fields:**
- `string UpstreamResource` — upstream resource address
- `string UpstreamAttributePath` — pre-formatted attribute path
- `bool IsChangingInThisPlan` — true when the upstream resource is itself replaced or destroyed in the same plan

**Acceptance Criteria:**
- [ ] `ForcedReplacementAnnotation.cs` exists as `internal sealed record` with the four properties above
- [ ] `DependsOnAnnotation.cs` exists as `internal sealed record` with the three properties above
- [ ] Both files are in the `Oocx.TfPlan2Md.MarkdownGeneration.Models` namespace
- [ ] The project compiles without errors after adding these files

**Dependencies:** None

**Notes:**  
Use the same namespace and visibility pattern as `RelevantAttributeModel.cs`.

---

### Task 2: Add annotation properties to `ResourceChangeModel`

**Priority:** High

**Description:**  
Add two new `internal` properties to `ResourceChangeModel` to hold the computed annotation lists. These properties are populated post-stage by `ReportModelBuilder` (the same pattern already used for `Summary`, `Actions`, `CodeAnalysisFindings`, etc.).

**File to modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ResourceChangeModel.cs`

**Properties to add (after existing `internal` properties):**
```csharp
internal IReadOnlyList<ForcedReplacementAnnotation> ForcedReplacementAnnotations { get; set; } = [];
internal IReadOnlyList<DependsOnAnnotation> DependsOnAnnotations { get; set; } = [];
```

**Acceptance Criteria:**
- [ ] Both properties are `internal` (not `public`)
- [ ] Both default to empty lists (`= []`), preserving null-safety for plans without `relevant_attributes`
- [ ] The project compiles without errors

**Dependencies:** Task 1

**Notes:**  
This follows the existing pattern for `Actions` and `CodeAnalysisFindings` on the same class. Place these after the existing `internal` properties so the diff is localized.

---

### Task 3: Implement `BuildInlineRelevantAttributeAnnotations` in `ReportModelBuilder.PlanContext.cs`

**Priority:** High

**Description:**  
Add the correlation method that matches `relevant_attributes[]` entries to `ResourceChangeModel` instances and populates `ForcedReplacementAnnotations` and `DependsOnAnnotations`. This is the core algorithmic change.

**File to modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.PlanContext.cs`

**Method signature:**
```csharp
private static IReadOnlyList<RelevantAttributeModel> BuildInlineRelevantAttributeAnnotations(
    IReadOnlyList<ResourceChangeModel> allChanges,
    IReadOnlyList<RelevantAttributeModel> allRelevantAttributes)
```

**Algorithm (per architecture doc):**
1. Fast-exit: if `allRelevantAttributes.Count == 0`, return empty list immediately (no-op path).
2. Build `byUpstream`: `Dictionary<string, List<RelevantAttributeModel>>` keyed by `ra.Resource`, case-insensitive (`StringComparer.OrdinalIgnoreCase`).
3. Build `replacedOrDestroyedAddresses`: `HashSet<string>` (case-insensitive) of resource addresses where action is `"replace"` or `"delete"`.
4. Track `HashSet<RelevantAttributeModel> correlated` (by reference).
5. Iterate `allChanges`; skip any resource where action is not `"replace"` or `"delete"` (in-place updates and drift are excluded — **drift resources are not in `allChanges`**, so they are already excluded).
6. For each eligible `ResourceChangeModel rc`:
   a. **Find all correlated entries:** iterate `rc.ConfigurationReferences` keys+values, normalize each reference string to a resource address (using `NormalizeReferenceToResourceAddress`), look up `byUpstream[normalizedRef]` and collect matching `RelevantAttributeModel` entries into `correlatedForResource`.
   b. **Identify forced-replacement entries:** for each `replacePath` in `rc.ReplacePaths`, extract the top-level attribute name (first segment), look up `rc.ConfigurationReferences[topAttr]`, normalize those references, find any `correlatedForResource` entries whose `Resource` matches — these are `forcedEntries`.
   c. **Populate `ForcedReplacementAnnotations`:** for each forced `(topAttr, ra)` pair, create a `ForcedReplacementAnnotation` with `IsChangingInThisPlan = replacedOrDestroyedAddresses.Contains(ra.Resource)`. Add `ra` to `correlated`.
   d. **Populate `DependsOnAnnotations`:** for entries in `correlatedForResource` NOT in `forcedEntries`, create a `DependsOnAnnotation` with `IsChangingInThisPlan = replacedOrDestroyedAddresses.Contains(ra.Resource)`. Add `ra` to `correlated`.
7. Return `allRelevantAttributes.Except(correlated).ToList()` as the uncorrelated list.

**Add private helper:**
```csharp
private static string NormalizeReferenceToResourceAddress(string reference)
```
- For `data.*` references (starts with `"data."`): take the first three dot-separated segments (e.g. `"data.azurerm_client_config.current.tenant_id"` → `"data.azurerm_client_config.current"`).
- For managed resources: take the first two dot-separated segments (e.g. `"azurerm_network_interface.web.id"` → `"azurerm_network_interface.web"`).
- If the reference has fewer segments than required (already a bare address), return it unchanged.

**Acceptance Criteria:**
- [ ] `BuildInlineRelevantAttributeAnnotations` compiles and is accessible within the `ReportModelBuilder` partial class
- [ ] Returns an empty list immediately when `allRelevantAttributes` is empty (TC-14 fast-exit path)
- [ ] Populates `ForcedReplacementAnnotations` correctly for a replace resource with matching `replace_paths` → `ConfigurationReferences` → `relevant_attributes` chain (covers TC-01, TC-02)
- [ ] Sets `IsChangingInThisPlan = true` when the upstream resource is in `replacedOrDestroyedAddresses` — i.e., action is `replace` or `delete` (covers TC-03, TC-04)
- [ ] Sets `IsChangingInThisPlan = false` when the upstream resource has action `update` (covers TC-05)
- [ ] Routes non-forced correlated entries to `DependsOnAnnotations` (covers TC-06)
- [ ] Returns an empty uncorrelated list when all attributes were correlated (covers TC-07, TC-12)
- [ ] Leaves `update` resources with empty annotation lists (covers TC-09); uncorrelated attributes remain in the fallback list
- [ ] `NormalizeReferenceToResourceAddress` handles managed resource references with `.attribute` suffix (covers TC-15) and data source references (covers TC-16)
- [ ] Correlation is case-insensitive (covers TC-17)
- [ ] Multiple `replace_paths` produce multiple `ForcedReplacementAnnotation` entries (covers TC-18)
- [ ] A `delete` resource is treated identically to `replace` (covers TC-19)
- [ ] `model.RelevantAttributes` after build contains only uncorrelated attributes (covers TC-13)

**Dependencies:** Task 1, Task 2

**Notes:**  
The method name and location follow the pattern of `BuildCodeAnalysisReport`, `BuildActionInvocations`, etc. in `ReportModelBuilder.PlanContext.cs`. The `NormalizeReferenceToResourceAddress` helper should be a small `private static` method in the same partial class. For the top-level attribute name extraction from `replace_paths`, use `ResourceSummaryPathFormatter.FormatReplacePath` (already exists) and take the segment before the first `[` or `.`.

---

### Task 4: Wire `BuildInlineRelevantAttributeAnnotations` into `ReportModelBuilder.Build.cs`

**Priority:** High

**Description:**  
Update the build pipeline to call the new correlation method after `BuildRelevantAttributes` and pass only the uncorrelated list to `ReportAssemblyInput`.

**File to modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`

**Change:**  
Replace:
```csharp
var relevantAttributes = BuildRelevantAttributes(plan);
```
With:
```csharp
var relevantAttributes = BuildRelevantAttributes(plan);
var uncorrelatedRelevantAttributes = BuildInlineRelevantAttributeAnnotations(allChanges, relevantAttributes);
```
And pass `uncorrelatedRelevantAttributes` (instead of `relevantAttributes`) wherever `relevantAttributes` was previously passed to `ReportAssemblyInput` or the model assembly.

**Acceptance Criteria:**
- [ ] `BuildInlineRelevantAttributeAnnotations` is called immediately after `BuildRelevantAttributes`
- [ ] The uncorrelated list (not the full list) is passed to downstream assembly
- [ ] The project compiles without errors
- [ ] Plans without `relevant_attributes` produce identical model output (no regression — the fast-exit path in Task 3 ensures this)

**Dependencies:** Task 3

---

### Task 5: Add `RenderInlineRelevantAttributeAnnotations` helper to `DefaultResourceRenderer.Helpers.cs`

**Priority:** High

**Description:**  
Add the private rendering helper that emits the `⚠️ Forced replacement` and `🔗 Depends on` / `🔗 Also depends on` blockquote lines inside each resource card.

**File to modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.Helpers.cs`

**Rendering logic:**

1. **Forced replacement lines** — if `change.ForcedReplacementAnnotations.Count > 0`:  
   For each `ForcedReplacementAnnotation ann`:
   ```
   > ⚠️ **Forced replacement** — `{ann.LocalAttribute}` reads `{ann.UpstreamResource}.{ann.UpstreamAttributePath}`{changingPhrase}
   ```
   Where `changingPhrase` is `, which is **changing in this plan**.` when `ann.IsChangingInThisPlan`, else `.` (period only).

2. **Depends-on line** — if `change.DependsOnAnnotations.Count > 0`:  
   - Label: `🔗 **Also depends on:**` when `ForcedReplacementAnnotations.Count > 0`; else `🔗 **Depends on:**`
   - Emit a single blockquote line listing all entries as comma-separated inline code:  
     ``> {label} `{ann.UpstreamResource}.{ann.UpstreamAttributePath}`[⚠️]``  
     where ` ⚠️` (with a preceding space) is appended after the closing backtick when `ann.IsChangingInThisPlan`.

3. If both lists are empty, emit nothing (preserves existing snapshot output for plans without relevant attributes).

**Acceptance Criteria:**
- [ ] Method signature: `private static void RenderInlineRelevantAttributeAnnotations(TextWriter writer, ResourceChangeModel change)` (or equivalent with `IndentedTextWriter` / `MarkdownWriter` matching the existing writer type in the class)
- [ ] Forced replacement line format matches spec exactly: `> ⚠️ **Forced replacement** — \`{attr}\` reads \`{upstream}.{path}\`, which is **changing in this plan**.`
- [ ] When `IsChangingInThisPlan` is false, line ends with `.` (period), no "changing in this plan" phrase (covers TC-32)
- [ ] Depends-on line uses `🔗 **Depends on:**` label when no forced-replacement line is present (covers TC-33)
- [ ] Depends-on line uses `🔗 **Also depends on:**` label when forced-replacement line is also present (covers TC-34)
- [ ] `⚠️` marker appended after each upstream reference where `IsChangingInThisPlan` is true in the depends-on line
- [ ] When both lists are empty, nothing is written (no regression for plans without relevant attributes)

**Dependencies:** Task 1, Task 2

**Notes:**  
Follow the existing pattern of `RenderInlineActions` in `DefaultResourceRenderer.Helpers.cs`. The `writer` type must match the writer already used in that file (check the actual type). The single blockquote for the depends-on list means all entries appear on **one line** separated by `, `.

---

### Task 6: Call `RenderInlineRelevantAttributeAnnotations` from `DefaultResourceRenderer.cs`

**Priority:** High

**Description:**  
Wire the new rendering helper into the resource card render flow, inserting it after `RenderCodeAnalysisMetadata` and before the attribute diff table.

**File to modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`

**Change:**  
In `DefaultResourceRenderer.Render` (or equivalent render method), after the `RenderCodeAnalysisMetadata` call and before the table rendering, add:
```csharp
RenderInlineRelevantAttributeAnnotations(writer, change);
```

**Acceptance Criteria:**
- [ ] `RenderInlineRelevantAttributeAnnotations` is called in the correct position: after any code-analysis metadata and before the attribute diff table
- [ ] The annotation lines appear **inside** the `<details>` block, above the diff table (per spec UX examples)
- [ ] Resources with empty annotation lists are not affected (covers the no-regression case)
- [ ] In-place update resources (which have empty annotation lists by construction) emit no annotation lines (covers TC-37)

**Dependencies:** Task 5

---

### Task 7: Replace `RenderRelevantAttributes` in `ReportRenderer.cs` with collapsible fallback section

**Priority:** High

**Description:**  
Replace the existing flat `## Relevant Attributes` H2 table renderer with a collapsible `<details>` fallback section. The section is omitted entirely when the uncorrelated list is empty.

**File to modify:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` (lines 118–141 per architecture)

**New rendering output:**
```markdown
<details>
<summary>🔗 Other plan inputs ({N}) — read by this plan but not tied to a specific change</summary>

> These existing values were read to compute the plan. If they change before apply, the plan may be stale.

- `{ra.Resource}.{ra.AttributePath}`
...

</details>
```
Where `{N}` is `attributes.Count`.

**Omit entirely** when `attributes.Count == 0`.

**Acceptance Criteria:**
- [ ] The `## Relevant Attributes` H2 heading is removed from all rendered output
- [ ] The 2-column table is removed from all rendered output
- [ ] A `<details>` block with the `🔗 Other plan inputs (N)` summary is rendered when uncorrelated attributes exist (covers TC-35)
- [ ] The fallback section is completely absent when `attributes.Count == 0` (covers TC-36)
- [ ] Each uncorrelated attribute is rendered as `` - `{ra.Resource}.{ra.AttributePath}` `` (a Markdown list item)
- [ ] The explanatory blockquote appears inside the `<details>` block, before the list

**Dependencies:** Task 4 (ensures `ReportModel.RelevantAttributes` now holds only uncorrelated entries)

**Notes:**  
Check how `ra.AttributePath` is already formatted in `RelevantAttributeModel.cs` — it may already be a dotted string. The fallback section replaces the **entire** `RenderRelevantAttributes` method body; no H2 heading or table markup should remain.

---

### Task 8: Write unit tests in `ReportModelBuilderInlineRelevantAttributeTests`

**Priority:** High

**Description:**  
Create the new unit test file covering the correlation logic in `BuildInlineRelevantAttributeAnnotations`. Follow the pattern of `ReportModelBuilderActionsTests.cs`.

**File to create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderInlineRelevantAttributeTests.cs`

**Test cases to implement (from test plan):**
- TC-01: Single `ForcedReplacementAnnotation` populated for replace resource
- TC-02: Annotation fields `LocalAttribute`, `UpstreamResource`, `UpstreamAttributePath` are correct
- TC-03: `IsChangingInThisPlan = true` when upstream is also being replaced
- TC-04: `IsChangingInThisPlan = true` when upstream is being deleted
- TC-05: `IsChangingInThisPlan = false` when upstream resource is in-place update
- TC-06: Correlated non-forced entries route to `DependsOnAnnotations`
- TC-07: All correlated → `model.RelevantAttributes` is empty
- TC-08: Both `ForcedReplacementAnnotations` and `DependsOnAnnotations` are populated simultaneously (enables "Also depends on" label in renderer)
- TC-09: In-place update resource has empty annotation lists; attribute remains in fallback
- TC-10: Drift resources have no annotations (drift resources are not in `allChanges`)
- TC-11: Uncorrelated attributes appear in `model.RelevantAttributes`
- TC-12: Fallback list empty when all attributes correlated
- TC-13: `model.RelevantAttributes` contains only uncorrelated attributes (semantic change from feature 122)
- TC-14: Empty `relevant_attributes` → no annotations, no fallback (fast-exit path)
- TC-15: Managed resource reference with `.attribute` suffix normalizes correctly
- TC-16: Data source reference (`data.type.name.attr`) normalizes to 3-segment address
- TC-17: Case-insensitive resource address matching
- TC-18: Multiple `replace_paths` → multiple `ForcedReplacementAnnotation` entries
- TC-19: `delete` action treated identically to `replace` for annotations

**Acceptance Criteria:**
- [ ] Test class is named `ReportModelBuilderInlineRelevantAttributeTests` in the `Oocx.TfPlan2Md.TUnit.MarkdownGeneration` namespace
- [ ] All 19 test cases (TC-01 through TC-19) are implemented with assertions matching the test plan
- [ ] Tests construct `TerraformPlan` instances inline (no JSON fixtures required) — follow `ReportModelBuilderActionsTests` pattern
- [ ] All tests pass (green) when the implementation is complete
- [ ] No test depends on snapshot files

**Dependencies:** Tasks 1–4

---

### Task 9: Create new snapshot test fixtures (JSON plan files)

**Priority:** High

**Description:**  
Create the 7 new JSON plan fixtures required by the snapshot tests. These fixtures must include a `configuration` block so that `ConfigurationReferenceResolver` can populate `ConfigurationReferences` on the change models.

**Files to create** (in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/tf114/`):

| File | Covers | Key content |
|---|---|---|
| `relevant-attrs-forced-replacement-upstream-changing-plan.json` | TC-30, TC-36 | NIC + VM both replaced; NIC ID in `relevant_attributes`; `configuration` linking VM → NIC |
| `relevant-attrs-forced-replacement-upstream-static-plan.json` | TC-32 | VM replaced; NIC NOT in plan; NIC ID in `relevant_attributes`; `configuration` linking VM → NIC |
| `relevant-attrs-depends-on-only-plan.json` | TC-33 | App service replaced; `site_config` → non-upstream; `identity` → data source in `relevant_attributes` |
| `relevant-attrs-combined-card-plan.json` | TC-34 | App service + key vault both replaced; KV vault_uri in `relevant_attributes`; data.client_config tenant_id also |
| `relevant-attrs-fallback-only-plan.json` | TC-35 | Update resource only; two unrelated `relevant_attributes` entries; no `replace` resources |
| `relevant-attrs-all-correlated-plan.json` | TC-36 | Same scenario as TC-30 fixture (all correlated, no fallback); can reuse or reference TC-30's fixture |
| `relevant-attrs-drift-with-relevant-attrs-plan.json` | TC-38 | Drift entry + one `update` change + relevant attribute referencing the upstream in drift |

**All fixtures must include:**
- `"format_version": "1.2"` and `"terraform_version": "1.14.0"`
- A `"configuration"` block with `root_module.resources[].expressions` containing `references` arrays (required for `ConfigurationReferenceResolver` to work)
- Minimal but realistic `"before"` / `"after"` values in `resource_changes`

**Acceptance Criteria:**
- [ ] All 7 fixture files exist and are valid JSON
- [ ] Each fixture has a `configuration` block with `root_module.resources[].expressions` (not empty)
- [ ] TC-30 fixture: two resources with `"actions": ["create", "delete"]`; NIC's address appears in VM's `network_interface_ids.references`; NIC address in `relevant_attributes`
- [ ] TC-32 fixture: only the VM resource is present (NIC absent from `resource_changes`); same `relevant_attributes` as TC-30
- [ ] TC-33 fixture: app service replaced; forced path traces to a non-upstream; `identity` → data source in `relevant_attributes` but NOT in `replace_paths`
- [ ] TC-34 fixture: app service and key vault both replaced; key vault appears in `replace_paths` → `app_settings` → `relevant_attributes`; data source in `identity` → `relevant_attributes`
- [ ] TC-35 fixture: only `update` resource changes; two unrelated entries in `relevant_attributes`
- [ ] TC-38 fixture: has `resource_drift` array with one entry; `resource_changes` has one `update`; `relevant_attributes` references the drift upstream

**Dependencies:** None (fixtures can be authored in parallel with code tasks)

**Notes:**  
Use the existing `relevant-attributes-present-plan.json` as a structural reference for how `relevant_attributes` and `configuration` blocks are formatted. For TC-36 (all-correlated), it is acceptable to reuse the TC-30 fixture by pointing the snapshot test at the same plan file.

---

### Task 10: Create new snapshot test class `InlineRelevantAttributeSnapshotTests`

**Priority:** High

**Description:**  
Add the new snapshot test class that exercises the 7 new fixtures and verifies the expected markdown output.

**File to create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/InlineRelevantAttributeSnapshotTests.cs`

**Test methods (one per scenario):**
- `ForcedReplacementUpstreamChanging` → fixture `relevant-attrs-forced-replacement-upstream-changing-plan.json`, snapshot `relevant-attrs-forced-replacement-upstream-changing.md` (TC-30)
- `ForcedReplacementUpstreamStatic` → fixture `relevant-attrs-forced-replacement-upstream-static-plan.json`, snapshot `relevant-attrs-forced-replacement-upstream-static.md` (TC-32)
- `DependsOnOnly` → fixture `relevant-attrs-depends-on-only-plan.json`, snapshot `relevant-attrs-depends-on-only.md` (TC-33)
- `CombinedCard` → fixture `relevant-attrs-combined-card-plan.json`, snapshot `relevant-attrs-combined-card.md` (TC-34)
- `FallbackOnly` → fixture `relevant-attrs-fallback-only-plan.json`, snapshot `relevant-attrs-fallback-only.md` (TC-35)
- `AllCorrelated` → fixture `relevant-attrs-all-correlated-plan.json`, snapshot `relevant-attrs-all-correlated.md` (TC-36) *(may reuse TC-30 fixture)*
- `DriftWithRelevantAttrs` → fixture `relevant-attrs-drift-with-relevant-attrs-plan.json`, snapshot `relevant-attrs-drift-with-relevant-attrs.md` (TC-38)

**Acceptance Criteria:**
- [ ] Test class follows the pattern of `Terraform114SnapshotTests.cs`
- [ ] Each test method loads the fixture JSON, generates the markdown report, and compares to the snapshot file
- [ ] On first run (no snapshot files exist), tests auto-generate the snapshot files — developer must review and commit them
- [ ] Snapshot for TC-30 contains `⚠️ **Forced replacement**` callout with `**changing in this plan**`
- [ ] Snapshot for TC-30 does NOT contain `## Relevant Attributes` heading
- [ ] Snapshot for TC-30 does NOT contain a `🔗 Other plan inputs` fallback section
- [ ] Snapshot for TC-32 contains forced-replacement callout WITHOUT "changing in this plan"
- [ ] Snapshot for TC-33 contains `🔗 **Depends on:**` (not "Also depends on")
- [ ] Snapshot for TC-34 contains both lines with `🔗 **Also depends on:**` label
- [ ] Snapshot for TC-35 contains `<details>` fallback section with count `(2)` and both attributes
- [ ] Snapshot for TC-36 has NO `<details>` fallback section
- [ ] Snapshot for TC-38 drift card has no annotation lines; fallback section contains the attribute

**Dependencies:** Tasks 5–9

**Notes:**  
The snapshot `.md` files do not need to be manually authored — they are generated by running the tests with snapshot-creation mode enabled. The developer should run the tests once, inspect the generated snapshots for correctness, then commit them.

---

### Task 11: Update existing snapshot `relevant-attributes-present.md`

**Priority:** High

**Description:**  
The existing `relevant-attributes-present.md` snapshot currently shows the `## Relevant Attributes` H2 table. After this feature, the existing `relevant-attributes-present-plan.json` fixture (which has one `update` resource and two uncorrelated `relevant_attributes`) should now render a `<details>` fallback section instead.

**File to update:**
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/relevant-attributes-present.md`

**Change:**
- Remove the `## Relevant Attributes` heading and 2-column table
- Add the `<details>` collapsible fallback section with the two attributes listed

**Also verify:**
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/relevant-attributes-absent.md` must NOT change (this is the regression guard for TC-39)

**Acceptance Criteria:**
- [ ] `relevant-attributes-present.md` no longer contains `## Relevant Attributes`
- [ ] `relevant-attributes-present.md` contains a `<details>` block with `🔗 Other plan inputs (2)` summary (TC-37)
- [ ] `relevant-attributes-absent.md` is byte-identical to the current approved baseline (TC-39)
- [ ] The existing `Terraform114SnapshotTests` test that references `relevant-attributes-present.md` passes with the updated snapshot

**Dependencies:** Tasks 4, 7

**Notes:**  
Use the `update-test-snapshots` skill or run the test suite in update mode to regenerate this snapshot automatically rather than editing it by hand.

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1** — New model types (`ForcedReplacementAnnotation`, `DependsOnAnnotation`) — No dependencies; unblocks everything else. Create these first.
2. **Task 2** — Add properties to `ResourceChangeModel` — Depends on Task 1; unblocks Tasks 3, 5.
3. **Task 9** — Create new JSON fixtures — No code dependencies; can be done in parallel with Tasks 1–2 while waiting for review.
4. **Task 3** — `BuildInlineRelevantAttributeAnnotations` algorithm — Core logic; depends on Tasks 1–2.
5. **Task 4** — Wire into `ReportModelBuilder.Build.cs` — Depends on Task 3; short change.
6. **Task 7** — Replace `RenderRelevantAttributes` fallback section — Depends on Task 4 (semantics of uncorrelated list); can be done in parallel with Tasks 5–6.
7. **Task 5** — `RenderInlineRelevantAttributeAnnotations` rendering helper — Depends on Tasks 1–2.
8. **Task 6** — Call the helper from `DefaultResourceRenderer.cs` — Depends on Task 5.
9. **Task 8** — Unit tests for `BuildInlineRelevantAttributeAnnotations` — Depends on Tasks 1–4.
10. **Task 10** — New snapshot test class — Depends on Tasks 5–9.
11. **Task 11** — Update existing snapshot `relevant-attributes-present.md` — Depends on Tasks 4 and 7; run tests in update mode.

## Open Questions

None — all design decisions are resolved in the specification and architecture documents.
