# Code Review: Top 5 Improvement Suggestions

## Summary

This is a general codebase review for `tfplan2md` identifying the five highest-impact improvements.
The review covers the `src/Oocx.TfPlan2Md/` production source and the associated test suite.

Overall, the codebase is in good shape — strong test coverage, consistent XML documentation,
well-structured provider abstraction, and active use of modern C# 13/NativeAOT patterns. The
issues below are genuine technical debt items or spec violations that will compound as the
codebase grows.

---

## Suggestion 1 — Remove snapshot-compatibility heuristics from production rendering logic

**Severity: Major**
**Files:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` (lines 300–360)

### Problem

`ReportRenderer.Render()` contains two private methods that fingerprint specific test-fixture data
to select a rendering mode:

```csharp
// Returns true only when model.Summary.Total == 11 and there are exactly:
//   azuread_group_member x5, azurerm_resource_group x2, azurerm_storage_account x1,
//   azurerm_virtual_network x1, azurerm_subnet x1, null_resource x1
private static bool IsKnownAfterApplyCompatibilityScenario(ReportModel model) { ... }

// Returns true only when model.Summary.Total == 3 and there are exactly:
//   vault_kv_secret_v2 x3, null_resource x1
private static bool IsEphemeralOpenCompatibilityScenario(ReportModel model) { ... }
```

These results gate a `useWideSummarySeparators` flag that changes the rendered markdown output.
They are used *only* to preserve the current snapshot baselines for two test fixtures
(`azuread-group-members-known-after-apply-plan.json` and `ephemeral-open-plan.json`) without
updating those snapshots.

### Why this matters

- **Production rendering is coupled to test data characteristics.** Any real Terraform plan
  that happens to have 11 resources across those six specific types will silently receive a
  different summary table format — a behaviour the user did not request and that is not
  documented anywhere in the CLI help or feature spec.
- **It breaks the rendering contract.** The wide-format summary table is supposed to be used
  only for outputs-focused reports and the NSG no-op parent scenario, per the comments in
  `RenderSummary`. Adding two more "magic" activation paths pollutes that contract.
- **It will silently break if test data changes.** If either snapshot test fixture is updated
  (e.g., to add a resource), the fingerprint no longer matches and the snapshot diffs.
- **Snapshot-update avoidance is not a valid reason to encode test data into production code.**
  The correct fix is to update the affected snapshots and add `SNAPSHOT_UPDATE_OK` to the
  commit message, not to special-case production behaviour.

### Recommended fix

1. Delete `IsKnownAfterApplyCompatibilityScenario` and `IsEphemeralOpenCompatibilityScenario`.
2. Remove the two corresponding branches from `useWideSummarySeparators` and from
   `ScenarioRenderContext`.
3. Regenerate the affected snapshots with `scripts/update-test-snapshots.sh` and include
   `SNAPSHOT_UPDATE_OK` in the commit message.

---

## Suggestion 2 — Convert `MarkdownHelpers` from `public` to `internal`

**Severity: Major (spec violation)**
**Files:** 13 partial-class files under
`src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/` and
`src/Oocx.TfPlan2Md/Platforms/Azure/MarkdownHelpers.Azure.cs`

### Problem

The project specification states explicitly:

> **This is NOT a class library** – tfplan2md is a standalone CLI tool … Use the most restrictive
> access modifier that works. Avoid `public` unless there is a clear justification.
> Use `InternalsVisibleTo` to expose `internal` members to test projects.

Despite this, the entire `MarkdownHelpers` static partial class is declared `public`:

```csharp
// 13 files, all of them:
public static partial class MarkdownHelpers { ... }
```

There are no external consumers. The test project (`Oocx.TfPlan2Md.TUnit`) already has access
via `InternalsVisibleTo`, and all test code that calls `MarkdownHelpers` compiles without
`public` because `InternalsVisibleTo` grants internal access.

### Why this matters

- It is a direct violation of the stated coding standard.
- `public` on a utility class signals "this is a stable public API" to future readers and
  AI agents, which can cause false concerns about backwards compatibility when refactoring.
- The project's own documentation calls out exactly this problem:
  > *Agents were considering backwards compatibility and breaking changes for `public` methods
  > even though no external consumers exist.*

### Recommended fix

Change `public static partial class MarkdownHelpers` to `internal static partial class MarkdownHelpers`
in all 13 files. This requires no changes to any call sites because `InternalsVisibleTo`
already grants test access.

---

## Suggestion 3 — Remove the vestigial `IResourceViewModelFactory.ApplyViewModel()` dead code

**Severity: Minor (design debt)**
**Files:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/IResourceViewModelFactory.cs`,
`src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/Factories.cs`,
`src/Oocx.TfPlan2Md/Providers/AzureRM/Models/Factories.cs`,
`src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryFactory.cs`

