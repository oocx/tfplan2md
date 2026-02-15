# Retrospective: Azure Display Enhancements (063)

**Date:** 2026-02-08
**Participants:** Maintainer, Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer (×3 sessions), Code Reviewer, UAT Tester (×2 sessions), Release Manager

## Summary

Feature 063 enriched Azure resource display across all Azure providers (azurerm, azapi, azuread, azdevops) with human-readable display names for subscriptions, management groups, tenants, and roles, plus resource-specific summaries for DNS records, PIM assignments, and role management policies.

The lifecycle spanned **~18.5 hours wall clock** (2026-02-07 16:56 → 2026-02-08 11:31) with 11 agent sessions totalling 84 requests. The first UAT **failed** due to missing icons in PIM summaries and incorrect subscription formatting in the comprehensive demo. A rework cycle (developer sessions 2 and 3) fixed the bugs plus additional improvement ideas the Maintainer had when reviewing the rendered output. The release (v1.13.0) completed successfully after the second UAT pass.

Key process themes: (1) UAT as the primary catch-all for rendering gaps the spec/architecture couldn't fully enumerate, (2) developer confusion with uncommitted working-tree changes between sessions, (3) agents still using `gh` CLI directly instead of wrapper scripts.

## Scoring Rubric

- Starting score: **10**
- Deductions:
  - **−1** — UAT failed on first pass due to rendering bugs (missing icons, wrong formatting) that should have been caught by code review or snapshot tests
  - **−1** — Developer used the comprehensive demo instead of creating a feature-specific UAT artifact (as the test plan explicitly required), requiring Maintainer intervention
  - **−1** — Developer repeatedly blocked on uncommitted working-tree changes during rework sessions, requiring Maintainer guidance
  - **−0.5** — Release manager used `gh run view` and `gh release view` directly (cancelled by Maintainer), ignoring wrapper script instructions
  - **−0.5** — UAT PRs initially only included the feature-specific report, missing the comprehensive demo for regression testing
- **Final workflow rating: 6/10**

## Session Overview

### Time Breakdown

| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** (sum of all chat sessions) | 10h 58m | 100% |
| Agent Work Time | 5h 32m | 50% |
| User Wait Time (confirmation prompts) | 1h 31m | 14% |
| Idle / Between Sessions | ~4h | 36% |

- **Start:** 2026-02-07 16:56 (Requirements Engineer)
- **End:** 2026-02-08 11:31 (Release Manager)
- **Wall Clock:** ~18h 35m
- **Total Requests:** 84
- **Total Tool Invocations:** 1,586
- **Files Changed:** ~133 (feature-related)
- **Commits:** 26 feature-related commits
- **Tests:** 867 total passing (0 failures)

### Session Timeline

| Session | Time | Requests | Duration | Model |
|---------|------|----------|----------|-------|
| Requirements Engineer | 16:56–17:21 | 16 | 25m | claude-sonnet-4.5 |
| Architect | 17:22–17:37 | 2 | 15m | claude-opus-4.6 |
| Quality Engineer | 17:39–17:46 | 4 | 7m | gemini-3-flash-preview |
| Task Planner | 17:48–17:50 | 2 | 2m | gemini-3-flash-preview |
| Developer (session 1) | 17:51–23:57 | 33 | 6h 6m | gpt-5.2-codex |
| Code Reviewer | 23:58–23:58 | 1 | 16m | claude-sonnet-4.5 |
| UAT (session 1) | 00:16–00:35 | 4 | 19m | gemini-3-flash-preview |
| Developer (session 2 — rework) | 00:36–02:13 | 12 | 1h 37m | gpt-5.2-codex |
| Developer (session 3 — rework) | 09:52–10:05 | 3 | 13m | gpt-5.2-codex |
| UAT (session 2) | 09:20–09:50 | 3 | 30m | gemini-3-flash-preview |
| Release Manager | 10:06–11:31 | 4 | 1h 25m | gemini-3-flash-preview |

## Agent Analysis

### Agent Attribution Note

Per-agent metrics are derived from separate chat export files (one per agent session). The chat export format does not record custom agent names, but since each session was exported individually with descriptive filenames, per-session attribution is reliable.

### Model Usage by Session

