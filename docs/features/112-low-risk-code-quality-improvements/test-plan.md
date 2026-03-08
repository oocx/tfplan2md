# Test Plan: Shared Diff Formatter Markdown Escaping Refactoring

## Overview

This test plan covers the Feature 112 implementation slice selected in
[`architecture.md`](architecture.md): extracting the duplicated markdown-escaping logic from
`GitHubDiffFormatter` and `AzureDevOpsDiffFormatter` into one shared internal helper under
`src/Oocx.TfPlan2Md/RenderTargets/`.

Feature 112 is an internal, behavior-preserving refactoring. The expected outcome is improved
maintainability with **no change** to CLI behavior, rendered markdown output, dependency footprint,
or render-target-specific formatting rules.

Relevant requirements:

- [`specification.md`](specification.md)
- [`architecture.md`](architecture.md)
- Existing regression guards in:
  - `src/tests/Oocx.TfPlan2Md.TUnit/RenderTargets/GitHubDiffFormatterTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/RenderTargets/AzureDevOpsDiffFormatterTests.cs`
  - `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/DiffComputationPerformanceTests.cs`

---

## No UAT Required

This implementation slice is not a user-facing feature. It intentionally preserves the current
GitHub and Azure DevOps rendering behavior rather than introducing a new rendering format or visual
change. A feature-specific UAT plan is therefore **not required** for this refactoring.

---

## Test Coverage Matrix

| Success Criterion | Test Case(s) | Verification Type |
|-------------------|--------------|-------------------|
| A prioritized and bounded set of low-risk code-quality changes is selected for implementation | TC-01 | Scope review |
| The selected work is based on a fresh pass over current code-quality hotspots rather than a previous pass | TC-01 | Artifact review |
| At least one meaningful hotspot involving duplication, excessive responsibility, unnecessary complexity, or implementation inconsistency is reduced or removed | TC-01, TC-08 | Scope review + regression |
| Any class or constructor targeted by this feature has clearer responsibility boundaries or simpler dependency flow after the change | TC-01, TC-08 | Scope review + regression |
| Any modern language features used come from the existing project stack and do not require new framework or package dependencies | TC-08 | Build/regression review |
| The resulting changes preserve user-visible behavior, including CLI usage and rendered markdown output | TC-02, TC-03, TC-04, TC-05, TC-06, TC-07, TC-08 | Automated unit/performance/regression |
| The resulting code is more consistent with existing project conventions and easier to review than before | TC-01, TC-08 | Scope review + regression |
| The final change set remains small and surgical enough to review, validate, and revert safely if needed | TC-01 | Scope review |

---

## Existing Relevant Tests

The current test suite already provides focused regression coverage for the two affected formatters:

| File | Current Coverage |
|------|------------------|
| `RenderTargets/GitHubDiffFormatterTests.cs` | Null/null guard, unchanged-value inline code formatting, changed-value GitHub simple diff formatting with markdown escaping |
| `RenderTargets/AzureDevOpsDiffFormatterTests.cs` | Null/null guard, unchanged-value inline code formatting, short-value HTML fast path, special-character short diffs, long-value character-level diff path |
| `MarkdownGeneration/DiffComputationPerformanceTests.cs` | Azure DevOps large-value performance guard to prevent LCS blowups |

These tests are the primary starting point for validating the refactoring. The recommended additions
below are intentionally minimal and target only the behavior exercised by the extracted helper.

---

## Minimal Targeted Tests Recommended

The refactoring should stay behavior-preserving and should only require small, focused test updates
if the helper extraction is not already fully pinned by existing tests.

1. **Keep both existing formatter test files as the primary regression guard.**
2. **Add or strengthen one equal-value escape test per formatter** using a single string that
   contains the full shared escape set exercised by the helper:
   - backslash `\`
   - backtick `` ` ``
   - asterisk `*`
   - underscore `_`
   - braces `{ }`
   - brackets `[ ]`
   - parentheses `( )`
   - hash `#`
   - plus `+`
   - minus `-`
   - period `.`
   - exclamation `!`
   - pipe `|`
3. **Keep one GitHub changed-value diff test** that asserts the exact
   `- before<br>+ after` output with escaped markdown characters.
4. **Keep one Azure DevOps short-value fast-path test** that confirms the HTML wrapper, line
   backgrounds, prefixes, and raw short values remain unchanged.
5. **Keep one Azure DevOps long-value test** that confirms the refactoring does not alter the
   character-level inline diff path.
6. **Keep the existing large-value performance test** to ensure the helper extraction does not
   accidentally route large inputs into slower behavior.

### Recommended Minimal Automated Test Set