### Problem

`IResourceViewModelFactory.ApplyViewModel()` takes six parameters:

```csharp
void ApplyViewModel(
    ResourceChangeModel model,
    ResourceChange resourceChange,
    string action,
    IReadOnlyList<AttributeChangeModel> attributeChanges,
    IPrincipalMapper principalMapper,
    IconProviderRegistry? iconProviderRegistry);
```

In the majority of concrete implementations, every parameter is discarded and the method body
is a comment explaining that view models are now created on-demand via `CreateViewModel`:

```csharp
// AzureDevOps/Models/Factories.cs (VariableGroupFactory and BuildDefinitionFactory)
public void ApplyViewModel(...) {
    _ = principalMapper;
    _ = iconProviderRegistry;
    _ = model;
    _ = action;
    _ = attributeChanges;
    _ = resourceChange;
    // View model is now created on-demand by CreateViewModel
}

// AzureRM/Models/Factories.cs (multiple factories)
public void ApplyViewModel(...) {
    _ = iconProviderRegistry;
    _ = action;
    _ = attributeChanges;
    _ = resourceChange;
    // ...
}
```

This pattern indicates that the `ApplyViewModel` approach was superseded by a different design
(on-demand creation in renderers) but the now-empty interface method was never cleaned up.

### Why this matters

- Six-parameter no-op methods are misleading — they signal that the factory *does* something
  with those parameters when it does not.
- Every new provider factory must implement this vestigial interface method, creating
  unnecessary boilerplate.
- The factory implementations that *do* use `ApplyViewModel` (e.g., `RoleAssignmentViewModelFactory`
  via `Factories.cs`) have a different calling convention and the interface no longer represents
  a coherent contract.

### Recommended fix

Either:

- **Option A:** Remove `ApplyViewModel` from the interface entirely and delete it from all
  concrete implementations. Factories that do real work can keep their `CreateViewModel` methods
  and be called directly by renderers.
- **Option B:** Simplify the interface to match current usage, dropping unused parameters
  (at minimum `action`, `attributeChanges`, and `iconProviderRegistry`).

Option A is preferred to eliminate the dual-path design.

---

## Suggestion 4 — Call `ClearLineDiffCache()` after each render pass

**Severity: Minor (latent test-isolation issue)**
**Files:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/DiffComputation.cs`,
`src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`

### Problem

`DiffComputation.cs` defines a `[ThreadStatic]` cache for LCS diff results:

```csharp
/// Thread-local cache for BuildLineDiff results.
/// Must be called after each render pass to prevent unbounded memory growth.
[ThreadStatic]
private static Dictionary<(string Before, string After), List<DiffEntry>>? _lineDiffCache;

