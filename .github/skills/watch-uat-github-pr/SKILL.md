---
name: watch-uat-github-pr
description: Watch a GitHub UAT PR for maintainer feedback or approval by polling comments until approved/passed.
compatibility: Requires GitHub CLI (gh) authenticated, plus network access.
---

# Watch UAT PR (GitHub)

## Purpose
UAT comment polling is historically brittle. This skill standardizes the watch loop so the agent can reliably wait for Maintainer feedback/approval using a single stable command.

This skill uses the repo wrapper script `scripts/uat-watch-github.sh`, which repeatedly calls `scripts/uat-github.sh poll` until:
- approval keywords are detected, or
- the PR is closed/merged, or
- a timeout is reached.

## Hard Rules
### Must
- Use `scripts/uat-watch-github.sh` (single stable command).
- Treat any detected approval (`approved|passed|accept|lgtm`) as UAT passed.
- Stop watching and report when the PR is closed.

### Must Not
- Spam comments or post follow-ups while waiting.
- Run many ad-hoc `gh` commands; prefer the wrapper.

## Actions

### 1. Watch the PR
```bash
scripts/uat-watch-github.sh <pr-number>
```

### 2. Optional: Tune polling interval / timeout
```bash
scripts/uat-watch-github.sh <pr-number> --interval-seconds 60 --timeout-seconds 3600
```

## Output
- Exit code `0`: approval detected or PR closed (treat as pass)
- Exit code `1`: timed out (treat as incomplete; ask Maintainer)
