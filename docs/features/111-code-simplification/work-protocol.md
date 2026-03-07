# Work Protocol: Code Simplification Refactoring

**Work Item:** `docs/features/111-code-simplification/`
**Branch:** `feature/111-code-simplification`
**Workflow Type:** Feature
**Created:** 2025-01-27

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2025-01-27
- **Summary:** Gathered and documented requirements for the code simplification refactoring feature. Verified all 16 findings against source files before writing the specification. Confirmed that findings 1.1–1.6, 2.1–2.5, 3.1–3.3, 4.1–4.3, and 5.1–5.3 are accurate and reproducible in the current codebase.
- **Artifacts Produced:** `docs/features/111-code-simplification/work-protocol.md`, `docs/features/111-code-simplification/specification.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2025-03-07
- **Summary:** Reviewed all 16 findings and their source files. Resolved all three open questions from the specification and produced the complete implementation design in `architecture.md`. Key decisions:
  1. **Finding 1.2 (AzDO mappers):** Abstract base class `AzdoEntityMapper` with virtual `GetEntityName`; `AzdoRepositoryMapper` overrides for icon formatting.
  2. **Finding 1.3 (AzDO formatters):** Static helper `AzdoFormatterHelper.TryFormat(value, getName, icon)` — each formatter delegates to it.
  3. **Finding 2.5 (`ApplyViewModelContext`):** New positional record in `MarkdownGeneration.Models`; six params matching the existing `ApplyViewModel` signature.
  4. **Finding 5.2 (AzDO no-op factories):** Remove `VariableGroupFactory` and `BuildDefinitionFactory` — confirmed vestigial (no `ApplyViewModel` override, `CreateViewModel` never called via interface).
  5. **Finding 5.3 (`BuildDefinitionRenderer`):** Remove the class; change `AzureDevOpsDelegatingRenderer` from `abstract` to concrete; replace registration with a direct instantiation.
  6. **Finding 3.3 (double `BuildReferenceIndex`):** Add optional `preBuiltReferenceIndex` parameter to `IResourceChangeStage.Build`; `ReportModelBuilder` passes its pre-built index; null fallback preserves all existing test call sites.
- **Artifacts Produced:** `docs/features/111-code-simplification/architecture.md`
- **Problems Encountered:** None — all open questions resolved from source inspection without needing maintainer input.

## Architecture Notes

### Key Design Decisions

#### AzDO Mapper Base Class (Finding 1.2)
`internal abstract class AzdoEntityMapper` in `Providers/AzureDevOps/`. Abstract property
`FailedResolutionType EntityType { get; }` supplies the enum value for diagnostics. The base
class provides virtual `GetEntityName` that returns `"{displayName} [{id}]"` — `AzdoRepositoryMapper`
overrides it to produce `"🗃️ {displayName} ({id})"` or `"🗃️ {id}"`.

#### AzDO Formatter Helper (Finding 1.3)
`internal static class AzdoFormatterHelper` with one method:
`TryFormat(string? value, Func<string, string?> getName, string icon) → string?`.
Each of the four formatter classes becomes a ~10-line thin wrapper.

#### `ApplyViewModelContext` Record (Finding 2.5)
Namespace: `Oocx.TfPlan2Md.MarkdownGeneration.Models`.
Parameters (positional): `Model`, `ResourceChange`, `Action`, `AttributeChanges`, `PrincipalMapper`, `IconProviderRegistry`.
`IResourceViewModelFactory.ApplyViewModel` shrinks to a single-parameter method.

#### AzDO No-Op Factories (Finding 5.2)
`VariableGroupFactory` and `BuildDefinitionFactory` are vestigial — rendering is done entirely
by dedicated `*Renderer` classes. Both factories and their `RegisterFactory` calls are removed.

#### `BuildDefinitionRenderer` (Finding 5.3)
`AzureDevOpsDelegatingRenderer` changes from `abstract` to concrete. `BuildDefinitionRenderer`
is deleted. `AzureDevOpsModule` registers `new AzureDevOpsDelegatingRenderer("azuredevops_build_definition")` directly.

#### Double `BuildReferenceIndex` Call (Finding 3.3)
`IResourceChangeStage.Build` gains an optional parameter
`IReadOnlyDictionary<(string, string), IReadOnlyList<string>>? preBuiltReferenceIndex = null`.
Production path passes the pre-built index; test paths pass `null` and self-compute (no test changes needed).
