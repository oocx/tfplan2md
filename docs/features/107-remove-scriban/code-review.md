# Code Review: Remove Scriban and Replace with Pure C# Rendering (Round 2)

## Summary

This is a re-review of feature 107 after the Developer addressed all blockers and major issues
found in Round 1. The core refactoring is now substantially complete and correct: all five
blockers from Round 1 have been fixed, most major issues are resolved, and the architecture is
clean. **However, one new critical blocker has been identified: ~80 production and test code
changes from the last two developer sessions (2026-03-04) are present in the working directory
but not committed to the branch.** Until these are committed, the review cannot be approved.

> **Round 1 review findings** are archived in the git history of this file.

---

## Verification Results

| Check | Status | Notes |
|-------|--------|-------|
| Tests | ✅ Pass | 1115 passed, 0 failed, 0 skipped |
| Coverage | ✅ Pass | Line 86.75% (≥84.48%), Branch 78.35% (≥72.80%) |
| Build | ✅ Pass | `dotnet build` succeeded |
| Docker | ✅ Pass | NativeAOT multi-arch image builds successfully (`src/Dockerfile`) |
| Markdownlint | ✅ **0 errors** | `artifacts/comprehensive-demo.md` regenerated and linted clean |
| CHANGELOG.md | ✅ Not modified | Correct |
| Uncommitted changes | ❌ **~80 files** | See Blocker B1 |

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|:-----------:|:------:|-------|
| `Scriban` NuGet package reference removed | ✅ | ✅ TC-S01 | Zero assembly references, confirmed via reflection test |
| All 27 `.sbn` template files deleted | ✅ | ✅ TC-S02 | Zero `.sbn` files in `src/` |
| `AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver` deleted | ✅ | ✅ TC-S02 | Deleted; build is green |
| `TrimmerRootDescriptor.xml` has no Scriban entries | ✅ | ✅ TC-S03 | File cleaned of Scriban entries |
| No C# file imports `using Scriban` or `Scriban.*` | ✅ | ✅ TC-S04 | Zero matches in `src/Oocx.TfPlan2Md/` (working dir + committed) |
| All existing snapshot tests pass | ✅ | ✅ TC-S05 | All 1115 tests pass; snapshot updates justified with `SNAPSHOT_UPDATE_OK` |
| NativeAOT binary builds successfully | ✅ | ✅ TC-S06 | Docker multi-arch build confirmed |
| Zero third-party NuGet references | ✅ | ✅ TC-S01 | Confirmed |
| Rendering logic is statically typed | ⚠️ | Partial | `IScenarioRenderContext` cast pattern persists — see Major M1 |
| Provider modules implement typed `IResourceRenderer` | ✅ | ✅ TC-S07 | All four provider modules register renderers |

**Spec Deviations Found:** None in working code. All major regressions from Round 1 are fixed.

---

## Round 1 Issue Resolution Status

| Issue | Status | Evidence |
|-------|--------|---------|
| B1 — AzDO secret variable exposure | ✅ Fixed | `VariableGroupRenderer` renders `(sensitive / hidden)` for secrets via `VariableGroupFormatters.cs:143` |
| B2 — Summary Refactoring/filtered note missing | ✅ Fixed | `MarkdownRenderer:174` calls `RenderRefactoring`; filtered-resources note at `ReportRenderer:498` |
| B3 — Firewall renderers fell back to default | ✅ Fixed | `FirewallNetworkRuleRenderer` and `FirewallAppRuleRenderer` fully implemented with structured tables |
| B4 — MD031 markdownlint errors | ✅ Fixed | `LargeValues.cs:235` uses `sb.AppendLine("```")`; markdownlint reports 0 errors |
| B5 — Technical Writer docs uncommitted | ✅ Fixed | `README.md`, `docs/architecture.md`, `docs/features.md` committed in `662e50d2` |
| M1 — NsgRenderer unimplemented | ✅ Fixed | Full security-rule table in `AzureRmResourceRenderers.cs` |
| M2 — RoleAssignmentRenderer narrow scope | ✅ Fixed | Handles create/update/delete/replace with enriched summary and attribute diff tables |
| M4 — FirewallAppRuleRenderer unimplemented | ✅ Fixed | Full application-rule table with header line |
| m3 — AzureAD `display_name` icon stripped | ✅ Fixed | Snapshot confirms `` `👤 Jane Doe` `` rendered correctly |

