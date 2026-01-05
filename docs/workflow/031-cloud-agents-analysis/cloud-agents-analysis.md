# Cloud Agents Analysis for tfplan2md Workflow

**Created:** January 5, 2026  
**Status:** Analysis Complete  
**Purpose:** Analyze how GitHub Copilot cloud agents differ from local agents and propose workflow enhancements

---

## Executive Summary

This analysis examines GitHub Copilot's **cloud agents** (also called "coding agents") and how they differ from the **local agents** currently used in the tfplan2md workflow. Cloud agents execute on GitHub's infrastructure asynchronously, enabling automation of well-scoped tasks while maintaining the existing local agent workflow for interactive development.

**Key Finding:** Cloud agents and local agents serve complementary purposes. Local agents excel at interactive, iterative development in VS Code, while cloud agents automate background tasks through GitHub Actions. Both can coexist using the same agent definitions with appropriate `target` configuration.

**Recommendation:** Enhance the Workflow Engineer agent to support cloud agents, enabling workflow improvements to be executed either locally (interactive) or in the cloud (automated).

---

## 1. Core Differences: Cloud Agents vs Local Agents

### 1.1 Execution Environment

| Aspect | Local Agents (`target: vscode`) | Cloud Agents (`target: github-copilot`) |
|--------|--------------------------------|------------------------------------------|
| **Execution Location** | Developer's VS Code instance | GitHub Actions infrastructure (remote) |
| **Interaction Model** | Synchronous, real-time chat | Asynchronous, task-based delegation |
| **Workspace Access** | Direct file system access | Isolated GitHub repository clone |
| **Tool Availability** | Full VS Code tools (edit, search, execute, etc.) | GitHub-specific tools (repo operations, PR management) |
| **State Persistence** | Session-based in chat | PR-based with branch tracking |
| **Output** | Chat responses and local file edits | Pull requests with code changes |

### 1.2 Workflow Integration

**Local Agents:**
- Invoked interactively via `@agent-name` in VS Code Copilot Chat
- Provide immediate feedback and iterate in real-time
- Maintainer guides the conversation and approves each step
- Changes remain local until manually committed and pushed
- Ideal for: prototyping, debugging, exploratory work, complex decision-making

**Cloud Agents:**
- Triggered by assigning GitHub issues to `@copilot` or delegating from Agents Panel
- Work independently in isolated GitHub Actions environment
- Create pull requests automatically for review
- Handle testing, linting, and verification autonomously
- Ideal for: automation, large refactoring, routine tasks, parallel work streams

### 1.3 Tool and Configuration Differences

**Shared Properties (Both Environments):**
```yaml
---
name: Agent Name
description: Brief description
tools: [...]  # Tool list (environment-specific)
---
```

**VS Code-Specific Properties (Ignored by Cloud Agents):**
```yaml
model: GPT-5.1-Codex-Max      # Model selection
handoffs:                      # Agent-to-agent handoffs
  - label: Next Step
    agent: "Other Agent"
    prompt: Handoff instruction
    send: false
argument-hint: "..."          # Input hints
```

**GitHub-Specific Context:**
- Issue assignments (`@copilot` mentions)
- Repository permissions and security scanning
- GitHub Actions workflow triggers
- Team/organization permissions
- Branch protection and required checks

---

## 2. Target Property Configuration

The `target` property in the agent frontmatter controls where the agent is available:

### 2.1 Target Values

```yaml
---
target: vscode           # Available only in VS Code
---
```

```yaml
---
target: github-copilot   # Available only on GitHub.com
---
```

```yaml
---
# (no target specified)   # Available in both environments
---
```

### 2.2 Current tfplan2md Configuration

All current agents use `target: vscode`:

```bash
$ grep -h "^target:" .github/agents/*.agent.md | sort | uniq -c
     13 target: vscode
```

**Agents currently limited to VS Code:**
- Issue Analyst
- Requirements Engineer  
- Architect
- Quality Engineer
- Task Planner
- Developer
- Technical Writer
- Code Reviewer
- UAT Tester
- Release Manager
- Retrospective
- Workflow Engineer
- Web Designer

---

## 3. Use Case Analysis: When to Use Each Type

### 3.1 Local Agent Use Cases (Current Workflow)

**Best For:**
- ✅ Interactive feature development with Maintainer guidance
- ✅ Exploratory analysis and design decisions
- ✅ Complex architectural changes requiring human judgment
- ✅ Debugging issues with real-time iteration
- ✅ Tasks requiring immediate feedback and course correction
- ✅ UAT validation where Maintainer reviews rendering in real platforms

**Workflow Pattern:**
```
Maintainer → @agent in Chat → Agent responds → Maintainer guides → Iterate
```

### 3.2 Cloud Agent Use Cases (New Capability)

**Best For:**
- ✅ Automated workflow improvements that don't require real-time guidance
- ✅ Routine refactoring tasks (e.g., updating all agent models)
- ✅ Batch documentation updates
- ✅ Periodic maintenance tasks (dependency updates, linting)
- ✅ Parallel work on multiple features simultaneously
- ✅ Background processing of well-defined tasks

**Workflow Pattern:**
```
Issue created → Assigned to @copilot → Cloud agent processes → PR created → Maintainer reviews
```

**Not Suitable For:**
- ❌ Tasks requiring local tool access (VS Code extensions, local terminals)
- ❌ Interactive debugging with Maintainer participation
- ❌ Complex decisions requiring iterative refinement
- ❌ UAT validation (requires real GitHub/Azure DevOps rendering)

