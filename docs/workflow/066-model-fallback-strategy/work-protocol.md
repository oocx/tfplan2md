# Work Protocol: Model Fallback Strategy (Workflow 066)

**Work Item**: docs/workflow/066-model-fallback-strategy
**Branch**: `copilot/add-primary-fallback-models`
**Workflow Type**: Workflow
**Issue**: Add explicit model lists (primary + fallback) to all agent definitions
**Status**: 🚧 Blocked - Awaiting maintainer clarification

## Context

From Feature 065 retrospective:
> VS Code silently switched to `azure/Azure/Kimi-K2.5` during GitHub Copilot availability outage (0% success rate, 2 failed requests). The maintainer was not notified of the switch.
> 
> **Recommendation**: Use the new VS Code custom agent `models` list to specify a primary and fallback model for each agent. This prevents VS Code from silently falling back to unsuitable models.

## Agent Work Log

### 2026-02-09 22:32 UTC — Workflow Engineer (Research Phase)

**Summary**: Researched VS Code custom agent capabilities for model fallback. **CRITICAL FINDING**: The `models` array feature mentioned in the retrospective does NOT exist in current VS Code (Feb 2026).

**Work Completed**:
- ✅ Reviewed all 30 agent files to understand current model assignments
- ✅ Researched official VS Code and GitHub documentation
- ✅ Performed web search for `models` array feature
- ✅ Reviewed AI model reference documentation
- ✅ Documented research findings with evidence

**Key Findings**:
1. VS Code only supports single `model:` string field in YAML frontmatter
2. No `models` array syntax exists in current release
3. No native fallback mechanism available
4. Official documentation confirms this limitation
5. Feature mentioned in retrospective appears to not exist

**Artifacts Produced**:
- `research-findings.md` - Comprehensive research documentation with evidence
- `work-protocol.md` - This file

**Problems Encountered**:
- Issue requests implementation of non-existent feature
- Need maintainer clarification on:
  - Whether they have preview/beta VS Code with this feature
  - Whether alternative solutions are acceptable
  - What the actual requirement is

**Next Steps**:
- ⏸️ **BLOCKED**: Waiting for maintainer clarification
- Cannot proceed with implementation until requirement is clarified

**Current Agent**: Workflow Engineer
**Next Agent**: TBD (pending maintainer response)

---

## Clarification Questions for Maintainer

See `research-findings.md` for full details. Key questions:

1. **Do you have a preview VS Code build** with `models` array support?
2. **Would alternative solutions work?** (documentation, scripts, procedures)
3. **Did I misunderstand the requirement?**

Please respond via PR comment or issue update.
