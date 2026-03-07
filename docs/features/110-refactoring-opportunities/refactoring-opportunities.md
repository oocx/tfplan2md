# Refactoring Opportunities Report

## Overview

This document captures a targeted architecture and design review of the current `tfplan2md`
codebase. The goal is not to propose cosmetic cleanup, but to identify structural changes that
would materially improve maintainability, testability, and design clarity.

The review focused on the main application pipeline, provider extensibility model, rendering
pipeline, diagnostics, and repeated infrastructure across the tool projects.

## Review Scope

The review examined the current mainline implementation, with emphasis on these areas:

- Application composition and workflow orchestration
- Report model building and provider-specific pipeline hooks
- Rendering architecture and provider-specific rendering logic
- Azure mapping and role-resolution infrastructure
- Diagnostics collection and formatting
- Shared tooling patterns across CLI projects

## Findings

### 1. ReportModelBuilder is acting as a hidden pipeline, not a focused builder

**Relevant files:**

- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.CodeAnalysis.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

**What is happening now**

`ReportModelBuilder` has accumulated multiple responsibilities that go beyond model assembly.
It builds resource changes, filters and transforms attribute changes, performs parent-child
merging, initializes provider-contributed callbacks, updates summaries, and integrates code
analysis findings.

The class is split into multiple `partial` files, but that only distributes the implementation.
It does not reduce responsibility or dependency breadth. Important lifecycle behavior is spread
across methods like callback registration and parent-child merge processing, which means the
execution order of internal steps becomes a correctness requirement.

**Why this is a structural problem**

- The class is conceptually a processing pipeline, but it is presented as a single builder.
- `partial` files reduce file size, but they hide how many phases and cross-cutting concerns the
  type owns.
- Provider behavior plugs into the middle of the build process through callbacks, which makes the
  internal sequence harder to reason about and harder to test in isolation.
- The repeated complexity and coupling suppressions indicate that the architectural boundaries are
  not holding.

**Impact**

- New features tend to be added to the builder because it already knows everything.
- Regression risk rises because small changes can alter phase ordering or shared mutable state.
- Unit tests need to exercise broad slices of behavior instead of narrow, deterministic stages.
- Maintenance cost is driven by implicit flow rather than explicit pipeline design.

**Recommended refactoring direction**

Split the builder into explicit pipeline stages with narrow inputs and outputs. A practical split
would be:

1. Resource extraction and normalization
2. Attribute filtering and transformation
3. Parent-child consolidation
4. Summary enrichment
5. Code-analysis enrichment
6. Final report assembly

Each stage should expose a focused contract and should operate on immutable or append-only data
where practical. Provider-specific hooks should attach to explicit pipeline stages instead of
mutating the builder itself.

**Expected benefits**

- Lower class coupling and lower method complexity
- Clearer lifecycle semantics
- Better test isolation
- Easier onboarding for contributors working on report generation

### 2. The provider extension model is too broad and forces invasive changes

**Relevant files:**

- `src/Oocx.TfPlan2Md/Providers/IProviderModule.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ProviderRegistry.cs`
- `src/Oocx.TfPlan2Md/CompositionRoot.cs`

**What is happening now**

The provider abstraction is responsible for many unrelated capability areas: factories, value
formatters, icon providers, parent-child relationships, post-merge callbacks, resource renderers,
and attribute change filters.

The registry mirrors this breadth with one fan-out method per capability. The composition root
then has to know which downstream components need which provider-derived registries.

**Why this is a structural problem**

- The abstraction boundary is too wide, so it becomes costly to add new extension points.
- Each new provider capability requires coordinated changes across the provider interface, the
  registry, the composition root, and often the renderer or model builder.
- The design optimizes for explicit registration, but not for capability cohesion.

**Impact**

- Architectural changes are more invasive than they should be.
- Provider modules become aggregation points rather than cohesive modules.
- The composition root keeps growing because it must understand all provider-facing concerns.

**Recommended refactoring direction**

Replace the single broad provider contract with one of these two approaches:

1. Capability-specific interfaces such as `IRegistersRenderers`, `IRegistersValueFormatters`,
   `IRegistersRelationships`, and similar.
2. A single `ProviderContribution` object returned by each provider module, where all optional
   capabilities are exposed as data or delegates and consumed in one place.

