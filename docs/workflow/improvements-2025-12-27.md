# Workflow Improvements - December 27, 2025

**Source:** Retrospective analysis from:
- `docs/issues/ci-deployment-duplication-and-versionize-major/retrospective.md`
- `docs/features/custom-report-title/retrospective.md`

**Date:** 2025-12-27

## Implementation Status

| # | Description | Status | Priority | Complexity | Value |
|---|-------------|--------|----------|------------|-------|
| **1** | Fix Quality Engineer test plan folder instructions | ✅ Complete | High | Low | High |
| **2** | Enforce strict PR creation boundaries | ✅ Complete | High | Low | High |
| **3** | Fix Release Manager polling behavior | ✅ Complete | High | Medium | High |
| **4** | Enforce project script usage | ✅ Complete | High | Low | High |
| **5** | Fix Task Planner boundary violation | ✅ Complete | Critical | Medium | Critical |
| **6** | Improve Retrospective Agent critical analysis | ✅ Complete | High | Medium | High |
| **7** | Add interactive retrospective phase | ✅ Complete | High | Medium | High |
| **8** | Ensure Retrospective terminal access | ✅ Complete | Medium | Low | Medium |
| **9** | Replace Gemini 3 Pro due to instability | ✅ Complete | Medium | High | Medium |
| **10** | Create analyze-run.sh helper script | 🔲 Not Started | Medium | Medium | Medium |
| **11** | Create cleanup-tags.sh helper script | 🔲 Not Started | Low | Low | Low |
| **12** | Enforce UAT artifact validation | ✅ Complete | High | Low | High |
| **13** | Enforce UAT report updates | ✅ Complete | High | Low | High |
| **14** | Investigate GPT-5.2 performance | ✅ Complete | Medium | High | Medium |
| **15** | Add pre-commit validation script | 🔲 Not Started | Medium | Medium | Medium |
| **16** | Add workflow completion checklist to Release Manager | ✅ Complete | High | Low | High |
| **17** | Fix release.yml duplicate trigger issue | ✅ Complete | Critical | Medium | Critical |
| **18** | Fix Versionize major release configuration | ✅ Complete | Critical | Medium | Critical |
| **19** | Add rejection tracking to retrospective analysis | 🔲 Not Started | Low | High | Low |
| **20** | Create workflow validation tool | 🔲 Not Started | High | High | High |
| **21** | Fix "UAT artifact validation" GitHub status check | ✅ Complete | Critical | Low | Critical |
| **22** | Fix "PR Validation" GitHub status check | ✅ Complete | Critical | Low | Critical |

## Detailed Improvements

### #1: Fix Quality Engineer test plan folder instructions
**Proposed Changes:** Update `.github/agents/quality-engineer.agent.md` to reference the correct test plan folder path (`docs/features/<feature-name>/test-plan.md` instead of incorrect path)

**Rationale:** Agent created test plans in wrong folder because instructions pointed to wrong location

**Complexity:** Low  
**Value:** High

---

### #2: Enforce strict PR creation boundaries
**Proposed Changes:** Update Code Reviewer agent instructions to explicitly prohibit suggesting PR creation (Release Manager responsibility). Add boundary check: "🚫 Never: Suggest creating a PR or merging code"

**Rationale:** Code Reviewer violated RM boundary by suggesting PR creation

**Complexity:** Low  
**Value:** High

---

### #3: Fix Release Manager polling behavior
**Proposed Changes:** Update Release Manager instructions to use `gh run watch <run-id>` (blocking) instead of polling loops with status checks. Remove chat flood by waiting synchronously

**Rationale:** RM flooded chat with waiting messages while polling

**Complexity:** Medium  
**Value:** High

✅ **Implemented:** Release Manager now uses blocking `gh run watch` guidance.

---

### #4: Enforce project script usage
**Proposed Changes:** Update all agent instructions to mandate use of `scripts/pr-github.sh` instead of raw `gh pr create`. Add explicit command examples and failure recovery patterns

