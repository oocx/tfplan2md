# Work Protocol: Known-After-Apply Rendering

**Work Item:** `docs/features/102-known-after-apply-rendering/`
**Branch:** `feature/102-known-after-apply-rendering`
**Workflow Type:** Feature
**Created:** 2026-02-25

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Requirements Engineer
- **Date:** 2026-02-25
- **Summary:** Analysed two existing Copilot branches (`copilot/fix-empty-summary-details` and `copilot/fix-empty-summary-details-azuread-group-member`) that each independently address the same underlying bug. Synthesised their approaches into a single feature specification covering all use-case scenarios (five group-member scenarios + generic resource scenarios + update scenario) and three explicit decision points (A/B/C) with rendered output examples for each option. Created the feature branch and specification document.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/specification.md`, `docs/features/102-known-after-apply-rendering/work-protocol.md`
- **Problems Encountered:** The two copilot branches conflict on three design decisions (which attributes to show in tables, whether to show config references in table values, and sensitivity vs. computed priority). These are presented as Decision A, B, and C in the specification with rendered examples for Maintainer review before the Architect begins implementation planning.

### Architect
- **Date:** 2026-02-25
- **Summary:** Produced the technical design for surfacing computed (known-after-apply) values in attribute tables and for fixing `azuread_group_member` summaries when IDs are computed. The design keeps unknown-value semantics in `ReportModelBuilder` (so computed attributes participate in update summaries) and confines AzureAD-specific summary rules to the AzureAD provider. Also defined a minimal way to suppress the default `_No attribute changes._` placeholder for the whole-resource `after_unknown: true` plan shape.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/architecture.md`
- **Problems Encountered:** None. One known variability risk is Terraform plan-shape differences for `after_unknown`; the design recommends conservative unknown-path parsing and snapshot coverage for the spec scenarios.

### Architect (revision)
- **Date:** 2026-02-25
- **Summary:** Enriched the architecture document with comprehensive codebase background so that a developer can work from this document and the specification alone, without needing the global `docs/architecture.md`. Added detailed explanations of: the full rendering pipeline (parsing → model building → template rendering), `JsonFlattener` behaviour with null values, the exact logic and root cause in `BuildAttributeChanges`, the `FormatAttributeValueTableWithRegistry` null-value gate, the `AzureAdSummaryBuilder.Groups.cs` empty-summary root cause, the `ConfigurationReferenceResolver` index structure, the provider module registration system, and the reference implementation in `DiffRenderer.Paths.cs`.
- **Artifacts Produced:** `docs/features/102-known-after-apply-rendering/architecture.md` (updated)
- **Problems Encountered:** None.
