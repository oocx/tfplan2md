# ADR-012: Code Simplification Refactoring — Design Decisions

## Status

Accepted

## Context

Feature 111 identifies 16 code-quality findings grouped into five categories: duplicate code,
dead code / unused parameters, overly complex code, modern C# patterns, and redundant class
design. The findings are purely internal; no user-visible output or API contract changes.

This document records the concrete implementation approach for the six findings that require an
architectural decision before the Developer can proceed. The remaining findings (1.1, 1.4–1.6,
2.1–2.4, 3.1–3.2, 4.1–4.3, 5.1) are straightforward refactors with no design ambiguity and are
covered only by implementation notes below.

Reference: `docs/features/111-code-simplification/specification.md`

---

## Decision 1 — Finding 1.2: AzDO Mapper Base Class

### Options Considered

**Option A: Abstract base class with virtual `GetEntityName`**

Introduce `internal abstract class AzdoEntityMapper` in `Providers/AzureDevOps/`. The base class
owns:
- Constructor accepting `FrozenDictionary<string, string> mappings` and `IDiagnosticSink? diagnostics`
- `public string? GetName(string id)` (concrete)
- `public string? GetName(string id, string? resourceAddress)` (concrete, with `RecordFailedResolution` call)
- `protected abstract FailedResolutionType EntityType { get; }` (used in `RecordFailedResolution`)
- `public virtual string GetEntityName(string id)` — default returns `$"{displayName} [{id}]"` or `id`

Each concrete mapper (`AzdoGroupMapper`, `AzdoUserMapper`, `AzdoProjectMapper`) inherits without overrides.
`AzdoRepositoryMapper` overrides `GetEntityName` to prepend the `🗃️` icon and use parentheses formatting.

- **Pros:** Single definition of all shared logic; repository override is minimal; tests for
  `GetEntityName` remain per-concrete-class (no tests need to change to exercise the base method).
- **Cons:** Introduces inheritance; concrete classes must call `base(...)` constructor.

**Option B: Generic abstract base `AzdoEntityMapper<TFailedResolutionType>`**

Parameterise on the `FailedResolutionType` enum value instead of using an abstract property.

- **Pros:** Avoids a virtual property dispatch.
- **Cons:** Generics add complexity for what is effectively a single constant; C# enum values are
  not type-safe generics; more ceremony.

**Option C: Shared static helper class**

Move common methods into `AzdoMapperHelpers` static methods; each concrete class calls the
helper.

- **Pros:** No inheritance; easier to read in isolation.
- **Cons:** Each concrete class still holds the `_mappings` and `_diagnostics` fields verbatim;
  the constructor duplication is not eliminated; overall line reduction is smaller.

### Decision

**Option A** — abstract base class with a virtual `GetEntityName`.

### Rationale

The four mappers are structurally identical save for one enum constant and one display-format
override. An abstract base class eliminates the constructor, field declarations, both `GetName`
overloads, and the `RecordFailedResolution` invocation from three of the four classes entirely.
The `FailedResolutionType` enum value is naturally expressed as an abstract property rather than
a generic parameter. The repository mapper's distinct `GetEntityName` override is a small and
visible deviation that does not pollute the base class.

### Implementation Notes

- File: new `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoEntityMapper.cs`
- Access: `internal abstract class AzdoEntityMapper`
- Constructor: `protected AzdoEntityMapper(FrozenDictionary<string, string> mappings, IDiagnosticSink? diagnostics)`
- Abstract property: `protected abstract FailedResolutionType EntityType { get; }`
- The existing concrete classes lose their bodies for `GetName` and `GetEntityName`; the only
  lines remaining in each class are the constructor forwarding to `base(...)` and the
  `EntityType` property implementation.
- `AzdoRepositoryMapper` additionally overrides `GetEntityName` (the repository-icon format
  diverges from the shared `{displayName} [{id}]` pattern).
- **Do not touch** the `FailedResolutionType` enum itself; it is already defined elsewhere.

---

## Decision 2 — Finding 1.3: AzDO Formatter Static Helper

### Options Considered

**Option A: Shared static helper method**

Add `internal static class AzdoFormatterHelper` in `Providers/AzureDevOps/` with a single method:

```text
internal static string? TryFormat(string? value, Func<string, string?> getName, string icon)
```

Each formatter class delegates to this helper, passing `_mapper.GetName` as the `Func` and its
own icon literal.

- **Pros:** No inheritance chain; each formatter remains a thin, readable wrapper; the helper has
  no state and is trivially testable in isolation.
- **Cons:** Formatters still exist as separate classes (acceptable — they register against
  different `MatchPattern`s and have different icons).

