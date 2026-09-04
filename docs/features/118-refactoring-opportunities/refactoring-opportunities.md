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

## Appendix: NativeAOT Binary Size Analysis

This section analyzes the binaries generated by `tfplan2md` with emphasis on the shipped
NativeAOT Linux build.

### Scope and methodology

The analysis used the current `main` branch publish configuration from
`src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj` and the release workflow publish model from
`.github/workflows/release.yml`.

Artifacts were collected from these publishes:

- `linux-x64` local NativeAOT publish
- `linux-musl-x64` NativeAOT publish using the same Alpine SDK container approach as the release
  workflow

The following analysis sources were used:

- NativeAOT compiler diagnostics via `IlcGenerateMstatFile=true`
- NativeAOT compiler DGML graphs via `IlcGenerateDgmlFile=true`
- NativeAOT linker map via `tfplan2md.map.xml` from the `linux-musl-x64` build
- ILLink/trim analysis via successful `dotnet publish` under `PublishAot=true` and `TrimMode=full`
- Binlogs for both publish runs

Requested tools status:

- `sizoscope` was installed locally, but the packaged CLI on this machine expects Wine and could
  not be used directly.
- `sizoscopeX` installed, but failed at startup due an incompatible native `SkiaSharp` library in
  the local environment.
- `dotnet-size` and a standalone `Linker Analyzer` executable were not available as local tool
  packages in this environment.

Because of those tool constraints, the final attribution in this report is based primarily on the
generated NativeAOT linker map, DGML files, and successful trim/AOT publishes rather than the UI
viewers.

### Binary outputs

Measured publish outputs:

| RID | File | Size |
| --- | --- | ---: |
| `linux-x64` | `tfplan2md` | 6,254,568 bytes |
| `linux-x64` | `tfplan2md.dbg` | 16,258,192 bytes |
| `linux-musl-x64` | `tfplan2md` | 6,262,448 bytes |
| `linux-musl-x64` | `tfplan2md.dbg` | 13,485,896 bytes |

Important observation:

- The shipped `linux-musl-x64` binary is effectively the same size as the local `linux-x64`
  binary. The size discussion below therefore uses the `linux-musl-x64` linker map because it is
  the release-representative build and provides the most useful attribution data.

### Final binary attribution

The `linux-musl-x64` NativeAOT linker map reports a total attributed size of `6,038,312` bytes for
the final executable.

High-level split:

| Area | Bytes | Share of final binary |
| --- | ---: | ---: |
| NativeAOT runtime data | 2,499,790 | 41.40% |
| `System.Private.CoreLib` | 1,374,694 | 22.77% |
| `tfplan2md` application code/data | 1,134,731 | 18.79% |
| Other framework/native code | 434,829 | 7.20% |
| `System.Text.RegularExpressions` | 311,255 | 5.15% |
| `System.Text.Json` | 242,421 | 4.01% |
| `System.Console` | 40,592 | 0.67% |

Key conclusion:

- Roughly `81%` of the final binary is not `tfplan2md` namespace code. Most of the size is coming
  from NativeAOT runtime scaffolding plus framework subsystems that the app roots.
- This means binary-size work should focus first on avoiding framework features that bring in large
  subsystems, not only on shrinking `Oocx.TfPlan2Md` source code.

### Largest NativeAOT/runtime buckets

Largest runtime-generated nodes from the linker map:

| Node | Bytes | Share |
| --- | ---: | ---: |
| `__dehydrated_data` | 763,071 | 12.64% |
| `__embedded_metadata` | 335,713 | 5.56% |
| `__FrozenSegmentStart` | 242,000 | 4.01% |
| `_stacktrace_methodRVA_to_token_mapping` | 118,273 | 1.96% |
| `__InterfaceDispatchCellSection_Start` | 61,632 | 1.02% |
| `__generic_types_hashtable` | 45,174 | 0.75% |
| `__external_NativeReferences_references` | 41,136 | 0.68% |
| `__generic_methods_hashtable` | 40,417 | 0.67% |

Interpretation:

- `__dehydrated_data` and `__embedded_metadata` indicate that a meaningful amount of the final size
  is structural NativeAOT data rather than user code.
- `__FrozenSegmentStart` is notable because the codebase uses `FrozenDictionary` in several mapping
  and lookup components. That may be a favorable runtime tradeoff, but it is also a size signal for
  a short-lived CLI.
