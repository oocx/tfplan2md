# Architecture: Cumulative Release Notes

## Status

Implemented

## Context

The release workflow currently extracts changelog content for only the version being released. Since not every version is deployed to Docker Hub, users need to see all changes accumulated since the last Docker deployment.

The workflow creates both GitHub releases and Docker images in a single run, triggered by tag push events (`v*`).

Currently, the "Extract changelog for version" step in [.github/workflows/release.yml](.github/workflows/release.yml) uses awk to extract only the section for the current version from CHANGELOG.md.

## Options Considered

### Option 1: Query GitHub Releases API and Extract Multiple Sections

**Description:**
- Add a step to query the GitHub API for the latest release
- If no release exists, fall back to current behavior (single version)
- If a release exists, extract all version sections between the last release version and current version
- Use awk/sed to accumulate all matching sections from CHANGELOG.md

**Pros:**
- No additional state files or tracking needed
- Works automatically - the GitHub releases themselves track what was deployed
- Handles the first release case naturally (no previous release = include only current version)
- Simple bash scripting using GitHub CLI or API

**Cons:**
- Requires GitHub API access (already available via GITHUB_TOKEN)
- Slightly more complex bash logic for version comparison
- Need to handle edge cases (deleted releases, draft releases)

### Option 2: Track Last Docker Release in Repository File

**Description:**
- Create a file like `.last-docker-release` containing the last released version
- Update this file at the end of successful releases
- Use this file to determine the range of versions to include

**Pros:**
- Simple version tracking
- No API calls needed

**Cons:**
- Requires committing state files back to the repository
- Adds complexity to the workflow (commit and push)
- Out of sync if file is manually edited or gets stale
- Introduces new state that can drift from GitHub releases
- Violates principle of releases as source of truth

### Option 3: Use Git Tags with Prefix for Docker Deployments

**Description:**
- Create additional tags like `docker-v0.12.0` when deploying to Docker Hub
- Query git tags with the `docker-` prefix to find last deployment

**Pros:**
- Git-based tracking
- No API dependencies

**Cons:**
- Duplicate tagging system adds confusion
- Still requires parsing and extracting version ranges
- More complex tag management
- Doesn't align with existing single-tag approach

## Decision

**Option 1: Query GitHub Releases API and Extract Multiple Sections**

## Rationale

- **Releases are source of truth**: GitHub releases already represent what was deployed. They're created by the same workflow that deploys to Docker Hub, making them the natural source of truth.
- **Minimal changes**: Only the changelog extraction step needs modification.
- **Idempotent**: Re-running the workflow for the same version produces the same result.
- **Self-documenting**: No hidden state files or additional tags to manage.
- **Handles edge cases**: When there are no previous releases, naturally falls back to single-version behavior.

## Consequences

### Positive
- No additional state management or repository modifications needed
- Works seamlessly with the tag-push trigger
- Easy to test and validate
- Maintains backward compatibility with existing workflow

### Negative
- Slightly more complex bash scripting
- Depends on GitHub API availability (mitigated by using built-in GITHUB_TOKEN)
- Requires careful version comparison logic

## Implementation Notes

### Workflow Modifications

Modify the "Extract changelog for version" step in [.github/workflows/release.yml](.github/workflows/release.yml#L47-L56):

1. **Query for last release:**
   - Use `gh release list` to get the latest non-draft release
   - Extract the version number from the release tag
   - If no releases exist, set `LAST_VERSION=""` (first release case)

2. **Extract version sections:**
   - If `LAST_VERSION` is empty, extract only current version (existing behavior)
   - If `LAST_VERSION` exists, extract all version sections between `LAST_VERSION` (exclusive) and current version (inclusive)
   - Use awk to accumulate multiple version blocks from CHANGELOG.md

3. **Version comparison:**
   - Parse semantic versions for comparison (e.g., 0.12.0 > 0.8.0)
   - Handle version formats with or without 'v' prefix
   - Respect CHANGELOG.md ordering (newest first)

### CHANGELOG.md Format Assumptions

The solution relies on the existing CHANGELOG.md structure:
- Version headers follow the pattern: `## [VERSION]` or `<a name="VERSION"></a>\n## [VERSION]`
- Versions are in descending order (newest first)
- Each version section is complete and self-contained

### Edge Cases to Handle

1. **First release (no previous releases)**: Extract only current version
2. **Re-running release for same version**: Extract same content (idempotent)
3. **Consecutive versions**: Extract only current version (no gap)
4. **Version not in CHANGELOG**: Create minimal release notes with version header

### Testing Strategy

- Unit test the awk script with sample CHANGELOG.md content
- Test with mock GitHub releases in different scenarios:
  - No previous release
  - One previous release with version gaps
  - Consecutive releases
- Verify formatting is preserved across multiple version blocks
- Test the tag-push trigger

### Tools and Dependencies

- **GitHub CLI (`gh`)**: Available in GitHub Actions runners by default
- **awk**: Available in ubuntu-latest runners
- **GITHUB_TOKEN**: Automatically provided in GitHub Actions context