---

## 4. Implications for Current Workflow

### 4.1 What Stays the Same

✅ **Agent Definitions:** The instruction content in `.github/agents/*.agent.md` files works for both local and cloud contexts.

✅ **Artifact Structure:** The `docs/features/`, `docs/issues/`, and `docs/workflow/` folder structure remains unchanged.

✅ **Handoff Pattern:** Sequential agent workflow (Requirements Engineer → Architect → ...) continues for local interactive work.

✅ **Skills:** Agent skills in `.github/skills/` work in both environments (though some may need tool adjustments).

✅ **Documentation:** `docs/agents.md` continues to define the workflow.

### 4.2 What Changes for Cloud Agents

⚠️ **Target Property:** Must explicitly set `target: github-copilot` or omit `target` for dual-environment support.

⚠️ **Tool References:** Cloud agents cannot use VS Code-specific tools like `execute/runInTerminal` or `edit`. They use GitHub repo tools instead.

⚠️ **Model Property:** Ignored by cloud agents (GitHub infrastructure chooses model).

⚠️ **Handoffs:** Not supported in cloud context; handoff logic must be issue-driven.

⚠️ **Invocation Method:** Cloud agents are triggered by issue assignment, not chat commands.

⚠️ **Output Format:** Cloud agents produce PRs, not chat messages. Maintainer reviews via PR comments, not chat.

### 4.3 Workflow Coexistence Strategy

**Dual-Mode Agent Pattern:**
```yaml
---
name: Developer
description: Implement features and tests according to specifications
# (no target) - Available in both VS Code and GitHub
tools: 
  # Tools common to both environments
  - 'search'
  - 'web'
  - 'github/*'
---

# Agent instructions work for both contexts
You are the **Developer** agent...

## Context Detection

Determine your execution context:
- **VS Code:** You are in an interactive session with the Maintainer. Use chat, iterate, request approvals.
- **GitHub (Cloud):** You are processing an assigned issue. Work autonomously, create a PR, document your decisions.

...
```

**Specialized Agent Pattern:**
```yaml
---
name: Workflow Automation Agent
description: Execute automated workflow improvements
target: github-copilot    # Cloud-only
tools: ['github/*']
---

You are the **Workflow Automation Agent**. You execute well-scoped workflow improvement tasks assigned via GitHub issues.

Your workflow:
1. Parse issue for task specification
2. Read related documentation
3. Make changes to agent files / documentation
4. Run validation checks
5. Create PR with detailed description
6. Request review from Maintainer
```

---

## 5. Proposed Changes to Support Cloud Agents

### 5.1 Option A: Enhance Workflow Engineer Agent (Recommended)

**Approach:** Modify the existing Workflow Engineer agent to support both local and cloud execution.

**Changes Required:**
1. Remove or make `target` optional (enable both environments)
2. Add context detection logic (VS Code vs GitHub)
3. Adjust tool references to use environment-agnostic tools where possible
4. Add cloud-specific sections for autonomous task execution
5. Keep `model` and `handoffs` properties (ignored by cloud, useful for local)

**Pros:**
- ✅ Single agent definition maintains consistency
- ✅ Local workflow remains unchanged
- ✅ Cloud capability is additive, not disruptive
- ✅ Maintainer can choose execution mode per task

**Cons:**
- ⚠️ Agent instructions become more complex (context branching)
- ⚠️ Testing requires validation in both environments

**Implementation Effort:** Low-Medium (1-2 hours)

### 5.2 Option B: Create Separate Cloud Workflow Agent

**Approach:** Create a new `workflow-automation.agent.md` specifically for cloud execution, keep existing Workflow Engineer for local use.

**Changes Required:**
1. Create `.github/agents/workflow-automation.agent.md` with `target: github-copilot`
2. Adapt Workflow Engineer instructions for cloud context
3. Remove VS Code-specific tool references
4. Focus on issue-driven task execution
5. Update `docs/agents.md` to document both agents

**Pros:**
- ✅ Clean separation of concerns
- ✅ No impact on existing local workflow
- ✅ Specialized instructions for each context
- ✅ Easier to maintain and test independently

**Cons:**
- ⚠️ Duplication of instruction content
- ⚠️ Must keep both agents synchronized over time
- ⚠️ Maintainer must remember which agent to use when

**Implementation Effort:** Medium (2-3 hours)

### 5.3 Option C: Phased Approach (Start with Option B, Consolidate Later)

**Approach:** Begin with separate agents for learning and experimentation, consolidate once patterns are established.

**Phase 1 (Immediate):**
1. Create cloud-specific `workflow-automation.agent.md`
2. Test cloud agent with 1-2 simple workflow tasks
3. Document lessons learned

**Phase 2 (After validation):**
1. Assess whether dual-mode agent (Option A) is viable
2. If yes, merge agents; if no, keep separate

**Pros:**
- ✅ Low-risk experimentation
- ✅ Learn cloud agent patterns before committing
- ✅ Can pivot based on real-world experience

**Cons:**
- ⚠️ Temporary duplication during Phase 1
- ⚠️ Additional work if consolidation is chosen

**Implementation Effort:** Medium, staged (Phase 1: 2-3 hours, Phase 2: 1-2 hours)

---

## 6. Recommended Approach

**Recommendation: Option A (Enhanced Workflow Engineer Agent)**

