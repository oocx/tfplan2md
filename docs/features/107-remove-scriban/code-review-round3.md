# Code Review: Remove Scriban and Replace with Pure C# Rendering (Round 3)

## Summary

This is the third review of feature 107 after the Developer addressed all issues from Round 2.
The three Round 2 findings — B1 (uncommitted changes), M1 (scenario detection tests), and
m1 (Task Planner entry) — are all resolved. Additionally, the Developer went beyond the
original specification scope to remove runtime reflection entirely and introduce a Roslyn
source generator (`JsonEmbedGenerator`) for compile-time JSON embedding, plus MSBuild-generated
`BuildInfo` constants replacing assembly attribute reflection. These additions are
architecturally coherent and valuable for NativeAOT safety.

**All verification checks pass. No blockers found. Recommended decision: Approved.**

> **Round 1 & Round 2 review findings** are archived in the git history of the prior
> `code-review.md` file.

---

## Verification Results

| Check | Status | Notes |
|-------|--------|-------|
| Tests | ✅ Pass | 1122 passed, 0 failed, 0 skipped (7 new scenario tests) |
| Coverage | ✅ Pass | Line 87.74% (≥84.48%), Branch 78.19% (≥72.80%) |
| Build | ✅ Pass | `dotnet build` succeeded |
| Docker | ✅ Pass | NativeAOT multi-arch image builds successfully (`src/Dockerfile`) |
| Markdownlint | ✅ 0 errors | `artifacts/comprehensive-demo.md` regenerated and linted clean |
| CHANGELOG.md | ✅ Not modified | Correct |
| Working tree | ✅ Clean | All changes committed |

---

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|:-----------:|:------:|-------|
| `Scriban` NuGet package reference removed | ✅ | ✅ TC-S01 | Zero PackageReferences in csproj |
| All 27 `.sbn` template files deleted | ✅ | ✅ TC-S02 | Zero `.sbn` files in `src/` |
| `AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver` deleted | ✅ | ✅ TC-S02 | Types no longer exist in codebase |
| `TrimmerRootDescriptor.xml` no Scriban entries | ✅ | N/A | File deleted entirely (no longer needed with `IlcDisableReflection=true`) |
| No C# file imports `using Scriban` or `Scriban.*` | ✅ | ✅ TC-S04 | Zero matches in production or test code |
| All existing snapshot tests pass | ✅ | ✅ TC-S05 | All 1122 tests pass; snapshot updates justified with `SNAPSHOT_UPDATE_OK` |
| NativeAOT binary builds without Scriban trimmer directive | ✅ | ✅ TC-S06 | Docker build confirmed; `IlcDisableReflection=true` now enabled |
| Zero third-party NuGet package references | ✅ | ✅ TC-S01 | Zero `<PackageReference>` elements in main csproj |
| Rendering logic is statically typed | ✅ | ✅ | All renderers use typed `IResourceRenderer`; `IScenarioRenderContext` pattern is interface-based, not string-based |
| Provider modules implement typed `IResourceRenderer` | ✅ | ✅ TC-S07 | All four providers register via `RegisterResourceRenderers` |

**Spec Deviations Found:** None.

---

## Round 2 Issue Resolution Status

| Issue | Severity | Status | Verification |
|-------|----------|--------|-------------|
| B1: ~80 uncommitted changes | Blocker | ✅ Fixed | Committed in `0993b2c5`; working tree clean |
| M1: No `IScenarioRenderContext` unit tests | Major | ✅ Fixed | 7 tests added in `da602551`; `ResolveScenarioFormatting` extracted as `internal static` |
| m1: Missing Task Planner work protocol entry | Minor | ✅ Fixed | Entry present in `work-protocol.md` |

---

## New Changes Since Round 2

Three commits since Round 2 (`0993b2c5`, `2a285718`, `da602551`), introducing:

1. **Reflection removal** (`2a285718`): Replaced assembly attribute reflection with
   MSBuild-generated `BuildInfo` constants; replaced `GetManifestResourceStream` with
   build-time Brotli-compressed JSON payloads via `JsonEmbedGenerator` Roslyn source
   generator; enabled `IlcDisableReflection=true`; deleted `TrimmerRootDescriptor.xml`.

2. **Scenario detection tests** (`da602551`): Added 7 unit tests for
   `DefaultResourceRenderer.ResolveScenarioFormatting`, covering known-after-apply markers,
   ephemeral-open heuristics, boundary conditions, and context flag overrides.

