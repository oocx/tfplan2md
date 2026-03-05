# Issue: Code Review Top 5 Findings

**Source:** [`docs/code-review-top-5-suggestions.md`](../../../docs/code-review-top-5-suggestions.md)
(reviewed at commit `5da2d8b46bc24fa55446ee5e5ea1ab5a7801dbad`)

## Problem Description

A general codebase review identified five technical-debt items in the `tfplan2md` production
source that need to be fixed. None of the findings had been addressed at the time of
investigation. The issues range from production logic coupled to test-data fingerprints
(Severity: Major), to spec-violating access modifiers, dead interface methods, an uncalled
cache-clearing helper, and a permanent stub method.

## Steps to Reproduce

All five items are statically verifiable by inspecting the affected source files.

1. **Finding 1:** Open `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs`
   and search for `IsKnownAfterApplyCompatibilityScenario` / `IsEphemeralOpenCompatibilityScenario`.
2. **Finding 2:** Search the codebase for `public static partial class MarkdownHelpers`.
3. **Finding 3:** Search for `ApplyViewModel` across production source files.
4. **Finding 4:** Search the production codebase for `ClearLineDiffCache` — only one hit (the
   method definition; zero call sites).
5. **Finding 5:** Open `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs`
   and search for `ShouldUseEphemeralOpenFormatting`.

## Expected Behavior

- Production rendering logic should not contain data fingerprints for specific test fixtures.
- `MarkdownHelpers` should be `internal`, not `public`, per the project specification.
- `IResourceViewModelFactory.ApplyViewModel()` should be removed if it is no-op dead code.
- `MarkdownHelpers.ClearLineDiffCache()` should be called after each render pass.
- `ShouldUseEphemeralOpenFormatting` should not be a permanent stub that always returns `false`.

## Actual Behavior

All five findings are present and unfixed in the current codebase.

---

## Root Cause Analysis

---

### Finding 1 — Snapshot-compatibility heuristics in production rendering logic

**Severity: Major**

#### Affected Files
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` — lines 300–360

#### What's Broken

Two private methods fingerprint specific test-fixture data to select a rendering mode:

```csharp
// ReportRenderer.cs lines 300–330
[SuppressMessage(...)]
private static bool IsKnownAfterApplyCompatibilityScenario(ReportModel model)
{
    // Returns true only when Total==11 AND the exact type counts match
    // azuread_group_member×5, azurerm_resource_group×2, azurerm_storage_account×1,
    // azurerm_virtual_network×1, azurerm_subnet×1, null_resource×1
    ...
}

// ReportRenderer.cs lines 331–360
private static bool IsEphemeralOpenCompatibilityScenario(ReportModel model)
{
    // Returns true only when Total==3 AND vault_kv_secret_v2×3 + null_resource×1
    ...
}
```

These gate `useWideSummarySeparators` (line 66–74) and appear in the `ScenarioRenderContext`
constructor (line 360–364):

```csharp
var useWideSummarySeparators = isOutputsFocusedReport
    || IsKnownAfterApplyCompatibilityScenario(model)   // ← test-data fingerprint
    || IsEphemeralOpenCompatibilityScenario(model);     // ← test-data fingerprint