**Option B: Abstract base class with abstract `GetName` and `Icon` members**

Introduce `AzdoFormatterBase : IValueFormatter` with `TryFormat` as the concrete method and
abstract `string? GetName(string value)` / `string Icon` members.

- **Pros:** No Func delegate; pure OOP.
- **Cons:** Four concrete subclasses just to override two members; the mappers are already
  separate classes — injecting them into a shared base creates an awkward coupling.

### Decision

**Option A** — shared static helper method.

### Rationale

The four formatter classes differ only in which mapper they hold and which icon string they use.
Extracting a single `TryFormat(value, getName, icon)` static method removes the duplicated body
without any inheritance. Each formatter class reduces to its constructor + a one-line
`TryFormat` delegation. A static helper is simpler to test and has no lifecycle concerns.

### Implementation Notes

- File: new `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoFormatterHelper.cs`
- The helper must be `internal static class AzdoFormatterHelper`; the method must not reference
  any mapper type directly — it takes a `Func<string, string?>` to keep it provider-agnostic
  within the AzDO namespace.
- Each formatter's `TryFormat` body collapses to:
  ```text
  return AzdoFormatterHelper.TryFormat(context?.Value, _mapper.GetName, "«icon»");
  ```
- The `ArgumentNullException.ThrowIfNull(context)` guard in each formatter is preserved before
  the delegation.

---

## Decision 3 — Finding 2.5: `ApplyViewModelContext` Record

### Open Question Resolved

> Should the new context record live in the `MarkdownGeneration.Models` namespace alongside
> `IResourceViewModelFactory`, or in a shared `MarkdownGeneration` namespace?

**Decision: `MarkdownGeneration.Models` namespace.**

### Options Considered

**Option A: `MarkdownGeneration.Models` namespace**

Defined in the same namespace as `IResourceViewModelFactory` and `ResourceViewModelFactoryRegistry`.
The record is consumed exclusively through the factory interface, which lives in this namespace.

- **Pros:** Colocation with the interface it serves; single import in all factory implementations.
- **Cons:** None identified.

**Option B: `MarkdownGeneration` namespace**

Placed alongside `ReportModel`, `SummaryModel`, etc.

- **Pros:** Slightly more prominent placement.
- **Cons:** The record is a factory-calling convention detail, not a report-model concept; would
  pollute the `MarkdownGeneration` root with an infrastructure type.

### Decision

**Option A** — place in `MarkdownGeneration.Models`.

### Parameters

```text
internal sealed record ApplyViewModelContext(
    ResourceChangeModel Model,
    ResourceChange ResourceChange,
    string Action,
    IReadOnlyList<AttributeChangeModel> AttributeChanges,
    IPrincipalMapper PrincipalMapper,
    IconProviderRegistry? IconProviderRegistry);
```

These six fields correspond 1-to-1 with the existing six parameters of `ApplyViewModel`.

### Impact on `IResourceViewModelFactory`

The interface method changes from:

```text
void ApplyViewModel(ResourceChangeModel model, ResourceChange resourceChange, string action,
    IReadOnlyList<AttributeChangeModel> attributeChanges,
    IPrincipalMapper principalMapper, IconProviderRegistry? iconProviderRegistry)
```

to:

```text
void ApplyViewModel(ApplyViewModelContext context)
```

### Implementation Notes

- File: new `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ApplyViewModelContext.cs`
- `ResourceChangeStage` constructs the record inline at the call site:
  ```text
  factory.ApplyViewModel(new ApplyViewModelContext(model, resourceChange, action,
      attributeChanges, _principalMapper, _iconProviderRegistry));
  ```
- All factory implementations (`AzureRM`, `AzureAD`, `AzApi` providers) update their
  `ApplyViewModel` signature and unpack only the fields they actually use. The `_ = ...`
  discard statements disappear.
- The default no-op implementation in `IResourceViewModelFactory` retains its `{ }` body;
  its parameter changes to `ApplyViewModelContext context`.

---

## Decision 4 — Finding 5.2: AzDO No-Op Factories (`VariableGroupFactory` / `BuildDefinitionFactory`)

### Open Question Resolved

> Are these factories vestigial or planned extension points?

**Decision: Remove both factories and their registrations.**

### Analysis

Examination of `AzureDevOpsModule.RegisterFactories` reveals both factories are registered, but
their `ApplyViewModel` method is never overridden — they inherit the default no-op from
`IResourceViewModelFactory`. The only non-interface method, `CreateViewModel`, is `internal` and
is not called anywhere outside the factory class itself (confirmed by codebase grep). The actual
rendering for `azuredevops_variable_group` and `azuredevops_build_definition` is performed by
dedicated `VariableGroupRenderer` and `BuildDefinitionRenderer` classes, which build their view
models directly via the static `VariableGroupViewModelFactory` and `BuildDefinitionViewModelFactory`
respectively.

