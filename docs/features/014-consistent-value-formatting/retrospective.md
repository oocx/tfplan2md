# Retrospective: Consistent Value Formatting

**Date:** 2025-12-25
**Participants:** Maintainer (human), Developer agent, Code Reviewer agent, UAT Tester agent, Release Manager agent, Retrospective agent

## Summary

This cycle started as a cross-cutting formatting change (“data values are code, labels are text”) spanning templates, helper functions, summaries, tests, and docs. The implementation was largely straightforward, but the biggest learning came from real-world markdown rendering differences in GitHub vs Azure DevOps PR comments.

UAT initially failed due to (1) a wrong artifact/comment being used and (2) rendering expectations not matching the original documentation, which led to a short loop of “capture findings → fix behavior → align docs → re-run UAT”. The release proceeded smoothly once UAT passed and CI was green.

Maintainer feedback highlighted additional workflow friction: role boundary confusion between Architect and Task Planner, inconsistent agent handoffs/status reporting, missing guidance adoption (style guide not wired into agent docs), and repeated terminal tool approvals (especially during UAT).

## Agent Performance

| Agent | Rating (1-5) | Strengths | Improvements Needed |
|-------|--------------|-----------|---------------------|
| Developer | ⭐⭐⭐⭐ | Implemented the formatting changes broadly, added focused unit coverage for the UAT regression, and kept snapshots consistent. | Tighten “docs always match behavior” discipline earlier; prefer an explicit pre-merge doc consistency scan. |
| Code Reviewer | ⭐⭐⭐⭐ | Identified contradictions and forced decisions into explicit resolutions; surfaced doc vs implementation mismatches. | When issues are resolved, ensure the review report is updated promptly so downstream agents don’t act on stale “Changes Requested” status. |
| UAT Tester | ⭐⭐⭐⭐ | Validated in both GitHub and Azure DevOps and captured platform-specific findings; used polling + cleanup reliably. | Add guardrails against posting the wrong artifact; ensure UAT always ships with an explicit test plan and produces a results report; never attempt code fixes. |
| Release Manager | ⭐⭐⭐⭐ | Ensured PR checks passed and release automation completed successfully (tag + GitHub release + Docker). | Make merge-method policy explicit and consistent (prefer rebase-and-merge) and include an explicit handoff to Retrospective after release. |
| Retrospective | ⭐⭐⭐ | Turned the development narrative into actionable workflow improvements. | Capture metrics earlier during the cycle (e.g., number of UAT iterations, time-to-green) to make improvements measurable. |

**Overall Workflow Rating:** 8/10

Rationale: Strong automation (tests, demo generation, markdownlint, CI/release pipelines) and effective UAT practice. Main inefficiencies were preventable: spec/doc contradictions and the initial UAT artifact mistake.

## Timeline / Metrics (Approximate)

- UAT iterations: 2 rounds (initial fail, then pass)
- Regression safety: Added 1 focused non-snapshot unit test to prevent recurrence
- Automation health: CI and release pipelines succeeded without manual intervention once the correct merge method was used

## Standard Metrics (Proposed Template)

Use this section (with the same fields) in all retrospectives so trends are trackable over time.

| Metric | Value | Notes |
|---|---:|---|
| Lead time (first commit → merged) | 4h 45m | 2025-12-25 09:02 → 13:47 (+0100). Derived from local `HEAD` reflog (first feature commit at `1766649763`, `main` fast-forward at `1766666872`). |
| Review cycles (Code Review: request changes → approved) | 1 | One “contradictions/doc alignment” loop before approval. |
| UAT rounds | 2 | Failed once, then passed. |
| UAT defects found | 2 | (1) Inline diff readability expectation, (2) NSG name code-formatting in summary/header contexts. |
| Tool approval prompts (Maintainer interventions) | High | Biggest pain point; especially during UAT. |
| CI runs to green on main | 1 | Post-merge CI succeeded. |
| Release pipeline attempts | 1 | Release succeeded on first run. |
| Net new tests added | 1 | Focused non-snapshot unit test for inline diff `-`/`+` prefixes. |
| Snapshots updated | Yes | Multiple markdown snapshots updated to match formatting changes. |
| Docs added/updated | Yes | Spec/test plan/tasks/style guide/UAT report/retrospective. |

## What Went Well

- End-to-end verification was strong: `dotnet test`, Docker build, demo regeneration, and markdownlint were consistently used.
- UAT was run in the actual target environments (GitHub + Azure DevOps PR comments), which caught real rendering problems.
- Capturing findings in a durable doc (UAT report) made it easy to coordinate follow-up work.
- The follow-up fix (“inline diffs also show `-`/`+`”) was pragmatic and improved degraded-mode readability.

