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
| 5 | **UAT Automation** | UAT workflow is fragmented and manual (scripts not executable, polling loops hanging) | Create consolidated `uat-run.sh` script handling entire lifecycle (setup, create, poll, cleanup) for both platforms | High | Medium | ✅ Done |
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
- **Completed:** 13
- **In Progress:** 0
- **Remaining:** 0

---

## Issue #5 Implementation Summary

**Implemented Solution:** Background Agent (Solution 2 from analysis below)

### What Was Created

**New File:** `.github/agents/uat-background.agent.md`
- Autonomous UAT execution agent running in CLI background mode
- Zero approval prompts - fully automated from start to finish
- Handles prerequisites, dirty tree, PR creation, monitoring, cleanup
- Reports final status in Chat view with PR links

**Updated File:** `.github/agents/uat-tester.agent.md`
- Added handoff capability: "Execute UAT Autonomously" → UAT Background agent
- Added documentation section explaining background agent usage

### How It Works

**User/Agent workflow:**
1. Determine UAT is ready to run
2. Hand off to `@uat-background` agent (via handoff button or @mention)
3. Background agent executes autonomously:
   - Verifies prerequisites (branch, gh/az auth, artifacts)
   - Auto-commits dirty working tree if needed
   - Runs `bash scripts/uat-run.sh` blocking to completion
   - Reports final status with PR numbers and URLs
4. User reviews results and proceeds

**Result:** Single handoff replaces 62+ manual terminal commands and constant supervision.

### Setup Requirements

- VS Code 1.107+ with Copilot CLI installed
- Enable experimental setting: `github.copilot.chat.cli.customAgents.enabled`
- Custom agents auto-discovered from `.github/agents/` directory

### Why This Solution

- **Simpler than enhanced script:** Configuration change vs code changes
- **Achieves north star goal:** Exactly one action (handoff) triggers complete UAT
- **Leverages VS Code built-in capability:** Background agents designed for this use case
- **Maintains interactive option:** `uat-tester` still available for debugging/verification

---

## Detailed Analysis: Issue #5 (Historical Context)

**Status:** Partially Complete - `uat-run.sh` exists but doesn't solve the core problem

### What Exists
- `scripts/uat-run.sh` - Consolidates PR lifecycle (create, poll, cleanup) for both GitHub and AzDO
- Integration with `uat-github.sh` and `uat-azdo.sh` helper scripts
- Polling mechanism for approval detection

### The Real Problem
Analysis of `docs/features/019-azure-resource-id-formatting/chat.json` reveals that during the UAT phase:

**Pre-Script Manual Steps (~7-10 commands):**
1. Environment verification: Check branch, GitHub auth (`gh auth status`), Azure CLI auth (`az account show`)
2. Script inspection: Read `uat-run.sh`, `uat-github.sh`, `uat-azdo.sh` to understand workflow
3. Working tree cleanup: Check `git status`, commit changes, re-verify

**During-Script Issues (~62 commands during entire UAT):**
- Script runs in blocking mode requiring agent to monitor
- Polling loop requires ongoing agent supervision
- Script failures require manual diagnosis and re-execution
- No autonomous operation - agent intervention required throughout

**Root Cause:** UAT isn't a single command - it's a multi-step process requiring:
- Manual prerequisite verification
- Script reading/understanding  
- Environment preparation
- Script invocation
- Ongoing monitoring
- Error recovery

### Proposed Solutions

#### Solution 1: Enhanced Single-Command Script (Incremental)
**Goal:** One safe-to-auto-approve command that handles everything

**Implementation:**
```bash
scripts/uat-run.sh --preflight --auto-commit --no-wait
```

**Changes to `uat-run.sh`:**
- `--preflight`: Check all prerequisites (auth, branch, artifacts) before proceeding
- `--auto-commit`: Automatically commit dirty tree with conventional message
- `--no-wait`: Create PRs and exit immediately without polling

