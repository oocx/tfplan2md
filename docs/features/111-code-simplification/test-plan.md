# Test Plan: Code Simplification Refactoring

## Overview

This test plan covers Feature 111 — a pure internal refactoring that eliminates duplicate code,
removes dead code and unused parameters, applies modern C# idioms, and simplifies overly complex
structures. **No user-visible output, CLI options, or API contracts change.**

Because all 22 tasks produce zero change to rendered Markdown output, the authoritative test
strategy is **regression via the existing automated test suite**. No new unit tests or
snapshot baselines are required; the test suite must remain fully green after each task.

Reference: [`docs/features/111-code-simplification/specification.md`](specification.md)

---

## No UAT Required

This feature has no user-facing changes. The specification states explicitly:

> "Users invoking `tfplan2md` will see identical output before and after."

There is nothing to validate visually in a GitHub or Azure DevOps PR comment. No UAT test plan
is produced.

---

## Test Coverage Matrix

Each of the 22 tasks maps to a success criterion from the specification. All criteria are
verified by the existing automated test suite — there is no new functionality to unit-test.

| Finding | Task | Success Criterion | Verification Method |
|---------|------|-------------------|---------------------|
| 2.5 step 1 | Task 1 | `ApplyViewModelContext` record compiles; existing tests pass | Full test suite |
| 2.5 step 2 | Task 2 | `ApplyViewModel` signature changed; no `_ =` discards remain | Full test suite |
| 1.2 step 1 | Task 3 | `AzdoEntityMapper` base class compiles; existing tests pass | Full test suite + AzDO mapper unit tests |
| 1.2 step 2 | Task 4 | Four concrete AzDO mappers derive from base; behaviour unchanged | AzDO mapper unit tests |
| 1.3 | Task 5 | `AzdoFormatterHelper` extracted; four formatter classes are thin wrappers | AzDO value formatter tests |
| 5.3 | Task 6 | `BuildDefinitionRenderer` removed; `AzureDevOpsDelegatingRenderer` is concrete | AzDO snapshot tests |
| 5.2 | Task 7 | `VariableGroupFactory` and `BuildDefinitionFactory` removed | Full test suite |
| 5.1 | Task 8 | `TryGetFactory` on `IResourceViewModelFactoryRegistry`; `ResourceChangeStage` uses interface | Full test suite |
| 2.2 | Task 9 | `ResourceViewModelFactoryRegistry` constructor has no unused params; `#pragma` removed | Full test suite |
| 1.1 | Task 10 | `FormatSummaryBreakdown` removed; `ReportRenderer` delegates to shared helper | Renderer unit tests + snapshot tests |
| 1.4 | Task 11 | `PatternMatchingRegistry<T>.TryResolveFirst` added; registries delegate to it | `PatternMatchingRegistryTests` + value formatter tests |
| 1.5 | Task 12 | `FormatAttributeValuePlain` delegates to `TryFormatSemanticValue`; no duplicate dispatch | Semantic formatting tests |
| 1.6 | Task 13 | `FormatAttributeValue` and `FormatAttributeValueWithResource` wrappers removed | Semantic formatting tests |
| 2.1 | Task 14 | `ShouldUseMultilineDetailsSummary` deleted; call site inlined to `true` | Full test suite |
| 2.3 | Task 15 | `VariableGroupRenderer(LargeValueFormat)` overload removed | Full test suite |
| 2.4 | Task 16 | `MarkdownRenderer` primary constructor has no `principalMapper`; legacy ctor removed | `MarkdownRendererTests` + full test suite |
| 3.1 | Task 17 | Single-pass helper replaces two separate iteration methods in `MatchPattern` | `PatternMatchingRegistryTests` |
| 3.2 | Task 18 | At most one empty dictionary instantiation in `BuildConfigurationReferencesForResource` | Full test suite |
| 3.3 | Task 19 | `BuildReferenceIndex` called once per `Build` invocation; index passed into `ResourceChangeStage` | `ReportModelBuilderStageDelegationTests` + full test suite |
| 4.1 | Task 20 | `ServiceResolutionContext` is a positional record; all construction sites updated | Full test suite |
| 4.2 | Task 21 | `SummaryModel` is a `sealed record`; all construction sites compile | `MarkdownRendererSummaryTests` + full test suite |
| 4.3 | Task 22 | `ActionSummary`, `SummaryModel`, `ResourceTypeBreakdown` are `internal` | Full test suite; no external callers broken |

---

## Test Approach

### Primary Verification: Full Automated Test Suite

Run the complete test suite after each task (or after all tasks, if applied atomically):

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

**Expected result:** All tests pass. The work-protocol records **1186/1186 tests passing**
after all 22 tasks were applied. No snapshot baselines were updated (confirming zero
user-visible output change).

### Key Test Areas

The following test files are the most relevant regression guards for this refactoring. All are
existing tests — none need to be created or modified.

#### AzDO Mapper Tests (Tasks 3–4, Finding 1.2)

