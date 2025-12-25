---
name: create-pr-azdo
description: Create an Azure DevOps pull request using az devops tooling; include the repo’s linear-history merge preference and ask the Maintainer if merge options differ.
compatibility: Requires git, Azure CLI (az) with azure-devops extension, authentication, and network access.
---

# Create PR (Azure DevOps)

## Purpose
Create an Azure DevOps pull request in a consistent way, while still encoding the repository’s preference for a **linear history**.

This skill prefers using the repo wrapper script `scripts/pr-azdo.sh` to minimize Maintainer approval interruptions (single terminal invocation).

## Notes on Merge Policy
`CONTRIBUTING.md` specifies **Rebase and merge** as the required merge strategy for this repo.

Azure DevOps UI/merge options differ by project settings. When merging an Azure DevOps PR, choose the most “rebase/linear-history” option available (often called **Rebase and fast-forward**) when available; otherwise, ask the Maintainer what to use.

## Hard Rules
### Must
- Work on a non-`main` branch.
- Ensure the working tree is clean before creating a PR.
- Push the branch before creating the PR.
- Keep PR title and description conventional and review-friendly.
- Before creating the PR, post a short PR preview in chat:
  - **Title**: the exact PR title you plan to use
  - **Summary**: 1–3 bullets describing the change

### Must Not
- Merge using a strategy that introduces merge commits unless the Maintainer explicitly requests it.

## Actions

### 0. PR Preview (Required)
Before running any PR creation command, provide in chat:
- **PR title** (exact)
- **PR summary** (1–3 bullets)

Recommended way to generate the preview (best-effort, based on current branch diff):
```bash
scripts/pr-azdo.sh preview --fill
```

### Recommended: One-Command Wrapper
```bash
scripts/pr-azdo.sh create --fill
```

Abandon a test PR (cleanup):
```bash
scripts/pr-azdo.sh abandon --id <pr-id>
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
This is a minimal example; set `--organization`/`--project` appropriately for the target repo.

```bash
az repos pr create \
  --title "<type(scope): summary>" \
  --description "<why + what + testing notes>" \
  --source-branch "$(git branch --show-current)" \
  --target-branch main
```

### 4. Merging
- If you need to merge the PR, confirm the exact merge option with the Maintainer first.
- Prefer a rebase/linear-history option when available.
