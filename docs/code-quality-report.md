# Code Quality Report — `src/Oocx.TfPlan2Md`

> Analyzed: 2026-03-08  
> Scope: `src/Oocx.TfPlan2Md/` (main source only, test files excluded)  
> Status: Approved for implementation

## Summary

| Category | Findings | Priority |
|----------|----------|----------|
| 1. Classes with Too Many Responsibilities | 2 | High, Medium |
| 2. Constructors with Too Many Parameters | 3 | High, Medium, Medium |
| 3. Duplicate Code | 6 | Critical, High, Medium×3, Low |
| 4. Code Complexity | 4 | High, Medium×3 |
| 5. Modern C# Language Features | 6 | Low×6 |
| 6. Implementation Inconsistencies | 5 | Medium×4, Low |

**Total: 22 findings**

---

## Category 1 — Classes with Too Many Responsibilities

### 1.1 `ReportModelBuilder` — God-Object (🔴 High)

**Location:** `MarkdownGeneration/ReportModelBuilder.cs`

**Description:**  
`ReportModelBuilder` is declared as a primary constructor with **19 parameters** (the class explicitly suppresses `#pragma warning disable S107`). It is split across **7 partial files** (`ReportModelBuilder.cs`, `Build.cs`, `CodeAnalysis.cs`, `Outputs.cs`, `ParentChildMerging.cs`, `ResourceChanges.cs`, `Summaries.cs`), but all state (19 private fields) lives in one constructor.

The class is responsible for:
- Building resource change models
- Filtering attributes (case changes, unchanged values)
- Merging parent-child relationships
- Building code-analysis reports
- Building output models
- Managing summary enrichment
- Managing metadata and title rendering
- Managing a view-model factory registry, value formatter registry, icon registry, principal mapper, attribute-change filter registry, and 5 optional stage overrides

**Impact:**  
Adding a new feature requires changes in `CompositionRoot.cs`, `ReportModelBuilder.cs`, and the test harness. Cognitive load is high.

**Recommended Fix:**  
Introduce a `ReportModelBuilderOptions` record that groups the boolean flags and enum options (`showSensitive`, `showUnchangedValues`, `renderTarget`, `reportTitle`, `hideMetadata`, `detailsDisplayMode`, `ignoreAzureIdCaseChanges`), and a `ReportModelBuilderServices` record for injected service dependencies. This reduces the constructor to 2–3 meaningful grouped parameters with named arguments at the call site.

---

### 1.2 Provider Modules Mixing Composition and Business Logic (🟡 Medium)

**Location:** `Providers/AzureDevOps/AzureDevOpsModule.cs`, `Providers/AzureRM/AzureRMModule.cs`

**Description:**  
Provider modules implement multiple narrow interfaces (`IProviderModule`, factory-registration, icon-registration, value-formatter registration, parent-child relationship registration) and carry the business logic for constructing and wiring specific formatters, factories, and mappers. `#pragma warning disable CA1506` suppressions already signal coupling above threshold.

**Impact:**  
Any new resource type added to a provider requires touching the module, growing it further.

**Recommended Fix:**  
Split registration from instantiation. Move resource-specific factory construction into dedicated `*FactoryConfiguration` static classes, and have the module delegate to them.

---

## Category 2 — Constructors with Too Many Parameters

### 2.1 `ReportModelBuilder` Primary Constructor: 19 Parameters (🔴 High)

**Location:** `MarkdownGeneration/ReportModelBuilder.cs:55–77`

**Description:**  
The constructor explicitly suppresses `S107` (too many parameters). All 19 parameters have defaults, which mitigates call-site pain but obscures the true coupling.

**Maintainer Preference:** Use named arguments at call sites for clarity. Group related parameters into records where possible.

**Recommended Fix:**  
Introduce:
```csharp
internal sealed record ReportModelBuilderOptions(
    bool ShowSensitive = false,
    bool ShowUnchangedValues = false,
    RenderTarget RenderTarget = RenderTarget.Generic,
    string? ReportTitle = null,
    bool HideMetadata = false,
    DetailsDisplayMode DetailsDisplayMode = DetailsDisplayMode.Default,
    bool IgnoreAzureIdCaseChanges = false);

internal sealed record ReportModelBuilderServices(
    ResourceSummaryBuilder SummaryBuilder,
    IPrincipalMapper PrincipalMapper,
    ProviderRegistry ProviderRegistry,
    IProviderContributionSet ProviderContributions,
    CodeAnalysisInput? CodeAnalysisInput,
    IIconProviderRegistry IconProviderRegistry,
    IAttributeChangeFilterRegistry AttributeChangeFilterRegistry);
```

