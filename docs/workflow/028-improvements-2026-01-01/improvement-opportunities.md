# Workflow Improvement Opportunities (028)

**Date:** 2026-01-01

## Context

Feature 027 trialed a per-agent chat workflow and produced a retrospective with a few concrete friction points:
- Manual handoffs between agents (prompt creation)
- Developer progress visibility during long sessions
- Per-agent workflow guidance not being explicit enough in the main workflow documentation

This document lists candidate improvement opportunities to discuss and implement **one by one**.

## Principles

- Discuss and agree on scope before implementing.
- Implement one improvement per PR/branch topic.
- Prefer small, reversible changes.
- Prefer existing patterns (skills + scripts + docs) over inventing new ones.

## Candidate Improvement Opportunities

### 1) Reduce manual handoff friction

**Problem:** When using separate chat sessions per agent, the Maintainer manually assembles the next prompt and gathers links/paths.

**Possible approaches (choose one):**
- **A. New agent skill: `handoff`**
  - Adds a skill under `.github/skills/handoff/`.
  - Provides a command/script to generate a standardized next-agent prompt.
  - Pros: repeatable, reduces cognitive load, works well with per-agent workflow.
  - Cons: introduces a new skill to maintain; must be careful not to over-prescribe.
- **B. Document-only guidance**
  - Add a short “handoff prompt template” section to `docs/agents.md`.
  - Pros: no code/scripts, minimal maintenance.
  - Cons: still manual copying; inconsistent prompts likely.
- **C. Improve handoff buttons in agent frontmatter**
  - Add/standardize `handoffs:` prompts across agents.
  - Pros: stays inside VS Code UI; no scripts.
  - Cons: prompts tend to become stale; doesn’t list per-work-item artifacts dynamically.

**Definition of Done (for whichever approach we pick):**
- Maintainer can create the next agent session prompt in <30 seconds.
- The prompt consistently references the work item folder and the key artifacts.

---

### 2) Improve developer progress visibility (without adding noise)

**Problem:** During long dev sessions, it can be unclear what is done vs remaining.

**Possible approaches (choose one):**
- **A. Tighten Developer agent instructions**
  - Add explicit guidance: “post a short progress update after each major phase”.
  - Pros: low effort, directly addresses feedback.
  - Cons: depends on compliance; may be redundant with existing status template.
- **B. Require periodic todo list updates**
  - Make it explicit that the `todo` tool should be updated whenever phase changes.
  - Pros: structured and visible.
  - Cons: overhead; might not fit very small tasks.
- **C. Add a lightweight ‘progress update’ section**
  - A simple two-line update cadence guideline in the Developer agent.
  - Pros: predictable; low verbosity.
  - Cons: still subjective; may be ignored.

**Definition of Done:**
- Maintainer can see (at any time) what step is in progress and what’s next.

---

### 3) Formalize the per-agent chat workflow

**Problem:** The workflow allows per-agent chats, but the “how” (exports, storage, naming) isn’t explicit.

**Possible approaches (choose one):**
- **A. Add a “Per-Agent Chat Workflow” section to `docs/agents.md`**
  - Include recommended export naming and storage location.
  - Pros: makes the process official and repeatable.
  - Cons: documentation maintenance.
- **B. Add as a skill (documentation-only skill)**
  - A skill that describes exact steps for exporting and storing chats.
  - Pros: discoverable and consistent.
  - Cons: might be overkill vs a short doc section.

**Definition of Done:**
- A new contributor can follow the instructions without guessing where to put exports.

---

## Out of Scope (for 028)

- Changing CI/CD pipelines.
- Changing model assignments across multiple agents.
- Large refactors of the entire agent system.

## Decision Log

Record decisions here as we agree on them.

- 2026-01-01: For improvement (1) “Reduce manual handoff friction”, use **VS Code prompt files** in `.github/prompts/` (short names like `/re`, `/dev`) so the Maintainer can start new chat sessions without copying prompt text from previous sessions.
