# Architecture: Core Report Pipeline and Provider Refactoring

## Executive Summary

This document describes the target architecture for the top three refactorings selected from the
refactoring review:

1. Converting `ReportModelBuilder` from a hidden pipeline into explicit report-generation stages
2. Replacing the broad provider module contract with a narrower provider contribution model
3. Replacing static mutable Azure role definition state with an instance-based resolver

The design goal is to improve clarity and maintainability while preserving current behavior,
output, and compatibility constraints.

## Current Problems

### 1. Hidden pipeline inside `ReportModelBuilder`

The current `ReportModelBuilder` owns multiple phases of report generation, including resource
change construction, filtering, parent-child merging, summary enrichment, and code-analysis
integration. These phases are not modeled explicitly, so execution order is encoded in the class
implementation rather than in the design.

### 2. Broad provider interface and repeated registration fan-out

Providers contribute multiple unrelated capabilities through one wide module interface. This makes
the provider abstraction costly to evolve and forces the registry and composition root to mirror
every capability explicitly.

### 3. Hidden mutable state in Azure role definition resolution

Custom role mappings and diagnostics are stored in static mutable fields, creating invisible
runtime coupling across the process.

## Architectural Principles

- Preserve current behavior first; structural cleanup must not change output
- Make execution phases explicit
- Keep dependencies directional and narrow
- Prefer immutable or run-scoped state over shared mutable state
- Preserve explicit, AOT-safe composition with Pure DI

## Target Architecture Overview

The implemented refactoring introduces three key structural concepts:

1. A report-generation pipeline coordinated through explicit stages
2. A centralized provider contribution set built from narrow provider capability interfaces
3. A run-scoped `IRoleDefinitionResolver` service

## Report Generation Pipeline

### Implemented stage model

The report-generation flow is now coordinated as a sequence of explicit stage abstractions:

1. `IResourceChangeStage`
2. `IAttributeFilteringStage`
3. `ISummaryEnrichmentStage`
4. `IDisplayFilteringStage`
5. `IReportAssemblyStage`

`ReportModelBuilder` remains the orchestration boundary for the still-shared parent-child merge
step, output-model construction, and code-analysis integration. The important architectural change
is that resource construction, attribute filtering, summary calculation, display filtering, and
final report assembly are no longer implemented inline as one hidden workflow.

### Responsibilities

#### Resource change stage

- Convert parsed Terraform plan structures into initial `ResourceChangeModel` instances
- Apply provider-specific view model factories where appropriate

#### Attribute filtering stage

- Apply attribute filters such as Azure ID case-change suppression
- Produce the filtered resource/change set used downstream

#### Summary enrichment stage

- Compute or update report summary data
- Produce summary-ready aggregates without leaking merge-stage behavior backward

#### Display filtering stage

- Apply post-merge display filtering and module-address normalization
- Remove visually suppressed resources without changing Terraform change counts
- Return filtered-resource counts used in the final report

#### Report assembly stage

- Build the final `ReportModel`
- Gather filtered-resource counts and final metadata in one place

#### Builder-owned coordination steps

- Build the configuration reference index used by parent-child fallback matching
- Merge parent-child relationships after pre-merge summary calculation
- Run provider-contributed post-merge callbacks
- Build output models before final assembly
- Map code-analysis results into the final report input

### Coordinator role

`ReportModelBuilder` may remain as a compatibility façade, but its responsibility should shift to
coordinating the explicit stages rather than directly implementing all behavior.

That gives the codebase a migration path with minimal public churn while still making the design
clear.

## Provider Contribution Model

### Current problem

Provider capabilities are spread across multiple registration methods on one broad interface.

### Implemented design

Each provider now implements a narrow core `IProvider` contract plus optional capability
interfaces such as:

- `IValueFormatterProvider`
- `IIconRegistrationProvider`
- `IParentChildRelationshipProvider`
- `IAttributeChangeFilterProvider`
- `IPostMergeCallbackProvider`
- `IResourceRendererProvider`

`ProviderRegistry` stores only explicitly registered `IProvider` instances. `ProviderContributionSet`
then aggregates those providers in one place and materializes the capability-specific registries
needed by `CompositionRoot`, `ReportModelBuilder`, and `MarkdownRenderer`.

### Benefits

- Adding a new capability no longer requires widening one monolithic provider-module interface
- Provider modules become cohesive declarations of which optional capabilities they support
- Composition code consumes one centralized contribution set instead of manually rebuilding each
  provider capability path

## Role Definition Resolution

### Implemented service boundary

Introduce an instance-based role definition resolver, such as:

```csharp
internal interface IRoleDefinitionResolver
{
    RoleDefinitionInfo GetRoleDefinition(string? roleDefinitionId, string? roleDefinitionName, string? resourceAddress = null);
}
```

The concrete implementation is created during application composition using:

- built-in immutable role definitions
- run-scoped custom role mappings from the mapping file
- run-scoped diagnostic sink or diagnostic context

### Design notes

- Built-in role data can remain static and immutable
- Custom mappings must be instance state
- Diagnostics must be scoped to the resolver instance or to a dedicated sink interface

## Composition Root Impact

### Implemented state

`CompositionRoot` now:

1. Builds run-scoped infrastructure, including `IRoleDefinitionResolver`
2. Registers providers explicitly in `ProviderRegistry`
3. Builds a `ProviderContributionSet` once per run
4. Creates provider-derived registries from that centralized contribution set
5. Passes the run-scoped role definition resolver and provider contribution set into downstream services

This keeps composition explicit while reducing orchestration knowledge in the root.

## Migration Outcome

The migration was completed incrementally:

### Phase 1: Role resolver

- Introduced `IRoleDefinitionResolver`
- Migrated AzureRM consumers to the resolver
- Removed the static mutable compatibility path from `AzureRoleDefinitionMapper`

### Phase 2: Pipeline extraction

- Introduced explicit stage abstractions behind `ReportModelBuilder`
- Moved resource construction, attribute filtering, summary enrichment, display filtering, and final report assembly into dedicated stage components
- Kept `ReportModelBuilder` as the external façade and orchestration boundary

### Phase 3: Provider contribution redesign

- Replaced `IProviderModule` with narrow capability interfaces on provider modules
- Updated `ProviderRegistry` and `CompositionRoot` to consume a centralized `ProviderContributionSet`
- Removed the legacy fan-out registration path

## Verification Strategy

Because behavior must stay stable, verification should rely on existing tests and snapshots plus
targeted new tests for the new boundaries.

### Recommended verification focus

- Snapshot and rendering parity after pipeline-stage extraction
- Provider contribution registration tests
- Role definition resolution tests that prove independence from shared static state

## Risks

### Risk 1: Staged extraction without real simplification

If the implementation only wraps existing builder logic in thin stage objects, the code will gain
indirection without gaining clarity.

**Mitigation:** Each stage must own a real responsibility boundary and reduce the builder's direct
knowledge.

### Risk 2: Provider migration churn

Changing provider integration can touch many provider modules.

**Mitigation:** Use adapters temporarily and migrate providers incrementally.

### Risk 3: Snapshot regressions from lifecycle changes

Even refactor-only changes can alter ordering or formatting indirectly.

**Mitigation:** Keep the migration small-step and run targeted verification after each phase.

## Conclusion

The target architecture keeps the current behavior but replaces implicit orchestration and hidden
state with explicit stages, explicit contributions, and run-scoped services. That is the minimum
structural change needed to reduce future maintenance cost in the report-generation core.