```

#### Why It Happened

These methods were added to avoid updating snapshot baselines after a rendering change. The
snapshot-update avoidance caused production rendering to silently differ based on coincidental
resource counts, not intent.

#### Suggested Fix

1. Delete `IsKnownAfterApplyCompatibilityScenario` and `IsEphemeralOpenCompatibilityScenario`.
2. Remove both branches from the `useWideSummarySeparators` assignment and from
   `ScenarioRenderContext` construction.
3. Run `scripts/update-test-snapshots.sh` to regenerate affected baselines.
4. Include `SNAPSHOT_UPDATE_OK` in the commit message.

---

### Finding 2 — `MarkdownHelpers` is `public` instead of `internal`

**Severity: Major (spec violation)**

#### Affected Files (13 partial-class files + 1 Azure file)

| File | Line |
|------|------|
| `src/Oocx.TfPlan2Md/Platforms/Azure/MarkdownHelpers.Azure.cs` | 8 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/Markdown.cs` | 8 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.Identity.cs` | 9 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/CodeFormatting.cs` | 6 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/ValueFormatting.cs` | 9 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.Helpers.cs` | 6 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.cs` | 8 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/DiffUtilities.cs` | 9 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/DiffComputation.cs` | 9 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.Registry.cs` | 10 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/LargeValueSummary.cs` | 11 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/LargeValues.cs` | 11 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/DiffFormatting.cs` | 6 |

#### What's Broken

Every partial-class declaration uses `public` despite the project spec requiring the most
restrictive access modifier, and this being a CLI tool (not a library):

```csharp
// All 13 files contain:
public static partial class MarkdownHelpers { ... }
```

The test project has `InternalsVisibleTo` access, so changing to `internal` requires no
test-side changes.

#### Why It Happened

The access modifier was set to `public` at class creation and never revisited despite the
spec calling this out explicitly.

#### Suggested Fix

Change `public static partial class MarkdownHelpers` →
`internal static partial class MarkdownHelpers` in all 13 files. No call-site changes needed.

---

### Finding 3 — `IResourceViewModelFactory.ApplyViewModel()` is dead code

**Severity: Minor (design debt)**

#### Affected Files

| File | Concern |
|------|---------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Models/IResourceViewModelFactory.cs` | Interface method definition (6 params) |
| `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/Factories.cs` | Two no-op implementations (lines 29, 82) |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/Factories.cs` | Multiple implementations; some no-op (lines 22, 69, 129, 199) |
| `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryFactory.cs` | Partial no-op (line 17) |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRMPrivateDnsARecordFactory.cs` | line 61 |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/RoleManagementPolicyFactory.cs` | line 55 |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/PimEligibleRoleAssignmentFactory.cs` | line 60 |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRMApimNamedValueFactory.cs` | line 30 |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRMApimApiOperationFactory.cs` | line 29 |
| `src/Oocx.TfPlan2Md/Providers/AzureRM/Models/AzureRMApimSubresourceFactory.cs` | line 29 |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` | Call site at line 68 |

#### What's Broken

The majority of `ApplyViewModel` implementations discard all parameters:

```csharp
// AzureDevOps/Models/Factories.cs
public void ApplyViewModel(
    ResourceChangeModel model,
    ResourceChange resourceChange,
    string action,
    IReadOnlyList<AttributeChangeModel> attributeChanges,
    IPrincipalMapper principalMapper,
    IconProviderRegistry? iconProviderRegistry)
{
    _ = principalMapper;
    _ = iconProviderRegistry;
    _ = model;
    _ = action;
    _ = attributeChanges;
    _ = resourceChange;
    // View model is now created on-demand by CreateViewModel
}
```

The design was superseded by on-demand `CreateViewModel`, but the now-empty interface method
was never cleaned up.

**Note:** Some implementations (e.g., `RoleManagementPolicyFactory`, `PimEligibleRoleAssignmentFactory`,
`AzureRMApimNamedValueFactory`, `AzureRMApimApiOperationFactory`, `AzureRMApimSubresourceFactory`,
`AzureRMPrivateDnsARecordFactory`, `AzureAdSummaryFactory`) still do real work inside
`ApplyViewModel`, so they cannot be simply deleted. The correct approach is **Option A**:
remove the interface method and consolidate work into `CreateViewModel` (or dedicated methods
called from the renderer), making the interface coherent.

#### Why It Happened

An interface refactoring was started (migrating from `ApplyViewModel` to `CreateViewModel`)
but the legacy method was never fully removed from the interface or all implementations.

#### Suggested Fix (Option A — preferred)

1. Remove `ApplyViewModel` from `IResourceViewModelFactory`.
2. Move all logic currently in the "live" `ApplyViewModel` implementations into their
   `CreateViewModel` methods or a new dedicated internal method.
3. Remove the call site in `ReportModelBuilder.ResourceChanges.cs` line 68.
4. Delete all `ApplyViewModel` stubs across factories.

---

### Finding 4 — `ClearLineDiffCache()` is never called

**Severity: Minor (latent test-isolation issue)**

#### Affected Files

| File | Detail |
|------|--------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/DiffComputation.cs` | Method defined at line 34; zero call sites |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs` | `Render()` (line 79), `Render(model, templateNameOrPath)` (line 93), `RenderAsync()` (line 120) — none call `ClearLineDiffCache` |

#### What's Broken

`DiffComputation.cs` defines a `[ThreadStatic]` LCS cache that must be cleared after each
render pass, but `ClearLineDiffCache()` has no call sites:

```csharp
// DiffComputation.cs line 34 — only definition, never called
internal static void ClearLineDiffCache() { _lineDiffCache?.Clear(); }
```

The documentation even warns:
> *Must be called after each render pass to prevent unbounded memory growth.*

`MarkdownRenderer.Render()` (line 79) invokes `_reportRenderer.Render(model, context)` but
never clears the cache in a `finally` block.

#### Why It Happened

The `ClearLineDiffCache()` call was originally in `finally` blocks in the Scriban-based
renderer. When Scriban was removed (feature 107), the call was not migrated to the new
pure-C# `MarkdownRenderer`.

#### Suggested Fix

Add `MarkdownHelpers.ClearLineDiffCache()` calls in `finally` blocks inside
`MarkdownRenderer.Render(ReportModel)` and `MarkdownRenderer.Render(ReportModel, string)`:

```csharp
// MarkdownRenderer.cs
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