- Even with `StackTraceSupport=false` and `IlcGenerateStackTraceData=false`, a non-trivial stack
  trace mapping blob remains. This appears to be partially unavoidable runtime support, so it is a
  low-confidence optimization target.

### `tfplan2md` feature attribution

Aggregating linker-map symbols by project namespace gives this approximate breakdown of
`tfplan2md`-owned size:

| Feature area | Bytes | Share of final binary | Share of app-owned size |
| --- | ---: | ---: | ---: |
| Markdown generation | 396,447 | 6.57% | 34.94% |
| AzureRM provider | 184,129 | 3.05% | 16.23% |
| AzAPI provider | 157,144 | 2.60% | 13.85% |
| Parsing | 119,915 | 1.99% | 10.57% |
| Static code analysis | 107,743 | 1.78% | 9.50% |
| Azure platform helpers | 51,425 | 0.85% | 4.53% |
| Azure DevOps provider | 50,265 | 0.83% | 4.43% |
| Diagnostics | 26,294 | 0.44% | 2.32% |
| CLI | 19,894 | 0.33% | 1.75% |
| Azure AD provider | 17,561 | 0.29% | 1.55% |

Most important internal findings:

- `MarkdownGeneration` is the single biggest `tfplan2md` feature bucket by a wide margin.
- Provider-specific functionality is not trivial in size. `AzureRM` + `AzAPI` alone account for
  `341,273` bytes, which is `5.65%` of the final binary and about `30%` of `tfplan2md`-owned size.
- Static code analysis support is materially visible in the binary at `107,743` bytes before also
  considering the framework features it pulls in.
- CLI size is small. There is no evidence that basic console handling is a major optimization
  target.

### Large internal symbols worth understanding

Largest `tfplan2md` symbols in the final `linux-musl-x64` binary include:

- `ResourceSummaryMappings..cctor` in markdown summary generation: `6,112` bytes
- `TfPlanJsonContext` property initializers in source-generated JSON metadata: multiple entries in
  the `2,000` to `3,600` byte range
- `CliParser.Parse`: `3,010` bytes
- `AzApiBodyRenderer.RenderUpdateBody`: `2,875` bytes
- `ReportModelBuilder.Build`: `2,497` bytes
- `SarifResultReader.ParseLocations`: `2,309` bytes
- `AzureRMModule.RegisterParentChildRelationships`: `2,305` bytes
- `DefaultResourceRenderer.RenderCodeAnalysisMetadata`: `2,118` bytes

This reinforces the broader grouping above:

- source-generated JSON metadata is a visible contributor
- markdown summary/rendering paths dominate many of the largest app symbols
- AzAPI and code-analysis rendering/parsing are meaningful, not incidental

### Framework subsystems currently rooted by features

#### `System.Text.RegularExpressions`: 311,255 bytes

This is large enough to matter. Current regex usage includes:

- markdown cleanup and formatting in `MarkdownWriter`
- icon rule and pattern matching infrastructure in `MatchPattern`
- provider filters such as AzureRM and AzAPI resource ID case-change filtering
- semantic identity formatting helpers
- Azure AD summary rewriting

Not all regex usage is equal:

- Some usage is configuration-driven and may justify keeping regex support.
- Several call sites are simple enough that manual scanning or direct string operations are likely
  sufficient.

This framework bucket is one of the clearest optimization candidates.

#### `System.Text.Json`: 242,421 bytes

This is expected because the application is fundamentally driven by Terraform plan JSON, but the
codebase also uses JSON heavily in secondary paths:

- Terraform plan parsing and source-generated serializer contexts
- SARIF parsing for static analysis integration
- repeated `JsonDocument.Parse` and `JsonElement` manipulation in rendering and formatting helpers
- embedded JSON mapping/icon payload loading

This subsystem is necessary, but there is still room to reduce secondary JSON use in formatting
paths and helpers.

#### Embedded JSON payloads are not a primary driver

Embedded JSON payload source sizes:

| Resource | Size |
| --- | ---: |
| `AzureRoleDefinitions.json` | 36,205 bytes |
| `AzureApiDocumentationMappings.json` | 12,165 bytes |
| Shared/AzureAD/AzureDevOps icon JSON combined | 4,138 bytes |

Total source payload size for these embedded assets is `52,508` bytes.

Conclusion:

- These files matter, but they are not where the first meaningful binary wins will come from.
- Optimizing code paths and framework pulls will yield larger gains than trimming these data files.

