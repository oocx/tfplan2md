# UAT Report: Compact Collection Diffs

## Status

Decision: UAT PASSED, judged on GitHub PR 128 alone. Azure DevOps not run (no token). PR 128 closed.

## UAT PRs

| Platform | PR | Status |
|---|---|---|
| GitHub | https://github.com/oocx/tfplan2md-uat/pull/128 | Closed after the decision; 2 comments (feature test, comprehensive demo regression) |
| Azure DevOps | not created | Blocked: no AZDO_UAT_TOKEN / AZURE_DEVOPS_EXT_PAT available to the agent |
| Bitbucket | not tested | No UAT tooling in repo |

Overview pages: https://github.com/oocx/tfplan2md-uat/pulls and
https://dev.azure.com/oocx/test/_git/test/pullrequests?_a=mine

## What was posted

- Feature report: `docs/features/148-compact-collection-diffs/uat-plan.md` (rendered from
  `uat-plan.json`, github flavour, default options) with resource-specific test instructions.
- Regression: `artifacts/comprehensive-demo-simple-diff.md`, regenerated for current code
  (commit "docs: regenerate demo artifacts for collection diff UAT").
- Only the default rendering run was posted; the other rendering runs of uat-test-plan.md
  (--drift variants, --show-sensitive, --show-unchanged-values, case filter off, name mapping)
  are not on the platforms.

## Tooling problems encountered

- Working tree not clean (driver-owned state.json modified): stashed around each script run, restored after.
- Demo artifacts were outdated: regenerated and committed locally (not pushed).
- `uat-repos/github` and `uat-repos/azdo` submodule gitlinks are not tracked on main
  (only `.gitmodules`), so `git submodule update --init` does nothing and uat-run.sh failed with
  "pathspec 'uat-repos/github' did not match". Worked around with UAT_GITHUB_SUBMODULE_PATH /
  AZDO_SUBMODULE_PATH pointing at clones in the scratchpad. Needs a Workflow Engineer fix.
- Full AzDO clone is very slow; a shallow clone was used.
- Cleanup state is in `.tmp/uat-run/last-run.json` (GitHub PR 128; copy in scratchpad
  `last-run-github.json`).

## Results

Maintainer decision: UAT PASSED on GitHub PR 128. No platform-rendered defects reported. Azure DevOps and Bitbucket were not validated.

A stash mix-up during cleanup (a stray `git stash pop` applied an unrelated stash) was repaired by the Maintainer's reset; nothing was lost.