**Rationale:** Agent attempted raw `gh pr create` commands which failed due to branch policies

**Complexity:** Low  
**Value:** High

---

### #5: Fix Task Planner boundary violation
**Proposed Changes:** Strengthen Task Planner system prompt with explicit "Plan Mode" enforcement: Must stop after creating plan and explicitly request approval before any implementation. Add "🚫 Never: Start implementation without approval"

**Rationale:** Task Planner started implementation immediately instead of creating a plan as explicitly instructed

**Complexity:** Medium  
**Value:** Critical

**Implementation Details:**
- Update `.github/agents/task-planner.agent.md`
- Add explicit "Plan Mode" enforcement section
- Strengthen boundaries section with implementation prohibition
- Add workflow step to explicitly wait for approval
- Add example of correct vs incorrect behavior

---

### #6: Improve Retrospective Agent critical analysis
**Proposed Changes:** Update Retrospective agent to adopt critical Scrum Master stance. Success ≠ perfection. Deduct points for boundary violations, instruction errors, manual interventions. Add scoring rubric

**Rationale:** Retrospective agent failed to provide critical analysis, gave unjustifiably high ratings

**Complexity:** Medium  
**Value:** High

---

### #7: Add interactive retrospective phase
**Proposed Changes:** Update Retrospective agent to include probing questions phase before final report (Scrum Master style). Ask about pain points, rejections, model performance, boundary violations

**Rationale:** User had to volunteer observations that should have been discovered through questioning

**Complexity:** Medium  
**Value:** High

---

### #8: Ensure Retrospective terminal access
**Proposed Changes:** Add prerequisite check in Retrospective agent: verify terminal access enabled at start. If not available, request it before proceeding with analysis

**Rationale:** Terminal access was disabled during retrospective, preventing detailed command analysis

**Complexity:** Low  
**Value:** Medium

---

### #9: Replace Gemini 3 Pro due to instability
**Proposed Changes:** Replace Gemini 3 Pro assignments with more reliable models. CI retrospective showed "multiple internal failures requiring retries." Review ai-model-reference.md for alternatives

**Rationale:** Gemini 3 Pro experienced multiple internal failures during sessions

**Complexity:** High  
**Value:** Medium

✅ **Implemented:** Agent model assignments were adjusted and model guidance updated in `docs/ai-model-reference.md`.

---

### #10: Create analyze-run.sh helper script
**Proposed Changes:** Create `scripts/analyze-run.sh` to fetch and summarize GitHub Actions run logs for errors. Reduces repeated manual `gh run view --log` commands

**Rationale:** Log analysis of GitHub Actions runs was performed manually multiple times

**Complexity:** Medium  
**Value:** Medium

---

### #11: Create cleanup-tags.sh helper script
**Proposed Changes:** Create `scripts/cleanup-tags.sh` to safely delete local/remote tags with validation. Prevents manual `git tag -d` and `git push --delete` errors

**Rationale:** Manual tag cleanup commands were used multiple times

**Complexity:** Low  
**Value:** Low

---

### #12: Enforce UAT artifact validation
**Proposed Changes:** Update UAT Tester instructions to validate artifact relevance before running tests. Add checklist: "Does this artifact exercise the changed code paths?"

**Rationale:** Initial UAT run used default artifacts that didn't test the changes

**Complexity:** Low  
**Value:** High

---

### #13: Enforce UAT report updates
**Proposed Changes:** Update UAT Tester instructions to update UAT report after *every* run automatically, not only when reminded. Make it a mandatory step in the workflow

**Rationale:** UAT agent needed reminders to update the report after second run

**Complexity:** Low  
**Value:** High

---

### #14: Investigate GPT-5.2 performance
**Proposed Changes:** Gather data on GPT-5.2 latency issues (subjectively slow, UAT stops mid-task). Compare with benchmarks. Consider model reassignment if performance is consistently poor

**Rationale:** User reported GPT-5.2 as subjectively slow, UAT agent stopped mid-task multiple times

