# Work Protocol: Terraform Outputs

**Work Item:** `docs/features/097-terraform-outputs/`
**Branch:** TBD (will be created by GitHub Copilot)
**Workflow Type:** Feature
**Created:** 2025-02-23

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2025-02-23
- **Summary:** Created feature specification for Terraform outputs support. Documented output data model, positioning strategy, table format, sensitivity masking, display name mapping integration, and CLI flag requirements.
- **Artifacts Produced:** 
  - `docs/features/097-terraform-outputs/specification.md`
  - `docs/features/097-terraform-outputs/work-protocol.md`
- **Key Requirements Clarified:**
  - Module outputs appear after module resource changes (4th-level header)
  - Global outputs appear in dedicated section after all resources (2nd-level header)
  - 4-column table format: Name, Description, Sensitive, Value
  - Sensitive values masked by default as `(sensitive value)`
  - `--show-sensitive` CLI flag to reveal sensitive values when needed
  - Output values leverage existing display name mappings automatically (Azure IDs, principals, subscriptions, roles)
  - Alphabetical ordering within each outputs section
  - Different actions (create/update/delete/no-op) handled with appropriate value display
  - Computed values show `(known after apply)` when value is unknown
- **Open Questions for Architect:**
  - Data model extension approach (extend TerraformPlan vs on-demand parsing)
  - Metadata correlation strategy (output_changes + configuration outputs)
  - Module output parsing approach (upfront mapping vs on-demand query)
  - Value rendering pipeline integration (reuse existing vs special handling)
  - Sensitivity detection precedence (configuration vs output_changes)
  - Update action display (before→after now vs future enhancement)
- **Problems Encountered:** None. Terraform plan JSON structure is well-documented, existing display name mapping features provide clear integration points.
- **Next Steps:** Handoff to Architect for technical design

### Architect
- **Date:** 2025-02-23
- **Summary:** Designed technical architecture for Terraform outputs support. Extended parsing layer with `OutputChanges` and `OutputChange` record, created `OutputChangeModel` for report model layer, extended `ModuleChangeGroup` and `ReportModel` to include outputs, designed value formatting pipeline integration, and specified template structure. All open questions from Requirements Engineer have been resolved with documented design decisions.
- **Artifacts Produced:**
  - `docs/features/097-terraform-outputs/architecture.md` - Complete technical architecture design
- **Key Design Decisions:**
  - **Parsing:** Extend `TerraformPlan` with optional `OutputChanges` property (`IReadOnlyDictionary<string, OutputChange>?`)
  - **Model Layer:** New `OutputChangeModel` with pre-computed masking flag (`IsMasked`), `ModuleChangeGroup.Outputs`, `ReportModel.GlobalOutputs`
  - **Correlation:** Navigate configuration JSON structure during model building to correlate output names with module addresses and metadata
  - **Value Formatting:** Reuse existing `ValueFormatterRegistry` pipeline to automatically apply display name mappings
  - **Sensitivity:** Use `after_sensitive`/`before_sensitive` from `output_changes` as primary source, fall back to configuration metadata
  - **Template Structure:** New `_outputs.sbn` partial template, integrated into `default.sbn` for module and global outputs
  - **Edge Cases:** Handle modules with only outputs (no resources), missing metadata, nested sensitivity markers
- **Open Questions Resolved:**
  1. Data Model Extension → Parse `output_changes` eagerly during plan parsing
  2. Metadata Correlation → Correlate by name + module address during model building using configuration structure
  3. Module Output Parsing → Build complete output list, then group by module; handle output-only modules
  4. Value Rendering → Reuse `ValueFormatterRegistry` for automatic display name mappings
  5. Sensitivity Detection → Precedence: `after_sensitive` > `before_sensitive` > `configuration.sensitive` > `false`
  6. Update Actions → Show only `after` value (before→after diff is future enhancement per spec)
- **Problems Encountered:** None. Existing architecture patterns (model building, value formatting, sensitivity masking from ADR-009) provide clear integration paths.
- **Next Steps:** Handoff to Quality Engineer for test plan creation

### Quality Engineer
- **Date:** 2025-02-23
- **Summary:** Created comprehensive test plan for Terraform outputs support. Defined 30 test cases covering unit tests (parsing, model building, value formatting, masking), integration tests (module/global positioning, complex scenarios), snapshot tests (full rendering validation), architecture tests (layer boundaries), and end-to-end Docker tests. Also created UAT test plan for real-world validation in GitHub and Azure DevOps PRs.
- **Artifacts Produced:**
  - `docs/features/097-terraform-outputs/test-plan.md` - Complete test plan with 30 test cases
  - `docs/features/097-terraform-outputs/uat-test-plan.md` - User acceptance testing plan for PR validation
- **Test Coverage:**
  - **Unit Tests (TC-01 to TC-16):** Parse `OutputChange` from JSON, correlate metadata from configuration, build `OutputChangeModel` with correct properties, handle missing descriptions, detect sensitivity from multiple sources, mask sensitive values by default, detect computed values, reveal sensitive values with `--show-sensitive`, apply display name mappings, alphabetical ordering, all output actions (create/update/delete/no-op), no outputs scenario, modules with only outputs
  - **Integration Tests (TC-17 to TC-20, TC-27 to TC-28):** Module output positioning after resources, multiple modules with outputs, global output positioning after all modules, mixed module and global outputs, nested sensitivity detection, complex output values (arrays/objects)
  - **Snapshot Tests (TC-21 to TC-26):** Full rendering validation for basic outputs, sensitive values revealed, computed values, display name mappings, all actions, no outputs
  - **Architecture Tests (TC-29):** Verify no circular dependencies introduced
  - **End-to-End Tests (TC-30):** Docker container validation with CLI flag testing
  - **UAT:** Feature-specific artifact (`uat-plan.json` and `uat-plan.md`) plus comprehensive demo for regression testing
- **Test Data Requirements:** 23 test plan JSON files documented, 6 snapshot baseline markdown files identified
- **Edge Cases Covered:** No outputs, modules with only outputs, missing descriptions, nested sensitivity objects, complex values, missing configuration metadata, computed sensitive values (masking precedence), empty module addresses, multiple actions
- **Non-Functional Requirements:** Performance (100+ outputs rendering), error handling (malformed data), compatibility (Terraform 1.0+ plan formats)
- **UAT Strategy:** Two-artifact approach:
  1. Feature-specific test artifact focusing on outputs rendering with various scenarios
  2. Comprehensive demo for regression detection
  Both tested on GitHub and Azure DevOps platforms
- **Problems Encountered:** None. Existing test infrastructure (TUnit, snapshot testing, AwesomeAssertions) provides clear patterns for outputs feature testing.
- **Next Steps:** Handoff to Developer for implementation with test-driven development approach