### `System.Xml` follow-up

The current `linux-musl-x64` linker map contains `0` `System.Xml`/`XDocument`/`XmlDocument` nodes.

That confirms the project has already realized the benefit of removing the heavier XML dependency
path. The current custom formatter approach is consistent with the measured size profile: avoiding
entire framework subsystems is materially more valuable than micro-optimizing small internal types.

### Recommendations

#### 1. Prioritize removal of avoidable regex usage

Reason:

- `System.Text.RegularExpressions` contributes `311,255` bytes or `5.15%` of the final binary.

Best candidates:

- `MarkdownWriter` post-processing regexes
- simple provider ID matching in AzureRM/AzAPI filters
- Azure AD summary rewrite logic
- CIDR and similar simple identity-format checks that can use direct parsing or scanning

Expected outcome:

- This is one of the few framework buckets large enough to plausibly save a noticeable fraction of
  a megabyte if usage is simplified enough.

#### 2. Revisit `FrozenDictionary` usage for a short-lived CLI

Reason:

- The `__FrozenSegmentStart` runtime bucket is `242,000` bytes.
- The codebase uses `FrozenDictionary` broadly in Azure mapping and provider lookup infrastructure.

Recommendation:

- Benchmark a few high-value mappings with ordinary `Dictionary<string, string>` or
  `IReadOnlyDictionary<string, string>`.
- Prefer the smaller representation unless the startup/runtime benefit is clearly measurable in real
  workloads.

Expected outcome:

- Moderate size reduction is plausible, especially when frozen objects are not buying much for a
  command-line tool that runs once per invocation.

#### 3. Keep secondary JSON parsing on a tight leash

Reason:

- `System.Text.Json` is unavoidable, but secondary use outside the core Terraform parser adds both
  framework and app-side size.

Best candidates:

- helper paths that parse JSON strings only to format them again
- rendering code that repeatedly materializes `JsonDocument`
- non-critical JSON manipulation in markdown helpers

Expected outcome:

- Smaller wins than regex elimination, but likely easier to do incrementally without architectural
  changes.

#### 4. Consider making static code analysis support optional if size is a product goal

Reason:

- The code-analysis subsystem contributes `107,743` bytes of app-owned size before its indirect
  framework cost.

Options:

- compile-time feature flag
- separate binary flavor
- separate tool/subcommand if the UX permits it

Expected outcome:

- This is one of the cleanest optional feature cuts if a smaller "core" distribution ever becomes
  more important than an all-in-one executable.

#### 5. Consider provider modularization only if you want a smaller product profile

Reason:

- Provider code is a real size contributor, but it is application value, not accidental bloat.
- `AzureRM` + `AzAPI` + `Azure DevOps` + `Azure AD` together account for a substantial part of
  app-owned code.

Recommendation:

- Do not modularize providers only for cleanliness.
- Do consider provider packs or build profiles if you want a "minimal Terraform core" binary.

Expected outcome:

- Meaningful savings are available here, but they come with product and maintenance tradeoffs.

#### 6. Do not spend early effort on embedded JSON payload trimming

Reason:

- The embedded payload files total about `52 KB` in source form and are much smaller than the large
  framework/runtime buckets.

Expected outcome:

- This is a low-return optimization area unless the files grow substantially.

### Practical next experiments

If the goal is to save megabytes rather than kilobytes, the most defensible next experiments are:

1. Replace the simple regex-based call sites with non-regex implementations and measure the binary
   delta.
2. Swap selected `FrozenDictionary` usage to `Dictionary` in one or two representative areas and
   measure the delta.
3. Disable or split static code analysis support in a test build and measure the delta.
4. If still needed, prototype a reduced-feature build without one provider family to establish the
   upper bound of provider modularization savings.

### Summary

The current shipped NativeAOT Linux binary is about `6.26 MB` stripped. The main size story is not
that `tfplan2md` itself is huge; it is that NativeAOT runtime structures and rooted framework
subsystems dominate the binary.

The most promising optimization themes are therefore:

- avoid pulling large framework subsystems for simple tasks
- treat regex as a measurable cost center
- question startup-optimized frozen data structures in a one-shot CLI
- make optional features truly optional if binary size becomes a first-class product requirement

The recent removal of `System.Xml` was exactly the right kind of optimization. The current profile
shows that similar subsystem-level decisions are where the largest future wins are likely to come
from.