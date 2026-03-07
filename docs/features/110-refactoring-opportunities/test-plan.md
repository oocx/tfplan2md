# Test Plan: Core Report Pipeline and Provider Refactoring

## Overview

This test plan covers verification of the three internal architectural refactorings defined in
`specification.md`:

1. **Task 1** – Replace static mutable Azure role definition state with an instance-based resolver *(completed)*
2. **Task 2** – Extract explicit report-generation stages behind `ReportModelBuilder`
3. **Task 3** – Introduce a provider contribution model to narrow the provider integration surface
4. **Task 4** – Remove migration scaffolding and simplify `CompositionRoot`
5. **Task 5** – Final verification and documentation alignment

Because this is a pure internal refactoring with no change to CLI options or rendered Markdown
output, **no UAT plan is required**. The specification explicitly states: "No change to rendered
Markdown structure, styling, or semantics." Regression correctness is verified entirely through
automated tests.

---

## Test Coverage Matrix

| Acceptance Criterion (from `tasks.md`) | Test Case(s) | Test Type |
|-----------------------------------------|--------------|-----------|
| **Task 1 – Role definition resolver** | | |
| Run-scoped resolver abstraction exists as an injected service | TC-01 | Unit |
| Built-in role definitions remain immutable and reusable | TC-02, TC-03 | Unit |
| Custom role mappings are held per run, not in static state | TC-04 | Unit |
| Diagnostics associated with role resolution are scoped to the run | TC-05 | Unit |
| Existing role-resolution behavior is preserved | TC-02, TC-03, TC-06 | Unit |
| Tests for role resolution pass or are updated to new service boundary | TC-01 – TC-06 | Unit |
| **Task 2 – Explicit report-generation stages** | | |
| Major pipeline phases represented as explicit stage abstractions | TC-10, TC-11 | Unit |
| `ReportModelBuilder` no longer directly owns all transformation logic | TC-12 | Unit / Structural |
| Parent-child merging remains behaviorally identical | TC-13 | Unit / Snapshot |
| Summary and filtered-resource calculations remain behaviorally identical | TC-14 | Unit / Snapshot |
| Snapshot or parity tests confirm unchanged Markdown output | TC-15, TC-16 | Snapshot |
| Stage components testable in narrower units than the current builder | TC-10, TC-11, TC-17 | Unit |
| **Task 3 – Provider contribution model** | | |
| Providers contribute through a narrower contract than the current broad interface | TC-18 | Unit / Structural |
| `ProviderRegistry` no longer needs one fan-out method per capability area | TC-19 | Unit / Structural |
| `CompositionRoot` consumes provider contributions centrally | TC-20 | Unit |
| Existing provider-specific behavior remains unchanged | TC-21, TC-22 | Snapshot |
| Provider registration remains explicit and AOT-safe | TC-23 | Unit |
| **Task 4 – Cleanup** | | |
| Compatibility shims for the static role mapper are removed | TC-24 | Structural |
| Legacy provider registration paths are removed | TC-24 | Structural |
| `CompositionRoot` does not mutate global state during startup | TC-25 | Unit |
| Architectural suppressions no longer needed are removed | TC-26 | Architecture |
| **Task 5 – Verification** | | |
| All relevant tests pass for report generation, provider behavior, role resolution | TC-15, TC-16, TC-21, TC-22 | Snapshot |
| Snapshot updates (if any) are intentional and explained | TC-16 | Manual review |
| Architecture documentation reflects new pipeline and provider model | *(documentation review)* | Manual review |

---

## Test Cases

### TC-01: Role resolver is correctly constructed by `CompositionRoot`

**Type:** Unit  
**Status:** Exists (`CompositionRootTests.cs` – `CreateRoleDefinitionResolver_*`)

**Description:**  
Verifies that `CompositionRoot.CreateRoleDefinitionResolver` returns a non-null
`IRoleDefinitionResolver` instance, confirming the service is injectable and scoped to the
application run.

