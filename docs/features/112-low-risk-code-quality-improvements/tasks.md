# Tasks: Shared Diff Formatter Markdown Escaping Refactoring

## Overview

Implement the approved Feature 112 slice defined in
[`specification.md`](specification.md),
[`architecture.md`](architecture.md), and
[`test-plan.md`](test-plan.md): remove the duplicated markdown-escaping logic from
`GitHubDiffFormatter` and `AzureDevOpsDiffFormatter` by introducing one shared internal helper
under `src/Oocx.TfPlan2Md/RenderTargets/`, while preserving all current rendering behavior.

## Status

- [x] Task 1 complete
- [x] Task 2 complete
- [x] Task 3 complete
- [x] Task 4 complete

This plan intentionally stays surgical:

- no CLI or output-format redesign
- no dependency injection or public API changes
- no new package dependencies
- only minimal targeted regression-test updates needed to prove parity

## Tasks

### Task 1: Add a shared internal diff-formatter markdown escaping helper

**Priority:** High

**Description:**
Create one narrowly scoped internal helper in `src/Oocx.TfPlan2Md/RenderTargets/` that owns the
shared markdown-escaping behavior currently duplicated in both diff formatters.

**Acceptance Criteria:**
- [ ] Exactly one new shared helper is added under `src/Oocx.TfPlan2Md/RenderTargets/`.
- [ ] The helper is `internal` and scoped specifically to diff formatter markdown escaping.
- [ ] The helper preserves the current escape set and escape order used by both existing
      formatter-local implementations.
- [ ] The helper remains within the `RenderTargets` layer and is not moved into
      `MarkdownGeneration/` or another broader utility area.
- [ ] XML documentation comments are present and explain why the helper remains render-target
      scoped.

**Dependencies:** None

**Notes:**
- Keep the helper small and literal; this task is not a general markdown utility extraction.
- Prefer naming that makes the behavior and scope obvious to future maintainers.

---

### Task 2: Migrate `GitHubDiffFormatter` to the shared helper

**Priority:** High

**Description:**
Replace the private `EscapeMarkdown` implementation in
`src/Oocx.TfPlan2Md/RenderTargets/GitHub/GitHubDiffFormatter.cs` with the shared helper while
leaving GitHub-specific diff orchestration unchanged.

**Acceptance Criteria:**
- [ ] The duplicated private `EscapeMarkdown` method is removed from `GitHubDiffFormatter`.
- [ ] `GitHubDiffFormatter` uses the shared helper for unchanged-value and changed-value escaping.
- [ ] The unchanged-value path still returns `<code>...</code>` with the exact same escaped content
      as before.
- [ ] The changed-value path still returns the exact GitHub table diff shape
      `- before<br>+ after` with escaped content and without adding backticks.
- [ ] No GitHub-specific rendering logic outside markdown escaping is changed.

**Dependencies:** Task 1

**Notes:**
- Keep `GitHubDiffFormatter` as the render-target-specific orchestration boundary.
- Avoid opportunistic cleanup unrelated to the duplicated escape method.

---

### Task 3: Migrate `AzureDevOpsDiffFormatter` to the shared helper

**Priority:** High

**Description:**
Replace the private `EscapeMarkdown` implementation in
`src/Oocx.TfPlan2Md/RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs` with the shared helper
while preserving the existing Azure DevOps fast path and long-value inline diff behavior.

**Acceptance Criteria:**
- [ ] The duplicated private `EscapeMarkdown` method is removed from `AzureDevOpsDiffFormatter`.
- [ ] `AzureDevOpsDiffFormatter` uses the shared helper where the current implementation escapes
      unchanged values and diff output content.
- [ ] The unchanged-value path still returns `<code>...</code>` with the exact same escaped content
      as before.
- [ ] The short-value HTML fast path remains unchanged, including prefixes, wrappers, and current
      rendering behavior.
- [ ] The long-value character-level inline diff pipeline remains unchanged apart from the shared
      escape call site.
- [ ] No changes are made to `IDiffFormatter`, DI registration, or provider boundaries.

**Dependencies:** Task 1

**Notes:**
- Treat the Azure DevOps formatter as the higher-risk of the two integrations because it has two
  rendering paths; keep this migration narrowly focused.

---

### Task 4: Add minimal regression coverage and run focused verification

**Priority:** High

**Description:**
Add only the minimal targeted tests needed to pin the shared escaping behavior through the public
formatter contracts, then run the focused and full verification steps identified in the test plan.

**Acceptance Criteria:**
- [ ] `GitHubDiffFormatterTests` includes or strengthens one unchanged-value test using a string
      that exercises the full shared markdown escape set.
- [ ] `AzureDevOpsDiffFormatterTests` includes or strengthens one unchanged-value test using the
      same escape-set input.
- [ ] Existing regression coverage for GitHub changed-value output, Azure DevOps short-value fast
      path, Azure DevOps long-value path, and Azure DevOps large-value performance remains present.
- [ ] Focused formatter/performance tests pass before running the broader suite.
- [ ] Full validation passes via
      `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`.
- [ ] No new snapshot files, fixture files, or package dependencies are introduced for this
      refactoring.

**Dependencies:** Tasks 2 and 3

**Notes:**
- Prefer indirect testing through public `FormatDiff` behavior instead of adding direct tests for
  the shared helper.
- If an existing test already pins the required behavior exactly, strengthen it instead of adding a
  parallel duplicate.

## Implementation Order

Recommended surgical implementation sequence:

1. **Task 1 - Add the shared helper first** so the common behavior is defined once before either
   formatter is touched.
2. **Task 2 - Migrate `GitHubDiffFormatter` next** because it has the simpler diff-rendering path
   and provides the lowest-risk first integration point.
3. **Task 3 - Migrate `AzureDevOpsDiffFormatter` after GitHub** so the helper is already proven in
   one formatter before applying it to the dual-path Azure DevOps implementation.
4. **Task 4 - Finish with tests and verification** to confirm both formatter integrations remain
   behavior-preserving and the change set stays safely reviewable.

## Open Questions

None. The implementation scope, architecture boundary, and required test coverage are already
approved and narrow enough for direct execution.
