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

### Quality Engineer
- **Date:** 2025-02-20
- **Summary:** Created comprehensive test plan covering unit tests (BuildDefinitionViewModelFactory, BuildDefinitionMapper) and integration tests (template rendering). Defined 23 test cases covering all acceptance criteria including secret masking, semantic diffing, conditional rendering, and all nested blocks (variables, CI trigger, repository, PR trigger, schedules). Created UAT test plan specifying feature-specific test artifact requirements and validation instructions for Maintainer review.
- **Artifacts Produced:**
  - `docs/features/094-build-definition-tables/test-plan.md` - Complete test plan with 23 test cases, edge cases, test data requirements, and traceability matrix
  - `docs/features/094-build-definition-tables/uat-test-plan.md` - UAT test plan with feature-specific artifact specification and detailed validation instructions for GitHub and Azure DevOps
- **Problems Encountered:** None
- **Key Testing Areas:**
  - Unit tests for ViewModel factory (create/update/delete, regular/secret variables, semantic diffing, large values)
  - Unit tests for nested blocks (CI trigger, repository, PR trigger, schedules, jobs)
  - Integration tests for template rendering (all operation types, conditional rendering, style guide compliance)
  - Security tests (secret value masking in all scenarios including transitions)
  - Edge cases (empty collections, null values, case-insensitive matching)
  - Performance and backwards compatibility tests
- **Test Data Requirements:**
  - New file: `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuredevops-build-definitions.json` with 6 test scenarios
  - New DemoPaths entry: `AzureDevOpsBuildDefinitionPlanPath`
  - UAT artifact: `docs/features/094-build-definition-tables/uat-plan.json` and `uat-plan.md` (to be created by Developer)

### Task Planner
- **Date:** 2025-02-20
- **Summary:** Created detailed implementation tasks document breaking down the feature into 16 actionable tasks following the azuredevops_variable_group pattern. Tasks cover the complete implementation pipeline: ViewModel → Extractors → Formatters → Change Builders → Factory → Mapper → Template → Registration → Tests → UAT.
- **Artifacts Produced:**
  - `docs/features/094-build-definition-tables/tasks.md` - Complete implementation plan with 16 prioritized tasks, each with specific acceptance criteria, dependencies, file locations, and test requirements
- **Problems Encountered:** None
- **Key Implementation Sequence:**
  1. Core data pipeline (Tasks 1-5): ViewModel, Extractors, Formatters, Change Builders, Factory
  2. Framework integration (Tasks 6-10): Factory adapter, Mapper, Template, Embedded resource, Registration
  3. Testing (Tasks 11-13, 16): Unit tests, integration tests, test data, verification
  4. Finalization (Tasks 14-15): UAT artifacts, demo paths
- **Critical Security Requirements Emphasized:**
  - Task 3: Secret masking logic - `is_secret: true` variables MUST always display `(sensitive / hidden)`
  - Task 12 (TC-17): Security integration test - verify no secret values leak in any scenario
- **Pattern Adherence:**
  - All tasks explicitly reference the `azuredevops_variable_group` implementation as the pattern to follow
  - Each task specifies exact file locations and component names
  - Dependencies clearly mapped to ensure correct implementation order