**Acceptance Criterion:** Run-scoped resolver abstraction exists as an injected service

**Test Data:** Default `CliOptions`; `MappingResult` with no custom entries

**Expected Result:** A non-null `IRoleDefinitionResolver` is returned.

---

### TC-02: Known built-in role ID maps to display name

**Type:** Unit  
**Status:** Exists (`AzureRoleDefinitionResolverTests.cs` – `GetRoleDefinition_KnownId_UsesMappedName`)

**Description:**  
Verifies that a well-known role GUID (e.g., Reader `acdd72a7...`) is resolved to its canonical
display name, confirming built-in role data is available on every resolver instance without
requiring static mutable state.

**Acceptance Criterion:** Built-in role definitions remain immutable and reusable; existing
role-resolution behavior is preserved

**Test Data:** Standard built-in Reader role ID

**Expected Result:** `info.Name == "Reader"`, `info.FullName` includes the GUID suffix.

---

### TC-03: Two independent resolver instances each return the same built-in role data

**Type:** Unit  
**Status:** Exists (`AzureRoleDefinitionResolverTests.cs` – `GetRoleDefinition_BuiltInRoles_AreImmutableAcrossInstances`)  
**Test Name:** `GetRoleDefinition_BuiltInRoles_AreImmutableAcrossInstances`

**Description:**  
Creates two `AzureRoleDefinitionResolver` instances with different custom-role sets and verifies
that both return identical results for a built-in role ID. This proves built-in data is shared
immutably rather than copied into mutable instance state.

**Acceptance Criterion:** Built-in role definitions remain immutable and reusable

**Test Data:** Two resolvers: one empty, one with a custom role that doesn't overlap the built-in
ID being tested

**Expected Result:** Both resolvers return the same `Name` and `FullName` for the same built-in
role ID.

---

### TC-04: Custom role mappings are isolated between resolver instances

**Type:** Unit  
**Status:** Exists (`AzureRoleDefinitionResolverTests.cs` – `GetRoleDefinition_CustomRoles_AreScopedPerResolverInstance`)

**Description:**  
Verifies that a custom role registered in one `AzureRoleDefinitionResolver` instance is not
visible in a separately constructed instance, confirming per-run scope.

**Acceptance Criterion:** Custom role mappings are held per application run, not in static
mutable state

**Test Data:** `resolverA` with custom role GUID; `resolverB` with no custom entries; same GUID
queried on both

**Expected Result:** `resolverA` returns the custom name; `resolverB` falls back to the raw
unknown value.

---

### TC-05: Diagnostics for unknown roles are scoped to the resolver instance

**Type:** Unit  
**Status:** Exists (`ResolutionDiagnosticsTests.cs`)

**Description:**  
Verifies that diagnostic events fired when an unrecognized role ID is encountered are captured by
the diagnostics sink associated with the resolver and do not leak to other diagnostic contexts.

**Acceptance Criterion:** Diagnostics associated with role resolution are scoped to the current run

**Test Data:** Resolver constructed with a mock or stub diagnostic sink; an unknown role ID

**Expected Result:** Diagnostic message appears in the resolver's sink; separate sink has no entries.

---

### TC-06: `AzureRoleDefinitionResolver` carries no mutable static fields

**Type:** Structural  
**Status:** Covered by `ProviderContributionStructureTests.cs` – `ProviderAndRoleResolutionTypes_HaveNoMutableStaticFields`  
**Test Name:** `ProviderAndRoleResolutionTypes_HaveNoMutableStaticFields`

**Description:**  
Uses reflection to inspect the `AzureRoleDefinitionResolver` class and asserts it exposes no
`static` non-readonly fields. This is a regression guard that prevents reintroduction of the
static mutable pattern.

**Acceptance Criterion:** `AzureRoleDefinitionResolver` no longer uses static mutable fields for
custom roles or diagnostics

**Test Data:** None (reflection over production assembly)

**Expected Result:** Zero mutable static fields found on the type.

---

### TC-10: `IResourceChangeStage` produces a `ResourceChangeModel` per plan resource

