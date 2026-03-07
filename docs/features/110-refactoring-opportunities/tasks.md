# Tasks: Core Report Pipeline and Provider Refactoring

## Overview

Implement the three top-priority refactorings from the refactoring opportunities review without
changing user-facing behavior:

1. Extract explicit report-generation stages from `ReportModelBuilder`
2. Narrow the provider integration surface through a provider contribution model
3. Remove static mutable state from Azure role definition resolution

## Task 1: Introduce an instance-based role definition resolver

**Priority:** High

**Description:**
Create a run-scoped role definition resolver service and migrate existing consumers away from the
static mutable `AzureRoleDefinitionMapper` state.

**Acceptance Criteria:**

- [x] A new role definition resolver abstraction exists as an injected service
- [x] Built-in role definitions remain immutable and reusable
- [x] Custom role mappings are held per application run, not in static mutable state
- [x] Diagnostics associated with role resolution are scoped to the current run
- [x] Existing role-resolution behavior is preserved
- [x] Existing tests for role resolution continue to pass or are updated to validate the new
      service boundary

**Dependencies:** None

**Status:** Completed 2026-03-06

---

## Task 2: Extract explicit report-generation stages behind `ReportModelBuilder`

**Priority:** High

**Description:**
Refactor `ReportModelBuilder` so that distinct phases of report generation are implemented by
separate stage components, with `ReportModelBuilder` acting as a façade or coordinator.

**Acceptance Criteria:**

- [x] At least the major current phases are represented as explicit stage abstractions
- [x] `ReportModelBuilder` no longer directly owns all transformation logic across all phases
- [x] Parent-child merging remains behaviorally identical
- [x] Summary and filtered-resource calculations remain behaviorally identical
- [x] Snapshot or parity tests confirm unchanged Markdown output
- [x] Stage components can be tested in narrower units than the current builder allows

**Dependencies:** Task 1 recommended first, but not strictly required

**Status:** Completed 2026-03-06

---

## Task 3: Introduce a provider contribution model

**Priority:** High

**Description:**
Replace the current broad provider module fan-out model with a narrower provider contribution
shape that can be consumed centrally by the registry and composition root.

**Acceptance Criteria:**

- [x] Providers contribute capabilities through a narrower architectural contract than the current
      broad interface
- [x] `ProviderRegistry` no longer needs one fan-out method per capability area in its final form
- [x] `CompositionRoot` consumes provider contributions centrally rather than manually coordinating
      as many capability-specific registries
- [x] Existing provider-specific behavior remains unchanged
- [x] Provider registration remains explicit and AOT-safe

**Dependencies:** Task 2 preferred, since extracted pipeline stages make provider boundaries easier
to define cleanly

**Status:** Completed 2026-03-06

---

## Task 4: Remove compatibility scaffolding and simplify composition

**Priority:** Medium

**Description:**
After the new resolver, pipeline stages, and provider contribution model are in place, remove any
temporary adapters or bridging code used during migration.

**Acceptance Criteria:**

- [x] Temporary compatibility shims for the static role mapper are removed
- [x] Legacy provider registration paths are removed if replaced by the new contribution model
- [x] `CompositionRoot` is simpler and no longer mutates global state during startup
- [x] Architectural suppressions that are no longer needed are removed where practical

**Dependencies:** Tasks 1-3

**Status:** Completed 2026-03-07

---

## Task 5: Verification and documentation alignment

**Priority:** High

**Description:**
Validate that behavior remains unchanged and ensure repository documentation reflects the new
architecture.

**Acceptance Criteria:**

- [x] Relevant tests pass for report generation, provider behavior, and role resolution
- [x] Snapshot updates, if any, are intentional and explained
- [x] Architecture documentation is updated to reflect the new report pipeline and provider model
- [x] `docs/features.md` is updated if the repository’s feature index should mention this work

**Dependencies:** Tasks 1-4

**Status:** Completed 2026-03-07

## Recommended Order

1. Task 1: instance-based role definition resolver
2. Task 2: extract report-generation stages
3. Task 3: provider contribution model
4. Task 4: cleanup and removal of migration scaffolding
5. Task 5: verification and documentation alignment

## Notes

- Task 1 is first because it removes a correctness risk and simplifies later composition work.
- Task 2 comes before Task 3 because a clear pipeline makes it easier to define where provider
  contributions should attach.
- Task 3 completed as a direct migration to narrow provider capability interfaces; no adapter
      layer remains.
- `docs/features.md` remains unchanged because it documents user-facing features, and this work is
      intentionally internal-only.