---

### 2.2 `CompositionRoot.CreateProviderRegistry`: 8 Parameters (🟡 Medium)

**Location:** `CompositionRoot.cs:139–160`

**Description:**  
Eight positional parameters, all Azure-specific mappers. Adding a new mapper type (as has happened for repositories) requires adding another parameter in three places.

**Recommended Fix:**  
Introduce an `AzdoMapperSet` value object and an `AzureMapperSet` for Azure-specific parameters. Use named arguments at all call sites.

---

### 2.3 `CompositionRoot.CreateReportModelBuilder`: 14 Named Arguments (🟡 Medium)

**Location:** `CompositionRoot.cs:196–226`

**Description:**  
The constructor call spans 14 named argument lines because positional order is unmanageable. Resolved by the fix in 2.1.

---

## Category 3 — Duplicate Code

### 3.1 `ResolveActiveState` Duplicated in 7 Factory Files (🔴 Critical)

**Location:**

| File | Line |
|------|------|
| `Providers/AzureAD/Models/AzureAdSummaryBuilder.cs` | L128 |
| `Providers/AzureRM/Models/AzureRMPrivateDnsARecordFactory.cs` | L89 |
| `Providers/AzureRM/Models/RoleManagementPolicyFactory.cs` | L103 |
| `Providers/AzureRM/Models/PimEligibleRoleAssignmentFactory.cs` | L125 |
| `Providers/AzureRM/Models/AzureRMApimNamedValueFactory.cs` | L44 |
| `Providers/AzureRM/Models/AzureRMApimApiOperationFactory.cs` | L45 |
| `Providers/AzureRM/Models/AzureRMApimSubresourceFactory.cs` | L45 |

Every instance is word-for-word identical:
```csharp
private static object? ResolveActiveState(ResourceChange resourceChange, string action)
{
    var state = action == "delete" ? resourceChange.Change.Before : resourceChange.Change.After;
    return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
}
```

**Impact:**  
Any bug fix or logic change must be applied to all 7 copies. Risk of divergence is high.

**Recommended Fix:**  
Add a single `static object? ResolveActiveState(ResourceChange, string)` method to a new `ResourceChangeHelpers` static utility class, and have all factories call it.

---

### 3.2 `EscapeMarkdown` Duplicated in Both Diff Formatters (🔴 High)

**Location:**
- `RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs:106–131`
- `RenderTargets/GitHub/GitHubDiffFormatter.cs:68–93`

Both classes contain an **identical 20-line private static** `EscapeMarkdown(string?)` method.

**Maintainer Preference:** Extract as a **string extension method** so it can be called as `myString.EscapeMarkdown()`.

**Recommended Fix:**  
Create a `StringMarkdownExtensions` static class with:
```csharp
public static string EscapeMarkdown(this string? value) { ... }
```
Remove the private static copies from both diff formatters.

---

### 3.3 `GetValue` Duplicated in 2 Factories (🟡 Medium)

**Location:**
- `Providers/AzureRM/Models/RoleManagementPolicyFactory.cs:181`
- `Providers/AzureRM/Models/PimEligibleRoleAssignmentFactory.cs:176`

Identical one-liner:
```csharp
private static string? GetValue(Dictionary<string, string?> state, string key)
    => state.TryGetValue(key, out var value) ? value : null;
```

**Recommended Fix:**  
Move to a shared utility class (e.g., `FlatStateHelpers`) referenced by both factories.

---

### 3.4 Change-Label Constants + Helpers Duplicated (🟡 Medium)

**Location:**
- `Providers/AzureDevOps/Models/VariableGroupFormatters.cs:21–39`
- `Providers/AzureDevOps/Models/BuildDefinitionFormatters.cs:23–38`

Both classes independently define `AddedChange`, `RemovedChange`, `UnchangedChange`, `ModifiedChange` constants, and identical `ConvertBoolToString` and `FormatOptionalString` methods.

