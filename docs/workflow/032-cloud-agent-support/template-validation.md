# Workflow Improvement Issue Template Validation

This document validates the workflow improvement issue template by simulating how it would be filled out for various real-world scenarios.

---

## Test Case 1: Agent Instruction Fix (Simple)

**Improvement Type:** Agent modification

**Current Situation:**
The Quality Engineer agent creates test plans in the wrong folder because instructions reference an incorrect path pattern (`docs/test-plans/` instead of `docs/features/NNN-<feature-slug>/test-plan.md`).

**Proposed Solution:**
Update `.github/agents/quality-engineer.agent.md` line 45 to reference the correct path: `docs/features/NNN-<feature-slug>/test-plan.md`

**Scope:**
- `.github/agents/quality-engineer.agent.md` (line 45, update path)
- `docs/agents.md` (Quality Engineer section, document artifact location)

**Why This Matters:**
Prevents wasted time recreating test plans in the correct location. Improves agent reliability and reduces manual corrections.

**Acceptance Criteria:**
- [x] Quality Engineer agent uses correct path in instructions
- [x] Test plan artifact location documented in docs/agents.md
- [x] Existing test plans remain in current locations (no migration)
- [x] PR follows conventional commit format

**Out of Scope:**
- Do NOT modify other agents' paths
- Do NOT migrate existing test plans

**Related Work:**
- Retrospective: docs/workflow/022-improvements-2025-12-27/improvements-2025-12-27.md #1

**Priority:** High
**Complexity:** Low

**✅ Template Effectiveness:** Excellent - All information needed for autonomous implementation is present. Agent can immediately make the change without questions.

---

## Test Case 2: New Script Creation (Complex)

**Improvement Type:** Script/automation enhancement

**Current Situation:**
We lack a way to validate agent definitions before committing, leading to broken handoffs (referencing non-existent agents) and invalid tool references (snake_case names that VS Code silently ignores).

**Proposed Solution:**
Create a validation script `scripts/validate-agents.py` that checks:
- All handoff agent names exist (cross-reference .github/agents/ files)
- All tool IDs follow VS Code naming conventions (no snake_case)
- Model names match available models in docs/ai-model-reference.md
- All required YAML frontmatter fields are present
- Descriptions are ≤100 characters

**Scope:**
- Create new file: `scripts/validate-agents.py`
- Update `.github/agents/workflow-engineer.agent.md` (add validation step to workflow)
- Update `docs/agents.md` (document validation process)
- Optional: Add pre-commit hook reference

**Why This Matters:**
Catches configuration errors at commit time rather than runtime, preventing broken workflows and failed handoffs. Reduces debugging time and improves workflow reliability.

**Acceptance Criteria:**
- [x] validate-agents.py script exists and is executable
- [x] Script checks handoffs, tools, models, and descriptions
- [x] Script returns non-zero exit code on validation failure
- [x] Script prints clear error messages identifying issues
- [x] Documentation updated with validation instructions
- [x] All current agents pass validation
- [x] Workflow Engineer instructions reference validation script

**Out of Scope:**
- Do NOT auto-fix issues (only report them)
- Do NOT enforce model assignments (only validate they exist)
- Do NOT check agent instruction content quality

**Related Work:**
- Multiple retrospectives identified broken handoffs as recurring issue

**Priority:** High
**Complexity:** Medium

**✅ Template Effectiveness:** Excellent - Clear specification of requirements, scope, and acceptance criteria. Agent can implement autonomously, though may ask clarifying questions about error message format or edge cases.

---

## Test Case 3: Model Assignment Update (Batch Change)

**Improvement Type:** Agent modification

**Current Situation:**
The ai-model-reference.md was updated on 2026-01-05 with new benchmark data. Several agents can benefit from model reassignments for better performance or cost efficiency.

**Proposed Solution:**
Update the `model` property in the following agent files based on recommendations in `docs/ai-model-reference.md`:
- Quality Engineer: Gemini 3 Flash (cost-effective, strong instruction following score: 74.86)
- Task Planner: Gemini 3 Flash (cost-effective, strong instruction following score: 74.86)
- Release Manager: Gemini 3 Flash (cost-effective, routine task)

**Scope:**
- `.github/agents/quality-engineer.agent.md` (model property only)
- `.github/agents/task-planner.agent.md` (model property only)
- `.github/agents/release-manager.agent.md` (model property only)
- `docs/agents.md` (update model assignments if documented)

**Why This Matters:**
Reduces costs while maintaining or improving performance. These agents follow templates and benefit from Gemini 3 Flash's strong instruction-following capabilities.

**Acceptance Criteria:**
- [x] All three agents use Gemini 3 Flash
- [x] Model name format: "Gemini 3 Flash (Preview)" (exact match)
- [x] Commits follow conventional commit format
- [x] PR description explains rationale with benchmark scores
- [x] No other unrelated changes included

**Out of Scope:**
- Do NOT update other agents not listed above
- Do NOT change agent instructions (only model property)
- Do NOT add new tools or modify handoffs

**Related Work:**
- docs/ai-model-reference.md (updated 2026-01-05)
- Analysis: Model Selection Guidelines in workflow-engineer.agent.md

**Priority:** Medium
**Complexity:** Low

**✅ Template Effectiveness:** Excellent - Precise specification with exact model names, clear scope boundaries, and specific "do not" constraints. Agent can implement without questions.

---

## Test Case 4: Process Documentation Update

**Improvement Type:** Documentation update

**Current Situation:**
The docs/agents.md file doesn't document the new cloud agent capability. Local and cloud execution contexts are not explained, making it unclear when to use each mode.

