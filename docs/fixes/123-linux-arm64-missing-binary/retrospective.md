# Retrospective: Fix 123 — Linux ARM64 Missing Binary

**Date:** 2026-05-04  
**Branch:** `copilot/fix-linux-arm64-binary-issue`  
**PR:** #652  
**Participants:** Maintainer, Issue Analyst, Developer, Technical Writer, Code Reviewer, Release Manager, Retrospective

---

## Summary

Fix 123 addressed the silent disappearance of `linux-arm64` and `linux-musl-arm64` pre-built
binaries from GitHub Release assets starting with v1.42.1 and v1.43.0. The root cause was a
supply-chain hardening commit (`ae4e33c`) that pinned Docker image digests to AMD64-only
platform manifests rather than multi-arch manifest lists. On `ubuntu-24.04-arm` (ARM64) runners,
Docker silently served the AMD64 image, which immediately crashed with `exec format error`.

The fix cycle ran in approximately 21 minutes from first commit to PR-ready. The Code Reviewer
caught a critical secondary defect: the Developer's initial musl fix added `--platform` flags but
left the single-platform AMD64 digests in place — which Docker cannot use to select an ARM64
image. The Code Reviewer corrected this with a second commit (`e723e16`), updating both Alpine
references to verified multi-arch manifest list digests. Without this correction, `linux-musl-arm64`
would have remained broken despite the PR.

On the positive side, the issue analysis was thorough, supply-chain security was preserved
throughout (digests were updated, not removed), and a new binary-presence validation step was
added to prevent future silent omissions.

---

## Scoring Rubric

Starting score: **10**

| Deduction | Reason | Evidence |
|-----------|--------|----------|
| −1.5 | Developer's `linux-musl-arm64` fix was incomplete: added `--platform` flag without updating single-platform AMD64 digests to multi-arch manifest lists. Would have shipped a still-broken ARM64 musl build. | Code Reviewer `code-review.md` § "Critical Bug Fixed During Review"; commit `e723e16` |
| −0.5 | Issue Analyst did not append a log entry to `work-protocol.md`. Analysis document exists and is thorough, but the protocol was not followed. | `work-protocol.md` has no `## Issue Analyst Agent Log` entry; noted by both Code Reviewer and Release Manager |

**Final workflow rating: 8/10**

---

## Session Overview

### Time Breakdown

| Metric | Duration | % of Session |
|--------|----------|--------------|
| **Session Duration** | ~21 min | 100% |
| Agent Work Time (estimated) | ~21 min | ~100% |
| User Wait Time | Unavailable | — |

> **Note:** Chat logs were not exported for this session. Timing data is derived from git commit
> timestamps only. Per-agent breakdowns below are from commit messages, not measured session durations.

- **Start:** 2026-05-04 11:25:23 UTC (commit `064191e` — Issue Analyst: `docs: analyze linux arm64 missing binary bug`)
- **End:** 2026-05-04 11:46:24 UTC (commit `d9696aa` — Release Manager: `docs: add release notes for fix 123`)
- **Total Commits:** 6
- **Files Changed:** 8 distinct files across production and documentation
  - Production: `.github/workflows/release.yml` (1 file, 2 fix commits)
  - Documentation: `README.md`, `docs/adr-008-multi-platform-binary-distribution.md`, `CONTRIBUTING.md`
  - Artifacts: `analysis.md`, `code-review.md`, `release-notes.md`, `work-protocol.md`
- **Total Insertions/Deletions:** +749 / −27
- **Tests Added:** 0 unit tests (N/A — CI workflow change; 1 runtime validation step added)
- **Total Tests Passing:** N/A (no .NET test suite changes)

### Commit Timeline

| Timestamp (UTC) | Commit | Agent | Description |
|-----------------|--------|-------|-------------|
| 11:25:23 | `064191e` | Issue Analyst | `docs: analyze linux arm64 missing binary bug` |
| 11:28:46 | `bca278a` | Developer | `fix: restore linux arm64 binary builds by fixing Docker platform pinning` |
| 11:32:29 | `7a6f998` | Technical Writer | `docs: update documentation for linux arm64 build fix` |
| 11:40:05 | `e723e16` | Code Reviewer | `fix: use multi-arch manifest list digests for musl Alpine Docker images` |
| 11:42:28 | `ae6853d` | Code Reviewer | `docs: code review report and work protocol update for fix 123` |
| 11:46:24 | `d9696aa` | Release Manager | `docs: add release notes for fix 123 (linux arm64 missing binary)` |

