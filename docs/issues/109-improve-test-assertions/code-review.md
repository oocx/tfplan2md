# Code Review: Improve Test Assertions (Issue #104)

## Summary

Reviewed two commits that replace fragmented `.Should().Contain()` assertion chains on single
string values with precise `.Should().Be()` assertions across 15 test files. No production code
was changed — only test files. This is a pure test quality improvement: each converted pair of
`Contain()` checks becomes a single `Be()` check that pins down the exact rendered output,
turning weak partial-match tests into regression-catching exact-match tests.

The changes are **correct and a strict improvement in test quality**. Every new `Be()` value was
traced against the production formatting code (or confirmed by the passing test suite) and matches
the actual output. No legitimate assertion was weakened.

**Verdict: APPROVED — no issues.**

---

## Verification Results

- **Tests (unit):** ✅ Pass — 1243/1243 passed, 0 failed (`scripts/test-with-timeout.sh --timeout-seconds 300`)
- **Build:** ✅ Success (implicit — tests ran without build errors)
- **Docker:** N/A — test-only change; no functional code modified
- **Snapshot changes:** None — no `.md` snapshot files touched
- **CHANGELOG.md:** ✅ Not modified
- **Production code changes:** None — only `src/tests/` files changed

---

## Specification Compliance

This PR is a test quality improvement, not a feature or bug fix with a formal specification.
The implicit requirement is: *replace 2-assertion `Contain()` chains with a single exact `Be()`
assertion that accurately reflects what the code produces*.

| File | Converted Assertions | Accuracy Verified | Notes |
|------|---------------------|-------------------|-------|
| `ProviderValueFormatterRegistryTests.cs` | 2 → 1 (role def ID) | ✅ | Traced through `RoleDefinitionFormatter` → `GetRoleDefinition` → `fullName` → `FormatCodeTable` |
| `AzureValueFormatterTests.cs` | 2 → 1 (role def override) | ✅ | Same formatting path; short UUID input, `ExtractGuid` succeeds |
| `RoleAssignmentViewModelFactoryTests.cs` (HEAD commit) | 2 → 1 (flat-mapping principal) | ✅ | Flat mapping → no type metadata → no icon → `FormatCodeTable(display)` + bracket ID |
| `RoleAssignmentManagementGroupFormattingTests.cs` | Multiple → Be() | ✅ | Management group scope summary and table attribute; logic traces to `BuildScopeSummary` and `FormatAzureScopeForTable` |
| `RoleAssignmentViewModelFactoryTests.cs` (first commit) | Multiple → Be() | ✅ | Existing passing tests confirm correctness |
| `AzureAdGroupSummaryMemberCountTests.cs` | 4 `Be()` HTML summaries | ✅ | Complex HTML; tests pass confirming exact output |
| `AzureAdGroupSummaryRebuilderTests.cs` | 2 `Be()` HTML summaries | ✅ | Tests pass confirming exact output |
| `ScribanHelpersAzureScopeFormattingTests.cs` | 6 `Be()` scope strings | ✅ | Traced through `FormatAzureScopeForTable` → `FormatAttributeValueTable` → Unicode NBSP icon patterns |
| `ScribanHelpersAzureMetadataTests.cs` | 4 `Be()` on ScriptObject fields | ✅ | `GetRoleInfo` returns `name`, `id`, `full_name` from `RoleDefinitionInfo` record |
| `ScribanHelpersAzdoTests.cs` | 6 `Be()` on mapper outputs | ✅ | Simple known/unknown mapper patterns; `"Name [id]"` or raw id |
| `ScribanHelpersAttributeCollectionTests.cs` | No string `Be()` changes | ✅ | Collection equivalence test; unaffected |
| `SensitivityHierarchyTests.cs` | No string `Be()` changes | ✅ | `BeTrue()`/`BeFalse()` only; unaffected |
| `ParentChildInlineDiffTests.cs` | No conversion (see below) | ✅ | HTML diff output — appropriate to keep `Contain()` |
| `AzureDevOpsDiffFormatterTests.cs` | Partial (null/equal `Be()`; HTML stays `Contain()`) | ✅ | Simple cases converted; multi-property HTML diffs stay as partial checks |
| `ScribanHelpersLargeValueTests.cs` | Multiple `Be()` on code-fence output | ✅ | Simple deterministic string outputs |

**Spec Deviations Found:** None.

---

## Key Assertion Accuracy Verification

### Role Definition Formatting (2 files, same code path)

Input: `"/subscriptions/.../roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7"` or the raw GUID.

Code path:
1. `AzureRoleDefinitionMapper.GetRoleDefinition(id, null)` — extracts GUID via `ExtractGuid`, finds
   "Reader" in the built-in roles map, builds `fullName = "Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)"`
2. `RoleDefinitionFormatter.TryFormat` — constructs `` `🛡️\u00A0Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)` ``
   via `FormatCodeTable($"🛡️{NonBreakingSpace}{roleInfo.FullName}")`
3. `EscapeMarkdown` only escapes `\`, `|`, `` ` ``, `&` — none appear in this string

**Result:** `` `🛡️\u00A0Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)` `` ✅

### Flat-Mapping Principal (No Type Metadata)

Input: `principal_id = "user-123"` mapped to `"user@example.com"` via flat JSON (no `"users":` key).

Code path in `RoleAssignmentViewModelFactory.FormatPrincipalValue`:
1. No `principalType` in flat mapping → `principalIcon = ""`
2. `decoratedName = "user@example.com"` (no type label appended)
3. `nameAndType = "user@example.com"` (no icon prefix; `needsIconPrefix` is `false`)
4. `nameValue = FormatCodeTable("user@example.com")` = `` `user@example.com` ``
5. `idValue = "[`user-123`]"` → full result: `` `user@example.com` [`user-123`] ``

