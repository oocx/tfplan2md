# Code Review: Extensible Provider Registry System

## Summary

This review covers the implementation of feature 061 — an extensible provider registry system that replaces hardcoded icon and formatting logic with a flexible, pattern-matching engine supporting four-dimensional regex matching (provider, resource type, attribute, value). The implementation spans ~115 files with approximately 15 new service files, 4 embedded JSON icon rule files, new Azure AD summary factories, and comprehensive test coverage. The feature is well-implemented: all 849 tests pass, coverage thresholds are met, Docker builds succeed, and the comprehensive demo produces clean Markdown. However, there are several file-size and code-duplication issues that need addressing before approval.

## Verification Results

- **Tests:** Pass (849 passed, 0 failed)
- **Coverage:** Line 86.86% (threshold ≥84.48% ✅), Branch 78.45% (threshold ≥72.80% ✅)
- **Build:** Success
- **Docker:** Builds (`docker build -f src/Dockerfile .` from repo root)
- **Markdownlint:** 0 errors on `artifacts/comprehensive-demo.md`
- **Errors:** Pre-existing SonarQube warnings only; no new workspace errors
- **CHANGELOG.md:** Not modified ✅

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| Service registry supports registration of factories, formatters, and icons | ✅ | ✅ | `PatternMatchingRegistry<T>`, typed wrappers |
| Pattern matching correctly evaluates regex for 4 dimensions | ✅ | ✅ | `MatchPattern` with Compiled regex, 1s timeout |
| Null patterns match all values (wildcard) | ✅ | ✅ | `PatternMatchingRegistryTests` |
| Specificity resolution selects most specific match | ✅ | ✅ | Specificity desc → dimension priority desc → reg. order asc |
| Services can decline and trigger fallback | ✅ | ✅ | `ValueFormatterRegistryTests` |
| Default behavior when no match or all decline | ✅ | ✅ | Falls through to existing hardcoded logic |
| File-based icon provider loads rules from JSON | ✅ | ✅ | `FileBasedIconProvider` + `FileBasedIconProviderTests` |
| File parsing handles errors gracefully | ✅ | ✅ | `ServiceRegistrationException` for invalid regex/JSON |
| Services registered during startup | ✅ | ✅ | `ProgramEntry.cs` wiring; `ProviderIconRegistryTests` |
| Existing functionality continues to work | ✅ | ✅ | Comprehensive demo + existing snapshot tests pass |
| Azure AD snapshot coverage | ✅ | ✅ | `AzureAdSnapshotTests.cs` (new) |
| Azure DevOps snapshot coverage | ✅ | ✅ | `AzureDevOpsSnapshotTests.cs` (new) |
| `get_icon` eliminated from all templates | ✅ | ✅ | Zero occurrences in `.sbn` and `.cs` files |
| Azure AD summaries pre-computed in C# | ✅ | ✅ | `AzureAdSummaryBuilder`/`AzureAdSummaryFactory` |
| `ChangeIcon` on `VariableChangeRowViewModel` | ✅ | ✅ | `variable_group.sbn` uses `var.change_icon` |
| `ActionIcons` centralizes action symbols | ✅ | ✅ | Single source of truth for ➕🔄❌⏺️♻️ |

**Spec Deviations Found:** None material. All success criteria met.

## Architecture Fixes Applied

| Architecture Fix | Status | Notes |
|-----------------|--------|-------|
| Fix #1: `ServiceResolutionContext` → `sealed record` | ✅ Applied | |
| Fix #3: Icon files → embedded resources | ✅ Applied | 4 provider JSON files as `<EmbeddedResource>` |
| Fix #5: `ServiceRegistrationException` → `internal` | ✅ Applied | |
| Fix #6: Dead `GetIcon` removed | ✅ Applied | Zero references |

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty input | Pass | `FormatAttributeValue` returns `string.Empty` for null/whitespace |
| Null values | Pass | Null patterns match all (wildcard). Null registries handled with `?.` |
| Invalid regex in JSON | Pass | `ServiceRegistrationException` thrown, caught by `FileBasedIconProviderTests` |
| Invalid JSON | Pass | `JsonException` propagated, tested |
| No matching service | Pass | Falls through to existing hardcoded logic |
| All services decline | Pass | Iterator exhausts matches, returns null |
| Large plan (comprehensive demo) | Pass | 0 markdownlint errors |

## Snapshot Changes

- **Snapshot files changed:** 2 new files added (`azuread-snapshot.md`, `azuredevops-snapshot.md`)
- **Existing snapshots modified:** None
- **`SNAPSHOT_UPDATE_OK` required:** No (only new additions, no modifications)

## Review Decision

**Status:** Changes Requested

## Issues Found

### Blockers

None.

### Major Issues

#### M1: `AzureAdSummaryBuilder.cs` exceeds 300-line limit (660 lines)

**File:** `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.cs` (660 lines)

This is a brand-new file that exceeds the project's 300-line file-size limit by more than 2×. The file contains 6 resource-type-specific builder methods (`BuildUserSummaryHtml`, `BuildGroupSummaryHtml`, `BuildGroupWithoutMembersSummaryHtml`, `BuildGroupMemberSummaryHtml`, `BuildServicePrincipalSummaryHtml`, `BuildInvitationSummaryHtml`) plus shared helpers (`FormatSummaryValue`, `BuildPrincipalSummary`, `ResolveIcon`, `ResolveMemberTypeIcon`, `BuildMemberCountSummary`, `GetStringProperty`, `GetStringArray`).

