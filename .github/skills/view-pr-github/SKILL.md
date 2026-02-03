---
name: view-pr-github
description: View GitHub PR status/details using GitHub MCP tools (preferred) or gh CLI (fallback).
compatibility: Preferred: GitHub MCP tools (github-mcp-server-*). Fallback: GitHub CLI (gh) authenticated, plus network access.
---

# View PR (GitHub)

## Purpose
Read pull request status/details from GitHub.

**Priority order:**
1. **FIRST**: Use GitHub MCP tools - stable, structured, no pager/editor issues
2. **SECOND**: Use `scripts/pr-github.sh` wrapper (for create/merge only)
3. **LAST**: Use `gh` CLI as final fallback

Use this skill for **read-only PR inspection** (status, checks, reviewers, files, body). For creating/merging PRs, use the `create-pr-github` skill.

## GitHub MCP Tools (Preferred)

Use these GitHub MCP tools for PR operations:

### PR Details and Metadata
```
github-mcp-server-pull_request_read
  method: "get"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
```

Returns: PR number, title, state, body, author, timestamps, merge status, etc.

### PR Diff
```
github-mcp-server-pull_request_read
  method: "get_diff"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
```

Returns: Full diff of the PR changes

### PR Status Checks
```
github-mcp-server-pull_request_read
  method: "get_status"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
```

Returns: Status of CI/CD checks, required checks, conclusion

### PR Changed Files
```
github-mcp-server-pull_request_read
  method: "get_files"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
  perPage: 100
```

Returns: List of files changed with stats (additions, deletions, changes)

### PR Review Comments (inline on code)
```
github-mcp-server-pull_request_read
  method: "get_review_comments"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
  perPage: 100
```

Returns: Code review comments with file paths, line numbers, and comment bodies

### PR Reviews
```
github-mcp-server-pull_request_read
  method: "get_reviews"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
  perPage: 100
```

Returns: Review submissions with state (APPROVED, CHANGES_REQUESTED, etc.)

### PR Conversation Comments
```
github-mcp-server-pull_request_read
  method: "get_comments"
  owner: "oocx"
  repo: "tfplan2md"
  pullNumber: 123
  perPage: 100
```

Returns: General conversation comments on the PR

### List PRs
```
github-mcp-server-list_pull_requests
  owner: "oocx"
  repo: "tfplan2md"
  state: "open"  # or "closed", "all"
  perPage: 30
  page: 1
```

Returns: List of PRs with basic metadata

## GitHub CLI Fallback (Last Resort)

**⚠️ Only use when GitHub MCP tools are unavailable**

### Hard Rules
- Prefer GitHub MCP tools when they can answer the question
- If using `gh`, use a **non-interactive pager** for every `gh` call:
  - Use `GH_PAGER=cat` (gh-specific, overrides gh's internal pager logic)
  - Also set `GH_FORCE_TTY=false` to reduce TTY-driven behavior
  - Prefer structured output (`--json`) and keep output small with `--jq` when practical
- Never run plain `gh ...` without `GH_PAGER=cat` (it may open `less` and block)
- Never change global GitHub CLI config (no `gh config set ...`)

### Fallback `gh` Patterns

Use this prefix for every command:

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh ...
```

#### View PR Summary
```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> \
  --json number,title,state,isDraft,url,mergeStateStatus,reviewDecision
```

#### View Status Checks
```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> \
  --json statusCheckRollup \
  --jq '.statusCheckRollup[] | {name, status, conclusion}'
```

#### View Reviews
```bash
GH_PAGER=cat GH_FORCE_TTY=false gh pr view <pr-number> \
  --json latestReviews,reviewRequests \
  --jq '{latestReviews: [.latestReviews[] | {author: .author.login, state, submittedAt}], reviewRequests: [.reviewRequests[].login]}'
```

## When To Use Different Approaches

### Prefer GitHub MCP Tools when
- You need **structured PR metadata** (details, changed files, status checks, comments, reviews)
- You want to avoid terminal-side pitfalls (auth prompts, pager behavior, large output)
- You want operations that can be permanently allowed in VS Code
- You're in any VS Code Copilot context (chat, inline, etc.)

### Prefer Wrapper Scripts when
- You're **creating** or **merging** a PR: use `scripts/pr-github.sh create` / `scripts/pr-github.sh create-and-merge`

### Prefer `gh` CLI when (rare)
- You need an API surface the MCP tools don't expose (use `gh api` for custom calls)
- You need commands for Maintainers to reproduce locally
- A maintainer explicitly requests CLI examples

**Rule of thumb:** Always start with GitHub MCP tools; only fall back to wrapper scripts for create/merge, and only use `gh` CLI as final resort.
