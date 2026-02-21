# Architecture: Sensitive Information Exposure Mitigation

## Status

Proposed

## Context

This issue (see [analysis.md](analysis.md)) identifies multiple paths where tfplan2md can render Terraform-sensitive values in plaintext, notably:

- Provider-specific rendering (AzApi body create/delete/update)
- Any Scriban template accessing `before_json` / `after_json` without sensitivity context
- Azure DevOps Variable Group secret masking inconsistencies
- Attribute-level masking edge cases when Terraform encodes sensitivity as:
  - Root boolean sensitivity (`before_sensitive: true` / `after_sensitive: true`)
  - Parent array sensitivity with top-level array keys (e.g., `secrets[0]`)

The repository’s architecture already states “Security by default” and that sensitivity is determined by Terraform’s `before_sensitive` / `after_sensitive` metadata. The architecture gap is that *template-based rendering exposes raw state without guaranteed masking*, and some hierarchical sensitivity encodings are currently missed.

## Goals

- Never output plaintext sensitive values unless the user explicitly enables `--show-sensitive`.
- Make the safe behavior *hard to bypass accidentally*, including in built-in and custom templates.
- Keep Terraform as the source of truth for sensitivity (no heuristic secret detection).
- Preserve architectural boundaries: provider-specific logic stays under `src/Oocx.TfPlan2Md/Providers/<ProviderName>/`.

## Non-Goals

- Changing CLI surface area (unless required for safety).
- Adding attribute-name-based secret heuristics.
- Replacing Scriban or redesigning the entire template system.

## Options Considered

### Option 1: Propagate sensitivity maps into Scriban and rely on templates to mask

Expose `before_sensitive` / `after_sensitive` in the Scriban context (in addition to `before_json` / `after_json`) and update built-in templates/helpers to check sensitivity before printing values.

- Pros
  - Minimal behavioral change for templates that already behave safely
  - Enables custom templates to implement correct masking
  - Straightforward wiring; aligns with existing domain model
- Cons
  - Not fail-safe: any template that directly prints `before_json` / `after_json` fields can still leak secrets
  - Hard to enforce across user-provided templates

### Option 2 (Recommended): Provide *masked-by-default* JSON to templates + still expose sensitivity maps

Keep `before_json` / `after_json` in the template context, but ensure they are **already masked** when `--show-sensitive` is not enabled.

In addition, expose `before_sensitive` / `after_sensitive` so advanced templates can:
- reason about why something is masked
- render alternate placeholders
- drive layout decisions without reading raw secret values

- Pros
  - Defense in depth: even an unsafe template can’t accidentally print secrets
  - Preserves template author ergonomics (they can still traverse JSON)
  - Aligns with the architectural intent “Security by default”
  - Works for custom templates without requiring template authors to adopt new patterns
- Cons
  - Requires a deterministic “mask JSON by sensitivity map” transformation in core MarkdownGeneration
  - Small risk of breaking templates that intentionally relied on seeing secrets without `--show-sensitive` (considered acceptable for a security fix)

### Option 3: Remove raw JSON (`before_json` / `after_json`) from template context

Templates would render only via precomputed `attribute_changes` and other “safe” view-model properties.

- Pros
  - Strong safety guarantee: templates cannot access raw state
  - Encourages the “templates are layout-only” direction
- Cons
  - High compatibility risk for built-in and custom templates
  - Forces a broader template system redesign (out of scope for this fix)

## Decision

Adopt **Option 2**:

1. Ensure template contexts have sensitivity metadata (`before_sensitive`, `after_sensitive`).
2. Ensure `before_json` / `after_json` values are **masked-by-default** based on that metadata unless `--show-sensitive` is enabled.
3. Treat provider-specific renderers as *consumers* of the same sensitivity rules, not independent implementations.

## Rationale

This issue is fundamentally a “multiple rendering paths, inconsistent safety” problem. The highest-leverage architecture change is to make the *unsafe-by-default* data surface (raw JSON in templates) safe by construction.

Propagating sensitivity maps alone (Option 1) fixes correctness for built-in templates but still leaves an easy-to-miss footgun for custom templates and future template changes.

Masking the JSON object graph before it reaches templates ensures:

- built-in templates cannot accidentally regress
- custom templates are safe even when authored by users unfamiliar with Terraform sensitivity encoding
- the security model is centralized and testable

## Consequences

### Positive

- Secrets no longer depend on template author discipline.
- Provider templates can still render semantic JSON structures without reintroducing leaks.
- The `--show-sensitive` flag becomes the single, auditable escape hatch for plaintext output.

### Negative / Risks

- Masking transformation must exactly follow Terraform’s sensitivity encoding rules; mistakes could either leak (under-mask) or reduce usability (over-mask).
- Snapshot tests will likely change (expected) and must be reviewed for correctness.

## Implementation Notes (for Developer Agent)

### 1) Centralize sensitivity semantics (cross-cutting)

- Define a single “is this path sensitive?” API that:
  - checks exact keys
  - checks hierarchical parents
  - handles root boolean sensitivity (`"" -> true`)
  - handles parent array sensitivity for keys without dots (e.g., `secrets[0]` checks `secrets`)

This API should live in core MarkdownGeneration (not in providers), so providers can call it without duplicating logic.

### 2) Mask JSON before passing into Scriban

- In `AotScriptObjectMapper` (or the nearest safe point), build the template `before_json` / `after_json` objects as follows:
  - If `--show-sensitive` is enabled: map raw JSON as-is.
  - Else: produce a structurally equivalent JSON-like tree where any sensitive leaf (or sensitive subtree) is replaced with the sentinel string `(sensitive)`.

Also map `before_sensitive` / `after_sensitive` into the template context.

### 3) Fix AzApi rendering paths

- Create/delete/replace: thread sensitivity into the AzApi body rendering helper and mask using the centralized sensitivity API.
- Update: treat `is_sensitive` as authoritative for masking at the helper layer (do not depend on template-level masking).

This aligns with the “templates are layout-only” principle from template simplification work: masking belongs in C# helpers/model building.

### 4) Azure DevOps Variable Group diff masking consistency

- Bring Variable Group diff masking into parity with Build Definition diff masking: mask when either side is secret (`before || after`).

### 5) Tests / regression harness

- Add focused unit tests for hierarchical sensitivity edge cases:
  - root boolean sensitivity (`"" -> true`)
  - top-level array parent sensitivity for keys without dots
  - mixed dotted + indexed paths
- Add snapshot tests for AzApi sensitive body (create/delete/update) verifying no plaintext secrets appear unless `--show-sensitive`.
- Add snapshot/unit tests for Variable Group secret transitions.

## Components Affected

- Core: `src/Oocx.TfPlan2Md/MarkdownGeneration/*` (template context mapping, sensitivity logic)
- Providers:
  - `src/Oocx.TfPlan2Md/Providers/AzApi/*`
  - `src/Oocx.TfPlan2Md/Providers/AzureDevOps/*`

## References

- Issue analysis: [analysis.md](analysis.md)
- Aggregated security findings: [../097-security-analysis/results.md](../097-security-analysis/results.md)
- Prior related issue: [../093-sensitive-attribute-disclosure/analysis.md](../093-sensitive-attribute-disclosure/analysis.md)