---
name: view-pr-github
description: View GitHub PR status/details safely (non-interactive) using gh.
compatibility: Requires GitHub CLI (gh) authenticated, plus network access.
---

# View PR (GitHub)

## Purpose
Read pull request status/details from GitHub without triggering an interactive pager.

Use this skill for **read-only PR inspection** (status, checks, reviewers, files, body). For creating/merging PRs, prefer the repo wrapper scripts (see the `create-pr-github` skill).

## Hard Rules
### Must
- Use a **non-interactive pager** for every `gh` call:
  - Prefer `GH_PAGER=cat` (gh-specific, overrides gh’s internal pager logic)
  - Also set `GH_FORCE_TTY=false` to reduce TTY-driven behavior
- Prefer structured output (`--json`) and keep output small with `--jq` when practical.

### Must Not
- Run plain `gh ...` without `GH_PAGER=cat` (it may open `less` and block).
- Change global GitHub CLI config (no `gh config set ...`).

## Patterns

### Minimal Safe Prefix
Use this prefix for every command:

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh ...
```

### 1) View PR Summary (safe JSON)

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> \
  --json number,title,state,isDraft,url,mergeStateStatus,reviewDecision
```

### 2) View Checks (success/fail)

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> \
  --json statusCheckRollup \
  --jq '.statusCheckRollup[] | {name, status, conclusion}'
```

### 3) View Reviews / Review Requests

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> \
  --json latestReviews,reviewRequests \
  --jq '{latestReviews: [.latestReviews[] | {author: .author.login, state, submittedAt}], reviewRequests: [.reviewRequests[].login]}'
```

### 4) View Changed Files (names only)

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> --json files \
  --jq '.files[].path'
```

### 5) View PR Body (for review)

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> --json body --jq '.body'
```

## When To Prefer Wrapper Scripts
- If you are about to **create** or **merge** a PR: use `scripts/pr-github.sh create` / `scripts/pr-github.sh create-and-merge`.
- If you just need to **inspect** PR state/checks/reviews: use this skill’s `gh` patterns.