---

## Issues Found

### Blockers

**B1 — ~80 production and test code changes are uncommitted**

The last two developer sessions logged in `work-protocol.md` (both dated 2026-03-04) produced
significant code changes that are currently in the working directory but **not staged or
committed** to the branch.

- **Session 1 ("Scriban-remnant cleanup"):** Modified ~30 production files to remove stale
  Scriban-era comments and update CLI/debug text (`CliParser.cs`, `HelpTextProvider.cs`,
  `DiagnosticContext.cs`, `TemplateResolution.cs`, `MarkdownRenderer.cs`,
  `DefaultResourceRenderer.cs`, `ReportModel.cs`, `ReportModelBuilder.Build.cs`, and many
  more provider files).

- **Session 2 ("ScribanHelpers → MarkdownHelpers rename"):** The entire
  `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/` directory has been replaced
  by an untracked `MarkdownHelpers/` directory. Old `ScribanHelpers*.cs` files show as deleted
  (unstaged). New `MarkdownHelpers*.cs` files are untracked. Six test files were renamed from
  `ScribanHelpers*Tests.cs` to `MarkdownHelpers*Tests.cs` — old files show as deleted,
  new files are untracked.

Running `git status --porcelain | grep -v '^??' | grep '^.' | wc -l` confirms **82 tracked
changes** (excluding submodules and review-generated artifacts). Running `git status --porcelain
| grep '^??' | grep -v uat-repos` shows **8 untracked new source files** (the new MarkdownHelpers
files).

While the working-directory state passes all 1115 tests, a code review must be conducted on
committed code. The current HEAD does not represent the final implementation.

**Fix required:** Commit all working-directory changes to the branch. Two commits (one per
session) matching the work-protocol entries would be cleanest. Include `SNAPSHOT_UPDATE_OK`
in the commit body only if any snapshot files are part of the rename commit.

---

### Major Issues

**M1 — No unit tests for `IScenarioRenderContext` scenario detection logic (carry-over)**

`DefaultResourceRenderer.cs` contains two static heuristic methods —
`ShouldUseKnownAfterApplyFormatting` and `ShouldUseEphemeralOpenFormatting` — plus
`context as IScenarioRenderContext` casts to detect special-case rendering scenarios.
This logic is exercised only implicitly by snapshot tests. No dedicated unit tests verify:
- The exact attribute patterns that trigger `ShouldUseKnownAfterApplyFormatting`
- The exact attribute patterns that trigger `ShouldUseEphemeralOpenFormatting`
- Boundary conditions where neither heuristic fires
- The `IScenarioRenderContext.IsOutputsFocusedReport` flag path

This was flagged as M3 in Round 1; it remains unaddressed.

**Fix required:** Add named unit tests covering each branch of both detection methods and the
context-flag override path. This ensures regressions are detectable if new resource types
accidentally match the heuristic patterns.

---

### Minor Issues

**m1 — Task Planner work-protocol entry missing**

A `task planner.chat.json` exists in `docs/features/107-remove-scriban/`, confirming the Task
Planner was consulted. However, there is no `### Task Planner` entry in `work-protocol.md`.
Per the required-agents matrix in `docs/agents.md`, the Task Planner is required for Feature
workflows and must log their work.

**Fix required:** Add a retrospective Task Planner entry to `work-protocol.md` documenting
that `tasks.md` was produced during that session.

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| AzDO secret variable masking | ✅ Pass | `(sensitive / hidden)` verified in `VariableGroupFormatters.cs` and snapshot |
| Refactoring summary rendering | ✅ Pass | `RenderRefactoring` called; filtered note with plural logic confirmed |
| Firewall rule collection (create/update/delete) | ✅ Pass | Structured tables + collection header confirmed in `AzureRmResourceRenderers.cs` |
| NSG security rules (create/update/delete) | ✅ Pass | Full rule table with all 11 columns confirmed |
| Role assignment enriched summary (create/update/delete) | ✅ Pass | `WriteEnrichedDetailsOpen` and update/delete diff table confirmed |
| AzureAD `display_name` icon | ✅ Pass | `` `👤 Jane Doe` `` in `azuread-snapshot.md:26` |
| MD031 blank lines around fences | ✅ Pass | 0 markdownlint errors on regenerated `comprehensive-demo.md` |
| NativeAOT build | ✅ Pass | Docker multi-arch image builds successfully |
| Large code fence trailing newline | ✅ Pass | `sb.AppendLine("```")` confirmed at `LargeValues.cs:235` |

