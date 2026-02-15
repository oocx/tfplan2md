# Retrospective: Terraform Show Output Approximation Tool

**Date:** 2026-01-07
**Participants:** Maintainer, Developer (Multi-model), Requirements Engineer, Architect, Task Planner, Technical Writer, Code Reviewer, Release Manager

## Summary
The development of the `TerraformShowRenderer` tool was a high-effort task focused on achieving "authentically indistinguishable" output from Terraform plan JSON files. While the planning and architectural phases were smooth, the implementation phase became a multi-day ordeal across various top-tier models. The core challenge was the AI's difficulty in deducing implicit rendering rules (whitespace, attribute ordering, ANSI color patterns) from provided examples. Success was eventually achieved through intensive user-guided test strategies and iterative model switching.

## Scoring Rubric
- Starting score: 10
- Deductions:
    - **Repeated retries / Flaky execution (-2):** GPT-5.1-codex-max frequently stopped prematurely requiring manual "proceed" prompts. The agent also struggled significantly with formatting fidelity, requiring over 20 fix commits.
    - **Boundary/Policy violation (-1):** Instruction on workspace-local temporary file handling was ignored multiple times; agents attempted to write to `/tmp`.
    - **Manual maintainer intervention (-1):** The user had to provide specific test and implementation strategies to unblock the agent after several failed attempts.
- **Final workflow rating:** 6/10

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | ~70h (3 days) | 100% |
| User Wait Time | ~6.5h | ~9% |
| Agent Work Time | ~5h (Non-Dev) | ~7% |

- **Start:** 2026-01-04 22:50
- **End:** 2026-01-07 20:09
- **Total Requests:** 29 (Non-Dev) + High (Dev)
- **Files Changed:** 57
- **Tests:** 33 added, 388 total passing

## Agent Analysis

### Agent Attribution Note
Per-agent metrics are available for all roles except the **Developer**, as those logs were not saved. The data below reflects only the non-developer session segments.

### Model Usage by Agent
| Agent | Model | Requests | % of Agent |
|-------|-------|----------|------------|
| architect | gpt-5.2 | 9 | 100% |
| code-reviewer | claude-sonnet-4.5 | 2 | 100% |
| quality-engineer | gemini-3-flash | 4 | 100% |
| release-manager | gemini-3-flash | 2 | 100% |
| requirements-eng | claude-sonnet-4.5 | 9 | 100% |
| task-planner | gemini-3-flash | 2 | 100% |
| technical-writer | claude-sonnet-4.5 | 1 | 100% |


### Request Counts by Agent
| Agent | Total Requests | Primary Model | Success Rate |
|-------|----------------|---------------|--------------|
| Architect | 9 | GPT-5.2 | 100% |
| Code Reviewer | 2 | Claude Sonnet 4.5 | 100% |
| Quality Engineer | 4 | Gemini 3 Flash | 100% |
| Release Manager | 2 | Gemini 3 Flash | 100% |
| Requirements Eng. | 9 | Claude Sonnet 4.5 | 100% |
| Task Planner | 2 | Gemini 3 Flash | 100% |
| Technical Writer | 1 | Claude Sonnet 4.5 | 100% |
| Developer | High (N/A) | Multi-model | Mixed |

### Automation Effectiveness by Agent
| Agent | Total Tools | Auto | Manual | Automation Rate |
|-------|-------------|------|--------|-----------------|
| Architect | 59 | 59 | 0 | 100% |
| Code Reviewer | 30 | 30 | 0 | 100% |
| Quality Engineer | 27 | 26 | 1 | 96.2% |
| Release Manager | 114 | 101 | 13 | 88.5% |
| Requirements Eng. | 14 | 13 | 1 | 92.8% |
| Task Planner | 13 | 13 | 0 | 100% |
| Technical Writer | 32 | 32 | 0 | 100% |

### Tool Usage by Agent
| Agent | Top Tools |
|-------|-----------|
| Architect | readFile (24), applyPatch (10), listDirectory (7) |
| Code Reviewer | readFile (14), run_in_terminal (7) |
| Quality Engineer | replaceString (9), manage_todo_list (6) |
| Release Manager | run_in_terminal (90), readFile (15) |
| Requirements Eng. | run_in_terminal (8), readFile (4) |
| Task Planner | listDirectory (4), readFile (4) |
| Technical Writer | readFile (17), manage_todo_list (5) |

## Rejection Analysis
- **Model Interruption:** GPT-5.1-codex-max showed a unique pattern of stopping without blockers, requiring "proceed" (1x Premium Request cost per occurrence).
- **Environment Violation:** Multiple rejections/warnings for attempting to use `/tmp` instead of workspace-local `.tmp/`.

## Automation Opportunities