**Rationale:**
1. **Minimal Disruption:** Local workflow continues unchanged; cloud capability is additive.
2. **Single Source of Truth:** One agent definition reduces maintenance burden.
3. **Flexibility:** Maintainer can choose execution mode (local chat vs cloud issue) per task.
4. **Simplicity:** No new agent to document, no workflow branching in `docs/agents.md`.
5. **Proven Pattern:** Other repositories successfully use dual-mode agents.

**Implementation Steps:**
1. ✅ Backup current `workflow-engineer.agent.md`
2. ✅ Remove `target: vscode` line (enable both environments)
3. ✅ Add "Context Detection" section to agent instructions
4. ✅ Add conditional logic for VS Code-specific features (handoffs, model)
5. ✅ Add cloud-specific workflow section (issue parsing, autonomous execution)
6. ✅ Update `docs/agents.md` to document cloud agent usage
7. ✅ Test in VS Code (verify no regression)
8. ✅ Create test issue and assign to `@copilot` (validate cloud execution)
9. ✅ Document findings and finalize approach

---

## 7. Cloud Agent Workflow for Workflow Improvements

### 7.1 Proposed Cloud Workflow

**Triggering a Cloud Agent Task:**
```
1. Maintainer creates GitHub issue:
   Title: "[Workflow] Update all agent models to Gemini 3 Flash"
   Body: 
     - Background: Cost optimization based on latest benchmarks
     - Scope: Update model property in all .github/agents/*.agent.md files
     - Acceptance: All agents use Gemini 3 Flash, docs updated
   
2. Maintainer assigns issue to @copilot
3. Cloud agent (Workflow Engineer) picks up task
4. Agent executes:
   - Reads issue specification
   - Reviews current agent files
   - Updates model properties
   - Updates docs/agents.md
   - Runs validation (if applicable)
5. Agent creates PR:
   - Branch: workflow/030-update-agent-models (auto-generated)
   - Commits: Follows conventional commit format
   - Description: References issue, lists changes, explains rationale
6. Maintainer reviews PR:
   - Check changes meet issue requirements
   - Approve/request changes via PR comments
7. Maintainer merges PR (or agent auto-merges if configured)
```

### 7.2 Advantages Over Local Workflow

**Parallel Execution:**
- Multiple workflow improvements can run concurrently
- Maintainer doesn't wait for completion

**Consistency:**
- Automated agents follow instructions precisely
- Less human error in repetitive tasks

**Audit Trail:**
- All decisions documented in issue/PR thread
- Easy to track what changed and why

**Time Savings:**
- Maintainer delegates routine work
- Focus on high-value activities (design, review, decision-making)

### 7.3 When to Stay Local

Some workflow improvements still benefit from local execution:

- ❌ **Exploratory Analysis:** When requirements are unclear, local chat enables iterative refinement.
- ❌ **Complex Design Decisions:** Architectural changes require Maintainer input at each step.
- ❌ **Rapid Prototyping:** Testing multiple approaches interactively before committing.
- ❌ **Learning/Training:** Understanding new concepts through guided conversation.

**Rule of Thumb:**
- **Local:** If you need to guide the agent or expect multiple clarifying questions.
- **Cloud:** If the task is clearly specified and can be executed autonomously.

---

## 8. Tool Considerations for Cloud Agents

### 8.1 Tools Available in Both Environments

✅ **Safe for Dual-Mode Agents:**
- `search` (code search)
- `web` (web search)
- `github/*` (GitHub operations - PRs, issues, repos)
- `memory/*` (memory storage, if configured)

### 8.2 VS Code-Only Tools (Avoid in Cloud Agents)

❌ **Not Available in GitHub Cloud Environment:**
- `execute/runInTerminal` → Cloud agents can't run shell commands interactively
- `execute/testFailure` → Testing must be done via GitHub Actions workflows
- `read/problems` → No VS Code problem pane in cloud
- `read/terminalLastCommand` → No terminal history
- `edit` (direct file edit) → Cloud agents use PR-based changes
- `todo` → VS Code-specific UI
- `copilot-container-tools/*` → Local Docker/container tools
- `io.github.chromedevtools/chrome-devtools-mcp/*` → Local browser tools

### 8.3 GitHub-Specific Tools (Cloud Agents)

✅ **Available to Cloud Agents (Not in VS Code):**
- GitHub Actions workflow triggers
- Repository security scanning integration
- GitHub Issues/Projects API
- Team/organization permissions
- Branch protection and required checks

### 8.4 Recommended Tool Configuration for Dual-Mode Agents

**Minimal Safe Set (Both Environments):**
```yaml
tools: ['search', 'web', 'github/*']
```

**Conditional Tool Instructions (In Agent Content):**
```markdown
## Tools

Depending on your execution environment:

**VS Code (Local):**
- Use `@workspace` for file searches
- Use `edit` tool for direct file modifications
- Use terminal tools for testing: `execute/runInTerminal`
- Iterate with Maintainer via chat

**GitHub (Cloud):**
- Use `search` for code/file searches
- Propose changes in PR description/comments
- Rely on GitHub Actions for testing
- Document decisions for Maintainer PR review
```

---

## 9. Modifications Required for Current Agents

### 9.1 Workflow Engineer Agent Changes

**File:** `.github/agents/workflow-engineer.agent.md`

**Minimal Changes (Option A - Recommended):**

1. **Remove target restriction:**
   ```diff
   ---
   description: Analyze, improve, and maintain the agent workflow
   name: Workflow Engineer
   - target: vscode
   model: GPT-5.2
   tools: [...]
   ---
   ```

