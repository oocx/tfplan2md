# Code Review: Remaining Security Findings (Issue 099)

## Summary

Reviewed the implementation of five security/correctness fixes from `analysis.md` (Issues 5, 7, 9, 10, 11): SARIF fail-gate bypass, path traversal in custom template directory, help-URI link injection, HTML injection in summary, and incorrect Terraform action mapping. The implementation is accurate and well-tested. One blocker exists: snapshot files were changed without the required `SNAPSHOT_UPDATE_OK` commit token.

---

## Verification Results

| Check | Result |
|-------|--------|
| Tests | ✅ Pass — 1209/1209 passed, 0 failed |
| Build | ✅ Success |
| Docker | Not checked (Linux host, no Docker available) |
| Workspace errors | None observed |
| Markdownlint (`artifacts/comprehensive-demo.md`) | ⚠️ 1 error — MD024 line 665 ("📦 Module: `module.network`") — **pre-existing on `main`**, not introduced by this PR |
| CHANGELOG.md | ✅ Not modified |
| Snapshot token | ❌ **Missing** — see Blockers |

---

## Specification Compliance

Each issue from `analysis.md` was verified against the implementation:

| Issue | Acceptance Criterion | Implemented | Tested | Notes |
|-------|---------------------|-------------|--------|-------|
| 5 — SARIF fail gate | Warnings cause failure when `--fail-on` set | ✅ | ✅ | `HandleCodeAnalysisFailureAsync` returns `true` on any `Warnings.Count > 0`; gated by `FailOnLevel is not null` |
| 5 — SARIF fail gate | stderr lists failed files | ✅ | ✅ | Each warning's `FilePath` and `Message` written to stderr |
| 7 — Path traversal | Traversal outside `--template-dir` throws | ✅ | ✅ | `Path.GetFullPath` canonicalization + `IsPathWithinRoot` guard in `LoadInternal` |
| 7 — Path traversal | Custom dir stored as full path | ✅ | ✅ | `Path.GetFullPath(customTemplateDirectory)` in constructor |
| 9 — help_uri link | Angle-bracket form used | ✅ | ✅ | Template changed to `[Details](<{{ help_uri | escape_markdown }}>)` |
| 10 — HTML injection | `model.Type` HTML-encoded | ✅ | ✅ | `HtmlEncoder.Default.Encode(model.Type)` |
| 11 — `forget` action | `forget` not classified as `no-op` | ✅ | ✅ | Explicit `ForgetAction` constant and branch in `DetermineAction` |
| 11 — unknown actions | Non-empty unknown sets classified as `unknown` | ✅ | ✅ | Diagnostic warning emitted to stderr; `UnknownAction` returned |
| 11 — empty action list | Empty list still maps to `no-op` | ✅ | ✅ | Explicit `actions.Count == 0` early return |

**Spec Deviations Found:** None

---

## Adversarial Testing

| Test Case | Result | Notes |
|-----------|--------|-------|
| Empty SARIF / parse error with `--fail-on` | ✅ Caught | `Main_WithInvalidSarifAndFailOnSeverity_ReturnsExitCode10` |
| Path traversal via `../` in template include | ✅ Caught | `Loader_WithTraversalInclude_ThrowsAndDoesNotReadOutsideCustomDirectory` throws `InvalidOperationException` |
| `help_uri` with `()` characters | ✅ Caught | `Render_CodeAnalysisFindingsTable_HelpUriWithParentheses_UsesAngleBracketLinks` |
| `model.Type` containing `<script>` | ✅ Caught | `Build_SummaryHtml_EncodesResourceType` — asserts `&lt;script&gt;` |
| `["forget"]` action list | ✅ Caught | `Build_ForgetAction_ActionIsForget` |
| Unknown action set `["future-action"]` | ✅ Caught | `Build_UnknownAction_ActionIsUnknown` |
| `help_uri` containing unencoded `>` | ⚠️ Not tested | `escape_markdown` deliberately does not escape `>`; a URL with a bare `>` would break the angle-bracket link. In practice malformed, but untested. |

---

## Review Decision

**Status: Changes Requested**

One Blocker must be resolved before approval.

---

## Snapshot Changes

| Item | Status |
|------|--------|
| Snapshot files changed | Yes — 2 files |
| `comprehensive-demo-full.md` | `[Details](url)` → `[Details](<url>)` in 3 help-link cells — correct, reflects Issue 9 fix |
| `parent-child-resource-grouping-uat.md` | Same template change in 2 cells — correct |
| `nsg-with-separate-rule-updates.md` | Mentioned in work-protocol as "added", but file was already present on `main` (commit `dbc9c38e` on history); no diff in this PR — no concern |
| `SNAPSHOT_UPDATE_OK` token present | ❌ **No** — neither commit in this branch (`fix: close issue 099 remaining security findings`, `docs: add issue analysis for remaining security findings`) contains the token |

