# Code Simplification Refactoring (Feature 111)

Internal code quality maintenance release. This refactoring eliminates 16 code-quality findings — duplicate logic, dead code, unused parameters, and overly complex structures — without changing any user-visible output, CLI options, or API contracts.

## ♻️ Refactoring

This release is a pure internal refactoring. There are no new features, no bug fixes visible to users, and no changes to the tool's output or behaviour.

**What changed internally:**

- **Shared AzDO mapper base class** — `AzdoGroupMapper`, `AzdoUserMapper`, `AzdoProjectMapper`, and `AzdoRepositoryMapper` now inherit from a common `AzdoEntityMapper` abstract base, eliminating ~300 lines of duplicated structure.
- **Shared AzDO formatter helper** — `AzdoGroupDescriptorFormatter`, `AzdoUserIdFormatter`, `AzdoProjectIdFormatter`, and `AzdoRepositoryIdFormatter` now delegate to a single `AzdoFormatterHelper.TryFormat` method, reducing each class to a thin wrapper.
- **`ApplyViewModelContext` record** — The six-parameter `ApplyViewModel` interface method is replaced by a single context record, removing scattered `_ = param` discards across all factory implementations.
- **Eliminated dead code** — `BuildDefinitionRenderer`, `VariableGroupFactory`, `BuildDefinitionFactory`, `ShouldUseMultilineDetailsSummary`, and the unused `principalMapper` constructor parameter have been removed.
- **Reduced duplicate dispatch** — `FormatAttributeValuePlain` now delegates to `TryFormatSemanticValue` instead of duplicating its dispatch chain; `IconProviderRegistry` and `ValueFormatterRegistry` share a `TryResolveFirst` helper in `PatternMatchingRegistry<T>`.
- **Modern C# idioms** — `ServiceResolutionContext` and `SummaryModel` converted to `sealed record`s; `ActionSummary`, `SummaryModel`, and `ResourceTypeBreakdown` tightened to `internal` access.
- **Single `BuildReferenceIndex` call** — Configuration reference index is built once per plan render and passed down, eliminating a redundant rebuild on each resource.

All 1186 tests pass. Snapshot output is unchanged.

## 🔗 Commits

- [`6cf04ae`](https://github.com/oocx/tfplan2md/commit/6cf04ae) refactor: introduce ApplyViewModelContext record (Tasks 1-2)
- [`7848a1c`](https://github.com/oocx/tfplan2md/commit/7848a1c) refactor: AzDO mapper base class and formatter helper (Tasks 3-5)
- [`f3f7bf5`](https://github.com/oocx/tfplan2md/commit/f3f7bf5) refactor: remove BuildDefinitionRenderer, vestigial factories, add TryGetFactory, remove unused registry params (Tasks 6-9)
- [`925f789`](https://github.com/oocx/tfplan2md/commit/925f789) Refactor Markdown Generation and Renderer Components
- [`0bf53db`](https://github.com/oocx/tfplan2md/commit/0bf53db) refactor: fix pre-existing build errors from Tasks 1-16 and implement Tasks 17-22
- [`ef13637`](https://github.com/oocx/tfplan2md/commit/ef13637) refactor: convert ServiceResolutionContext to positional record
- [`2856bd9`](https://github.com/oocx/tfplan2md/commit/2856bd9) refactor: fix code review issues — remove unused principalMapper param, fix naming, add comment
