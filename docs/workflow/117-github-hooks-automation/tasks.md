# GitHub Hooks Automation — Candidate Improvements

## Background

This work item explores how GitHub Actions event hooks can improve and automate the agent-based development workflow. The current workflow uses GitHub Actions for CI/CD (build, test, release), but several high-value GitHub events are not yet leveraged for agent workflow automation.

### Currently Used GitHub Events

| Event | Workflow | Purpose |
|-------|----------|---------|
| `push` → `main` | `ci.yml` | Versioning after merge |
| `pull_request` → `main` | `pr-validation.yml` | Build, test, lint, coverage |
| `pull_request` → `main` | `uat-validate.yml` | UAT script validation |
| `push` → `v*` tags | `release.yml` | Binary release |
| `workflow_run` (CI) | `release.yml` | Post-CI release |
| `workflow_dispatch` | `release.yml` | Manual release trigger |
| `push` → `main` | `deploy-website.yml` | Website deployment |

### Unused GitHub Events with Workflow Potential

| Event | Trigger Condition | Agent Workflow Relevance |
|-------|-------------------|--------------------------|
| `issues.labeled` | Label added to issue | Trigger @copilot assignment |
| `pull_request.closed` (merged) | PR merged to main | Trigger retrospective |
| `pull_request_review.submitted` | Review with CHANGES_REQUESTED | Notify developer agent |
| `issue_comment.created` | Comment on issue | Parse agent routing keywords |
| `issues.assigned` | Issue assigned to user | Create feature branch |
| `check_run.completed` | CI check fails on PR | Developer re-invocation |
| `label.created` | New label added | Repository labeling maintenance |
| `pull_request.labeled` | Label added to PR | Agent action dispatch |

---

## Candidate Workflow Improvements

| ID | Title | Source | Status | Rationale | Impact | Effort | Risk | Notes |
|---:|---|---|---|---|---|---|---|---|
| 1 | Issue label → auto-assign to @copilot | exploration | ✅ Done | Eliminates the manual step of assigning an issue to @copilot after labeling. Maintainer labels issue with `copilot` and the workflow orchestrator is automatically triggered. | High | Low | Low | Adds `copilot` label + `.github/workflows/issue-copilot-assign.yml` |
| 2 | PR merge → retrospective reminder | exploration | ✅ Done | After a feature/fix PR is merged, the workflow currently ends without a structured reminder to run the retrospective agent. Automating this comment ensures retrospective is never skipped. | Medium | Low | Low | `.github/workflows/pr-merge-retrospective.yml`; only triggers on feature/fix branches |
| 3 | PR review CHANGES_REQUESTED → developer notification | exploration | ⬜ Not started | When a reviewer submits CHANGES_REQUESTED, the developer agent should be re-invoked. Currently this is a manual step. A structured GitHub Actions comment on the PR provides the developer coding agent the context to act on feedback. | High | Medium | Medium | Requires careful comment format to avoid false triggers |
| 4 | CI failure → structured developer feedback | exploration | ⬜ Not started | When CI fails on a `copilot/*` branch, post a structured comment that helps the developer coding agent understand the failure and act autonomously. | High | Medium | Medium | Must parse CI failure details for agent context |
| 5 | Issue comment keyword routing | exploration | ⬜ Not started | Parse issue/PR comments for `/assign-to:agent-name` keywords to route to specific agents. | Medium | High | High | Complex; risk of unintended triggers |
| 6 | Release tag → close work item issues | exploration | ⬜ Not started | When a release tag is pushed, auto-close or comment on issues resolved in that release. | Low | Medium | Low | Requires convention for linking issues to releases |
| 7 | Weekly agent workflow health check | exploration | ⬜ Not started | Scheduled workflow (`schedule` event) that validates agent definitions, checks for broken tool references, and posts a summary issue. | Medium | Medium | Low | Uses `scripts/validate-agents.py` |

---

## Implementation Summary (Items 1 & 2)

### Item 1: Issue Label → Auto-assign to @copilot

**File:** `.github/workflows/issue-copilot-assign.yml`

**Trigger:** `issues.labeled` when label = `copilot`

**Behavior:**
1. When any issue is labeled with `copilot`, GitHub Actions assigns it to `@copilot`
2. GitHub's native @copilot assignment then automatically creates a `copilot/*` branch and PR
3. The workflow orchestrator coding agent runs automatically

**Benefits:**
- Maintainer only needs to add a label — no manual assignment step
- Integrates seamlessly with existing @copilot orchestration
- Zero risk to existing workflows (new independent workflow)

### Item 2: PR Merge → Retrospective Reminder

**File:** `.github/workflows/pr-merge-retrospective.yml`

**Trigger:** `pull_request.closed` when `merged = true` and base branch = `main`

**Behavior:**
1. Detects if merged PR is a feature or fix branch (pattern: `feature/*` or `fix/*`)
2. Posts a structured comment on the merged PR with:
   - Link to retrospective agent instructions
   - Work item folder path inferred from branch name
   - Clear call-to-action for running the retrospective agent

**Benefits:**
- Ensures retrospective is never skipped after a feature/fix delivery
- Low risk — purely informational comment on already-merged PR
- Works with both manual (@workflow-orchestrator) and automated (@copilot) flows

---

## Recommendations

- **Option 1 (Best balance of effort/impact):** **Items 1 + 2** — Both are low-effort, high-value automations that directly support the coding agent workflow. Implemented together in this PR.
- **Option 2 (Next quick win):** **Item 7** — Scheduled agent validation check; reuses existing `scripts/validate-agents.py`.
- **Option 3 (Highest impact, more work):** **Items 3 + 4** — PR review and CI failure notifications would close the feedback loop for autonomous coding agents but require more careful design.

## Decision

Items 1 and 2 have been implemented in this PR. Items 3–7 are documented for future workflow improvement sprints.