---

## Agent Analysis

### Agent Attribution Note

Chat logs were not exported for this session. Per-agent metrics (model usage, request counts,
tool call patterns, automation effectiveness) are **Unavailable**. The analysis below is based
entirely on git commit history, artifact content, and work-protocol entries.

### Agents by Role (Bug Fix Workflow)

| Agent | Required | Present | Log in work-protocol.md | Evidence |
|-------|----------|---------|--------------------------|----------|
| Issue Analyst | ✅ Required | ✅ Yes | ⚠️ Missing | `analysis.md` exists and is complete |
| Developer | ✅ Required | ✅ Yes | ✅ Present | `work-protocol.md` § Developer Agent Log |
| Technical Writer | ✅ Required | ✅ Yes | ✅ Present | `work-protocol.md` § Technical Writer Agent Log |
| Code Reviewer | ✅ Required | ✅ Yes | ✅ Present | `work-protocol.md` § Code Reviewer Agent Log |
| UAT Tester | ⚠️ If needed | — Skipped | N/A | CI workflow change; no UAT required |
| Release Manager | ✅ Required | ✅ Yes | ✅ Present | `work-protocol.md` § Release Manager Agent Log |
| Retrospective | ✅ Required | ✅ Yes | ✅ Present | This document |

---

## Rejection Analysis

Per-agent and per-model rejection metrics are **Unavailable** (no exported chat logs).

No manual maintainer interventions to unblock any agent were identified from the artifact record.
The Code Reviewer's correction commit (`e723e16`) was self-initiated within the review scope —
this is expected reviewer behaviour, not a maintainer unblock.

---

## Automation Opportunities

### Terminal Command Patterns

| Pattern | Observed | Recommendation |
|---------|----------|----------------|
| Docker manifest digest verification via MCR registry API | Manual (Code Reviewer) | Consider adding a script `scripts/verify-docker-digests.sh` that checks each pinned digest for multi-arch manifest list type |
| Binary presence validation in release workflow | Added as CI step (Option A) | ✅ Already automated by this fix |

### Suggested Skills / Scripts

| Opportunity | Proposed Script/Skill | Where It Fits | Evidence | Verification |
|-------------|----------------------|---------------|----------|--------------|
| Validate Docker digest architecture type (manifest list vs. single-platform) | `scripts/check-docker-digests.sh` | Pre-commit hook or CI lint step on `.github/workflows/*.yml` changes | Code Reviewer manually called MCR API to check `sha256:828a5235...` and `sha256:0191ff38...`; this is repeatable and deterministic | Script exits non-zero if any digest in `release.yml` resolves to a non-list manifest |
| Enforce `--platform` + multi-arch manifest list pairing in workflow YAML | Extend existing YAML linting / add `validate-agent` check | Developer stage (before code review) | Developer submitted `--platform` with AMD64-only digest — a subtle pairing error that linting could catch | Lint rule: if `docker run --platform` is present, the image reference must use a manifest list digest |

### Script Usage Analysis

- **Available scripts not misused:** No available wrapper scripts were skipped or bypassed.
- **Repeated manual command:** Docker registry API calls (`curl` or browser calls to MCR) were done manually by the Code Reviewer. A helper script would speed up future supply-chain audits and reduce the chance of accepting a wrong digest.

---

## Model Effectiveness Assessment

### Assigned vs Actual Model Usage

Per-session model usage is **Unavailable** (no exported chat logs). Assigned models are from
`.github/agents/*.agent.md`.

| Agent | Assigned Model | Actual Usage | Assessment |
|-------|----------------|--------------|------------|
| Issue Analyst | GPT-5.4 | Unavailable | GPT-5.4 is appropriate for root-cause analysis |
| Developer | GPT-5.4 | Unavailable | GPT-5.4 appropriate; incomplete musl fix suggests Docker nuance knowledge gap, not model limitation |
| Technical Writer | Claude Sonnet 4.6 | Unavailable | Sonnet 4.6 appropriate for documentation tasks |
| Code Reviewer | Claude Sonnet 4.6 | Unavailable | Sonnet 4.6 performed well — caught critical Docker manifest subtlety |
| Release Manager | Claude Sonnet 4.6 | Unavailable | Appropriate for release notes and protocol verification |
| Retrospective | Gemini 3 Flash (Preview) | Gemini 3 Flash (Preview) | Appropriate for structured retrospective analysis |

### Model Performance Statistics

**Unavailable** — no exported chat logs.

### Recommendations