2. **Add context detection section:**
   ```markdown
   ## Execution Context
   
   Determine your environment at the start of each interaction:
   
   ### VS Code (Local/Interactive)
   - You are in an interactive chat session with the Maintainer
   - Use handoff buttons to navigate to other agents
   - Iterate and refine based on Maintainer feedback
   - Use VS Code tools (edit, execute, todo)
   - Follow existing workflow patterns
   
   ### GitHub (Cloud/Automated)
   - You are processing a GitHub issue assigned to @copilot
   - Work autonomously following issue specification
   - Create a pull request with your changes
   - Document all decisions in PR description
   - Use GitHub-safe tools (search, web, github/*)
   ```

3. **Add cloud workflow section:**
   ```markdown
   ## Cloud Agent Workflow (GitHub Issues)
   
   When executing as a cloud agent:
   
   1. **Parse Issue:** Extract task specification from issue body
   2. **Validate Scope:** Ensure task is well-defined and within capabilities
   3. **Read Context:** Review relevant docs, agent files, specifications
   4. **Execute Changes:** Modify files according to task requirements
   5. **Create PR:** 
      - Branch: `workflow/<NNN>-<slug>`
      - Commits: Conventional format
      - Description: Standard template (Problem/Change/Verification)
   6. **Request Review:** Assign PR to Maintainer or relevant reviewers
   ```

