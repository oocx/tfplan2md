# Work Protocol: Issue #100 - Readable Display Names on Identity Attributes

## Workflow Type

Bug Fix

## Issue Summary

Readable display names are incorrectly applied to a resource's own identity attributes (`id`, `name`) when rendering attribute tables, causing redundant and confusing output. The readable display name feature should only be used when referencing other resources, not for the resource's own identity.

## Problem Statement

When displaying resource attributes in detail tables, all attributes named `id` or `name` get semantic formatting with icons and contextual information. This is correct for **reference attributes** (e.g., `virtual_network_id` pointing to another resource) but wrong for **self-identity attributes** (e.g., the resource's own `id` attribute).

Example of incorrect rendering:
- Attribute: `id` (the resource's own ID)
- Current: `MetricAlerts 🆔 name in resource group 📁 rg name of subscription 🔑 subscription name (id)`
- Expected: `/subscriptions/.../resourceGroups/rg/providers/.../metricAlerts/alert-name` (raw value)

## Agent Work Log

### Issue Analyst - 2024-02-23

**Summary:**
Completed comprehensive investigation of the readable display name bug. Identified root cause in the semantic formatting chain and provided detailed analysis with three fix approaches.

**Artifacts Produced:**
- `docs/issues/100-readable-display-names-on-identity-attributes/analysis.md` - Comprehensive issue analysis
- `docs/issues/100-readable-display-names-on-identity-attributes/work-protocol.md` - This work protocol

**Key Findings:**
1. Root cause: `TryFormatNameAttribute` in `SemanticFormatting.Identity.cs` applies formatting to ALL attributes named `name`, without distinguishing self-identity from references
2. The template `_resource.sbn` calls `format_attribute_value_table` for every attribute, triggering semantic formatting
3. No existing tests specifically cover `id` or `name` attribute formatting behavior
4. The feature was designed for cross-resource references but lacks context awareness

**Recommended Fix:**
Context-aware formatting approach (Option 1 in analysis):
- Add `is_self_identity` parameter to formatting function chain
- Update templates to detect `id` attributes and mark them as self-identity
- Skip semantic formatting for self-identity attributes
- Preserve readable formatting for reference attributes

**Files Affected:**
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Identity.cs` (line 237-253)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn` (lines 27, 39, 50-51)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.Registry.cs` (line 246-295)
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs` (function signatures)

**Next Steps:**
Hand off to Developer agent to implement the fix following Option 1 approach.

**Problems Encountered:**
None. Investigation completed successfully.

---

## Current Status

✅ **Analysis Complete** - Ready for Developer

## Next Agent

**Developer** - Implement the context-aware formatting fix as detailed in the analysis document.
