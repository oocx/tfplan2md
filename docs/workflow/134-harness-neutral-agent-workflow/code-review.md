# Code Review: 125-harness-neutral-agent-workflow

**Reviewer:** codex (gpt-5.6-sol) · **Base:** `origin/main` · **Date:** 2026-09-04

## Summary

The harness-neutral migration is not release-ready. Static review found mandatory UAT paths that can be skipped or invalidated, contradictory rework instructions, incomplete attempt-cap enforcement, and stale Copilot-specific behavior. The branch also lacks verifiable CI evidence.

## Verification Results

The read-only review did not run the test suite. work-protocol.md records 54 workflow-driver assertions but includes no raw results and no results for adapter synchronization, agent validation, website verification, or diagram regeneration. GitHub was unreachable, HEAD is not present on a fetched remote branch, and CI/coverage status could not be verified. All 15 branch commits use the non-version-bumping workflow: type; CHANGELOG.md and snapshots are untouched. git diff --check fails on trailing whitespace.

## Specification Compliance

Decision 1 (drop Copilot): old agent/prompt corpus is deleted, but active UAT scripts and generated comments retain broken Copilot-specific behavior; not compliant and untested. Decision 2 (Claude drives the workflow): implemented by .agents roles, run-workflow, workflow-next, and generated .claude adapters; the recorded driver assertions provide partial evidence, but duplicate rework instructions remain. Decision 3 (Codex reviewer): implemented by scripts/codex-review.sh and the JSON schema; live-review timings are recorded, but wrapper state transitions/fallback have no automated or CI evidence. Decision 4 (canonical .agents plus generated .claude and CI drift check): implemented by sync-agent-config.sh and PR-validation steps, but role deletion is not handled and current CI status is unavailable. Decision 5 (three path-driven gates): implemented for feature/fix paths in workflow.json and driver tests, but website changes are excluded contrary to the criterion and UAT cleanup instructions invalidate the gate. Decision 6 (required developer tools): implemented by agent-doctor.sh and setup-agent-tools.sh; no recorded verification result. Decision 7 (deep Requirements Engineer and Issue Analyst): implemented in role frontmatter/tiers.json and covered by the driver assertion for Issue Analyst. Process checks: required Workflow Engineer protocol entry exists; commits are workflow:; CHANGELOG.md and snapshots are untouched; README.md and docs/workflow.md were updated.

## What I Tried To Break

Checked all workflow types, gate opening/rejection, UAT dispatch and cleanup ordering, repeated rework, stage advancement, Codex fallback/reporting, adapter removal and drift behavior, CI wiring, stale Copilot references, commit policy, snapshots, CHANGELOG.md, generated website assets, and large/generated-file hygiene. The probes exposed UAT bypasses, premature cleanup instructions, double rework routing, an uncapped rejection path, stale generated roles, unpinned diagram tooling, and whitespace errors.

## Issues Found

### Blockers

- **Website changes bypass the mandatory path-based UAT gate** — `scripts/workflow-gate.sh:63`
  docs/workflow.md says UAT is determined solely by changed paths and that website changes always trigger it. This code instead declares UAT inapplicable whenever a workflow type lacks uat-tester; the website workflow has no such stage. Consequently every website work item—and this workflow item’s own user-visible website changes—can reach release without the required Maintainer decision. No test covers a website work item touching website/.
- **The run-uat skill instructs agents to destroy artifacts before the UAT gate** — `.agents/skills/run-uat/SKILL.md:20`
  The skill’s Hard Rules require cleanup and forbid leaving UAT PRs open, while the UAT Tester role requires --create-only, returning with both PRs open so the subsequent Maintainer gate can inspect them. Following the skill closes the artifacts before the gate and recreates a defect that work-protocol.md claims was fixed.
- **Copilot-only UAT machinery remains active despite the explicit removal criterion** — `scripts/uat-github.sh:177`
  The design requires Copilot support and machinery to be removed, and tasks.md marks Copilot-ism removal complete. Active failure handling still directs users to the deleted copilot-setup-steps.yml and a Copilot environment; UAT comments are also branded “Copilot Code Reviewer.” In the new harness these remediation instructions cannot work.

### Majors

- **The driver skill can count every Codex rejection twice** — `.agents/skills/run-workflow/SKILL.md:98`
  The earlier exit-code table correctly says codex-review.sh already routes REWORK to Developer, but this later section instructs the driver to invoke --rework code-reviewer again whenever the reviewer returns REWORK. Following it increments Developer attempts twice per review and can block the workflow after only two failed reviews. The driver tests do not exercise this integration.
- **Gate rejection bypasses the three-attempt cap** — `scripts/wp-append.sh:126`
  The gate-rejection branch increments the target role’s attempt counter but never applies MAX_ATTEMPTS or marks the run blocked. Repeated specification, architecture, or UAT rejection can therefore loop indefinitely despite the documented three-attempt stop. The only cap test exercises the separate --rework branch.
- **Regeneration cannot remove obsolete generated roles** — `scripts/sync-agent-config.sh:96`
  sync_roles creates or overwrites current roles but never clears the generated agents directory. Removing or renaming a canonical role leaves an active stale Claude agent; --check then fails while the prescribed regeneration command cannot repair it. Skills and commands correctly replace their destination trees, but roles lack equivalent handling and no removal test exists.
- **Required CI evidence is absent** — `docs/workflow/134-harness-neutral-agent-workflow/work-protocol.md:208`
  The protocol records only “54 assertions” without raw output and does not record the other relevant validation results. GitHub CI could not be inspected, and no fetched remote branch contains HEAD. Under the reviewer policy, an unverified branch cannot be treated as release-ready.
- **Diagram drift validation depends on an unpinned latest Mermaid CLI** — `scripts/render-workflow-diagram.py:115`
  When mmdc is unavailable, CI downloads @mermaid-js/mermaid-cli without a version. A future upstream release can change SVG markup or layout and make --check fail—or break the regex parser—without any repository change. Pin the tool and add focused parser/render tests.

### Minors

- **The branch fails git diff --check** — `.claude/skills/arc42-documentation/templates/arc42-template.md:3`
  Generated adapter files and the issue template contain extensive trailing whitespace. The first reported error is here; the same problem appears throughout generated skill assets.

## Decision

`VERDICT: REWORK`
