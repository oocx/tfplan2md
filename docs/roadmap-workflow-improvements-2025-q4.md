# Roadmap: Workflow Improvements (Q4 2025)

**Source:** [Retrospective: Consistent Value Formatting](features/consistent-value-formatting/retrospective.md)
**Date:** 2025-12-25
**Status:** Planned

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

## 2. 🤝 Agent Communication & Role Clarity

**Goal:** Reduce maintainer overhead by fixing "who does what" and "what's the status".

*   **Roles:** Clarify **Architect** vs. **Task Planner** boundaries (Architect = design, Task Planner = `tasks.md`).
*   **Protocol:** Add mandatory "Handoff Templates" and "Status Update" formats to **Developer** and other agents.
*   **Items:**
    *   Clarify Architect vs. Task Planner deliverable ownership.
    *   Standardize handoff messages (what's done, what's next).
    *   Require explicit Developer status updates and `tasks.md` sync.

## 3. 🛡️ Process Gates & Standards

**Goal:** Prevent defects and inconsistencies earlier in the pipeline.

*   **Review:** Add "Doc Alignment" gate to **Code Reviewer**.
*   **Standards:** Wire the Report Style Guide into agent instructions.
*   **Release:** Enforce "Rebase and Merge" policy for **Release Manager**.
*   **Items:**
    *   Add "doc consistency checklist" to Code Review.
    *   Wire Report Style Guide into workflow/agent docs.
    *   Encode "rebase and merge" policy for Release Manager.

## 4. 📊 Metrics & Retrospectives

**Goal:** Ensure we can measure improvement over time.

*   **Templates:** Standardize the "Standard Metrics" table in the workflow.
*   **Agents:** Teach **Retrospective** agent how to calculate lead time and review cycles from git/chat history.
*   **Items:**
    *   Standardize "Standard Metrics" section in retrospectives.
    *   Provide Retrospective agent with a "metrics recipe".
