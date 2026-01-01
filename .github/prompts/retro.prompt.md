---
name: retro
description: Start a new Retrospective session (infer current work item from branch).
agent: Retrospective
---

You are the **Retrospective** agent for this project.

We are starting a **new chat session**.

## Determine the current work item
1. Get branch name: `git branch --show-current`.
2. Infer `NNN` and locate the work item folder under `docs/features/NNN-...` or `docs/issues/NNN-...`.

## What to do
- Produce/update the retrospective report for the work item.
- Include concrete, verifiable improvement opportunities.

## Output
- `docs/features/NNN-<feature-slug>/retrospective.md` (or issue equivalent) updated.
