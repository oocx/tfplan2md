# Code Reviewer Agent Improvements - Task List

## Background

During Feature 068 (Parent-Child Resource Grouping), the Code Reviewer agent missed a critical issue: the Azure AD group template was missing the `{{ include "/_child_resources.sbn" }}` directive, causing ALL member tables to be absent from the generated output. The issue was only caught during UAT.

**Root cause:** The reviewer focused on complex configuration reference matching logic and trusted snapshot tests without manually generating and inspecting the actual rendered output.

## Candidate Workflow Improvements

Based on the detailed root cause analysis in `docs/features/068-parent-child-resource-grouping/code-review-post-uat-fixes.md`, the following 5 improvements would help catch similar issues:

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Template Verification Checklist | Feature 068 RCA | ⬜ Not started | Forces direct inspection of template files rather than assuming they're correct based on test setup. Adds explicit checklist items for template includes, parent-child directives, and grep validation of expected output. | High | Low | Low | Add to "Review Checklist" section with grep commands |
| 2 | Mandatory Manual Artifact Generation | Feature 068 RCA | ⬜ Not started | Prevents false confidence from "snapshot tests exist and pass." Requires generating test artifacts manually before trusting snapshots. Snapshots may have been generated before feature was complete or approved with SNAPSHOT_UPDATE_OK despite being incorrect. | High | Low | Low | Add to "Review Approach" section with example commands |
| 3 | Simplest Test Case First | Feature 068 RCA | ⬜ Not started | Focuses attention on core functionality before edge cases. The complexity of configuration reference matching distracted from basic rendering requirement. Requires testing minimal example (1 parent + 1 child) before complex scenarios. | Medium | Low | Low | Add to "Adversarial Testing" section |
| 4 | Line-by-Line Spec-to-Output Comparison | Feature 068 RCA | ⬜ Not started | Ensures spec examples match reality. Feature 068 had rendering-examples.md with clear examples showing "#### Members" tables. Character-by-character comparison would have immediately revealed missing tables. | High | Low | Low | Enhance existing instruction in "Review Approach" |
| 5 | Distinguish Test Data Issues from Implementation Issues | Feature 068 RCA | ⬜ Not started | Avoids misdiagnosis. Reviewer saw `(known after apply)` in test data and assumed that was the blocker, without first verifying the feature worked for simpler known-value cases. | Medium | Low | Low | Add to "Critical Questions for Every Review" |

## Recommendations

- **Option 1 (Comprehensive - Best balance):** **All 5 items (IDs 1-5)** — Addresses all identified gaps from the root cause analysis. Since all items are Low effort and Low risk, implementing all provides maximum protection against similar failures. Total effort: ~30-45 minutes.

- **Option 2 (Critical only):** **IDs 1, 2, 4** — Implements the three CRITICAL improvements (template verification, manual artifact generation, spec-to-output comparison). These directly address the core failure modes. Leaves out the two HIGH/MEDIUM priority items. Total effort: ~20-30 minutes.

- **Option 3 (Highest impact single item):** **ID 2** — Mandatory Manual Artifact Generation alone would have caught the issue immediately by forcing the reviewer to inspect actual output rather than trusting snapshot tests. Most direct solution. Total effort: ~10 minutes.

## Decision

**Maintainer:** Please review the options above and indicate which improvements to implement.

- Type "Option 1", "Option 2", "Option 3", or specify custom task IDs (e.g., "IDs 1, 2, 5")