4. **Update tool guidance:**
   ```markdown
   ## Tool Usage
   
   **Both Environments:** `search`, `web`, `github/*`
   
   **VS Code Only:** `edit`, `execute/*`, `todo`, `read/problems`
   → If in cloud, describe changes in PR instead of using edit tool
   
   **GitHub Only:** GitHub Actions, repository security scanning
   → If in VS Code, use local testing and manual PR creation
   ```

### 9.2 Documentation Updates

**File:** `docs/agents.md`

**New Section (After "Agent Skills"):**

```markdown
## Cloud Agents vs Local Agents

The tfplan2md workflow supports both **local agents** (running in VS Code) and **cloud agents** (running on GitHub infrastructure).

### Local Agents (Interactive)
- **Invocation:** `@agent-name` in VS Code Copilot Chat
- **Use Case:** Interactive development, design decisions, debugging
- **Output:** Chat responses, local file edits
- **Best For:** Tasks requiring Maintainer guidance and iteration

### Cloud Agents (Automated)
- **Invocation:** Assign GitHub issue to `@copilot`
- **Use Case:** Well-scoped automation, batch updates, routine tasks
- **Output:** Pull requests with code changes
- **Best For:** Tasks with clear specifications that can run autonomously

### Dual-Mode Agents
Most agents support both execution modes. The agent detects its context and adapts behavior accordingly.

**Example: Workflow Engineer**
- **Local:** Interactive workflow analysis, design discussions, complex decisions
- **Cloud:** Automated workflow improvements from GitHub issues (e.g., batch agent updates)

See [docs/workflow/031-cloud-agents-analysis/](./workflow/031-cloud-agents-analysis/) for detailed analysis.
```

**Update to Workflow Engineer Role:**
```markdown
### 12. Workflow Engineer (Meta-Agent)
- **Goal:** Analyze, improve, and maintain the agent-based workflow.
- **Execution Modes:**
  - **Local (VS Code):** Interactive workflow analysis with Maintainer guidance
  - **Cloud (GitHub):** Automated execution of well-defined workflow improvements
- **Deliverables:** Updated agent definitions, workflow documentation, PRs with changes.
- **Definition of Done:** Changes documented, validated, and PR created.
- **Note:** Can operate in both local (chat) and cloud (issue) contexts.
```

---

## 10. Advanced Features: Multi-Agent Handoffs and Label-Based Routing

### 10.1 Overview

Cloud agents support two advanced features that enable sophisticated automation workflows:
1. **Agent Handoffs:** Sequential workflows where one agent passes context to another
2. **Label-Based Routing:** Automatic agent selection based on GitHub issue labels

Both features enable more complex, multi-step automation while maintaining clarity and human oversight.

---

### 10.2 Agent Handoffs Configuration

#### 10.2.1 What Are Handoffs?

Agent handoffs allow one agent to transfer execution to another agent, maintaining context and optionally pre-filling prompts. This enables multi-step workflows like:
- Requirements Engineer → Architect → Quality Engineer → Developer
- Issue Analyst → Developer → Code Reviewer
- Workflow Engineer → Developer → Release Manager

#### 10.2.2 Handoff Configuration Syntax

Handoffs are defined in the agent's YAML frontmatter using the `handoffs` property:

```yaml
---
name: Requirements Engineer
description: Gather and document feature requirements
target: vscode  # VS Code only (GitHub ignores handoffs property)
tools: ['search', 'edit', 'web']
handoffs:
  - label: "Design Architecture"
    agent: "Architect"
    prompt: "Review the feature specification and design the architecture."
    send: false
  - label: "Skip to Development"
    agent: "Developer"
    prompt: "Implement the feature based on the specification."
    send: false
---
```

**Property Definitions:**
- `label`: Button text shown in VS Code (e.g., "Design Architecture")
- `agent`: Target agent name (must match another agent's `name` property)
- `prompt`: Pre-filled message for the target agent
- `send`: If `true`, auto-submits prompt; if `false`, user reviews first

#### 10.2.3 How Handoffs Work

**In VS Code (Local):**
1. Agent completes its task (e.g., Requirements Engineer writes specification)
2. Handoff buttons appear in chat interface
3. Maintainer clicks button (e.g., "Design Architecture")
4. VS Code opens chat with Architect agent
5. Prompt is pre-filled with context
6. Maintainer reviews and sends (or edits first if `send: false`)

**In GitHub Cloud (Current Limitations):**
- **Important:** The `handoffs` property is currently **ignored by GitHub cloud agents**
- Cloud agents do not support interactive handoff buttons
- Multi-agent workflows in cloud require different orchestration:
  - Option 1: Agent creates sub-issues for next steps (manually assigned)
  - Option 2: Agent mentions next agent in PR comment (requires manual delegation)
  - Option 3: Use GitHub Actions workflows to chain agents (advanced)

#### 10.2.4 Cloud Agent Multi-Step Workflow (Alternative Pattern)

Since cloud agents don't support the `handoffs` property, use this pattern instead:

**Pattern: Issue-Driven Sequential Workflow**

```markdown
## Workflow Engineer Instructions (Cloud Context)

When completing a workflow improvement:

1. **Complete Primary Task:** Make the requested changes
2. **Create Sub-Issues for Next Steps:** If follow-up work is needed:
   ```
   Title: [Implementation] Implement workflow changes from #<parent-issue>
   Body:
     - Parent Issue: #<parent-issue>
     - Task: Implement the workflow changes documented in <folder>
     - Assignee: @copilot (or specific agent if configured)
   ```
3. **Document in PR:** List any sub-issues created in the PR description
4. **Request Review:** Assign PR to Maintainer with clear next steps

This enables chaining without relying on the `handoffs` property.
```

#### 10.2.5 Example: Complete Handoff Chain (VS Code Only)

```yaml
# .github/agents/requirements-engineer.agent.md
---
name: Requirements Engineer
handoffs:
  - label: "Design Architecture"
    agent: "Architect"
    prompt: "Review the specification and design the solution."
    send: false
---
```

```yaml
# .github/agents/architect.agent.md
---
name: Architect
handoffs:
  - label: "Define Tests"
    agent: "Quality Engineer"
    prompt: "Create test plan based on the architecture."
    send: false
---
```

```yaml
# .github/agents/quality-engineer.agent.md
---
name: Quality Engineer
handoffs:
  - label: "Start Development"
    agent: "Developer"
    prompt: "Implement features according to architecture and test plan."
    send: false
---
```

**Result:** Maintainer can progress through the entire workflow with guided transitions, each agent receiving appropriate context.

---

### 10.3 Label-Based Agent Selection

#### 10.3.1 Overview

Label-based routing enables automatic agent selection when issues are assigned to `@copilot`. Issues labeled "workflow" trigger the Workflow Engineer, "bug" triggers Issue Analyst, etc.

#### 10.3.2 Configuration Approach

**Method 1: Agent Instructions (Recommended)**

Configure each agent to self-select based on issue labels:

```markdown
## Requirements Engineer Agent Instructions

### Context Detection

When invoked as a cloud agent (GitHub issue):

1. **Check Issue Labels:**
   - If issue has label `feature` or `enhancement`: Proceed
   - If issue has label `bug`: Respond "This issue should be handled by the Issue Analyst agent. Please reassign."
   - If issue has label `workflow`: Respond "This issue should be handled by the Workflow Engineer agent. Please reassign."

2. **Validate Scope:** Ensure task is requirements gathering...
```

**Method 2: Organization-Level Routing (Advanced)**

Use GitHub API or Actions workflow to route based on labels:

```yaml
# .github/workflows/agent-router.yml
name: Agent Router
on:
  issues:
    types: [assigned]

jobs:
  route:
    if: github.event.assignee.login == 'copilot[bot]'
    runs-on: ubuntu-latest
    steps:
      - name: Route by Label
        run: |
          LABELS="${{ join(github.event.issue.labels.*.name, ',') }}"
          
          if [[ "$LABELS" == *"workflow"* ]]; then
            # Add instruction comment for Workflow Engineer
            gh issue comment ${{ github.event.issue.number }} --body "@copilot Use the Workflow Engineer agent."
          elif [[ "$LABELS" == *"bug"* ]]; then
            gh issue comment ${{ github.event.issue.number }} --body "@copilot Use the Issue Analyst agent."
          elif [[ "$LABELS" == *"feature"* ]]; then
            gh issue comment ${{ github.event.issue.number }} --body "@copilot Use the Requirements Engineer agent."
          fi
```

#### 10.3.3 Example Label-to-Agent Mapping

| Issue Label | Target Agent | Use Case |
|-------------|--------------|----------|
| `workflow` | Workflow Engineer | Agent workflow improvements |
| `feature` | Requirements Engineer | New feature requests |
| `bug` | Issue Analyst | Bug investigation and analysis |
| `website` | Web Designer | Website content/design changes |
| `docs` | Technical Writer | Documentation updates |
| `security` | Code Reviewer | Security vulnerability fixes |
| `refactor` | Developer | Code refactoring tasks |

#### 10.3.4 Issue Template with Label Guidance

```markdown
---
name: Workflow Improvement
about: Suggest improvements to the agent workflow
labels: workflow
assignees: copilot
---

## Workflow Improvement Request

**Current Behavior:**
Describe the current workflow process that needs improvement.

**Proposed Improvement:**
Describe the desired improvement.

**Expected Outcome:**
What should change after this improvement?

---

**Note:** This issue will be handled by the Workflow Engineer agent.
```

#### 10.3.5 Agent Decision Logic

Agents can detect their appropriate scope through instructions:

```markdown
## Issue Analyst Agent

### Cloud Workflow

When processing a GitHub issue:

1. **Validate Label:** Check if issue has `bug`, `incident`, or `issue` label
2. **If Label Missing:** 
   - Comment: "This issue is missing a 'bug' label. Is this a bug report?"
   - Wait for Maintainer confirmation
3. **If Wrong Label:**
   - Comment: "This issue has label 'feature' which should be handled by Requirements Engineer."
   - Suggest reassignment
4. **If Correct Label:**
   - Proceed with analysis...
```

---

### 10.4 Combining Handoffs and Labels

#### 10.4.1 Hybrid Workflow

**Scenario:** Feature development with automatic routing

1. **Issue Created:** Labeled `feature`, assigned to `@copilot`
2. **Requirements Engineer (Cloud):** Analyzes requirements, creates specification
3. **Requirements Engineer (Cloud Action):** Creates follow-up issues:
   - Issue: "[Architecture] Design solution for feature X" (label: `architecture`)
   - Issue: "[Implementation] Implement feature X" (label: `implementation`)
4. **Architect (Cloud):** Picks up architecture issue via label routing
5. **Developer (Cloud):** Picks up implementation issue via label routing

**Benefits:**
- Automated routing reduces manual assignment
- Clear separation of concerns via labels
- Each agent works independently
- Maintainer approves PRs from each phase

#### 10.4.2 Local to Cloud Transition

**Pattern:** Start locally, finish in cloud

1. **Local (VS Code):** Requirements Engineer works with Maintainer interactively
2. **Local:** Architect designs solution with Maintainer feedback
3. **Handoff to Cloud:** Create GitHub issue for implementation, assign to `@copilot`
4. **Cloud:** Developer implements autonomously, creates PR
5. **Local:** Code Reviewer reviews PR via VS Code

**Best of Both Worlds:**
- Complex decisions made locally with guidance
- Routine implementation automated in cloud
- Human oversight at key decision points

---

### 10.5 Implementation Recommendations

#### 10.5.1 Start Simple

**Phase 1: VS Code Handoffs Only**
- Implement handoffs in local agents first
- Test workflow transitions in VS Code
- Validate context preservation
- **Outcome:** Proven handoff patterns

**Phase 2: Label-Based Routing**
- Add label detection to agent instructions
- Create issue templates with default labels
- Test with simple workflow improvements
- **Outcome:** Automatic agent selection working

**Phase 3: Cloud Multi-Step Workflows**
- Implement sub-issue creation pattern
- Test end-to-end cloud workflow
- Monitor for issues with context loss
- **Outcome:** Automated sequential workflows

#### 10.5.2 Best Practices

**Handoffs:**
- ✅ Always use `send: false` for review opportunities
- ✅ Keep prompts concise and specific
- ✅ Test handoff chains before deploying
- ✅ Document handoff paths in `docs/agents.md`
- ⚠️ Limit chains to 3-4 agents (avoid complexity)
- ⚠️ Provide "skip ahead" handoffs for flexibility
- ❌ Don't rely on `handoffs` property for cloud agents (not supported)

**Label-Based Routing:**
- ✅ Use consistent label names across repository
- ✅ Document label-to-agent mappings
- ✅ Provide clear error messages for wrong labels
- ✅ Create issue templates with pre-set labels
- ⚠️ Handle unlabeled issues gracefully
- ⚠️ Allow manual override of routing
- ❌ Don't assume labels are always correct (validate in agent)

---

### 10.6 Limitations and Workarounds

#### 10.6.1 Current Limitations

**Cloud Agent Handoffs:**
- ❌ `handoffs` property ignored by GitHub cloud agents
- ❌ No interactive handoff buttons in cloud context
- ❌ Cannot pass context via handoff mechanism
- **Workaround:** Use sub-issues and PR comments

**Label-Based Routing:**
- ⚠️ Requires manual label addition or issue templates
- ⚠️ No built-in GitHub routing (requires custom logic)
- ⚠️ Ambiguous when multiple labels present
- **Workaround:** Agent validates labels and requests clarification

**Session Timeouts:**
- ⚠️ 59-minute timeout per cloud agent session
- ⚠️ Long workflows may timeout mid-execution
- **Workaround:** Break into smaller sub-tasks with separate issues

#### 10.6.2 Future Improvements

As GitHub Copilot evolves, these features may improve:
- Native cloud agent handoff support
- Built-in label-based routing
- Extended session timeouts
- Better context preservation across agents

---

## 11. Testing and Validation Plan

### 10.1 Phase 1: Local Agent Regression Testing

**Objective:** Ensure modified Workflow Engineer agent still works in VS Code.

**Steps:**
1. ✅ Start VS Code chat with `@workflow-engineer`
2. ✅ Request a simple workflow analysis (e.g., review agent tool names)
3. ✅ Verify agent responds appropriately
4. ✅ Check handoff buttons work
5. ✅ Validate agent can edit files and commit changes

**Success Criteria:**
- No degradation of existing functionality
- Agent recognizes VS Code context
- Handoffs and tools work as before

### 10.2 Phase 2: Cloud Agent Validation

**Objective:** Validate Workflow Engineer agent can execute in cloud context.

**Steps:**
1. ✅ Create test issue:
   ```
   Title: [Workflow] Test cloud agent execution
   Body:
     Task: Review all agent descriptions and ensure they are under 100 characters.
     Acceptance: PR created with any necessary description updates.
   ```
2. ✅ Assign issue to `@copilot`
3. ✅ Monitor cloud agent execution via GitHub Actions logs (if available)
4. ✅ Review generated PR:
   - Branch naming follows convention
   - Commits use conventional format
   - PR description follows template
   - Changes match issue specification
5. ✅ Approve/merge PR or provide feedback via comments

**Success Criteria:**
- Cloud agent successfully picks up issue
- Agent recognizes cloud context
- PR is created with correct structure
- Changes address issue requirements

### 10.3 Phase 3: Edge Case Testing

**Test Cases:**
1. **Ambiguous issue specification** → Agent should request clarification (via PR comment)
2. **Issue out of scope** → Agent should decline and explain why
3. **Complex change requiring judgment** → Agent should recommend local execution
4. **Tool not available in cloud** → Agent should adapt workflow (document in PR vs edit directly)

---

## 12. Migration Path and Rollout Strategy

### 11.1 Immediate (This PR)

✅ **Deliverables:**
1. This analysis document (`docs/workflow/031-cloud-agents-analysis/cloud-agents-analysis.md`)
2. No changes to agents yet (analysis only)

**Outcome:** Maintainer reviews analysis and approves approach.

### 11.2 Phase 1: Enable Dual-Mode Workflow Engineer (Next PR)

✅ **Deliverables:**
1. Updated `workflow-engineer.agent.md` (remove target, add context detection)
2. Updated `docs/agents.md` (cloud agents section)
3. Regression testing results (local VS Code validation)

**Timeline:** 1-2 hours

### 11.3 Phase 2: Cloud Agent Validation (Following PR)

✅ **Deliverables:**
1. Test issue created and assigned to `@copilot`
2. Cloud agent execution results documented
3. PR review and merge (if successful)
4. Lessons learned document (edge cases, tool limitations)

**Timeline:** 2-3 hours (includes monitoring and review)

### 11.4 Phase 3: Expand to Other Agents (Optional, Future)

⚠️ **Evaluate After Phase 2:**
- Based on cloud agent test results, determine which other agents benefit from cloud execution
- Candidates: Developer (automated refactoring), Technical Writer (batch doc updates), Code Reviewer (automated PR reviews)
- Update agents incrementally, one at a time

**Timeline:** Per-agent basis (1-2 hours each)

---

## 13. Risks and Mitigations

### 13.1 Risk: Cloud Agent Misinterprets Instructions

**Impact:** Cloud agent makes incorrect changes, creates confusing PR.

**Mitigation:**
- Start with simple, well-defined tasks
- Test with non-critical workflow improvements first
- Always require Maintainer PR review before merge
- Document expectations clearly in issue descriptions

### 13.2 Risk: Tool Incompatibility Breaks Agent

**Impact:** Agent fails due to using VS Code-only tools in cloud context.

**Mitigation:**
- Use only common tools (`search`, `web`, `github/*`) in agent tool list
- Add context detection and tool adaptation logic in agent instructions
- Test in cloud environment during Phase 2

### 13.3 Risk: Local Workflow Regression

**Impact:** Changes to support cloud agents break existing VS Code behavior.

**Mitigation:**
- Regression test in VS Code before releasing changes
- Keep `model` and `handoffs` properties (ignored by cloud, used by local)
- Document local vs cloud behavior clearly in agent instructions

### 13.4 Risk: Increased Complexity

**Impact:** Dual-mode agents become hard to understand and maintain.

**Mitigation:**
- Use clear sectioning ("VS Code Context" vs "GitHub Context")
- Provide examples of each execution mode
- Consider separate agents if complexity becomes unmanageable (fall back to Option B)

### 13.5 Risk: GitHub Actions Quota Exhaustion

**Impact:** Cloud agents consume all free GitHub Actions minutes.

**Mitigation:**
- Monitor Actions usage in repository settings
- Use cloud agents sparingly (only for well-scoped tasks)
- Fall back to local execution if quota concerns arise

---

## 14. Success Metrics

### 14.1 Adoption Metrics (After Phase 3)

📊 **Track:**
- Number of workflow improvements executed via cloud agents (target: 25% of routine tasks)
- Time saved by Maintainer (estimate: 2-3 hours/week)
- Number of cloud agent PRs requiring rework (target: <20%)

### 14.2 Quality Metrics

📊 **Track:**
- Cloud agent PR approval rate (target: >80%)
- Issues closed per cloud agent PR (target: 1:1 ratio)
- Rework cycles per cloud agent task (target: <2 on average)

### 14.3 Workflow Health Metrics

📊 **Track:**
- Local agent usage (should remain stable or increase)
- Maintainer satisfaction (qualitative feedback)
- Documentation completeness (all cloud agent runs documented)

---

## 15. Conclusion

Cloud agents offer a powerful complement to the existing local agent workflow in tfplan2md. By enhancing the Workflow Engineer agent to support both local (VS Code) and cloud (GitHub) execution, we can:

1. **Maintain Current Workflow:** All existing local agents continue to work unchanged.
2. **Add Automation Capability:** Well-defined workflow improvements can be delegated to cloud agents.
3. **Preserve Flexibility:** Maintainer chooses execution mode (local vs cloud) per task.
4. **Reduce Maintenance Burden:** Single agent definition reduces duplication.

**Next Steps:**
1. ✅ Review this analysis with Maintainer
2. ✅ Approve approach (recommend Option A)
3. ✅ Implement Phase 1 (update Workflow Engineer agent)
4. ✅ Test in both environments (Phase 2)
5. ✅ Document lessons learned and expand to other agents (Phase 3, optional)

Cloud agents are not a replacement for local agents—they're an addition that enables automation of routine tasks while preserving the interactive, guided workflow that makes tfplan2md development effective.

---

## Appendix A: Example Cloud Agent Issue Template

```markdown
### [Workflow] Update agent model assignments based on latest benchmarks

**Background:**
The ai-model-reference.md was updated on 2026-01-05 with new benchmark data. Several agents can benefit from model reassignments for better performance or cost efficiency.

**Task:**
Update the `model` property in the following agent files based on recommendations in `docs/ai-model-reference.md`:
- Quality Engineer: Gemini 3 Flash (cost-effective, strong instruction following)
- Task Planner: Gemini 3 Flash (cost-effective, strong instruction following)
- Release Manager: Gemini 3 Flash (cost-effective, routine task)

**Scope:**
- Edit `.github/agents/quality-engineer.agent.md`
- Edit `.github/agents/task-planner.agent.md`
- Edit `.github/agents/release-manager.agent.md`
- Update `docs/agents.md` if model assignments are documented there

**Acceptance Criteria:**
- [ ] All three agents use specified models
- [ ] Commits follow conventional commit format
- [ ] PR description explains rationale for changes
- [ ] No other unrelated changes included

**Out of Scope:**
- Do NOT update other agents not listed above
- Do NOT change agent instructions (only model property)

**Assignee:** @copilot
```

---

## Appendix B: Example Cloud Agent PR Template

Cloud agents should create PRs following this structure:

```markdown
## Problem
The ai-model-reference.md (updated 2026-01-05) recommends using Gemini 3 Flash for agents with high instruction-following needs and cost sensitivity. Quality Engineer, Task Planner, and Release Manager fit this profile but currently use higher-cost models.

## Change
Updated `model` property in three agent files:
- `quality-engineer.agent.md`: GPT-5.2 → Gemini 3 Flash (Preview)
- `task-planner.agent.md`: GPT-5.1 → Gemini 3 Flash (Preview)
- `release-manager.agent.md`: Gemini 3 Pro → Gemini 3 Flash (Preview)

**Rationale:**
- Gemini 3 Flash scores 74.86 on Instruction Following (vs 65.85 for Gemini 3 Pro)
- Premium multiplier: 0.33x (vs 1x for GPT/Gemini Pro)
- Suitable for template-based tasks (test plans, task lists, release notes)

## Verification
- [x] All three agent files updated with new model
- [x] No other changes introduced
- [x] Commits follow conventional format (`refactor(agents): update models for cost efficiency`)
- [x] Referenced issue #<issue-number> in commit message

## Related
- Issue: #<issue-number>
- Reference: `docs/ai-model-reference.md`
```

---

## Appendix C: Workflow Engineer Agent Diff Preview

**Preview of changes for Option A (Dual-Mode Agent):**

```diff
--- .github/agents/workflow-engineer.agent.md
+++ .github/agents/workflow-engineer.agent.md
@@ -2,7 +2,6 @@
 description: Analyze, improve, and maintain the agent workflow
 name: Workflow Engineer
-target: vscode
 model: GPT-5.2
 tools: ['vscode', 'execute', 'read', 'edit', 'search', 'web', ...]
 ---
@@ -15,6 +14,30 @@
 
 Evolve and optimize the agent workflow...
 
+## Execution Context
+
+Determine your environment at the start of each interaction:
+
+### VS Code (Local/Interactive)
+- You are in an interactive chat session with the Maintainer
+- Use handoff buttons to navigate to other agents
+- Iterate and refine based on Maintainer feedback
+- Use VS Code tools (edit, execute, todo)
+- Follow existing workflow patterns
+
+### GitHub (Cloud/Automated)
+- You are processing a GitHub issue assigned to @copilot
+- Work autonomously following issue specification
+- Create a pull request with your changes
+- Document all decisions in PR description
+- Use GitHub-safe tools (search, web, github/*)
+
+**How to detect context:**
+- VS Code: You receive a chat message in VS Code Copilot Chat
+- GitHub: Your input is a GitHub issue body with a task specification
+
+If uncertain, ask: "Are you running me in VS Code or via a GitHub issue?"
+
 ## Boundaries
 
 ### ✅ Always Do
@@ -50,6 +73,28 @@
 ...existing boundaries...
 
+## Cloud Agent Workflow (GitHub Issues)
+
+When executing as a cloud agent:
+
+1. **Parse Issue:** Extract task specification from issue body
+2. **Validate Scope:** Ensure task is well-defined and within capabilities
+   - If ambiguous, comment on issue requesting clarification
+   - If out of scope, comment explaining why and suggest alternative
+3. **Read Context:** Review relevant docs, agent files, specifications
+4. **Execute Changes:** Modify files according to task requirements
+5. **Create PR:**
+   - Branch: `workflow/<NNN>-<slug>`
+   - Commits: Conventional format
+   - Description: Standard template (Problem/Change/Verification)
+6. **Request Review:** Assign PR to Maintainer or relevant reviewers
+
+**Cloud Limitations:**
+- Cannot use `edit` tool directly; propose changes via PR
+- Cannot run terminal commands; rely on GitHub Actions for testing
+- Cannot iterate with Maintainer in real-time; document decisions in PR
+
+If task requires interactive guidance, recommend local execution in issue comment.
+
 ## Workflow
 
 ### 1. Understand the Request
```

---

**End of Analysis**