| File | Covers |
|------|--------|
| `Providers/AzureDevOps/AzdoGroupMapperTests.cs` | `AzdoGroupMapper` behaviour via base class |
| `Providers/AzureDevOps/AzdoUserMapperTests.cs` | `AzdoUserMapper` behaviour via base class |
| `Providers/AzureDevOps/AzdoProjectMapperTests.cs` | `AzdoProjectMapper` behaviour via base class |
| `Providers/AzureDevOps/AzdoRepositoryMapperTests.cs` | `AzdoRepositoryMapper` behaviour + icon override |

#### AzDO Formatter Tests (Task 5, Finding 1.3)

| File | Covers |
|------|--------|
| `Providers/AzureDevOps/AzdoValueFormatterTests.cs` | All four AzDO ID formatter classes delegating to `AzdoFormatterHelper` |

#### Renderer and Snapshot Tests (Task 10, Finding 1.1)

| File | Covers |
|------|--------|
| `MarkdownGeneration/ReportRendererTests.cs` | `ReportRenderer.RenderSummary` delegates to shared `FormatBreakdown` helper |
| `MarkdownGeneration/MarkdownSnapshotTests.cs` | Full snapshot coverage; confirms no output change |
| `MarkdownGeneration/ComprehensiveDemoTests.cs` | End-to-end demo; confirms no output change |

#### Pattern Matching Registry Tests (Tasks 11, 17, Findings 1.4, 3.1)

| File | Covers |
|------|--------|
| `MarkdownGeneration/PatternMatchingRegistryTests.cs` | `TryResolveFirst` delegation; single-pass specificity helper |

#### MarkdownRenderer and Summary Tests (Tasks 16, 21, Findings 2.4, 4.2)

| File | Covers |
|------|--------|
| `MarkdownGeneration/MarkdownRendererTests.cs` | `MarkdownRenderer` constructor without `principalMapper` |
| `MarkdownGeneration/MarkdownRendererSummaryTests.cs` | `SummaryModel` as `sealed record` |

#### Stage Delegation Tests (Task 19, Finding 3.3)

| File | Covers |
|------|--------|
| `MarkdownGeneration/Stages/` | `ReportModelBuilder` passes pre-built reference index; no double call |
| `MarkdownGeneration/ReportModelBuilderStageDelegationTests.cs` | Stage wiring and index propagation |

---

## Snapshot Regression Check

Snapshot tests are the strongest regression guard for a pure refactoring. The snapshot baselines
must **not** change after any task in this feature.

Relevant snapshot test files:

- `MarkdownGeneration/MarkdownSnapshotTests.cs`
- `MarkdownGeneration/ComprehensiveDemoTests.cs`
- `MarkdownGeneration/AzapiSnapshotTests.cs`
- `MarkdownGeneration/AzureAdSnapshotTests.cs`
- `MarkdownGeneration/AzureDevOpsSnapshotTests.cs`
- `MarkdownGeneration/KnownAfterApplySnapshotTests.cs`
- `MarkdownGeneration/OutputsSnapshotTests.cs`
- `MarkdownGeneration/EphemeralSnapshotTests.cs`
- `MarkdownGeneration/ParentChildUatSnapshotTests.cs`
- `MarkdownGeneration/ParentChildConditionalColumnSnapshotTests.cs`

**Expected result for all snapshot tests:** All baselines match without modification. Any snapshot
update during this refactoring indicates an unintended output change and must be investigated.

---

## Edge Cases

| Scenario | Expected Behaviour | Verification |
|----------|--------------------|--------------|
| AzDO mapper with no diagnostic sink (`null`) | `GetName` resolves correctly; no `NullReferenceException` | Existing mapper unit tests |
| AzDO formatter with empty input | Returns `null`; guard logic in `AzdoFormatterHelper` is exercised | `AzdoValueFormatterTests` |
| `FormatAttributeValuePlain` with input that has no semantic formatting | Returns raw value unchanged (after backtick-strip no-op) | `MarkdownHelpersSemanticFormattingTests` |
| `PatternMatchingRegistry` with no matching pattern | `TryResolveFirst` returns `null`; no exception | `PatternMatchingRegistryTests` |
| `ServiceResolutionContext` positional record construction with `null` optional fields | Compiles and behaves as before | Full test suite |
| `SummaryModel` record equality | Record equality works correctly for summary comparison | `MarkdownRendererSummaryTests` |
| `BuildReferenceIndex` called with `null` pre-built index (test paths) | Falls back to self-computing the index; no regression | Existing stage tests with `null` parameter |

---

## Non-Functional Verification

### No New Compiler Warnings

After all 22 tasks are applied, the build must produce zero new Roslyn analyser warnings. The
`#pragma warning disable IDE0060` suppression in `ResourceViewModelFactoryRegistry.cs`
(Finding 2.2) must be removed — its continued presence would indicate Task 9 was not completed.

### Architecture Boundary Tests

Existing architecture tests guard that no new unintended cross-layer dependencies are introduced:

- `Architecture/ArchitectureBoundaryTests.cs` — verifies layer isolation
- `Architecture/ProviderContributionStructureTests.cs` — verifies no mutable static state

These must continue to pass without modification.

---

## Open Questions

None. All specification open questions were resolved by the Architect in `architecture.md` before
the Developer implemented the tasks. All 22 tasks are complete and verified.
