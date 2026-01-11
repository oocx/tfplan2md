## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Remove duplicate DoD + reconcile contradictions | maintainer feedback | ✅ Done | Two DoD sections + overlapping rules increases confusion and token waste | High | Med | Low | Kept strict “Definition of Done (CRITICAL)”; removed redundant DoD + outdated command guidance |
| 2 | Consolidate “Phases” vs “Workflow” guidance | maintainer feedback | ✅ Done | Two different process descriptions cause ambiguity and slow the agent | High | Med | Med | Replaced with a single concise “Workflow (VS Code)” section aligned to the strict DoD |
| 3 | Replace legacy validation commands with `scripts/website-verify.sh` | maintainer feedback | ✅ Done | The agent still mentions `html5validator` / `linkchecker`; the repo now has a wrapper script | Med | Low | Low | Legacy commands removed; verification is documented via `scripts/website-verify.sh` in the strict DoD + workflow |
| 4 | Extract accessibility guidance into an on-demand skill | maintainer feedback | ✅ Done | WCAG details are important but don’t need to be in the always-loaded agent prompt | High | Med | Med | Created skill `website-accessibility-check` and removed the detailed WCAG list from the agent prompt |
| 5 | Prune/triage “Context to Read” list | maintainer feedback | ✅ Done | Too many “must read” files increases context cost; many are situational | High | Low | Low | Split into “Always read” vs “Read when relevant” and removed duplicate “Website Memory Docs” section |
| 6 | Move page structure + audience strategy out of agent | maintainer feedback | ✅ Done | Agent repeats content that already belongs in `website/_memory/*`; duplication risks drift | Med | Med | Low | Removed page structure + strategy details from the agent prompt; agent now points to `website/_memory/site-structure.md` and `website/_memory/content-strategy.md` |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1 + 2 + 5** — remove duplication, unify workflow guidance, and cut low-signal context.
- **Option 2 (Quick win):** **3** — delete/replace the outdated command section; point to `scripts/website-verify.sh`.
- **Option 3 (Highest impact):** **4 + 6** — move heavy guidance into skills/memory docs to shrink the always-loaded agent prompt.

## Decision
Which item should I implement first? (Reply with the Option number, or reply with "work on task <task id>")
