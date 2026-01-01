# Retrospective: Markdown to HTML Rendering Tool (Feature 027)

**Date:** 2026-01-01
**Participants:** Maintainer, Requirements Engineer, Architect, Quality Engineer, Task Planner, Developer, Code Reviewer, Release Manager, Technical Writer, Retrospective

## Summary
Feature 027 implemented a standalone .NET tool for converting tfplan2md reports to HTML. This was the first feature to trial a **per-agent chat workflow**, where each agent/phase had its own dedicated chat session. This allowed for precise per-agent metrics and better context management, though it introduced some manual overhead for handoffs. The development was highly automated (97%+ auto-approval rate) and resulted in a high-quality tool with comprehensive testing and documentation.

## Scoring Rubric
- Starting score: 10
- Deductions:
    - **Process Friction (Handoffs):** -1 (Manual prompt creation required for per-agent chats)
    - **Communication Gap (Developer):** -1 (Unclear progress/next steps during long developer session)
- Final workflow rating: 8/10

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | 5h 28m* | 100% |
| User Wait Time | 10h 11m | 186%** |
| Agent Work Time | 2h 46m | 50% |

*\* Sum of individual agent session durations. Total wall-clock time was likely longer due to gaps between sessions.*
*\*\* User wait time exceeds session duration because it is cumulative across multiple agents and includes overnight/break periods.*

- **Start:** 2026-01-01 09:00 (approx)
- **End:** 2026-01-01 17:00 (approx)
- **Total Requests:** 79
- **Files Changed:** ~15 (new project + tests + docs)
- **Tests:** 29 new unit tests, 377 total passing

## Agent Analysis

### Model Usage by Agent
| Agent | Model | Requests | % of Agent |
|-------|-------|----------|------------|
| requirements-engineer | claude-sonnet-4.5 | 25 | 96% |
| architect | gpt-5.2 | 4 | 100% |
| quality-engineer | gemini-3-flash-preview | 3 | 100% |
| task-planner | gemini-3-flash-preview | 2 | 100% |
| developer | gpt-5.1-codex-max | 39 | 100% |
| code-reviewer | claude-sonnet-4.5 | 1 | 100% |
| release-manager | gemini-3-flash-preview | 2 | 100% |
| technical-writer | claude-sonnet-4.5 | 2 | 100% |

### Request Counts by Agent
| Agent | Total Requests | Primary Model |
|-------|----------------|---------------|
| requirements-engineer | 26 | claude-sonnet-4.5 |
| developer | 39 | gpt-5.1-codex-max |
| architect | 4 | gpt-5.2 |
| quality-engineer | 3 | gemini-3-flash-preview |
| task-planner | 2 | gemini-3-flash-preview |
| release-manager | 2 | gemini-3-flash-preview |
| technical-writer | 2 | claude-sonnet-4.5 |
| code-reviewer | 1 | claude-sonnet-4.5 |

### Automation Effectiveness by Agent
| Agent | Total Tools | Auto | Manual | Cancelled | Automation Rate |
|-------|-------------|------|--------|-----------|-----------------|
| requirements-engineer | 48 | 48 | 0 | 0 | 100% |
| developer | 492 | 481 | 11 | 0 | 97.7% |
| code-reviewer | 50 | 49 | 1 | 0 | 98% |
| release-manager | 52 | 48 | 4 | 0 | 92.3% |
| technical-writer | 43 | 43 | 0 | 0 | 100% |

### Tool Usage by Agent
| Agent | Top Tools |
|-------|-----------|
| developer | run_in_terminal (194), readFile (108), applyPatch (89), manage_todo_list (53) |
| requirements-engineer | replaceString (13), readFile (10), run_in_terminal (7), fetchWebPage (6) |
| code-reviewer | readFile (15), run_in_terminal (11), findTextInFiles (8), manage_todo_list (8) |
| release-manager | run_in_terminal (41), manage_todo_list (5), readFile (4) |

## Rejection Analysis

### Rejections by Agent
| Agent | Total | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|-------|-----------|--------|-----------------|----------------|
| developer | 39 | 0 | 1 | 0 | 2% |
| requirements-engineer | 26 | 0 | 1 | 0 | 3% |

### Rejections by Model
| Model | Total | Cancelled | Failed | Tool Rejections | Rejection Rate |
|-------|-------|-----------|--------|-----------------|----------------|
| gpt-5.1-codex-max | 39 | 0 | 1 | 0 | 2% |
| gpt-5.2 | 5 | 0 | 1 | 0 | 20% |