| Session | Model | Requests |
|---------|-------|----------|
| Developer (session 1) | gpt-5.2-codex | 33 |
| Requirements Engineer | claude-sonnet-4.5 | 16 |
| Developer (session 2) | gpt-5.2-codex | 12 |
| Quality Engineer | gemini-3-flash-preview | 4 |
| UAT (session 1) | gemini-3-flash-preview | 4 |
| Release Manager | gemini-3-flash-preview | 4 |
| Developer (session 3) | gpt-5.2-codex | 3 |
| UAT (session 2) | gemini-3-flash-preview | 3 |
| Architect | claude-opus-4.6 | 2 |
| Task Planner | gemini-3-flash-preview | 2 |
| Code Reviewer | claude-sonnet-4.5 | 1 |

### Overall Model Usage

| Model | Requests | % of Total |
|-------|----------|------------|
| gpt-5.2-codex | 48 | 57% |
| claude-sonnet-4.5 | 17 | 20% |
| gemini-3-flash-preview | 17 | 20% |
| claude-opus-4.6 | 2 | 2% |

### Automation Effectiveness by Session

| Session | Total Tools | Auto | Manual | Cancelled | Automation Rate |
|---------|-------------|------|--------|-----------|-----------------|
| Developer (session 1) | 819 | 813 | 4 | 2 | 99% |
| Developer (session 2) | 331 | 331 | 0 | 0 | 100% |
| Release Manager | 103 | 98 | 3 | 2 | 95% |
| Code Reviewer | 89 | 87 | 2 | 0 | 97% |
| Developer (session 3) | 81 | 81 | 0 | 0 | 100% |
| Architect | 59 | 59 | 0 | 0 | 100% |
| Quality Engineer | 37 | 37 | 0 | 0 | 100% |
| UAT (session 1) | 27 | 27 | 0 | 0 | 100% |
| UAT (session 2) | 20 | 18 | 2 | 0 | 90% |
| Requirements Engineer | 13 | 13 | 0 | 0 | 100% |
| Task Planner | 7 | 7 | 0 | 0 | 100% |
| **Total** | **1,586** | **1,571** | **11** | **4** | **99%** |

### Tool Usage by Session (Top Tools)

| Session | Top Tools |
|---------|-----------|
| Developer (session 1) | readFile (272), applyPatch (164), run_in_terminal (131), findTextInFiles (105) |
| Developer (session 2) | readFile (141), run_in_terminal (56), applyPatch (53), findTextInFiles (49) |
| Release Manager | run_in_terminal (69), readFile (19), listDirectory (5) |
| Developer (session 3) | readFile (38), findTextInFiles (14), run_in_terminal (12) |
| Code Reviewer | readFile (28), findTextInFiles (22), run_in_terminal (21) |
| Architect | readFile (34), findTextInFiles (7), manage_todo_list (6) |
| UAT (session 1) | run_in_terminal (22), readFile (4) |
| UAT (session 2) | run_in_terminal (15), readFile (5) |
| Requirements Engineer | run_in_terminal (8), readFile (2) |
| Quality Engineer | run_in_terminal (13), readFile (7) |
| Task Planner | readFile (3), run_in_terminal (2) |

### File Edit Statistics

| Status | Count |
|--------|-------|
| Kept | 157 |
| Undone | 0 |
| Modified by user | 3 |

All 157 file edits were kept (0 undone), indicating high edit quality. 3 edits were further modified by the Maintainer.

## Rejection Analysis

### Rejections by Session

| Session | Total Requests | Cancelled | Failed | Tool Rejections | Rejection Rate |
|---------|---------------|-----------|--------|-----------------|----------------|
| Release Manager | 4 | 2 | 0 | 2 | 100%* |
| Developer (session 1) | 33 | 0 | 2 | 0 | 6% |
| All others | 47 | 0 | 0 | 0 | 0% |

*Release Manager rejection rate is 100% by the metric formula (tool rejections counted proportionally), but this reflects only 2 cancelled `gh` CLI commands, not session-wide failure.

### Rejections by Model

| Model | Requests | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|----------|-----------|--------|-----------------|----------------|
| gpt-5.2-codex | 48 | 0 | 2 | 0 | 4% |
| gemini-3-flash-preview | 17 | 2 | 0 | 2 | 24% |
| claude-sonnet-4.5 | 17 | 0 | 0 | 0 | 0% |
| claude-opus-4.6 | 2 | 0 | 0 | 0 | 0% |

### Common Rejection Reasons

