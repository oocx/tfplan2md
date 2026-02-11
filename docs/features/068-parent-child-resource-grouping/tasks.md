# Tasks: Parent-Child Resource Grouping — Code Review Fixes

## Overview

Feature 068’s parent-child inline rendering framework is in place, but code review identified follow-up work that changes the matching architecture and requires additional tests:

- Parse additional Terraform plan JSON data (`configuration` block)
- Replace the old fallback concept with **configuration reference matching** for `(known after apply)` parent IDs
- Add/extend tests and synthetic plans to cover the new fallback and related edge cases
- Fix Docker build blocker (CA1875) so CI can pass

References:
- [code-review.md](code-review.md)
- [architecture.md](architecture.md) (Section “3a) Configuration Reference Matching”)
- [test-plan.md](test-plan.md) (TC-12..TC-21, TC-E6..E7)

## Tasks

### Task 1: Fix Docker Build Blocker (CA1875)

**Priority:** High

**Description:**
Fix the code analysis errors that break Docker builds by replacing `Regex.Matches(...).Count` with `Regex.Count(...)` in tests.

**Acceptance Criteria:**
- [ ] All CA1875 occurrences in test projects are fixed.
- [ ] `dotnet build` succeeds with analyzers-as-errors enabled (same settings as Docker/CI).
- [ ] No behavior change in the tests beyond the equivalent regex counting.

**Dependencies:** None

---

### Task 2: Extend Plan Parsing — Capture `configuration`

**Priority:** High

**Description:**
Update the Terraform plan parsing model to include the top-level `configuration` block (as a lightweight `JsonElement?`) so fallback matching can use Terraform expression references.

**Acceptance Criteria:**
- [ ] `TerraformPlan` includes `Configuration` mapped from JSON property `configuration` (nullable).
- [ ] Source generation / serializer context is updated so the new property is deserialized in all environments.
- [ ] Unit test TC-12 verifies parsing with a minimal plan JSON containing a `configuration.root_module.resources[].expressions.*.references` structure.
- [ ] Unit test verifies graceful handling when `configuration` is absent or null.

**Dependencies:** None

---

### Task 3: Implement `ConfigurationReferenceResolver`

**Priority:** High

**Description:**
Implement a resolver that walks the plan `configuration` tree and builds a reference index for lookups like:

`(child_address, attribute_name) -> referenced_addresses[]`

This enables precise parent-child matching even when parent IDs are `(known after apply)`.

**Acceptance Criteria:**
- [ ] Resolver supports root module resources (TC-13).
- [ ] Resolver returns an empty index when configuration is null/absent (TC-14).
- [ ] Resolver supports nested modules via `module_calls` with module-qualified addresses (TC-16).
- [ ] Resolver supports for_each/count instances by handling instance keys correctly (TC-17).
- [ ] Resolver does not throw on missing optional nodes; it fails closed (empty/partial index) rather than crashing.

**Dependencies:** Task 2

---

### Task 4: Integrate Fallback Matching into Parent-Child Merging

**Priority:** High

**Description:**
Update the separate-child matching logic to support `(known after apply)` parents by using configuration reference matching when the parent’s ID value is not available.

**Acceptance Criteria:**
- [ ] When `ParentIdAttribute` value is present, the existing value-based matching remains the primary path.
- [ ] When the parent ID is missing/empty, matching falls back to configuration references (TC-18).
- [ ] Matching is precise with multiple parents of the same type in the same module (TC-20).
- [ ] Graceful degradation: if configuration is absent OR no reference match exists, children remain standalone (no guessing) (TC-19).
- [ ] Integration snapshot test TC-15 demonstrates known-after-apply merging end-to-end.

**Dependencies:** Task 3

---

### Task 5: Add/Update Synthetic Plan Fixtures for Configuration Matching

**Priority:** High

**Description:**
Create/extend test data plans so the configuration fallback path is exercised by tests (including nested modules, for_each, no-configuration, and multiple-parents scenarios).

**Acceptance Criteria:**
- [ ] New fixtures exist as documented in [test-plan.md](test-plan.md):
	- [ ] `azuread-group-members-known-after-apply-plan.json`
	- [ ] `configuration-with-nested-modules.json`
	- [ ] `configuration-with-for-each.json`
	- [ ] `no-configuration-block-plan.json`
	- [ ] `multiple-parents-same-type.json`
- [ ] Fixtures include realistic `configuration.*.expressions.*.references` data that matches actual Terraform output shapes.
- [ ] Snapshot baselines are updated/added for TC-15 and TC-19.
- [ ] If the public demo plan(s) depend on the missing fallback, update the relevant example plan(s) so the demos show child tables (as intended).

**Dependencies:** Task 4

---

### Task 6: Add UAT/Example Artifact Coverage for Examples 1–6A

**Priority:** Medium

**Description:**
Create a deterministic markdown artifact (and snapshot coverage) that matches the documented rendering examples (Examples 1–6A) so UAT reviewers can validate GitHub/Azure DevOps rendering with confidence.

**Acceptance Criteria:**
- [ ] A single artifact markdown file in `artifacts/` demonstrates Examples 1–6A output structure (tables, columns, warnings, findings placement).
- [ ] At least one snapshot test asserts the key formatting invariants from the examples (table headers, Terraform Resource column labeling for inline vs separate, mixed-management warning, findings attribution).
- [ ] The artifact is stable (no timestamps, random IDs, or ordering instability).

**Dependencies:** Task 5

---

### Task 7: Add Robustness + Performance Tests for Resolver & Extractors

**Priority:** Medium

**Description:**
Add test coverage for the new error-handling and performance requirements introduced by configuration reference matching.

**Acceptance Criteria:**
- [ ] TC-E6: If an `IChildRowExtractor` throws, merging does not crash; the child remains standalone.
- [ ] TC-E7: Malformed/invalid JSON in child state is handled without crashing.
- [ ] TC-21: `ConfigurationReferenceResolver.BuildReferenceIndex()` has a performance-oriented test that enforces linear-ish behavior and a bounded runtime for a large configuration.

**Dependencies:** Task 3, Task 4

---

### Task 8: Minor Cleanup — `ChildTableColumn` Consistency

**Priority:** Low

**Description:**
Align `ChildTableColumn` with the lightweight record style used elsewhere (positional record) if it improves consistency without changing behavior.

**Acceptance Criteria:**
- [ ] Model is simplified without changing serialized/public behavior.
- [ ] No net new warnings; all tests remain green.

**Dependencies:** None

## Implementation Order

Recommended sequence for implementation:
1. Task 1 — unblock Docker/CI.
2. Task 2 + Task 3 — enable configuration reference matching.
3. Task 4 — integrate fallback behavior with graceful degradation.
4. Task 5 — add fixtures + snapshots for the new paths.
5. Task 6 + Task 7 — close the coverage gaps (examples, robustness, performance).
6. Task 8 — optional cleanup.

## Open Questions

- None.
