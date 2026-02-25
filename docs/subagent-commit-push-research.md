# Subagent Commit/Push Research

**Date:** 2026-02-25  
**Author:** GitHub Copilot coding agent (oocx/tfplan2md experiment)  
**Branch:** `copilot/experiment-subagent-commit-push`

---

## Summary

This document reports the findings from online research and live experiments on how
GitHub Copilot coding agent **subagents** (spawned via the `task` tool) should commit
and push changes so that the parent agent can access them.

**TL;DR:**

- **`report_progress` is NOT available to subagents.** Subagents must use `git commit` instead.
- **`git push` fails with HTTP 403** in subagent context (no credentials).
- **Subagents share the same filesystem** as the parent agent — committed and uncommitted
  changes are all visible to the parent.
- **The parent agent's `report_progress` pushes ALL local commits** (including subagent
  commits) to the remote branch.
- **The correct pattern** is: subagent commits with `git commit` → parent pushes with
  `report_progress`.

---

## Background

When the workflow orchestrator (or any primary coding agent) uses the `task` tool to
spawn subagents, the subagents run in an **isolated context window** but on the
**same filesystem and git repository**. The question is: how should subagents persist
their file changes so they are available to the parent?

The existing `coding-agent-workflow` skill (`.github/skills/coding-agent-workflow/SKILL.md`)
states:

> **CRITICAL — Delegated agents**: When running as a delegated agent (via `task` tool),
> you **MUST still call `report_progress`**.

This turns out to be **incorrect** — subagents cannot call `report_progress`.

---

## Online Research Findings

GitHub's official documentation confirms that the coding agent runs inside an
**ephemeral GitHub Actions environment**. Key points:

- The agent has its own development environment powered by GitHub Actions.
- It can explore code, make changes, execute tests, and commit files.
- The `report_progress` tool is a GitHub-provided built-in that handles `git add`,
  `git commit`, and `git push` using the Actions `GITHUB_TOKEN`.
- Manual `git push` fails because the environment does not expose personal git
  credentials — only the Actions token used by `report_progress` can push.
- **The `report_progress` tool exists only in the primary agent's tool context.** It
  is NOT passed down to subagents spawned via `task`.

---

## Experiments

All experiments were run on branch `copilot/experiment-subagent-commit-push` in the
`oocx/tfplan2md` repository. Each subagent was the `developer-coding-agent` type.

### Experiment 1 — Subagent tries to call `report_progress`

**Instruction:** Create a file and call `report_progress`.

**Result:**
- `report_progress` was **NOT available** in the subagent's tool list.
- The subagent fell back to `git add -f` + `git commit`.
- The commit (`5ca2de5`) was created locally and was visible to the parent.
- The parent's subsequent `report_progress` call pushed this commit to the remote.

**Conclusion:** Subagents cannot call `report_progress`. Instructions telling them to
do so are misleading. Smart subagents fall back to `git commit`, but this is implicit
and fragile.

---

### Experiment 2 — Subagent uses `git commit` (no push)

**Instruction:** Create a file and commit with `git add` + `git commit`. Then try
`git push` and report the error.

**Result:**
- `git commit` succeeded: commit `8de8fb9` created locally.
- `git push` failed with:
  ```
  remote: Permission to oocx/tfplan2md.git denied to oocx.
  fatal: unable to access 'https://github.com/oocx/tfplan2md/': 
  The requested URL returned error: 403
  ```
- The local commit was visible to the parent.

**Conclusion:** `git commit` is the correct approach for subagents. `git push` is not
possible due to missing credentials.

---

### Experiment 3 — Subagent creates `.gitignore`d file without committing

**Instruction:** Create a file inside `.tmp/` (which is gitignored) without committing.

**Result:**
- File was created on disk but was completely invisible to git (`git status` showed
  "nothing to commit, working tree clean").
- The parent's `report_progress` did **NOT** pick up this file (correctly — it respects
  `.gitignore`).

**Conclusion:** Subagents must not rely on leaving gitignored files as a communication
mechanism. Any changes intended for the PR must be either committed or placed in
non-ignored paths.

---

### Experiment 4 — Subagent creates tracked file without committing

**Instruction:** Create a file in `docs/` (not gitignored) without committing.

**Result:**
- The file appeared as an **untracked file** in `git status` on the parent.
- When the parent called `report_progress`, it ran `git add .` which **picked up the
  untracked file** and committed it.

**Conclusion:** Leaving untracked files in tracked directories works as a fallback, but
it is suboptimal because:
1. The parent's `report_progress` commits all untracked files together in a single
   commit, losing the subagent's commit message context.
2. It is implicit and error-prone (e.g., unintended files could be committed).

