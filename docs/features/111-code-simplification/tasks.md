# Tasks: Code Simplification Refactoring

## Overview

Feature 111 addresses 16 code-quality findings grouped into five categories: duplicate code,
dead code / unused parameters, overly complex code, modern C# patterns, and redundant class
design. All changes are pure internal refactors — no user-visible output, CLI options, or API
contracts change. Every task must leave the full test suite green on completion.

References:
- Specification: `docs/features/111-code-simplification/specification.md`
- Architecture: `docs/features/111-code-simplification/architecture.md`

---

## Tasks

### Task 1: Introduce `ApplyViewModelContext` record (Finding 2.5 — step 1 of 2)

**Priority:** High

**Description:**
Create the new positional record `ApplyViewModelContext` in the
`Oocx.TfPlan2Md.MarkdownGeneration.Models` namespace. This record will replace the six
individual parameters of `IResourceViewModelFactory.ApplyViewModel` and must exist before
the interface and its implementors can be updated (Task 2).

**Acceptance Criteria:**
- [ ] New file `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/ApplyViewModelContext.cs` exists.
- [ ] The record is declared `internal sealed record ApplyViewModelContext` with six positional
      parameters: `ResourceChangeModel Model`, `ResourceChange ResourceChange`, `string Action`,
      `IReadOnlyList<AttributeChangeModel> AttributeChanges`, `IPrincipalMapper PrincipalMapper`,
      `IconProviderRegistry? IconProviderRegistry`.
- [ ] Namespace is `Oocx.TfPlan2Md.MarkdownGeneration.Models`.
- [ ] The record compiles and the existing test suite passes without modification.
- [ ] No existing code is changed in this task — the record is additive only.

**Dependencies:** None

**Notes:**
Architecture decision is in ADR-012 Decision 3. This is a pure addition; Task 2 wires it in.

---

### Task 2: Update `IResourceViewModelFactory.ApplyViewModel` to use `ApplyViewModelContext` (Finding 2.5 — step 2 of 2)

**Priority:** High

**Description:**
Change `IResourceViewModelFactory.ApplyViewModel` from six individual parameters to a single
`ApplyViewModelContext context` parameter. Update every implementation and every call site in
one commit so the build stays green throughout.

**Acceptance Criteria:**
- [ ] `IResourceViewModelFactory.ApplyViewModel` signature is `void ApplyViewModel(ApplyViewModelContext context)`.
- [ ] The default no-op implementation on the interface (if any) compiles with the new signature.
- [ ] All factory implementations across `Providers/AzureRM/Models/`, `Providers/AzureAD/`,
      `Providers/AzApi/`, and `Providers/AzureDevOps/Models/` updated to accept
      `ApplyViewModelContext context`.
- [ ] Each updated implementation unpacks only the fields it actually uses; no `_ = ...` discards
      remain for `principalMapper` or `iconProviderRegistry`.
- [ ] `ResourceChangeStage` constructs the record inline:
      `factory.ApplyViewModel(new ApplyViewModelContext(model, resourceChange, action, attributeChanges, _principalMapper, _iconProviderRegistry))`.
- [ ] Full test suite passes with no regressions.

**Dependencies:** Task 1

**Notes:**
This is a broad interface change — all factory implementations must be updated in a single
commit. See ADR-012 Decision 3 for the full parameter list.

---

### Task 3: Introduce `AzdoEntityMapper` abstract base class (Finding 1.2 — step 1 of 2)

**Priority:** High

**Description:**
Create the new abstract base class `AzdoEntityMapper` in `Providers/AzureDevOps/`. This class
consolidates the shared constructor, field declarations, both `GetName` overloads, and the
`RecordFailedResolution` pattern. It must exist before the concrete mapper classes are
simplified (Task 4).

**Acceptance Criteria:**
- [ ] New file `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoEntityMapper.cs` exists.
- [ ] Class is declared `internal abstract class AzdoEntityMapper`.
- [ ] Constructor is `protected AzdoEntityMapper(FrozenDictionary<string, string> mappings, IDiagnosticSink? diagnostics)`.
- [ ] Abstract property `protected abstract FailedResolutionType EntityType { get; }` is declared.
- [ ] Concrete `public string? GetName(string id)` and
      `public string? GetName(string id, string? resourceAddress)` are implemented (including
      `RecordFailedResolution` call on the two-arg overload).
