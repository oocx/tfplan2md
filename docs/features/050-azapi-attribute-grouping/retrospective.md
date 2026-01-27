# Retrospective: Improved AzAPI Attribute Grouping and Array Rendering

**Date:** 2026-01-27
**Participants:** Maintainer, Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer, Technical Writer, UAT Tester, Release Manager

## Summary
This feature cycle successfully implemented intelligent grouping and array rendering for AzAPI resources, significantly improving the readability of complex JSON body attributes in tfplan2md reports. While the final output passed UAT on both GitHub and Azure DevOps, the development process was marked by multiple workflow frictions: a deviation from the specification (unauthorized collapsible sections), a visual regression bug in metadata formatting, and a failure in the UAT automation to detect maintainer feedback comments. 

## Scoring Rubric
- **Starting score: 10**
- **Deductions:**
    - **Boundary violation (Developer):** −2 (Implemented out-of-scope collapsible section for "other attributes").
    - **Tool/Process failure (UAT):** −2 (UAT script failed to detect maintainer comments in both PRs, leading to delayed feedback).
    - **Regression/Bug (Developer):** −1 (Metadata formatting issues with redundant icons and backticks).
- **Final workflow rating: 5/10**

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | 55h 39m | 100% |
| User Wait Time | 3h 48m | 6.8% |
| Agent Work Time | 3h 4m | 5.5% |

- **Start:** 2026-01-25 10:22 UTC
- **End:** 2026-01-27 18:01 UTC
- **Total Requests:** 41
- **Files Changed:** 28
- **Tests:** 14 added, 156 total passing

## Agent Analysis (Overall)

### Model Usage
| Model | Requests | % |
|-------|----------|---|
| gpt-5.2 / gpt-5.2-codex | 22 | 53.6% |
| gemini-3-flash-preview | 11 | 26.8% |
| claude-sonnet-4.5 | 10 | 24.4% |
| claude-opus-4.5 | 1 | 2.4% |

### Automation Effectiveness
| Total Tools | Auto | Manual | Cancelled/Unknown | Automation Rate |
|-------------|------|--------|-------------------|-----------------|
| 690 | 660 | 24 | 6 | 95.6% |

## Rejection Analysis

### Rejections by Model
| Model | Total | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|-------|-----------|--------|-----------------|----------------|
| gpt-5.2 | 22 | 1 | 0 | 0 | 4.5% |
| gemini-3-flash-preview | 11 | 1 | 0 | 0 | 9.0% |
| claude-sonnet-4.5 | 10 | 0 | 0 | 0 | 0% |

### User Vote-Down Reasons
| Reason | Count |
|--------|-------|
| deviationFromSpec | 1 |
| incorrectOutput | 1 |

## Automation Opportunities

### Suggested Skills / Scripts
| Opportunity | Proposed Skill/Script | Where It Fits | Evidence | Verification |
|------------|------------------------|---------------|----------|--------------|
| Metadata icon validation | Linter rule or unit test | Pre-flight | Redundant icons discovered in UAT | No double-rendering of `🆔` or `🌍` |
| UAT Comment Polling | `.github/skills/watch-uat-github-pr` | Post-UAT | Script failed to see maintainer comments | Script correctly identifies non-bot comments |

### Terminal Command Patterns
| Pattern | Count | Current | Recommendation |
|---------|-------|---------|----------------|
| `dotnet test` | 42 | Auto | ✅ Already automated |
| `git commit` | 12 | Manual | Consider: wrapper script for conventional commits |
| `jq -s '.'` | 8 | Manual | Already used in `analyze-chat.py` |

## Model Effectiveness Assessment

### Assigned vs Actual Model Usage
| Agent | Assigned Model | Actual Usage | Assessment |
|-------|----------------|--------------|------------|
| Developer | gpt-5.2 | 100% match | ✅ Correct |
| UAT | gemini-3-flash | 100% match | ✅ Correct |
| Architect | gpt-5.2 / opus | 83% match | ⚠️ Switched to Opus for design |

### Model Performance Statistics
| Model | Requests | Avg Response (s) | Success Rate |
|-------|----------|------------------|--------------|
| gpt-5.2 | 22 | 310s | 100% |
| gemini-3-flash-preview | 11 | 380s | 100% |
| claude-sonnet-4.5 | 10 | 224s | 100% |

## Agent Performance

| Agent | Rating (1-5) | Strengths | Improvements Needed |
|-------|--------------|-----------|---------------------|
| Requirements Engineer | ⭐⭐⭐⭐⭐ | Crisp specification, clear out-of-scope. | None. |
| Architect | ⭐⭐⭐⭐ | Solid hybrid strategy. | None. |
| Developer | ⭐⭐ | Good code quality. | Boundary violation (added out-of-scope collapsible), metadata formatting bug. |
| UAT Tester | ⭐⭐ | Found bugs. | Automation failed to detect comments, required maintainer poking. |
| technical writer | ⭐⭐⭐⭐⭐ | Excellent doc updates. | None. |

**Overall Workflow Rating: 5/10** - The process was inefficient due to reworks caused by boundary violations and automation failures.

## What Went Well
- **Multi-platform UAT:** Proactively Testing on both GitHub and Azure DevOps simultaneously ensured visual consistency.
- **Hybrid Rendering Solution:** Combining matrix tables for small arrays and per-item tables for complex ones proved to be the correct UX choice.
- **Documentation:** The architecture and specification were kept in sync with implementation changes.

## What Didn't Go Well
- **Boundary Violation:** The Developer agent implemented a collapsible "other attributes" section that was not in the spec, leading to rework.
- **Automation Oversight:** The UAT script only polled for "approval" but not for "comments with issues," causing it to report "Passed" when the Maintainer had actually requested changes.
- **Visual Regression:** A fix in the template introduced redundant decorative icons (e.g., `🆔 🆔`) that weren't caught until manual review.

## Improvement Opportunities
| Issue | Proposed Solution | Action Item | Verification |
|-------|-------------------|-------------|--------------|
| UAT Comment Blindness | Update `uat-watch-*` scripts to check for any new comments from non-bot users. | Modify `scripts/uat-github.sh` and `scripts/uat-azdo.sh`. | Script fails if a new comment from a human exists without approval. |
| Metadata Consistency | Encapsulate icon rendering into a single Scriban filter or helper to prevent duplication in templates. | Refactor `AzApi.Metadata.cs` helpers. | Unit test verifying single icon output. |
| Boundary Awareness | Strengthen "Out of Scope" enforcement in `developer.agent.md`. | Update Developer agent prompt. | Agent rejects adding features listed in "Out of Scope" sections. |

## User Feedback (verbatim)
- "The UAT did not detect that I commented with issues in both PRs (the UAT already improved its scripts to fix the issue for the future)."
- "There was a bug in the initial implementation that was discovered in the UAT (redundant icons/backticks in metadata formatting)."
- "There was a deviation from the specification in the initial implementation (manual collapsible 'other attributes' section)."

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (chat export + artifacts + CI)
- [x] Evidence timeline normalized across lifecycle phases
- [x] Findings clustered by theme and supported by evidence
- [x] No unsupported claims (assumptions labeled or omitted)
- [x] Action items include where + verification
- [x] Required metrics and required sections are present
- [x] All retro-related user feedback captured verbatim