| Priority | Test | Purpose |
|----------|------|---------|
| Required | `GitHubDiffFormatterTests.FormatDiff_WhenValuesAreEqualWithMarkdownSpecialCharacters_ReturnsEscapedInlineCode` | Pins shared escaping behavior through the GitHub unchanged-value path |
| Required | `GitHubDiffFormatterTests.FormatDiff_WhenValuesDifferWithMarkdownSpecialCharacters_UsesSimpleDiffWithoutBackticks` | Pins shared escaping behavior through the GitHub changed-value path |
| Required | `AzureDevOpsDiffFormatterTests.FormatDiff_WhenValuesAreEqualWithMarkdownSpecialCharacters_ReturnsEscapedInlineCode` | Pins shared escaping behavior through the Azure DevOps unchanged-value path |
| Required | `AzureDevOpsDiffFormatterTests.FormatDiff_WhenShortValuesDiffer_UsesHtmlInlineDiffFastPath` | Confirms short-value Azure DevOps behavior is unchanged |
| Required | `AzureDevOpsDiffFormatterTests.FormatDiff_WhenLongValuesDiffer_UsesCharLevelDiff` | Confirms full LCS/inline-diff behavior is unchanged |
| Required | `DiffComputationPerformanceTests.FormatDiff_AzureDevOps_WithLargeValues_CompletesWithinTimeLimit` | Preserves the large-value performance guard |

**Recommendation:** Prefer validating the shared helper indirectly through the public
`FormatDiff` methods rather than adding a dedicated helper test class. That keeps the helper
internal, avoids widening the feature scope, and proves the formatter contracts remain unchanged.

---

## Test Cases

### TC-01: Refactoring Scope Remains Surgical

**Type:** Scope review

**Description:** Verify that the implementation matches the architected slice and does not expand
past the two diff formatters plus one shared internal helper.

**Preconditions:**
- Feature 112 implementation is available for review.
- The reviewer has access to `specification.md` and `architecture.md`.

**Test Steps:**
1. Review the PR diff for Feature 112.
2. Confirm the code change is limited to:
   - `GitHubDiffFormatter`
   - `AzureDevOpsDiffFormatter`
   - one new internal helper under `src/Oocx.TfPlan2Md/RenderTargets/`
   - minimal related test updates, if any
3. Confirm no new CLI options, render targets, DI contracts, or provider-level abstractions were added.
4. Confirm no new package references or framework dependencies were introduced.

**Expected Result:**
- The change set stays within the architecture's recommended minimal implementation scope.
- The duplication hotspot is reduced without broadening the feature.

**Test Data:** PR diff and project files.

---

### TC-02: GitHub Unchanged Values Preserve Escaped Inline Code Output

**Type:** Automated unit

**Description:** Verify that a GitHub unchanged value still returns the exact escaped
`<code>...</code>` output after the shared helper extraction.

**Preconditions:**
- Formatter unit tests compile in `Oocx.TfPlan2Md.TUnit`.

**Test Steps:**
1. Execute the `GitHubDiffFormatterTests` suite.
2. Run a test using identical before/after values containing markdown special characters.
3. Assert the exact escaped inline-code string.

**Expected Result:**
- Output matches pre-refactoring GitHub behavior exactly.
- Every character escaped by the shared helper remains escaped in the unchanged-value path.

