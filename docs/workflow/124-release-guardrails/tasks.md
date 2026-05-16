## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Enforce release-notes + work-protocol guardrails in PR validation | problem statement / prior-work 079 | ✅ Done | The cloud orchestrator can finish a change without a Release Manager handoff, so release artifacts are skipped silently. | High | Med | Low | Added validation for workflow work items, work-protocol presence, and Release Manager log entries. |
| 2 | Require screenshot targeting metadata for release-note images | problem statement / prior-work 084 | ✅ Done | Screenshot files can exist while still showing the wrong part of the report; reviewers need explicit capture intent. | High | Med | Low | Validates screenshot URLs/files and requires per-image selector/target metadata plus focus rationale. |
| 4 | Deterministic hook: block `report_progress` when work-protocol.md is missing required agent entries | problem statement (deterministic check) | ✅ Done | Agent instructions are non-deterministic and get ignored. A `preToolUse` hook on `report_progress` enforces completeness before any commit is pushed, catching missing agents during the orchestrator session — before CI runs. | High | Low | Low | Hook fires only after Code Reviewer/Workflow Engineer has run, so early-stage pushes are unaffected. |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1 + 2** — Together they make release artifacts mandatory and make screenshot intent reviewable.
- **Option 2 (Quick win):** **1** — Prevents missing Release Manager / release-notes handoffs with the lowest workflow risk.
- **Option 3 (Highest impact):** **2** — Adds the strongest guardrail against misleading screenshots, but works best when paired with option 1.

## Decision
Maintainer selected the requested fixes directly in the task statement: implement both items 1 and 2 in this workflow change.
