# Code Quality Fixes: Dead Code, Access Modifiers, and Cache Leak

Internal code quality fixes: removes production rendering heuristics tied to test fixtures, eliminates dead scaffolding code, corrects access modifiers, and closes a `[ThreadStatic]` cache leak.

## 🐛 Bug fixes

- **Production snapshot heuristics removed** (`ReportRenderer.cs`): Deleted `IsKnownAfterApplyCompatibilityScenario` and `IsEphemeralOpenCompatibilityScenario` helper methods that fingerprinted test-fixture resource counts to change table formatting in production builds. The heuristics were originally added as compatibility shims and had been dead weight since the scenarios they handled were properly covered.

- **`ShouldUseEphemeralOpenFormatting` stub removed** (`DefaultResourceRenderer.cs`): Deleted a permanently-dead formatting scaffolding — the method always returned `false`, making every branch it guarded unreachable. The ephemeral-open formatting path, the `useEphemeralOpenFormatting` variable, and the corresponding `IsEphemeralOpenScenario` property on `IScenarioRenderContext`/`ScenarioRenderContext` are all gone. `ResolveScenarioFormatting` return tuple narrowed from 3 to 2 elements.

- **`MarkdownHelpers` visibility corrected** (14 files): All 14 partial-class declarations of `MarkdownHelpers` were incorrectly `public`. Changed to `internal` to match the project specification. `tfplan2md` is a CLI tool, not a library; the test project already had `InternalsVisibleTo` access, so no tests are affected.

- **`ClearLineDiffCache()` cache leak fixed** (`MarkdownRenderer.cs`): Both `Render()` overloads now call `MarkdownHelpers.ClearLineDiffCache()` in a `try/finally` block. The `[ThreadStatic]` LCS (Longest Common Subsequence) cache was documented to require cleanup after each render pass, but the call was inadvertently lost when the Scriban renderer was removed. Under sustained load this caused unbounded memory growth per render thread.

- **`ApplyViewModel()` dead stubs removed** (`IResourceViewModelFactory` and implementations): Added a default no-op body to the `IResourceViewModelFactory.ApplyViewModel()` interface method, eliminating the need for pure-stub overrides. Deleted 4 stub implementations (`NetworkSecurityGroupFactory`, `RoleAssignmentFactory`, `VariableGroupFactory`, `BuildDefinitionFactory`) that contributed nothing. Real implementations (those building `SummaryHtml`/`ChangedAttributesSummary`) are preserved.

## 🔗 Commits

- [`f9f7682`](https://github.com/oocx/tfplan2md/commit/f9f7682f) fix: remove snapshot-compatibility heuristics and dead code from rendering pipeline
- [`f09fb4d`](https://github.com/oocx/tfplan2md/commit/f09fb4dc) fix: remove 5 code review findings (dead code, visibility, cache leak)