- [ ] Virtual `public virtual string GetEntityName(string id)` returns
      `"{displayName} [{id}]"` (using resolved display name) or falls back to `id`.
- [ ] The class compiles and the existing test suite passes without modification.
- [ ] No concrete mapper classes are changed in this task — the base class is additive only.

**Dependencies:** None

**Notes:**
Architecture decision in ADR-012 Decision 1. This is purely additive; Task 4 migrates the
concrete classes.

---

### Task 4: Simplify the four concrete AzDO mapper classes (Finding 1.2 — step 2 of 2)

**Priority:** High

**Description:**
Refactor `AzdoGroupMapper`, `AzdoUserMapper`, `AzdoProjectMapper`, and `AzdoRepositoryMapper`
to extend `AzdoEntityMapper`. Remove all duplicated boilerplate from each class, leaving only
the constructor forwarding to `base(...)` and the `EntityType` property override.
`AzdoRepositoryMapper` additionally overrides `GetEntityName` for its icon-based format.

**Acceptance Criteria:**
- [ ] All four mapper classes extend `AzdoEntityMapper`.
- [ ] Each class contains only: a constructor that forwards to `base(mappings, diagnostics)`,
      and the `EntityType` property returning the appropriate `FailedResolutionType` value.
- [ ] `AzdoRepositoryMapper` additionally overrides `GetEntityName` to produce
      `"🗃️ {displayName} ({id})"` (or `"🗃️ {id}"` when display name is unavailable).
- [ ] No duplicated field declarations (`_mappings`, `_diagnostics`) remain in the concrete classes.
- [ ] No duplicated `GetName` overloads or `RecordFailedResolution` logic remain in the concrete classes.
- [ ] All existing mapper tests pass; no tests are removed.
- [ ] Full test suite passes with no regressions.

**Dependencies:** Task 3

---

### Task 5: Introduce `AzdoFormatterHelper` and simplify the four AzDO formatter classes (Finding 1.3)

**Priority:** Medium

**Description:**
Create a shared static helper `AzdoFormatterHelper.TryFormat` and reduce
`AzdoGroupDescriptorFormatter`, `AzdoUserIdFormatter`, `AzdoProjectIdFormatter`, and
`AzdoRepositoryIdFormatter` to thin wrappers that delegate to it.

**Acceptance Criteria:**
- [ ] New file `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzdoFormatterHelper.cs` exists.
- [ ] Class is `internal static class AzdoFormatterHelper`.
- [ ] Method signature: `internal static string? TryFormat(string? value, Func<string, string?> getName, string icon)`.
- [ ] The helper encapsulates the shared logic: guard empty value, call `getName`, guard raw == display, format with icon + `MarkdownHelpers.FormatCodeTable`.
- [ ] The helper does not reference any specific mapper type directly.
- [ ] Each of the four formatter classes' `TryFormat` body is replaced with:
      `ArgumentNullException.ThrowIfNull(context);` followed by
      `return AzdoFormatterHelper.TryFormat(context?.Value, _mapper.GetName, "«icon»");`.
- [ ] All existing formatter tests pass with no regressions.

**Dependencies:** None

---

### Task 6: Remove `BuildDefinitionRenderer`; make `AzureDevOpsDelegatingRenderer` concrete (Finding 5.3) — **atomic**

**Priority:** Medium

**Description:**
Remove the empty `BuildDefinitionRenderer` subclass and change `AzureDevOpsDelegatingRenderer`
from `abstract` to a concrete (non-abstract) class. Update `AzureDevOpsModule` to register a
direct `AzureDevOpsDelegatingRenderer` instance. This task must be done atomically — both
changes in a single commit — because removing `BuildDefinitionRenderer` requires
`AzureDevOpsDelegatingRenderer` to be instantiable first.

