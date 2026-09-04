---
name: pre-pr-checklist
description: Minimum-requirements checklist for any change — code or docs-only. Run this before every PR creation or push to avoid CI failures on the first attempt.
---

# Pre-PR Checklist

## Purpose

Ensure every change — including simple, single-agent tasks — satisfies the repository's minimum requirements before a PR is created. This avoids predictable CI failures (release-notes guardrail, snapshot guardrail, formatting, coverage thresholds).

## When to Use

**Mandatory** before any `report_progress` call (coding agents) or `scripts/pr-github.sh create` call (local agents) that introduces changes to the repository.

## Step 0 — Determine Your Change Category

```bash
scripts/git-diff.sh --name-only origin/main...HEAD
```

**Category A — Docs/agent-tooling only** (all changed files are under `docs/` *except* `docs/workflow.md` and `docs/spec.md`, `.github/` or `.agents/` (any subdirectory or file), `website/`, `website.old/`, `assets/`, `tests/`, `scripts/`, or `src/tests/shell/`):
→ Skip to [Step 6 (Commit hygiene)](#step-6--commit-hygiene). Build/test/work-item checks are not required.

**Category B — Code/tooling** (any file in `src/` (except `src/tests/shell/`), `examples/`, `docs/workflow.md`, `docs/spec.md`, `README.md`, or `CONTRIBUTING.md`):
→ Complete all steps below.

---

## Step 1 — Work Item Folder

Every code/tooling PR **must** be anchored to a work item folder. If you are doing a simple task without a full workflow, create a minimal one now.

### Determine the folder

| Branch prefix | Folder prefix |
|---|---|
| `feature/<NNN>-...` | `docs/features/<NNN>-.../` |
| `fix/<NNN>-...` | `docs/issues/<NNN>-.../` |
| `workflow/<NNN>-...` | `docs/workflow/<NNN>-.../` |

If the branch does not match any prefix, use `docs/workflow/` (most internal/tooling changes fall here). Use the `next-issue-number` skill to reserve an issue number if you don't already have one.

### Create minimal required files (if missing)

**`docs/<type>/<NNN>-<slug>/release-notes.md`** — describe the change in one or two sentences. No screenshots required for simple changes:

```markdown
## <Short title>

<One or two sentences describing what changed and why.>
```

**`docs/<type>/<NNN>-<slug>/work-protocol.md`** — log your agent entry (required by the validate-work-protocol hook):

```markdown
# Work Protocol: <Short title>

**Work Item:** `docs/<type>/<NNN>-<slug>/`
**Branch:** `<branch-name>`
**Workflow Type:** <Feature | Bug Fix | Workflow>
**Created:** <YYYY-MM-DD>

## Agent Work Log

### <Your Agent Name>
- **Date:** <YYYY-MM-DD>
- **Summary:** <What you did>
- **Artifacts Produced:** <Files created or modified>
- **Problems Encountered:** None
```

> **Note:** For simple single-agent changes the validate-work-protocol hook only blocks `report_progress` *after* the trigger agent (Code Reviewer for features/bugs; Workflow Engineer for workflow items) has logged an entry. So for single-agent tasks the hook will not block you — but the **release-notes guardrail** (`validate-release-notes.sh`) will. Both `release-notes.md` and `work-protocol.md` must be present.

---

## Step 2 — Code Formatting

```bash
dotnet format src/tfplan2md.slnx --verify-no-changes --verbosity diagnostic
```

If formatting errors are found, auto-fix and re-check:

```bash
dotnet format src/tfplan2md.slnx
dotnet format src/tfplan2md.slnx --verify-no-changes
```

---

## Step 3 — Build

```bash
dotnet build src/tfplan2md.slnx --configuration Release --no-incremental
```

All errors must be resolved before proceeding.

---

## Step 4 — Tests and Coverage

Use the `run-dotnet-tests` skill. The project has mandatory coverage thresholds (84.48 % line, 72.80 % branch). New code must be tested.

```bash
scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx --configuration Release
```

### Snapshot changes

If any file under `src/tests/Oocx.TfPlan2Md.Tests/TestData/Snapshots/` changed, use the `update-test-snapshots` skill and include `SNAPSHOT_UPDATE_OK` in at least one commit message:

```bash
git commit --amend -m "$(git log -1 --format=%B)

SNAPSHOT_UPDATE_OK"
```

---

## Step 5 — Demo Artifact Regeneration

Required when any change could affect markdown rendering output:

```bash
node -e "require('child_process').execSync('dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- examples/comprehensive-demo/plan.json --principals examples/comprehensive-demo/demo-principals.json --output artifacts/comprehensive-demo.md', {stdio: 'inherit'})"
npx markdownlint-cli2 artifacts/comprehensive-demo.md
```

Or more directly:

```bash
dotnet run --project src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj -- examples/comprehensive-demo/plan.json --principals examples/comprehensive-demo/demo-principals.json --output artifacts/comprehensive-demo.md
npx markdownlint-cli2 artifacts/comprehensive-demo.md
```

Skip this step only if the change has no effect on markdown output (e.g., internal refactoring with no observable output change, or pure tooling/docs changes).

---

## Step 6 — Commit Hygiene

Before creating the PR, verify:

- [ ] All commits follow [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`
  - Valid types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
  - For internal-only PRs (docs/scripts/agents only), use `docs:`, `chore:`, `ci:`, `style:`, or `refactor:` — **not** `feat:`, `fix:`, or `perf:` (those trigger version bumps)
- [ ] `CHANGELOG.md` has **not** been edited (it is auto-generated by Versionize in CI)
- [ ] Working tree is clean: `scripts/git-status.sh --short`

---

## Step 7 — Local Guardrail Scripts

Run the same checks that CI will run, locally:

```bash
# Release notes guardrail
scripts/validate-release-notes.sh

# Snapshot integrity guardrail
scripts/validate-snapshot-changes.sh
```

Both scripts default to comparing against `origin/main`. Fix any reported errors before pushing.

---

## Step 8 — PR Title and Description

Before calling `report_progress` or `scripts/pr-github.sh create`, compose the PR title and description using the standard template:

```markdown
## Problem
<Why is this change needed?>

## Change
<What changed?>

## Verification
<How was it validated? List the checks you ran from this checklist.>
```

---

## Quick Reference (Category B)

| Step | Command / Action |
|------|-----------------|
| Work item folder | Create `release-notes.md` + `work-protocol.md` with agent log entry |
| Format | `dotnet format src/tfplan2md.slnx --verify-no-changes` |
| Build | `dotnet build src/tfplan2md.slnx --configuration Release --no-incremental` |
| Tests | `scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx` |
| Snapshots | Use `update-test-snapshots` skill; add `SNAPSHOT_UPDATE_OK` to commit |
| Demo | `dotnet run ... -- examples/comprehensive-demo/plan.json ... --output artifacts/comprehensive-demo.md` |
| Lint demo | `npx markdownlint-cli2 artifacts/comprehensive-demo.md` |
| Guardrails | `scripts/validate-release-notes.sh` + `scripts/validate-snapshot-changes.sh` |
| Commits | Conventional Commits, no `CHANGELOG.md` edits |
