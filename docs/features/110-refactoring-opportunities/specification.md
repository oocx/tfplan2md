# Feature: Refactoring the Core Report Pipeline and Provider Architecture

## Overview

Refactor the three highest-value architectural hotspots identified in the refactoring review:

1. Decompose `ReportModelBuilder` into explicit report-generation pipeline stages
2. Replace the broad provider module contract with a narrower contribution model
3. Remove static mutable state from Azure role definition resolution

This feature is an internal architecture improvement initiative. It is intended to reduce hidden
coupling, make the execution flow explicit, and improve maintainability without changing the
rendered Markdown output or CLI behavior.

## Background

The review documented in `refactoring-opportunities.md` found that several core abstractions now
carry more responsibility than their names suggest.

The most significant issues are:

- `ReportModelBuilder` is effectively a multi-phase pipeline hidden behind a single builder type
- the provider extension model has grown into a wide interface that forces invasive changes when
  capabilities evolve
- Azure role definition resolution depends on global mutable process state, which creates hidden
  runtime coupling

These issues are not isolated style concerns. They affect correctness boundaries, test isolation,
and the cost of future feature work.

## Goals

- Make report generation phases explicit and testable as distinct stages
- Reduce architectural coupling between provider-specific behavior and application composition
- Eliminate hidden process-wide mutable state from Azure role definition resolution
- Preserve existing user-facing behavior and Markdown output
- Lower the ongoing need for broad complexity and coupling suppressions in core types

## Non-Goals

- No change to CLI options or command-line behavior
- No change to rendered Markdown structure, styling, or semantics
- No redesign of diagnostics, render-policy extraction, or shared tooling CLI infrastructure in
  this feature; those remain follow-up opportunities outside the top three items
- No provider discovery via reflection or runtime plugin loading

## User Experience

This is an internal refactoring feature. From the user's perspective:

- Commands behave the same
- Markdown output remains unchanged
- No new flags or configuration are introduced
- Existing integrations with GitHub and Azure DevOps continue to work the same way

## Scope

### In Scope

- Introduce explicit pipeline-stage abstractions for report model generation
- Move `ReportModelBuilder` toward a coordination role that composes those stages
- Replace direct provider registration fan-out with a narrower provider contribution model
- Update application composition to consume the new provider contribution model
- Replace static mutable Azure role definition state with an instance-based resolver service
- Update tests to validate the new boundaries without changing behavior
- Add feature documentation for specification, architecture, and implementation tasks

### Out of Scope

- Refactoring diagnostics into a sink/formatter split
- Splitting rendering policy from markdown emission
- Consolidating CLI infrastructure across tool projects
- Reworking unrelated provider-specific model factories unless needed to support the new
  boundaries

## Functional Requirements

### FR-1: Explicit report-generation stages

The report-generation flow must be represented as explicit stages with well-defined inputs and
outputs rather than as a single monolithic builder implementation.

### FR-2: Preserved output behavior

The refactoring must preserve current report content and Markdown output behavior.

### FR-3: Narrower provider integration boundary

Provider-specific behavior must be contributed through a narrower architectural contract than the
 current broad `IProviderModule` surface.

### FR-4: Instance-based role resolution

Azure role definition resolution must operate through an instance-based service whose behavior is
scoped to the current application run.

### FR-5: Composition-root compatibility

The application must still be composed using explicit, AOT-safe, Pure DI-friendly registration.

## Quality Requirements

### Maintainability

- Core orchestration classes should have fewer cross-cutting responsibilities
- New provider capabilities should require fewer coordinated architectural edits
- Hidden shared state must be removed from role-resolution behavior

### Testability

- Pipeline stages should be testable independently
- Provider contributions should be testable without requiring full application composition
- Role resolution should be testable without global state reset requirements

### Safety

- The migration must be incremental and behavior-preserving
- Existing output compatibility should be protected by current tests and snapshots

## Success Criteria

- [x] `ReportModelBuilder` no longer contains the full report-generation pipeline as one implicit
  mutable workflow
- [x] The architecture exposes named pipeline stages with explicit sequencing
- [x] Provider-specific contributions are supplied through a narrower contract than the current
      broad module interface
- [x] `CompositionRoot` no longer needs to manually understand as many provider-specific registry
      capability types
- [x] `AzureRoleDefinitionMapper` no longer uses static mutable fields for custom roles or
  diagnostics
- [x] Existing rendered output remains unchanged according to current verification tests
- [x] Core tests covering report generation, providers, and role resolution continue to pass

## Implementation Status

### Completed

- Task 1: Azure role definition resolution now uses a run-scoped resolver service instead of
  static mutable state.
- Task 2: `ReportModelBuilder` now coordinates explicit stages for resource-change construction,
  attribute filtering, summary enrichment, display filtering, and final report assembly.
- Task 3: Providers now declare narrow capability interfaces, `ProviderRegistry` builds a
  centralized `ProviderContributionSet`, and `CompositionRoot` consumes provider contributions
  through that central object instead of capability-specific fan-out methods.
- Task 4: Legacy migration scaffolding has been removed, including the static
  `AzureRoleDefinitionMapper` compatibility wrapper, the old `IProviderModule` path, and the
  now-unneeded `ProviderRegistry` suppression.
- Task 5: Final verification completed with a full `scripts/test-with-timeout.sh -- dotnet test
  --solution src/tfplan2md.slnx` run passing at `1161` succeeded, `0` failed, `0` skipped.
- No snapshot baseline updates were required because the refactoring preserved rendered Markdown
  output.
- `docs/features.md` intentionally remains unchanged because it is a user-facing feature index and
  this work is internal-only.

## Constraints

- Preserve NativeAOT compatibility
- Preserve Pure DI and explicit registration patterns
- Avoid introducing new external dependencies
- Avoid large-scale rewrites that make behavior regression hard to isolate

## Resolved Decisions

1. The new provider contribution model fully replaced `IProviderModule`; no adapter layer remains.
2. `ReportModelBuilder` remains as the façade name while delegating the extracted pipeline stages.
3. The feature remains internal-only, so the user-facing feature index in `docs/features.md` does
  not need an entry.

## Related Documents

- `docs/features/110-refactoring-opportunities/refactoring-opportunities.md`
- `docs/features/110-refactoring-opportunities/architecture.md`
- `docs/features/110-refactoring-opportunities/tasks.md`
