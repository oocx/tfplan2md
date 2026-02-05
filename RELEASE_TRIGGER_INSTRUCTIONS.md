# Triggering the v1.11.0 Release

## Problem Summary

The v1.11.0 release commit contains `[skip ci]` in its message, which prevents GitHub Actions from running ANY workflow triggered by pushing that commit or tags pointing to it. **This includes annotated tags** - GitHub Actions checks the commit message, not the tag annotation.

## Root Cause - UPDATED UNDERSTANDING

**Critical Discovery**: Even with annotated tags, GitHub Actions checks the **COMMIT message** for `[skip ci]`, not the tag annotation. If the commit that a tag points to has `[skip ci]`, the workflow will NOT run, regardless of tag type.

From GitHub documentation:
> "If any commit message in your push—including the commit to which the tag points—contains `[skip ci]`, then workflows triggered by `push` (including those set to run on tag push) will not run. This applies to annotated tags and not just branch commits."

## Current State

- **Release commit**: `c0a0e01265be22aee696835c4045fba402250ce9`
- **Commit message**: `chore(release): 1.11.0 [skip ci]` ❌ (blocks all workflows)
- **CHANGELOG**: Updated with v1.11.0 entries ✅
- **Tag status**: Annotated tag exists on remote ✅ (but doesn't help due to [skip ci] in commit)
- **Workflow fix**: Removed `[skip ci]` step from CI workflow (this PR) ✅
- **Release workflow fix**: Added `workflow_dispatch` for manual triggering (this PR) ✅

## Solution for v1.11.0

Since the commit c0a0e01 has `[skip ci]` and is already on main, and creating tags won't work regardless of type, the solution is to **manually trigger the release workflow** using the new `workflow_dispatch` capability.

### Option 1: Manual Trigger via GitHub UI (Recommended)

1. Go to: https://github.com/oocx/tfplan2md/actions/workflows/release.yml
2. Click "Run workflow" button (top right)
3. Enter tag: `v1.11.0`
4. Click "Run workflow"

### Option 2: Manual Trigger via GitHub CLI

```bash
gh workflow run release.yml -f tag=v1.11.0
```

## What Will Happen

1. The release workflow will be manually triggered for tag v1.11.0
2. The workflow will checkout the v1.11.0 tag (commit c0a0e01)
3. The workflow will create a GitHub Release with release notes from the CHANGELOG
4. The workflow will build and push the Docker image with tags:
   - `tfplan2md:1.11.0`
   - `tfplan2md:1.11`
   - `tfplan2md:1`
   - `tfplan2md:latest`

## Verification

After manually triggering the workflow, verify:
1. Check the workflow run: https://github.com/oocx/tfplan2md/actions/workflows/release.yml
2. Verify the GitHub release: https://github.com/oocx/tfplan2md/releases/tag/v1.11.0
3. Verify the Docker image: https://hub.docker.com/r/oocx/tfplan2md/tags

## Long-term Fix

This PR makes two changes to prevent this issue for future releases:

### 1. Removed [skip ci] from CI Workflow
The CI workflow no longer adds `[skip ci]` to version bump commits. The existing "Detect release-worthy changes" logic already prevents infinite loops by checking if there are meaningful changes since the last tag.

### 2. Added Manual Trigger to Release Workflow
The release workflow now supports `workflow_dispatch`, allowing manual triggering as a fallback if automatic tag-based triggering fails.

### Why Future Releases Will Work

- When CI creates a version bump commit and pushes it with a tag:
  - The commit does NOT have `[skip ci]` ✅
  - The tag push triggers the release workflow ✅
  - The commit push triggers CI again, but "Detect release-worthy changes" finds no new changes since the tag just created, so versionize is skipped ✅
- No infinite loop, no `[skip ci]`, release workflow runs properly

## Why the Annotated Tag Solution Didn't Work

**Important Note**: The previous instructions suggested using an annotated tag, based on incorrect understanding. This does NOT work because:
- GitHub Actions checks the **commit message** for `[skip ci]`, not the tag annotation
- Even if the tag annotation is clean, if the commit has `[skip ci]`, workflows are skipped
- This is documented GitHub behavior: "If any commit message in your push—including the commit to which the tag points—contains `[skip ci]`, then workflows triggered by `push` will not run. This applies to annotated tags."