**Acceptance Criteria:**
- [ ] `AzureDevOpsDelegatingRenderer` is declared `internal class` (not `abstract`).
- [ ] `BuildDefinitionRenderer` class is deleted from `AzureDevOpsResourceRenderers.cs`.
- [ ] `AzureDevOpsModule.RegisterResourceRenderers` registers
      `new AzureDevOpsDelegatingRenderer("azuredevops_build_definition")` directly.
- [ ] `VariableGroupRenderer` continues to extend `AzureDevOpsDelegatingRenderer` and override
      `Render` — no changes required to that class.
- [ ] The project compiles with no errors.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

**Notes:**
Architecture decision in ADR-012 Decision 5. **This is an atomic task — both the `abstract`
removal and the `BuildDefinitionRenderer` deletion must land in the same commit.**

---

### Task 7: Remove `VariableGroupFactory` and `BuildDefinitionFactory` (Finding 5.2)

**Priority:** Medium

**Description:**
Remove the vestigial `VariableGroupFactory` and `BuildDefinitionFactory` classes from
`Providers/AzureDevOps/Models/Factories.cs` and remove their `RegisterFactory` calls from
`AzureDevOpsModule.RegisterFactories`.

**Acceptance Criteria:**
- [ ] `VariableGroupFactory` and `BuildDefinitionFactory` classes are deleted.
- [ ] `AzureDevOpsModule.RegisterFactories` no longer calls `registry.RegisterFactory(...)` for
      these two types.
- [ ] If `Factories.cs` is otherwise empty after the deletions, the file is deleted.
- [ ] If `RegisterFactories` has a default no-op on the `IProvider` interface, the method body
      may be removed (or left as an empty body with a comment); otherwise leave an empty body.
- [ ] No test asserts on factory registration for `azuredevops_variable_group` or
      `azuredevops_build_definition`.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

**Notes:**
Architecture decision in ADR-012 Decision 4. Confirmed vestigial — rendering is done entirely
by the dedicated renderer classes.

---

### Task 8: Add `TryGetFactory` to `IResourceViewModelFactoryRegistry` (Finding 5.1)

**Priority:** Medium

**Description:**
Add the `TryGetFactory` method to `IResourceViewModelFactoryRegistry` so that
`ResourceChangeStage` can depend on the interface rather than the concrete type.

**Acceptance Criteria:**
- [ ] `IResourceViewModelFactoryRegistry` declares `TryGetFactory` with the same signature as
      the existing method on `ResourceViewModelFactoryRegistry`.
- [ ] `ResourceChangeStage` holds a reference of type `IResourceViewModelFactoryRegistry` (not
      the concrete `ResourceViewModelFactoryRegistry`).
- [ ] No other call sites are broken; `CompositionRoot.cs` continues to compile.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 9: Remove unused constructor parameters from `ResourceViewModelFactoryRegistry` (Finding 2.2)

**Priority:** Medium

**Description:**
Remove the unused `LargeValueFormat largeValueFormat` and `IPrincipalMapper principalMapper`
parameters from `ResourceViewModelFactoryRegistry`'s constructor. Remove the
`#pragma warning disable IDE0060` suppression.

**Acceptance Criteria:**
- [ ] `ResourceViewModelFactoryRegistry` constructor takes no parameters (or only parameters
      that are actually used).
- [ ] The `#pragma warning disable IDE0060` pragma in `ResourceViewModelFactoryRegistry.cs` is
      removed.
- [ ] `CompositionRoot.cs` is updated to pass no unused arguments to the constructor.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 10: Consolidate `FormatBreakdown` / `FormatSummaryBreakdown` (Finding 1.1)

**Priority:** Medium

**Description:**
Remove the duplicate private static `ReportRenderer.FormatSummaryBreakdown` method. Move or
expose the existing `SummaryRenderer.FormatBreakdown` as the single shared helper and update
`ReportRenderer.RenderSummary` (or equivalent call site) to delegate to it.

**Acceptance Criteria:**
- [ ] `ReportRenderer.FormatSummaryBreakdown` (private static) is deleted.
- [ ] `ReportRenderer` calls the shared helper from `SummaryRenderer` (or a new shared static
      helper) wherever `FormatSummaryBreakdown` was previously called.
