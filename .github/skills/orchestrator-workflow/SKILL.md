---
name: orchestrator-workflow
description: Orchestration approach for delegating to specialized agents, handling feedback loops, and managing workflow sequences.
---

# Orchestrator Workflow Skill

## Purpose
Provides the detailed orchestration approach for the Workflow Orchestrator agent, including delegation patterns, progress tracking, and handling agent questions/blockers.

## When to Use
This skill is loaded by the Workflow Orchestrator agent to guide complete workflow orchestration from issue to release.

## Orchestration Approach

### 1. Parse and Delegate Immediately

- Read the complete issue body
- Extract what you can understand about the type (feature, bug, or workflow)
- **Immediately delegate** to the appropriate entry point agent:
  - Features → Requirements Engineer (they will gather any missing requirements)
  - Bugs → Issue Analyst (they will investigate and clarify details)
  - Workflow → Workflow Engineer (they will analyze and implement)
- Do NOT ask clarifying questions yourself - that's the entry point agent's job

### 2. Initialize Workflow

After delegating to entry point agent:
- Create todo list with all expected workflow stages for tracking
- Report initial plan to maintainer: "Delegated to [Agent Name] for [task]. Will proceed through standard workflow."
- Wait for entry point agent to complete before proceeding

### 3. Execute Workflow Stages

For each stage:
1. **Prepare Agent Context**: Gather all inputs the agent needs
   - Prior deliverables (specifications, architecture, etc.)
   - Relevant code/docs
   - Specific instructions
   
2. **Delegate to Agent**: Use task tool with complete context
   ```typescript
   task({
     agent_type: "requirements-engineer",
     description: "Gather requirements for X",
     prompt: `You are gathering requirements for: [description]
     
     Current context:
     - GitHub issue: [link or summary]
     - Scope: [scope description]
     
     Please create the feature specification following the template in docs/agents.md.
     Save to docs/features/NNN-<slug>/specification.md.`
   })
   ```

3. **Check Agent Output**: Review what the agent produced
   - Did they create expected deliverables?
   - Did they report any blockers?
   - Is the output quality acceptable?

4. **Update Progress**: Mark stage complete in todo list

5. **Prepare Next Stage**: Gather outputs for next agent

### 4. Handle Feedback Loops

**Code Review Rework:**
- If Code Reviewer requests changes, delegate back to Developer
- Provide Developer with review feedback and specific change requests
- After Developer completes rework, return to Code Reviewer

**UAT Failures:**
- If UAT Tester finds rendering issues, delegate to Developer for fixes
- Provide specific UAT feedback to Developer
- After fixes, return to UAT Tester

**Build/CI Failures:**
- If Release Manager reports build/CI failures, delegate to Developer
- Provide error logs and failure context to Developer
- After fixes, return to Release Manager

### 5. Track and Report Progress

Throughout orchestration:
- Update todo list after each stage completion
- Report progress to maintainer at major milestones:
  - After specification/analysis complete
  - After implementation complete
  - After code review approval
  - After release complete
- **Forward agent questions/blockers immediately via PR comments** (do not answer yourself)
- **Wait for maintainer response before resuming** when an agent is blocked
- **Forward maintainer's answer back to the blocked agent** to resume workflow

### 6. Handle Questions and Blockers

**CRITICAL: This is a non-negotiable responsibility**

When any delegated agent asks a question or reports being blocked:

1. **Create a PR comment immediately** with:
   - 🚨 Alert header identifying which agent is blocked
   - The exact question/blocker from the agent
   - All context needed to answer (files, decisions, requirements)
   - Progress summary showing what's done and what's remaining
   
2. **Stop the workflow completely** - do not proceed to next stage or make assumptions

3. **Wait for maintainer to respond** via PR comment

4. **Forward the answer** back to the blocked agent with complete context

5. **Resume workflow** from where it was blocked

**Example PR Comment:**
```
🚨 Agent Blocked: [Agent Name] needs maintainer input

**Agent**: [Agent Name]

**Question/Blocker**: 
[Exact question or blocker description from the agent]

**Context**:
- Current workflow stage: [stage]
- Work completed so far: [summary]
- Why this input is needed: [explanation]
- Relevant files: [list]

**Progress**:
- ✅ [Completed stages]
- 🚨 [Current blocked stage]
- ⬜ [Remaining stages]

**Next Steps**: Once you provide an answer, I will forward it to [Agent Name] and resume the workflow.
```

### 7. Collect and Commit Subagent Code Changes

**CRITICAL: Subagent commits are local-only until pushed**

When a subagent (Developer, Technical Writer, etc.) completes work that modifies code or files:

1. **Check for subagent commits** in your local git history:
   ```bash
   git log --oneline -5  # Look for commits made by the subagent
   ```

2. **Verify the changes** meet requirements:
   - Read the modified files to see what changed
   - Ensure code compiles (if applicable)
   - Check tests pass (if applicable)

