# ADR-009: Mask Sensitive JSON Before Template Rendering

## Status

Proposed

## Context

tfplan2md renders Markdown via Scriban templates (ADR-001). The template context currently exposes rich Terraform state, including `before_json` and `after_json` objects.

The project’s architectural intent is “security by default”: sensitive values must be masked unless the user explicitly opts in with `--show-sensitive` (see [docs/architecture.md](architecture.md) § 8.1 Security).

Issue 098 (see [docs/issues/098-sensitive-info-exposure/analysis.md](issues/098-sensitive-info-exposure/analysis.md)) demonstrates that this intent can be violated when:

- a built-in provider template (e.g., AzApi) renders raw JSON paths without consistent sensitivity checks
- a custom template prints values from `before_json` / `after_json` directly
- sensitivity metadata (`before_sensitive` / `after_sensitive`) is missing from the template context

This is an architectural problem because it creates a recurring footgun: *template authors can accidentally exfiltrate secrets by rendering raw state*.

## Options Considered

### Option 1: Pass sensitivity maps into templates; require templates/helpers to mask

Expose `before_sensitive` / `after_sensitive` alongside `before_json` / `after_json` and document that templates must mask based on those maps.

- Pros
  - Minimal change in what data templates can access
  - Template authors retain full control over masking behavior
  - Aligns with “templates are flexible” positioning
- Cons
  - Not fail-safe: a single missed check leaks secrets
  - Hard to enforce for custom templates
  - High regression risk as templates evolve

### Option 2 (Chosen): Provide masked-by-default JSON to templates, plus sensitivity maps

Continue exposing `before_json` / `after_json`, but when `--show-sensitive` is **not** enabled, supply a *masked* JSON tree where any sensitive leaf (or sensitive subtree) is replaced with the sentinel string `(sensitive)`.

Also expose `before_sensitive` / `after_sensitive` in the context so templates can:
- explain why something is masked
- drive layout decisions
- render alternate placeholders

- Pros
  - Defense in depth: unsafe templates cannot accidentally leak secrets
  - Keeps template ergonomics (templates can still traverse JSON)
  - Centralizes sensitivity semantics in C# where it is testable
  - Reduces provider-specific duplication and drift
- Cons
  - Requires a deterministic masking transform based on Terraform sensitivity encoding
  - May break templates that implicitly relied on seeing secrets without `--show-sensitive` (acceptable for security)

### Option 3: Remove raw JSON (`before_json` / `after_json`) from template context

Templates would render only from precomputed, safe view models (e.g., `attribute_changes`).

- Pros
  - Strongest guarantee: templates cannot access raw secrets
  - Encourages “templates are layout-only” discipline
- Cons
  - High compatibility risk for built-in and custom templates
  - Forces broader redesign of template capabilities (out of scope)

## Decision

Adopt **Option 2**:

1. Always expose sensitivity metadata (`before_sensitive`, `after_sensitive`) to templates.
2. Expose `before_json` / `after_json` as:
   - **raw** when `--show-sensitive` is enabled
   - **masked-by-default** when `--show-sensitive` is disabled

Provider-specific rendering must not introduce alternate masking semantics; providers should consume the same centralized sensitivity logic.

## Rationale

Sensitivity handling is a cross-cutting security concern. Allowing raw JSON into a general-purpose template engine creates an attractive but dangerous API surface.

Masking the JSON object graph before templates run ensures:

- security is the default, not a convention
- regressions become difficult to introduce accidentally
- correctness is enforceable with unit and snapshot tests

## Consequences

### Positive

- Reduces secret exposure risk from built-in and custom templates.
- Centralizes sensitivity semantics, reducing duplication across providers.
- Keeps advanced template scenarios possible (JSON traversal) without leaking secrets.

### Negative

- Requires ongoing maintenance of the masking transform to keep pace with Terraform sensitivity encodings.
- Some custom templates may need updates if they expected raw values without `--show-sensitive`.

## Implementation Notes

- Implement masking at the template-context boundary (nearest to where `before_json` / `after_json` are mapped).
- The masking transform must respect Terraform plan semantics:
  - exact-path sensitivity
  - hierarchical parent sensitivity
  - root boolean sensitivity (`before_sensitive: true` / `after_sensitive: true`)
  - array parent sensitivity, including keys without `.` (e.g., `secrets[0]` should check `secrets`)
- Keep provider-specific logic isolated to `Providers/<ProviderName>/` (no provider logic in core MarkdownGeneration).

## References

- Scriban templating: [docs/adr-001-scriban-templating.md](adr-001-scriban-templating.md)
- Architecture security intent: [docs/architecture.md](architecture.md)
- Issue 098 analysis: [docs/issues/098-sensitive-info-exposure/analysis.md](issues/098-sensitive-info-exposure/analysis.md)
- Issue 098 architecture notes: [docs/issues/098-sensitive-info-exposure/architecture.md](issues/098-sensitive-info-exposure/architecture.md)