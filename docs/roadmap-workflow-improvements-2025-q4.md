# Roadmap: Workflow Improvements (Q4 2025)

**Source:** [Retrospective: Consistent Value Formatting](features/consistent-value-formatting/retrospective.md)
**Date:** 2025-12-25
**Status:** In progress

This roadmap outlines a series of workflow improvements derived from the "Consistent Value Formatting" feature retrospective. The goal is to reduce friction (especially in UAT), clarify agent roles, and improve process measurability.

## 1. 🚨 UAT Tooling & Process Overhaul (High Priority)

**Goal:** Reduce manual approval fatigue and prevent "wrong artifact" errors.

*   **Scripting:** Create `scripts/uat-run.sh` wrapper to batch permissions and harden existing scripts against wrong artifacts/branch collisions.
*   **Agents:** Update **UAT Tester** and **Quality Engineer** to use the new wrapper, enforce canonical artifacts, and strictly forbid code fixes.
*   **Items:**
    *   Reduce manual approvals for terminal commands (batching/wrappers).
    *   Add guardrails against posting wrong artifacts in UAT.
    *   Standardize UAT inputs (comprehensive demo).
    *   Make UAT a first-class test phase (report required, no code fixes).
    *   Standardize UAT branch naming.

**Progress**
- ✅ Added stable wrapper scripts to reduce ad-hoc commands: GitHub ([#87](https://github.com/oocx/tfplan2md/pull/87)), Azure DevOps ([#88](https://github.com/oocx/tfplan2md/pull/88)), AzDO abandon cleanup ([#89](https://github.com/oocx/tfplan2md/pull/89))
- ✅ Added UAT watch scripts/skills for polling: ([#90](https://github.com/oocx/tfplan2md/pull/90))
- ✅ Hardened GitHub UAT polling to use structured JSON and filter agent comments: ([#92](https://github.com/oocx/tfplan2md/pull/92))
- ✅ Added `scripts/uat-run.sh` end-to-end wrapper (GitHub + AzDO orchestration): ([#95](https://github.com/oocx/tfplan2md/pull/95))
- ✅ Added initial guardrails against wrong artifacts in UAT scripts (canonical artifact enforcement still pending): ([#95](https://github.com/oocx/tfplan2md/pull/95))

## 2. 🤝 Agent Communication & Role Clarity

**Goal:** Reduce maintainer overhead by fixing "who does what" and "what's the status".

*   **Roles:** Clarify **Architect** vs. **Task Planner** boundaries (Architect = design, Task Planner = `tasks.md`).
*   **Protocol:** Add mandatory "Handoff Templates" and "Status Update" formats to **Developer** and other agents.
*   **Items:**
    *   Clarify Architect vs. Task Planner deliverable ownership.
    *   Standardize handoff messages (what's done, what's next).
    *   Require explicit Developer status updates and `tasks.md` sync.

**Progress**
- ⏳ Not implemented yet (still planned)

## 3. 🛡️ Process Gates & Standards

**Goal:** Prevent defects and inconsistencies earlier in the pipeline.

*   **Review:** Add "Doc Alignment" gate to **Code Reviewer**.
*   **Standards:** Wire the Report Style Guide into agent instructions.
*   **Release:** Enforce "Rebase and Merge" policy for **Release Manager**.
*   **Items:**
    *   Add "doc consistency checklist" to Code Review.
    *   Wire Report Style Guide into workflow/agent docs.
    *   Encode "rebase and merge" policy for Release Manager.

**Progress**
- ✅ Added skill design guidance to minimize Maintainer approvals (prefer stable wrapper commands): ([#86](https://github.com/oocx/tfplan2md/pull/86))
- ✅ PR creation skills now require a chat preview (title + summary) before creating PRs: ([#93](https://github.com/oocx/tfplan2md/pull/93))
- ✅ Wrapper scripts add a `preview` helper command to generate the chat preview consistently: ([#96](https://github.com/oocx/tfplan2md/pull/96))
- ⏳ Doc alignment gate in Code Review not implemented yet
- ⏳ Report style guide wiring into agent instructions not implemented yet
- ⏳ Release Manager merge-method enforcement not implemented yet

## 4. 📊 Metrics & Retrospectives

**Goal:** Ensure we can measure improvement over time.

*   **Templates:** Standardize the "Standard Metrics" table in the workflow.
*   **Agents:** Teach **Retrospective** agent how to calculate lead time and review cycles from git/chat history.
*   **Items:**
    *   Standardize "Standard Metrics" section in retrospectives.
    *   Provide Retrospective agent with a "metrics recipe".

**Progress**
- ✅ Added metrics guidance/templates as part of workflow docs updates: ([#84](https://github.com/oocx/tfplan2md/pull/84))
