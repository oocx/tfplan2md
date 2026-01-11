## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Web Designer: add strict “Definition of Done” (DoD) + evidence | Maintainer feedback (2026-01-11) | ✅ Done | Agent frequently claims completion without working output. A DoD reduces false positives and tightens iteration loops. | High | Low | Low | Implemented in `.github/agents/web-designer.agent.md` and aligned in `docs/agents.md` (includes style guide validation/updates, and shared-component propagation). |
| 2 | Add `scripts/website-lint.sh` (HTML/CSS/JS) + configs | Maintainer feedback (2026-01-11) | ✅ Done | Linting catches broken/incomplete UI changes early and provides objective failure signals for the agent to fix before claiming done. | High | Med | Med | Added `scripts/website-lint.sh` + `website/.htmlhintrc` + `website/.stylelintrc.json` + `website/eslint.config.js`. Calibrated `--all` to ignore prototype/icon HTML and fixed remaining baseline HTML issues. |
| 3 | Add `scripts/website-verify.sh` wrapper (lint + basic checks) | prior-work + Maintainer feedback (2026-01-11) | ✅ Done | One command reduces tool-choice ambiguity and makes verification habitual for the agent. | Med | Low | Low | Added `scripts/website-verify.sh` which runs `scripts/website-lint.sh` plus a lightweight static link/asset existence check for HTML. |
| 4 | Web Designer: switch model away from Claude Sonnet 4.5 | docs/ai-model-reference.md (Instruction Following + Coding) | ⬜ Not started | Claude Sonnet 4.5 scores poorly for Coding and Instruction Following; aligns with observed “incomplete/broken implementation” behavior. | High | Low | Med | Candidates: `GPT-5.1-Codex-Max` (best coding) or `Gemini 3 Flash (Preview)` (best instruction following/cost). |
| 5 | Web Designer: require Chrome DevTools console “clean” check before done | prior-work (#034) + Maintainer feedback (2026-01-11) | ✅ Done | Many web bugs show up as console errors or missing assets; DevTools provides fast validation signal. | Med | Low | Low | Added to Web Designer DoD: must check DevTools and report “no console errors” (or list and fix). |
| 6 | Extend `website-quality-check` skill to include lint + console checks | Maintainer feedback (2026-01-11) | ✅ Done | Consolidates quality gates in a reusable checklist, reducing drift across agent updates. | Med | Low | Low | Skill now mandates `scripts/website-lint.sh` for any website HTML/CSS/JS changes and includes a DevTools console sanity check when available. |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1** — Tightens “done” definition immediately with minimal repo/tooling changes.
- **Option 2 (Quick win):** **5** — Low-effort guardrail that catches many breakages early.
- **Option 3 (Highest impact):** **2** — Introduces objective quality gates (linters) that directly address incomplete/broken outputs.

## Decision
Which item should I implement first? (Reply with the Option number, or reply with "work on task <task id>")