- Model assignments appear appropriate for each agent's role. No model reassignment is recommended
  based on available evidence.
- If chat logs are available for future retrospectives, compare Code Reviewer response times when
  Sonnet 4.6 is used for Docker-heavy analysis to assess whether a stronger reasoning model
  (e.g., GPT-5.4) would reduce rework.

---

## Agent Performance

| Agent | Rating | Strengths | Improvements Needed |
|-------|--------|-----------|---------------------|
| Issue Analyst | ⭐⭐⭐⭐ | Thorough root-cause analysis with exact commit (`ae4e33c`) identified; clear Fix 1 / Fix 2 separation; included automated detection options (A and B); ARM64 vs AMD64 manifest distinction explained precisely | Missing work-protocol log entry — violates the standard protocol requiring every agent to append before handoff |
| Developer | ⭐⭐⭐ | linux-arm64 fix was correct and complete; `sudo` non-root pattern matched existing UPX pattern; binary presence validation (Fix 3, Option A) implemented correctly | linux-musl-arm64 fix was incomplete: added `--platform` flags but did not update single-platform AMD64 digests to multi-arch manifest lists; the critical Docker subtlety (list digest required for `--platform` to work) was missed, requiring Code Reviewer intervention |
| Technical Writer | ⭐⭐⭐⭐⭐ | Proactively removed stale "Available starting with next release" notice that predated this fix; added previously undocumented musl platforms to the README platform table; ADR-008 updated with regression note and phase completion status; all three docs (README, ADR, CONTRIBUTING) updated consistently | None identified |
| Code Reviewer | ⭐⭐⭐⭐⭐ | Caught the critical secondary defect (AMD64-only digest + `--platform` = no-op); verified digests live against the MCR registry API; applied the fix in-review with a well-documented commit message; performed adversarial test matrix including false-positive substring checks for the validation step; flagged dead `apk add` branch as a future cleanup suggestion | None identified |
| Release Manager | ⭐⭐⭐⭐ | Accurate pre-release checklist; correctly assessed UAT as not required; release notes clearly distinguish the two-commit fix (developer + code reviewer correction); noted the Issue Analyst protocol gap without blocking | Minor: The work-protocol entry records the Issue Analyst gap as "minor" — consistent assessment, but the gap is a concrete protocol violation and could have been called out more forcefully as a process issue |
| Retrospective (self) | ⭐⭐⭐⭐ | Evidence-based report; constrained claims to git/artifact record; clearly labeled unavailable metrics; protocol gap surfaced and tracked | No exported chat logs available — session metrics section is thinner than ideal; future retrospectives should export chat before starting analysis |

**Overall Workflow Rating: 8/10**

---

## What Went Well

1. **Rapid cycle time (~21 minutes end-to-end):** From first analysis commit to release-ready PR
   in a single continuous session. Each agent handed off promptly without blocking.

2. **Thorough root-cause analysis:** The `analysis.md` correctly identified commit `ae4e33c` as
   the introducing change, explained the AMD64-only vs. multi-arch manifest distinction clearly,
   and provided two ready-to-implement fix options with code examples.

3. **Code Reviewer caught a critical defect before it could ship:** The `--platform` flag without
   a manifest list digest is a non-obvious Docker behavior. Catching it via MCR registry API
   verification (rather than waiting for a failing CI run) saved at least one additional release
   cycle.

4. **Supply-chain security preserved:** Both the Developer and Code Reviewer kept image references
   pinned by digest throughout. The fix updated to better digests (multi-arch manifest lists)
   rather than removing pinning.

5. **Proactive detection mechanism added:** The binary-presence validation step in
   `consolidate-checksums` means future broken ARM64 builds will fail loudly in CI rather than
   silently omitting platform archives from the release.

6. **Technical Writer caught stale documentation:** The "Available starting with the next release"
   notice — which should have been removed post v1.42.0 — was identified and cleaned up,
   improving the accuracy of the public-facing README.

7. **Commit messages were information-dense:** Especially `e723e16`, which explains the Docker
   manifest list reasoning in full, provides the registry-API evidence, and names the exact
   digests verified. Future engineers can reconstruct the reasoning without reading the
   code-review report.

---

## What Didn't Go Well