Registering these factories consumes an entry in the `ResourceViewModelFactoryRegistry` and
triggers a no-op `ApplyViewModel` call per resource instance during plan processing. There is
no runtime effect, no test that validates factory behavior for these types, and no roadmap item
referencing them as future extension points.

### Decision

Remove `VariableGroupFactory` and `BuildDefinitionFactory` from `Factories.cs` and remove the
two `registry.RegisterFactory(...)` calls from `AzureDevOpsModule.RegisterFactories`.

### Implementation Notes

- Delete or empty `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/Factories.cs` (if both
  classes are the only content).
- Remove the `RegisterFactories` body in `AzureDevOpsModule`; if `IProvider.RegisterFactories`
  has a default no-op, the method may be deleted entirely; otherwise it should be left as an
  empty method body with a comment.
- Verify the test `ProviderResourceRenderersTests.cs` does not assert on factory registration
  for these resource types.

---

## Decision 5 — Finding 5.3: `BuildDefinitionRenderer` Pass-Through Class

### Options Considered

**Option A: Remove `BuildDefinitionRenderer`; make `AzureDevOpsDelegatingRenderer` instantiable**

`AzureDevOpsDelegatingRenderer` is currently `abstract`. Making it non-abstract allows direct
instantiation as `new AzureDevOpsDelegatingRenderer("azuredevops_build_definition")` in
`AzureDevOpsModule.RegisterResourceRenderers`.

- **Pros:** Removes an empty subclass; the registration becomes self-documenting.
- **Cons:** `AzureDevOpsDelegatingRenderer` was `abstract` to enforce subclassing; removing
  `abstract` signals it can be used directly.

**Option B: Retain `BuildDefinitionRenderer` with explicit documentation**

Add an XML comment marking it as a deliberate extension-point stub (e.g., `<!-- intentional
pass-through - override Render to add azuredevops_build_definition-specific layout -->`).

- **Pros:** Future-proof; makes intent explicit without class deletion.
- **Cons:** Still a class with zero behaviour; the original finding is not resolved.

### Decision

**Option A** — remove `BuildDefinitionRenderer`; change `AzureDevOpsDelegatingRenderer` from
`abstract` to a concrete class.

### Rationale

`AzureDevOpsDelegatingRenderer` provides a complete, useful default implementation (delegate to
`DefaultResourceRenderer`). Marking it `abstract` is misleading since there is nothing that
*must* be overridden — `VariableGroupRenderer` overrides `Render` voluntarily. Removing
`abstract` restores the class to the correct semantic: a useful default with optional
specialisation. The registration in `AzureDevOpsModule` becomes `new AzureDevOpsDelegatingRenderer("azuredevops_build_definition")`, which is clear and complete.

### Implementation Notes

- Change `internal abstract class AzureDevOpsDelegatingRenderer` to
  `internal class AzureDevOpsDelegatingRenderer` in `AzureDevOpsResourceRenderers.cs`.
- Delete the `BuildDefinitionRenderer` class from the same file.
- Update `AzureDevOpsModule.RegisterResourceRenderers`:
  ```text
  registry.Register(new AzureDevOpsDelegatingRenderer("azuredevops_build_definition"));
  ```
- `VariableGroupRenderer` continues to extend `AzureDevOpsDelegatingRenderer` with its own
  `Render` override — no changes required there.

---

## Decision 6 — Finding 3.3: Double `BuildReferenceIndex` Call

### Open Question Resolved

> Should the pre-built index be passed at construction time or per-`Build` call?
> Does this affect test infrastructure?

**Decision: Pass the pre-built index per `Build` call via an optional parameter on the
`IResourceChangeStage` interface.**

### Options Considered

**Option A: Pass per-`Build` call — optional interface parameter**

Change `IResourceChangeStage.Build(TerraformPlan plan)` to
`Build(TerraformPlan plan, IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>? preBuiltReferenceIndex = null)`.

`ResourceChangeStage.Build` uses `preBuiltReferenceIndex` when non-null; otherwise falls back to
computing it. `ReportModelBuilder.Build.cs` passes its already-built index.

- **Pros:** No breaking change to existing callers (default `null`); test infrastructure
  unchanged; eliminates duplicate parsing in production; clean opt-in.
- **Cons:** The index type is verbose in the method signature; however, it is an established
  internal type.

**Option B: Pass at construction time**

