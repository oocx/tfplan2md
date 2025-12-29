# Tasks: Replacement Reasons and Resource Summaries

## Overview

This feature adds concise summary lines to all resource changes in the generated Markdown report and displays replacement reasons (from `replace_paths` in Terraform plan JSON) to explain why resources are being recreated.

**References:**
- [Specification](specification.md)
- [Architecture](architecture.md)
- [Test Plan](test-plan.md)

## Tasks

### Task 1: Update Data Models

**Priority:** High

**Description:**
Update the core data models to support `replace_paths` and the new `Summary` property.

**Acceptance Criteria:**
- [x] `Change` record in `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs` includes `ReplacePaths` property with `JsonPropertyName("replace_paths")`.
- [x] `ResourceChangeModel` in `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` includes `ReplacePaths` and `Summary` properties.
- [x] Code compiles successfully.

**Dependencies:** None

---

### Task 2: Update Terraform Plan Parser

**Priority:** High

**Description:**
Ensure the `TerraformPlanParser` correctly deserializes the `replace_paths` field from the plan JSON.

**Acceptance Criteria:**
- [x] `TerraformPlanParser` parses `replace_paths` into the `Change` record.
- [x] Unit test `TC-01` (from test plan) passes.

**Dependencies:** Task 1

---

### Task 3: Implement ResourceSummaryBuilder Interface and Registry

**Priority:** Medium

**Description:**
Create the `IResourceSummaryBuilder` interface and a registry for resource-specific attribute mappings.

**Acceptance Criteria:**
- [x] `IResourceSummaryBuilder` interface created in `src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/`.
- [x] `ResourceAttributeMappingRegistry` (or similar internal structure) contains all 49+ mappings defined in the specification.
- [x] Registry supports provider-level fallbacks for `azurerm`, `azuredevops`, `azuread`, `azapi`, and `msgraph`.

**Dependencies:** Task 1

---

### Task 4: Implement ResourceSummaryBuilder Logic

**Priority:** High

**Description:**
Implement the core logic for generating summary strings based on action types and attribute mappings.

**Acceptance Criteria:**
- [x] `BuildSummary` handles `create` action using key attribute mappings (TC-03, TC-04, TC-05).
- [x] `BuildSummary` handles `update` action showing changed attributes (TC-06, TC-07).
- [x] `BuildSummary` handles `replace` action with replacement reasons (TC-08, TC-09).
- [x] `BuildSummary` handles `delete` action (TC-10).
- [x] `msgraph` resources are handled correctly using `url` and `body.displayName` (TC-12).
- [x] All unit tests for `ResourceSummaryBuilder` pass.

**Dependencies:** Task 3

---

### Task 5: Integrate Summary Builder into ReportModelBuilder

**Priority:** Medium

**Description:**
Update `ReportModelBuilder` to use `IResourceSummaryBuilder` during the model building process.

**Acceptance Criteria:**
- [x] `ReportModelBuilder` constructor accepts `IResourceSummaryBuilder`.
- [x] `BuildResourceChangeModel` calls `summaryBuilder.BuildSummary`.
- [x] `ResourceChangeModel.Summary` is populated in the final `ReportModel`.
- [x] Integration test `TC-02` passes.

**Dependencies:** Task 4

---

### Task 6: Update Program.cs and Dependency Injection

**Priority:** Medium

**Description:**
Wire up the new services in `Program.cs`.

**Acceptance Criteria:**
- [x] `ResourceSummaryBuilder` is instantiated in `Program.cs`.
- [x] `ResourceSummaryBuilder` is passed to `ReportModelBuilder`.
- [x] Application runs without errors.

**Dependencies:** Task 5

---

### Task 7: Update Default Template

**Priority:** Medium

**Description:**
Update the `default.sbn` template to display the summary line.

**Acceptance Criteria:**
- [x] `default.sbn` includes `**Summary:** {{ change.summary }}` above the `<details>` tag.
- [x] Summary is only displayed if it is not null or empty.
- [x] Snapshot test `TC-13` passes.

**Dependencies:** Task 5

---

### Task 8: Update Documentation and Demo

**Priority:** Low

**Description:**
Update the comprehensive demo and any relevant user documentation to reflect the new feature.

**Acceptance Criteria:**
- [x] `examples/comprehensive-demo/report.md` (or similar) shows the new summaries.
- [x] README or other docs mention the new summary feature.

**Dependencies:** Task 7

## Implementation Order

1. **Task 1 & 2** - Foundational work for data parsing.
2. **Task 3 & 4** - Core logic for summary generation (can be developed and tested in isolation).
3. **Task 5 & 6** - Integration into the main pipeline.
4. **Task 7** - UI/Template update.
5. **Task 8** - Final documentation and demo updates.

## Open Questions

- None at this stage.
