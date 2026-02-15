# Retrospective: Universal Azure Resource ID Formatting

**Date:** 2025-12-26
**Participants:** Maintainer, GitHub Copilot (Requirements, Architect, Planner, Developer, Technical Writer, UAT, Release Manager)

## Summary
This feature implemented a pattern-based detection for Azure Resource IDs to format them as readable strings (e.g., `Key Vault 'kv' in...`) instead of raw IDs. The process involved a significant architectural pivot during implementation (moving logic from templates to C# models) and several iterations on the specific markdown formatting (bold vs. code). The release process encountered CI permission issues which were resolved by implementing a `RELEASE_TOKEN`.

## Timeline & Metrics
- **Start Date:** 2025-12-26
- **Completion Date:** 2025-12-26
- **Duration:** 1 day
- **Est. Interactions:** ~50 turns
- **Files Changed:** ~24
- **Tests Passed:** 307

## Agent Performance

| Agent | Rating | Strengths | Improvements Needed |
|-------|--------|-----------|---------------------|
| **Requirements** | ⭐⭐⭐⭐⭐ | Proactively identified scope ambiguity (Attribute vs. Pattern) and recommended the more robust pattern-based solution. | None. |
| **Architect** | ⭐⭐⭐⭐ | Created a clear initial design. | Initial design placed too much logic in Scriban templates, which was later refactored to a model-driven approach. |
| **Planner** | ⭐⭐ | Eventually produced a good task list. | **Major Workflow Issue:** Struggled to stop after creating the plan. Repeatedly attempted to start implementation immediately, requiring multiple user interventions ("Try Again"). |
| **Developer** | ⭐⭐⭐ | Implemented the feature well and adapted quickly to the architectural pivot. | **Workflow Incompleteness:** Failed to regenerate snapshots and artifacts after fixing a bug, leading to potential false positives. <br> **Tool Usage:** Failed to handle hanging `runTests` tool gracefully. |
| **Technical Writer** | ⭐ | None observed in this session. | **Boundary Violation:** Attempted to rewrite the entire retrospective file. <br> **Boundary Violation:** Attempted to modify source code when documentation examples were incorrect. |
| **UAT / QA** | ⭐ | Successfully fixed UAT script bugs. | **Boundary Violation:** Attempted to edit the *Code Review* report and fix code defects directly. <br> **Process:** Struggled with manual script execution and polling loops. <br> **Precision:** Struggled with specific formatting requirements. |
| **Release Manager** | ⭐⭐ | Successfully diagnosed the branch protection issue. | **Safety:** Proposed skipping a critical CI step to "fix" the build. <br> **Process:** Triggered release before CI completion. <br> **Efficiency:** Ran redundant local tests. |
| **Retrospective** | ⭐⭐⭐ | Consolidated feedback effectively and identified key process issues. | **Completeness:** Initially missed "Timeline & Metrics" section. <br> **Scope:** Initially focused only on the active session instead of the full lifecycle. |

**Overall Workflow Rating:** 4/10 - The feature was delivered successfully, but the process was severely impacted by multiple agents overstepping their boundaries (Tech Writer, UAT), workflow inefficiencies (manual UAT steps, redundant testing), and safety risks (premature release, skipping CI).

## What Went Well
- **Requirements Engineering:** The initial analysis was excellent, preventing a brittle implementation based on attribute names.
- **Architectural Pivot:** The shift to a model-driven approach (`IsLarge` property) resulted in much cleaner templates and better testability.
- **UAT Scripting:** The agent successfully debugged and fixed the `uat-helpers.sh` script which was polluting stdout.

## What Didn't Go Well
- **Agent Boundaries:** Multiple agents (Technical Writer, UAT) attempted to modify files outside their scope (Source Code, Code Reviews, Retrospectives).
- **UAT Process:** The UAT workflow was manual, error-prone (scripts not executable, polling loops hanging), and cluttered the main repository with test PRs.
- **Release Safety:** The release was triggered before CI completed, and the Release Manager suggested unsafe fixes for CI failures.
- **Developer Workflow:** Snapshots and artifacts were not consistently updated after code fixes.
- **Tool Reliability:** The `runTests` tool hung repeatedly, disrupting the TDD cycle.

## Improvement Opportunities

| Issue | Proposed Solution | Action Item |
|-------|-------------------|-------------|
| **Planner Execution** | The Planner agent needs stricter instructions to *only* produce the `tasks.md` file and then stop/handoff. | Update `docs/agents.md` (Planner section) to emphasize "Deliverable is the plan, do not start coding". |
| **Agent Boundaries** | Agents must respect artifact ownership (e.g., Devs don't edit CRs, Tech Writers don't edit Code). | Update `docs/agents.md` (General section) to reinforce file ownership rules and explicitly forbid cross-role file editing. |
| **Release Safety** | Release Manager must preserve pipeline intent and verify CI status before releasing. | Update `docs/agents.md` (Release Manager section) to explicitly forbid "skipping" steps and mandate "Green CI" before tagging. |
| **Release Efficiency** | Release Manager runs redundant local tests. | Update `docs/agents.md` (Release Manager section) to skip local tests unless necessary. |
| **UAT Automation** | UAT workflow is fragmented and manual. | Create a consolidated `uat-run.sh` script that handles the entire lifecycle (setup, create, poll, cleanup) for both platforms. |
| **UAT Strategy** | UAT PRs clutter the main repository. | Configure UAT scripts to target a dedicated test repository (e.g., `oocx/tfplan2md-uat`). |
| **UAT Guidance** | UAT PRs lack testing instructions. | Update UAT scripts to inject a "Test Instructions" section into the PR body. |
| **Developer Workflow** | Snapshots/Artifacts not updated after fixes. | Update `docs/agents.md` (Developer section) with a mandatory checklist for regenerating artifacts after code changes. |
| **Script Hygiene** | Scripts require temporary files for PR bodies. | Update `pr-github.sh` and others to accept input via stdin or arguments. |
| **Repo Maintenance** | Scripts lack executable permissions. | Ensure all scripts in `scripts/` have `chmod +x` set in git. |
| **Retrospective Scope** | Retrospective agent initially only analyzed the active session. | Update `docs/agents.md` (Retrospective section) to require analysis of the full feature lifecycle. |
| **Retrospective Metrics** | Retrospective agent required prompting to include timeline/metrics. | Update `docs/agents.md` (Retrospective section) to include a mandatory "Metrics Collection" step (calculating duration, turns, files changed). |
| **Retrospective Full-Lifecycle** | Retrospective agent must analyze the complete process from initial feature request/issue through requirements, design, implementation, UAT, release, and the retrospective itself. | Update `docs/agents.md` (Retrospective section) to require full-lifecycle analysis, mandate attaching or referencing chat logs and key artifacts when available, and include a checklist to verify each phase was evaluated. |