**Type:** Unit  
**Status:** Must be added (new test file under `MarkdownGeneration/Stages/`)  
**Test Name:** `ResourceChangeStage_Build_ProducesOneModelPerPlanResource`

**Description:**  
Constructs `IResourceChangeStage` in isolation (without the full builder) and verifies that, given
a minimal `TerraformPlan` with N resources, it produces exactly N initial `ResourceChangeModel`
instances.

**Preconditions:** `IResourceChangeStage` and its implementation exist (Task 2 complete for this
phase).

**Acceptance Criterion:** Stage components testable in narrower units than the current builder

**Test Data:** Inline `TerraformPlan` with 2–3 synthetic resource changes

**Expected Result:** Stage output contains exactly the same number of resource models as the input
plan.

---

### TC-11: `IAttributeFilteringStage` suppresses Azure ID case-change-only differences

**Type:** Unit  
**Status:** Must be added  
**Test Name:** `AttributeFilteringStage_Build_SuppressesCaseChangeOnlyAttributes`

**Description:**  
Constructs `IAttributeFilteringStage` in isolation and verifies that an Azure resource ID
attribute whose only change is letter casing is removed from the output.

**Acceptance Criterion:** Stage components testable in narrower units; attribute filtering remains
behaviorally identical

**Test Data:** A `ResourceChangeModel` with one attribute that differs only in case (e.g.,
`/SUBSCRIPTIONS/…` vs. `/subscriptions/…`)

**Expected Result:** The attribute is absent from the stage output.

---

### TC-12: `ReportModelBuilder` delegates to the six explicit stage interfaces

**Type:** Unit / Structural  
**Status:** Must be added  
**Test Name:** `ReportModelBuilder_Build_DelegatesToExplicitStages`

**Description:**  
Verifies that `ReportModelBuilder` no longer contains inline implementations of all pipeline
phases. Uses test doubles for each of the six stage interfaces (`IResourceChangeStage`,
`IAttributeFilteringStage`, `IParentChildMergeStage`, `ISummaryEnrichmentStage`,
`ICodeAnalysisEnrichmentStage`, `IReportAssemblyStage`) and confirms the builder calls each stage
in order. Alternatively, verifies via NetArchTest that `ReportModelBuilder` does not directly
reference the internal types that formerly did the work.

**Acceptance Criterion:** `ReportModelBuilder` no longer directly owns all transformation logic
across all phases

**Test Data:** Minimal plan; mock stages configured to return empty/identity outputs

**Expected Result:** Each stage's execution method is invoked exactly once in the expected order
(`IResourceChangeStage` → `IAttributeFilteringStage` → `IParentChildMergeStage` →
`ISummaryEnrichmentStage` → `ICodeAnalysisEnrichmentStage` → `IReportAssemblyStage`).

---

### TC-13: `IParentChildMergeStage` output is identical before and after stage extraction

**Type:** Unit  
**Status:** Must be added (supplement existing `ReportModelBuilderParentChildTests.cs`)  
**Test Name:** `ParentChildMergeStage_Build_ProducesIdenticalOutputToPreRefactoringBuilder`

**Description:**  
Runs the parent-child merge stage (or the refactored builder) with the same input that the
pre-refactoring `ReportModelBuilder` previously handled, and compares the resulting parent-child
tree structure field by field.

**Acceptance Criterion:** Parent-child merging remains behaviorally identical

**Test Data:** Existing test data from `ReportModelBuilderParentChildTests.cs`

**Expected Result:** The child/parent assignment and merge outputs are structurally equal to the
pre-refactoring results.

---

### TC-14: `ISummaryEnrichmentStage` output is unchanged after stage extraction

**Type:** Unit  
**Status:** Must be added (supplement or reuse `ReportModelBuilderSummaryTests.cs`)  
**Test Name:** `SummaryEnrichmentStage_Build_ProducesIdenticalSummaryToPreRefactoringBuilder`

