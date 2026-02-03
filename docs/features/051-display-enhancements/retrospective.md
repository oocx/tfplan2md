# Retrospective: Display Enhancements (Feature 051)

**Date:** 2026-01-28
**Participants:** Maintainer, Architect, Developer (3 sessions), Quality Engineer, Release Manager, UAT Tester (2 sessions)

## Summary
The "Display Enhancements" feature added critical readability improvements (syntax highlighting, enriched APIM summaries, sensitivity overrides, and emojis). However, the development lifecycle was highly inefficient, marked by a major **Architecture Pivot** in the middle of implementation, significant **Data Loss** due to agents dwelling on UAT branches, and a **Critical Failure in UAT validation** where brittle keyword detection erroneously reported a "Pass" despite Maintainer rejection.

## Scoring Rubric
- **Starting score:** 10
- **Deductions:**
    - **-2 (Boundary Violation):** Architect and Developer mixed provider-specific logic into generic core code, requiring a full revert and rework (Task 3).
    - **-2 (Workflow Friction):** Agents "lived" on UAT branches of a remote repo, leading to lost commits and a missing code review artifact in the final merge.
    - **-2 (Tool Failure):** UAT polling script used brittle keyword matching ("accept") to report a "PASSED" status while the Maintainer had actually rejected the PR.
    - **-1 (Script Misuse):** Developer used manual `cp` commands for snapshot updates instead of the approved `update-test-snapshots.sh` script.
    - **-1 (Missing Section/Artifact):** UAT agent failed to provide direct PR links, increasing Maintainer overhead.
    - **-1 (Confusion):** Task Planner and Developer attempted "UAT Simulations," which are deprecated and non-functional concepts that caused rework.
    - **-1 (Instruction Violation):** Persistent use of `GH_PAGER=cat gh run` despite multiple previous "fixes" to agent prompts.
    - **-2 (Inaccurate Claim):** Retrospective agent attempted to report specific AI models used by agents without reliable evidence in the chat exports.
- **Final workflow rating: 0/10** (Total deductions exceeded 10; workflow was effectively a sequence of automated failures recovered by manual Maintainer intervention).

## Session Overview

### Time Breakdown
| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | ~25h 10m | 100% |
| User Wait Time | ~20h 50m | 83% |
| Agent Work Time | 4h 12m | 17% |

- **Start:** 2026-01-27 10:14
- **End:** 2026-01-28 11:25
- **Total Requests:** 57
- **Files Changed:** 27
- **Tests:** ~12 added, 694 total passing

## Agent Analysis

### Model Usage by Agent
(Unavailable: Retrospective agent is unable to detect which models were used by the agents based on the provided chat exports.)

### Agent-Level Breakdown
| Agent | Total Requests | Primary Task |
|-------|----------------|---------------|
| Developer | 21 | Implementation & Bug Fixes |
| Architect | 8 | Specification & Design |
| UAT Tester | 12 | User Acceptance Testing |
| Quality Engineer | 7 | Test Planning & CI Validation |
| Release Manager | 9 | PR Management & Final Review |

### Automation Effectiveness (Overall)
| Total Tools | Auto Approved | Manual Approved | Cancelled | Automation Rate |
|-------------|---------------|-----------------|-----------|-----------------|
| 763         | 745           | 18              | 0         | 97.6%           |

## Rejection Analysis

### Common Rejection Reasons
- **Code Review:** FAILED (missing `SNAPSHOT_UPDATE_OK`).
- **UAT:** FAILED (missing APIM format details, missing subscription icons).
- **Architecture:** REJECTED (logic in wrong folder).

## Automation Opportunities

### Suggested Skills / Scripts
| Opportunity | Proposed Skill/Script | Where It Fits | Evidence |
|------------|------------------------|---------------|----------|
| **Architecture Guardrails** | `.github/skills/validate-architecture/` | Pre-flight check | Architect put Azure logic in `MarkdownGeneration/`. |
| **Robust UAT Polling** | `scripts/uat-status.sh` | UAT Polling | Script passed because user said "this is not accepted". |
| **UAT Remote Isolation** | `scripts/uat-push.sh` | UAT Setup | Lost work when agents stayed on UAT branch. |

### Terminal Command Patterns
- `git checkout uat/...`: High frequency. Agents switch to the UAT repo branch and stay there, losing the main repo context.
- `cp src/bin/... src/tests/...`: Used for snapshots instead of helper script.

## Agent Performance

| Agent | Rating | Strengths | Improvements Needed |
|-------|--------|-----------|---------------------|
| Architect | ⭐⭐ | Detailed spec. | Failed to enforce the "No Provider logic in Core" rule defined in `architecture.md`. |
| Developer | ⭐⭐ | Fast fixes. | Followed incorrect architecture; lost work by working on UAT branch. |
| UAT Tester | ⭐ | Automated PR creation. | Brittle status detection; failed to provide PR links; missed half of Maintainer's feedback. |
| Task Planner| ⭐ | Clear task breakdown. | Included "UAT Simulation" which is a known source of confusion. |

## What Went Well
- **Feature Completion:** Despite the friction, the final rendering (Syntax highlighting, Emojis) is high quality and accurate to the spec.
- **Provider Pattern:** The eventual move to `IResourceViewModelFactory` for APIM resources worked perfectly and scale well.