### Common Rejection Reasons
| Error Code | Count | Sample Message |
|------------|-------|----------------|
| failed | 2 | Sorry, your request failed. Please try again. |
| canceled | 1 | Canceled |

## Automation Opportunities

### Suggested Skills / Scripts
| Opportunity | Proposed Skill/Script | Where It Fits | Evidence | Verification |
|------------|------------------------|---------------|----------|--------------|
| Automated Handoffs | `.github/skills/handoff/` | Between agents | User feedback on manual prompt creation | Script generates next agent prompt |
| Developer Progress Reporting | Update Developer Agent | During implementation | User feedback on unclear progress | Developer logs task completion to chat |

### Terminal Command Patterns
| Pattern | Count | Current | Recommendation |
|---------|-------|---------|----------------|
| `dotnet build` | ~20 | Auto | ✅ Already automated |
| `dotnet test` | ~15 | Auto | ✅ Already automated |
| `git add/commit` | ~10 | Manual | Consider: wrapper script for atomic commits |

## Model Effectiveness Assessment

### Assigned vs Actual Model Usage
| Agent | Assigned Model | Actual Usage | Assessment |
|-------|----------------|--------------|------------|
| Developer | gpt-5.1-codex-max | 100% match | ✅ Correct |
| Requirements Engineer | claude-sonnet-4.5 | 96% match | ✅ Correct |
| Architect | gpt-5.2 | 100% match | ✅ Correct |

### Model Performance Statistics
| Model | Requests | Avg Response (s) | Success Rate |
|-------|----------|------------------|--------------|
| gpt-5.1-codex-max | 39 | 127s | 97% |
| claude-sonnet-4.5 | 28 | 117s | 100% |
| gemini-3-flash-preview | 9 | 311s* | 100% |

*\* High average due to long-running release manager tasks.*

## Agent Performance

| Agent | Rating (1-5) | Strengths | Improvements Needed |
|-------|--------------|-----------|---------------------|
| Requirements Engineer | ⭐⭐⭐⭐⭐ | Comprehensive research on GFM vs ADO. | None. |
| Architect | ⭐⭐⭐⭐⭐ | Clear ADR for Markdig selection. | None. |
| Developer | ⭐⭐⭐ | High automation, clean code. | Better progress communication. |
| Code Reviewer | ⭐⭐⭐⭐⭐ | Thorough validation of all ACs. | None. |
| Release Manager | ⭐⭐⭐⭐ | Correct PR workflow. | Slightly higher manual intervention. |

**Overall Workflow Rating:** 8/10 - The per-agent workflow is a significant improvement for observability and context management, but needs better handoff automation and developer-to-user communication.

## What Went Well
- **Per-Agent Workflow:** Successfully trialed, providing excellent metrics and context isolation.
- **High Automation:** 97%+ auto-approval rate across all agents.
- **Comprehensive Testing:** 29 new tests covering edge cases and platform-specific rendering.
- **Research Quality:** Requirements engineer provided deep insights into platform rendering differences.

## What Didn't Go Well
- **Handoff Friction:** Manual prompt creation for each agent was a "minor nuisance".
- **Developer Visibility:** User felt unclear about progress and next steps during the long developer session.
- **Model Failures:** Occasional "request failed" errors required retries.

## Improvement Opportunities
| Issue | Proposed Solution | Action Item |
|-------|-------------------|-------------|
| Manual Handoffs | Create a handoff skill/script to generate the next agent's prompt. | Create `.github/skills/handoff/SKILL.md` |
| Developer Visibility | Update Developer agent instructions to provide regular status updates. | Update `developer.agent.md` |
| Per-Agent Workflow | Formalize the per-agent chat workflow in `docs/agents.md`. | Update `docs/agents.md` |

## User Feedback (verbatim)
- "When working with the developer, it was often unclear to me how far the development was completed, which tasks were still open and what the next step would be."
- "I initially avoided a per agent workflow, because it complicates handoffs... This is however a minor nuisance, and I think the ability to analyze chats per agent makes up for the increased friction. It may also help with optimizing the context window for each agent."

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (chat export + artifacts + CI/status checks)
- [x] Evidence timeline normalized across lifecycle phases
- [x] Findings clustered by theme and supported by evidence
- [x] No unsupported claims (assumptions labeled or omitted)
- [x] Action items include where + verification
- [x] Required metrics and required sections are present