| Error | Count | Context |
|-------|-------|---------|
| Bad Unicode escape in JSON | 1 | Developer session 1 — minor model output error, self-resolved |
| Canceled | 1 | Developer session 1 — user cancelled a request |
| `gh run view` rejected | 1 | Release Manager — used `gh` CLI directly instead of wrapper script |
| `gh release view` rejected | 1 | Release Manager — used `gh` CLI directly instead of wrapper script |

### User Vote-Down Reasons

None recorded in any session.

## Automation Opportunities

### Theme: gh CLI Replacement

The most actionable automation improvement. Two `gh` commands were rejected by the Maintainer because they require manual approval every time. The following table maps common `gh` use cases to existing or needed scripts:

| `gh` Command | Current Script | Gap / Action Needed |
|--------------|---------------|---------------------|
| `gh run view <id> --log-sel <step>` | `scripts/check-workflow-status.sh view <id>` | **Gap**: no `--log-sel` equivalent to view a specific step's logs. Add a `logs` subcommand. |
| `gh release view <tag>` | None | **Gap**: Create `scripts/gh-release-view.sh` wrapper or add a subcommand to an existing script. |
| `gh pr create` | `scripts/pr-github.sh create` | ✅ Already available |
| `gh pr merge` | `scripts/pr-github.sh create-and-merge` | ✅ Already available |
| `gh run list` | `scripts/check-workflow-status.sh list` | ✅ Already available |
| `gh workflow run` | `scripts/check-workflow-status.sh trigger` | ✅ Already available |

### Theme: UAT Artifact Generation

The UAT agent initially only pushed the feature-specific report, missing the comprehensive demo for regression testing.

| Opportunity | Proposed Change | Verification |
|------------|-----------------|--------------|
| UAT PRs missing comprehensive demo | Update `scripts/uat-run.sh` to always include `artifacts/comprehensive-demo.md` (or `comprehensive-demo-simple-diff.md`) alongside the feature artifact | UAT PR contains both reports by default |
| UAT agent instructions incomplete | Update UAT agent instructions to explicitly require both feature-specific AND regression artifacts | Code review of UAT agent `.agent.md` |

### Terminal Command Patterns

| Pattern | Count | Session(s) | Status |
|---------|-------|-----------|--------|
| `scripts/check-workflow-status.sh` | 12 | Release Manager | ✅ Using wrapper |
| `scripts/pr-github.sh` | 1 | Release Manager | ✅ Using wrapper |
| `scripts/uat-run.sh` | 5 | UAT 1, UAT 2 | ✅ Using wrapper |
| `scripts/git-status.sh` | 2 | Release Manager | ✅ Using wrapper |
| `scripts/git-log.sh` | 3 | Release Manager | ✅ Using wrapper |
| `scripts/generate-demo-artifacts.sh` | 1 | Release Manager | ✅ Using wrapper |
| `docker build` / `docker run` | 6 | Developer 1, Code Reviewer | ⚠️ Consider wrapper |
| `gh run view --log-sel` | 1 | Release Manager | ❌ Rejected — needs wrapper |
| `gh release view` | 1 | Release Manager | ❌ Rejected — needs wrapper |

### Suggested Skills / Scripts

| Opportunity | Proposed Script | Where It Fits | Evidence | Verification |
|------------|-----------------|---------------|----------|--------------|
| View CI step logs | `scripts/check-workflow-status.sh logs <id> <step>` | Release process | Release Manager used `gh run view --log-sel Versionize` (rejected) | Command completes without `gh` approval prompt |
| View GitHub release | `scripts/gh-release-view.sh <tag>` | Release verification | Release Manager used `gh release view v1.13.0` (rejected) | Command completes without `gh` approval prompt |
| Docker build/run for markdownlint | `scripts/markdownlint.sh <file>` | Code review / CI | Code Reviewer ran 3 `docker run` commands to run markdownlint | Single command replaces multi-step docker invocations |

## Model Effectiveness Assessment

### Assigned vs Actual Model Usage

