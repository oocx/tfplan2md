## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Robust UAT polling (real approval + raw comments) | issue #367 | ✅ Done | Current keyword matching can report false positives/negatives; scripts should surface platform approval state and full maintainer comments for agent analysis. | High | Med | Med | Implemented: GitHub uses PR review state; AzDO uses reviewer vote/status; both print raw non-agent comments (and support `--json`). |
| 2 | Prevent UAT branch data loss (no upstream switching) | issue #368 | ✅ Done | UAT scripts should not change branch upstream/remotes in a way that can cause subsequent pushes to go to the UAT repo accidentally. | High | Low | Low | Implemented: removed `git push -u` for GitHub/AzDO; pushes use explicit refspec without changing branch upstream. |
| 3 | Reduce UAT repo overhead (stop mirroring main repo) | issue #369 | ✅ Done | Current push-to-UAT-repo approach can be very slow/large; UAT repo should be independent with minimal content pushed. | High | High | Med | Implemented: UAT now uses two dedicated git submodules (GitHub + AzDO UAT repos) and creates lightweight marker commits/branches in those repos only (no main-repo branch mirroring). |
| 4 | Remove simulation wording and logic | issue #370 | ✅ Done | Non-real/dry-run UAT language causes repeated confusion and wasted cycles; only real GitHub/AzDO UAT should be referenced. | Med | Low | Low | Implemented: removed simulation mode from scripts; updated agent prompts + UAT test plans to say “Run UAT” (real GitHub/AzDO). |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1** — Fixes the most harmful failure mode (false “pass”) and aligns UAT with platform review/vote states.
- **Option 2 (Quick win):** **4** — Low effort cleanup that removes a recurring confusion source.
- **Option 3 (Highest impact):** **3** — Tackles the big latency/overhead problem, but is a larger redesign.

## Decision
Which item should I implement first? (Reply with the Option number, or reply with "work on task <task id>")
