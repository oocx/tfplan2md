# Retrospective Improvements Tracker

**Source:** [retrospective.md](retrospective.md)  
**Feature:** Universal Azure Resource ID Formatting  
**Created:** 2025-12-26  
**Status:** In Progress

---

## Improvement Items

| # | Issue | Description | Potential Solutions | Impact | Effort | Status |
|---|-------|-------------|---------------------|--------|--------|--------|
| 1 | **Planner Execution** | Planner agent repeatedly attempts to start implementation instead of stopping after creating the plan | Update `docs/agents.md` (Planner section) to emphasize "Deliverable is the plan, do not start coding"; add explicit handoff instruction | High | Low | ✅ Done |
| 2 | **Agent Boundaries** | Multiple agents (Tech Writer, UAT) modified files outside their scope (source code, code reviews, retrospectives) | Update `docs/agents.md` with reinforced file ownership rules; explicitly forbid cross-role file editing in each agent's Boundaries section | High | Medium | ✅ Done |
| 3 | **Release Safety** | Release Manager triggered release before CI completed and suggested skipping critical CI steps | Update Release Manager agent to mandate "Green CI" verification before tagging; explicitly forbid "skipping" pipeline steps | High | Low | ✅ Done |
| 4 | **Release Efficiency** | Release Manager runs redundant local tests that already run in CI | Update Release Manager agent instructions to skip local tests unless specifically needed for debugging | Low | Low | ✅ Done |
| 5 | **UAT Automation** | UAT workflow is fragmented and manual (scripts not executable, polling loops hanging) | Create consolidated `uat-run.sh` script handling entire lifecycle (setup, create, poll, cleanup) for both platforms | High | Medium | ⬜ Not Started |
| 6 | **UAT Strategy** | UAT PRs clutter the main repository | Configure UAT scripts to target a dedicated test repository (e.g., `oocx/tfplan2md-uat`) | Medium | Medium | ✅ Done |
| 7 | **UAT Guidance** | UAT PRs lack testing instructions for reviewers | Update UAT scripts to inject a "Test Instructions" section into PR body | Medium | Low | ✅ Done |
| 8 | **Developer Workflow** | Snapshots and artifacts not consistently regenerated after bug fixes | Add mandatory checklist to Developer agent for regenerating artifacts after code changes | High | Low | ✅ Done |
| 9 | **Script Hygiene** | PR scripts require temporary files for PR bodies (awkward) | Update `pr-github.sh` and others to accept input via stdin or arguments | Low | Low | ✅ Done |
| 10 | **Repo Maintenance** | Scripts lack executable permissions in git | Run `chmod +x` on all scripts in `scripts/` and commit | Low | Low | ✅ Done |
| 11 | **Retrospective Scope** | Retrospective agent initially only analyzed active session, not full lifecycle | Update Retrospective agent to require analysis of full feature lifecycle from issue through release | Medium | Low | ✅ Done |
| 12 | **Retrospective Metrics** | Retrospective agent required prompting to include timeline/metrics | Update Retrospective agent with mandatory "Metrics Collection" step (duration, turns, files changed) | Medium | Low | ✅ Done |
| 13 | **Retrospective Full-Lifecycle** | Agent must analyze complete process with chat logs and artifacts | Update Retrospective agent to mandate attaching/referencing chat logs and key artifacts; add checklist to verify each phase was evaluated | Medium | Low | ✅ Done |

---

## Legend

| Status | Meaning |
|--------|---------|
| ⬜ Not Started | Work has not begun |
| 🔄 In Progress | Currently being worked on |
| ✅ Done | Completed and verified |
| ❌ Won't Fix | Decided not to implement |

---

## Progress Summary

- **Total Items:** 13
- **Completed:** 12
- **In Progress:** 0
- **Remaining:** 1

---

## Notes

- Items are ordered by impact (High → Medium → Low), then by effort (Low → Medium → High)
- High-impact, low-effort items should be prioritized
- Each completed item should reference the commit or PR where it was implemented