**Description:**  
Runs the summary enrichment logic through `ISummaryEnrichmentStage` and asserts that
`ReportModel.Summary` fields (total resources, counts by action, etc.) match the values produced
by the pre-refactoring builder.

**Acceptance Criterion:** Summary and filtered-resource calculations remain behaviorally identical

**Test Data:** Existing summary test plans (e.g., plans with create/update/destroy mixes)

**Expected Result:** `Summary.TotalChanges`, per-action counts, and filtered counts all match
expected values unchanged.

---

### TC-15: Existing `ReportModelBuilder` snapshot tests pass after pipeline extraction

**Type:** Snapshot  
**Status:** Existing (all snapshot tests in `MarkdownGeneration/`)

**Description:**  
All existing snapshot-based tests must continue to pass against the refactored builder without any
change to the expected snapshot files. This is the primary regression guard during pipeline
extraction.

**Acceptance Criterion:** Snapshot or parity tests confirm unchanged Markdown output

**Test Data:** All existing snapshot test data files in `src/tests/.../TestData/`

**Expected Result:** Zero snapshot regressions.

---

### TC-16: End-to-end parity test: full plan produces identical Markdown before and after

**Type:** Snapshot / End-to-end  
**Status:** Existing (comprehensive demo snapshot tests)

**Description:**  
Generates Markdown output from the comprehensive demo plan before and after Tasks 2–4. Any
difference in the output must be flagged as a failing test unless accompanied by an intentional
snapshot update commit (with token `SNAPSHOT_UPDATE_OK`).

**Acceptance Criterion:** Snapshot or parity tests confirm unchanged Markdown output

**Test Data:** `artifacts/comprehensive-demo` test data or equivalent

**Expected Result:** Byte-for-byte identical Markdown output.

---

### TC-17: All six pipeline stages can be instantiated without `ReportModelBuilder`

**Type:** Unit / Structural  
**Status:** Must be added  
**Test Name:** `PipelineStage_CanBeInstantiated_WithoutFullBuilderGraph`

**Description:**  
Instantiates each of the six concrete stage implementations (`IResourceChangeStage`,
`IAttributeFilteringStage`, `IParentChildMergeStage`, `ISummaryEnrichmentStage`,
`ICodeAnalysisEnrichmentStage`, `IReportAssemblyStage`) using only their declared constructor
parameters — with no `ReportModelBuilder` in scope — and asserts each instance is non-null.
This proves stages are independently composable.

**Acceptance Criterion:** Stage components testable in narrower units

**Expected Result:** All six stages instantiate without exception.

---

### TC-18: Provider contribution aggregation exposes all capability registries

**Type:** Unit  
**Status:** Exists (`Providers/ProviderContributionSetTests.cs`)  
**Test Name:** `CreateContributionSet_CreatesNonNullRegistriesForRealProviders`

**Description:**  
Builds a `ProviderContributionSet` from the four production providers and asserts that each
downstream registry can be created successfully. This validates the narrowed provider capability
surface without relying on legacy fan-out registration paths.

**Acceptance Criterion:** Providers contribute capabilities through a narrower architectural
contract; contribution object aggregates optional capabilities in one place

**Test Data:** Registered production providers from `ProviderRegistry`

**Expected Result:** All downstream registries are created successfully and are non-null.

---

### TC-19: `ProviderRegistry` registers all capabilities centrally through one contribution set

**Type:** Unit  
**Status:** Exists (`Providers/ProviderContributionSetTests.cs`)  
**Test Name:** `CreateContributionSet_RegistersAllCapabilityTypesAtOnce`

**Description:**  
Registers a synthetic provider implementing every optional capability interface, then verifies
that a single `CreateContributionSet()` flow exposes factories, value formatters, icon providers,
parent-child relationships, filters, and renderers through the resulting registries.

**Acceptance Criterion:** `ProviderRegistry` no longer needs one fan-out method per capability
area; contributions consumed centrally

**Test Data:** Synthetic `ProviderContribution` with one entry per capability collection

