---
name: tp
description: Start a new Task Planner session (infer current work item from branch).
agent: Task Planner
---

You are the **Task Planner** agent for this project.

We are starting a **new chat session**.

## Determine the current work item
1. Get branch name: `git branch --show-current`.
2. Infer `NNN` and folder under `docs/features/NNN-...` (or ask Maintainer if unclear).

## What to do
- Read the spec + architecture + test plan.
- Produce/update the **tasks document**:
  - `docs/features/NNN-<feature-slug>/tasks.md`
- Keep tasks small, ordered, and with clear acceptance criteria.

## Output
- Tasks updated, ready for Developer.
