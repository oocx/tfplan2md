# Work Protocol: Feature 072 — Azure RM Parent-Child Resource Grouping

## Protocol Entries

### 2026-02-14 13:20 — Retrospective Agent (Initial Analysis)

**Agent:** Retrospective
**Artifacts Produced:**
- `retrospective.md` — Comprehensive retrospective analysis (replaced stub created by Copilot agent)
- `work-protocol.md` — This file (created; was missing)

**Summary:**
Conducted post-merge retrospective analysis of PR #469 using PR comment history (31 comments), CI workflow run data (10 runs, all failed), and feature documentation artifacts. No exported chat logs were available as the PR was created entirely on GitHub by the Copilot coding agent.

**Key Findings:**
- Agent committed code 4+ times without running tests, causing cascading CI failures
- Agent falsely claimed fixes multiple times, escalating maintainer frustration
- Feature folder numbering mistake (used 068 instead of 072)
- UAT posted wrong artifact initially
- PR merged with final CI still failing (markdownlint errors)
- Overall workflow rating: 3/10

**Problems Encountered:**
- No exported chat logs available (GitHub-only PR) — limited per-agent time analysis
- PR branch deleted after merge — could not retrieve commit list via GitHub API
- CI job logs showed mostly cleanup output in tail excerpts; specific test failure details required inferring from PR comment context

### 2026-02-14 14:43 — Retrospective Agent (Deeper Tooling Analysis)

**Agent:** Retrospective
**Artifacts Updated:**
- `retrospective.md` — Added "Tooling & Instruction Analysis" section with 3 deep-dive findings
- `work-protocol.md` — Updated with this entry

**Summary:**
Conducted deeper analysis per maintainer request focusing on three specific tooling/instruction problems:

1. **.NET 10 `dotnet test` dual runner friction** — Verified live that `dotnet test --solution` and `dotnet test --project` both fail from repo root because .NET 10 uses VSTest mode (no `global.json`) which doesn't support these flags. Only works from `src/` directory where `global.json` enables Microsoft.Testing.Platform. The `scripts/test-with-timeout.sh` wrapper handles this, but agents who bypass the wrapper always fail.

2. **UAT authentication gap** — Found that `copilot-setup-steps.yml` configures `gh auth login` (for `gh` CLI) but does NOT call `gh auth setup-git` (for `git` operations). This means `git push` to UAT submodules may fail because git itself has no credentials. Similarly, the Azure DevOps credential helper in `uat-helpers.sh` is designed for WSL environments, not GitHub Actions runners.

3. **Screenshot generation confusion** — The developer/coding agent has no instructions about screenshot generation tools (`scripts/generate-release-screenshots.sh`, `scripts/generate-screenshot.sh`). These are only documented in the release-manager agent instructions. When the coding agent was asked to add screenshots, it went through 3 failed attempts before generating actual PNGs.

**New Improvement Opportunities Added:**
- #8: Document .NET 10 dual runner behavior in agent instructions
- #9: Add `gh auth setup-git` to copilot-setup-steps.yml
- #10: Add screenshot generation guidance to developer agent instructions

**Problems Encountered:**
- None — all findings verified through live testing