**Recommended Fix:**  
Extract to a `ChangeLabels` static class and a `FormatterHelpers` static class in the same namespace.

---

### 3.5 Principal-Type Icon Mapping Duplicated 3× (🟡 Medium)

**Location:**
- `MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.Identity.cs` — `TryFormatPrincipalType`
- `Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs` — `FormatPrincipalIdValue`
- `Providers/AzureRM/Models/PimEligibleRoleAssignmentFactory.cs` — `FormatPrincipalSummary`

All three encode the same mapping: `"User"→👤`, `"Group"→👥`, `"ServicePrincipal"→💻`.

**Recommended Fix:**  
Centralise in `SemanticFormatting.Identity.cs` as a single `public static string GetPrincipalIcon(string? principalType)` method.

---

### 3.6 Summary Table Rows Re-Implemented in `ReportRenderer` (🟢 Low)

**Location:**
- `MarkdownGeneration/Rendering/SummaryRenderer.cs:29–49`
- `MarkdownGeneration/Rendering/ReportRenderer.cs:68–90`

`ReportRenderer.RenderSummary` re-implements the full table instead of delegating to `SummaryRenderer.Render` (which already accepts a `boldTotal` parameter).

**Recommended Fix:**  
Remove the inline re-implementation from `ReportRenderer.RenderSummary` and delegate to `SummaryRenderer.Render`.

---

## Category 4 — Code Complexity

### 4.1 `DefaultResourceRenderer.Render` — 12 Inline Phases (🔴 High)

**Location:** `MarkdownGeneration/Rendering/DefaultResourceRenderer.cs:50–100`

**Description:**  
The `Render` method carries two suppressed warnings (`CA1502` and `S3776`). It performs 12 distinct phases inline:
1. `detailsTag` selection (3-way switch)
2. `summary` string construction
3. Policy resolution
4. 4 different Raw HTML writes
5. Code-analysis metadata rendering
6. Attribute splitting (small/large)
7. Small-attribute table rendering
8. Tags badges rendering
9. "No attribute changes" empty-state logic
10. Child resource rendering
11. Code-analysis finding rendering
12. Large attribute rendering

**Recommended Fix:**  
Extract each phase into a small named private method (`WriteDetailsHeader`, `WriteSmallAttributeSection`, `WriteNoChangesMessage`, `WriteChildResourceSection`, etc.), making `Render` a clean orchestrator.

---

### 4.2 `RenderCodeAnalysisMetadata` — 5-Pass Severity Iteration (🟡 Medium)

**Location:** `MarkdownGeneration/Rendering/DefaultResourceRenderer.Helpers.cs:12–55`

**Description:**  
Five separate `.Count()` calls — one per severity level — iterate the collection 5 times.

**Recommended Fix:**  
Single `GroupBy` pass to build a count-per-severity dictionary, then iterate a canonical ordered severity list.

---

### 4.3 `RenderChildResources` — Hardcoded Column Separator (🟡 Medium)

**Location:** `MarkdownGeneration/Rendering/DefaultResourceRenderer.Helpers.cs:95–140`

**Description:**  
A hardcoded check for group label `"Security Rules"` with a 12-column separator string not derived from actual column count.

**Recommended Fix:**  
Derive separator from actual column count (`group.Columns`), removing the hardcoded special case.

---

### 4.4 `TryFormatSemanticValue` — 12-Step Sequential Chain (🟡 Medium)

**Location:** `MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.cs:118–180`

**Description:**  
12 sequential `TryFormat*` checks — open/closed principle violation. Every new semantic format grows the chain.

**Recommended Fix:**  
Register semantic formatters in a static list ordered by priority and dispatch via a loop.

---

## Category 5 — Modern C# Language Features

### 5.1–5.2 `new[] { }` → Collection Expressions (🟢 Low)

**Location:** `RoleAssignmentViewModelFactory.cs:546–555`, `AzureRMModule.cs:185,199,223`

Replace `new[] { ... }` with C# 12 collection expression syntax `[...]`.

---

### 5.3 `.Count() == 1` on `IGrouping` → `.Skip(1).Any()` (🟢 Low)

**Location:** `MarkdownGeneration/ReportModelBuilder.ParentChildMerging.cs:136`

