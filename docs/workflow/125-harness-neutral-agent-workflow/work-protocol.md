# Work Protocol: Harness-Neutral Agent Workflow

**Work Item:** `docs/workflow/125-harness-neutral-agent-workflow/`
**Branch:** `workflow/125-harness-neutral-agent-workflow`
**Workflow Type:** Workflow
**Created:** 2026-09-03

## Agent Work Log

<!-- Each role appends their entry below when they complete their work. -->

### Workflow Engineer

- **Date:** 2026-09-03
- **Summary:** Designed the migration from the Copilot-shaped instruction corpus to
  harness-neutral roles under `.agents/`, with Claude running the full workflow and
  Codex running the Code Reviewer role. Phase 1 (scaffolding) implemented: `AGENTS.md`,
  `CLAUDE.md` pointer, `.agents/` layout, tier registry, adapter generator, toolchain
  doctor and setup scripts, and the reference role.
- **Artifacts Produced:** `AGENTS.md`, `CLAUDE.md`, `.agents/tiers.json`,
  `.agents/roles/code-reviewer.md`, `scripts/sync-agent-config.sh`,
  `scripts/agent-doctor.sh`, `scripts/setup-agent-tools.sh`,
  `docs/workflow/125-harness-neutral-agent-workflow/{design,tasks,work-protocol}.md`,
  `state.json`, generated `.claude/agents/code-reviewer.md`
- **Problems Encountered:**
  - `.next-issue-number` was stale (119) while `docs/` already contained 124; used 125.
  - The Husky `commit-msg` regex rejects the `workflow:` type that `docs/spec.md`
    mandates. 14 such commits exist in history because Husky is not installed locally.
    Scheduled as task 5 rather than fixed inline.
  - `sg` on Linux is util-linux's setgid binary, not `ast-grep`. Guard added to
    `agent-doctor.sh`; scripts must always invoke `ast-grep` by full name.
  - The npm packages named `code2prompt` and `tokei` are unrelated to the Rust tools of
    the same name; both are cargo-only. `tokei` was dropped from the design as a result.

### Workflow Engineer (phase 2)

- **Date:** 2026-09-03
- **Summary:** Merged the 26 Copilot agent files into 13 harness-neutral roles under
  `.agents/roles/` (9,820 → 887 lines, 91% reduction). Extracted the plumbing repeated
  across every agent — commit amending, fixup prohibition, response style, "one question
  at a time", artifact ownership — into the `agent-runtime` skill. Rewrote
  `scripts/validate-agents.py` to validate roles instead of Copilot model names. Wrote
  `docs/workflow.md` defining stages, gates, state and rework loops.
- **Artifacts Produced:** `.agents/roles/*.md` (13), `.agents/skills/agent-runtime/SKILL.md`,
  `scripts/validate-agents.py`, `docs/workflow.md`, regenerated `.claude/`
- **Problems Encountered:**
  - The Quality Engineer agent instructed "follow xUnit and AwesomeAssertions patterns",
    contradicting `docs/spec.md`, which states TUnit is the only test framework. The new
    role says TUnit; the stale instruction had been live for an unknown period.
  - `validate-agents.py`'s new link checker caught a forward reference to
    `docs/workflow.md` before it existed — the file was written as a result rather than
    the link being removed.

### Workflow Engineer (phase 3)

- **Date:** 2026-09-03
- **Summary:** Built the workflow driver. Stage sequences, gate rules and rework targets
  are data in `.agents/workflow.json`; three scripts share `workflow-lib.sh`, which
  derives the work item from the branch name so a cold session resumes without being
  told where it is. Verified against this work item: gate checks correctly refuse
  release while Release Manager has no entry, UAT is correctly skipped for a
  workflow-only diff, and model escalation resolves the Developer to opus after rework.
- **Artifacts Produced:** `.agents/workflow.json`, `scripts/workflow-lib.sh`,
  `scripts/workflow-next.sh`, `scripts/workflow-gate.sh`, `scripts/wp-append.sh`,
  `.agents/skills/run-workflow/SKILL.md`, `.agents/commands/*.md` (4), regenerated `.claude/`