**Auto-Approve Pattern:**
```regex
^cd .* && bash scripts/uat-run\.sh --preflight --auto-commit --no-wait$
```

**Pros:**
- Minimal changes to existing infrastructure
- Single command achieves goal
- Safe auto-approval pattern (specific flags prevent misuse)
- Agent doesn't need to monitor

**Cons:**
- Agent still needs to manually check PR approval status later
- Doesn't fully eliminate agent involvement

---

#### Solution 2: Background Agent Integration (North Star)
**Goal:** Fully autonomous UAT with zero agent monitoring

**Implementation:**
Create custom UAT background agent (`.github/agents/uat-background.agent.md`) that:
1. Runs autonomously via Copilot CLI background agent
2. Uses Git worktree for isolation
3. Handles full UAT lifecycle without approval prompts
4. Reports final status when complete

**Workflow:**
```
User/Agent: "Run UAT" → Hands off to background agent → Agent exits
Background Agent: Autonomous execution (preflight, PRs, polling, cleanup)
Background Agent: Reports completion in Chat view
```

**VS Code Background Agent Features:**
- **Autonomous execution** - No approval prompts for terminal commands
- **Git worktree isolation** - Prevents conflicts with active work
- **Progress tracking** - Visual status in Chat view
- **Handoff support** - Can receive context from local agents

**Custom Agent Configuration:**
```markdown
---
description: Autonomous UAT execution for markdown rendering validation
name: UAT Background
target: cli
tools: ['run_in_terminal', 'read_file', 'git']
---

# UAT Background Agent

You are the autonomous UAT (User Acceptance Testing) background agent.

## Your Goal
Execute the complete UAT workflow without human intervention:
1. Verify prerequisites (auth, branch, clean tree)
2. Create UAT PRs on GitHub and Azure DevOps
3. Poll for maintainer approval
4. Clean up PRs and branches
5. Report final status

## Workflow
1. Check prerequisites: `gh auth status`, `az account show`
2. Auto-commit if tree dirty: `git add -A && git commit -m "chore: pre-UAT commit"`
3. Run UAT: `scripts/uat-run.sh` (full blocking execution)
4. Report success/failure with PR links

## Commands
All commands run without approval prompts in background mode.
```

**Pros:**
- **True zero-touch UAT** - Agent starts it and walks away
- Built-in VS Code feature (no custom infrastructure)
- Isolated execution via worktrees prevents conflicts
- Visual progress tracking in Chat view
- Can hand off from any agent conversation

**Cons:**
- Requires VS Code 1.107+ with Copilot CLI
- Experimental feature (worktree isolation)
- More complex initial setup

---

#### Solution 3: Hybrid Approach (Recommended)
**Combine both solutions for maximum flexibility**

**Short-term (Solution 1):**
- Implement `--preflight`, `--auto-commit`, `--no-wait` flags
- Create safe auto-approval pattern
- Document single-command invocation

**Long-term (Solution 2):**
- Create UAT background agent
- Configure for autonomous execution
- Update agent instructions to hand off to background agent

**Benefits:**
- Immediate improvement (one command)
- Clear upgrade path to full automation
- Works for users without background agent support
- Ultimate goal: zero-touch UAT execution

---

### Recommendation
**Implement Solution 3 (Hybrid):**

1. **Phase 1 (Immediate):** Enhance `uat-run.sh` with flags
   - Add prerequisite checking
   - Add auto-commit capability  
   - Add non-blocking mode
   - Document auto-approval pattern

2. **Phase 2 (Future):** Create UAT background agent
   - Define custom agent configuration
   - Enable handoff from regular agents
   - Document background agent workflow

This provides immediate value while establishing path to full automation.

---
## Notes

- Items are ordered by impact (High → Medium → Low), then by effort (Low → Medium → High)
- High-impact, low-effort items should be prioritized
- Each completed item should reference the commit or PR where it was implemented
