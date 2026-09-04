## Harness-neutral agent workflow

The agent workflow moves off GitHub Copilot. Role definitions, skills and commands now
live in `.agents/` as harness-neutral markdown, with `AGENTS.md` as the single entry
point and `.claude/` generated from the canonical source by
`scripts/sync-agent-config.sh`.

The 26 Copilot agent files — 13 roles duplicated for local and cloud execution — become
13 role definitions, 9,820 lines to 987. Everything the duplicates had in common moves
into one `agent-runtime` skill. A role now declares a **tier** rather than a model name,
and `.agents/tiers.json` is the only place tiers map to models.

Claude runs the workflow. Code review runs in Codex, in a different model family from
the author, so the review tests the code instead of ratifying the reasoning that
produced it. A driver (`scripts/workflow-next.sh` plus a per-work-item `state.json`)
sequences the stages and stops at three gates: specification approval, a contested
architecture choice, and UAT when the diff touches user-visible output.

This is internal tooling. There is no change to `tfplan2md` itself, its output, or the
published image.