1. **Developer's `linux-musl-arm64` fix was incomplete (critical):** Commit `bca278a` added
   `--platform ${{ matrix.docker_platform }}` to both `docker run` commands but left the image
   references pinned to single-platform AMD64 manifest digests (`sha256:828a5235...`,
   `sha256:06c12910...`). Docker's `--platform` flag requires a manifest list to select a
   platform; it cannot transform a single-arch digest. The fix appeared correct structurally
   but would have shipped a still-broken `linux-musl-arm64` build.
   > *Evidence: `code-review.md` § "Critical Bug Fixed During Review"; Code Reviewer commit `e723e16`.*

2. **Issue Analyst did not append a work-protocol entry:** The `work-protocol.md` file was
   created by the Developer as part of commit `bca278a`, but contains no `## Issue Analyst
   Agent Log` section. The Issue Analyst produced a high-quality `analysis.md` but skipped
   the mandatory protocol step.
   > *Evidence: `work-protocol.md` has Developer as the first log entry; Code Reviewer noted
   > "⚠️ Minor gap" in both the code-review report and the Release Manager's pre-release
   > check table.*

3. **No automated guard against AMD64-only digest pinning:** The original regression (`ae4e33c`)
   and the Developer's initial musl fix both fell into the same pattern: obtaining digest values
   on an AMD64 host and pinning them without verifying manifest type. There is no CI lint or
   script to catch this class of error before review.

4. **Chat logs not exported:** Per-agent model usage, tool call patterns, and automation rate
   metrics are unavailable for this session. The retrospective metrics section is significantly
   thinner than intended.

---

## Improvement Opportunities

| Theme | Issue | Proposed Solution | Action Item | Verification |
|-------|-------|-------------------|-------------|--------------|
| **Docker digest hygiene** | AMD64-only digests can be pinned silently; `--platform` is a no-op without a manifest list | Add `scripts/check-docker-digests.sh` that iterates `release.yml` image references and calls the registry API to assert each pinned digest is a manifest list (`application/vnd.oci.image.index.v1+json` or `vnd.docker.distribution.manifest.list.v2+json`) | Create `scripts/check-docker-digests.sh` and add it as a `ci:` step in a `validate-workflow.yml` or pre-commit hook | Script exits 0 on current `release.yml` after the fix; would have exited 1 on `bca278a` pre-fix state |
| **Work Protocol enforcement** | Issue Analyst skipped the mandatory work-protocol log entry | Update `.github/agents/issue-analyst.agent.md` to include an explicit instruction: "Before handing off, append your log entry to `work-protocol.md`" (matching the Developer/Technical Writer/Code Reviewer/Release Manager agent instructions) | Edit `.github/agents/issue-analyst.agent.md` to add work-protocol append instruction | Next bug fix cycle: Issue Analyst log entry present in `work-protocol.md` before Developer begins |
| **Developer Docker-platform knowledge gap** | Developer correctly structured the fix but missed the manifest-list requirement for `--platform` | Add a note to `.github/agents/developer.agent.md` (or `docs/spec.md`) documenting: "When pinning Docker digests, always verify with the registry API that the digest is a manifest list (not a single-platform manifest) before committing." | Edit `.github/agents/developer.agent.md` to add Docker digest guidance | Next release.yml update with image pinning: manifest list digests used from the start |
| **Session observability** | Chat logs not exported; session metrics (model usage, automation rate) are unavailable | Make chat export a standard retrospective prerequisite: update Retrospective agent instructions or `docs/agents.md` to note "export chat logs before running the retrospective agent" | Update the Retrospective agent's workflow step 1 (Export Chat History) to include a note that chat export should happen immediately after each agent session ends, not only at retrospective time | Next retrospective: model usage and automation rate tables are populated |
| **Commit hygiene** | The original regression (`ae4e33c`) mixed an unrelated runtime bug fix with supply-chain hardening in one commit, making the regression harder to spot and bisect | Reinforce the guidance in `docs/spec.md` or `CONTRIBUTING.md` that supply-chain hardening changes (digest pinning) should be in separate commits from functional fixes | Add a note in `CONTRIBUTING.md` § Development Guidelines: "Avoid mixing security hardening changes (e.g., digest pinning) with functional bug fixes in a single commit" | The note is present in CONTRIBUTING.md; post-release commits in the repo follow single-concern practice |

---

## Work Protocol Analysis

### Required Agents for Bug Fix Workflow

| Agent | Required | Log Entry Present | Deliverable Present |
|-------|----------|-------------------|---------------------|
| Issue Analyst | ✅ | ⚠️ Missing | `analysis.md` ✅ |
| Developer | ✅ | ✅ | `release.yml` changes ✅ |
| Technical Writer | ✅ | ✅ | `README.md`, `CONTRIBUTING.md`, `ADR-008` ✅ |
| Code Reviewer | ✅ | ✅ | `code-review.md` ✅ |
| Release Manager | ✅ | ✅ | `release-notes.md` ✅ |
| Retrospective | ✅ | ✅ | `retrospective.md` ✅ |

