# Workflow 069: Screenshot Quality Enforcement

## Problem Context
During Feature 068 release, the Release Manager agent bypassed the "Screenshots" section of release notes when encountering a `ScreenshotGenerator` timeout. This pattern has recurred in previous releases, where agents prioritize "completing the workflow" (merging PR) over "quality of artifacts," leading to incomplete releases that violate project quality standards.

**Root Cause:** Screenshot generation is currently treated as "optional" in agent instructions, with conditional language ("if visual changes", "recommended but not required"). When technical friction occurs (timeout, environment issues), agents interpret this as permission to skip screenshots entirely.

**Evidence:**
- Feature 068 retrospective shows Release Manager bypassed repo scripts and potentially skipped screenshots
- Issue description mentions this is a "recurring pattern" across multiple releases
- Current Release Manager instructions use conditional language: "Only include a screenshots section if you have actual screenshots"

## Candidate Workflow Improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Harden Release Manager instructions: Make screenshots MANDATORY for visual features | Issue description | ✅ Done | Added explicit MUST STOP rule in Boundaries and release notes workflow. Screenshots now non-negotiable with quality-over-speed principle. | High | Low | Low | Updated `.github/agents/release-manager-coding-agent.agent.md` in Always Do, Ask First, and Never Do sections |
| 2 | Update copilot-instructions.md: Add global screenshot quality standards | Issue description | ⛔ Excluded | Project-wide guidance not needed per Maintainer - release-specific only. | N/A | N/A | N/A | Excluded per Maintainer feedback |
| 3 | Add pre-release validation script: Fail if screenshots missing for visual features | Issue description | ⛔ Excluded | Automated guardrail not needed if tooling is fixed per Maintainer. | N/A | N/A | N/A | Excluded per Maintainer feedback |
| 4 | Investigate ScreenshotGenerator timeout root causes | Issue description | ✅ Done | Root cause: External CDN dependencies for CSS/JS; 60s timeout insufficient with network latency + browser init overhead. Increased to 90s with detailed error messages. | Medium | High | Low | Enhanced timeout handling in HtmlScreenshotCapturer.cs |
| 5 | Implement local CSS/asset caching for ScreenshotGenerator | Issue description | ✅ Done | Added fallback inline CSS in all HTML templates (github-wrapper-light.html, github-wrapper.html, azdo-wrapper.html) with onerror handlers. CDN loads first, falls back to inline styles if CDN fails. | Medium | High | Medium | Templates now work offline or with CDN failures |
| 6 | Enhance screenshot generation script with retry logic and verbose errors | Issue description | ✅ Done | Added 3-attempt retry with 5s delays, comprehensive error messages, and troubleshooting guidance in generate-release-screenshots.sh | Low | Medium | Low | Script now more resilient to transient failures |

## Recommendations

- **Option 1 (Best balance of effort/impact):** **IDs 1, 2, 3** — Instruction hardening (#1, #2) plus validation guardrail (#3) creates defense-in-depth: agents know the rule, and automation enforces it. Medium effort (need validation script), high impact (prevents incomplete releases).
  
- **Option 2 (Quick win):** **IDs 1, 2** — Instruction-only changes (lowest effort). Makes expectations explicit in agent and global instructions. Relies on agent compliance without automated enforcement.
  
- **Option 3 (Root cause fix):** **IDs 4, 5** — Address technical debt causing timeouts. Highest effort (debugging Playwright, implementing caching), uncertain impact (may not eliminate all failures). Best done after implementing guardrails to prevent workarounds.

## Decision
**Maintainer selected: Tasks 4, 5, 6, then 1**

Implementation order:
1. Fix ScreenshotGenerator timeout root causes (task 4)
2. Implement local CSS/asset caching (task 5)
3. Enhance script with retry logic and verbose errors (task 6)
4. Update Release Manager instructions (task 1)

Tasks 2 and 3 excluded per Maintainer feedback:
- Task 2 (copilot-instructions.md) - Not needed, release-specific only
- Task 3 (validation script) - Not needed if tooling is fixed