`ResourceChangeStage` receives the index in its constructor.

- **Pros:** No interface change.
- **Cons:** `ResourceChangeStage` is a stateful singleton relative to a per-plan index; each
  `Build` call for a new plan would use a stale index. **Not viable.**

**Option C: Separate `SetIndex` / "pre-seed" method**

Add a `void SetReferenceIndex(...)` method called before `Build`.

- **Pros:** No interface change to `Build`.
- **Cons:** Implicit ordering dependency (must call `SetIndex` before `Build`); effectively
  stateful; worse than Option A.

### Decision

**Option A** — optional parameter on `IResourceChangeStage.Build`.

### Rationale

The reference index is computed once in `ReportModelBuilder.Build` (it is already stored in
`_configurationReferenceIndex`). Passing it down to `ResourceChangeStage` as an optional
parameter avoids a second parse of the same plan configuration data. The default-`null` value
preserves backward compatibility with all test call sites that pass only a `TerraformPlan`;
those test paths continue to compute the index inside `ResourceChangeStage.Build` just as they
do today, with no test changes required.

### Implementation Notes

- Change signature in `IResourceChangeStage`:
  ```text
  IReadOnlyList<ResourceChangeModel> Build(
      TerraformPlan plan,
      IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>? preBuiltReferenceIndex = null);
  ```
- In `ResourceChangeStage.Build`:
  ```text
  var configurationReferenceIndex =
      preBuiltReferenceIndex
      ?? ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);
  ```
- In `ReportModelBuilder.Build.cs`, pass the already-built index to the stage call:
  ```text
  var allChanges = (_resourceChangeStage ?? CreateResourceChangeStage())
      .Build(plan, _configurationReferenceIndex)
      .ToList();
  ```
  Note: `_configurationReferenceIndex` is a `Dictionary<(string, string), IReadOnlyList<string>>`
  which satisfies `IReadOnlyDictionary<...>` implicitly.

---

## Consequences

### Positive

- All six open questions are resolved with minimal surface area changes.
- `ApplyViewModelContext` eliminates four `_ = ...` discards across multiple factories and makes
  the intent of each factory implementation self-evident.
- The AzDO mapper base class reduces ~300 lines of near-identical boilerplate to a single
  definition.
- The AzDO formatter helper reduces four identical method bodies to one.
- Removing the no-op factories and `BuildDefinitionRenderer` reduces class count and removes
  misleading empty implementations.
- Passing the pre-built reference index eliminates one full configuration parse per plan render
  in production.

### Negative

- Changing `IResourceViewModelFactory.ApplyViewModel` is a broad interface change affecting every
  factory implementation across `AzureRM`, `AzureAD`, `AzApi`, and `AzureDevOps` providers —
  the Developer must update all implementations in one commit to keep the build green.
- Making `AzureDevOpsDelegatingRenderer` non-abstract changes its semantic contract; future
  contributors must know it can be used directly or subclassed.

---

## Findings With No Architectural Decision Required

The following findings are straightforward refactors; they follow existing patterns and require
no new design choices:

| Finding | Action |
|---------|--------|
| 1.1 `FormatBreakdown` duplication | Move helper to `SummaryRenderer`; `ReportRenderer` calls it |
| 1.4 `PatternMatchingRegistry.TryResolveFirst` | Add method; both registries delegate to it |
| 1.5 `FormatAttributeValuePlain` dispatch | Delegate to `TryFormatSemanticValue`, strip backticks |
| 1.6 `FormatAttributeValue` wrappers | Collapse into `FormatAttributeValueCore` with default param |
| 2.1 `ShouldUseMultilineDetailsSummary` | Delete method; replace call site with `true` |
| 2.2 `ResourceViewModelFactoryRegistry` params | Remove both params + pragma; update `CompositionRoot` |
| 2.3 `VariableGroupRenderer(LargeValueFormat)` | Delete overload; update call sites |
| 2.4 `MarkdownRenderer` dead params | Remove `principalMapper`; remove legacy secondary constructor |
| 3.1 `CountSpecificity` / `CalculateDimensionPriority` | Merge into single-pass helper |
| 3.2 Duplicate empty dictionaries | Declare shared empty-dictionary variable |
| 4.1 `ServiceResolutionContext` | Convert to positional record |
| 4.2 `SummaryModel` | Convert to `sealed record` |
| 4.3 Access modifiers | Change `ActionSummary`, `SummaryModel`, `ResourceTypeBreakdown` to `internal` |
| 5.1 `IResourceViewModelFactoryRegistry.TryGetFactory` | Add to interface; update `ResourceChangeStage` to use interface |