**Expected Result:** Each capability resolves correctly from the registry after a single
contribution registration call.

---

### TC-20: `CompositionRoot` builds provider registry without shared global state

**Type:** Unit  
**Status:** Covered by `CompositionRootTests.cs` – `CreateProviderRegistry_RegistersAllProviders` and `CreateRoleDefinitionResolver_SequentialRoots_DoNotShareCustomRoleState`

**Description:**  
Verifies that `CompositionRoot.CreateProviderRegistry` still registers the expected providers and
that sequential compositions do not leak role-definition state across roots.

**Acceptance Criterion:** `CompositionRoot` consumes provider contributions centrally

**Test Data:** Default `CliOptions`; full composition run

**Expected Result:** Registry resolves the expected providers, and sequential compositions remain isolated.

---

### TC-21: AzureRM provider behavior is snapshot-identical after migration to contribution model

**Type:** Snapshot  
**Status:** Existing (AzureRM snapshot tests in `Providers/AzureRM/`)

**Description:**  
All existing AzureRM snapshot and rendering tests pass without change after the provider module is
migrated to the contribution model.

**Acceptance Criterion:** Existing provider-specific behavior remains unchanged

**Test Data:** Existing AzureRM test data (role assignment plans, resource models, etc.)

**Expected Result:** Zero regressions in existing provider tests.

---

### TC-22: All provider snapshot tests pass after contribution model migration

**Type:** Snapshot  
**Status:** Existing (all provider test files)

**Description:**  
Runs all provider-specific tests (AzureRM, AzureAD, AzApi, AzureDevOps) end-to-end and checks for
snapshot regressions.

**Acceptance Criterion:** Existing provider-specific behavior remains unchanged

**Expected Result:** All tests pass; no unintended snapshot changes.

---

### TC-23: Provider registration is explicit and does not use reflection or dynamic loading

**Type:** Structural / Architecture  
**Status:** Exists (`Providers/ProviderContributionStructureTests.cs`)  
**Test Name:** `ProviderRegistration_UsesExplicitStaticTypes`

**Description:**  
Uses NetArchTest or reflection to verify that no provider-registration path references
`Assembly.GetTypes()`, `Activator.CreateInstance`, `Type.GetType`, or `IServiceCollection.Scan`
(or any NativeAOT-hostile API). This guards AOT compatibility after the contribution model
migration.

**Acceptance Criterion:** Provider registration remains explicit and AOT-safe

**Expected Result:** No reflection-based registration paths found in the `Providers` or
`CompositionRoot` namespaces.

---

### TC-24: Post-cleanup: legacy provider contract is gone and no static mutable fields remain

**Type:** Structural  
**Status:** Exists (`Providers/ProviderContributionStructureTests.cs`)  
**Test Name:** `ProviderModuleContractType_IsRemovedFromProductionAssembly` and `ProviderAndRoleResolutionTypes_HaveNoMutableStaticFields`

**Description:**  
After Task 4 cleanup, uses reflection to verify that:

1. The legacy provider contract type no longer exists in the production assembly.
2. None of the production provider modules or `AzureRoleDefinitionResolver` expose mutable static fields.

**Acceptance Criterion:** Compatibility shims removed; legacy provider registration paths removed;
`CompositionRoot` no longer mutates global state

**Expected Result:** Legacy provider contract type not found in assembly; zero mutable static fields
found across all inspected provider types.

---

### TC-25: `CompositionRoot` startup does not write to static or global state

**Type:** Unit  
**Status:** Exists (`CompositionRootTests.cs` – `CreateRoleDefinitionResolver_SequentialRoots_DoNotShareCustomRoleState`)  
**Test Name:** `CreateRoleDefinitionResolver_SequentialRoots_DoNotShareCustomRoleState`

**Description:**  
Constructs a `CompositionRoot` and performs a full composition run twice sequentially in the same
process. Verifies that both runs produce the same results and that no cross-run interference
occurs. This detects residual global state mutations introduced during composition.

