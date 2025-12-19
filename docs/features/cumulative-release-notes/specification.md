# Feature: Cumulative Release Notes

## Overview

When manually triggering a release to Docker Hub and GitHub, the release notes should include all changes since the last GitHub release, not just changes for the current version. This ensures users see all accumulated changes in Docker deployments, even when not every version is released.

## User Goals

- **Docker Hub users** want to see all changes included in a Docker image release, not just the changes for the latest version tag
- **Maintainers** want to skip releasing certain versions to Docker Hub while still tracking them in git/changelog
- **End users** need a clear understanding of what changed between Docker releases, which may span multiple version increments

## Scope

### In Scope
- Modify the release workflow to extract changelog sections for all versions since the last GitHub release
- Accumulate changelog content from multiple version sections in CHANGELOG.md
- Replace the single-version release notes with accumulated notes when creating GitHub releases
- Support the existing workflow_dispatch trigger mechanism

### Out of Scope
- Automatic determination of which versions should be released to Docker Hub
- Changes to the CI workflow or Versionize configuration
- Changes to how CHANGELOG.md is generated
- Modifying how version tags are created
- Creating separate tracking mechanisms for Docker deployments (tags/labels/files)

## User Experience

### Current Behavior
1. Maintainer manually triggers release workflow for v0.12.0
2. GitHub release contains only changes from v0.12.0
3. Docker image is tagged and pushed with those release notes

### New Behavior
1. Maintainer manually triggers release workflow for v0.12.0
2. Workflow detects the last GitHub release was v0.8.0
3. Workflow extracts changelog sections for v0.9.0, v0.10.0, v0.11.0, and v0.12.0
4. GitHub release contains accumulated changes from all four versions
5. Docker image is tagged and pushed with the complete release notes

### Example Output
```markdown
## [0.12.0] - 2025-12-18
### Features
- Feature from v0.12.0

## [0.11.0] - 2025-12-18
### Features
- Feature from v0.11.0

## [0.10.0] - 2025-12-17
### Features
- Feature from v0.10.0

## [0.9.0] - 2025-12-16
### Features
- Feature from v0.9.0
```

## Success Criteria

- [ ] When triggering a release with no previous GitHub releases, release notes contain only the current version's changes
- [ ] When triggering a release after skipped versions, release notes contain all changes since the last GitHub release
- [ ] The accumulated release notes maintain the original CHANGELOG.md formatting and structure
- [ ] Version headers and dates are preserved in the accumulated notes
- [ ] The workflow continues to work with the existing workflow_dispatch trigger
- [ ] Both automatic (tag push) and manual (workflow_dispatch) triggers produce correct release notes

## Open Questions

None - requirements are clear.