---

## Snapshot Changes

- Snapshot files changed on this branch: **Yes** — restored baseline after AzApi parity
  fixes; targeted changes for fixes 5, 6, 7, 8, 11, 12.
- `SNAPSHOT_UPDATE_OK` token present:
  - Body of `9df5978c` — snapshot parity fixes 11, 6, 7, 8 ✅
  - Body of `a48b4556` — AzApi array table parity fix ✅
  - Body of `6f517161` — original Scriban removal ✅
- Why snapshot diff is correct: All snapshot changes restore pre-existing rendering behaviors
  that were temporarily regressed. The `58c4568d` revert-to-main commit first established
  the correct baseline, and subsequent commits applied only verified fixes. The regenerated
  `comprehensive-demo.md` passes markdownlint with 0 errors.

---

## Review Decision

**Status: Changes Requested**

The implementation is functionally correct — all Round 1 blockers are resolved, tests pass,
Docker builds, markdownlint passes. However, the branch cannot be approved while ~80
production and test files from the 2026-03-04 developer sessions remain uncommitted.

---

## Checklist Summary

| Category | Status | Notes |
|----------|--------|-------|
| Correctness | ✅ | All provider renderers implemented correctly |
| Spec Compliance | ✅ | All acceptance criteria met in working code |
| Code Quality | ⚠️ | `IScenarioRenderContext` scenario detection lacks unit tests |
| Architecture | ✅ | Scriban fully removed; typed `IResourceRenderer` pattern throughout |
| Testing | ⚠️ | 1115 tests pass; scenario detection boundary tests still missing |
| Documentation | ✅ | README.md, architecture.md, features.md updated and committed |
| All code committed | ❌ | ~80 working-directory changes not committed to branch |

---

## Work Protocol & Documentation Verification

| Item | Status | Notes |
|------|--------|-------|
| `work-protocol.md` exists | ✅ | Present |
| Requirements Engineer logged | ✅ | Entry present |
| Architect logged | ✅ | Entry present |
| Quality Engineer logged | ✅ | Entry present |
| Task Planner logged | ❌ Minor | `task planner.chat.json` exists but no work-protocol entry |
| Developer logged | ✅ | Multiple entries across all sessions |
| Technical Writer logged | ✅ | Entry present |
| Code Reviewer logged | ✅ | Round 1 and Round 2 entries |
| `docs/features.md` updated | ✅ | Updated in `662e50d2` |
| `docs/architecture.md` updated | ✅ | Updated in `662e50d2` |
| `README.md` updated | ✅ | Updated in `662e50d2` |
| CHANGELOG.md NOT modified | ✅ | Correct |

---

## Critical Questions Answered

- **What could make this code fail?**
  The `IScenarioRenderContext` heuristics in `DefaultResourceRenderer` could accidentally
  trigger on future resource types with attributes shaped like `"(known after apply)"` or
  ephemeral values. Low risk currently; fragile without boundary tests.

- **What edge cases might not be handled?**
  All major edge cases are now covered. Scenario detection boundary conditions lack explicit
  coverage.

- **Are all error paths tested?**
  The main rendering paths are thoroughly exercised. `IScenarioRenderContext` detection
  branches have implicit snapshot coverage but no dedicated boundary tests.

---

## Next Steps

The Developer must fix the following before re-review:

1. **[B1 — Blocker]** Commit all working-directory changes from the 2026-03-04 developer
   sessions to the branch. Use one or two commits with clear messages. The rename commit may
   require `git mv` or `git add -A` to capture both deletions and new files correctly.

2. **[M1 — Major]** Add unit tests for `ShouldUseKnownAfterApplyFormatting` and
   `ShouldUseEphemeralOpenFormatting` covering trigger conditions, suppression conditions,
   and boundary values.

3. **[m1 — Minor]** Add a retrospective `### Task Planner` entry to `work-protocol.md`.

After fixes: run full test suite, confirm markdownlint passes on `comprehensive-demo.md`,
and re-request code review.