The second option is likely the cleaner fit if the project wants to preserve explicit, AOT-safe
registration while reducing interface churn.

**Expected benefits**

- Narrower architectural surface area
- Easier provider evolution
- Less duplication in the registry and composition root
- Lower cost for adding new provider extension points

### 3. AzureRoleDefinitionMapper relies on hidden process-wide mutable state

**Relevant files:**

- `src/Oocx.TfPlan2Md/Platforms/Azure/AzureRoleDefinitionMapper.cs`
- `src/Oocx.TfPlan2Md/CompositionRoot.cs`

**What is happening now**

`AzureRoleDefinitionMapper` is a static type that stores custom role mappings and diagnostic
context in static mutable fields. Those values are updated during service composition before the
rest of the application runs.

This means role resolution behavior is affected by prior initialization in the current process.
The effective runtime state of the mapper is not visible from the consuming call sites.

**Why this is a structural problem**

- The mapper is not a pure lookup service; it has hidden runtime configuration.
- Service composition mutates global state as a side effect.
- This creates cross-run coupling within the same process.
- Diagnostic routing is also global, not scoped to a specific report generation.

**Impact**

- Parallel tests or future in-process multi-run scenarios can interfere with each other.
- Debugging becomes harder because the mapper's behavior depends on prior setup.
- The architecture resists reuse in long-lived hosts or library scenarios.

**Recommended refactoring direction**

Replace the static mutable mapper with an instance-based role definition resolver that is created
from the loaded mapping file. The built-in role registry can remain static and immutable, but the
custom role dictionary and diagnostics should be instance state.

For example:

- `BuiltInRoleDefinitionRegistry` stays static and immutable
- `RoleDefinitionResolver` is an injected service per application run
- Diagnostics are passed to the resolver instance or to a dedicated diagnostic sink

**Expected benefits**

- No hidden shared state
- Safer tests and future concurrency
- Clearer dependency injection boundaries
- Better alignment between application composition and runtime behavior

### 4. DiagnosticContext mixes event collection, mutable shared state, and markdown presentation

**Relevant files:**

- `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/AzureMappingFileLoader.cs`
- `src/Oocx.TfPlan2Md/Platforms/Azure/PrincipalMapper.cs`

**What is happening now**

`DiagnosticContext` is a mutable object shared across components. Those components append directly
to collections such as failed resolutions and template resolutions. The same type then renders the
final markdown debug section.

This couples three concerns into one object:

- collecting diagnostic events
- storing mutable diagnostic state
- formatting diagnostics for markdown output

**Why this is a structural problem**

- Any component that wants to emit diagnostics needs to understand the mutable structure.
- The debug representation is locked to markdown-oriented formatting.
- The design makes it harder to support structured logging, JSON export, or future telemetry.
- The type becomes a shared mutable dependency instead of a clean sink boundary.

**Impact**

- Cross-cutting diagnostic behavior leaks into unrelated components.
- The system is harder to evolve toward richer diagnostics.
- The code encourages direct list mutation from multiple areas rather than typed event emission.

**Recommended refactoring direction**

Split diagnostics into separate concepts:

1. A typed diagnostic sink interface such as `IDiagnosticSink`
2. Immutable or append-only diagnostic records/events
3. A dedicated formatter such as `DiagnosticMarkdownRenderer`

Components should emit typed diagnostic events rather than manipulating shared collections.
At the end of the workflow, a formatter can render the collected snapshot into markdown.

**Expected benefits**

- Cleaner cross-cutting boundaries
- Easier support for alternate output formats
- More explicit diagnostic semantics
- Better separation between instrumentation and presentation

### 5. Rendering classes mix domain policy, compatibility rules, and markdown layout mechanics

**Relevant files:**

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
- `src/Oocx.TfPlan2Md/Providers/AzApi/Helpers/AzApiBodyRenderer.cs`

**What is happening now**

The rendering layer contains substantial policy logic that goes beyond formatting. In the default
renderer, the main `Render` method decides scenario-specific formatting behavior, known-after-apply
compatibility behavior, details block structure, empty-state rules, and output sequencing.

The AzApi body renderer goes further by combining flattening, change comparison, grouping,
sensitivity handling, and markdown table generation in a single helper-oriented rendering path.

**Why this is a structural problem**

