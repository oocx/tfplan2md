---
name: arc42-documentation
description: Create comprehensive architecture documentation using the arc42 template structure (12 sections covering introduction, constraints, context, solution strategy, building blocks, runtime, deployment, concepts, decisions, quality, risks, and glossary).
---

# arc42 Architecture Documentation

## Purpose
Generate complete, structured architecture documentation following the proven arc42 template (https://arc42.org/). The template provides 12 standardized sections covering all aspects of software architecture, from stakeholder requirements to technical implementation details.

## When to Use This Skill
- Starting a new project that needs comprehensive architecture documentation
- Documenting existing systems that lack structured architecture descriptions
- Creating architecture overviews for stakeholders with different technical levels
- Preparing architecture documentation for certification (e.g., iSAQB)
- Establishing a consistent documentation standard across multiple projects

## arc42 Template Overview

The arc42 template consists of 12 sections:

1. **Introduction and Goals** - Requirements overview, quality goals, stakeholders
2. **Constraints** - Technical and organizational limitations
3. **Context and Scope** - System boundaries, external interfaces
4. **Solution Strategy** - Fundamental architectural decisions
5. **Building Block View** - Static decomposition (components, modules)
6. **Runtime View** - Dynamic behavior (scenarios, workflows)
7. **Deployment View** - Infrastructure and technical environment
8. **Crosscutting Concepts** - Recurring patterns and principles
9. **Architecture Decisions** - Important ADRs with rationale
10. **Quality Requirements** - Quality tree and scenarios
11. **Risks and Technical Debt** - Known issues and limitations
12. **Glossary** - Domain and technical terminology

## Hard Rules

### Must
- Create documentation in `docs/architecture/arc42/` directory
- Use the template structure provided in `templates/arc42-template.md`
- Fill in all 12 sections (mark sections as "TBD" if information is not yet available)
- Include references to existing ADRs in Section 9 (Architecture Decisions)
- Update `docs/architecture.md` to reference the new arc42 documentation
- Tailor content to the specific project context (remove boilerplate explanations)
- Use Mermaid diagrams for visual representations (context, building blocks, deployment)
- Keep stakeholder-focused sections (1, 3, 10) accessible to non-technical readers
- Minimize terminal approvals by batching file operations

### Must Not
- Copy arc42 help text verbatim into the final document
- Create documentation that duplicates existing ADRs without adding value
- Skip sections without marking them as "TBD" or "Not Applicable"
- Use arc42 as a substitute for code-level documentation
- Create arc42 docs for trivial features (use standard ADRs instead)

## Actions

### 1. Assess Documentation Scope
Ask the maintainer:
- Is this for the entire system or a specific subsystem/feature?
- Are there existing ADRs that should be referenced in Section 9?
- What level of detail is needed (high-level overview vs. detailed technical spec)?
- Who are the primary stakeholders (developers, architects, management)?

### 2. Create Directory Structure
```bash
mkdir -p docs/architecture/arc42
```

### 3. Generate arc42 Document
Copy the template and customize for the project:
```bash
# Use the template from this skill
# The template is in templates/arc42-template.md
```

Read the template file, then create the customized version at `docs/architecture/arc42/architecture.md`.

### 4. Fill Core Sections (Iteratively)
Work through sections in recommended order:
1. Start with **Section 1** (Introduction and Goals) - easiest to fill from spec.md
2. Add **Section 3** (Context and Scope) - define system boundaries
3. Document **Section 4** (Solution Strategy) - key architectural decisions
4. Complete **Section 5** (Building Block View) - component structure
5. Fill remaining sections based on available information

**Progressive approach:** It's acceptable to mark sections as "TBD" and fill them over time. The document is a living artifact.

### 5. Add Visual Diagrams
For key sections, create Mermaid diagrams:
- **Section 3:** Context diagram (C4 Level 1)
- **Section 5:** Component diagram (C4 Level 2/3)
- **Section 6:** Sequence diagrams for critical scenarios
- **Section 7:** Deployment diagram

Use the `mcp-mermaid` tool to preview diagrams before committing.

### 6. Link Existing ADRs
In **Section 9 (Architecture Decisions)**:
- List all existing ADRs from `docs/adr-*.md`
- Provide a brief summary of each decision
- Link to the full ADR file

### 7. Validate Completeness
Check that:
- [ ] All 12 sections exist (even if marked TBD)
- [ ] At least sections 1, 3, 4, 5 are filled with meaningful content
- [ ] Diagrams are included for context and building blocks
- [ ] Glossary includes domain-specific terms
- [ ] No arc42 help text remains in the document

### 8. Update Documentation Index
Add reference to the arc42 document in `docs/architecture.md`:
```markdown
## Comprehensive Architecture Documentation

For a complete architecture overview following the arc42 standard, see:
- [arc42 Architecture Documentation](architecture/arc42/architecture.md)
```

### 9. Commit the Documentation
```bash
git add docs/architecture/arc42/ docs/architecture.md
git commit -m "docs: add arc42 architecture documentation"
```

## Maintenance Guidelines

The arc42 document should be:
- **Updated when**: Major architectural changes occur, new quality requirements emerge, significant risks are identified
- **Reviewed**: During sprint planning or architecture review meetings
- **Versioned**: Commit changes with clear messages linking to features/ADRs
- **Evolved**: It's better to have incomplete sections marked "TBD" than to skip documentation entirely

## References

| Resource | Description |
|----------|-------------|
| [arc42.org](https://arc42.org/) | Official arc42 website with downloads and examples |
| [arc42 Docs](https://docs.arc42.org/) | Detailed explanations of each section with practical tips |
| [arc42 by Example](https://arc42.org/examples) | Real-world architecture documentation examples |
| [arc42 on GitHub](https://github.com/arc42/arc42-template) | Template source repository |
| [iSAQB Curriculum](https://www.isaqb.org/) | Certification program that uses arc42 |

## Tips for Success

1. **Start small**: Fill sections 1, 3, 4 first, then expand
2. **Use diagrams**: A good context diagram is worth 1000 words
3. **Link, don't duplicate**: Reference existing ADRs, don't copy them
4. **Tailor to audience**: Adjust detail level per section based on stakeholders
5. **Keep it current**: Update when architecture changes, not on a schedule
6. **Version control**: Commit documentation with related code changes
