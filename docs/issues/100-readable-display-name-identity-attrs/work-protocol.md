# Work Protocol: Readable Display Name Applied to Identity Attributes

**Work Item:** `docs/issues/100-readable-display-name-identity-attrs/`
**Branch:** `copilot/fix-readable-display-name-issue`
**Workflow Type:** Bug Fix
**Created:** 2025-02-23

## Agent Work Log

<!-- Each agent appends their entry below when they complete their work. -->

### Issue Analyst — 2025-02-23

**Summary:**
Investigated the bug where "readable display name" formatting with semantic icons is incorrectly applied to a resource's own identity attributes (`id`, `name`) in attribute tables. The feature is designed to add semantic icons and context when **referencing other resources**, but is mistakenly applied to a resource's own identity attributes, causing redundant and confusing output.

**Work Completed:**
- Searched codebase for "readable display name" feature implementation
- Identified core formatting code in `ScribanHelpers` partial classes
- Located `TryFormatNameAttribute` in `SemanticFormatting.Identity.cs:237-253` which applies 🆔 icon to ANY `name` attribute
- Located `TryFormatIdentityAttribute` in `SemanticFormatting.Identity.cs:157-174` which formats identity-related attributes
- Found formatting pipeline: Templates call `format_attribute_value_table_resource` → `FormatAttributeValueTableWithRegistryResource` → `FormatAttributeValueCore` → `TryFormatSemanticValue` → `TryFormatNameAttribute`
- Verified there is NO special handling to exclude a resource's own `id` or `name` attributes from semantic formatting
- Created issue analysis document at `docs/issues/100-readable-display-name-identity-attrs/analysis.md`

**Artifacts Produced:**
- `docs/issues/100-readable-display-name-identity-attrs/analysis.md` - Complete issue analysis with root cause, affected files, and fix approach

**Problems Encountered:**
None

**Next Agent:** Developer (to implement the fix based on the analysis)
