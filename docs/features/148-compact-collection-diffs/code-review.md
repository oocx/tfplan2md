# Code Review: Compact, Semantically Accurate Collection Diffs

Reviewer: Claude (fallback, because the Codex review failed twice with 401 Unauthorized).
Round 2: a re-review after the Developer rework. Branch `feature/148-compact-collection-diffs`
at `ab9ca177`. I read the rework diff `f76d07de..HEAD` in full and re-checked the whole branch
against `origin/main`. The round 1 review is in git history at `f76d07de`.

## Summary

The rework fixes B1 in the unmasked path. A realistic `azurerm_linux_web_app` create and delete
no longer shows `[]` rows. m1, m2 and m4 are fixed, and s1 is resolved by linking the UAT
fixture into the test project. Tests pass and coverage clears both thresholds.

The B1 fix missed one path. When a collection is masked (sensitive, without
`--show-sensitive`), change detection still goes through `RawKey`. `RawKey` treats absent and
`null` as equal, but treats `[]` as different from both. A sensitive empty list on create or
delete therefore still produces a `(sensitive)` row. A sensitive empty list that appears in
drift produces a collection drift entry. On main, neither of these exists. The realistic case
is destroying an AKS cluster without AAD admin, where `kube_admin_config` is `[]` and
schema-sensitive. This is Major M1. The fix is one line plus tests.

## Verification Results

I ran all of these myself in the worktree at `ab9ca177`:

- **Tests and coverage.** CI-equivalent Release run with Cobertura coverage:
  `scripts/test-with-timeout.sh -- dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --configuration Release ... --coverage`.
  - 1580 total, 1580 passed, 0 failed, 0 skipped (1m35s). This matches the Developer's
    1580/1580 claim.
  - `Oocx.TfPlan2Md.CoverageEnforcer` with the PR-validation thresholds: line 89.18%
    (threshold 84.48%) PASS, branch 80.92% (threshold 72.80%) PASS.
- **Demo artifacts.** I ran `scripts/generate-demo-artifacts.sh`. All 31 regenerated files
  differed only in the generation timestamp, which confirms the Developer's claim that the
  rework changes no demo output. I restored the files afterwards.
- **UAT output.** Rendering `uat-plan.json` with the built CLI reproduces the committed
  `uat-plan.md`. The only difference is the trailing blank line that stdout output always has,
  also on main.
- **markdownlint 0.49.1** with `.markdownlint.json`:
  - Clean: `uat-plan.md`, `artifacts/comprehensive-demo.md` and `tasks.md`. `tasks.md` had 30
    findings in round 1.
  - Probe outputs written to stdout have one MD012 at the end of the file. Main's stdout has
    the same finding, so it predates this change.
  - `docs/features.md` (135), `report-style-guide.md` (58) and `testing-strategy.md` (31)
    have exactly the same finding counts as on main.
- **Snapshots.** The rework changes none.
- **Main comparison.** I built `origin/main` in a separate worktree and rendered the same probe
  plans with both builds.
- **CI.** There is no PR yet, so I could not check CI status.

## Specification Compliance

The criterion-to-code-to-test mapping from round 1 still holds. The rework changes C9 and C12:

| Criterion | Implementing code | Proving test |
|---|---|---|
| C9 empty/absent/null distinct, no noise | `IsElementless` in `IsCollectionChanged` (`ResourceChangeStage.Collections.cs:176-179`) | `CreateAndDelete_RealisticResourceWithUnsetBlockLists_RendersNoEmptyCollectionRows`, `Create_EmptyCollection_ProducesNoRow`, `Create_EmptyCollectionWithShowUnchangedValues_RendersEmptyBrackets`, `Update_AbsentToEmpty_*`, `Update_NullToEmpty_*`, `Drift_AbsentToEmptyOnly_*`, `Drift_NullToElements_*`, `Update_EmptyAbsentAndNull_RenderDistinctCells`. The masked path is not covered: see M1 |
| C12 mixed scalar types | `CollectionChangeModel.HasMixedKinds`, `ElementDisplayText(element, quoteAmbiguous)` | existing `Update_StringOneToNumberOne_*` and `Update_BooleanVersusString*`, new `Create_AllStringListOfNumericAndBooleanLookingValues_RendersUnquoted`, `Update_WhitespaceOnlyStrings_*` |

Each criterion still has implementing code and a test. C9 has a test gap only in the masked
path.

## What I Tried To Break

I wrote probe plans in the scratchpad and rendered them with both builds:

- **Sensitive empty list on delete.** `azurerm_kubernetes_cluster` delete with
  `kube_admin_config: []` and `before_sensitive.kube_admin_config: true`. The branch renders
  `| kube_admin_config | (sensitive) |`, and main renders no row. **Fails: M1.**
- **Sensitive empty list on create.** `secret_list: []` with `after_sensitive.secret_list:
  true`. The branch renders a `(sensitive)` row, and main renders none. **Fails: M1.**