---

## Issues Found

### Blockers

**B1 — `SNAPSHOT_UPDATE_OK` token missing from commit message**

Two snapshot files were modified (`comprehensive-demo-full.md`, `parent-child-resource-grouping-uat.md`) but no commit in this branch includes the required `SNAPSHOT_UPDATE_OK` token in its message.

**Fix:** Amend or add a new commit whose message includes `SNAPSHOT_UPDATE_OK` and briefly explains why the snapshot diff is correct (example: `SNAPSHOT_UPDATE_OK: help_uri links now use CommonMark angle-bracket form per Issue 9 fix`).

Required by: `.github/copilot-instructions.md` — "Include the token `SNAPSHOT_UPDATE_OK` in at least one commit message in the PR and explain why the snapshot changes are correct."

---

### Major Issues

None.

---

### Minor Issues

**m1 — `forget` bucketed into "to change" summary count**

In [ReportModelBuilder.Build.cs](../../src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs#L37):

```csharp
var toChange = BuildActionSummary(allChanges.Where(c => c.Action is "update" or "forget" or "unknown"));
```

`forget` removes a resource from Terraform state management — semantically closer to a delete than a change. The delete icon is correctly assigned for single-resource display, but the header-level summary count lumps it with "update". A reviewer could see "2 items will change" and miss that one is a state-removal. No test covers the summary bucket for `forget` actions.

This is not a spec deviation (the analysis does not specify the bucket), but consider whether `forget` should be counted under "destroy" or as its own bucket. At minimum, add a unit test that verifies the summary count for a `forget` plan.

**m2 — `escape_markdown` not safe for URLs in angle-bracket context**

The template change renders URLs as `[Details](<{{ finding.help_uri | escape_markdown }}>)`. The `escape_markdown` helper intentionally does not escape `>` (see [Markdown.cs comment](../../src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Markdown.cs#L18-L19)). A `help_uri` value containing a bare `>` would close the angle-bracket prematurely and corrupt the link. While malformed in real HTTP URLs, SARIF files could contain such values. No test covers this edge case.

---

### Suggestions

**S1 — Commit message for fix is generic**

`fix: close issue 099 remaining security findings` covers five distinct fixes. If this PR is ever bisected, the message won't help narrow down which fix introduced a regression. Splitting into per-fix commits or adding a body listing the five changes would help.

---

## Critical Questions Answered

- **What could make this code fail?**
  - A SARIF file with warnings is always rejected when `--fail-on` is set, even if those warnings are non-fatal. No opt-out is provided (analysis notes this as acceptable). Risk: overly strict, could cause false-positive CI failures if SARIF tools emit non-critical warnings.
  - A URL containing an unencoded `>` character in `help_uri` breaks the angle-bracket link (see m2).

- **What edge cases might not be handled?**
  - `forget` in combination with other actions (e.g., `["forget", "create"]`) is not explicitly tested. `DetermineAction` would currently reach the `contains(NoOpAction)` → `contains(ForgetAction)` section, and return `ForgetAction` since the `create`+`forget` combination isn't listed as `replace` or other compound.

- **Are all error paths tested?**
  - The SARIF warning path is fully tested end-to-end via `ProgramMainTests`.
  - Path traversal throws an exception — tested via template render.
  - `unknown` action emits a console warning — not explicitly asserted in the test (test only checks `Action` value). Low risk, but the stderr output could be silently lost in future refactors.

---

## Checklist Summary

| Category | Status |
|----------|--------|
| Correctness | ✅ |
| Spec Compliance | ✅ |
| Code Quality | ✅ |
| Architecture | ✅ |
| Testing | ✅ (with minor gaps noted) |
| Documentation | ✅ (fix workflow; no global docs update required) |
| Snapshot token | ❌ Blocker |
| CHANGELOG.md untouched | ✅ |

---

## Work Protocol & Documentation Verification

- `work-protocol.md` exists: ✅
- Required agents (Issue Analyst, Developer) have logged entries: ✅
- Global docs: This is a bug-fix with no user-visible rendering changes other than angle-bracket link formatting. `docs/features.md` does not require update. `docs/architecture.md` does not require update. ✅

---

## Next Steps

1. **Developer:** Add `SNAPSHOT_UPDATE_OK` to the fix commit message (amend the commit or add a new commit), explaining the two snapshot diffs are due to the Issue 9 help-URI angle-bracket template change.
2. **Developer (optional):** Add a test verifying that a `forget` action contributes to the correct summary bucket, and a test for a URL with `>` in `help_uri`.
3. **Code Reviewer:** Re-verify after Blocker is resolved; approve and hand off to Release Manager (no markdown rendering UAT needed — the link format change is cosmetic and angle brackets are standard CommonMark, validated by the existing snapshot tests).