/// Clears the BuildLineDiff result cache.
/// Must be called after each render pass to prevent unbounded memory growth.
internal static void ClearLineDiffCache() { _lineDiffCache?.Clear(); }
```

The documentation is explicit: this method **must** be called. Yet there are **zero call sites**
in the entire codebase:

```
$ grep -rn "ClearLineDiffCache" src/
# Result: only the method definition (DiffComputation.cs line 34)
```

### Why this matters

- For the CLI binary this is safe: one render per process lifetime, then exit.
- In the **test suite**, all test methods that render a diff are evaluated sequentially on
  the same thread pool thread (TUnit batches work per thread). The cache grows across every
  test that invokes `BuildLineDiff`, accumulating `(before, after) → List<DiffEntry>` entries
  indefinitely. On large test runs this inflates memory and could mask failures if a test
  receives a stale cache hit.
- The previous implementation (Scriban-based renderer) called this in `finally` blocks.
  When Scriban was removed (feature 107), the `ClearLineDiffCache()` call was not migrated
  to the new pure-C# renderer path.

### Recommended fix

Call `MarkdownHelpers.ClearLineDiffCache()` in `MarkdownRenderer.Render()` and
`MarkdownRenderer.RenderAsync()` in a `finally` block, matching the original Scriban pattern:

```csharp
public string Render(ReportModel model)
{
    _diagnosticContext?.TemplateResolutions.Add(...);
    var context = CreateContext(model);
    try
    {
        return _reportRenderer.Render(model, context);
    }
    finally
    {
        MarkdownHelpers.ClearLineDiffCache();
    }
}
```

---

## Suggestion 5 — Remove the permanent `ShouldUseEphemeralOpenFormatting` stub

**Severity: Suggestion**
**Files:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs` (line 248),
`src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` (ScenarioRenderContext)

### Problem

`DefaultResourceRenderer` has a method that always returns `false` and discards its parameter:

```csharp
private static bool ShouldUseEphemeralOpenFormatting(ResourceChangeModel change)
{
    _ = change;
    return false;
}
```

Its result `useEphemeralOpenFormatting` is wired into:

- `ResolveScenarioFormatting` (which returns a tuple used in rendering branches)
- `ShouldUseMultilineDetailsSummary`
- `ShouldUseExtraBlankLineBeforeSummary`

But because it always returns `false`, none of those branches are ever taken. Similarly,
`ReportRenderer.ScenarioRenderContext` tracks an `IsEphemeralOpenScenario` property
that is populated by `IsEphemeralOpenCompatibilityScenario` (which itself fingerprints test
data — see Suggestion 1), but the `DefaultResourceRenderer` methods that receive the context
never read `IsEphemeralOpenScenario`.

This implementation skeleton suggests the feature was designed with an ephemeral-open rendering mode in mind,
but either:

- It was never implemented and the scaffold was left behind, or
- It was removed but the stub and wiring were not cleaned up.

### Why this matters

- Dead code paths add cognitive overhead for every future reader of these files.
- The `_ = change;` discard suggests the parameter was added prematurely "in case" — a pattern
  that the spec explicitly discourages.
- The stub interacts with Suggestion 1 (`IsEphemeralOpenCompatibilityScenario`) and will
  become fully dead once that fingerprint method is removed.

### Recommended fix

1. Remove `ShouldUseEphemeralOpenFormatting` from `DefaultResourceRenderer`.
2. Remove the `useEphemeralOpenFormatting` local variable and all branches conditioned on it
   in `Render()`, `ShouldUseMultilineDetailsSummary`, and `ShouldUseExtraBlankLineBeforeSummary`.
3. Remove the `IsEphemeralOpenScenario` property from `ScenarioRenderContext` and its
   constructor parameter (after Suggestion 1 removes the `IsEphemeralOpenCompatibilityScenario`
   source).

---

## Summary Table

| # | Issue | Severity | Primary File |
|---|-------|----------|--------------|
| 1 | Snapshot-compat heuristics in production rendering | Major | `ReportRenderer.cs` |
| 2 | `MarkdownHelpers` is `public` instead of `internal` | Major (spec) | 13 partial-class files |
| 3 | `IResourceViewModelFactory.ApplyViewModel` is dead code | Minor | `Factories.cs` (multiple) |
| 4 | `ClearLineDiffCache()` is never called | Minor | `DiffComputation.cs` |
| 5 | `ShouldUseEphemeralOpenFormatting` is a permanent stub | Suggestion | `DefaultResourceRenderer.cs` |

## Next Steps

These suggestions are ordered by impact. Suggestions 1 and 2 should be addressed first as
they violate the project's spec and/or create incorrect production behaviour.

Suggestions 3–5 are clean-up items that can be batched into a single refactoring PR.
The Developer agent is the appropriate next step for all items.
