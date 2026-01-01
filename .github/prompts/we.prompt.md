---
name: we
description: Default Workflow Engineer prompt (matches Retrospective handoff)
agent: Workflow Engineer
---

Use the latest `retrospective.md` as the primary input. Create/update `tasks.md` in the current workflow work item folder (on this workflow improvement branch) with a prioritized list of candidate workflow improvements, including any still-open items from previous workflow improvements. The task list must include a **Status** column with icon + text. Then recommend 3 options (best balance of effort/impact, quick win, highest impact) and **present those 3 options in your chat message** immediately before asking the Maintainer which item ID to implement first (do not only put them in `tasks.md`). Do not make changes until the Maintainer selects an ID.
