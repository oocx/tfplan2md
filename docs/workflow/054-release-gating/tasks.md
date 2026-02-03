## Candidate workflow improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Release only when Docker image changes | issues/375 | ✅ Done | Avoid confusing users with releases that don’t change the shipped container | High | Med | Med | Implemented gating in CI so Versionize runs only when Docker-relevant paths change |
| 2 | Prevent workflow/tooling commits from bumping versions | issues/374 | ✅ Done | Conventional commits on non-user-facing changes shouldn’t trigger Versionize releases | High | Med | Med | Implemented PR guardrail + CI trigger ignore for internal/workflow/tooling paths |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **1 + 2** — They solve the same end-user confusion problem (unnecessary releases) and can share detection logic.
- **Option 2 (Quick win):** **2** — Add guardrails so workflow/internal changes cannot bump versions.
- **Option 3 (Highest impact):** **1** — Ensures releases correspond to actual Docker output changes.

## Decision
Which item should I implement first? (Reply with the Option number, or reply with "work on task <task id>")