- Domain rules and formatting rules are interleaved.
- Compatibility quirks become embedded in rendering code instead of living in named policies.
- Renderers become the default place to absorb every edge case, which pushes them toward the same
  complexity problems seen elsewhere.
- Snapshot compatibility becomes harder to preserve because the logic is not decomposed into
  independently testable policy units.

**Impact**

- Rendering changes are high-risk and expensive to review.
- It is difficult to tell whether a change affects data preparation, policy, or layout.
- Specialized providers end up with dense renderer/helper classes that are difficult to reuse.

**Recommended refactoring direction**

Introduce a stronger separation between:

1. Data preparation
2. Render policy selection
3. Markdown emission

For example:

- Extract scenario-specific render policies into named strategy objects
- Compute render-ready view models before emitting markdown
- Keep markdown writers focused on layout rather than transformation logic

For AzApi specifically, split compare/group/sensitivity decisions from final markdown table
rendering so the path can be tested as data transformations first.

**Expected benefits**

- Lower renderer complexity
- Better snapshot stability through narrower changes
- More reusable rendering logic
- Easier testing of policy changes without full markdown diffs

### 6. Tooling projects duplicate CLI infrastructure that should be shared

**Relevant files:**

- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/CLI/CliParser.cs`
- `src/tools/Oocx.TfPlan2Md.HtmlRenderer/CLI/HelpTextProvider.cs`
- `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/CLI/CliParser.cs`
- `src/tools/Oocx.TfPlan2Md.ScreenshotGenerator/CLI/HelpTextProvider.cs`
- `src/tools/Oocx.TfPlan2Md.TerraformShowRenderer/CLI/CliParser.cs`
- `src/tools/Oocx.TfPlan2Md.TerraformShowRenderer/CLI/HelpTextProvider.cs`

**What is happening now**

The supporting tools use parallel, mostly independent CLI parsing and help-text infrastructure.
The patterns are similar enough that they are clearly part of the same internal design, but they
are implemented separately.

**Why this is a structural problem**

- Common behavior can drift across tools.
- Error wording and option conventions may become inconsistent over time.
- Bug fixes in argument parsing must be applied in multiple places.
- The codebase pays a repeated maintenance cost for infrastructure that is not domain-specific.

**Impact**

- Lower consistency across internal tools
- More repetitive code review and testing effort
- More places to update when introducing shared CLI behavior

**Recommended refactoring direction**

Extract a lightweight shared CLI utility layer for internal tools. That shared layer does not need
to be a full framework. It only needs to centralize the recurring concerns:

- argument scanning
- common option/value reading
- help/version handling patterns
- reusable validation helpers
- consistent parse exception messages

This should stay intentionally small. The goal is consistency and reuse, not abstraction for its
own sake.

**Expected benefits**

- Less duplication
- More consistent tool behavior
- Lower maintenance cost for shared CLI patterns
- Simpler future tool creation

## Prioritization

### Highest-value refactorings

These items are likely to return the most architectural benefit relative to effort:

1. Refactor `ReportModelBuilder` into explicit stages
2. Remove static mutable state from `AzureRoleDefinitionMapper`
3. Narrow the provider extension surface

These changes would reduce the most cross-cutting complexity and create clearer boundaries for
future work.

### Medium-value refactorings

These are important, but are best done after the main orchestration boundaries are improved:

1. Separate diagnostic collection from diagnostic rendering
2. Decompose render-policy logic from markdown emission

### Opportunistic refactoring

This can be done incrementally with low risk:

1. Share CLI infrastructure across tool projects

## Recommended Implementation Sequence

1. Introduce an instance-based Azure role definition resolver and remove static mutable mapper
   state.
2. Extract a report-generation pipeline abstraction from `ReportModelBuilder` without changing the
   rendered output.
3. Redesign provider contributions around narrower capabilities or a single contribution object.
4. Move diagnostics behind a typed sink plus separate markdown formatter.
5. Decompose renderer policy logic into named strategy objects.
6. Consolidate CLI utilities across tooling projects.

This order reduces architectural coupling first, then addresses the layers built on top of that
coupling.

## Conclusion

The codebase has a solid amount of functionality, but several central abstractions have become
too broad. The main theme across the findings is that orchestration, extension, and presentation
concerns are carrying more responsibility than their names suggest.

The recommended refactorings are aimed at making execution phases explicit, reducing hidden shared
state, and replacing broad extension surfaces with narrower and more coherent contracts.