- **Sensitive empty list in drift.** Drift where `kube_admin_config: []` (sensitive) appears.
  The branch adds "1 changed collection: `kube_admin_config`" with a `(sensitive)` line.
  **Fails: M1.**
- **Unmasked absent, null and `[]`.** These are hidden on create, update and drift. `[x]` →
  `[]` renders as ➖ `a` → `[]`, and `[]` → `[x]` as `[]` → ➕ `x`. Correct.
- **Mixed-kind quoting.**
  - `["1",2]` → `["1",3]` quotes `"1"` (string next to a number). Correct.
  - All-string `["80","443"]` renders unquoted, as on main.
  - `HasMixedKinds` concatenates both sides, so `[null]` → `["null"]` quotes the string and
    the two stay distinguishable.
  - `true` and `false` count as one kind, so `[true,false]` does not trigger quoting.
  - Unknown elements are excluded, so a partly unknown list does not flip quoting.
- **Whitespace-only strings.** `[" "]` → `["  "]` renders as ➖ `" "` and ➕ `"  "`, which
  can be told apart. Fixed.
- **Fixture wiring.** `feature-148-uat-plan.json` is linked from `docs/` into TestData and
  loaded by `CollectionDiffsFixture_RendersCompactRowsWithoutEmptyBlockRowsOrSecrets`. The
  test also asserts that the sensitive sentinel never appears in the output. No other
  reference to the deleted `collection-diffs-plan.json` remains outside the work protocol and
  this review.
- **`--show-unchanged-values` on create.** Every unset block now appears as a `[]` row. Main
  showed no row for them, even with this flag, because `JsonFlattener` emits no key for `[]`.
  The behaviour is opt-in and documented in `docs/features.md`, so I accept it. It is not
  logged as an issue.

## Issues Found

### Blockers

None.

### Major

**M1. Sensitive empty collections still render as rows on create and delete, and as drift
entries.**

Location: `src/Oocx.TfPlan2Md/MarkdownGeneration/Stages/ResourceChangeStage.Collections.cs:57`
together with `RawKey` at lines 198-202.

When `masked` is true, `valuesEqual` compares `RawKey(Before)` with `RawKey(After)`. `RawKey`
maps absent and `null` to `""`, but `[]` to `"[]"`. So absent → `[]` counts as a change, which
is exactly the B1 rule that the rework fixed only in the unmasked `IsCollectionChanged` path.
`detect` still treats `[]` as a primitive collection, so a schema-sensitive block list such as
AKS `kube_admin_config` produces:

- `| kube_admin_config | (sensitive) |` on delete, where main shows no row.
- a `(sensitive)` row on create, where main shows no row.
- a "changed collection" drift entry when the empty list appears in drift.

The row leaks nothing, but it is a regression from main and contradicts the C9 rule that the
rework documented ("absent, null and [] are equivalent for change detection").

The doc comment on `RawKey` ("null and absent are equivalent") also states the old rule.

Fix direction:

- Make `RawKey` return `string.Empty` for an empty array as well, which matches
  `IsElementless`.
- Add tests for:
  - a masked create with `[]`
  - a masked delete with `[]`
  - masked drift absent → `[]`
  - a masked `[]` → `["x"]` update, which must still show the row
- Optionally add a sensitive empty block list to the web app in `uat-plan.json`.

### Minor

**m3 (carried over). Architecture section 7 still says `Null` renders as `-`.**

This is the Architect's artifact. The Developer and the Technical Writer both flagged it, but
nobody has changed it.

**m5. `uat-test-plan.md` still expects `absent -> []` and `null -> []` as visible rows.**

Line 27 of `uat-test-plan.md` still expects these rows, which contradicts the reworked
behaviour, and step 4 on line 65 is ambiguous about them. This is the Quality Engineer's
artifact, and the Developer recorded it as an open question. It must be settled before the UAT
gate, or the UAT tester will judge against a stale expectation.

### Suggestions

- **s2 (carried over).** TC-35 is still covered only through the value-formatter stand-in for
  feature 147.
- **s3 (carried over).** The first open question in `state.json` still says "Known sets remain
  order-insensitive", which the Maintainer's resolution supersedes.
- **s4 (carried over).** The Release test run takes 1m35s locally against the 120 s CI
  timeout, with seven more tests than in round 1.

### Process

- **Work protocol.** `work-protocol.md` has entries for:
  - the Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer and
    Technical Writer
  - Code Reviewer round 1
  - Developer round 2 and Technical Writer round 2
- **Changelog.** `CHANGELOG.md` is untouched.
- **Commits.** The rework commits are `fix:` and `docs:`. They change no snapshots, so they
  need no `SNAPSHOT_UPDATE_OK`.
- **Global docs.** `features.md`, `report-style-guide.md` and `testing-strategy.md` were
  updated for the new rules. README needs no change.

## Decision

Rework. M1 is the masked variant of B1. It is a small, well-localised fix, but it is a
user-visible regression from main in realistic destroy plans (AKS), and no test covers it.
Everything else from round 1 is resolved or tracked. m3 and m5 belong to other roles and should
be cleared before UAT.