- [ ] No duplicate method body for the breakdown formatting logic exists anywhere in the codebase.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 11: Add `PatternMatchingRegistry<T>.TryResolveFirst`; delegate registries to it (Finding 1.4)

**Priority:** Medium

**Description:**
Add a `TryResolveFirst` method to `PatternMatchingRegistry<T>` and update
`IconProviderRegistry.TryGetIcon` and `ValueFormatterRegistry.TryFormat` to delegate to it,
eliminating the duplicated iteration loop.

**Acceptance Criteria:**
- [ ] `PatternMatchingRegistry<T>` exposes a `TryResolveFirst(...)` method that iterates
      `ResolveAll` and returns the first non-null result.
- [ ] `IconProviderRegistry.TryGetIcon` delegates to `TryResolveFirst`; its own iteration loop
      is removed.
- [ ] `ValueFormatterRegistry.TryFormat` delegates to `TryResolveFirst`; its own iteration loop
      is removed.
- [ ] The duplicated iteration logic is defined in exactly one place.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 12: Refactor `FormatAttributeValuePlain` to delegate to `TryFormatSemanticValue` (Finding 1.5)

**Priority:** Medium

**Description:**
Remove the manual dispatch chain inside `SemanticFormatting.FormatAttributeValuePlain` and
replace it with a call to `TryFormatSemanticValue`, then strip any backtick wrapping from
the result.

**Acceptance Criteria:**
- [ ] `SemanticFormatting.FormatAttributeValuePlain` no longer contains a manual chain of
      `TryFormat*` helper calls.
- [ ] The method delegates to `TryFormatSemanticValue` (passing an appropriate context) and
      strips leading/trailing backticks from the result.
- [ ] The output of `FormatAttributeValuePlain` is identical to before for all inputs.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 13: Collapse `FormatAttributeValue` and `FormatAttributeValueWithResource` wrappers (Finding 1.6)

**Priority:** Medium

**Description:**
Remove the two thin wrapper methods `FormatAttributeValue` and `FormatAttributeValueWithResource`
in `SemanticFormatting.Registry.cs`. Add a default parameter `string? resourceType = null` to
`FormatAttributeValueCore` (or equivalent), and update all call sites to call `FormatAttributeValueCore`
directly.

**Acceptance Criteria:**
- [ ] `FormatAttributeValue` and `FormatAttributeValueWithResource` wrapper methods are deleted.
- [ ] `FormatAttributeValueCore` (or the renamed method) accepts `string? resourceType = null`
      as a default parameter.
- [ ] All call sites that previously called the two wrapper methods now call
      `FormatAttributeValueCore` directly.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 14: Remove `ShouldUseMultilineDetailsSummary` dead method (Finding 2.1)

**Priority:** Low

**Description:**
Delete `DefaultResourceRenderPolicy.ShouldUseMultilineDetailsSummary` (which always returns
`true` after discarding all parameters) and replace its single call site with the literal `true`.

**Acceptance Criteria:**
- [ ] `DefaultResourceRenderPolicy.ShouldUseMultilineDetailsSummary` method is deleted.
- [ ] The single call site in `DefaultResourceRenderPolicy.Resolve` is replaced with the
      literal `true`.
- [ ] No `_ = ...` discard statements referencing the method's former parameters remain.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 15: Remove `VariableGroupRenderer(LargeValueFormat)` overload (Finding 2.3)

**Priority:** Low

**Description:**
Delete the `VariableGroupRenderer(LargeValueFormat largeValueFormat)` constructor overload (which
only discards its parameter with `_ = largeValueFormat;`). Update all call sites and the
AzureDevOps module registration to use the parameterless constructor.

**Acceptance Criteria:**
- [ ] The `VariableGroupRenderer(LargeValueFormat)` constructor overload is deleted.
- [ ] All call sites (including `AzureDevOpsModule` registration) use the parameterless
      constructor.
- [ ] No `_ = largeValueFormat;` discard statement remains.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 16: Remove dead parameters from `MarkdownRenderer` (Finding 2.4)

**Priority:** Low

**Description:**
Remove the `IPrincipalMapper? principalMapper` parameter from `MarkdownRenderer`'s primary
constructor (which discards it with `_ = principalMapper;`). Remove the legacy secondary
constructor (`customTemplateDirectory` shim) if it has no external callers.

