# Architecture: Enhanced Azure AD Resource Display

## Status

Proposed

## Context

This feature enhances how Azure AD (Entra) Terraform resources render in the generated Markdown report by adding:
- richer summary lines (inside `<summary>`), and
- semantic icons for key identity attributes (both in summaries and in attribute tables).

Primary requirements are defined in docs/features/053-azuread-resources-enhancements/specification.md.

Key architectural constraints and existing patterns:
- Reports are rendered via Scriban templates (docs/adr-001-scriban-templating.md).
- Resource blocks are rendered as `<details>` with a `<summary>` line; summary values must use HTML `<code>` spans (docs/report-style-guide.md).
- Provider-specific behavior is intended to live in provider modules under `src/Oocx.TfPlan2Md/Providers/*` (docs/architecture.md, `Providers/README.md`).
- Principal name resolution is available via the existing `--principal-mapping` file and `Platforms/Azure/PrincipalMapper`.
  - The mapper already supports a nested mapping format that can infer principal type (User/Group/ServicePrincipal) without calling external APIs.

## Options Considered

### Option 1: Extend core summary builder and semantic formatting only

Implement Azure AD-specific summary formatting in the shared summary generation (e.g., `ResourceSummaryHtmlBuilder`) and add Azure AD attribute-name icon rules to the global semantic formatting helpers.

Pros:
- No new provider module or templates required.
- Consistent behavior across all templates that rely on `change.summary_html`.

Cons:
- Azure AD-specific logic becomes global and grows shared complexity.
- Harder to test and reason about regressions for non-Azure AD resources.
- Global semantic formatting lacks access to resource type context, which is needed to decide which icon to use for `display_name` (user vs group vs service principal).

### Option 2: Add an Azure AD provider module with resource-specific templates (recommended)

Introduce a dedicated Azure AD provider module plus resource-specific templates for the in-scope Azure AD resources:
- `azuread_user`
- `azuread_invitation`
- `azuread_group_member`
- `azuread_group`
- `azuread_service_principal`
- `azuread_group_without_members`

Templates fully define the Azure AD summary line and can also control how table values render (icons inside code formatting). They can use:
- `change.after_json` / `change.before_json` to read raw state,
- existing Scriban helpers (`format_code_summary`, `format_code_table`, `escape_markdown`, etc.), and
- Azure principal helpers (`azure_principal_info`) backed by `PrincipalMapper`.

Pros:
- Provider separation: Azure AD behavior stays isolated and maintainable.
- Templates can access resource type context directly, solving the `display_name` icon ambiguity cleanly.
- Minimal blast radius: existing providers are unaffected.

Cons:
- Requires adding a new provider module and registering it.
- Some template logic may be more complex than pure C# (but is still bounded and testable).

### Option 3: Add Azure AD view model factories for these resources

Create C# view models/factories (similar to `azurerm_role_assignment`) that precompute:
- the Azure AD summary line text,
- member counts,
- resolved principal names/types,
- preformatted attribute rows.

Pros:
- Most complexity lives in C# (strong typing, unit tests, reuse).
- Templates remain small and consistent.

Cons:
- More code surface area and new models for what is primarily display logic.
- Higher implementation cost and risk of overengineering vs template-based rendering.

## Decision

Choose Option 2: add an Azure AD provider module with resource-specific templates, plus a small helper extension to support type inference and consistent icon formatting.

## Rationale

- The project already has an explicit provider-module pattern for isolating resource-specific rendering.
- Azure AD rendering needs resource-type context (e.g., `display_name` means different principal kinds), which templates naturally have.
- The design keeps changes localized and reduces regression risk.

## Consequences

### Positive
- Azure AD resources render with consistent, scan-friendly summaries.
- Type-aware member relationships and counts can leverage the existing principal mapping (no API calls).
- Easy to extend to more Azure AD resources later by adding templates.

### Negative
- The Azure AD provider module becomes another registered provider to maintain.
- If the principal mapping file does not include type metadata for member IDs, type-specific icons/counts will be limited.

## Implementation Notes

High-level guidance for the Developer agent (no implementation here):

### 1) Provider module and templates
- Add a new provider module under `src/Oocx.TfPlan2Md/Providers/AzureAD/` implementing `IProviderModule`.
- Embed Azure AD templates under `src/Oocx.TfPlan2Md/Providers/AzureAD/Templates/azuread/` using the `{provider}/{resource}.sbn` convention.
- Ensure `ProviderRegistry` registers the Azure AD module so templates can resolve via `resolve_template`.

### 2) Summary formatting rules (in templates)
- Render summary values using HTML code spans (via `format_code_summary` or `format_attribute_value_summary`) to maintain GitHub + Azure DevOps compatibility.
- Follow the report style guide: data values inside code, connectors/labels as plain text.
- Implement the spec’s edge cases directly in templates:
  - Omit optional `description` and `mail` segments when missing.
  - For `azuread_group_member`, if `member_object_id` is missing, display only group information.

### 3) Principal mapping + type inference
- For `azuread_group_member` and `azuread_group` member counts, resolve principal names via the existing principal mapping mechanism.
- Add a small Scriban helper (or enhance `azure_principal_info`) so templates can infer principal type from the mapping file when Terraform state does not provide it.
  - Recommendation: allow passing an empty type and have the helper fill it using `IPrincipalMapper.TryGetPrincipalType`.

### 4) Member counting (azuread_group)
- Compute counts from `members` in `change.after_json` for create/update/replace and `change.before_json` for delete.
- Split counts by principal type (User/Group/ServicePrincipal) and render as `N 👤 N 👥 N 💻` in code formatting.
- If types cannot be inferred for some IDs, treat those IDs as unknown and do not misclassify them; keep output deterministic and avoid external lookups.
- If there are unknowns, include them explicitly as a fourth count using a question emoji, e.g. `N 👤 N 👥 N 💻 N ❓`.

### 5) Table icon formatting
- Apply icons to identity values inside tables for the specified attributes:
  - `azuread_user`: `display_name` (👤), `user_principal_name` (🆔), `mail` (📧)
  - `azuread_invitation`: `user_email_address` (📧)
- Prefer a helper-based approach for consistent non-breaking icon spacing (U+00A0) and correct code wrapping in both summary and table contexts.

### 6) Testing and docs updates
- Add/extend provider template tests under `tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/` for each resource template.
- Update snapshot test baselines intentionally to reflect the new icon-enhanced output.
- Update documentation that describes report patterns and/or provider support, as required by the specification.

## Components Affected

- docs/features/053-azuread-resources-enhancements/specification.md (reference)
- src/Oocx.TfPlan2Md/Providers/* (new Azure AD module + templates)
- src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/* (small helper addition for type inference and/or icon formatting)
- tests/Oocx.TfPlan2Md.TUnit/Providers/* (new Azure AD template tests)