| Session | Actual Model | Assessment |
|---------|-------------|------------|
| Developer (×3) | gpt-5.2-codex | ✅ Appropriate for heavy coding; 48 requests, high success rate (96%) |
| Requirements Engineer | claude-sonnet-4.5 | ✅ Good for interactive requirements; 16 requests, 100% success |
| Architect | claude-opus-4.6 | ✅ Appropriate for complex architecture decisions; 2 requests, 100% success |
| Code Reviewer | claude-sonnet-4.5 | ⚠️ Only 1 request — efficient but missed rendering gaps |
| Quality Engineer | gemini-3-flash-preview | ✅ Quick test plan generation; 4 requests, 100% success |
| Task Planner | gemini-3-flash-preview | ✅ Minimal overhead; 2 requests, 100% success |
| UAT (×2) | gemini-3-flash-preview | ✅ Adequate for UAT script execution |
| Release Manager | gemini-3-flash-preview | ⚠️ Used `gh` directly despite instructions; 2 tool rejections |

### Model Performance Statistics

| Model | Requests | Avg Response (s) | Total Time (s) | Success Rate |
|-------|----------|-------------------|-----------------|--------------|
| gpt-5.2-codex | 48 | 234 | 11,019 | 96% |
| gemini-3-flash-preview | 17 | 404 | 6,882 | 100% |
| claude-sonnet-4.5 | 17 | 72 | 1,235 | 100% |
| claude-opus-4.6 | 2 | 389 | 779 | 100% |

### Observations

- **gpt-5.2-codex** handled the heaviest workload (48 requests) with a 96% success rate and 234s average response time. The 2 failures were minor (JSON escape error, user cancel).
- **gemini-3-flash-preview** had the highest average response time (404s), primarily driven by the release manager session's long-running CI monitoring commands and UAT script executions. Success rate was 100%.
- **claude-sonnet-4.5** was the fastest model at 72s average, well-suited for the interactive requirements and code review tasks.
- **claude-opus-4.6** was used sparingly (2 requests) for architecture, which is appropriate given its higher cost.

## Agent Performance

| Agent | Rating | Strengths | Improvements Needed |
|-------|--------|-----------|---------------------|
| Requirements Engineer | ⭐⭐⭐⭐ | Crisp spec from a complex initial request (16 interactions in 25m). Captured scope, edge cases, fallback behavior. | Some rendering touch-points (icons in summaries, subscription icon in scope) were missed — hard to enumerate exhaustively. |
| Architect | ⭐⭐⭐⭐ | 4 clear ADRs with options/tradeoffs. Architecture decisions all implemented correctly. Efficient (2 requests, 15m). | Did not flag the cross-cutting nature of icon/summary rendering (many resource types affected). |
| Quality Engineer | ⭐⭐⭐ | Test plan covered core acceptance criteria (TC-01 through TC-17). Quick execution (7m, 4 requests). | UAT test plan did not sufficiently enumerate all rendering scenarios. Feature-specific UAT artifact did not cover all new features. |
| Task Planner | ⭐⭐⭐⭐⭐ | Minimal overhead (2 requests, 2m). Tasks were well-ordered with clear acceptance criteria and dependencies. | None — executed efficiently within boundaries. |
| Developer (combined) | ⭐⭐⭐ | Implemented all 9 tasks across 3 sessions. 867 tests passing, 157 edits all kept (0 undone). Code quality praised in code review. | (1) Generated comprehensive demo instead of feature-specific UAT artifact — significant error requiring Maintainer intervention. (2) Repeatedly blocked on uncommitted changes during rework. (3) Required 3 sessions due to UAT rework. |
| Code Reviewer | ⭐⭐⭐ | Thorough review: Docker build, markdownlint, adversarial testing, architecture compliance. Found 0 blockers, approved correctly. | **Missed rendering gaps** that UAT subsequently caught (missing icons in PIM summary, subscription formatting in comprehensive demo). Should have detected that the feature-specific demo didn't cover all new rendering. |
| UAT Tester (combined) | ⭐⭐⭐ | Correctly identified rendering bugs and failed the first UAT. Used wrapper scripts (`uat-run.sh`). | (1) Initially only added feature-specific report, not comprehensive demo. (2) Feature-specific report didn't cover all new features/scenarios. |
| Release Manager | ⭐⭐⭐ | Successfully merged PR, verified CI, created release v1.13.0. Used `scripts/pr-github.sh` and `scripts/check-workflow-status.sh` correctly for most operations. | Used `gh run view --log-sel` and `gh release view` directly (both rejected by Maintainer). Spent significant time on CI tag verification (~45m). |
| Retrospective (self) | ⭐⭐⭐⭐ | Comprehensive data collection from 11 chat exports. Metrics extracted systematically. Interactive phase covered key friction points. | Self-assessment is inherently limited. |