**Complexity:** High  
**Value:** Medium

✅ **Implemented:** Performance/latency and comparative benchmark data was added to `docs/ai-model-reference.md` and agent model assignments were updated accordingly.

---

### #15: Add pre-commit validation script
**Proposed Changes:** Create `scripts/pre-commit-checks.sh` to run Docker build + markdown lint before commits. Catches issues earlier in workflow

**Rationale:** Would prevent issues from reaching CI stage

**Complexity:** Medium  
**Value:** Medium

---

### #16: Add workflow completion checklist to Release Manager
**Proposed Changes:** Add explicit completion checklist in RM instructions: ✅ PR merged, ✅ CI passed, ✅ Release deployed. Prevents premature handoffs

**Rationale:** RM suggested handoff before CI/Deployment completed

**Complexity:** Low  
**Value:** High

---

### #17: Fix release.yml duplicate trigger issue
**Proposed Changes:** Update `.github/workflows/release.yml` to handle tag triggers correctly in GitHub Pro. Remove conflicting `workflow_dispatch` or make triggers mutually exclusive

**Rationale:** Switch to GitHub Pro caused duplicate release runs (tag trigger + manual trigger = 3 runs)

**Complexity:** Medium  
**Value:** Critical

✅ **Implemented:** Tag-only release triggering and prerelease handling was implemented in commit `40aed00`.

---

### #18: Fix Versionize major release configuration
**Proposed Changes:** Enforce prerelease mode permanently for 0.x phase. Update Versionize config or CI env vars to prevent unintended major bumps (v1.2.0 instead of v0.x.y)

**Rationale:** Versionize created major release v1.2.0 despite attempts to configure pre-release mode

**Complexity:** Medium  
**Value:** Critical

✅ **Implemented:** CI now runs Versionize with `--pre-release alpha` (commit `40aed00`).

---

### #19: Add rejection tracking to retrospective analysis
**Proposed Changes:** Enhance chat log analysis to capture UI-level rejections and retry button events (currently missing from exported logs). Improves accuracy of failure metrics

**Rationale:** User reported rejections that weren't captured in chat log analysis

**Complexity:** High  
**Value:** Low

---

### #20: Create workflow validation tool
**Proposed Changes:** Create tool to validate agent definitions: check tool names exist, verify handoff targets exist, validate model availability, check boundary consistency

**Rationale:** Multiple issues with agent configuration (wrong tool names, invalid handoffs)

**Complexity:** High  
**Value:** High

---
# #21: Fix "UAT artifact validation" GitHub status check
**Proposed Changes:** Investigate and fix the "UAT artifact validation" required status check that never runs. This check is expected by GitHub but doesn't execute, forcing manual policy override to merge PRs.

**Rationale:** Required check blocks PR merges, requires bypassing rules manually

**Complexity:** Low  
**Value:** Critical

**Implementation Details:**
- Check if workflow exists but has wrong trigger conditions
- Check if workflow file is missing or disabled
- Verify branch protection rules reference correct check name
- Either fix the workflow to run or remove from required checks

---

##
## Priority Groups

### Critical (Do First)
- #5: Task Planner boundary violation ✅
- #17: Release pipeline duplication ✅
- #18: Versionize configuration ✅
- #21: UAT artifact validation check (blocks PR merges)

### Quick Wins (High Value / Low Effort)
- #1: Quality Engineer folder path
- #2: Code Reviewer PR boundaries
- #4: Enforce script usage
- #12: UAT artifact validation
- #13: UAT report updates
- #16: Release Manager checklist

### Medium Priority
- #3: Release Manager polling ✅
- #6: Retrospective critical analysis
- #7: Interactive retrospective
- #10: analyze-run.sh script
- #14: GPT-5.2 performance ✅
- #15: Pre-commit checks

### Research/Long-term
- #9: Gemini 3 Pro replacement ✅
- #11: cleanup-tags.sh
- #19: Rejection tracking
- #20: Workflow validation tool