**Acceptance Criterion:** `CompositionRoot` is simpler and no longer mutates global state during
startup

**Test Data:** Default options; two sequential full-composition calls in one test

**Expected Result:** Results of both composition runs are equivalent; no exception or behavioral
difference between run 1 and run 2.

---

### TC-26: Architecture boundary exemption count does not increase after refactoring

**Type:** Architecture / Structural  
**Status:** Exists (`ArchitectureBoundaryTests.cs` – `Architecture_RefactoringDoesNotIncreaseExemptionCount`)  
**Test Name:** `Architecture_RefactoringDoesNotIncreaseExemptionCount`

**Description:**  
Verifies that the number of documented NetArchTest exemptions in `ArchitectureBoundaryTests.cs`
has not grown. Optionally verifies that existing exemptions related to `MarkdownGeneration →
Providers` are removed once the contribution model migration is complete.

**Acceptance Criterion:** Architectural suppressions that are no longer needed are removed where
practical

**Expected Result:** Fewer or equal exemption annotations compared to the pre-feature baseline
(ideally zero for the `MarkdownGeneration → Providers` exemptions).

---

## Test Data Requirements

| File | Purpose |
|------|---------|
| *(existing)* `TestData/minimal-plan.json` | Used by TC-10, TC-12, TC-15 |
| *(existing)* Parent-child test data in `TestData/` | Used by TC-13 |
| *(existing)* Summary test plans | Used by TC-14 |
| *(existing)* Comprehensive demo plan | Used by TC-16 |
| *(inline)* Synthetic plan with 2–3 resources | TC-10, TC-11 (created inline in test) |
| *(inline)* Synthetic `ProviderContribution` with stub capabilities | TC-18, TC-19 (created inline in test) |

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Pipeline stage receives empty plan | Returns empty list without throwing | TC-10 |
| Provider contribution with empty capability lists | Registry accepts contribution without error | TC-18 |
| Two `CompositionRoot` compositions in the same process | Identical outputs; no cross-run interference | TC-25 |
| Stage receives unknown provider name | Passes through without filtering | TC-10 |
| Custom role mapping file absent | Resolver falls back to built-in roles; no exception | TC-01 |
| Duplicate provider contribution registered | Registry should either deduplicate or throw consistently | TC-19 |

---

## Open Questions

### ~~OQ-1: Pipeline stage interface names~~ *(Resolved 2026-03-06)*

The stage interface names defined in `architecture.md` are the **final contracted names**:
`IResourceChangeStage`, `IAttributeFilteringStage`, `IParentChildMergeStage`,
`ISummaryEnrichmentStage`, `ICodeAnalysisEnrichmentStage`, `IReportAssemblyStage`.
All test cases in this plan use those names.

---

### ~~OQ-2: `IProviderModule` migration strategy~~ *(Resolved 2026-03-06)*

`IProviderModule` is **fully replaced in one step**. There is no temporary adapter layer.
TC-18, TC-19, TC-20, and TC-24 cover only the new `ProviderContribution`-based path.

---

## Non-Functional Verification

| Concern | Verification Approach |
|---------|----------------------|
| NativeAOT compatibility | TC-23 (no reflection-based registration); existing AOT build continues to pass in CI |
| Pure DI / explicit registration | TC-20, TC-23 |
| Incremental migration safety | TC-15, TC-16, TC-22 (snapshot coverage maintained throughout each task) |

---

## Definition of Done

- [ ] All TC-01 through TC-06 pass (Task 1 – already completed per `tasks.md`)
- [ ] TC-03 and TC-06 are added and pass
- [ ] TC-10 through TC-17 pass (Task 2)
- [ ] TC-18 through TC-23 pass (Task 3)
- [ ] TC-24 through TC-26 pass (Task 4)
- [ ] TC-15, TC-16, TC-21, TC-22 confirm zero snapshot regressions throughout (Task 5)
- [x] OQ-1 resolved: stage interface names are final
- [x] OQ-2 resolved: `IProviderModule` replaced in one step, no adapter layer