- **Problems Encountered:**
  - `wp-append.sh --question` originally accepted a question with no `--assumed`, which
    would have let a role record a doubt without saying what it did about it. Now refused.
  - `workflow-next.sh --json` reported the declared tier while printing an escalated
    model, which reads as a contradiction to anything consuming the JSON. It now reports
    both `declared_tier` and the effective `tier`.

### Workflow Engineer (phase 4)

- **Date:** 2026-09-03
- **Summary:** Wired the Code Reviewer role to Codex and verified it against the real
  CLI on a live diff. The review took 4m34s and 199,652 tokens, and returned
  `VERDICT: REWORK` with 6 Blockers and 1 Major — all of them real defects in the
  phase 1-3 work.
- **Artifacts Produced:** `scripts/codex-review.sh`, `.agents/codex-review-schema.json`,
  corrected `.agents/tiers.json`, fixed `scripts/agent-doctor.sh`, `code-review.md`
- **Problems Encountered:** Four assumptions about the Codex CLI were wrong and only
  surfaced by running it:
  - `codex exec review` rejects `--color`, which belongs to `codex exec`.
  - `codex exec review` refuses a custom prompt alongside `--base`, so the role file
    could not be used with that subcommand. Switched to plain `codex exec`, which also
    accepts `--sandbox read-only` — the reviewer now physically cannot edit the code it
    reviews, making the role boundary a guarantee rather than an instruction.
  - Bare model names `sol` / `terra` / `luna` are rejected: "not supported when using
    Codex with a ChatGPT account". The real slugs are `gpt-5.6-sol`, `gpt-5.6-terra`,
    `gpt-5.6-luna`.
  - OpenAI strict structured output requires every property to appear in `required`;
    optional fields must be typed nullable instead.
  `scripts/agent-doctor.sh` also reported the Rust tools missing when they were
  installed, because `~/.cargo/bin` is absent from non-login shell PATH. It now looks
  there and warns.

### Workflow Engineer (phase 5)

- **Date:** 2026-09-03
- **Summary:** Fixed the 6 Blockers and 1 Major from the Codex review. Five were real
  defects in the phase 1-3 driver: stage completion had two owners and could skip a
  role; a rejected gate read as permission to continue; the UAT gate was declared
  `before_stage` but nothing ever opened it; the Developer role read feature artifacts
  that a bug workflow never produces; and the Release Manager required a code review
  that workflow and website sequences never schedule. The sixth was a correct
  observation that HEAD was mid-migration, which phase 4 resolved. Added
  `scripts/test-workflow-driver.sh` — 31 assertions against a throwaway repo — which
  addresses the Major finding and makes the fixes verifiable.
- **Artifacts Produced:** `scripts/test-workflow-driver.sh`, fixed `scripts/wp-append.sh`,
  `scripts/workflow-next.sh`, `.agents/roles/developer.md`, `.agents/roles/release-manager.md`,
  `.agents/skills/run-workflow/SKILL.md`
- **Problems Encountered:**
  - The first version of the suite included an assertion that passed against the
    pre-fix code, so it was proving nothing. Replaced with one that checks the
    consequence — that a rejection does not let the run enter the guarded stage —
    rather than the stored label. Verified by running the suite against the pre-fix
    scripts: 9 failures before, 0 after.
  - Cross-family review found five defects that had survived my own manual testing of
    the same code. Manual happy-path checking was not sufficient evidence, which is
    exactly what the review said.

### Workflow Engineer (phase 5, round 2)

- **Date:** 2026-09-04
- **Summary:** Second Codex review returned REWORK with 10 Blockers and 3 Major. None of
  the first round's five defects reappeared — removing the surface bugs exposed deeper
  ones. Fixed nine of them; the remainder are phases 6-8 by definition. The most serious
  was created by my own round-one fix: making stage completion single-owner meant an
  APPROVED review never completed its stage, so the workflow would re-run the reviewer
  forever.
- **Artifacts Produced:** fixed `scripts/codex-review.sh`, `scripts/wp-append.sh`,
  `scripts/workflow-gate.sh`, `scripts/workflow-next.sh`, `.agents/workflow.json`,
  `.agents/codex-review-schema.json`, `.agents/roles/{architect,release-manager,web-designer}.md`,
  `AGENTS.md`, `docs/workflow.md`, `scripts/test-workflow-driver.sh` (38 assertions)
