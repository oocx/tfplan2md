# Triggering the v1.11.0 Release

## Problem Summary

The v1.11.0 release commit contains `[skip ci]` in its message, which prevents GitHub Actions from running ANY workflow triggered by pushing that commit or tags pointing to it. This includes the release workflow.

## Root Cause

The CI workflow was adding `[skip ci]` to version bump commits to prevent infinite loops. However, this also blocks the release workflow from running when tags are pushed. The `[skip ci]` directive affects:
- Commit pushes (intended - prevents CI from running again)
- Tag pushes to that commit (unintended - blocks release workflow)

## Current State

- **Release commit**: `c0a0e01265be22aee696835c4045fba402250ce9`
- **Commit message**: `chore(release): 1.11.0 [skip ci]` ❌
- **CHANGELOG**: Updated with v1.11.0 entries ✅
- **Tag status**: Lightweight tag exists on remote, but won't trigger workflow due to `[skip ci]`
- **Workflow fix**: Removed `[skip ci]` step from CI workflow (this PR)

## Solution for v1.11.0

Since we cannot change the commit message (it's already on main), we need to use an **annotated tag** instead of a lightweight tag. GitHub Actions checks the tag annotation for `[skip ci]`, not the commit message, when a tag is pushed.

```bash
# Delete the existing lightweight tag (both locally and remotely)
git tag -d v1.11.0
git push origin :refs/tags/v1.11.0

# Create an annotated tag WITHOUT [skip ci] in the annotation
git tag -a v1.11.0 c0a0e01265be22aee696835c4045fba402250ce9 -m "Release v1.11.0"

# Push the annotated tag to trigger the release workflow
git push origin v1.11.0
```

**Important**: The annotated tag annotation must NOT contain `[skip ci]`. The commit message still has it, but GitHub Actions will check the tag annotation for tag pushes.

## What Will Happen

1. Deleting and recreating the tag will trigger the `.github/workflows/release.yml` workflow
2. The annotated tag annotation does not contain `[skip ci]`, so the workflow will run
3. The workflow will create a GitHub Release with release notes from the CHANGELOG
4. The workflow will build and push the Docker image with tags:
   - `tfplan2md:1.11.0`
   - `tfplan2md:1.11`
   - `tfplan2md:1`
   - `tfplan2md:latest`

## Verification

After pushing the annotated tag, verify:
1. Check the release workflow run: https://github.com/oocx/tfplan2md/actions/workflows/release.yml
2. Verify the GitHub release: https://github.com/oocx/tfplan2md/releases/tag/v1.11.0
3. Verify the Docker image: https://hub.docker.com/r/oocx/tfplan2md/tags

## Long-term Fix

This PR removes the `[skip ci]` step from the CI workflow entirely. The existing "Detect release-worthy changes" logic already prevents infinite loops by checking if there are meaningful changes since the last tag. Future releases will work correctly without needing the `[skip ci]` workaround.

### Why This Works

- When CI creates a version bump commit and pushes it with a tag:
  - The tag push triggers the release workflow ✅
  - The commit push triggers CI again, but "Detect release-worthy changes" finds no new changes since the tag just created, so versionize is skipped ✅
- No infinite loop, no `[skip ci]` needed, release workflow runs properly
