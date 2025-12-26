---
name: run-uat
description: Run User Acceptance Testing by creating a PR with rendered markdown on GitHub or Azure DevOps. Use when validating markdown rendering in real platforms.
compatibility: Requires git. GitHub UAT uses repo scripts which require GitHub CLI (gh) authenticated. Azure DevOps UAT requires Azure CLI (az) + azure-devops extension. Network access required.
---

# Run UAT

## Purpose
Execute end-to-end User Acceptance Testing by posting the comprehensive demo artifact to a real PR comment on GitHub or Azure DevOps, then validating the rendered output.

## Hard Rules
### Must
- Generate fresh artifacts before posting (use `generate-demo-artifacts` skill).
- Create a unique UAT branch to avoid collisions.
- Default to the comprehensive demo artifact when there are no special requirements.
- Clean up (close/abandon) the UAT PR after testing.
- Report platform-specific rendering issues clearly.

### Must Not
- Post a minimal or simulation artifact (reject files containing "minimal" or "simulation" in the name).
- Leave UAT PRs open after testing completes.
- Modify any source code during UAT.

## Pre-requisites
- GitHub UAT: repo scripts require GitHub CLI (`gh`) authenticated.
- Azure DevOps UAT: Azure CLI (`az`) with DevOps extension.

For read-only inspection of PR state/comments during UAT (outside the scripts), prefer GitHub chat tools when available.

## Actions

### Recommended: Single Wrapper Command
This repository provides a stable wrapper that creates UAT PR(s), polls for approval, and cleans up in one command.

```bash
# Default artifact selection:
# - GitHub: $UAT_ARTIFACT_GITHUB (fallback: artifacts/comprehensive-demo.md)
# - AzDO:   $UAT_ARTIFACT_AZDO   (fallback: artifacts/comprehensive-demo.md)
scripts/uat-run.sh
```

If a feature requires a different report (e.g., summary-only, custom template tests), pass the artifact explicitly:

```bash
scripts/uat-run.sh artifacts/<feature-specific-report>.md
```

If you need to target only one platform:
```bash
scripts/uat-run.sh --platform github
scripts/uat-run.sh --platform azdo

# Or with an explicit artifact override for a feature-specific UAT:
scripts/uat-run.sh artifacts/<feature-specific-report>.md --platform github
scripts/uat-run.sh artifacts/<feature-specific-report>.md --platform azdo
```

### 0. Recommended: Rebase on Latest Main
Before running UAT, ensure your branch is up to date to avoid testing against stale base changes.
Use the `git-rebase-main` skill.

### 1. Create UAT Branch
```bash
# Generate unique branch name
original_branch=$(git branch --show-current)
timestamp=$(date -u +%Y%m%d%H%M%S)
uat_branch="${original_branch}-uat-${timestamp}"

# Create and switch to UAT branch
git checkout -b "$uat_branch"
```

### 2. Generate Fresh Artifacts
Use the `generate-demo-artifacts` skill to ensure the artifact is current.

### 3. Validate Artifact
```bash
artifact="artifacts/comprehensive-demo.md"

# Reject known bad artifacts
if echo "$artifact" | grep -qiE '(minimal|simulation)'; then
  echo "ERROR: Refusing to use a minimal/simulation artifact for UAT."
  exit 1
fi

# Verify file exists and is substantial
if [[ ! -s "$artifact" ]] || [[ $(wc -l < "$artifact") -lt 50 ]]; then
  echo "ERROR: Artifact is missing or too small. Generate it first."
  exit 1
fi

echo "Artifact validated: $artifact ($(wc -l < "$artifact") lines)"
```

### 4. Run UAT on GitHub
```bash
scripts/uat-github.sh create "$artifact"
# Note the PR number from output

# Poll for rendering and approval
scripts/uat-github.sh poll <pr-number>

# After validation, cleanup
scripts/uat-github.sh cleanup <pr-number>
```

### 5. Run UAT on Azure DevOps
```bash
# Setup (first time only)
scripts/uat-azdo.sh setup

# Create PR and post artifact
scripts/uat-azdo.sh create "$artifact"
# Note the PR ID from output

# Poll for rendering and approval
scripts/uat-azdo.sh poll <pr-id>

# After validation, cleanup
scripts/uat-azdo.sh cleanup <pr-id>
```

### 6. Return to Original Branch
```bash
git checkout "$original_branch"
```

## Golden Example
```bash
$ scripts/uat-github.sh create artifacts/comprehensive-demo.md
[INFO] Pushing branch to GitHub...
[INFO] Creating PR...
PR created: #42
[INFO] Posted comprehensive-demo.md as comment on PR #42

$ scripts/uat-github.sh poll 42
[INFO] Checking PR #42 for new comments...
[INFO] Found 1 new comment(s). Waiting for approval...

$ scripts/uat-github.sh cleanup 42
[INFO] Closing PR #42...
[INFO] UAT complete.
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Refusing to use minimal artifact" | Run `generate-demo-artifacts` skill first |
| "Branch already exists" | Delete old UAT branch: `git branch -D <branch>` |
| "gh: command not found" | Install GitHub CLI: `brew install gh` or `apt install gh` |
| Azure DevOps auth fails | Run `az login` and `scripts/uat-azdo.sh setup` |