**Fix guidance:** Split into a partial class structure:
- `AzureAdSummaryBuilder.cs` — public entry point (`BuildSummaryHtml`) and shared helpers
- `AzureAdSummaryBuilder.Resources.cs` — per-resource-type builder methods
- Alternatively, extract `GetStringProperty`/`GetStringArray` into a `JsonStateReader` utility class since they are generic JSON helpers unrelated to Azure AD

#### M2: `SemanticFormatting.cs` grew from 277 → 495 lines (exceeds limit)

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs`

This file grew by 218 lines (+79%) due to adding registry-aware overloads. The partial-class pattern already exists (`SemanticFormatting.Helpers.cs`, `SemanticFormatting.Identity.cs`), so the new `WithRegistry` methods and delegate types should be extracted.

**Fix guidance:** Extract the 9 new `WithRegistry` methods and their delegate types into `SemanticFormatting.Registry.cs`. This brings the main file back under 300 lines and groups registry-related concerns together.

#### M3: Near-duplicate code in `FormatAttributeValue` / `FormatAttributeValueWithResource`

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs` (lines 249–370)

`FormatAttributeValue` (lines 249–305) and `FormatAttributeValueWithResource` (lines 311–370) are ~60 lines each and differ only in:
1. The extra `resourceType` parameter
2. The icon resolution call (`TryGetRegistryIcon` vs `GetIconWithRegistry`)

All other logic (normalization, semantic formatting, wildcard, IP, location, fallback) is identical.

**Fix guidance:** Consolidate into a single private method with an optional `resourceType` parameter:
```csharp
private static string FormatAttributeValueCore(
    string? attributeName, string? value, string? providerName,
    string? resourceType, ValueFormatContext context,
    IconProviderRegistry? iconProviderRegistry)
```
Then have the public overloads delegate to it with `resourceType: null`.

### Minor Issues

#### m1: `ServiceRegistration<T>` implemented as class, not record

**File:** `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ServiceRegistration.cs`

The architecture document describes it as "Record: (TService, MatchPattern, specificity score)" but it's implemented as `sealed class`. Since it's a simple data carrier (Pattern + Service properties), a `sealed record` would be more consistent with project conventions and provide value equality for free.

#### m2: `EnsureServiceRegistriesInitialized()` is a no-op workaround

**Files:** `ReportModelBuilder.cs` (line 94), `MarkdownRenderer.cs` (line 39)

Both methods read the registry fields with a discard (`_ = _valueFormatterRegistry;`) solely to suppress unused-field analyzer warnings. However, the registries ARE used in `RegisterHelpers()` and `CreateIconProviderRegistry()`. Consider either:
- Adding `#pragma warning disable` for the specific warning, or
- Restructuring the field declarations to avoid the analyzer trigger (e.g., make them non-nullable with a null-object pattern)

#### m3: `azurerm-icons.json` and `azapi-icons.json` are identical copies

**Files:** `src/Oocx.TfPlan2Md/Providers/AzureRM/Icons/azurerm-icons.json`, `src/Oocx.TfPlan2Md/Providers/AzApi/Icons/azapi-icons.json`

Both 73-line JSON files contain identical rules. While the separation follows the architecture of per-provider icon files, the duplication creates a maintenance risk (changes to one must be manually replicated to the other).

**Fix guidance:** Consider sharing a common JSON file loaded by both providers, or adding a comment/test that asserts they stay in sync.

#### m4: Task 5 AC says `get_icon` helper was "introduced" but it was removed by Task 8d

**File:** `docs/features/061-extensible-provider-registry/tasks.md` (Task 5, last AC)

The acceptance criterion `[x] New get_icon helper introduced for explicit icon resolution in templates.` is marked as complete, but the final state has no `get_icon` helper (removed by Task 8d). Update the wording to indicate it was introduced and subsequently removed, or strike the AC with a note pointing to Task 8d.

### Suggestions

#### S1: Extract `GetStringProperty` / `GetStringArray` into a shared utility

`AzureAdSummaryBuilder` contains generic JSON state-reading helpers (`GetStringProperty`, `GetStringArray`) that could be useful for other providers. Consider extracting into a shared `JsonStateReader` utility.

#### S2: Consider marking `BuildMemberCountSummary` parameter count

`BuildMemberCountSummary` takes 8 parameters. While it's a private method, this could be simplified with a small `MemberCounts` record or similar.

## Critical Questions Answered

- **What could make this code fail?** Invalid regex patterns in icon JSON files could throw `ServiceRegistrationException` at startup. This is properly handled and tested. The `RegexOptions.Compiled` with 1-second timeout prevents ReDoS.
- **What edge cases might not be handled?** Overlapping regex patterns across providers are handled by the specificity algorithm. Empty/null states in Azure AD are handled with null-coalescing throughout `AzureAdSummaryBuilder`.
- **Are all error paths tested?** Yes — invalid JSON, invalid regex, missing embedded resources, declining services, and null registries are all tested.

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ❌ (file size limits, code duplication) |
| Access Modifiers | ✅ (all types internal, no unnecessary public) |
| Code Comments | ✅ (XML docs on all members) |
| Architecture | ✅ (all architecture fixes applied) |
| Testing | ✅ (all test plan cases covered) |
| Documentation | ⚠️ (minor: Task 5 AC stale wording) |

## Next Steps

1. **Developer:** Address Major issues M1, M2, M3 (file splits and duplication removal)
2. **Developer:** Fix Minor issues m1 through m4 as feasible
3. **Code Reviewer:** Re-review after rework
4. **UAT Tester:** After re-approval, validate markdown rendering in GitHub/Azure DevOps PRs (user-facing feature)
