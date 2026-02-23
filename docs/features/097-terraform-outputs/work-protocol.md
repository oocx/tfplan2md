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