## What Didn’t Go Well

- Documentation drifted temporarily:
  - The spec/plan/tasks disagreed about NSG header formatting.
  - Inline diff examples in the spec didn’t match post-UAT behavior.
- UAT process had a preventable slip: the minimal artifact was posted first, requiring re-posting the correct comprehensive demo.
- Merge method expectation (“rebase and merge”) wasn’t enforced early, leading to a last-minute correction.
- Tool-execution friction was highest during **UAT**, requiring many manual approvals for terminal commands.
- Role boundary confusion: the Architect created/edited tasks that should be owned by the Task Planner, which muddied handoffs.
- Handoff messaging quality: at least one architect handoff implied implementation could proceed without clarifying the next required agent step.
- Status visibility (top Maintainer pain point): the Developer sometimes stopped without a clear “done vs pending” status update, and progress wasn’t consistently reflected in `tasks.md`, which forced the Maintainer to repeatedly ask for status.
- Guidance adoption gap: a report style guide was created, but the workflow/agent docs were not updated to instruct agents to follow it.
- Release-to-retro handoff: the Release Manager did not explicitly hand off to the Retrospective as a final step.

## Improvement Opportunities

| Issue | Proposed Solution | Action Item |
|------|-------------------|------------|
| Too many manual approvals for terminal command execution (mostly UAT) | Reduce command volume and group commands where possible (single tool batch), prefer file-based tools for read-only actions, and document a recommended “allowlist-friendly” command set. For UAT specifically, route work through a small number of stable wrapper commands (e.g., one “UAT run” and one “UAT cleanup”) so future sessions can permanently allow a few predictable commands rather than many unique invocations. | Add a `scripts/uat-run.sh` wrapper (GitHub+AzDO create/comment/poll/cleanup in one script) and update the UAT Tester agent guidance to use it by default. |
| Wrong artifact posted during UAT | Add script guardrails: print the artifact path prominently, reject known “simulation/minimal” artifacts unless `--force`, and optionally verify the artifact contains the expected report header. | Update scripts/uat-github.sh and scripts/uat-azdo.sh to enforce safer defaults. |
| Spec/tasks/test-plan inconsistency | Add a lightweight “doc consistency checklist” step before Code Review is marked Approved: verify that tasks + test-plan + spec agree on key UX rules. | Update docs/agents.md (and/or Code Reviewer checklist) to include a “doc alignment pass” gate. |
| Platform-specific markdown rendering expectations | Standardize UAT inputs: always use the comprehensive demo artifact, and explicitly generate per-platform variants only when needed (and label them clearly). | Update UAT Tester agent guidance to prefer one canonical artifact, with explicit naming for variants. |
| Merge method mismatch late in the process | Encode merge policy in Release Manager agent instructions and PR description template. | Update Release Manager agent docs to default to “rebase and merge” unless Maintainer specifies otherwise. |
| Hard-to-measure workflow improvements | Track a few simple metrics per feature (UAT rounds, time-to-green, number of doc fixes after review). | Standardize a “Standard Metrics” section in every retrospective and add it to the workflow docs/template (Workflow Engineer follow-up). |
| Architect creating Task Planner deliverables | Clarify deliverable ownership: Architect produces architecture + ADRs only; Task Planner owns `tasks.md`. Add a checklist item to prevent cross-role output. | Update Architect agent and Task Planner agent definitions/checklists to enforce ownership boundaries. |
| Unclear/incorrect handoff messages | Standardize handoff messages to include: (a) what’s done, (b) what’s next, (c) whether implementation is blocked, (d) which artifact to read. | Add a “handoff template” section to agent docs and require it in final messages. |
| Developer status updates missing | Require an explicit end-of-turn status every time: `Status: Done/Blocked/In progress` + “What changed / What’s next / What I need from Maintainer”. Also require updating `tasks.md` checkboxes (or equivalent) whenever a task is completed. | Update Developer agent instructions to always close with the status template and keep `tasks.md` in sync. |
| Style guide not wired into workflow | Treat style guides as “policy docs”: once added, they must be referenced by agent instructions and/or `docs/agents.md` so future work uses them automatically. | Update agent docs to reference and require adherence to the report style guide. |
| UAT expectations unclear/incomplete | Make UAT a first-class test phase: (1) Test plan must include UAT scenarios; (2) UAT PR description must embed the UAT checklist; (3) UAT must always publish a UAT results report; (4) UAT must never change code/docs; (5) if issues found, UAT hands off to Developer with a report. | Update Quality Engineer and UAT Tester agent definitions to enforce these rules and artifacts. |
| UAT branch naming collisions | Standardize a high-uniqueness naming scheme, e.g., `<original-branch>-uat-<UTC timestamp>` (and keep `-vN` as a fallback). | Update UAT scripts to generate unique branch names by default and print them in the output. |
| Retrospectives miss consistent, reproducible metrics | Provide the Retrospective agent a short “metrics recipe” with examples: how to compute lead time from git reflog / tags and how to estimate review/UAT loops from chat history + docs. | Update the Retrospective agent instructions to include a metrics section and a recommended method per metric (including fallbacks when branch history is squashed/rebased). |