---

### Experiment 5 — Parent calls `report_progress` after subagent commits

**Instruction:** Parent calls `report_progress` after subagents have made local commits
(Experiments 1 and 2) and left uncommitted changes (Experiment 4).

**Result:**
- `report_progress` ran `git add .` (picking up the untracked file from Experiment 4).
- `git commit` created commit `4e68e13`.
- `git push` pushed **ALL local commits** (Experiments 1, 2, and 5) to the remote branch.
- GitHub confirmed all commits are now on the remote: `360de99..4e68e13`.

**Conclusion:** The parent's `report_progress` pushes the entire range of local commits
accumulated since the last push. This means subagent commits DO make it to the remote
as long as the parent eventually calls `report_progress`.

---

### Experiment 6 — Subagent stages file (git add) without committing

**Instruction:** Create a file and run `git add` to stage it, but do NOT commit.

**Result:**
- The staged file appeared under "Changes to be committed" in `git status` on the parent.
- The parent's `report_progress` would commit and push it (since `git add .` preserves
  already-staged changes before running `git commit`).

**Conclusion:** Staged (but uncommitted) changes from subagents persist to the parent
context and are picked up by `report_progress`. However, committing explicitly is still
the better practice.

---

## Key Findings

| Finding | Confirmed? |
|---|---|
| `report_progress` is NOT available to subagents | ✅ Yes |
| Subagents can commit with `git commit` | ✅ Yes |
| `git push` fails for subagents (HTTP 403) | ✅ Yes |
| Subagent commits ARE visible to the parent (local git history) | ✅ Yes |
| Parent's `report_progress` pushes ALL accumulated local commits | ✅ Yes |
| Uncommitted tracked files from subagent are visible to parent | ✅ Yes |
| Parent's `report_progress` picks up untracked files via `git add .` | ✅ Yes |
| Gitignored files from subagent are invisible to git | ✅ Yes |
| All subagents share the same filesystem as the parent | ✅ Yes |

---

## Failure Modes (Why Subagents "Often Fail to Commit Back")

Based on the experiments, there are two main failure modes:

### Failure Mode 1: Subagent tries to call `report_progress` and gives up

The current documentation (`coding-agent-workflow` skill) tells subagents:
> "You MUST call `report_progress` before completing."

If a subagent follows this instruction literally and `report_progress` is not available:
- The subagent may fail to commit anything.
- The changes exist only in memory (never written to disk, or written but not committed).
- The parent's `report_progress` cannot rescue uncommitted changes if no files were
  written to disk.

### Failure Mode 2: Subagent creates gitignored files without committing

If a subagent writes files to `.tmp/` or other gitignored paths and doesn't commit,
those files are invisible to git. The parent's `report_progress` will not include them.

---

## Correct Pattern for Subagents

```
Subagent (developer-coding-agent, etc.):
  1. Make file changes using edit/create tools
  2. Run: git add <changed files>
  3. Run: git commit -m "type: description of changes"
  4. Return results to parent

Parent agent (workflow orchestrator, etc.):
  1. Call report_progress(commitMessage=..., prDescription=...)
     → This pushes ALL accumulated local commits (parent's + all subagent commits)
```

**Key rule**: Subagents commit with `git commit`. The parent pushes with `report_progress`.

---

## Incorrect Pattern (Original Documentation — Now Fixed)

The original `coding-agent-workflow` skill incorrectly stated:
> "You MUST call `report_progress` before completing"

This has been corrected to:
> "You MUST use `git commit` to commit your changes before completing"

---

## Implications for the `coding-agent-workflow` Skill

The skill at `.github/skills/coding-agent-workflow/SKILL.md` has been corrected as follows:

**Was (incorrect):**
> CRITICAL — Delegated agents: When running as a delegated agent (via task tool), you
> MUST still call report_progress. Your commits are added to the orchestrator's local
> branch (not pushed to the remote PR directly). The orchestrator will push your commits
> using their own report_progress call. If you skip report_progress, your file changes
> remain uncommitted and will be lost.

**Now (correct):**
> CRITICAL — Delegated agents: When running as a delegated agent (via task tool),
> report_progress is NOT available. You MUST use git commit to commit your changes.
> Use: `git add <changed files> && git commit -m "type: description"`.
> Your commits are added to the orchestrator's local branch (not pushed to the remote
> PR directly). The orchestrator will push them using their own report_progress call.
> If you skip git commit, your file changes remain uncommitted and will be lost.

---

## Recommendations

1. ✅ **Update `coding-agent-workflow` skill** to correctly state that subagents must use
   `git commit` (not `report_progress`). — **Done.**

2. **Update agent instructions** (e.g., `developer-coding-agent.agent.md`) to include
   explicit `git commit` steps.

