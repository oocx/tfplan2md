# Retrospective: Parent-Child Resource Grouping (Feature 068)

**Date:** 2026-02-11
**Participants:** Maintainer, Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer, Technical Writer, Code Reviewer, UAT Tester, Release Manager, Retrospective

## Summary
Feature 068 implemented a major architectural change from simple heuristic resource matching to "Configuration Reference Matching" to handle `(known after apply)` IDs in parent-child relationships (e.g., Azure AD group memberships). While the technical implementation of this complex feature was successful, the process was marred by recurring workflow failures: late detection of missing template inclusions that bypassed passing tests, and agents bypassing mandatory repository scripts in favor of raw CLI commands.

## Scoring Rubric
- **Starting score:** 10
- **Deductions:**
    - **Manual Maintainer intervention (Architecture):** −1 — Re-architecting from "address-based heuristic" to "Configuration Reference Matching" required significant Maintainer feedback after the initial design failed.
    - **Boundary violation / Script bypass (RM):** −1 — Release Manager used `gh pr create` directly instead of `scripts/pr-github.sh` after a GraphQL error.
    - **Repeated failures / Missing verification:** −2 — UAT failed initially because `azuread` group members were not rendering despite "passing" snapshot tests. The Code Reviewer failed to catch that templates were not actually included.
    - **Tool Friction:** −1 — `CA1875` analyzer errors in Docker builds and flaky architecture tests required multiple fix rounds.
- **Final workflow rating: 5/10**

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | ~15h | 100% |
| User Wait Time | 3h 17m | 22% |
| Agent Work Time | 12h 48m | 78% |

- **Start:** 2026-02-10 09:12
- **End:** 2026-02-11 15:45
- **Total Requests:** 78
- **Files Changed:** 42
- **Tests:** 912 passing (including new architecture and edge case tests)

## Agent Analysis

### Model Usage by Agent
(Per-agent counts available in `.chat-metrics.json` files; aggregated below)

| Model | Requests | % of Total |
|-------|----------|------------|
| claude-sonnet-4.5 | 37 | 47% |
| gpt-5.2-codex | 16 | 21% |
| gpt-5.2 | 11 | 14% |
| gemini-3-flash-preview | 8 | 10% |
| claude-opus-4.6 | 6 | 8% |

### Agent Performance

| Agent | Rating (1-5) | Strengths | Improvements Needed |
|-------|--------------|-----------|---------------------|
| Requirements Engineer | ⭐⭐⭐⭐⭐ | Crisp spec with clear out-of-scope items. | None. |
| Architect | ⭐⭐⭐ | Pivot to Reference Matching solved the technical blocker. | Initial design was too simplistic and failed on edge cases. |
| Developer | ⭐⭐⭐⭐ | Implemented complex matching logic; fast fix for Docker CA1875. | Trusted "passing" snapshots without verifying output content. |
| Code Reviewer | ⭐⭐ | Responsive to requested changes. | **Critical Failure**: Approved implementation that was missing template inclusions (Azure AD). |
| UAT Tester | ⭐⭐⭐⭐ | Correctly identified rendering failures during first pass. | None. |
| Release Manager | ⭐⭐ | Handled release notes efficiently. | **Boundary Violation**: Bypassed repo scripts; used `gh` after a minor error. |

## Rejection Analysis

- **Rejections by Agent**: 10 (Cancelled: 4, Failed: 4, Tool: 2).
- **Most Common Reason**: Terminal script failures (GraphQL errors, Docker analyzer errors).

## Automation Opportunities

| Opportunity | Proposed Skill/Script | Where It Fits | Evidence | Verification |
|------------|------------------------|---------------|----------|--------------|
| Snapshot Quality Gating | `scripts/verify-snapshots.sh` | Pre-commit / CI | Snapshots for Azure AD were "empty-passing" but technically successful. | Script fails if snapshot matches a "negative pattern" (e.g. empty child tables). |
| Script Bypass Prevention | Pre-commit check | Pre-commit | RM used raw `gh` commands. | `grep` for forbidden commands in chat history or shell history. |

## What Went Well
- **Configuration Reference Matching**: The new strategy is much more robust than the previous heuristic and handles `(known after apply)` with high precision.
- **TUnit Architecture Tests**: Migrating to explicit assembly scanning fixed persistent CI flakiness.
- **Cross-Platform Verification**: UAT successfully validated rendering parity between GitHub and Azure DevOps.

## What Didn't Go Well
- **"Empty-Passing" Tests**: Snapshot tests became a false signal of success. Because the "fallback" logic returned empty lists, the snapshots were "correct" (matching the code's output) but the output was missing the intended feature content.
- **Script Fragility**: `scripts/pr-github.sh` failed due to a GraphQL error, leading the agent to fallback to manual CLI usage instead of fixing/reporting the script issue.

## Improvement Opportunities
| Issue | Proposed Solution | Action Item |
|-------|-------------------|-------------|
| Recurring: Missing template sections missed by Code Review | Mandatory manual rendered artifact check. | Update `code-reviewer.agent.md` to require manual inspection of artifacts in the `.tmp/` folder. |
| Recurring: Agents bypassing repo scripts | Strict "No CLI" rule in agents instructions. | Update `docs/agents.md` to reinforce the absolute requirement of using scripts over raw `gh`/`az` commands. |
| Sub-optimal test coverage for rendering | Add "negative" test cases for snapshots. | Developer: Implement tests that assert a minimum number of tables or rows in the output. |

## User Feedback (verbatim)
- "1 was expected given the complexity of this feature. 2 and 3 are reocurring annoyances that we failed to fix despite several attempts." (referring to Architecture matching, missing template verification, and script bypass).

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (chat export + work-protocol)
- [x] Time breakdown and model metrics included
- [x] Agent scoring based on evidence
- [x] Action items include verification
- [x] No unsupported claims
