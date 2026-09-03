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
