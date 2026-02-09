# Clarification Needed: VS Code `models` Array Feature

## Problem Statement

From Feature 065 retrospective:
> VS Code silently switched to `azure/Azure/Kimi-K2.5` during GitHub Copilot availability outage (0% success rate, 2 failed requests). The maintainer was not notified of the switch.
> 
> **Recommendation**: Use the new VS Code custom agent `models` list to specify a primary and fallback model for each agent. This prevents VS Code from silently falling back to unsuitable models.

## Research Findings

**CRITICAL**: Current VS Code custom agents do **NOT** support a `models` array feature in YAML frontmatter (as of February 2026).

### Evidence

1. **Official VS Code Documentation** ([Custom Agents](https://code.visualstudio.com/docs/copilot/customization/custom-agents)):
   - Only supports single `model:` field (string value)
   - No mention of array or list syntax
   - No native fallback mechanism documented

2. **GitHub Documentation** ([Custom Agent Configuration Reference](https://docs.github.com/en/copilot/reference/custom-agents-configuration)):
   - Explicitly states: "The `model`, `argument-hint`, and `handoffs` properties from VS Code... are currently not supported for Copilot coding agent on GitHub.com"
   - Only single model string supported

3. **Web Research** (multiple sources, January-February 2026):
   - Confirmed no `models` array syntax exists
   - Only one model can be specified per agent
   - No automatic fallback mechanism available
   - Community discussions confirm this limitation

### Current YAML Format

What **IS** supported:
```yaml
---
name: Developer (coding agent)
model: GPT-5.2-Codex
---
```

What **IS NOT** supported:
```yaml
---
name: Developer (coding agent)
models:
  - GPT-5.2-Codex
  - GPT-5.2
  - GPT-5 mini
---
```

## Questions for Maintainer

Before proceeding with implementation, I need clarification:

### Option 1: Unreleased Feature?
Do you have access to a preview/beta version of VS Code that includes this feature? If so:
- What version/build?
- What is the exact YAML syntax?
- Are there any limitations or requirements?

### Option 2: Alternative Solutions?
Would you like me to implement alternative approaches such as:

**A. Documentation-based solution:**
- Document recommended fallback models for each agent
- Create troubleshooting guide for model availability issues
- Add monitoring/alerting procedures

**B. Manual failover procedures:**
- Script to quickly switch all agents to fallback models
- Guidance on detecting silent model switches
- Best practices for model selection during outages

**C. Improved model assignments:**
- Review and optimize current model selections
- Ensure each agent has a clear "next best" model documented
- Add rationale for each model choice to agent instructions

### Option 3: Different Interpretation?
Is there a different feature or approach you had in mind that I may have misunderstood?

## Current Agent Model Assignments

For reference, here are the current model assignments across all agents:

| Model | Agent(s) | Count |
|-------|----------|-------|
| **GPT-5.2-Codex** | Developer | 2 |
| **GPT-5.2** | Architect, Issue Analyst, Workflow Engineer | 6 |
| **Claude Sonnet 4.5** | Code Reviewer, Requirements Engineer, Technical Writer, Web Designer | 8 |
| **Gemini 3 Flash (Preview)** | Quality Engineer, Release Manager, Retrospective, Task Planner, UAT Tester, Workflow Orchestrator | 12 |

## Proposed Next Steps

Once you provide clarification, I can:

1. **If Option 1** (unreleased feature exists):
   - Add `models` arrays to all 30 agent files
   - Select appropriate fallback models based on ai-model-reference.md
   - Document the feature in docs/agents.md

2. **If Option 2** (alternative solutions):
   - Implement chosen alternative approach
   - Update relevant documentation
   - Create any needed scripts or procedures

3. **If Option 3** (different interpretation):
   - Adjust approach based on your guidance

## Status

**🚧 BLOCKED** - Waiting for maintainer clarification before proceeding.

---

**Prepared by**: Workflow Engineer (coding agent)
**Date**: 2026-02-09
**Branch**: `copilot/add-primary-fallback-models`