**Proposed Solution:**
Add a new section "Cloud Agents vs Local Agents" to docs/agents.md that explains:
- What cloud agents are (GitHub issue assignment to @copilot)
- What local agents are (VS Code chat with @agent-name)
- When to use each mode
- Tool availability differences
- Context detection approach

**Scope:**
- `docs/agents.md` (add new section after "Workflow Overview")
- Update Workflow Engineer description to mention dual-mode capability
- Link to docs/workflow/031-cloud-agents-analysis/ for details

**Why This Matters:**
Users need to understand when and how to use cloud agents. Without documentation, the feature won't be used effectively or may cause confusion.

**Acceptance Criteria:**
- [x] New "Cloud Agents vs Local Agents" section added
- [x] Section explains both modes clearly
- [x] Tool availability table included
- [x] Workflow Engineer description updated
- [x] Links to analysis document
- [x] No contradictions with existing documentation

**Out of Scope:**
- Do NOT modify agent definitions (only documentation)
- Do NOT add implementation details (link to analysis instead)

**Related Work:**
- Analysis: docs/workflow/031-cloud-agents-analysis/cloud-agents-analysis.md
- Implementation: PR #XXX (cloud agent support)

**Priority:** High
**Complexity:** Low

**✅ Template Effectiveness:** Good - Clear requirements, but agent might ask about section placement or specific wording preferences. Could be more specific about content structure.

---

## Test Case 5: New Agent Creation

**Improvement Type:** New agent creation

**Current Situation:**
We have no specialized agent for website design and development. The Developer agent handles website work but lacks domain-specific instructions for accessibility, responsive design, and GitHub Pages deployment.

**Proposed Solution:**
Create a new Web Designer agent (`.github/agents/web-designer.agent.md`) with:
- Goal: Design, develop, and maintain the tfplan2md website
- Responsibilities: HTML/CSS development, design prototypes, accessibility (WCAG 2.1 AA)
- Tools: Standard web development tools, website-specific scripts
- Boundaries: Website files only (/website/ directory)
- Handoffs: From Architect (for new features), to Code Reviewer (for validation)

**Scope:**
- Create `.github/agents/web-designer.agent.md`
- Update `docs/agents.md`:
  - Add Web Designer to Mermaid diagram
  - Add agent role section
  - Add website artifacts to artifacts table
  - Add handoff criteria
- Select appropriate model (consult docs/ai-model-reference.md)

**Why This Matters:**
Specialized agent brings focused expertise on web design, accessibility, and GitHub Pages. Improves website quality and frees Developer agent from context-switching.

**Acceptance Criteria:**
- [x] Web Designer agent file created with all sections
- [x] Agent uses appropriate model for web development tasks
- [x] Mermaid diagram includes Web Designer node and connections
- [x] docs/agents.md fully updated with new agent
- [x] Handoffs from Architect and to Code Reviewer defined
- [x] Boundaries clearly prevent work outside /website/ directory

**Out of Scope:**
- Do NOT implement website features (agent creation only)
- Do NOT modify existing agents' responsibilities
- Do NOT change CI/CD workflows yet (separate task)

**Related Work:**
- Feature request: #XX (website overhaul)
- Architecture: docs/features/YYY-website-redesign/architecture.md

**Priority:** Medium
**Complexity:** High

**✅ Template Effectiveness:** Good - Provides overall vision and requirements, but agent will likely ask questions about specific responsibilities, tool selections, and handoff details. This is acceptable for complex tasks - the template gives enough structure to start, and the agent can ask follow-up questions via issue comments (as designed for cloud agents).

---

## Validation Summary

### Template Strengths ✅

1. **Clear Structure:** Separate fields for problem, solution, scope, and acceptance criteria prevent confusion
2. **Specific Scope:** Dedicated field for "which files" enables precise targeting
3. **Boundary Setting:** "Out of Scope" field prevents scope creep
4. **Actionable Criteria:** Checkbox format for acceptance criteria makes completion clear
5. **Context Linking:** Related work field connects to retrospectives and analysis
6. **No Redundancy:** Each field asks for distinct information:
   - Current Situation = the problem
   - Proposed Solution = how to fix it
   - Scope = which files
   - Why This Matters = impact/rationale
   - Acceptance Criteria = definition of done
   - Out of Scope = boundaries

### Template Effectiveness by Improvement Type

| Type | Autonomous Implementation | Typical Questions | Verdict |
|------|--------------------------|-------------------|---------|
| Simple agent instruction fix | ✅ Yes | 0 | Excellent |
| Batch model updates | ✅ Yes | 0 | Excellent |
| Script creation | ✅ Mostly | 1-2 (error format, edge cases) | Very Good |
| Documentation update | ⚠️ Partial | 2-3 (placement, structure) | Good |
| New agent creation | ⚠️ Partial | 3-5 (responsibilities, tools) | Good |

### Improvement Opportunities 💡

None identified - template balances thoroughness with ease of use. For complex tasks (new agents, major refactors), some follow-up questions are expected and desirable. The cloud agent workflow supports multiple questions via comments, so this is by design.

### Final Assessment

**✅ Template is production-ready** - It enables users to create issues that are:
- Detailed enough for autonomous implementation (simple/medium tasks)
- Structured enough to guide agents on complex tasks
- Easy to fill out (no redundant questions, clear prompts)
- Actionable (clear acceptance criteria and boundaries)

The template successfully avoids asking duplicate questions while providing sufficient structure for agents to work effectively.