## Progress Update (As of 2025-12-26)

This section tracks follow-up implementation progress from this retrospective.

| Item | Status | PRs / Notes |
|---|---|---|
| Reduce manual approvals via stable wrappers | ✅ Done | GitHub PR wrapper ([#87](https://github.com/oocx/tfplan2md/pull/87)); Azure DevOps PR wrapper ([#88](https://github.com/oocx/tfplan2md/pull/88)); AzDO abandon cleanup ([#89](https://github.com/oocx/tfplan2md/pull/89)); end-to-end UAT wrapper ([#95](https://github.com/oocx/tfplan2md/pull/95)); GitHub tools preferred over gh CLI ([#108](https://github.com/oocx/tfplan2md/pull/108), [#109](https://github.com/oocx/tfplan2md/pull/109)); demo generation + snapshot update scripts ([#117](https://github.com/oocx/tfplan2md/pull/117)). |
| Make PR feedback polling less brittle | ✅ Done | UAT PR watch scripts/skills added ([#90](https://github.com/oocx/tfplan2md/pull/90)); GitHub UAT poll hardened to use JSON queries and filter agent comments ([#92](https://github.com/oocx/tfplan2md/pull/92)); GitHub tools preferred for PR inspection ([#108](https://github.com/oocx/tfplan2md/pull/108), [#109](https://github.com/oocx/tfplan2md/pull/109)). |
| Reduce Maintainer “Allow” uncertainty | ✅ Done | Workflow now requires agents to post the **exact PR title + description** in chat (using the standard template) before running PR creation commands. This supersedes the earlier wrapper-based `preview` approach (see [#93](https://github.com/oocx/tfplan2md/pull/93) and [#96](https://github.com/oocx/tfplan2md/pull/96) for history). |
| Skill authoring guidance (approval minimization) | ✅ Done | Added explicit guidance to prefer stable wrapper commands to reduce approvals ([#86](https://github.com/oocx/tfplan2md/pull/86)). |
| UAT run/simulate skills foundation | ✅ Done | Initial UAT-related skills and scripts registered in workflow docs ([#85](https://github.com/oocx/tfplan2md/pull/85)). |
| Standard metrics section in retrospectives | ✅ Done | Added/standardized metrics guidance in workflow docs/retrospective artifacts ([#84](https://github.com/oocx/tfplan2md/pull/84)). |
| Guardrails against wrong UAT artifact | ✅ Done | Added guardrails in UAT scripts and updated UAT guidance to prefer the UAT wrapper ([#95](https://github.com/oocx/tfplan2md/pull/95)). Filename-based canonical artifact enforcement implemented in `scripts/uat-helpers.sh` and tests added. |
| Doc alignment gate in Code Review | ✅ Done | Added documentation alignment checklist gate to Code Reviewer ([#113](https://github.com/oocx/tfplan2md/pull/113)). |
| Role boundaries + handoff/status templates | ✅ Done | Enforced role boundaries (Architect/Task Planner), added handoff template, added Developer status template ([#114](https://github.com/oocx/tfplan2md/pull/114)). |
| Wire report style guide into agents | ✅ Done | Added report style guide references to Requirements Engineer, Developer, Code Reviewer, Technical Writer, UAT Tester ([#112](https://github.com/oocx/tfplan2md/pull/112)). |
| Release-to-retro handoff consistency | ✅ Done | Added Release Manager → Retrospective handoff button ([#111](https://github.com/oocx/tfplan2md/pull/111)). |

## Draft Notes

- Initial UAT failed due to artifact mix-up and rendering mismatches between platforms.
- Documentation inconsistencies (spec vs tasks/test plan) caused review friction and rework.
- Merge method expectation required correction late.
- Too many manual approvals were required for terminal command execution; **UAT was the biggest offender** and many commands could not be permanently allowed.
- Maintainer observations: Architect created tasks (should be Task Planner), architect handoff wording was off, developer status updates were sometimes unclear, style guide wasn’t wired into workflow docs, RM attempted squash merge before being corrected, and RM → Retrospective handoff was missing.
- Maintainer’s biggest Developer-related annoyance: missing/unclear status reporting, requiring repeated follow-ups.