**Acceptance Criteria:**
- [ ] `MarkdownRenderer`'s primary constructor no longer accepts `IPrincipalMapper? principalMapper`.
- [ ] The `_ = principalMapper;` discard statement is gone.
- [ ] All call sites (including tests) that passed `principalMapper` are updated to omit the argument.
- [ ] The legacy secondary constructor (`customTemplateDirectory`) is removed if it has no
      callers outside `MarkdownRenderer` itself (verified by grep/reference search).
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 17: Merge `CountSpecificity` / `CalculateDimensionPriority` into a single-pass helper (Finding 3.1)

**Priority:** Low

**Description:**
Refactor `MatchPattern.cs` so that the two private methods iterating the same four nullable
properties are replaced by a single private helper that computes both the specificity count and
the dimension priority in one pass.

**Acceptance Criteria:**
- [x] A single private helper method in `MatchPattern` iterates `ProviderPattern`,
      `ResourceTypePattern`, `AttributeNamePattern`, and `ValuePattern` exactly once.
- [x] `CountSpecificity()` and `CalculateDimensionPriority()` (or their replacements) obtain
      their results from this single-pass helper.
- [x] The four nullable properties are no longer iterated twice independently.
- [x] The computed values of `CountSpecificity` and `CalculateDimensionPriority` are identical
      to before for all inputs.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 18: Deduplicate empty dictionary creation in `BuildConfigurationReferencesForResource` (Finding 3.2)

**Priority:** Low

**Description:**
In `ResourceChangeStage.Helpers.cs`, replace the two separate
`return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)`
early-exit paths with a single shared empty-dictionary variable (or a single shared static
empty instance).

**Acceptance Criteria:**
- [x] `BuildConfigurationReferencesForResource` constructs at most one empty
      `Dictionary<string, IReadOnlyList<string>>` instance.
- [x] There is no second `return new Dictionary<...>(...)` path in the method.
- [ ] Runtime behaviour (return value) is identical to before.
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 19: Eliminate the double `BuildReferenceIndex` call (Finding 3.3)

**Priority:** Low

**Description:**
Add an optional `preBuiltReferenceIndex` parameter to `IResourceChangeStage.Build` and
`ResourceChangeStage.Build`. Update `ReportModelBuilder.Build.cs` to pass its already-computed
index. Existing test call sites pass `null` and self-compute as before.

**Acceptance Criteria:**
- [x] `IResourceChangeStage.Build` signature is:
      `IReadOnlyList<ResourceChangeModel> Build(TerraformPlan plan, IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>? preBuiltReferenceIndex = null)`.
- [x] `ResourceChangeStage.Build` uses `preBuiltReferenceIndex ?? ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration)`.
- [x] `ReportModelBuilder.Build.cs` passes `_configurationReferenceIndex` to the `Build` call.
- [x] `ConfigurationReferenceResolver.BuildReferenceIndex` is called exactly once per
      `ReportModelBuilder.Build` invocation in production.
- [x] Existing test call sites that pass only `plan` continue to compile and pass without
      modification (default `null` ensures backward compatibility).
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

**Notes:**
Architecture decision in ADR-012 Decision 6.

---

### Task 20: Convert `ServiceResolutionContext` to a positional record (Finding 4.1)

**Priority:** Low

**Description:**
Replace the explicit constructor body of `ServiceResolutionContext` with a positional record
declaration. Update all construction call sites to use positional or named-argument syntax.

**Acceptance Criteria:**
- [x] `ServiceResolutionContext` is a positional `sealed record` with four positional parameters:
      `string? ProviderName`, `string? ResourceType`, `string? AttributeName`, `string? Value`.
- [x] The explicit constructor body is removed.
- [x] All construction call sites compile with positional or named-argument syntax.
- [x] Full test suite passes with no regressions.

**Dependencies:** None

---

### Task 21: Convert `SummaryModel` to a `sealed record` (Finding 4.2)

**Priority:** Low

**Description:**
Convert `SummaryModel` from a plain `class` with `required init` properties to a `sealed record`
(positional or with `required init` properties). Update construction call sites as needed.

