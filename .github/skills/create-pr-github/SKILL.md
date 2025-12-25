---
name: create-pr-github
description: Create and (optionally) merge a GitHub pull request using gh, following the repo policy to use rebase and merge for a linear history.
compatibility: Requires git and GitHub CLI (gh) authenticated, plus network access.
---

# Create PR (GitHub)

## Purpose
Create a GitHub pull request in a consistent, policy-compliant way, and include the repo’s preferred merge method guidance (rebase and merge).

This skill prefers using the repo wrapper script `scripts/pr-github.sh` to minimize Maintainer approval interruptions (single terminal invocation).

## Hard Rules
### Must
- Work on a non-`main` branch.
- Ensure the working tree is clean before creating a PR.
- Push the branch to `origin` before creating the PR.
- Before creating the PR, post a short PR preview in chat:
  - **Title**: the exact PR title you plan to use
  - **Summary**: 1–3 bullets describing the change
- Use **Rebase and merge** for merging PRs to maintain a linear history (see `CONTRIBUTING.md`).

### Must Not
- Create PRs from `main`.
- Use “Squash and merge” or “Create a merge commit”.

## Actions

### 0. PR Preview (Required)
Before running any PR creation command, provide in chat:
- **PR title** (exact)
- **PR summary** (1–3 bullets)

Recommended way to generate the preview (best-effort, based on current branch diff):
```bash
scripts/pr-github.sh preview --fill
```

### Recommended: One-Command Wrapper
Create a PR:
```bash
scripts/pr-github.sh create --fill
```

Create and merge (only when explicitly requested):
```bash
scripts/pr-github.sh create-and-merge --fill
```

### 1. Pre-flight Checks
```bash
git branch --show-current
git status --short
```

### 2. Push the Branch
```bash
git push -u origin HEAD
```

### 3. Create the PR
```bash
PAGER=cat gh pr create \
  --base main \
  --head "$(git branch --show-current)" \
  --title "<type(scope): summary>" \
  --body "<why + what + testing notes>"
```

### 4. Merge (Only When Explicitly Requested)
This repository requires **rebase and merge**.

```bash
PAGER=cat gh pr merge <pr-number> --rebase --delete-branch
```

### 5. If Rebase-Merge Is Blocked (Conflicts)
```bash
git pull --rebase origin main
# resolve conflicts

git push --force-with-lease
```

Then retry the merge.
