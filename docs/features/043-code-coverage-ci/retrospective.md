# Retrospective: Code Coverage Reporting and Enforcement in CI

**Date:** 2026-01-22
**Participants:** Maintainer, Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer, Release Manager, Technical Writer

## Summary
The "Code Coverage Reporting and Enforcement in CI" feature was implemented over a two-week period. The project successfully added automated coverage collection (using TUnit/Microsoft Testing Platform), threshold enforcement via a new `CoverageEnforcer` tool, PR visibility with markdown summaries, and historical trend tracking with a README badge.

The process involved multiple agents and models, with a significant amount of work done by the Developer using heavy-weight models and the Release Manager using light-weight models for extensive polling and verification.

## Scoring Rubric
- **Starting score:** 10
- **Deductions:**
    - **Wrong tool/script used (-1):** Release Manager used raw `gh` commands with `cat` instead of repository wrapper scripts, leading to significant approval friction.
    - **Repeated retries / tool failures (-1):** Massive polling overhead (243+ terminal calls) during UAT/verification phase.
    - **Manual maintainer intervention (-1):** Maintainer had to fix `release.yml` Dockerfile path and manually prompt for missing commits.
    - **Missing required artifact/section/metric (-1):** Chat logs were found misplaced in `src/` subdirectories instead of the feature folder.
- **Final workflow rating: 6/10**

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | 13 days 7h | 100% |
| User Wait Time | 3h 04m | ~1% |
| Agent Work Time | 3h 37m | ~1.1% |

- **Start:** 2026-01-08 17:15
- **End:** 2026-01-22 00:21
- **Total Requests:** 50
- **Files Changed:** 47
- **Tests:** 505 total passing (including new integration and unit tests for coverage)

## Agent Analysis

### Model Usage by Agent
| Agent | Model | Requests |
|-------|-------|----------|
| Requirements Engineer | claude-sonnet-4.5 | 1 |
| Architect | gpt-5.2 | 8 |
| Task Planner | claude-sonnet-4.5 | 1 |
| Developer | claude-sonnet-4.5 (13), gpt-5.2-codex (15) | 28 |
| Quality Engineer | gemini-3-flash-preview | 3 |
| Release Manager | gemini-3-flash-preview (8) | 8 |
| Technical Writer | gemini-3-flash-preview | 1 |

### Request Counts by Agent
| Agent | Total Requests | Primary Model |
|-------|----------------|---------------|
| Developer | 28 | gpt-5.2-codex |
| Architect | 8 | gpt-5.2 |
| Release Manager | 8 | gemini-3-flash-preview |
| Others | 6 | mixed |

### Automation Effectiveness by Agent
| Agent | Total Tools | Auto | Manual | Automation Rate |
|-------|-------------|------|--------|-----------------|
| Release Manager | 277 | 264 | 13 | 95.3% |
| Architect | 31 | 31 | 0 | 100% |
| Developer | ~200 | ~190 | ~10 | 95% |

## Rejection Analysis

### Rejections by Model
| Model | Total | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|-------|-----------|--------|-----------------|----------------|
| gpt-5.2-codex | 15 | 3 | 0 | 3 | 20% |
| gemini-3-flash-preview | 11 | 1 | 0 | 1 | 9% |
| Others | 24 | 0 | 0 | 0 | 0% |

### Common Rejection Reasons
| Reason | Count | Message |
|--------|-------|---------|
| manualApproval | 13 | Manual intervention needed for raw commands |
| toolFailure | 4 | Misconfigured paths in early attempts |

## Automation Opportunities

### Terminal Command Patterns
| Pattern | Count | Current | Recommendation |
|---------|-------|---------|----------------|
| `gh run view` | 51+ | Manual | Already automated in `scripts/uat-watch-github.sh` but ignored. |
| `date -u` | 9 | Manual | Use internal agent timestamp or dedicated script if needed. |
| `git status` | 19 | Manual | Use `scripts/git-status.sh` as per guidelines. |