3. **Docker build optimization**: Added `.dockerignore` reducing build context from ~3.39GB
   to ~78KB.

### Assessment of Scope Extension

The reflection removal and `JsonEmbedGenerator` go beyond the original Feature 107 specification
(which focused on Scriban removal). However:

- They are **architecturally coherent** — removing reflection is a natural follow-on to removing
  Scriban, as both reduce runtime dynamism for NativeAOT safety
- The `JsonEmbedGenerator` is a clean, well-structured incremental source generator with proper
  diagnostics (JEG001, JEG002)
- `BuildInfo` generation is a simple MSBuild target that replaces fragile reflection with
  compile-time constants
- No user-facing behavior changes

This extension is acceptable within the feature branch.

---

## Code Quality Assessment

### New Types Reviewed

| Type | LOC | Assessment |
|------|-----|------------|
| `JsonEmbedSourceGenerator` | 332 | Clean Roslyn `IIncrementalGenerator`; proper immutable pipeline records; Brotli compression via runtime type activation for SDK compatibility; diagnostics for error cases |
| `EmbeddedJsonPayloads` | 37 | Thin wrapper with clear mapping from legacy resource names to generated classes |
| `DefaultResourceRendererScenarioTests` | 283 | 7 well-structured tests with custom `ScenarioRenderContext` implementing both `IRenderContext` and `IScenarioRenderContext` |
| `BuildInfo` (generated) | ~10 | MSBuild-generated constants; verified in `AssemblyMetadataProviderTests` |

### Code Patterns

- ✅ `_camelCase` for private fields
- ✅ Modern C# features (pattern matching, expression-bodied members, collection expressions)
- ✅ Immutable records for pipeline values (`EmbeddingCandidate`, `EmbeddingInput`)
- ✅ `internal static` extraction for testability (`ResolveScenarioFormatting`)
- ✅ `InternalsVisibleTo` used for test access (not `public`)
- ✅ Files under 300 lines (except `JsonEmbedSourceGenerator` at 332 — acceptable for a
  self-contained source generator)

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty input (no resources) | ✅ Pass | Covered by existing `Render_EmptyModel_ContainsOnlyHeaderAndSummary` |
| Null scenario context | ✅ Pass | `ResolveScenarioFormatting` tested with explicit null/missing markers |
| Boundary: config ref without marker | ✅ Pass | `AzureAdGroupMember_WithoutConfigReferences_NoScenarioFormatting` test |
| Ephemeral-open heuristic | ✅ Pass | `EphemeralOpen_ChangesWithMarker_SetsEphemeralFormatting` test |
| Context flag overrides | ✅ Pass | `ContextOverrides_WhenBothContextAndMarkers_ContextWins` test |
| Large JSON embedding | ✅ Pass | 5 JSON files embedded via source generator; runtime decompression verified by existing provider tests |
| NativeAOT without reflection | ✅ Pass | Docker build with `IlcDisableReflection=true` succeeds |

---

## Snapshot Changes

- Snapshot files changed: Yes (regenerated via `scripts/update-test-snapshots.sh`)
- Commit message token `SNAPSHOT_UPDATE_OK` present: Yes
- Why the snapshot diff is correct: Snapshots were regenerated to reflect the pure C# rendering
  output which is functionally equivalent to the Scriban template output. Differences are
  limited to formatting normalization (tag badge line breaks, numeric format preservation).
  All changes were previously reviewed and approved in Round 1/Round 2.

---

## Work Protocol & Documentation Verification

### Work Protocol

- ✅ `work-protocol.md` exists with all required feature agents logged:
  Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer (multiple
  sessions), Technical Writer, Code Reviewer (Rounds 1 & 2)
- ✅ UAT Tester not required (test plan explicitly states "No UAT Required" — internal
  refactoring with no visual/CLI behavior changes)

### Global Documentation

| Document | Updated | Notes |
|----------|---------|-------|
| `docs/features.md` | ✅ | Feature 107 section added (lines 2939–2964); rendering section updated with Feature 107 reference |
| `docs/architecture.md` | ⚠️ Partial | Major Scriban removal reflected, but stale `ScribanHelpers/` directory references remain — see Minor m1 |
| `README.md` | ✅ | Updated: "Renderer resolution" replaces "Template resolution"; "Built-in Templates" replaces "Custom Templates" |
| `docs/testing-strategy.md` | ✅ | No new test patterns requiring update |
| `docs/agents.md` | ✅ | No workflow changes |