### Protocol Observations

- **Issue Analyst protocol gap**: The work-protocol entry was not created by the Issue Analyst.
  The Developer created `work-protocol.md` as part of their first commit (`bca278a`), suggesting
  the Issue Analyst handed off without creating or updating the file. The analysis document
  itself was complete and correct, so this is a process hygiene issue rather than a quality issue.

- **Code Reviewer self-correction pattern**: The Code Reviewer both filed the review *and* applied
  the correction commit (`e723e16`), consistent with the Code Reviewer agent mandate to apply fixes
  during review. The work-protocol entry correctly documents this as a finding and fix.

- **Sequential handoffs worked correctly**: The commit timestamps show a clean sequential pipeline
  (Issue Analyst → Developer → Technical Writer → Code Reviewer → Release Manager) with no
  out-of-order work or backtracking after handoff.

---

## CI / Status Checks Summary

| Item | Status | Notes |
|------|--------|-------|
| Affected release v1.42.1 run | ❌ Failed | Run [25079249616](https://github.com/oocx/tfplan2md/actions/runs/25079249616) — linux-arm64 and linux-musl-arm64 build jobs failed |
| Affected release v1.43.0 run | ❌ Failed | Run [25192580223](https://github.com/oocx/tfplan2md/actions/runs/25192580223) — same failures |
| Last working release v1.42.0 | ✅ Passed | Run [24940253234](https://github.com/oocx/tfplan2md/actions/runs/24940253234) — before `ae4e33c` |
| PR #652 CI status | Pending merge | CI results for the fix branch not yet available in artifact record |

---

## User Feedback (Verbatim)

*No retrospective-specific feedback was provided during development (no "note for retro:" comments
found in PR history or work-protocol entries). The following is the maintainer-provided summary
supplied at retrospective time:*

> "Bug: linux arm64 binaries missing from releases v1.43.0 and v1.42.1. Root cause: AMD64-specific
> Docker digest pinning broke ARM64 runners. Fix: Removed container override for linux-arm64,
> updated Alpine to multi-arch manifests, added binary presence validation. Code review caught a
> critical secondary bug (Alpine digest was still AMD64-only; needed multi-arch manifest list).
> PR #652 created and ready for review."

**Mapping to improvement opportunities:**

| Feedback Item | Improvement Opportunity |
|---------------|------------------------|
| "AMD64-specific Docker digest pinning broke ARM64 runners" | → `scripts/check-docker-digests.sh` to lint manifest types in CI |
| "Code review caught a critical secondary bug" | → Developer Docker-platform guidance in agent instructions |
| (implicit: Issue Analyst no work-protocol entry) | → Issue Analyst agent instructions update to require work-protocol append |

*Retrospective interactive phase not conducted (maintainer provided explicit summary and requested
direct report creation). No additional issues raised.*

---

## Retrospective DoD Checklist

- [x] Evidence sources enumerated (git commit history, `work-protocol.md`, `analysis.md`, `code-review.md`, `release-notes.md`)
- [x] Evidence timeline normalized across lifecycle phases (Issue Analyst → Developer → Technical Writer → Code Reviewer → Release Manager → Retrospective)
- [x] Findings clustered by theme (Docker digest hygiene, Work Protocol enforcement, Developer knowledge gap, Session observability, Commit hygiene)
- [x] No unsupported claims — all findings cite specific commit hash, document section, or artifact
- [x] Unavailable metrics explicitly labeled (chat logs, per-agent model usage, automation rate, response times)
- [x] Agent attribution note present (per-agent metrics unavailable; only git/artifact record used)
- [x] No guessed agent attribution — agent → commit mapping based on commit message context and work-protocol entries, not speculation
- [x] Action items include where + verification for each improvement opportunity
- [x] Required metrics and required sections present (Session Overview, Agent Analysis, Rejection Analysis, Automation Opportunities, Model Effectiveness, Agent Performance, Scoring Rubric, What Went Well, What Didn't Go Well, Improvement Opportunities, User Feedback, CI Summary, Work Protocol Analysis, DoD Checklist)
- [x] All retro-related user feedback captured verbatim
- [x] Work Protocol Analysis section present with gap identified and documented
- [x] Scoring rubric deductions are evidence-based and explicitly listed