`IGrouping<K,V>` doesn't implement `ICollection`, so `.Count()` causes full LINQ enumeration. Use `!labelGroup.Skip(1).Any()` instead — this short-circuits after the second element and means "has at most one element" (equivalent to `Count() == 1` for non-empty groupings, without materialising the whole sequence).

---

### 5.4 `List<T>.Find` → `FirstOrDefault` (🟢 Low)

**Location:** `Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs:568–580`

`List<T>.Find` is less idiomatic than LINQ `FirstOrDefault` in a codebase that otherwise uses LINQ consistently.

---

### 5.5 Multi-Condition `||` Chains → `is not (... or ...)` Patterns (🟢 Low)

**Location:** `MarkdownGeneration/Helpers/MarkdownHelpers/SemanticFormatting.Identity.cs`

Multiple methods have 3-condition OR chains on string comparisons. These can be simplified using C# `or` patterns or a static array with `.Contains`.

---

### 5.6 Null-Guard Ternary Patterns → Pattern Matching (🟢 Low)

**Location:** `Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs` (~20 occurrences)

Pattern `!IsNullOrEmpty(x) ? Format(x) : ""` can be simplified to `x is { Length: > 0 } n ? Format(n) : ""`.

---

## Category 6 — Implementation Inconsistencies

### 6.1 Mixed `#pragma warning disable` vs `[SuppressMessage]` for CA1506 (🟡 Medium)

**Location:** `CompositionRoot.cs`, `Program.cs`, `ProgramEntry.cs`, `AzureDevOpsModule.cs`, `BuildDefinitionFormatters.cs`, `BuildDefinitionViewModelFactory.cs`

Some files use `[SuppressMessage]` with justification; others use file-scoped `#pragma` with no justification.

**Recommended Fix:** Standardise on `[SuppressMessage]` with `Justification` for all CA1506 suppressions.

---

### 6.2 `IsNullOrEmpty` vs `IsNullOrWhiteSpace` Interchangeable (🟡 Medium)

**Location:** `Providers/AzureRM/Models/RoleAssignmentViewModelFactory.cs`

Both methods used interchangeably for the same semantic intent within the same class. Whitespace-only values would behave differently.

**Recommended Fix:** Standardise on `IsNullOrWhiteSpace` for display/formatting code where whitespace is meaningless.

---

### 6.3 Named vs Positional Arguments Inconsistent (🟢 Low)

**Location:** `CompositionRoot.cs:159–171`

Some module constructors use named arguments, some use positional.

**Maintainer Preference:** Use named arguments for all constructors with 2+ parameters (unless obvious).

---

### 6.4 `VariableGroupFormatters` and `BuildDefinitionFormatters` Share No Code (🟡 Medium)

**Location:** `Providers/AzureDevOps/Models/`

Identical structure but no shared base or utility class. Mask text `(sensitive / hidden)` already duplicated.

**Recommended Fix:** Extract shared helpers into `FormatterHelpers` static class.

---

### 6.5 Role Assignment Attribute Constants Duplicated Across 3 Files (🟡 Medium)

**Location:** `RoleManagementPolicyFactory.cs`, `PimEligibleRoleAssignmentFactory.cs`, `RoleAssignmentViewModelFactory.cs`

`role_definition_id`, `role_definition_name`, `principal_id`, `principal_type` constants defined independently.

**Recommended Fix:** Centralise in an `AzureRoleAssignmentAttributes` static class.

---

## Implementation Order

Based on impact and risk, the recommended implementation order is:

1. **3.1** — Extract `ResolveActiveState` (7 copies → 1 shared utility, pure refactoring)
2. **3.2** — Extract `EscapeMarkdown` as string extension method
3. **6.5 + 3.3** — Centralise role attribute constants + `GetValue` helper
4. **3.4 + 6.4** — Extract AzureDevOps formatter shared helpers
5. **3.5** — Centralise principal-type icon mapping
6. **2.1 + 1.1** — Introduce `ReportModelBuilderOptions`/`ReportModelBuilderServices` records
7. **4.1** — Decompose `DefaultResourceRenderer.Render` phases
8. **4.2** — Single-pass severity counting
9. **4.3** — Dynamic column separator
10. **5.x** — Modern C# language features
11. **6.x** — Remaining inconsistencies (suppression style, IsNullOrEmpty/IsNullOrWhiteSpace)
