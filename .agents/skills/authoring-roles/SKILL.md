---
name: authoring-roles
description: How to write and validate a role definition - required structure, the tier system, and the line budget.
---

# Authoring Roles

Role definitions live in `.agents/roles/*.md` and are generated into `.claude/agents/`
by `scripts/sync-agent-config.sh`. **Edit the canonical files; never the generated ones.**

## Frontmatter

```yaml
---
name: Code Reviewer          # display name, used in work-protocol entries
description: ...             # <= 100 chars, one line
tier: deep                   # deep | standard | cheap
---
```

That is the whole schema. A role must not declare:

- `model` — the tier resolves to a model through `.agents/tiers.json`. Naming a model in
  a role file puts the mapping in thirteen places instead of one, and the generator
  rejects it.
- `tools` — roles run with the harness default toolset. Per-role tool lists were the
  single largest source of drift in the previous corpus, because a tool ID renamed
  upstream broke a role silently.
- `handoffs` or `target` — sequencing lives in `state.json` and `.agents/workflow.json`.

## Structure

```markdown
# Role Name

Read [AGENTS.md](../../AGENTS.md) and the `agent-runtime` skill first.

## Goal            <!-- one sentence: the outcome, not the process -->
## Boundaries      <!-- Always / Never; Ask first only where it is real -->
## Inputs          <!-- what to read, and in what order -->
## Steps           <!-- numbered, with exact commands -->
## Output          <!-- artifact paths and their shape -->
## Definition of Done
```

`scripts/validate-agents.py` enforces `## Goal`, `## Boundaries`,
`## Definition of Done`, and a reference to AGENTS.md.

## The line budget

**160 lines, enforced.** A role that outgrows it is doing more than one job, or is
repeating something that belongs in `AGENTS.md` (project-wide rules) or the
`agent-runtime` skill (how roles operate). The previous corpus averaged 378 lines per
agent, and almost all of the excess was duplicated boilerplate.

Write only what is true of *this* role and no other.

## Choosing a tier

| Tier | Use for |
|------|---------|
| deep | Work whose errors propagate — requirements, architecture, review, the workflow itself |
| standard | Work constrained by an approved spec, where mistakes surface in review or CI |
| cheap | Transcription and mechanical work against a fixed input |

Cost is a consideration, but the deciding question is what an error costs to unwind.
A role escalates one tier automatically on rework, so the tier is the *starting* point,
not a ceiling.

## Before committing

```bash
scripts/sync-agent-config.sh     # regenerate .claude/
scripts/validate-agents.py       # structure, tier, budget, links, adapter drift
```