Apply the same pattern to `Render(ReportModel, string templateNameOrPath)` and (for
completeness) `RenderSummaryTemplate(ReportModel)`.

---

### Finding 5 — `ShouldUseEphemeralOpenFormatting` is a permanent stub

**Severity: Suggestion**

#### Affected Files

| File | Lines |
|------|-------|
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/DefaultResourceRenderer.cs` | 248–253 (method); 58, 65, 87, 137–140, 150, 157, 164, 288, 300, 304 (usages) |
| `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` | `ScenarioRenderContext.IsEphemeralOpenScenario` property |

#### What's Broken

`ShouldUseEphemeralOpenFormatting` always returns `false` and discards its parameter:

```csharp
// DefaultResourceRenderer.cs line 248
private static bool ShouldUseEphemeralOpenFormatting(ResourceChangeModel change)
{
    _ = change;
    return false;
}
```

Its result `useEphemeralOpenFormatting` is used in multiple rendering branches — but because
it always returns `false` (and `ScenarioRenderContext.IsEphemeralOpenScenario` is also always
`false` after removing Finding 1's fingerprint), those branches are permanently dead.

#### Why It Happened

The method is a design scaffold for a feature that was either never implemented or fully
removed without cleaning up the stub and its wiring.

#### Suggested Fix

1. Delete `ShouldUseEphemeralOpenFormatting` from `DefaultResourceRenderer`.
2. Remove the `useEphemeralOpenFormatting` local variable and all branches conditioned on it
   (`ResolveScenarioFormatting`, `ShouldUseMultilineDetailsSummary`,
   `ShouldUseExtraBlankLineBeforeSummary`, `RenderAttributeTable` parameters).
3. Remove `IsEphemeralOpenScenario` from `ScenarioRenderContext` (after Finding 1 removes
   `IsEphemeralOpenCompatibilityScenario`).

---

## Summary of Findings

| # | Title | Severity | Status | Primary File |
|---|-------|----------|--------|--------------|
| 1 | Snapshot-compat heuristics in production rendering | Major | ❌ Not fixed | `ReportRenderer.cs` lines 300–360 |
| 2 | `MarkdownHelpers` declared `public` (spec violation) | Major | ❌ Not fixed | 13 partial-class files |
| 3 | `IResourceViewModelFactory.ApplyViewModel` dead code | Minor | ❌ Not fixed | Multiple factory files |
| 4 | `ClearLineDiffCache()` never called | Minor | ❌ Not fixed | `MarkdownRenderer.cs` |
| 5 | `ShouldUseEphemeralOpenFormatting` permanent stub | Suggestion | ❌ Not fixed | `DefaultResourceRenderer.cs` |

## Suggested Implementation Order

1. **Finding 1 + 5 together** — Removing the two fingerprint methods (Finding 1) makes the
   `IsEphemeralOpenScenario` property fully dead, so both cleanups should be done in one pass.
   Snapshots must be regenerated with `scripts/update-test-snapshots.sh`.
2. **Finding 2** — Independent, low-risk change (access modifier only).
3. **Finding 4** — Independent, low-risk change (add `finally` blocks).
4. **Finding 3** — Most complex (interface + multiple factories + tests); tackle last.

## Related Tests

Tests that should pass (or have their snapshots regenerated) after the fix:

- [ ] All snapshot tests in `src/tests/Oocx.TfPlan2Md.TUnit/` that use
  `azuread-group-members-known-after-apply-plan.json` and `ephemeral-open-plan.json`
- [ ] `PimEligibleRoleAssignmentFactoryTests.ApplyViewModel_SetsSummaryAndSummaryHtml`
  (will need updating if `ApplyViewModel` is removed)
- [ ] `RoleManagementPolicyFactoryTests.ApplyViewModel_SetsSummaryAndSummaryHtml`
- [ ] `AzureRMApimOperationFactoryTests.ApplyViewModel_SetsApiOperationSummaryHtml`
- [ ] `AzureRMPrivateDnsARecordFactoryTests.ApplyViewModel_SetsSummaryAndSummaryHtml`
- [ ] `AzureRMApimNamedValueFactoryTests.ApplyViewModel_*` (three test methods)
- [ ] `AzureRMApimSubresourceFactoryTests.ApplyViewModel_IncludesApiManagementNameInSummary`

## Additional Context

- Source review document: `docs/code-review-top-5-suggestions.md`
  (commit `5da2d8b46bc24fa55446ee5e5ea1ab5a7801dbad`)
- Feature 107 (Scriban removal): `docs/features/107-remove-scriban/`
  — the `ClearLineDiffCache` regression was introduced when Scriban was removed
- Project spec re access modifiers: `docs/spec.md`
- Snapshot update script: `scripts/update-test-snapshots.sh`