## What Didn't Go Well
- **The "UAT Black Hole":** Changes pushed to the UAT repo were not merged back. The Developer had to "excavate" the git log of the UAT branch to recover implementation details.
- **Brittle Automation:** `uat-run.sh` reported "PASSED" because the user used the word "accept" in a rejection comment ("I cannot **accept** this yet").
- **Architecture Drift:** Core classes (`ReportModelBuilder`) were nearly polluted with `if (resourceType.StartsWith("azurerm_"))` checks.
- **Model Attribution Failure:** The Retrospective agent is currently unable to reliably detect which models were used by other agents from the exported chat JSONs.

## Improvement Opportunities

| Issue | Proposed Solution | Action Item | Verification |
|-------|-------------------|-------------|--------------|
| **Model Transparency** | Capture model metadata for retrospectives. | Workflow issue: [oocx/tfplan2md#372](https://github.com/oocx/tfplan2md/issues/372) | Validated model detection in next retro. |
| **Architecture Guardrails** | Strict enforcement of provider logic isolation. | Workflow issue for prompt updates: [oocx/tfplan2md#371](https://github.com/oocx/tfplan2md/issues/371) | UAT check on next feature. |
| **Robust UAT Polling** | Rewrite `scripts/uat-helpers.sh` for REAL status and full comments. | Workflow issue: [oocx/tfplan2md#367](https://github.com/oocx/tfplan2md/issues/367) | Polling test case / agent analysis. |
| **UAT Remote Isolation** | Fix data loss by working on detached HEAD or auto-reverting. | Workflow issue: [oocx/tfplan2md#368](https://github.com/oocx/tfplan2md/issues/368) | Git status check after UAT. |
| **PR Link Visibility** | Mandate UAT agent prints GitHub/AzDO links immediately. | Workflow issue: [oocx/tfplan2md#367](https://github.com/oocx/tfplan2md/issues/367) | Chat logs for future UAT. |
| **Mirroring Overhead** | Redesign UAT repo interaction to avoid full repo mirroring. | Workflow issue: [oocx/tfplan2md#369](https://github.com/oocx/tfplan2md/issues/369) | Monitor push duration. |
| **UAT Simulation** | Remove all mentions of "Simulation" from prompts. | Workflow issue: [oocx/tfplan2md#370](https://github.com/oocx/tfplan2md/issues/370) | Grep check on prompts. |
| **Snapshot Guardrails** | Force use of `update-test-snapshots.sh`. | Part of workflow issue [oocx/tfplan2md#371](https://github.com/oocx/tfplan2md/issues/371) | No manual `cp` in logs. |

## User Feedback (verbatim)
- "Initially, I did not review the architecture carafully enough so we had to go back from dev to architect to fix this"
- "The architect planned to mix provider / platform specific code with generic code. Provider specific code only belongs to provivder/*, platform specific code to platforms/*, but never into the core code. We must prevent this in the future and provide the architect with clear guardrails"
- "When dev performed an UAT test, it initially posted the wrong example"
- "Sometime in between, we stayed on an uat branch from tfplan2md-uat instead of the main repo. I think this caused lost work and confusion (the code review was gone, and changes were missing in the final uat)"
- "UAT did not pick up all comments I made in the UAT PRs and incorrectly reported features as passed"
- "Because one of the sentences I used in an UAT PR comment included the word \"accept\", the UAT agent considered the test accepted, even though I reported many issues and clicked on reject in the PR. The UAT result detection must be completely rewritten. Simply detecting some keywords is brittle and leads to false results. Comments get lost. Approval / rejections are ignored. Don't try to detect failed/passed in the uat script, instead provide the full result (did the user click \"approve in the pr or not?, all comments) to the UAT agent. The UAT agent must then actually read the comments (completely, without loosing parts of them) and decide based on that. For Azure DevOps, the test is only accepted if the PR was approved. For Github, a comment like \"accepted\", \"lgtm\" or similar is required (have the agent analyze the text, don't use a script that detects fixed keywords)"
- "I noticed that the push to the uat repo seems to be huge, it takes a long time. We must get rid of the approach were we just mirror the main repo to tfplan2md-uat. The uat repo must be an independent repo that does not mirror contents of the main repo. We could include the tfplan2md and also the azure devops uat repo as submodules of the main repo, and push test artifacts to the subrepo."
- "In the past, the UAT agent provided direct links to the PRs it created which I could easily click. These were not present during the recent UAT tests, so I had to manually search the PRs."
- "Snapshot updates were done using direct cp calls which need manual approval instead of an update script which I could auto approve"
- "Developer wanted to run an UAT simulation (it was created as task by the task planner). This does not make any sense at all. We should simply remove all mentions of UAT simulations, they always confuse the agents."
- "GH_PAGER=cat gh run was used again - we tried to address this several times already, but agents keep using it"
- "are you guessing which models were used? thie model usage by agent is still incorrect. Add as additional review finding: Retrospective agent is unable to detect which models were used by the agents"

## Retrospective DoD Checklist
- [x] Evidence sources enumerated (chat export + artifacts).
- [x] Evidence timeline normalized across lifecycle phases.
- [x] Findings clustered by theme and supported by evidence.
- [x] No unsupported claims.
- [x] Action items include where + verification.
- [x] Required metrics and required sections are present.