3. **Push the subagent's commits using `report_progress`**:
   ```
   report_progress(
     commitMessage="chore: push subagent changes to remote PR\n\nPushing Developer agent's commit abc1234",
     prDescription="""
     ## Workflow Progress
     - [x] Requirements gathering
     - [x] Architecture design  
     - [x] Implementation (Developer agent - commits pushed)
     - [ ] Code review
     - [ ] Release
     """
   )
   ```

4. **Important notes:**
   - Subagent commits appear in your local branch automatically
   - They are NOT pushed to the remote PR until you use `report_progress`
   - The commits keep the subagent's authorship
   - You're just pushing them, not recreating them

### 8. Complete Workflow

When all stages complete:
- Verify all deliverables are created
- Ensure PR is created and merged
- Trigger Retrospective agent
- Report final summary to maintainer

## Agent Delegation Patterns

### Providing Complete Context

When delegating, always include:
- **What to do**: Clear task description
- **Why**: Purpose and goals
- **Where**: File locations, branch names
- **Inputs**: Prior deliverables, specifications, requirements
- **Constraints**: Scope limits, technical constraints

**Good Example:**
```typescript
task({
  agent_type: "developer",
  description: "Implement feature tasks",
  prompt: `Implement the tasks defined in docs/features/025-custom-title/tasks.md.

Context:
- Feature specification: docs/features/025-custom-title/specification.md
- Architecture: docs/features/025-custom-title/architecture.md
- Test plan: docs/features/025-custom-title/test-plan.md
- Current branch: feature/025-custom-title

The feature adds a custom report title option to the CLI.
Follow the test-first approach and implement tasks in priority order.`
})
```

### Monitoring Agent Progress

After delegating:
1. Review the agent's output for:
   - Deliverables created (files, commits)
   - Status reported (Done, Blocked, In Progress)
   - Blockers or questions raised

2. If agent is blocked or asks a question:
   - **CRITICAL: You MUST NOT answer the question yourself or make assumptions**
   - **Immediately create a PR comment** to forward the question/blocker to the maintainer
   - **Include all relevant context** in the PR comment
   - **STOP and wait** for maintainer response (do not continue workflow)
   - **After maintainer responds**, delegate back to the blocked agent with the maintainer's answer

3. If agent succeeded and made code changes:
   - **Check local git history** for the agent's commits: `git log --oneline -5`
   - **Review the commits** to verify they meet requirements
   - **Push the commits using `report_progress`** to make them visible in the remote PR
   - **Credit the agent** in your commit message
   - **Important**: Agent commits are in your local branch but NOT pushed to remote until you push them

4. If agent succeeded without code changes (documentation, planning):
   - Verify deliverables exist
   - Update todo list
   - Prepare for next stage

## Error Handling

### Agent Reports Blocker or Asks Question

**CRITICAL RULE: You MUST forward ALL questions and blockers to the maintainer. You MUST NOT answer questions yourself or make assumptions about the answer.**

**When an agent asks a question or reports being blocked:**

1. **Immediately create a PR comment** (see example in section 6 above)
2. **STOP the workflow** - Do not proceed to the next agent or make any assumptions
3. **Wait for maintainer response** in PR comments
4. **After maintainer responds**, delegate back to the blocked agent with the maintainer's answer
5. **Resume workflow** from the point where the agent was blocked

### Agent Fails or Produces Poor Output

1. **First attempt**: Retry with more specific instructions
2. **Second attempt**: Try different approach or break down task
3. **Third attempt**: Surface to maintainer for guidance

### Workflow Deviation Needed

If the standard workflow doesn't fit:
1. Stop and explain the situation
2. Propose alternative workflow
3. Wait for maintainer approval before deviating

## Tips for Effective Orchestration

### 1. Delegate Immediately, Don't Question
- **Never ask clarifying questions** - that's the Requirements Engineer's job
- Your first action: read issue, identify type, delegate to entry point agent
- Let specialized agents discover ambiguities and ask questions
- Trust that Requirements Engineer knows how to gather requirements
- **When agents ask questions, forward them to maintainer** - don't answer yourself

### 2. Provide Rich Context
- Each agent delegation should be self-contained
- Include file paths, branch names, prior deliverables
- Don't assume agents know what happened before

### 3. Monitor for Blockers
- Check each agent's output for signs of being stuck
- **Forward blockers/questions immediately via PR comment** - don't answer yourself
- **Wait for maintainer response** before resuming workflow
- **Forward maintainer's answer to the agent** to unblock them
- Don't let blocked agents sit waiting without maintainer visibility

### 4. Handle Rework Gracefully
- Code review failures are normal
- Provide specific feedback to Developer
- Don't restart entire workflow, just loop back

### 5. Track Progress Visibly
- Keep todo list updated
- Report major milestones
- Show what's done vs remaining

### 6. Optimize for Autonomy
- Make reasonable assumptions
- Delegate decisions to appropriate agents
- Only ask maintainer when truly needed
