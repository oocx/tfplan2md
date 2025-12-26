# GitHub CLI (gh) Instructions

## Pager / Non-Interactive Mode
# GitHub CLI Usage Instructions for Agents

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

**Editor suppression:** Some `gh` commands may try to launch an editor (e.g., when creating or editing issues/PRs). Prefer passing content via flags or `--body-file -` instead of relying on an editor; alternatively set `EDITOR` to a non-interactive command.

```bash
# Safe non-interactive issue creation
echo "Automated issue body" | GH_PAGER=cat GH_FORCE_TTY=false gh issue create --title "Automated" --body-file -
```

## Common Commands - Correct Usage

### Pull Requests

```bash
# List pull requests
GH_PAGER=cat GH_FORCE_TTY=false gh pr list --json number,title,state,author

# View pull request details
GH_PAGER=cat GH_FORCE_TTY=false gh pr view 123 --json number,title,body,state,commits,reviews
GH_PAGER=cat GH_FORCE_TTY=false gh pr view 123 --json number,title,body,state,commits,reviews

## Prefer GitHub Chat Tools In VS Code

When operating inside VS Code chat, prefer GitHub chat tools for read-only PR inspection (details, files, reviews, status checks, comments). This makes it easier for the Maintainer to permanently allow a small set of tools and reduces repeated terminal approvals.

Use `gh` only as a fallback when there is no matching GitHub chat tool (or for `gh api` flexibility).

# Create pull request (preferred: repo wrapper scripts)
# CRITICAL: Post the exact PR Title + Description in chat BEFORE creating/merging a PR.

# Standard PR body template:
#   ## Problem
#   <why is this change needed?>
#
#   ## Change
#   <what changed?>
#
#   ## Verification
#   <how was it validated?>

scripts/pr-github.sh create --title "<type(scope): summary>" --body-file <path-to-pr-body.md>

# Manual fallback (only if wrapper scripts are unavailable)
GH_PAGER=cat GH_FORCE_TTY=false gh pr create --title "Title" --body "Description" --base main --head feature-branch

# Check pull request status
GH_PAGER=cat GH_FORCE_TTY=false gh pr status --json number,title,state
```

### Issues

```bash
# List issues
GH_PAGER=cat GH_FORCE_TTY=false gh issue list --json number,title,state,author

# View issue details
GH_PAGER=cat GH_FORCE_TTY=false gh issue view 123 --json number,title,body,state,comments

# Create issue
GH_PAGER=cat GH_FORCE_TTY=false gh issue create --title "Title" --body "Description"

# Close issue
GH_PAGER=cat GH_FORCE_TTY=false gh issue close 123
```

### Repository Information

```bash
# View repository details
PAGER=cat gh repo view --json name,description,url,defaultBranchRef

# List repository branches
PAGER=cat gh api repos/{owner}/{repo}/branches --jq '.[].name'

# Check workflow runs
PAGER=cat gh run list --json conclusion,status,name,createdAt
```

### Workflow Operations

```bash
# View workflow run
PAGER=cat gh run view 12345 --json conclusion,status,jobs

# List workflow runs
PAGER=cat gh run list --workflow "CI" --json databaseId,conclusion,status
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