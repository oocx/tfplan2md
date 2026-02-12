---
name: github-cloud-agent-workflow
description: Workflow guidance for agents running in GitHub cloud context (assigned issues and PR coding agents).
---

# GitHub Cloud Agent Workflow Skill

## Purpose
Provides specific guidance for agents running in GitHub's cloud environment, either triggered by issue assignments to `@copilot` or working on existing PRs.

## When to Use
- When executing from a GitHub issue assigned to `@copilot`
- When running as a coding agent on an existing PR (typically `copilot/*` branches)
- When deciding whether to recommend local vs cloud execution

## Workflow

### GitHub Issue Assigned to `@copilot`

When executing as a cloud agent from a GitHub issue assigned to `@copilot`:

1. **Parse Issue:** Extract task specification from issue body
   - Identify the specific workflow improvement requested
   - Note any constraints, scope, or acceptance criteria
   
2. **Validate Scope:** Ensure task is well-defined and within capabilities
   - If ambiguous, comment on issue requesting clarification
   - **Unlike local mode, you may ask multiple questions via issue comments**
   - Wait for user responses to your questions before proceeding
   - If out of scope, comment explaining why and suggest alternative
   - If task requires extensive interactive guidance, recommend local execution

3. **Read Context:** Review relevant documentation and current state
   - Check docs/agents.md for workflow patterns
   - Review affected agent files in .github/agents/
   - Consult docs/ai-model-reference.md if model changes are involved
   - Check .github/copilot-instructions.md for conventions

4. **Execute Changes:** Modify files according to task requirements
   - Make minimal, focused changes
   - Follow existing patterns and conventions
   - Ensure all handoff references are valid
   - Update documentation to match code changes

5. **Create PR:**
   - Branch: `workflow/<NNN>-<slug>` (e.g., workflow/032-cloud-agent-support)
   - Commits: Use conventional format (`workflow:`, `docs:`, `chore:`, `ci:`, `refactor:`) — do NOT use `feat:` or `fix:` for workflow-only changes (these trigger Versionize version bumps)
   - Description: Follow standard template (Problem/Change/Verification)
   - Link to the originating issue

6. **Request Review:** Assign PR to Maintainer or relevant reviewers
   - Document all decisions in PR description
   - Explain rationale for any non-obvious changes
   - Note any limitations or follow-up work needed

### GitHub PR Coding Agent (Existing PR)

When executing as a GitHub **coding agent on an existing pull request** (often on a `copilot/*` branch):

- **Do not create a new branch** and **do not `git switch` away** from the current branch.
- **Do not create a new PR**. Your job is to push commits to the existing PR branch.
- If you need clarification, **ask via PR comments** and wait for an answer (do not guess and do not "fill in" answers in docs).
- If you need to update with latest main, prefer `git fetch origin && git rebase origin/main` while staying on the current branch.

**Cloud Environment Limitations:**
- Cannot use `edit`, `execute`, `vscode`, `todo` tools directly
- Cannot run terminal commands interactively
- Rely on GitHub Actions for testing
- Document decisions upfront in PR

**Cloud Environment Advantages:**
- **Can ask multiple clarifying questions via issue comments** (unlike local mode which should minimize questions)
- User responds via comments, creating clear audit trail
- Asynchronous communication allows time for thoughtful responses

**When to Recommend Local Execution:**
- Task requires exploratory analysis
- Requirements are unclear or ambiguous
- Multiple design decisions need Maintainer input
- Rapid prototyping and iteration are beneficial
- Complex architectural changes are involved

## Key Principles

- **Leverage asynchronous communication** - cloud context allows multiple questions via comments
- **Work on existing branch** - never create new branches when working on existing PRs
- **Document thoroughly** - compensate for lack of interactive exploration with clear documentation
- **Know your limits** - recommend local execution when appropriate
