# Feature: Code Simplification Refactoring

## Overview

This feature addresses a set of code review findings that reduce maintainability and increase the risk of inconsistency bugs. The changes eliminate duplicate logic, remove dead code and unused parameters, apply modern C# idioms, and simplify overly complex structures. None of these changes alter any user-visible output or behaviour.

## User Goals

- **Maintainers** can modify shared formatting logic in a single place without risking drift between copies.
- **Maintainers** adding a new AzDO entity type (mapper + formatter) need to write only the entity-specific parts, not an entire near-identical class.
- **Maintainers** reading constructor signatures can trust that every parameter is actually used.
- **Reviewers** can understand each class's purpose without deciphering why parameters are suppressed or discarded.
- **New contributors** can follow consistent patterns (positional records, `internal` access, default parameters) throughout the codebase.

## Scope

### In Scope

All items are internal refactors with no change to user-visible output, CLI options, or API contracts.

#### 1. Duplicate Code

**1.1 — `FormatBreakdown` / `FormatSummaryBreakdown`**
- `SummaryRenderer.FormatBreakdown` (private static) and `ReportRenderer.FormatSummaryBreakdown` (private static) have identical method bodies.
- Consolidate into a single shared helper (e.g., move to `SummaryRenderer` or a new shared static helper) and update `ReportRenderer` to call it.

**1.2 — Four near-identical AzDO Mapper classes**
- `AzdoGroupMapper`, `AzdoUserMapper`, `AzdoProjectMapper`, `AzdoRepositoryMapper` share approximately 300 lines of duplicated structure: a `FrozenDictionary<string,string>` of mappings, an optional `IDiagnosticSink?`, two `GetName` overloads, a `GetEntityName` display-format method, and the `RecordFailedResolution` pattern.
- The only differences are the `FailedResolutionType` enum value used in diagnostics and the display format produced by `GetEntityName`.
- Extract an abstract base class or a generic helper parameterised by entity type so the common structure is defined once.

**1.3 — Four near-identical AzDO ID Formatter classes**
- `AzdoGroupDescriptorFormatter`, `AzdoUserIdFormatter`, `AzdoProjectIdFormatter`, `AzdoRepositoryIdFormatter` each implement `IValueFormatter` with identical structure: inject mapper, guard empty value, call `GetName`, guard raw == display, format with icon + `MarkdownHelpers.FormatCodeTable`.
- The only differences are the injected mapper type and the icon emoji.
- Extract a shared static helper `Format(string? rawValue, Func<string, string?> getName, string icon)` and reduce each class to a thin wrapper.

**1.4 — `IconProviderRegistry` and `ValueFormatterRegistry` near-identical wrappers**
- Both wrap `PatternMatchingRegistry<T>`, expose `Register`, and iterate `ResolveAll` to return the first non-null result.
- Add a `TryResolveFirst` method to `PatternMatchingRegistry<T>` and delegate both registries to it, eliminating the duplicated iteration loop.

**1.5 — `FormatAttributeValuePlain` duplicates `TryFormatSemanticValue` dispatch logic**
- `SemanticFormatting.FormatAttributeValuePlain` manually calls every `TryFormat*` helper then strips backticks from the result. `TryFormatSemanticValue` performs the same dispatch with a context argument.
- Refactor `FormatAttributeValuePlain` to call `TryFormatSemanticValue` and strip any backtick wrapping from the result, removing the duplicated dispatch chain.

**1.6 — `FormatAttributeValue` and `FormatAttributeValueWithResource` trivial wrappers**
- In `SemanticFormatting.Registry.cs` both methods are single-line delegates to `FormatAttributeValueCore`, where one simply passes `null` for `resourceType`.
- Collapse into `FormatAttributeValueCore` with a default parameter `string? resourceType = null`, or inline at call sites, and remove the two wrapper methods.

---

#### 2. Dead Code / Unused Parameters

**2.1 — `ShouldUseMultilineDetailsSummary` always returns `true`**
- `DefaultResourceRenderPolicy.ShouldUseMultilineDetailsSummary` has a body of four `_ = ...` discard statements followed by `return true`. The method is called once.
- Remove the method and replace its single call site in `DefaultResourceRenderPolicy.Resolve` with the literal `true`.

**2.2 — `ResourceViewModelFactoryRegistry` constructor has 2 unused params suppressed with pragma**
- The constructor for `ResourceViewModelFactoryRegistry` accepts `LargeValueFormat largeValueFormat` and `IPrincipalMapper principalMapper` but does nothing with them. Both are suppressed with `#pragma warning disable IDE0060`.
- Remove the two parameters and update the call sites in `CompositionRoot.cs`.

**2.3 — `VariableGroupRenderer` constructor accepts and discards `LargeValueFormat`**
- `VariableGroupRenderer(LargeValueFormat largeValueFormat)` contains `_ = largeValueFormat;` and is kept only for API compatibility.
- Remove the overload; update call sites (including the AzureDevOps module registration) to use the parameterless constructor.