### Suggested Skills / Scripts
- **Skill: Commit-Before-Handoff**: A new skill or instruction update to ensure all agents commit artifacts and code *before* suggesting a handoff to the next agent.
- **Skill: PR-Polling-Automation**: Improve `uat-watch-*` scripts to handle job-level failures more gracefully so agents don't resort to raw `gh` commands.

## Model Effectiveness Assessment

### Assigned vs Actual Model Usage
- **Developer**: Used `gpt-5.2-codex` and `claude-sonnet-4.5`. The codex model had a higher rejection rate (20%) compared to Sonnet.
- **Release Manager**: Used `gemini-3-flash-preview`. Highly effective for repetitive polling tasks but prone to using raw commands.

### Model Performance Statistics
| Model | Avg Response (s) | Success Rate |
|-------|------------------|--------------|
| gpt-5.2 | 53s | 100% |
| claude-sonnet-4.5 | ~45s | 100% |
| gemini-3-flash-preview | ~32s | 91% |

## Agent Performance

| Agent | Rating | Strengths | Improvements Needed |
|-------|--------|-----------|---------------------|
| Requirements Engineer | ⭐⭐⭐⭐⭐ | Crisp specification, handled edge cases (override). | None. |
| Architect | ⭐⭐⭐⭐⭐ | Clear decision on separate CLI tool for coverage. | None. |
| Developer | ⭐⭐⭐⭐ | High output, implementation matches spec closely. | Misplaced chat logs in `src/`. |
| Quality Engineer | ⭐⭐⭐ | Good test plan. | **Did not commit any files** before handoff. |
| Release Manager | ⭐⭐ | Persistent polling until success. | **Extreme friction** from raw `gh` commands; missed Dockerfile path bug. |

**Overall Workflow Rating: 6/10** - Shipped a high-quality feature with excellent docs, but material workflow friction and manual overhead significantly reduced efficiency.

## What Went Well
- **Multi-model collaboration:** Different models were used effectively for their strengths (Sonar/GPT for coding, Gemini for verification).
- **Document Quality:** Full suite of architecture, spec, and test plans were produced and aligned.
- **Incremental commits:** The Developer (mostly) followed clean commit patterns.

## What Didn't Go Well
- **Polling Loop:** The Release Manager entered very long polling loops using raw commands, causing approval fatigue.
- **Handoff Friction:** The QE and RM often asked for handoff without committing their final work, requiring manual maintainer prompts.
- **Artifact Hygiene:** Chat logs were left in production source directories.
- **Release Bug:** A trivial path bug in `release.yml` was missed by the RM and fixed manually.

## Improvement Opportunities
| Issue | Proposed Solution | Action Item |
|-------|-------------------|-------------|
| Missing commits before handoff | Enforce "commit then handoff" rule in agent instructions. | Update `docs/agents.md` and agent prompts. |
| Raw `gh` command usage | Strictly prohibit raw `gh` calls when scripts exist. | Update RM agent prompt and `.github/gh-cli-instructions.md`. |
| Misplaced chat logs | Add validation check for chat log locations. | Update `scripts/validate-agents.py` or new pre-commit hook. |
| CI Polling friction | Improve `uat-watch` scripts to be more "agent-friendly" (simpler output). | Refactor `scripts/uat-watch-github.sh`. |

## User Feedback (verbatim)
- "Whenever an agent thinks it has completed all tasks and asks if we should handoff to the next agent, it must commit files *before* it asks for the handoff - otherwise, I would always need one extra message to confirm and request the commit."
- "The QE did not commit at all. All agents must commit their changes when they are done. They may also commit intermediate results if they have something that is worth commiting."
- "The RM used 'cat gh run...' instead of using scripts or tools. This causes friction, as every call needs manuasl approval."
- "note for retro: RM missed the Dockerfile path bug in release.yml which I had to fix manually."

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (12 chat logs found and analyzed)
- [x] Evidence timeline normalized across lifecycle phases
- [x] Findings clustered by theme (Friction, Hygiene, Commit Patterns)
- [x] No unsupported claims
- [x] Action items include where + verification
- [x] Required metrics and required sections are present
