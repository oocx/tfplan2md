# GitHub CLI Usage Instructions for Agents

## Priority Order for GitHub Operations

**CRITICAL: Follow this priority order to minimize manual approval friction:**

1. **FIRST: Use GitHub MCP Tools** (when available in your session)
   - Available via `github-mcp-server-*` tool names
   - Can be permanently allowed in VS Code
   - Structured, reliable output
   - No pager/editor issues
   - Examples: `github-mcp-server-pull_request_read`, `github-mcp-server-list_pull_requests`

2. **SECOND: Use Repository Wrapper Scripts** (when MCP tools don't cover the use case)
   - **`scripts/pr-github.sh`** - For PR operations (create, merge)
   - **`scripts/check-workflow-status.sh`** - For workflow operations (list, watch, trigger, view)
   - **`scripts/uat-watch-github.sh`** - For UAT PR watching
   - **`scripts/git-status.sh`** - For git status
   - **`scripts/git-diff.sh`** - For git diff
   - **`scripts/git-log.sh`** - For git log

3. **LAST: Use `gh` CLI as final fallback** (only when neither MCP tools nor scripts are available)
   - A maintainer explicitly requests CLI reproduction steps
   - The required operation is not available via MCP tools or scripts
   - You need `gh api` flexibility for custom API calls

## GitHub MCP Tools (Preferred)

GitHub MCP tools provide reliable, structured access to GitHub operations without the friction of terminal approvals.

### Available GitHub MCP Tools

**Pull Requests:**
- `github-mcp-server-pull_request_read` - Get PR details, diff, status, files, reviews, comments
- `github-mcp-server-list_pull_requests` - List PRs with filters (state, base, head, sort)
- `github-mcp-server-search_pull_requests` - Search PRs across repositories

**Issues:**
- `github-mcp-server-issue_read` - Get issue details, comments, labels
- `github-mcp-server-list_issues` - List issues with filters (state, labels, since)
- `github-mcp-server-search_issues` - Search issues across repositories

**Repository:**
- `github-mcp-server-get_file_contents` - Read file contents from repository
- `github-mcp-server-list_commits` - List commits with filters
- `github-mcp-server-get_commit` - Get commit details with diff
- `github-mcp-server-list_branches` - List repository branches
- `github-mcp-server-list_tags` - List repository tags
- `github-mcp-server-search_code` - Search code across repositories
- `github-mcp-server-search_repositories` - Search for repositories

**Releases:**
- `github-mcp-server-list_releases` - List releases
- `github-mcp-server-get_latest_release` - Get latest release
- `github-mcp-server-get_release_by_tag` - Get specific release by tag

**Workflows:**
- `github-mcp-server-actions_list` - List workflows, runs, jobs, artifacts
- `github-mcp-server-actions_get` - Get workflow, run, or job details
- `github-mcp-server-get_job_logs` - Get logs for workflow jobs

### Example Usage

**Instead of:**
```bash
GH_PAGER=cat gh pr view 123 --json number,title,state
```

**Use:**
```
github-mcp-server-pull_request_read with method="get", owner="oocx", repo="tfplan2md", pullNumber=123
```

**Instead of:**
```bash
GH_PAGER=cat gh pr list --state open --json number,title
```

**Use:**
```
github-mcp-server-list_pull_requests with owner="oocx", repo="tfplan2md", state="open"
```

**Instead of:**
```bash
GH_PAGER=cat gh run list --limit 5
```

**Use:**
```
github-mcp-server-actions_list with method="list_workflow_runs", owner="oocx", repo="tfplan2md", perPage=5
```

## Critical: Prevent Interactive Mode

GitHub CLI (`gh`) may trigger interactive pagers that block agent execution. **Always disable the pager** when running `gh` commands.

## Required Pattern: Always Disable Paging

**Use this pattern for EVERY `gh` command:**

```bash
GH_PAGER=cat GH_FORCE_TTY=false gh [command] [options]
```

Why `GH_PAGER`?

- `GH_PAGER` is `gh`’s own pager override (most reliable way to prevent `gh` from launching `less`).
- `PAGER` is a general-purpose pager variable used by many tools; it can still be useful, but it’s easier to miss and can affect unrelated commands.

Alternatively, set it once at the beginning of your session:

```bash
export PAGER=cat
# or
export GH_PAGER=cat
```

**Also set** `GH_FORCE_TTY=false` to ensure the GitHub CLI does not attempt TTY-dependent behavior (prompts, colorized output, or pager negotiation) in non-interactive environments:

```bash
export GH_FORCE_TTY=false
```

**Editor suppression:** Some `gh` commands may try to launch an editor (e.g., when creating or editing issues/PRs). Pass content via stdin using `--body-file -` instead of relying on an editor; alternatively set `EDITOR` to a non-interactive command.

```bash
# Safe non-interactive issue creation
echo "Automated issue body" | GH_PAGER=cat GH_FORCE_TTY=false gh issue create --title "Automated" --body-file -
```

## Workflow Operations

**Priority order:**
1. **FIRST**: Use GitHub MCP tools (`github-mcp-server-actions_list`, `github-mcp-server-actions_get`, `github-mcp-server-get_job_logs`)
2. **SECOND**: Use `scripts/check-workflow-status.sh` wrapper script
3. **LAST**: Raw `gh run` or `gh workflow` commands (avoid)

**Wrapper script usage:**

```bash
# List workflow runs
scripts/check-workflow-status.sh list --branch main --limit 5

# List specific workflow
scripts/check-workflow-status.sh list --workflow release.yml --limit 1

# Watch a run until completion
scripts/check-workflow-status.sh watch <run-id>

# View run details
scripts/check-workflow-status.sh view <run-id>

# Trigger a workflow
scripts/check-workflow-status.sh trigger release.yml --field tag=v1.0.0
```

**Why?** The wrapper script handles pager suppression and is designed for permanent approval in VS Code, reducing friction during long-running operations like CI polling.

## GitHub CLI Fallback Patterns (Last Resort)

**⚠️ Only use these `gh` patterns when:**
- GitHub MCP tools don't support the operation
- Repository wrapper scripts don't cover the use case
- You need custom API access via `gh api`

When using `gh`, always follow the critical paging rules below.

### Pull Requests (Fallback Examples)

**⚠️ Prefer:** `github-mcp-server-pull_request_read` and `github-mcp-server-list_pull_requests` tools

**Fallback (if MCP tools unavailable):**
```bash
# List pull requests (prefer: github-mcp-server-list_pull_requests)
GH_PAGER=cat GH_FORCE_TTY=false gh pr list --json number,title,state,author

# View pull request details (prefer: github-mcp-server-pull_request_read)
GH_PAGER=cat GH_FORCE_TTY=false gh pr view 123 --json number,title,body,state,commits,reviews

# Check pull request status (prefer: github-mcp-server-pull_request_read with method="get_status")
GH_PAGER=cat GH_FORCE_TTY=false gh pr status --json number,title,state
```

**For PR Creation/Merge:** ALWAYS use `scripts/pr-github.sh` wrapper, never raw `gh pr create` or `gh pr merge`

### Issues (Fallback Examples)

**⚠️ Prefer:** `github-mcp-server-issue_read`, `github-mcp-server-list_issues`, `github-mcp-server-search_issues` tools

**Fallback (if MCP tools unavailable):**
```bash
# List issues (prefer: github-mcp-server-list_issues)
GH_PAGER=cat GH_FORCE_TTY=false gh issue list --json number,title,state,author

# View issue details (prefer: github-mcp-server-issue_read)
GH_PAGER=cat GH_FORCE_TTY=false gh issue view 123 --json number,title,body,state,comments
```

**Note:** Issue creation/closing is typically not needed for agents

### Repository Information (Fallback Examples)

**⚠️ Prefer:** GitHub MCP tools for repository operations

**Fallback (if MCP tools unavailable):**
```bash
# List repository branches (prefer: github-mcp-server-list_branches)
PAGER=cat gh api repos/{owner}/{repo}/branches --jq '.[].name'

# Check workflow runs (prefer: github-mcp-server-actions_list)
PAGER=cat gh run list --json conclusion,status,name,createdAt
```

### Workflow Operations

**⚠️ DEPRECATED: Do not use raw `gh run` commands. Use `scripts/check-workflow-status.sh` instead.**

```bash
# ❌ WRONG - Requires manual approval every time
PAGER=cat gh run list --limit 5

# ✅ CORRECT - Use the wrapper script
scripts/check-workflow-status.sh list --branch main --limit 5

# ❌ WRONG - Requires manual approval every time
PAGER=cat gh run view 12345 --json conclusion,status,jobs

# ✅ CORRECT - Use the wrapper script
scripts/check-workflow-status.sh view 12345
```

## Output Format Guidelines

### Use --json for Structured Data

Always specify the exact fields you need:

```bash
PAGER=cat gh pr list --json number,title,state,author,createdAt,updatedAt
```

Common fields by command type:
- **PRs**: `number`, `title`, `state`, `author`, `body`, `commits`, `reviews`, `url`
- **Issues**: `number`, `title`, `state`, `author`, `body`, `comments`, `url`
- **Repos**: `name`, `description`, `url`, `defaultBranchRef`, `owner`

### Use --jq for Formatted Output

If you need human-readable text instead of JSON:

```bash
PAGER=cat gh pr list --jq '.[] | "#\(.number) - \(.title) (\(.state))"'
```

## Important: Never Modify Global Configuration

**DO NOT use `gh config set` or modify global settings.** This would interfere with the user's personal GitHub CLI configuration.

Only use local, per-command configuration:
- Environment variables (`PAGER=cat`, `GH_PAGER=cat`)
- Command-line arguments
- Piping to `cat`

## Why This Is Critical

Without `PAGER=cat`:
1. `gh` detects a TTY and launches an interactive pager (usually `less`)
2. The pager waits for user input (space, arrow keys, 'q' to quit)
3. Your agent blocks indefinitely waiting for the pager to exit
4. When you manually quit with 'q', the agent may not capture the output correctly

## Troubleshooting

If you still see interactive behavior:

1. **Verify PAGER is set**: `echo $PAGER` should output `cat`
2. **Use explicit prefix**: Always use `PAGER=cat gh ...` even if exported
3. **Check for aliases**: Ensure `gh` isn't aliased to something else
4. **Pipe to cat as last resort**: `gh command | cat` forces non-interactive output

## Summary - Quick Reference

```bash
# ✅ CORRECT - Always use this pattern
PAGER=cat gh pr list --json number,title,state,author

# ❌ WRONG - May block with interactive pager
gh pr list --json number,title,state,author

# ✅ CORRECT - Alternative with export
export PAGER=cat
gh pr list --json number,title,state,author
gh issue list --json number,title,state

# ✅ CORRECT - Fallback with pipe
gh pr list --json number,title,state,author | cat
```

**Remember**: Every `gh` command must either have `PAGER=cat` prefix or be run in an environment where `PAGER=cat` has been exported.