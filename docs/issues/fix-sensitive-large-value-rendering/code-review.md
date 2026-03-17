# Code Review: Sensitive + Large Output Value Rendering Bugs

## Summary

Reviewed two bug fixes in `ReportRenderer.cs` that address a critical security leak (sensitive large
output values rendered verbatim) and a formatting defect (large JSON output values not pretty-printed).
The fixes are correct, minimal, well-documented with security rationale, and all 1201 tests pass.
The comprehensive demo regenerates without markdownlint errors.

## Verification Results

- **Tests:** Pass — 1201 passed, 0 failed, 0 skipped
- **Build:** Success (included in test run)
- **Docker:** Not run (no Dockerfile changes; core fix is in C# rendering logic)
- **Markdownlint:** 0 errors (`artifacts/comprehensive-demo.md` regenerated)
- **Snapshot Changes:** None — no snapshot files were modified on this branch

## Specification Compliance

| Acceptance Criterion | Implemented | Tested | Notes |
|---------------------|-------------|--------|-------|
| Sensitive large output shows `(sensitive value)` in table cell | ✅ | ✅ | `IsMasked` checked first (line 175) |
| Sensitive large output is NOT rendered in below-table block | ✅ | ✅ | `\|\| output.IsMasked` guard (line 225) |
| Non-sensitive large JSON value is pretty-printed below table | ✅ | ✅ | `FormatLargeOutputValueContent` helper |
| Non-JSON large values continue to render via `ToString()` | ✅ | ✅ (implicitly) | Fallback at line 337 |
| Normal sensitive (non-large) values unaffected | ✅ | ✅ (existing snapshot) | `outputs-sensitive.md` snapshot intact |
| `--show-sensitive` with large sensitive value: renders below table | ✅ | ❌ | See Minor issue M-1 below |

**Spec Deviations Found:** None

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Sensitive + large value (table cell) | Pass | `Render_SensitiveLargeOutput_TableCellShowsSensitiveValue` |
| Sensitive + large value (below-table block omitted) | Pass | `Render_SensitiveLargeOutput_BelowTableBlockOmitted` |
| Large JSON array pretty-printed | Pass | `Render_LargeJsonArrayOutput_BelowTableIsPrettyPrinted` |
| Non-JSON large value fallback (`ToString`) | Pass | Covered by existing `outputs-complex-values.md` snapshot |
| Normal sensitive output unaffected | Pass | Covered by existing `outputs-sensitive.md` snapshot |
| `--show-sensitive` + large sensitive value | Not Tested | See M-1 |
| String-embedded JSON path | Not Tested | See M-2 |
| `null` value input to `FormatLargeOutputValueContent` | Pass | Returns `string.Empty` (line 337) |

## Security Analysis

The security fix is complete and correct within the `RenderOutputTable` method.

**Table-cell path (Bug 1A):** The condition order swap is unambiguous. By checking `IsMasked`
before `IsLargeOutputValue`, any output with `IsMasked = true` is unconditionally routed to the
`"(sensitive value)"` display string and never to `"_(see below)_"`. The fix is minimal — a
two-branch swap — and does not affect any other rendering path.

**Below-table path (Bug 1B):** The guard `if (!output.IsLargeOutputValue || output.IsMasked)`
(line 225) is a correct fix. When `IsMasked = true` the condition evaluates to `true` and the
loop `continue`s — the value is never written to the markdown writer. This is correct for
the default case.

**The `--show-sensitive` interaction:** When the CLI flag `--show-sensitive` is set,
`isMasked = isSensitive && !_showSensitive` evaluates to `false`. With `IsMasked = false`, the
table-cell path correctly falls through to `else if (output.IsLargeOutputValue)` →
`"_(see below)_"`, and the below-table block renders the value. This is the expected and
correct behaviour for explicit opt-in.

**No other leakage paths found:** The `ReportRenderer` has no other method that renders an
output's `Value` property directly. The `TryFormatJsonOutputValue` method (line 346) is only
reached for non-masked, non-large values in the table-cell path, so it is not affected.

## Review Decision

**Status: ✅ Approved**

The security fix is complete, the logic is correct, and the changes are well-documented with
explicit security rationale in inline comments. All tests pass and no regressions are introduced.

## Snapshot Changes

- **Snapshot files changed:** No
- **`SNAPSHOT_UPDATE_OK` token:** N/A

## Issues Found

### Blockers

None.

### Major Issues

None.

### Minor Issues

**M-1: No test for `--show-sensitive` + large sensitive output being rendered below table**

The case where `IsSensitive = true`, `IsMasked = false` (because `--show-sensitive` is set), and
`IsLargeOutputValue = true` is not covered by a test. In this case the fix correctly routes the
table cell to `"_(see below)_"` and renders the value below the table. Adding a test would ensure
that using `--show-sensitive` does not accidentally suppress large sensitive values.

File: `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportRendererTests.cs`

---

**M-2: No test for the string-embedded JSON path in `FormatLargeOutputValueContent`**

`FormatLargeOutputValueContent` (line 308) handles the case where a `JsonElement` of kind
`String` contains embedded JSON (i.e., a JSON string whose content is itself a JSON
object/array). This path is triggered when `IsLargeOutputValue` detected a large embedded JSON
string. The path is untested; a test with a `JsonElement` of kind `String` containing an
embedded object would verify this branch.

File: `src/Oocx.TfPlan2Md/MarkdownGeneration/Rendering/ReportRenderer.cs` (lines 307–333)

### Suggestions

**S-1: Minor code duplication in `FormatLargeOutputValueContent`**

The `MemoryStream` + `Utf8JsonWriter` + `Encoding.UTF8.GetString` pattern is repeated verbatim
in both the `Object/Array` branch and the string-embedded JSON branch. Extracting this to an
`inline` or small private helper (e.g., `SerializeIndented(JsonElement e)`) would reduce
duplication without changing behaviour.

**S-2: Test assertion for pretty-print could be more specific**

The test `Render_LargeJsonArrayOutput_BelowTableIsPrettyPrinted` asserts
`markdown.Should().Contain("\n  {")`. This is a reasonable heuristic, but an assertion on a
fuller multi-line substring (e.g., verifying at least one property key appears on its own
indented line such as `"principal": "user@example.com"`) would make the test more robust
against edge cases in JSON serialiser behaviour.

## Critical Questions Answered

- **What could make this code fail?**
  The only remaining risk is the untested `--show-sensitive` + large sensitive value path
  (M-1), but manual inspection confirms it is correct. The string-embedded JSON path (M-2) has
  a `try/catch (JsonException)` fallback so it cannot throw; it could silently return a plain
  string for malformed embedded JSON, which is acceptable.

- **What edge cases might not be handled?**
  A `null` `Value` in the below-table block is handled via the `value?.ToString() ?? string.Empty`
  fallback. The `FormatLargeOutputValueContent` method covers `JsonElement.Object`, `.Array`,
  `.String` (with embedded JSON), and falls back for all other kinds.

- **Are all error paths tested?**
  The `JsonException` catch in the string-embedded path is not directly tested, but the catch
  block is trivial (fall-through) and represents a well-understood failure mode. All other code
  paths have tests or are covered implicitly by existing snapshot tests.

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Security Fix | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ (with minor gaps noted) |
| Documentation | ✅ |
| No CHANGELOG.md Changes | ✅ |

## Work Protocol & Documentation Verification

| Check | Status | Notes |
|-------|--------|-------|
| `work-protocol.md` exists | ✅ | Present at `docs/issues/fix-sensitive-large-value-rendering/work-protocol.md` |
| Issue Analyst logged entry | ✅ | Entry present |
| Developer logged entry | ✅ | Entry present, including test counts and CodeQL results |
| Technical Writer logged entry | ✅ | Entry present |
| `docs/features.md` updated | ✅ | Sensitive Value Protection section expanded; new Large Output Value Formatting subsection added |
| `docs/architecture.md` update needed | N/A | No architectural changes |
| `README.md` update needed | N/A | No CLI changes |
| `docs/testing-strategy.md` update needed | N/A | No new test patterns introduced |

## Next Steps

The fix is approved. The minor issues (M-1, M-2) and suggestions (S-1, S-2) are optional
improvements — they do not block merge.

**Recommended next agent: Release Manager** — the code is ready for release.