**Test Data:**
- Inline string containing the shared escape set, for example:
  ``\`*_{}[]()#+-.!|``

---

### TC-03: GitHub Changed Values Preserve Simple Diff Formatting

**Type:** Automated unit

**Description:** Verify that the GitHub changed-value path still emits the exact simple diff format
used in markdown tables.

**Preconditions:**
- Formatter unit tests compile in `Oocx.TfPlan2Md.TUnit`.

**Test Steps:**
1. Execute the `GitHubDiffFormatterTests` suite.
2. Run a test using different before/after values that contain markdown special characters.
3. Assert the exact `- before<br>+ after` output string with escaping applied.

**Expected Result:**
- The formatter still returns a compact GitHub diff line.
- No code wrapping is added to the changed-value path.
- Markdown escaping remains identical to the pre-refactoring behavior.

**Test Data:**
- Short inline literals such as `a|b+c` and `a|c+d`.

---

### TC-04: Azure DevOps Unchanged Values Preserve Escaped Inline Code Output

**Type:** Automated unit

**Description:** Verify that unchanged Azure DevOps values still return the exact escaped
`<code>...</code>` output after the shared helper extraction.

**Preconditions:**
- Formatter unit tests compile in `Oocx.TfPlan2Md.TUnit`.

**Test Steps:**
1. Execute the `AzureDevOpsDiffFormatterTests` suite.
2. Run a test using identical before/after values containing markdown special characters.
3. Assert the exact escaped inline-code string.

**Expected Result:**
- Output matches pre-refactoring Azure DevOps unchanged-value behavior exactly.
- Shared escape behavior matches the GitHub formatter for unchanged values.

**Test Data:**
- Same inline string used by TC-02.

---

### TC-05: Azure DevOps Short Values Preserve HTML Fast Path

**Type:** Automated unit

**Description:** Verify that short single-line changed values still use the existing Azure DevOps
HTML fast path rather than the full character-level LCS pipeline.

**Preconditions:**
- Formatter unit tests compile in `Oocx.TfPlan2Md.TUnit`.

**Test Steps:**
1. Execute the `AzureDevOpsDiffFormatterTests` suite.
2. Run a test using short single-line before/after values with special characters.
3. Assert the code wrapper, removed/added background colors, `-` / `+` prefixes, and line break.

**Expected Result:**
- The formatter still uses the HTML fast path for short values.
- Short-value behavior remains unchanged after the shared helper extraction.

**Test Data:**
- Inline literals such as `a|b` and `a|c`.

---

### TC-06: Azure DevOps Long Values Preserve Character-Level Inline Diff

**Type:** Automated unit

**Description:** Verify that longer changed values still use the full inline-diff pipeline with
character-level highlighting.

**Preconditions:**
- Formatter unit tests compile in `Oocx.TfPlan2Md.TUnit`.

**Test Steps:**
1. Execute the `AzureDevOpsDiffFormatterTests` suite.
2. Run a test using before/after values longer than the fast-path threshold.
3. Assert the removed and added line backgrounds plus removed and added character highlights.

**Expected Result:**
- Azure DevOps still uses the full LCS-backed inline diff path for long values.
- The helper extraction does not alter long-value rendering behavior.

**Test Data:**
- Existing long-string literals used by `FormatDiff_WhenLongValuesDiffer_UsesCharLevelDiff`.

---

### TC-07: Azure DevOps Large-Value Performance Guard Remains Intact

**Type:** Automated performance regression

**Description:** Verify that large inputs still complete within the current performance threshold
and do not regress into pathological LCS behavior.

**Preconditions:**
- Performance tests compile in `Oocx.TfPlan2Md.TUnit`.

**Test Steps:**
1. Execute `DiffComputationPerformanceTests.FormatDiff_AzureDevOps_WithLargeValues_CompletesWithinTimeLimit`.
2. Measure completion time using the existing test.
3. Confirm the formatter still returns non-empty output within the established threshold.

**Expected Result:**
- The Azure DevOps formatter completes within 10 seconds for the large test payloads.
- The refactoring does not affect the size guard or large-value path selection.

**Test Data:**
- Existing generated 50,000-character pseudo-JSON strings from the performance test.

---

### TC-08: Full Regression Suite Remains Green

**Type:** Automated regression

**Description:** Verify that the refactoring does not introduce broader regressions outside the
targeted formatter tests.

**Preconditions:**
- All Feature 112 code changes are applied.

**Test Steps:**
1. Run the targeted formatter and performance tests relevant to the refactoring.
2. Run the full solution test suite:
   ```bash
   scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
   ```
3. Confirm no test baselines or unrelated outputs require updates.

**Expected Result:**
- All targeted tests pass.
- The full automated suite passes without requiring snapshot or artifact updates.
- No unrelated regressions are introduced.

**Test Data:** Existing repository test data and generated large-value performance inputs.

---

## Test Data Requirements

No new JSON or snapshot test data files are required for this refactoring.

Recommended inline literals only:

- One shared special-character string for unchanged-value escape assertions
- One short changed-value pair for GitHub
- One short changed-value pair for Azure DevOps fast path
- Existing long-value and large-value inputs already present in the current test suite

---

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| `before == null` and `after == null` | Returns `string.Empty` | Existing formatter tests; covered by TC-08 |
| Equal value contains markdown special characters | Returns exact escaped `<code>...</code>` output | TC-02, TC-04 |
| GitHub changed value contains markdown special characters | Returns exact escaped `- before<br>+ after` output | TC-03 |
| Azure DevOps short changed value contains markdown-sensitive characters | Uses existing HTML fast path with unchanged wrapper and prefixes | TC-05 |
| Azure DevOps changed values exceed fast-path threshold | Uses character-level inline diff path | TC-06 |
| Azure DevOps large values approach LCS worst-case input size | Completes within established threshold | TC-07 |

---

## Non-Functional Verification

### Regression Command

Primary verification command:

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx
```

### Focused Test Areas

If quick validation is needed before the full suite, start with:

- `RenderTargets/GitHubDiffFormatterTests.cs`
- `RenderTargets/AzureDevOpsDiffFormatterTests.cs`
- `MarkdownGeneration/DiffComputationPerformanceTests.cs`

### Dependency and Surface-Area Guard

The change should not introduce:

- new NuGet dependencies
- new public APIs
- changes outside the `RenderTargets` implementation slice except minimal test updates

Any such change should be treated as an out-of-scope regression for this feature.

---

## Open Questions

None. The specification and architecture define a narrow, behavior-preserving refactoring scope, and
the current test suite provides clear regression points for the affected formatters.
