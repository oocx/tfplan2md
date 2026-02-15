# Retrospective: AOT-Compiled Trimmed Docker Image

**Date:** 2026-01-14
**Participants:** Maintainer, Requirements Engineer, Architect, Task Planner, Developer, Quality Engineer, Technical Writer, Code Reviewer, UAT Tester, Release Manager, Retrospective

## Summary
The feature successfully replaced the standard .NET runtime Docker image with a NativeAOT-compiled version. The final implementation exceeded the original target (50MB) by achieving a 14.7MB image (89.6% reduction) using a `FROM scratch` base with minimal musl libraries. While the architectural outcome was excellent, the process revealed friction in test command consistency, integration test overhead, and approval-heavy release steps.

## Scoring Rubric
- **Starting score:** 10
- **Deductions:**
    - **Repeated retries / tool failures (-2):** Multiple attempts required for `dotnet test` due to outdated TUnit instructions; multiple Developer handoffs.
    - **Wrong tool/script used (-1):** RM used raw `gh` commands instead of project wrappers or optimized patterns for status checks.
    - **Manual maintainer intervention (-1):** Significant overhead for RM approvals.
    - **Missing required documentation update (-1):** Retrospective agent instructions still refer to manual chat export which is now automated.
- **Final workflow rating: 5/10**

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | 42h 56m | 100% |
| User Wait Time | 13h 24m | 31.2% |
| Agent Work Time | 6h 7m | 14.2% |

- **Start:** 2026-01-12 (approx)
- **End:** 2026-01-14
- **Total Requests:** 52
- **Files Changed:** 33 (estimated from chat-metrics.json)
- **Tests:** 393 TUnit tests passing; native metadata verification added.

## Agent Analysis

### Model Usage by Agent
*Note: Per-agent metrics are unavailable in the consolidated export; totals are provided below.*

| Model | Requests | % |
|-------|----------|---|
| claude-sonnet-4.5 | 20 | 38% |
| gpt-5.1-codex-max | 15 | 28% |
| claude-opus-4.5 | 7 | 13% |
| gemini-3-flash-preview | 5 | 9% |
| gpt-5.2 | 5 | 9% |

### Tool Usage
| Tool | Count | Top Tools |
|------|-------|-----------|
| Overall | 794 | run_in_terminal (351), readFile (176), replaceString (63) |

### Automation Effectiveness
| Total Tools | Auto | Manual | Automation Rate |
|-------------|------|--------|-----------------|
| 794 | 736 | 58 | 92.6% |

## Rejection Analysis

### Rejections by Model
| Model | Total | Cancelled | Failed | Rejection Rate |
|-------|-------|-----------|--------|----------------|
| gpt-5.1-codex-max | 15 | 2 | 1 | 20% |
| claude-opus-4.5 | 7 | 0 | 1 | 14% |
| Others | 30 | 0 | 0 | 0% |

## Automation Opportunities

### Suggested Skills / Scripts
| Opportunity | Proposed Skill/Script | Where It Fits | Evidence | Verification |
|------------|------------------------|---------------|----------|--------------|
| Pre-build Test Image | `scripts/prepare-test-image.sh` | Before `dotnet test` | User feedback on slow integration tests. | Reduced CI timeout; cached image used in tests. |
| Non-interactive Status | `scripts/check-workflow-status.sh` | RM / CI monitoring | RM used many manual `gh run view` calls. | Permanent allow rule possible in VS Code. |

### Terminal Command Patterns
| Pattern | Count | Current | Recommendation |
|---------|-------|---------|----------------|
| `dotnet test` | ~30 | Inconsistent args | Standardize in `scripts/test-all.sh` with TUnit args. |
| `gh run view` | ~10 | Raw | Use a wrapper to avoid pager/approval friction. |

## Agent Performance

| Agent | Rating (1-5) | Strengths | Improvements Needed |
|-------|--------------|-----------|---------------------|
| Requirements Engineer | ⭐⭐⭐⭐⭐ | Crisp spec with clear image size targets. | None. |
| Architect | ⭐⭐⭐⭐⭐ | Identified and executed superior "Option 4" optimization. | None. |
| Developer | ⭐⭐⭐ | Delivered high-quality AOT code. | Struggled with test arguments; multiple handoffs. |
| Release Manager | ⭐⭐ | Completed release. | Too many manual approval steps; slow execution. |
| Retrospective | ⭐⭐⭐ | Captured detailed metrics. | Outdated instructions on chat export tool. |

**Overall Workflow Rating:** 5/10 - Excellent technical outcome (AOT/scratch), but high friction in testing and release orchestration.

## What Went Well
- **Architectural Excellence:** Transitioning to `FROM scratch` with musl libraries delivered a 14.7MB image, far exceeding the 50MB target.
- **Automation Rate:** 92.6% automation rate shows strong tool proficiency despite approval friction.
- **Successful UAT:** Identical visual output confirmed in both GitHub and Azure DevOps.

## What Didn't Go Well
- **Testing Friction:** Outdated instructions for TUnit led to repeated failures when running tests.
- **Integration Test Slowness:** Building the test image *inside* the test suite causes timeouts and discourages frequent runs.
- **Release Overhead:** The Release Manager required excessive manual approvals for standard observability tasks.
- **Process Latency:** Release process is perceived as slow and includes potentially redundant steps.

## Improvement Opportunities

| Issue | Proposed Solution | Action Item |
|-------|-------------------|-------------|
| TUnit Args Outdated | Update `docs/spec.md` and agent prompts with correct TUnit CLI syntax. | Update `docs/spec.md`; Update `developer.agent.md`. |
| Slow Integration Tests | Move `docker build` out of tests into a pre-test script. | Create `scripts/prepare-test-image.sh`. |
| RM Approval Friction | Encapsulate workflow status checks in non-interactive scripts. | Update `release-manager.agent.md` to use new status script. |
| Duplicate Test Runs | Audit release and CI workflows to remove redundant test executions. | Review `.github/workflows/` and `scripts/uat-run.sh`. |
| Outdated Retro Instructions | Remove manual chat export steps from `retrospective.md` mode instructions. | Update `.github/copilot-instructions.md` (Retrospective section). |

## User Feedback (verbatim)
- "The transition was an optimization opportunity."
- "Instructions on how to run unit tests seem to be wrong or outdated (I think the arguments changed with the change from xunit to tunit). When agents started unit tests, they often needed several attempts to find the correct arguments for dotnet test."
- "The integration tests call 'docker build' for the integration test docker image tfplan2md-test:latest. This can be slow and requires long test timeouts. If we build the image before we run dotnet test, the tests can use a cached iamge (so that the build time is not counted inside the test run) and we can reduce test timeouts again."
- "The release manager used too many commands that required manual approval (github action status updates via `PAGER=cat GH_PAGER=cat GH_FORCE_TTY=false gh run view 21000736921` and similar), even though tools exist that would work without aproval or could be safely allowed permanently"
- "The release process takes long in general and seems to be slow. Can we optimize it? Are there duplicate steps or duplicate test runs we can remove? Can we reduce tool usage by the RM that requires approval?"
- "The retrospecitve agent must be updated to not ask the user to execute the chat tool anymore, as this is now done at the end of every agent session."

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (chat export + artifacts + CI)
- [x] Evidence timeline normalized
- [x] Findings clustered by theme
- [x] No unsupported claims
- [x] Action items include where + verification
- [x] Required metrics and sections present
