---
name: Retrospective
description: Analyse a completed cycle and identify concrete workflow improvements
tier: standard
---

# Retrospective

Read [AGENTS.md](../../AGENTS.md) and the `agent-runtime` skill first.

## Goal

Find what actually went wrong in this cycle and turn it into changes someone can make.

## Boundaries

**Always:** be evidence-based. Every finding cites something — a work-protocol entry, a
commit, a CI result, a rework loop in `state.json`.

**Never** write a retrospective that concludes everything went well without having
looked. Never propose an improvement without naming the file that would change and how
you would know it worked.

## Evidence

Read, in this order:

| Source | What it tells you |
|--------|-------------------|
| `work-protocol.md` | Which roles ran, and the problems they logged |
| `state.json` | Rework loops, attempt counts, gates hit, questions raised |
| `git log --oneline origin/main..HEAD` | How often work was redone, and commit hygiene |
| `code-review.md` | What review caught, and what it should have caught earlier |
| CI results | What failed, and how late it failed |

The `attempts` map and `open_questions` in `state.json` are the highest-signal source:
a stage that ran three times, or a question nobody answered, is a workflow defect, not
an accident.

## Steps

1. Gather the evidence above.
2. Cluster findings by theme rather than listing them chronologically.
3. For each improvement opportunity, state the change location and how you would verify
   it worked. An action item without both is a wish.
4. Write the report, commit, append your work-protocol entry.

## Output

`docs/<type>/NNN-<slug>/retrospective.md`:

```markdown
# Retrospective: <work item>

## Summary
## What Went Well          <!-- with evidence -->
## What Didn't             <!-- with evidence -->
## Improvement Opportunities
<!-- each: problem | change location | verification method | priority -->
## Automation Opportunities  <!-- suggested skills or scripts -->
## Checklist
```

## Definition of Done

Report written with evidence-backed findings and actionable items, committed,
work-protocol entry appended.
