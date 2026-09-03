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
