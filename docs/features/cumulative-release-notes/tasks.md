# Tasks: Cumulative Release Notes

## Overview

This feature modifies the release workflow to include all changes since the last GitHub release in the release notes. This is particularly important for Docker deployments which are triggered manually and may skip several intermediate versions.

Reference:
- [Specification](specification.md)
- [Architecture](architecture.md)

## Tasks

### Task 1: Develop Multi-Version Extraction Logic

**Priority:** High

**Description:**
Create a robust bash script or command sequence that identifies the last GitHub release version and extracts all relevant changelog entries from `CHANGELOG.md`.

**Acceptance Criteria:**
- [x] Uses `gh release list --limit 1 --json tagName --jq '.[0].tagName'` (or similar) to find the last release.
- [x] If no previous release exists, extracts only the current version's section.
- [x] If a previous release exists, extracts all sections from the current version (inclusive) down to the last release (exclusive).
- [x] Preserves all Markdown formatting, including headers, lists, and links.
- [x] Handles version strings with and without the `v` prefix consistently.
- [x] Logic is idempotent (re-running for the same version produces the same output).

**Dependencies:** None

---

### Task 2: Update Release Workflow

**Priority:** High

**Description:**
Integrate the new extraction logic into the `.github/workflows/release.yml` file.

**Acceptance Criteria:**
- [x] The `Extract changelog for version` step in `release.yml` is replaced with the new logic.
- [x] The `release-notes.md` file generated contains the accumulated changes.
- [x] The `softprops/action-gh-release` step uses the accumulated `release-notes.md`.
- [x] The workflow correctly handles both `push: tags` and `workflow_dispatch` triggers.
- [x] The `GITHUB_TOKEN` has sufficient permissions to run `gh release list`.

**Dependencies:** Task 1

---

### Task 3: Verification and Edge Case Testing

**Priority:** Medium

**Description:**
Validate the implementation against various scenarios using a test script or manual verification with mock data.

**Acceptance Criteria:**
- [x] Test Case 1: First release (no previous releases) -> Only current version notes.
- [x] Test Case 2: Consecutive versions (v0.1.0 then v0.1.1) -> Only v0.1.1 notes.
- [x] Test Case 3: Version gap (v0.1.0 then v0.5.0) -> Notes for v0.1.1 through v0.5.0 (if they exist in CHANGELOG.md).
- [x] Test Case 4: Re-run of same version -> Same notes as previous run.
- [x] Test Case 5: Version not found in CHANGELOG.md -> Graceful fallback (e.g., "Release vX.Y.Z").

**Dependencies:** Task 2

---

### Task 4: Documentation and Cleanup

**Priority:** Low

**Description:**
Ensure all documentation is up to date and the feature branch is ready for merge.

**Acceptance Criteria:**
- [x] `docs/features/cumulative-release-notes/tasks.md` is committed.
- [x] Architecture and Specification documents are updated if any implementation details changed.
- [x] Feature branch is clean and follows project conventions.

**Dependencies:** Task 3

## Implementation Order

1. **Task 1** - Foundational logic for extraction.
2. **Task 2** - Integration into the CI/CD pipeline.
3. **Task 3** - Ensuring robustness and handling edge cases.
4. **Task 4** - Final documentation and handoff.

## Open Questions

- Should we include a "Changes since last Docker release" header in the accumulated notes, or just list the versions as they appear in the changelog? (Recommendation: Keep the changelog format as is, as it's already clear).
