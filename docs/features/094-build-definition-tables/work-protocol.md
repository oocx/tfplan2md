# Work Protocol: Azure DevOps Build Definition Tables

**Work Item:** `docs/features/094-build-definition-tables/`
**Branch:** `copilot/add-build-definition-tables-again`
**Workflow Type:** Feature
**Created:** 2025-02-20

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2025-02-20
- **Summary:** Gathered requirements for displaying azuredevops_build_definition nested blocks as structured tables, following the pattern established by azuredevops_variable_group. Created Feature Specification based on Terraform registry documentation and existing codebase patterns.
- **Artifacts Produced:** 
  - `docs/features/094-build-definition-tables/specification.md`
  - `docs/features/094-build-definition-tables/work-protocol.md`
- **Problems Encountered:** None

### Architect
- **Date:** 2025-02-20
- **Summary:** Analyzed the existing azuredevops_variable_group pattern and designed technical architecture for build definition table rendering. The design follows the exact same pattern (ViewModel → Factory → Extractor → Formatter → Change Builder → Mapper → Template) with additional block types beyond variables. No new ADR required as this directly applies the established pattern.
- **Artifacts Produced:**
  - `docs/features/094-build-definition-tables/architecture.md` - Complete technical design with component structure, secret masking logic, semantic diffing approach, and template structure
- **Problems Encountered:** None
- **Key Decisions:**
  - Follow variable_group pattern exactly (ViewModel, Factory, Extractors, Formatters, Change Builders, Mapper, Template)
  - Semantic diffing for variables (match by name), simple before/after display for other blocks
  - Secret masking: `is_secret: true` → `(sensitive / hidden)` in Value column
  - Conditional rendering: only show tables when blocks contain data
  - 8 new files + 2 modified files following existing provider structure
