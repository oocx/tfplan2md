# Roadmap: Workflow Improvements (Q4 2025)

**Source:** [Retrospective: Visual Report Enhancements](features/024-visual-report-enhancements/retrospective.md)
**Date:** 2025-12-29
**Status:** In Progress

This roadmap outlines a series of workflow improvements derived from retrospectives. The goal is to reduce friction, clarify agent roles, and improve process reliability.

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
- ✅ Platform-specific artifact validation with smart defaults (inline-diff for AzDO, simple-diff for GitHub): ([#116](https://github.com/oocx/tfplan2md/pull/116))
- ✅ Stable demo generation + snapshot update scripts reduce approval prompts from ~7-10 to 1-2: ([#117](https://github.com/oocx/tfplan2md/pull/117))
- ✅ Migrated to tools-first approach: GitHub chat tools preferred over gh CLI for PR inspection/management ([#108](https://github.com/oocx/tfplan2md/pull/108), [#109](https://github.com/oocx/tfplan2md/pull/109))

## 2. 🤝 Agent Communication & Role Clarity

**Goal:** Reduce maintainer overhead by fixing "who does what" and "what's the status".

*   **Roles:** Clarify **Architect** vs. **Task Planner** boundaries (Architect = design, Task Planner = `tasks.md`).
*   **Protocol:** Add mandatory "Handoff Templates" and "Status Update" formats to **Developer** and other agents.
*   **Items:**
    *   Clarify Architect vs. Task Planner deliverable ownership.
    *   Standardize handoff messages (what's done, what's next).
    *   Require explicit Developer status updates and `tasks.md` sync.

**Progress**
- ✅ Role boundaries enforced: Architect forbidden from creating/editing tasks.md, Task Planner owns tasks.md ([#114](https://github.com/oocx/tfplan2md/pull/114))
- ✅ Handoff template added to Architect, Task Planner, Developer ([#114](https://github.com/oocx/tfplan2md/pull/114))
- ✅ Developer status template added requiring explicit status + tasks.md sync ([#114](https://github.com/oocx/tfplan2md/pull/114))

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
- ✅ PR creation skills require agents to post the exact PR title + description in chat (using the standard template) before creating PRs (supersedes earlier "preview" approach).
- ✅ Wrapper scripts require explicit `--title` and `--body`/`--body-file` (no heuristics like `--fill`).
- ✅ Doc alignment gate added to Code Reviewer ([#113](https://github.com/oocx/tfplan2md/pull/113))
- ✅ Report style guide wired into Requirements Engineer, Developer, Code Reviewer, Technical Writer, UAT Tester ([#112](https://github.com/oocx/tfplan2md/pull/112))
- ✅ Release Manager → Retrospective handoff added ([#111](https://github.com/oocx/tfplan2md/pull/111))
- ✅ Release Manager merge-method enforcement added (rebase-only with script/CLI guardrails)

## 4. 📊 Metrics & Retrospectives

**Goal:** Ensure we can measure improvement over time.

*   **Templates:** Standardize the "Standard Metrics" table in the workflow.
*   **Agents:** Teach **Retrospective** agent how to calculate lead time and review cycles from git/chat history.
*   **Items:**
    *   Standardize "Standard Metrics" section in retrospectives.
    *   Provide Retrospective agent with a "metrics recipe".

**Progress**
- ✅ Added metrics guidance/templates as part of workflow docs updates: ([#84](https://github.com/oocx/tfplan2md/pull/84))

## 5. 🛡️ Repository Integrity & Visual Feedback (High Priority)

**Goal:** Prevent documentation loss and reduce visual rendering friction.

*   **Scripting:** Create `scripts/safe-merge.sh` to verify file integrity post-merge.
*   **Skills:** Implement `.github/skills/visual-validator/` for markdown-to-image rendering.
*   **Process:** Add "Detail Checklist" to Developer and "Conflict Check" to Release Manager.
*   **Commits:** Enforce "1 topic per commit" by amending previous commits for fixes to recent work.
*   **Items:**
    *   Automate post-merge file integrity verification.
    *   Provide agents with visual feedback for markdown rendering.
    *   Explicitly flag UAT rejections in test output.
    *   Enforce detail-oriented checklists for UI/UX features.
    *   Amend previous commits for immediate fixes to maintain clean history.

**Progress**
- ⏳ Proposed in [Workflow Improvements: 2025-12-29](workflow/025-improvements-2025-12-29/workflow-improvements.md)
- ⏳ Follow-up tasks consolidated in [Workflow Improvements: 2025-12-31](workflow/026-improvements-2025-12-31/workflow-improvements.md)
