# CLAUDE.md

Read **[AGENTS.md](AGENTS.md)** — it is the canonical instruction file for this
repository and applies in full to Claude Code.

Claude-specific notes:

- Roles in `.agents/roles/` are generated into `.claude/agents/` by
  `scripts/sync-agent-config.sh`. Edit the files under `.agents/`, never the
  generated ones under `.claude/`.
- Prefer spawning a role as a subagent over adopting it in-session: the isolated
  context is the main reason this workflow is affordable.
