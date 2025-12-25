# GitHub CLI Usage Instructions for Agents

## Critical: Prevent Interactive Mode

GitHub CLI (`gh`) may trigger interactive pagers that block agent execution. **Always disable the pager** when running `gh` commands.

## Required Pattern: Always Set PAGER

**Use this pattern for EVERY `gh` command:**

```bash
PAGER=cat gh [command] [options]
```

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
echo "Automated issue body" | PAGER=cat GH_FORCE_TTY=false gh issue create --title "Automated" --body-file -
```

## Common Commands - Correct Usage

### Pull Requests

```bash
# List pull requests
PAGER=cat gh pr list --json number,title,state,author

# View pull request details
PAGER=cat gh pr view 123 --json number,title,body,state,commits,reviews

# Create pull request (preferred: repo wrapper scripts)
# CRITICAL: Show the preview output in chat BEFORE creating/merging a PR.
scripts/pr-github.sh preview --fill
scripts/pr-github.sh create --fill

# Manual fallback (only if wrapper scripts are unavailable)
PAGER=cat gh pr create --title "Title" --body "Description" --base main --head feature-branch

# Check pull request status
PAGER=cat gh pr status --json number,title,state
```

### Issues

```bash
# List issues
PAGER=cat gh issue list --json number,title,state,author

# View issue details
PAGER=cat gh issue view 123 --json number,title,body,state,comments

# Create issue
PAGER=cat gh issue create --title "Title" --body "Description"

# Close issue
PAGER=cat gh issue close 123
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