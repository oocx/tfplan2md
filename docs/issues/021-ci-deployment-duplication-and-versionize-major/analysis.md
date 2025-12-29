# Analysis: CI Deployment Duplication and Versionize Major Version Bump

## Problem Description

Two related issues were identified in the CI/CD pipeline:

1.  **Duplicate Deployments:** The release workflow was being triggered twice for the same version. This was likely due to a combination of tag-based triggers and manual triggers (or `workflow_dispatch` being available and potentially misused or misunderstood).
2.  **Unwanted Major Version Bumps:** The project is currently in an initial development phase (0.x). However, commits marked with `BREAKING CHANGE` were causing [Versionize](https://github.com/versionize/versionize) to bump the version to `1.0.0` (major version bump), which signals a stable release prematurely.

## Root Cause Analysis

### Duplicate Deployments
The `release.yml` workflow was configured with both `push: tags: ['v*']` and `workflow_dispatch`. While `workflow_dispatch` allows manual triggering, the primary release mechanism is the tag pushed by the `ci.yml` workflow. Having both active, or potentially triggering the workflow manually after a tag push, led to confusion and duplicate runs.

### Unwanted Major Version Bumps
By default, Versionize follows Semantic Versioning rules strictly. A `BREAKING CHANGE` footer in a commit message triggers a major version bump. Since the project was at `0.x.x`, the next major version is `1.0.0`. To maintain a `0.x.x` versioning scheme or use pre-release versions (e.g., `1.0.0-alpha.x`) while still allowing breaking changes, Versionize needs to be configured explicitly.

## Solution Implementation

### 1. Enforce Pre-release Versioning
The `ci.yml` workflow was updated to use the `--pre-release` flag with Versionize.

```yaml
- name: Versionize
  id: versionize
  run: |
    versionize --exit-insignificant-commits --skip-dirty --pre-release alpha
    echo "version_bumped=true" >> $GITHUB_OUTPUT
```

This ensures that:
- Breaking changes result in a pre-release version bump (e.g., `1.0.0-alpha.1`) instead of a stable `1.0.0`.
- The project can continue to evolve with breaking changes without implying API stability.

### 2. Remove Manual Release Trigger
The `workflow_dispatch` trigger was removed from `.github/workflows/release.yml`. The release workflow is now exclusively triggered by pushing a tag that matches the `v*` pattern. This eliminates the ambiguity and potential for duplicate manual runs.

### 3. Conditional Docker Tagging
The `release.yml` workflow was updated to handle pre-release versions correctly. It now checks if the version is a pre-release (contains a hyphen).

- **Stable Releases:** Tagged with `version`, `major`, `minor`, and `latest`.
- **Pre-release Versions:** Tagged ONLY with the specific `version`. They do NOT update `latest`, `major`, or `minor` tags.

```yaml
      - name: Compute Docker tags
        id: docker_tags
        run: |
          TAGS="${{ secrets.DOCKERHUB_USERNAME }}/tfplan2md:${{ needs.release.outputs.version }}"
          if [ "${{ needs.release.outputs.is_prerelease }}" != "true" ]; then
            TAGS="$TAGS\n${{ secrets.DOCKERHUB_USERNAME }}/tfplan2md:${{ needs.release.outputs.minor }}"
            TAGS="$TAGS\n${{ secrets.DOCKERHUB_USERNAME }}/tfplan2md:${{ needs.release.outputs.major }}"
            TAGS="$TAGS\n${{ secrets.DOCKERHUB_USERNAME }}/tfplan2md:latest"
          fi
```

### 4. GitHub Release Prerelease Status
The `softprops/action-gh-release` step was updated to mark the release as a "Prerelease" on GitHub if the version string indicates it is one.

```yaml
          prerelease: ${{ steps.version.outputs.is_prerelease == 'true' }}
```

## Verification
- **Versionize Behavior:** Verified locally that `versionize --pre-release alpha` correctly bumps to `alpha` versions even with breaking changes.
- **Workflow Logic:** The `release.yml` changes were committed and the logic for `is_prerelease` extraction was verified.
- **Cleanup:** Accidental `1.x` releases and tags were removed from the repository and Docker Hub.