### Suggested Skills / Scripts
| Opportunity | Proposed Skill/Script | Where It Fits | Evidence | Verification |
|------------|------------------------|---------------|----------|--------------|
| **Formatting Fidelity** | `.github/skills/formatting-fidelity/` | Planning / Dev | Agent struggled to deduce implicit rules from examples. | Agent can explain rendering rules before coding. |
| **Playwright Check** | `scripts/check-playwright.sh` | Pre-test | Repeated redundant installation attempts. | Script returns 0 if ready, evitando `npx playwright install`. |
| **Temp Path Enforcement** | Update `copilot-instructions.md` | Policy | Recurring violations of workspace-local temp path rule. | Agent uses `$PWD/.tmp` consistently. |

### Terminal Command Patterns
| Pattern | Count | Current | Recommendation |
|---------|-------|---------|----------------|
| `npx playwright install` | Repeated | Manual | Automate check in `scripts/uat-run.sh` |
| `dotnet test` | High | Auto | ✅ Already automated |

## Model Effectiveness Assessment
- **GPT-5.1-codex-max:** Poor efficiency due to premature "stop and ask" behavior. Higher TCO despite 1x rate because of friction.
- **Claude Opus 4.5:** Higher completion rate per request; better value for complex, multi-step implementation tasks.
- **Claude Sonnet 4.5:** Strong performance but hit a ceiling on high-fidelity formatting detail.

## Agent Performance

| Agent | Rating | Strengths | Improvements Needed |
|-------|--------|-----------|---------------------|
| Requirements | ⭐⭐⭐⭐⭐ | Crisp spec; minimal back-and-forth; 92.8% automation. | None. |
| Architect | ⭐⭐⭐⭐⭐ | Decoupled tool design; 100% automation rate; solid ADR. | None. |
| Task Planner | ⭐⭐⭐⭐⭐ | Clear ordering of tasks; 100% automation rate. | None. |
| Developer | ⭐⭐ | Persistent but required high maintainer overhead to reach fidelity. | Formatting deduction; temp file discipline. |
| Code Reviewer | ⭐⭐⭐⭐⭐ | Actionable feedback; 100% success rate with Claude 4.5. | None. |
| Quality Eng. | ⭐⭐⭐⭐⭐ | 100% success; efficient todo management. | None. |
| Release Manager | ⭐⭐⭐⭐ | Managed 114 tool calls across long sessions; high automation. | Long response times with small models on big tasks. |
| Technical Writer | ⭐⭐⭐⭐⭐ | Comprehensive docs; 1 request completion (32 tools). | None. |
| Retrospective | ⭐⭐⭐⭐ | Identified model usage patterns and cost implications. | Self-critique of missing log collection. |

## What Went Well
- **Final Result:** The tool achieves 100% fidelity against Terraform's output, enabling authentic website examples.
- **Test Coverage:** High regression coverage (33 tests) ensures no regressions in the complex rendering logic.
- **Architecture:** Standalone tool approach keeps the main production library clean.

## What Didn't Go Well
- **Model Friction:** GPT-5.1 interruption pattern caused frustration and increased costs.
- **Reasoning Ceiling:** No single model could "see" the implicit whitespace/ordering rules without explicit user guidance.
- **Policy Adherence:** Repeated attempts to use `/tmp` despite project-specific instructions.

## Improvement Opportunities
| Issue | Proposed Solution | Action Item |
|-------|-------------------|-------------|
| GPT-5.1 premature stops | Investigate prompt engineering to suppress "ask before proceed" behavior. | Update `docs/agents.md` model guidance. |
| Redundant Playwright setup | Add a guard script to check for Playwright before installation. | Create `scripts/ensure-playwright.sh`. |
| Path policy violations | Add path enforcement to the system prompt or lint for `/tmp` usage in terminal commands. | Update `.github/copilot-instructions.md`. |
| High-Fidelity requirements | When fidelity is critical, explicitly list all implicit rules (spacing, ordering) in the task description. | Update `docs/spec.md` for tool development. |

## User Feedback (verbatim)
- "GPT-5.1-codex-max seems to have a tendency to stop and ask for user input, even though there is no need for a decision... often had to provide a simple 'proceed'."
- "The instruction on temporary file handling was ignored by the developer agent on multiple occasions. It tried to write to /tmp... instead of using the temporary folder in the workspace."
- "When tests require playwright, they always try to install playwright first, even though it is already installed... These steps slow down the process."
- "The agents were initially unable to deduct the exact rendering rules from the provided examples... results were close, but not perfect."

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (git logs + artifacts + session notes)
- [x] Evidence timeline normalized (Multi-day lifecycle)
- [x] Findings clustered by theme (Model friction, formatting fidelity, policy adherence)
- [x] No unsupported claims (Developer metrics marked as Unavailable)
- [x] Action items include where + verification
- [x] User feedback captured verbatim and mapped to improvements