**Overall Workflow Rating: 6/10** — Feature was delivered successfully, but the UAT failure, developer rework cycle, and recurring `gh` CLI violations indicate process friction that is partially systematic (rendering coverage) and partially instruction-related (gh CLI, uncommitted changes handling).

## What Went Well

- **Requirements phase was efficient and thorough.** 16 interactive exchanges in 25 minutes produced a detailed specification covering 8+ acceptance criteria, before/after examples, and CLI usage documentation. (Evidence: `specification.md` is 264 lines with clear scope boundaries.)
- **Architecture decisions were sound.** All 4 ADRs (attribute matching, shared loader, enriched formatter, ViewModelFactory pattern) were implemented as designed with no deviation found during code review. (Evidence: code review architecture compliance table shows 4/4 match.)
- **Zero file edits undone.** All 157 agent file edits were accepted by the Maintainer, indicating high edit quality.
- **High automation rate (99%).** Only 11 of 1,586 tool invocations required manual approval. Wrapper scripts (`scripts/check-workflow-status.sh`, `scripts/pr-github.sh`, `scripts/uat-run.sh`) worked well.
- **UAT correctly caught bugs.** The first UAT identified two rendering issues and correctly failed the test, preventing release of broken output.
- **Task planning was minimal-overhead.** 2 requests, 2 minutes, well-ordered tasks with dependencies — the Task Planner stayed within boundaries.

## What Didn't Go Well

- **UAT failed on first pass.** Two rendering bugs (missing `🛡️`/`👤` icons in PIM summary, incorrect subscription key formatting) were not caught by code review or snapshot tests. The code reviewer ran markdownlint and Docker build but did not visually verify the rendered output against the spec's before/after examples. (Evidence: UAT report shows "FAILED" status; code review shows "APPROVED".)
- **Developer generated wrong UAT artifact.** The test plan explicitly asked for a "separate test plan to test additions of this feature only," but the developer generated the comprehensive demo instead. The Maintainer noted this as a "significant error." (Evidence: developer.chat.json feedback: "The comprehensive demo is too generic.")
- **Developer blocked on uncommitted changes.** During rework sessions (developer 2, developer 3), the developer agent didn't know how to handle existing uncommitted changes from previous sessions and manually-saved chat exports. The Maintainer had to instruct it to stash and continue. (Evidence: developer 2.chat.json feedback: "the developer is repeatedly blocking because it does not know how to handle existing changes.")
- **Release Manager used `gh` CLI directly.** Despite wrapper script instructions, the release manager ran `gh run view --log-sel Versionize` and `gh release view v1.13.0` directly, which were rejected by the Maintainer. (Evidence: release manager.chat.json — 2 tool invocations with `isConfirmed.type == 0`.)
- **UAT only added feature-specific report initially.** The comprehensive demo was missing from the first UAT PR, reducing regression coverage. The UAT agent had to be prompted to include both.
- **Feature-specific UAT artifact didn't cover all changes.** Not all new rendering scenarios (e.g., subscription icon in scope, all resource types with role formatting) were included in the UAT test artifact, making it impossible for the Maintainer to verify full coverage.
- **Rendering gap coverage is hard to enumerate in spec/architecture.** Icons, summaries, and display names touch many resource types. The spec and architecture couldn't feasibly list every touch-point, so UAT became the only safety net for rendering completeness.
- **askQuestions tool not available to all agents.** Some agents present numbered options in chat and wait for the Maintainer to type a number, instead of using the interactive `vscode/askQuestions` tool which provides single-select, multi-select, and free-text UI elements. This makes interaction more error-prone and less user-friendly.

## Improvement Opportunities

