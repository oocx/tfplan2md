# Cloud Agents Analysis

**Created:** January 5, 2026  
**Status:** Analysis Complete

## Overview

This folder contains the comprehensive analysis of GitHub Copilot cloud agents and how they can be integrated into the tfplan2md workflow alongside the existing local agent workflow.

## Documents

- **[cloud-agents-analysis.md](./cloud-agents-analysis.md)** - Complete analysis covering:
  - Core differences between cloud and local agents
  - Target property configuration
  - Use case analysis and when to use each type
  - Tool considerations and compatibility
  - Three proposed implementation options
  - Recommended approach (Option A: Dual-Mode Workflow Engineer)
  - Testing and validation plan
  - Migration path and rollout strategy
  - Risk analysis and mitigations
  - Success metrics
  - Example templates for issues and PRs

## Key Findings

1. **Cloud agents and local agents are complementary**, not competitive
2. **Local agents** excel at interactive, iterative development in VS Code
3. **Cloud agents** excel at automated, asynchronous background tasks
4. **Both can coexist** using the same agent definitions with proper `target` configuration
5. **Workflow Engineer agent** is the ideal first candidate for dual-mode support

## Recommended Next Steps

1. Review the analysis document
2. Approve the recommended approach (Option A)
3. Implement Phase 1: Update Workflow Engineer agent for dual-mode operation
4. Test in both environments (local VS Code and cloud GitHub)
5. Document lessons learned
6. Optionally expand to other agents based on results

## Related Documentation

- [docs/agents.md](../../agents.md) - Current workflow documentation
- [docs/ai-model-reference.md](../../ai-model-reference.md) - Model selection reference
- [.github/agents/workflow-engineer.agent.md](../../../.github/agents/workflow-engineer.agent.md) - Current Workflow Engineer definition