- **Problems Encountered:**
  - The UAT gate opened *before* the UAT Tester created the PRs it asked the Maintainer
    to review — a gate nobody could answer. Moved to after the stage.
  - `release-manager` was in every `gate_blocking_stages` list, so its own pre-flight
    check required a work-protocol entry it only writes afterwards: every release
    failed on itself.
  - Gate decisions were accepted on gates that were not open, so a caller could
    pre-approve UAT while it was `n/a` and the mandatory stop silently vanished.
  - `workflow-gate.sh all` printed gate state without failing on it and swallowed the
    UAT result, so it could report success with an unresolved approval outstanding.
  - Four driver tests failed after these changes because they encoded the old, wrong
    gate ordering. Updating tests to match corrected behaviour is right; it is worth
    recording that the suite was rewritten rather than the behaviour bent to fit it.
  - Two review rounds cost 4m34s / 199,652 tokens and 4m14s / 106,184 tokens.

### Workflow Engineer (phase 7)

- **Date:** 2026-09-04
- **Summary:** Removed the Copilot corpus: 26 agent files, 19 prompt files, 3 hook
  configs, the Copilot instructions and setup workflow, the model reference, the
  chat-analysis tooling, and the three git wrappers that RTK's hook replaces.
  `docs/agents.md` is replaced by `docs/workflow.md`, carrying a rewritten mermaid
  diagram that shows the new roles, driver and three gates. Agent validation, adapter
  drift and the driver tests now run in PR validation.
- **Artifacts Produced:** deleted 60+ files; updated `.github/workflows/pr-validation.yml`,
  `.github/ISSUE_TEMPLATE/workflow_improvement.yml`, `CONTRIBUTING.md`, `README.md`,
  `website/src/_data/aiWorkflowPage.js`; added `.agents/claude-settings.json`,
  `.markdownlint-cli2.jsonc`
- **Problems Encountered:**
  - `git rm` aborts entirely when one pathspec misses, so an earlier batch silently
    deleted nothing; `analyze-chat.py` and `analyze-run.sh` survived until re-checked.
  - `src/tests/shell/analyze_chat_test.sh` tested a script being deleted, and CI ran it.
    Both removed. `validate_work_protocol_test.sh` tested the deleted hook; its rule now
    lives in `workflow-gate.sh`, which the driver suite covers.
  - `src/tests/shell/uat_validate_test.sh` was not executable and had been failing on
    `main` as well; CI never invoked it, so nobody noticed. Fixed the bit; it passes.
  - The README's "Development Team" section still claimed GitHub Copilot wrote 100% of
    the project and linked to the deleted model reference. Rewritten to describe the
    actual roles and tiers. It is public-facing copy and worth the Maintainer's review.
  - A 20 MB VS Code chat export sits untracked in the C# source tree at
    `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/MarkdownHelpers/developer.chat.json`.
    Left in place deliberately: it was never committed, so deleting it would be
    irreversible rather than recoverable from history.

### Workflow Engineer (round 7)

- **Date:** 2026-09-04
- **Summary:** Completed phases 1-8 of the harness-neutral workflow migration: canonical .agents/ layout, 13 roles, the auto-mode driver, the Codex reviewer, skills migration, and removal of the Copilot corpus.
- **Artifacts Produced:** AGENTS.md, .agents/ (13 roles, 28 skills, 4 commands, tiers.json, workflow.json), 8 workflow scripts, docs/workflow.md, generated .claude/
- **Problems Encountered:** Recorded per phase above. The dry run itself surfaced one: workflow-gate.sh reported UAT as REQUIRED for a workflow-type item with no UAT stage, because the path rule was not type-aware. Fixed, with a regression test.

### Workflow Engineer (round 8)

- **Date:** 2026-09-04
- **Summary:** Moved Issue Analyst from standard to deep tier at the Maintainer's
  direction, closing the last open question. Both first stages now run deep: a wrong
  requirement and a misdiagnosed root cause propagate through every later stage alike.
- **Artifacts Produced:** `.agents/roles/issue-analyst.md`, `design.md`, `README.md`,
  `scripts/test-workflow-driver.sh` (43 assertions), regenerated `.claude/`
- **Problems Encountered:** None. `state.json` now carries no open questions.