**GitHub Issues Created:** [#408](https://github.com/oocx/tfplan2md/issues/408), [#407](https://github.com/oocx/tfplan2md/issues/407), [#409](https://github.com/oocx/tfplan2md/issues/409), [#410](https://github.com/oocx/tfplan2md/issues/410)

| # | Theme | Issue | Proposed Solution | Action Item |
|---|-------|-------|-------------------|-------------|
| 1 | **gh CLI elimination** ([#408](https://github.com/oocx/tfplan2md/issues/408)) | Release Manager used `gh run view --log-sel` and `gh release view` directly (rejected). Agents keep ignoring wrapper script instructions. | (a) Add `logs` subcommand to `scripts/check-workflow-status.sh` for viewing CI step logs. (b) Create `scripts/gh-release-view.sh` for viewing releases. (c) Add a clear alternatives table to agent instructions mapping every `gh` subcommand to its wrapper script. (d) Add an explicit "NEVER use `gh` directly" rule in release manager agent instructions. | **Where:** `scripts/check-workflow-status.sh`, new `scripts/gh-release-view.sh`, `.github/agents/release-manager.agent.md`, `.github/copilot-instructions.md`. **Verify:** No `gh` CLI usage in next 3 features. |
| 2 | **Uncommitted changes handling** ([#407](https://github.com/oocx/tfplan2md/issues/407)) | Developer agent blocks when it finds uncommitted changes from previous agents or manually-saved chat exports, instead of stashing and continuing. | Add explicit instructions to the Developer agent: "If you find uncommitted changes at session start, stash them (`git stash push -m 'pre-existing changes'`), complete your work, then `git stash pop`. Never block on existing changes." | **Where:** `.github/agents/developer.agent.md`. **Verify:** Developer does not block on uncommitted changes in next rework cycle. |
| 3 | **Feature-specific UAT artifacts** ([#407](https://github.com/oocx/tfplan2md/issues/407)) | Developer generated the comprehensive demo instead of a feature-specific UAT artifact as required by the test plan. | (a) Add explicit instruction to Developer agent: "When test plan specifies feature-specific artifacts, create those — do not substitute the comprehensive demo." (b) Code Reviewer should check that the test plan's artifact requirements are met before approving. | **Where:** `.github/agents/developer.agent.md`, `.github/agents/code-reviewer.agent.md`. **Verify:** Developer creates correct artifact type; Code Reviewer flags missing artifacts. |
| 4 | **UAT always includes regression artifact** ([#409](https://github.com/oocx/tfplan2md/issues/409)) | UAT PRs initially only contained the feature-specific report. The comprehensive demo for regression testing was missing. | (a) Update `scripts/uat-run.sh` to always include `artifacts/comprehensive-demo-simple-diff.md` as a second artifact. (b) Update UAT agent instructions to always push both artifacts. | **Where:** `scripts/uat-run.sh`, `.github/agents/uat-tester.agent.md`. **Verify:** UAT PR always contains both feature and regression artifacts. |
| 5 | **Rendering coverage gaps** ([#409](https://github.com/oocx/tfplan2md/issues/409)) | UAT feature-specific artifact didn't cover all new rendering scenarios. Only UAT (visual review) caught icon/formatting bugs that code review missed. | (a) Quality Engineer should enumerate all rendering touch-points explicitly in the UAT test plan when a feature affects cross-cutting rendering (icons, summaries, display names). (b) Code Reviewer must verify that the feature-specific demo artifact exercises every acceptance criterion before approving. (c) Consider adding a "rendering checklist" for features that touch display formatting. | **Where:** `.github/agents/quality-engineer.agent.md`, `.github/agents/code-reviewer.agent.md`. **Verify:** UAT artifact covers all acceptance criteria; Code Reviewer explicitly checks artifact coverage. |
| 6 | **Docker/markdownlint wrapper** ([#410](https://github.com/oocx/tfplan2md/issues/410)) | Code Reviewer ran 3 docker commands to set up markdownlint. | Create `scripts/markdownlint.sh` to wrap `docker run davidanson/markdownlint-cli2`. | **Where:** New `scripts/markdownlint.sh`. **Verify:** Code Reviewer uses single script for markdownlint validation. |
| 7 | **askQuestions tool missing** ([#410](https://github.com/oocx/tfplan2md/issues/410)) | Not all agents have the `vscode/askQuestions` tool available. Agents currently present numbered options and wait for the Maintainer to type a number in chat, which is less interactive and error-prone. | (a) Add `vscode/askQuestions` to all agent tool lists. (b) Update agent instructions to prefer `askQuestions` for presenting choices (single-select, multi-select) or gathering structured input. | **Where:** All `.github/agents/*.agent.md` files (check `tools:` section). **Verify:** All agents have `askQuestions` tool; agents use it instead of chat-based option selection. |

## User Feedback (Verbatim)

### From Chat Exports

1. **Developer session 2** (developer 2.chat.json):
   > "note for retrospective: the developer is repeatedly blocking because it does not know how to handle existing changes. These changes are usually changes from the current feature."

   → Maps to **Improvement #2** (uncommitted changes handling)

2. **Developer session 1** (developer.chat.json):
   > "The comprehensive demo is too generic. The test plan explicitely asked for a separate test plan to test additions of this feature only so that the feature is easier to test. The comprehensive demo is not a good example for a test of new features, as it covers too many unrelated resources. If artifacts needed for the test plan don't exist, then you must create those!"
   >
   > "note to retrospective agent (will read this chats procotol): this is a significant error by the developer. we must improve developer instructions to avoid that in the future."

   → Maps to **Improvement #3** (feature-specific UAT artifacts)

3. **Release Manager** (release manager.chat.json):
   > "you are not supposed to use gh run view directly, as this always requires approvals. we have scripts for that"
   >
   > "note for retrospective: agents keep using gh directly, need to improve instructions"

   → Maps to **Improvement #1** (gh CLI elimination)

### From Interactive Phase

4. **Developer rework sessions:** Developer sessions 2 and 3 were caused by rework after failed UAT tests and additional improvement ideas from seeing the final result (e.g., additional attributes in DNS record summaries).

   → Maps to **Improvement #5** (rendering coverage gaps)

5. **Uncommitted changes detail:** Previous agents often did not commit all changes, and manually exported chat logs were present. Developer should stash and continue rather than blocking.

   → Maps to **Improvement #2** (uncommitted changes handling)

6. **Spec/architecture rendering gaps:** Both phases missed some rendering touch-points because the codebase surface area for icons/summaries is spread across many files. Gaps were mostly caught during UAT, not during development.

   → Maps to **Improvement #5** (rendering coverage gaps)

7. **UAT artifact coverage:** Feature-specific UAT reports did not cover all new features and changes, so the Maintainer could not fully test them. Quality Engineer specs and Code Reviewer checks need to ensure complete coverage.

   → Maps to **Improvement #5** (rendering coverage gaps)

8. **UAT missing regression artifact:** UAT often added only the feature-specific report. Must improve UAT instructions/scripts so both feature and regression artifacts are always included.

   → Maps to **Improvement #4** (UAT always includes regression artifact)

9. **Cross-cutting rendering coverage:** When rendering changes touch many different resources (e.g., subscription display names appear in many resource types), coverage must be more thorough. The code reviewer should detect missing cases.

   → Maps to **Improvement #5** (rendering coverage gaps)

10. **`gh` CLI annoyance:** Agents repeatedly using `gh` commands directly interrupts work and requires approval. Instructions should disallow those usages completely and provide a clear alternatives table.

    → Maps to **Improvement #1** (gh CLI elimination)

11. **askQuestions tool availability:** Not all agents have the `vscode/askQuestions` tool. All agents must have this tool and use it as the preferred method to get input from the user instead of presenting numbered options in chat.

    → Maps to **Improvement #7** (askQuestions tool missing)

## CI / Status Checks Summary

- **PR #406** ("feat: Azure Display Enhancements (063)"): Created 2026-02-08 09:14, merged 2026-02-08 09:20 (6 minutes)
- **CI status checks:** No explicit status checks recorded (GitHub status API returned `pending` with 0 statuses — likely using branch protection with required workflows)
- **Release workflow:** v1.13.0 released successfully after Release Manager triggered the workflow and verified the tag/CHANGELOG
- **Release verification challenges:** Release Manager spent ~45 minutes verifying CI because the Versionize step and tag creation had timing issues. Eventually manually created the tag `v1.13.0` and triggered the release workflow.

## Retrospective DoD Checklist

- [x] Evidence sources enumerated (11 chat exports + feature artifacts + git history + PR metadata)
- [x] Evidence timeline normalized across lifecycle phases (requirements → architecture → QA → planning → development → code review → UAT → rework → UAT 2 → release)
- [x] Findings clustered by theme (gh CLI friction, uncommitted changes, UAT coverage, rendering gaps, artifact generation)
- [x] No unsupported claims (all findings backed by chat log evidence, artifacts, or interactive phase feedback)
- [x] No guessed agent attribution (per-session exports with descriptive filenames provide reliable attribution)
- [x] Action items include where + verification
- [x] Required metrics and required sections are present
- [x] All retro-related user feedback captured verbatim (3 from chat exports + 8 from interactive phase)
