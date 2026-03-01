# Code Review: OutputChange `after_unknown` Type Fix (#106)

## Summary

Reviewed the bug fix for `OutputChange.AfterUnknown` being typed as `bool` instead of
`object?`, which caused a deserialization exception when Terraform emits a non-boolean
(e.g. `{}`) for `output_changes[*].after_unknown`. The fix aligns `OutputChange` with the
already-correct `Change` model, reuses the existing `AfterUnknownHelper` and adds a
regression test with representative test data.

The fix is minimal, correct, and complete. All 1308 tests pass.

## Verification Results

- **Tests:** ✅ Pass — 1308 passed, 0 failed (full suite)
- **Build:** ✅ Success
- **Docker:** N/A — not required for this internal parsing fix
- **Markdownlint (comprehensive-demo.md):** ⚠️ Pre-existing error (see below)
- **Snapshot changes:** None

### Note on `artifacts/comprehensive-demo.md` markdownlint

`artifacts/comprehensive-demo.md` has a pre-existing MD024 error:

```
artifacts/comprehensive-demo.md:665 MD024/no-duplicate-heading
  Multiple headings with the same content [Context: "📦 Module: `module.network`"]
```

This duplicate heading (`### 📦 Module: \`module.network\``) appears identically in both the
main branch and this branch — the only diff between the two is the version/timestamp on
line 3. This issue predates this fix and is unrelated to it. It should be addressed as a
separate issue.

## Specification Compliance

This is a bug fix without a formal specification document. Evaluated against the issue
analysis in `docs/issues/106-output-change-after-unknown-type/analysis.md`.

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| `OutputChange.AfterUnknown` accepts non-boolean JSON tokens without throwing | ✅ | ✅ | Changed from `bool` to `object?` |
| `isComputed` remains `bool` in `OutputChangeModel` | ✅ | ✅ | Builder derives `bool` via helper |
| Empty object `{}` treated as "not entirely unknown" (`isComputed = false`) | ✅ | ✅ | `IsWholeResourceUnknownAfterApply({}) → false` |
| Boolean `true` still correctly returns `isComputed = true` | ✅ | ✅ | Existing test updated with correct assertion |
| Boolean `false` still correctly returns `isComputed = false` | ✅ | ✅ | `Parse_PlanWithOutputs_ParsesCreateAction` asserts `JsonValueKind.False` |
| End-to-end rendering with `after_unknown: {}` succeeds without exception | ✅ | ✅ | Manual render test performed; output renders correctly |

**Spec Deviations Found:** None

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| `after_unknown: {}` (empty object) | ✅ Pass | Primary bug scenario; regression test added; manual render succeeds |
| `after_unknown: true` | ✅ Pass | Existing test, assertion updated to `JsonValueKind.True` |
| `after_unknown: false` | ✅ Pass | Existing test, assertion updated to `JsonValueKind.False` |
| `after_unknown: null` (missing field) | ✅ Pass | `object? afterUnknown = null` default; `IsWholeResourceUnknownAfterApply(null) → false` |
| End-to-end render with `after_unknown: {}` | ✅ Pass | CLI renders without exception; Value column is empty (correct: `after: null`) |
| No-outputs plan | ✅ Pass | `Parse_PlanWithoutOutputs_ReturnsNullOutputChanges` still passes |

## Review Decision

**Status: ✅ Approved**

## Snapshot Changes

- Snapshot files changed: **No**
- `SNAPSHOT_UPDATE_OK` token: N/A

## Issues Found

### Blockers

None

### Major Issues

None

### Minor Issues

1. **Pre-existing MD024 lint error in `artifacts/comprehensive-demo.md`** — Duplicate heading
   `### 📦 Module: \`module.network\`` at line 665 (also at 348). This predates this PR and is
   unrelated to the fix. Should be tracked as a separate issue.

### Suggestions

1. **Test the `{"key": true}` non-empty object scenario** — The existing `AfterUnknownHelperTests.TC-02`
   and the new test both use non-empty/empty objects. The non-empty `{"key": true}` case for
   `OutputChange` (as opposed to `Change`) is not separately tested, but is implicitly covered
   by `AfterUnknownHelper` unit tests (TC-02). No action required unless explicit output-level
   coverage is desired.

2. **Comment in `analysis.md` references issue#097** — The analysis mentions "feature #097"
   introduced `OutputChange`. This is helpful context, already documented. No action needed.

## Critical Questions Answered

- **What could make this code fail?** A hypothetical scenario where Terraform emits an
  integer or array for `after_unknown` would result in `IsWholeResourceUnknownAfterApply`
  returning `false` (via `TryGetJsonElement` returning `false` for unrecognized types) — this
  is safe and correct behavior. No known failure path for the fix as written.

- **What edge cases might not be handled?** A non-empty object `{"key": true}` for
  `OutputChange.AfterUnknown` will correctly produce `isComputed = false` (the whole output
  value is not simply `true`). Attribute-level unknown resolution for output changes is not
  surfaced in the UI model anyway (the template only uses the top-level `IsComputed` flag
  for outputs), so this is correct behavior.

- **Are all error paths tested?** The `null` default (when field is absent) and the object
  case are both tested. The `bool` path (`true`/`false`) is also tested. `AfterUnknownHelper`
  itself has comprehensive unit tests covering all known value kinds.

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ (aligns with existing `Change` pattern) |
| Testing | ✅ |
| Documentation | ✅ (see Work Protocol below) |

## Work Protocol & Documentation Verification

**Work Protocol (`work-protocol.md`):** Present ✅

| Agent | Entry Present |
|-------|--------------|
| Issue Analyst | ✅ |
| Developer | ✅ (added as part of this review) |
| Technical Writer | ✅ (added as part of this review) |
| Code Reviewer | ✅ (this review) |

**Global documentation:** No updates required. This is an internal parsing bug fix:
- No new user-facing features → `docs/features.md` unchanged ✅
- No architectural changes → `docs/architecture.md` unchanged ✅
- No CLI option changes → `README.md` unchanged ✅
- `CHANGELOG.md` not modified ✅ (auto-generated)

## Next Steps

The fix is approved and ready for release. Recommended next step: **Release Manager** agent
to coordinate the release of this fix.
