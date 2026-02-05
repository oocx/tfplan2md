# Triggering the v1.11.0 Release

## Problem Summary

The v1.11.0 release was prepared by the CI workflow, but the tag was never pushed to the remote repository due to a workflow issue that has since been fixed in commit `5b84833`.

## Current State

- **Release commit**: `c0a0e01265be22aee696835c4045fba402250ce9`
- **Commit message**: `chore(release): 1.11.0 [skip ci]`
- **CHANGELOG**: Updated with v1.11.0 entries
- **Tag status**: Tag `v1.11.0` exists locally but NOT on the remote repository
- **Fix applied**: Commit `5b84833` fixed the workflow to properly move tags after commit amends

## Solution

To trigger the release workflow for v1.11.0, the tag needs to be pushed to the remote repository:

```bash
# Create the tag on the release commit
git tag v1.11.0 c0a0e01265be22aee696835c4045fba402250ce9

# Push the tag to trigger the release workflow
git push origin v1.11.0
```

## What Will Happen

1. Pushing the `v1.11.0` tag will trigger the `.github/workflows/release.yml` workflow
2. The workflow will create a GitHub Release with release notes from the CHANGELOG
3. The workflow will build and push the Docker image with tags:
   - `tfplan2md:1.11.0`
   - `tfplan2md:1.11`
   - `tfplan2md:1`
   - `tfplan2md:latest`

## Verification

After pushing the tag, verify:
1. Check the release workflow run: https://github.com/oocx/tfplan2md/actions/workflows/release.yml
2. Verify the GitHub release: https://github.com/oocx/tfplan2md/releases/tag/v1.11.0
3. Verify the Docker image: https://hub.docker.com/r/oocx/tfplan2md/tags

## Future Prevention

The fix in commit `5b84833` ensures this issue won't happen again. The workflow now properly moves tags to amended commits before pushing.