---

## Review Decision

**Status: Approved**

All 10 acceptance criteria are met. All Round 2 issues are resolved. Tests (1122), coverage
(line 87.74%, branch 78.19%), Docker build, and markdownlint all pass. The scope extension
(reflection removal + JSON embed generator) is architecturally sound and well-implemented.

Three minor documentation issues are noted below for follow-up but do not block approval.

---

## Issues Found

### Blockers

None.

### Major Issues

None.

### Minor Issues

#### m1: Stale `ScribanHelpers/` Directory References in `docs/architecture.md`

The `ScribanHelpers/` directory was renamed to `MarkdownHelpers/` during this feature, but the
architecture document still references the old name in the directory structure listing and
component descriptions:

- Line 253: `└── ScribanHelpers/      # C# helper methods (originally Scriban-registered)`
- Lines 254–258: File listing under `ScribanHelpers/`
- Line 307: `ScribanHelpers/` in Providers directory structure
- Line 460: Component reference
- Line 472: Component reference
- Line 864: Section reference
- Line 1343: Component reference

**Fix:** Replace `ScribanHelpers/` with `MarkdownHelpers/` and remove the "(originally
Scriban-registered)" parenthetical. The ADR references at lines 1500/1504 are historical
records and should remain unchanged.

#### m2: Stale Scriban References in `docs/features.md`

Two references to Scriban-era concepts remain in the features documentation:

- Line 835: "Scriban template JSON context" — should reference the current C# rendering path
- Lines 960–970: Complete Scriban template example (`{{ for module in module_changes }}`) —
  should be replaced with a description of the current `ReportRenderer` module grouping behavior

**Fix:** Update the sensitivity masking bullet and module grouping example to reflect the
current pure C# rendering implementation.

#### m3: `src/Oocx.TfPlan2Md/Providers/README.md` Entirely Describes Obsolete Scriban Architecture

The provider development guide references Scriban throughout — `RegisterHelpers(Scriban.TemplateContext)`,
`ScribanHelpers{ProviderName}`, `.sbn` templates, `ScribanTemplateLoader`, etc. The entire
document describes the pre-Feature-107 architecture.

**Fix:** Rewrite the README to describe the current `IProviderModule.RegisterResourceRenderers`
+ `IResourceRenderer` architecture. This can be done as a follow-up task.

### Suggestions

#### S1: `JsonEmbedSourceGenerator` Exceeds 300-Line Guideline

At 332 lines, this file slightly exceeds the project's 300-line soft limit. Given that it is
a self-contained Roslyn source generator with no external dependencies, this is acceptable.
If it grows further, consider extracting the hex formatting or Brotli compression into
separate helper classes.

---

## Critical Questions Answered

- **What could make this code fail?** The `JsonEmbedGenerator` uses runtime type activation
  for `BrotliStream` (to avoid a direct SDK dependency). If the Brotli types are trimmed or
  unavailable, the generator would fail at build time with a clear diagnostic — this is
  acceptable since it runs at compile time, not runtime.

- **What edge cases might not be handled?** The generator handles missing `EmbedAsJson`
  metadata (JEG001 diagnostic) and unreadable files (JEG002 diagnostic). Empty JSON files
  would produce valid but empty decompressed payloads — downstream consumers would get
  empty collections, which is handled gracefully.

- **Are all error paths tested?** The generator diagnostics are not directly unit-tested (they
  require a Roslyn test harness), but the happy path is thoroughly validated by the existing
  provider tests that consume the generated payloads. The scenario detection paths in
  `DefaultResourceRenderer` are now covered by 7 dedicated tests.

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ✅ |
| Access Modifiers | ✅ |
| Code Comments | ✅ |
| Architecture | ✅ |
| Testing | ✅ |
| Documentation | ⚠️ Minor stale references (m1, m2, m3) |
| Work Protocol | ✅ |

---

## Next Steps

1. **Release Manager** can proceed with PR creation — no blockers or major issues outstanding
2. Minor documentation issues (m1, m2, m3) can be addressed as a follow-up task or during
   the next documentation sweep — they do not affect functionality or user experience
3. No UAT required (confirmed by test plan)

**Next:** Hand off to **Release Manager** to create the pull request.
