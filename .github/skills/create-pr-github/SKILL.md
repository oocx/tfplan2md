---
name: create-pr-github
description: Create and (optionally) merge a GitHub pull request using gh, following the repo policy to use rebase and merge for a linear history.
compatibility: Requires git and GitHub CLI (gh) authenticated, plus network access.
---

# Create PR (GitHub)

## Purpose
Create a GitHub pull request in a consistent, policy-compliant way, and include the repo’s preferred merge method guidance (rebase and merge).

## Hard Rules
### Must
- Work on a non-`main` branch.
- Ensure the working tree is clean before creating a PR.
- Push the branch to `origin` before creating the PR.
- Use **Rebase and merge** for merging PRs to maintain a linear history (see `CONTRIBUTING.md`).

### Must Not
- Create PRs from `main`.
- Use “Squash and merge” or “Create a merge commit”.

## Actions

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
