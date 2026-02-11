# Work Protocol: Code Reviewer Agent Instruction Improvements

## Metadata
- **Work Item Type**: Workflow Improvement
- **Work Item ID**: 069
- **Branch**: `copilot/improve-code-reviewer-instructions`
- **Related Issue**: #446
- **Status**: Awaiting Maintainer Decision

## Problem Statement

During Feature 068 (Parent-Child Resource Grouping), the Code Reviewer agent missed a critical issue where the Azure AD group template was missing the `{{ include "/_child_resources.sbn" }}` directive, causing ALL member tables to be absent from the generated output. The issue was only caught during UAT.

**Root Cause**: The reviewer focused on complex configuration reference matching logic and trusted that existing snapshot tests proved the feature worked, without manually generating and inspecting the actual rendered output.

## Proposed Solution

Implement 5 instructional improvements to the Code Reviewer agent based on the root cause analysis from Feature 068:

1. **Template Verification Checklist** (CRITICAL) - Forces direct inspection of template files
2. **Mandatory Manual Artifact Generation** (CRITICAL) - Prevents trusting snapshots blindly  
3. **Simplest Test Case First** (HIGH) - Focuses on core before edge cases
4. **Line-by-Line Spec-to-Output Comparison** (HIGH) - Ensures examples match reality
5. **Distinguish Test Data vs Implementation Issues** (MEDIUM) - Avoids misdiagnosis

## Work Status

**Current State**: Task list created, awaiting Maintainer decision on which improvements to implement.

**Decision Required**: Maintainer needs to select from:
- **Option 1**: All 5 improvements (comprehensive, ~30-45 min)
- **Option 2**: IDs 1, 2, 4 only (critical improvements, ~20-30 min)  
- **Option 3**: ID 2 only (highest impact single item, ~10 min)
- **Custom**: Specify task IDs

See `docs/workflow/069-code-reviewer-template-verification/tasks.md` for detailed task descriptions.

## Agent Work Log

### 2026-02-11 - Workflow Engineer (coding agent)

**Tasks Completed:**
- ✅ Read and analyzed current Code Reviewer agent instructions  
- ✅ Reviewed the 5 proposed improvements from issue #446
- ✅ Analyzed Feature 068 root cause documentation (code-review-post-uat-fixes.md, retrospective.md)
- ✅ Created workflow folder: `docs/workflow/069-code-reviewer-template-verification/`
- ✅ Created prioritized task list with 3 recommended options

**Artifacts Produced:**
- `docs/workflow/069-code-reviewer-template-verification/tasks.md` - Detailed task list with rationale, impact, effort, and risk assessment
- `docs/workflow/069-code-reviewer-template-verification/work-protocol.md` - This document

**Status**: ⏸️ **BLOCKED** - Awaiting Maintainer decision on which option to implement

**Next Steps**: Once Maintainer selects option, implement approved improvements to `.github/agents/code-reviewer-coding-agent.agent.md`

**Problems Encountered**: None
