---
name: execution-context-detection
description: Detect whether agent is running in VS Code (local/interactive) or GitHub (cloud/automated) context and adapt behavior accordingly.
---

# Execution Context Detection Skill

## Purpose
Enable agents to detect their execution environment and adapt behavior appropriately for local (VS Code) or cloud (GitHub) contexts.

## Context Types

### VS Code (Local/Interactive)
- You are in an interactive chat session with the Maintainer
- Use handoff buttons to navigate to other agents
- Iterate and refine based on Maintainer feedback
- Use VS Code tools (edit, execute, todo, vscode)
- Can run tests, builds, and commands locally
- Ask one question at a time when clarification is needed
- Follow existing workflow patterns

### GitHub (Cloud)

#### GitHub Issue (Assigned to `@copilot`)
- You are processing a GitHub issue assigned to `@copilot`
- Work autonomously following issue specification
- Create a pull request with your changes
- Document all decisions in PR description
- Use GitHub-safe tools (search, web, github/*)
- **Unlike local mode, you may ask multiple questions via issue comments**
- Wait for user responses to your questions before proceeding
- Cannot run tests/builds locally - rely on CI/CD or document expected outcomes
- Cannot use interactive tools (execute, edit with file watching, todo, vscode)

#### GitHub Pull Request (PR Coding Agent)
- You are executing as a GitHub Copilot **coding agent on an existing pull request**
- Work on the **branch already associated with the PR** (often `copilot/*`)
- **Do not create/switch branches**; otherwise changes may not appear in the PR
- If clarification is needed, **ask via PR comments** and wait for an answer (do not guess)

## Detection Methods

**How to detect context:**
- **VS Code:** You are in an interactive chat session. The input is conversational and you can see chat history. Available tools include `edit`, `execute`, `vscode`, and `todo`.
- **GitHub Issue:** The input starts with issue metadata (title, labels, body).
- **GitHub PR coding agent:** You are operating on an existing PR (PR title/description/comments are the primary feedback loop) and/or the current branch starts with `copilot/`.

**Reliable detection approach:**
- Check if `edit`, `execute`, or `vscode` tools are available → VS Code context
- Check if input contains GitHub issue structure (title, labels, assignee) → GitHub Issue context
- If you are operating on an existing PR (or branch is `copilot/*`) → GitHub PR coding agent context
- Default to VS Code if detection is ambiguous

## Key Behavioral Differences

| Aspect | Local (VS Code) | GitHub Issue | GitHub PR Coding Agent |
|--------|----------------|-------------|------------------------|
| Interaction | One question at a time | Multiple questions via issue comments | Ask via PR comments and wait |
| Tools | Full access (execute, edit, todo, vscode) | GitHub-safe only (search, web, github/*) | GitHub-safe only (search, web, github/*) |
| Validation | Run tests/builds locally | Document outcomes, rely on CI/CD | Document outcomes, rely on CI/CD |
| Iteration | Real-time with Maintainer | Asynchronous via issue comments | Asynchronous via PR comments |

## Usage in Agent Instructions

Include a reference to this skill in the "Execution Context" section of agent instructions:

```markdown
## Execution Context

Determine your environment at the start of each interaction. See the `execution-context-detection` skill for detailed guidance on context detection and behavioral adaptation.

### VS Code (Local/Interactive)
[Agent-specific local behaviors]

### GitHub (Cloud/Automated)
[Agent-specific cloud behaviors]

### GitHub Pull Request (PR Coding Agent)
[Agent-specific PR-coding-agent behaviors]
```

## When to Adapt Behavior

- **Question asking:** Use one-at-a-time (local) vs multiple questions (GitHub issue) vs PR comments (GitHub PR coding agent)
- **Validation:** Run commands (local) vs document expectations (cloud)
- **Tool selection:** Use all available tools (local) vs GitHub-safe only (cloud)
- **Iteration:** Real-time feedback (local) vs asynchronous comments (GitHub)