**Acceptance Criteria:**
- [x] `SummaryModel` is declared as a `sealed record`.
- [x] All construction call sites compile without modification (or with minimal, mechanical updates).
- [ ] Full test suite passes with no regressions.

**Dependencies:** None

**Notes:**
`SummaryModel` will also have its access modifier changed to `internal` in Task 22. These
can be combined if the developer prefers, but Task 22 depends on Task 21 being complete first
(or they are done together).

---

### Task 22: Restrict access modifiers on `ActionSummary`, `SummaryModel`, `ResourceTypeBreakdown` to `internal` (Finding 4.3)

**Priority:** Low

**Description:**
Change the access modifier on `ActionSummary`, `SummaryModel`, and `ResourceTypeBreakdown`
from `public` to `internal`. Verify that no tests or other assemblies reference these types
via a `public` access path.

**Acceptance Criteria:**
- [x] `ActionSummary`, `SummaryModel`, and `ResourceTypeBreakdown` are all declared `internal`.
- [x] No external assembly (test projects or otherwise) fails to compile due to visibility.
- [ ] Full test suite passes with no regressions.

**Dependencies:** Task 21 (for `SummaryModel` — must be a record before or at the same time as
the access-modifier change, to avoid two separate edits to the same type declaration)

---

## Implementation Order

Recommended sequence for implementation:

| # | Task | Reason |
|---|------|--------|
| 1 | Task 1 — Introduce `ApplyViewModelContext` record | Infrastructure prerequisite for Task 2; additive only |
| 2 | Task 2 — Update `IResourceViewModelFactory.ApplyViewModel` | Broad interface change; do immediately after Task 1 so the record is in place |
| 3 | Task 3 — Introduce `AzdoEntityMapper` base class | Infrastructure prerequisite for Task 4; additive only |
| 4 | Task 4 — Simplify concrete AzDO mapper classes | Depends on Task 3 |
| 5 | Task 5 — `AzdoFormatterHelper` + formatter simplification | Independent; natural grouping with other AzDO work |
| 6 | Task 6 — Remove `BuildDefinitionRenderer`; make renderer concrete (**atomic**) | Atomic task; do before other AzDO class removals for clarity |
| 7 | Task 7 — Remove AzDO no-op factories | Independent; completes AzDO class cleanup |
| 8 | Task 8 — Add `TryGetFactory` to `IResourceViewModelFactoryRegistry` | Registry interface work; natural follow-on to factory cleanup |
| 9 | Task 9 — Remove unused `ResourceViewModelFactoryRegistry` constructor params | Completes registry / composition-root cleanup |
| 10 | Task 10 — Consolidate `FormatBreakdown` duplication | Independent duplicate-code fix |
| 11 | Task 11 — `PatternMatchingRegistry.TryResolveFirst` | Independent duplicate-code fix |
| 12 | Task 12 — `FormatAttributeValuePlain` delegation | Independent duplicate-code fix |
| 13 | Task 13 — Collapse `FormatAttributeValue` wrappers | Independent duplicate-code fix |
| 14 | Task 14 — Remove `ShouldUseMultilineDetailsSummary` | Trivial dead-code removal |
| 15 | Task 15 — Remove `VariableGroupRenderer(LargeValueFormat)` overload | Trivial dead-code removal |
| 16 | Task 16 — Remove dead `MarkdownRenderer` params | Straightforward dead-code removal |
| 17 | Task 17 — `MatchPattern` single-pass helper | Low-risk internal refactor |
| 18 | Task 18 — Deduplicate empty dictionary creation | Trivial; low risk |
| 19 | Task 19 — Eliminate double `BuildReferenceIndex` call | Interface change with backward-compatible default |
| 20 | Task 20 — `ServiceResolutionContext` positional record | Modern C# pattern; low risk |
| 21 | Task 21 — `SummaryModel` to `sealed record` | Must precede Task 22 |
| 22 | Task 22 — Restrict access modifiers to `internal` | Depends on Task 21 for `SummaryModel` |

## Open Questions

None — all open questions from the specification were resolved by the Architect in
`docs/features/111-code-simplification/architecture.md`.