**2.4 — `MarkdownRenderer` primary constructor discards `principalMapper`; secondary constructor is legacy**
- The primary constructor of `MarkdownRenderer` accepts `IPrincipalMapper? principalMapper` and discards it with `_ = principalMapper;`.
- A secondary constructor accepting `customTemplateDirectory` is a legacy compatibility shim.
- Remove `principalMapper` from the primary constructor (update all call sites and tests). Remove the legacy secondary constructor if it has no external callers.

**2.5 — `IResourceViewModelFactory.ApplyViewModel` fat interface with discarded params**
- Multiple factory implementations across `Providers/AzureRM/Models/` discard 2–3 of the 6 `ApplyViewModel` parameters with `_ = principalMapper; _ = iconProviderRegistry;` etc.
- Replace the individual parameters with an `ApplyViewModelContext` record; each implementation unpacks only the fields it needs.

---

#### 3. Overly Complex / Verbose Code

**3.1 — `MatchPattern.CountSpecificity()` and `CalculateDimensionPriority()` iterate the same 4 properties twice**
- `MatchPattern.cs` contains two private methods that each iterate the same four nullable properties (`ProviderPattern`, `ResourceTypePattern`, `AttributeNamePattern`, `ValuePattern`) in identical `if (x is not null)` blocks.
- Merge into a single-pass helper that computes both the specificity count and the dimension priority in one iteration.

**3.2 — `BuildConfigurationReferencesForResource` creates two identical empty dictionaries**
- `ResourceChangeStage.Helpers.cs`: the method has two early-exit paths that each `return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)`.
- Deduplicate by declaring a shared empty-dictionary variable, or use `GetValueOrDefault` with a single shared empty instance.

**3.3 — `ConfigurationReferenceResolver.BuildReferenceIndex` called twice per plan render**
- `ReportModelBuilder.Build.cs` (line ~28) and `ResourceChangeStage.cs` (line ~131) each independently call `ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration)`, parsing the same data twice.
- Pass the pre-built index from `ReportModelBuilder` into `ResourceChangeStage` so it is computed only once.

---

#### 4. Modern C# Patterns

**4.1 — `ServiceResolutionContext` verbose constructor syntax**
- `ServiceResolutionContext` is a `sealed record` but uses an explicit constructor that manually assigns four properties, adding ~35 lines of boilerplate.
- Convert to a positional record (`record(string? ProviderName, string? ResourceType, string? AttributeName, string? Value)`); update all construction call sites.

**4.2 — `SummaryModel` should be a `record`**
- `SummaryModel` is a plain `class` with `required init` properties. It is immutable once constructed and has no behaviour.
- Convert to a `sealed record` with positional parameters (or `required init` properties on a record); update all construction call sites.

**4.3 — `ActionSummary`, `SummaryModel`, `ResourceTypeBreakdown` access modifiers**
- These types are declared `public` in an internal CLI tool. Per project coding standards (_use the most restrictive access modifier that works_), they should be `internal`.
- Change the access modifier to `internal` on all three types and verify that no external callers (tests or other assemblies) break.

---

#### 5. Redundant Class Design

**5.1 — `IResourceViewModelFactoryRegistry` interface omits `TryGetFactory`**
- `ResourceChangeStage` holds a concrete `ResourceViewModelFactoryRegistry` (not the interface) because it needs to call `TryGetFactory`, which is absent from `IResourceViewModelFactoryRegistry`.
- Add `TryGetFactory` to `IResourceViewModelFactoryRegistry` so that `ResourceChangeStage` can depend on the interface rather than the concrete type.

**5.2 — `VariableGroupFactory` and `BuildDefinitionFactory` have no-op `ApplyViewModel`**
- Both factories in `Providers/AzureDevOps/Models/Factories.cs` implement `IResourceViewModelFactory` but provide only a test-only `CreateViewModel` method; `ApplyViewModel` is never overridden and therefore always falls through to the default no-op.
- Either remove the factories (and their registration) if they serve no runtime purpose, or document clearly why they are registered.

**5.3 — `BuildDefinitionRenderer` is a pure pass-through renderer**
- `BuildDefinitionRenderer` extends `AzureDevOpsDelegatingRenderer` with no overrides, making it functionally identical to using the base class with the `"azuredevops_build_definition"` resource type string.
- Remove `BuildDefinitionRenderer` and replace its registration with a direct `AzureDevOpsDelegatingRenderer` instance, or document and retain it as an extension point.

---

### Out of Scope

- Changes to any user-visible CLI options, output format, or report content.
- Changes to public API contracts used by external callers (if any exist).
- Performance benchmarking or optimisation beyond what the refactors naturally produce.
- New features or new resource-type support.
- Test coverage improvements beyond what is required to keep the test suite green after each refactor.

## User Experience

This feature has no user-facing changes. Users invoking `tfplan2md` will see identical output before and after. The only observable change is that the codebase is easier to maintain and extend.

## Success Criteria

Each finding must be resolved independently and verifiably:

### Duplicate Code
- [ ] **1.1** `ReportRenderer.FormatSummaryBreakdown` is removed; `ReportRenderer.RenderSummary` delegates to the shared helper from `SummaryRenderer` (or an equivalent shared location). No duplicate method body exists.
- [ ] **1.2** The four AzDO mapper classes share a common base class or generic helper. The common logic (constructor, `GetName` overloads, `GetEntityName`, `RecordFailedResolution`) is defined in exactly one place. Each concrete class contains only entity-specific values.
- [ ] **1.3** The four AzDO formatter classes share a common static helper for the `TryFormat` body. Each concrete class delegates to the helper, passing only its mapper and icon.
- [ ] **1.4** `PatternMatchingRegistry<T>` exposes a `TryResolveFirst` method. `IconProviderRegistry.TryGetIcon` and `ValueFormatterRegistry.TryFormat` each delegate to it; the iteration loop is not duplicated.
- [ ] **1.5** `FormatAttributeValuePlain` no longer contains a manual dispatch chain of `TryFormat*` calls. It delegates to `TryFormatSemanticValue` and strips backtick wrapping.
- [ ] **1.6** `FormatAttributeValue` and `FormatAttributeValueWithResource` wrapper methods are removed. Their call sites call `FormatAttributeValueCore` directly (with a default parameter where applicable).

### Dead Code / Unused Parameters
- [ ] **2.1** `ShouldUseMultilineDetailsSummary` is deleted. Its single call site in `Resolve` is replaced with `true`.
- [ ] **2.2** `ResourceViewModelFactoryRegistry` constructor takes no parameters. The `#pragma warning disable IDE0060` suppression is removed. `CompositionRoot.cs` is updated.
- [ ] **2.3** `VariableGroupRenderer(LargeValueFormat)` overload is deleted. All call sites use the parameterless constructor.
- [ ] **2.4** `MarkdownRenderer` primary constructor has no `principalMapper` parameter. The legacy secondary constructor (with `customTemplateDirectory`) is removed if it has no callers. All affected tests and call sites compile without change.
- [ ] **2.5** `ApplyViewModel` signature is replaced with an `ApplyViewModelContext` parameter. No implementation contains `_ = principalMapper;` or `_ = iconProviderRegistry;` discards.

### Overly Complex / Verbose Code
- [ ] **3.1** `MatchPattern` has a single private helper that computes both `CountSpecificity` and `CalculateDimensionPriority` in one pass over the four pattern properties.
- [ ] **3.2** `BuildConfigurationReferencesForResource` constructs at most one empty dictionary instance. There is no second `return new Dictionary<...>(...)` path.
- [ ] **3.3** `ConfigurationReferenceResolver.BuildReferenceIndex` is called exactly once per `ReportModelBuilder.Build` invocation. The resulting index is passed into `ResourceChangeStage` rather than rebuilt there.

### Modern C# Patterns
- [ ] **4.1** `ServiceResolutionContext` is a positional record. All construction call sites use the positional syntax (or named arguments). The explicit constructor body is gone.
- [ ] **4.2** `SummaryModel` is a `sealed record`. All construction call sites compile without modification or with minimal updates.
- [ ] **4.3** `ActionSummary`, `SummaryModel`, and `ResourceTypeBreakdown` are declared `internal`. The test suite and all other callers remain green.

### Redundant Class Design
- [ ] **5.1** `IResourceViewModelFactoryRegistry` declares `TryGetFactory`. `ResourceChangeStage` depends on the interface, not the concrete type.
- [ ] **5.2** `VariableGroupFactory` and `BuildDefinitionFactory` are either removed (with their registrations) or explicitly documented as future extension points with a tracking comment. If removed, their registrations in the module are gone.
- [ ] **5.3** `BuildDefinitionRenderer` class is either removed (with its registration replaced by a direct `AzureDevOpsDelegatingRenderer` instance) or explicitly documented as a deliberate extension-point stub.

### Overall
- [ ] All existing tests pass with no regressions.
- [ ] No new Roslyn analyser warnings or suppression pragmas are introduced.
- [ ] The `#pragma warning disable IDE0060` in `ResourceViewModelFactoryRegistry.cs` is removed (finding 2.2 resolved).

## Open Questions

1. **Finding 2.5 (`ApplyViewModelContext`):** Should the new context record live in the `MarkdownGeneration.Models` namespace alongside `IResourceViewModelFactory`, or in a shared `MarkdownGeneration` namespace? The Architect should decide the placement.

2. **Finding 3.3 (double `BuildReferenceIndex` call):** Passing the pre-built index into `ResourceChangeStage` will change its constructor signature. The Architect should confirm whether the index should be passed at construction time or per-`Build` call, and whether this affects any test infrastructure.

3. **Finding 5.2 (AzDO factories with no-op `ApplyViewModel`):** The intent of `VariableGroupFactory` and `BuildDefinitionFactory` is not clear from the code alone — they expose `CreateViewModel` but it is not called via the `IResourceViewModelFactory` interface. The Architect or maintainer should confirm whether these factories are vestigial or planned extension points before the Developer removes them.
