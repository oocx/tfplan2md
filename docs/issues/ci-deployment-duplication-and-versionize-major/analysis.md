# Issue: Duplicate deployments + unintended 1.x releases

## Problem Description

Two related CI/release automation issues were observed:

1. A single release event resulted in **three deployments**:
   - **Two automatic** runs triggered by tag pushes
   - **One manual** run triggered by the Release Manager (workflow dispatch)

2. A commit containing a **BREAKING CHANGE** marker caused Versionize to bump from `v0.49.0` to a **`v1.x`** release. The repo is intended to remain in **pre-1.0** mode and should not produce `1.x` tags/releases yet.

## Steps to Reproduce

### Duplicate deployments
1. Push a change to `main` that triggers the CI Versionize step.
2. CI runs `versionize` and pushes the generated tag `vX.Y.Z`.
3. The Release workflow triggers automatically on `push` of `v*` tags.
4. If the Release Manager also triggers the Release workflow manually for the same tag, the release/deploy pipeline runs an additional time.

### Unintended 1.x releases
1. Merge/push a commit to `main` whose body contains a line starting with `BREAKING CHANGE:`.
2. CI runs Versionize.
3. Versionize detects a breaking change and performs a **major** bump.

## Expected Behavior

- Exactly **one** release/deployment per version tag.
- While in pre-1.0 mode, Versionize should **not** produce `v1.x` tags/releases.

## Actual Behavior

- The `Release` workflow was executed multiple times for the same release window:
  - Tag-triggered run (automatic)
  - Manual `workflow_dispatch` run
  - In at least one instance, **two tag-triggered** runs occurred because the same tag (`v1.0.0`) was created twice and moved between commits.

- Versionize created a **major** bump (e.g., `v1.0.0`, `v1.1.0`, `v1.2.0`).

## Root Cause Analysis

### Affected Components

- Release workflow triggers:
  - `.github/workflows/release.yml#L3-L12`
  - `on.push.tags: 'v*'` and `on.workflow_dispatch`

- CI Versionize/tag creation:
  - `.github/workflows/ci.yml#L56-L95`
  - `versionize --exit-insignificant-commits --skip-dirty` then `git push --follow-tags`

- Version source:
  - `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj#L10` contains `<Version>...</Version>`

- Versionize configuration:
  - `.versionize#L1-L54` (changelog sections and `preReleasePrefix` only)

### What’s Broken (1): Duplicate deployments

**Mechanically**, the Release workflow is triggered in two independent ways:

- Automatic tag-triggered runs:
  - `.github/workflows/release.yml#L3-L7`

- Manual runs:
  - `.github/workflows/release.yml#L7-L12`

Once tag-based triggers started working, running the workflow manually for the same tag causes an extra release/deployment.

Additionally, we observed a case where the **same tag (`v1.0.0`) triggered two separate Release runs** because the tag pointed to two different commits at different times.

Evidence:

- `Release` runs for `v1.2.0`:
  - Automatic tag push run: https://github.com/oocx/tfplan2md/actions/runs/20539734896 (event: `push`, headBranch: `v1.2.0`)
  - Manual run: https://github.com/oocx/tfplan2md/actions/runs/20539756045 (event: `workflow_dispatch`)

- Two distinct `Release` runs for `v1.0.0` (both `push` events) with different commit SHAs:
  - https://github.com/oocx/tfplan2md/actions/runs/20531852982 → headSha `840b561c...`
  - https://github.com/oocx/tfplan2md/actions/runs/20532284571 → headSha `b6ef9ad5...`

This indicates the tag `v1.0.0` was created, then later **moved** to point at a different release commit.

We confirmed the two release commits have different parents:

- `840b561c...` parent: `2046e43...` (and is not contained in any branch now)
- `b6ef9ad5...` parent: `8d6d563...` (current `v1.0.0` tag)

That implies there was at least one non-fast-forward/force update or tag recreation during that window.

### What’s Broken (2): Versionize creates 1.x major bumps

Versionize’s documented behavior is:

- `feat` → minor bump
- `fix` → patch bump
- `BREAKING CHANGE:` (in commit body) → major bump

In this repo, there is an explicit `BREAKING CHANGE:` commit in the range between `v0.49.0` and `v1.0.0`:

- Commit: `4338625` (`refactor(scripts): ...`)
- Contains: `BREAKING CHANGE: PR scripts no longer accept ...`

This is sufficient for Versionize to choose a major version bump.

The attempted configuration change in `8d6d563` (“configure versionize to stay in v0.x.x prerelease mode”) only sets `preReleasePrefix` in `.versionize#L53`.

Based on Versionize’s documented behavior, `preReleasePrefix` (and the `--pre-release` flag) influences **suffixes** like `-alpha.0`, but does **not** change the “breaking change → major bump” rule.

## Suggested Fix Approach (High-level)

### Prevent duplicate deployments

- Pick one canonical release trigger path:
  - **Option A:** Tag triggers only. Keep `on.push.tags` and remove or heavily restrict `workflow_dispatch`.
  - **Option B:** Manual only. Remove tag trigger and rely on `workflow_dispatch`.

- If keeping `workflow_dispatch`, consider adding process/guardrails:
  - Document: “Do not manually dispatch Release when tag triggers are enabled.”
  - Add concurrency cancellation for `release-${{ github.ref }}` (currently `cancel-in-progress: false` at `.github/workflows/release.yml#L14-L16`).

- Investigate and prevent tag movement:
  - Ensure tags are never force-updated.
  - Consider protecting tags or adding a workflow check that fails if a tag already exists.

### Keep repo in pre-1.0 (prevent 1.x tags)

Versionize does not appear to provide a config option to “cap major at 0”. Practical options are:

- **Recommended (process):** While in pre-1.0 mode, do **not** use `BREAKING CHANGE:` markers in commit bodies.
  - Use alternate phrasing (e.g., “Breaking:” without the exact prefix) until ready for `1.0.0`.

- **Alternative (automation):** Wrap Versionize in CI logic:
  - Detect when Versionize would bump major from `0.*` to `1.*`.
  - Override with `versionize --release-as 0.<nextMinor>.0` (or similar).

- **Alternative (policy enforcement):** Fail CI if a commit body contains `BREAKING CHANGE:` until the project officially moves to 1.0.

## Related Tests / Validation

- Verify CI still tags and pushes:
  - GitHub Actions workflow: `CI` / job `Version and Tag`

- Verify only a single Release run happens for a tag:
  - GitHub Actions workflow: `Release`

## Additional Context / Notes

- Current Release trigger configuration is dual-triggered (`push.tags` and `workflow_dispatch`): `.github/workflows/release.yml#L3-L12`.
- Versioning happens automatically on `push` to `main` and performs `git push --follow-tags`: `.github/workflows/ci.yml#L56-L95`.