3. ✅ **Update the custom instructions** in `.github/copilot-instructions.md` to clarify
   the distinction between `report_progress` (parent only) and `git commit` (subagents). — **Done.**

4. **Consider a note** that `report_progress` in the parent will pick up uncommitted
   tracked files via `git add .` — this can act as a safety net but should not be
   relied upon as the primary mechanism.

---

## Validation (10-Run Reliability Test)

After the skill was updated with the correct `git commit` instructions, a second round of 10
experiments was run to confirm that the new instructions are followed reliably across multiple
agent types and commit scenarios.

### Protocol

Each subagent was instructed to:
1. Read the updated `coding-agent-workflow` SKILL.md and confirm the commit procedure
2. Perform a real task (creating/editing files)
3. Commit using `git add <files> && git commit -m "..."` (the correct subagent method)
4. Report whether `report_progress` appeared in their tool list
5. Show `git log --oneline -3`

After all 10 runs, the parent called `report_progress` once to push all 11+ accumulated
commits (including one nested sub-subagent commit from run 10).

### Results

| Run | Agent Type | Variant | Commit SHA | report_progress available? | Success? |
|-----|-----------|---------|-----------|---------------------------|---------|
| 01 | developer-coding-agent | basic create | cdfe385 | No | ✅ |
| 02 | technical-writer-coding-agent | basic create | 7e18aea | No | ✅ |
| 03 | code-reviewer-coding-agent | basic create | 828896e | No | ✅ |
| 04 | developer-coding-agent | multi-file (edit + create) | c1eae75 | No | ✅ |
| 05 | general-purpose | general purpose agent type | b8d0081 | No | ✅ |
| 06 | developer-coding-agent | multi-step work (read 2 files first) | e231a95 | No | ✅ |
| 07 | task | bash-only agent | 2a94e6c | No | ✅ |
| 08 | workflow-engineer-coding-agent | workflow engineer type | 371ba59 | No | ✅ |
| 09 | architect-coding-agent | multi-directory commit | f6437a2 | No | ✅ |
| 10 (outer) | developer-coding-agent | spawns nested subagent | 30e4e66 | No | ✅ |
| 10 (nested) | developer-coding-agent | nested sub-subagent | 7f5d5e1 | No | ✅ |

**Result: 11/11 commits succeeded (100% reliability)**

All subagents:
- Correctly read the skill and identified `git commit` as the required method
- Confirmed `report_progress` was NOT in their tool list
- Successfully committed their changes using `git add` + `git commit`
- Noted that nested subagents are equally constrained (same tool context rules)

### Key Observations from Validation

1. **Consistent across all agent types**: `developer`, `technical-writer`, `code-reviewer`,
   `general-purpose`, `task`, `workflow-engineer`, and `architect` agents all behaved identically.

2. **Consistent tool context**: Every single subagent confirmed `report_progress` is absent
   from their tool list — there is no ambiguity.

3. **Nested subagents work the same way**: A subagent spawning another subagent (run 10)
   results in the same behaviour — `report_progress` unavailable, `git commit` works.

4. **Multi-file and multi-directory commits work**: Runs 4 and 9 confirmed that committing
   multiple files across directories in a single commit works correctly.

5. **Multi-step work before commit is reliable**: Run 6 confirmed that doing substantial
   preparatory work (reading files, reasoning) before committing does not disrupt the commit.

6. **All commits reach the remote via one `report_progress` call**: The parent's single
   `report_progress` pushed all 11+ accumulated local commits from the 10 runs at once,
   confirming the "accumulate locally, push once" pattern works at scale.

### Conclusion

The updated skill instructions are **100% reliable** across all tested agent types, commit
patterns, and nesting levels. The root cause of the original "work often lost" problem is
confirmed to be the incorrect instruction to call `report_progress` in subagents. With the
corrected `git commit` instruction, all 11 subagent commits succeeded without exception.

---

## Appendix: Environment Details

- **Repository:** oocx/tfplan2md
- **Branch:** copilot/experiment-subagent-commit-push
- **Initial experiment date:** 2026-02-25
- **Validation date:** 2026-02-25
- **Agent types used for subagents:** developer-coding-agent, technical-writer-coding-agent,
  code-reviewer-coding-agent, general-purpose, task, workflow-engineer-coding-agent,
  architect-coding-agent
- **Shared filesystem:** Yes (all agents operate on `/home/runner/work/tfplan2md/tfplan2md`)
- **Git credentials in subagent context:** None (push fails with HTTP 403)
- **report_progress availability in subagent:** None (tool not in subagent's tool list) —
  confirmed independently by all 11 subagent invocations