`EscapeMarkdown` does not escape `@` — confirmed. ✅

---

## "Not Converted" Contain Usages — Appropriateness Review

The following `.Should().Contain()` usages were intentionally left as-is in the changed files, and all are appropriate:

| File | Count | Reason |
|------|-------|--------|
| `ParentChildInlineDiffTests.cs` | 70 | Checking for CSS color values, HTML `<code style="...">` attributes, and `<br>` separators within complex dynamically-generated character-level diff HTML. The full HTML output is dozens of characters of interspersed `<span>` tags — `Be()` would produce unmaintainable literals and test implementation details rather than behaviour. |
| `AzureDevOpsDiffFormatterTests.cs` | 12 | Same reason — HTML diff with per-character `<span>` styling. Simple cases (null input → `""`, equal values → `<code>value\*</code>`) were correctly converted to `Be()`. |
| `AzureAdGroupSummaryRebuilderTests.cs` | 3 | Lines 419, 469, 519 check `changes[0].SummaryHtml.Should().Contain("1 👤", ...)`. These three tests each verify a different *ID extraction* strategy (formatted name, backtick-wrapped, HTML code tags) to ensure the count updates correctly. The summary HTML is seeded as `"Test Group | <code>0 👤 0 👥 0 💻</code>"` and the rebuilt value depends on the full rebuilder; a partial `Contain("1 👤")` is the correct scope for what each test asserts. |
| `RoleAssignmentViewModelFactoryTests.cs` | 1 | `viewModel.SummaryText.Should().Contain("👤")` (line 191) is a supplementary check confirming icon presence in the *summary text*, while the primary assertion at line 190 (`principal.After.Should().Be(...)`) already uses exact `Be()` for the table cell value. |
| Same file | 1 | `viewModel.SmallAttributes.Should().Contain(item => item.Name == "scope")` uses the collection overload (predicate), not string substring — correct API. |
| Same file | 3 | Three `NotContain("👤")`, `NotContain("(User)")`, `NotContain("👤")` negative checks. Negative partial assertions are inherently appropriate. |

No cases were found where a `Contain()` check could and should have been converted to `Be()` but was overlooked.

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Role def ID with full path prefix | Pass | `ExtractGuid` strips path; GUID extracted, mapped to "Reader" |
| Role def ID as bare GUID | Pass | Input is already the GUID; same result |
| Flat-mapping principal (no type) | Pass | No icon/type in output; assertion verified against code path |
| Non-breaking space U+00A0 in assertions | Pass | `\u00A0` is the literal `NonBreakingSpace` constant used throughout production code |
| EscapeMarkdown side effects on `@`, `(`, `-` | Pass | None of these characters are escaped; assertions are exact |
| All 1243 tests | Pass | No regressions introduced |

---

## Review Decision

**Status: Approved**

---

## Snapshot Changes

- **Snapshot files changed:** No
- **`SNAPSHOT_UPDATE_OK` token:** N/A

---

## Issues Found

### Blockers

None.

### Major Issues

None.

### Minor Issues

None.

### Suggestions

None — the scope of conversion is complete and correct.  The three retained `Contain()` checks
in `AzureAdGroupSummaryRebuilderTests` for ID extraction scenarios (lines 419, 469, 519) are
arguably candidates for future conversion to `Be()` once the full rebuilt summary format
stabilises, but they are not incorrect as-is.

---

## Critical Questions Answered

- **What could make this code fail?** The `Be()` assertions would fail if the production
  formatting changes (e.g., icon change, non-breaking space removed, parenthesis format altered).
  That is the desired behaviour — they now act as precise regression tests.
- **What edge cases might not be handled?** The conversion covered all 2-assertion `Contain()`
  chains on single string values; no remaining chains were missed in the files listed.
- **Are all error paths tested?** This change does not affect error paths — it only improves
  assertion precision on existing passing tests.
- **Were any assertions weakened?** No. Every `Be()` assertion is strictly stronger than the
  pair of `Contain()` assertions it replaced: it checks the complete output string, including
  formatting characters, Unicode separators, and structural delimiters that the old checks
  ignored.

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ N/A — test quality improvement, no formal spec |
| Code Quality | ✅ |
| Architecture | ✅ No architectural changes |
| Testing | ✅ All 1243 tests pass; assertions are stronger |
| Documentation | ✅ No doc updates required (test-only change) |
| CHANGELOG.md | ✅ Not modified |
| Snapshot changes | ✅ None |
| Production code changed | ✅ None |

---

## Work Protocol & Documentation Verification

- `work-protocol.md` exists: ❌ — `docs/issues/575-improve-test-assertions/` was not created
  prior to this review. This is a process observation: the PR was created directly by the
  Copilot agent as a lightweight quality improvement without a full workflow. The missing
  work-protocol is noted but is not a blocker given the nature of the change (test-only,
  no specification, no risk of regression).
- Global documentation:
  - `docs/features.md`: N/A — test improvement, not a new feature ✅
  - `docs/architecture.md`: N/A — no architectural changes ✅
  - `README.md`: N/A — no CLI/usage changes ✅
  - `docs/testing-strategy.md`: N/A — no new test patterns; this is housekeeping ✅

---

## Next Steps

No blockers or issues. The change is approved and ready to proceed.

**Release Manager** can proceed with release.
