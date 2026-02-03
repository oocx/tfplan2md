# Architecture: Static Code Analysis Integration

## Status

Proposed

## Context

This feature integrates static code analysis findings (e.g., Checkov, Trivy, TFLint, Semgrep) into the existing Terraform plan markdown report.

Key constraints and existing patterns:

- Report output must render reliably in both **GitHub** and **Azure DevOps** markdown engines (see `docs/markdown-specification.md`).
- Report visuals follow the project’s report styling rules (see `docs/report-style-guide.md`).
- Template rendering is Scriban-based (see `docs/adr-001-scriban-templating.md`).
- The existing architecture favors:
  - minimal dependencies,
  - immutable models,
  - model-building in C# with thin templates,
  - platform-specific rendering behind abstractions when needed.

Reference: [Feature Specification](specification.md)

## Options Considered

### Option 1: Use a full SARIF SDK (Microsoft.CodeAnalysis.Sarif)
Parse SARIF using an external library that provides a full object model.

**Pros:**
- Faster initial implementation (schema already modeled).
- Better schema correctness “out of the box”.

**Cons:**
- Adds a relatively heavy dependency surface for a small subset of SARIF.
- Higher maintenance and versioning risk.
- Potentially increases build size/time and complicates trimming.

### Option 2: Minimal SARIF 2.1.0 reader using System.Text.Json (subset parser)
Parse only the fields required by the specification using `System.Text.Json` with a tolerant, defensive parser.

**Pros:**
- Fits the project’s “minimal dependency” and “offline” constraints.
- Easier to handle malformed files gracefully.
- Keeps the parsing surface limited to what the report needs.

**Cons:**
- Requires careful defensive parsing.
- Must validate against real outputs from multiple tools.

### Option 3: Treat SARIF as an opaque blob and surface it to templates
Load SARIF as JSON and let templates decide how to display it.

**Pros:**
- Minimal application logic.

**Cons:**
- Pushes complexity into templates (opposite of established architecture).
- Harder to test and to keep markdown valid.
- Makes severity mapping, filtering, and resource mapping inconsistent.

## Decision

- **Parsing:** Option 2 (minimal SARIF subset parser) is used.
- **Presentation:** Use **Variant C (Hybrid with Severity Indicators)** from `presentation-drafts.md`.

## Rationale

- A subset SARIF parser is sufficient because the feature only needs a stable, well-defined portion of SARIF: tool metadata, results, rule IDs, help URIs, message text, and logical locations.
- Variant C is already decided in the specification and best balances scanability (inline indicators) with completeness (non-collapsible findings table).
- The design keeps templates thin and keeps platform differences constrained to rendering rules already captured in the markdown specification/style guide.

## Key Design Decisions

### 1) Data model integration strategy

Add a dedicated `CodeAnalysisModel` to the report model, and attach per-resource findings to the resource-level model.

This supports:
- Top-level summary (counts, tools used, status).
- Resource-level display (Variant C).
- “Findings-only resources” that must appear even if they do not show up in the plan’s resource changes.
- Unmatched findings section at end.

**Non-goals:** templating the raw SARIF structure directly.

### 2) Presentation and markdown compatibility

Use only markdown constructs known to render consistently:
- Standard markdown headings and tables.
- Existing `<details>` resource containers.

Avoid introducing any new “collapsible” constructs for findings (Variant C explicitly avoids reliance on collapsible sections for findings).

### 3) Severity mapping and filtering

SARIF’s built-in `result.level` is too coarse for the five-level UI requirement (Critical/High/Medium/Low/Informational). Tools vary, but many emit additional severity signals.

Severity derivation is therefore **hybrid**:

1. **Primary signal (if present):** `properties.security-severity` (commonly a 0–10 numeric).
2. **Secondary signal (if present):** `rank` (numeric ordering; lower often means more severe, but varies).
3. **Fallback:** `result.level` (`error`, `warning`, `note`, `none`).

Recommended mapping (for a 0–10 `security-severity`):
- `>= 9.0` → Critical
- `>= 7.0` → High
- `>= 4.0` → Medium
- `>= 1.0` → Low
- otherwise → Informational

Fallback mapping from `result.level`:
- `error` → High
- `warning` → Medium
- `note` → Low
- `none` / missing → Informational

**CLI filtering:**
- `--code-analysis-minimum-level` filters what is displayed.
- `--fail-on-static-code-analysis-errors <level>` gates the exit code.

To avoid “failing on hidden findings”, the effective display threshold should be:

- `effectiveMinimumLevel = min(userMinimumLevel, failOnLevel)` (when `failOn` is set)

This ensures all findings that can trigger a failure are always visible in the report.

### 4) Visual prominence and accessibility

Prominence is achieved via:
- Emoji + text label in the Severity column (do not rely on color).
- Consistent ordering of findings in tables from most severe to least.
- A short per-resource “Security & Quality” summary line (Variant C) that shows counts.

Icons should align with the style guide’s “icons + labels” approach and remain stable across platforms.

### 5) Resource and attribute mapping

**Resource mapping (required):**
- Primary source: `runs[].results[].locations[].logicalLocations[].fullyQualifiedName`.
- Extract the Terraform resource address from the fully qualified name using best-effort normalization.
- Matching strategy: choose the first substring that matches a Terraform resource address form:
  - `module.<path>.<type>.<name>`
  - `<type>.<name>`

**Attribute mapping (best-effort):**
- If a logical location contains a suffix beyond the resource address, treat the remainder as an attribute path.
- If no reliable attribute path exists, store `Attribute = null` and show the finding as resource-level.

This matches the specification’s requirement to “include specific attribute when available” and to “gracefully fall back”.

### 6) Tools used extraction

Tool identity should be derived from:
- `runs[].tool.driver.name` (required)
- `runs[].tool.driver.semanticVersion` (preferred if present)
- `runs[].tool.driver.version` (fallback)

Output should show “Name version” when available (e.g., `Checkov 3.2.10`).

### 7) Invalid SARIF handling and reporting

Invalid or malformed SARIF inputs must:
- be skipped,
- produce a warning entry in the report (including error details),
- not prevent report generation,
- still allow `--fail-on-static-code-analysis-errors` to run after report generation.

### 8) Wildcard expansion

Relying on shell expansion is not sufficient (users may quote patterns, and CI shells vary). The application should perform its own expansion:
- Support `*.sarif` and `path/*.sarif` (at minimum).
- Multiple `--code-analysis-results` flags are concatenated.
- Preserve deterministic ordering (sort file paths ordinally) to keep output stable for snapshots.

## Consequences

### Positive
- Clear separation of concerns: SARIF parsing and mapping are isolated.
- Report remains readable with many findings due to consistent sorting and summary lines.
- Backward compatible by default: if no SARIF files are provided, output is unchanged.

### Negative / Risks
- SARIF tool outputs vary; mapping logic must be validated against real outputs.
- Attribute-level mapping may be inconsistent across tools; best-effort must be clearly represented.
- More content in the report can increase PR comment length; needs careful suppression of empty sections.

## Implementation Notes

High-level guidance for the Developer agent (non-prescriptive):

### 1) Suggested component boundaries

- `CodeAnalysis/` (new):
  - SARIF reading and validation (subset schema)
  - Normalization/mapping to internal finding models
  - Severity derivation

- `MarkdownGeneration/` (existing):
  - Extend `ReportModel` to expose `CodeAnalysis` summary + findings.
  - Extend `ResourceChangeModel` (or an adjacent view model) to include:
    - per-resource findings list
    - per-resource severity counts for the Variant C metadata line

### 2) Findings-only resources

When a finding maps to a Terraform resource address that is not present in the plan’s change set:

- Include it as a “no-op / unchanged” resource entry in the appropriate module section.
- Render it with a summary line and findings table, but suppress the normal attribute table if no attribute data exists.

This satisfies:
- “Include resources with findings even if they wouldn't normally appear in the report”
- “Display findings at the resource level”

### 3) Unmatched findings

Maintain a separate collection for:
- Module-level findings (when module can be identified but not a specific resource)
- Fully unmatched findings (no stable module/resource mapping)

Render these in a dedicated end-of-report section, and suppress it entirely if empty.

### 4) Exit code behavior

Implement exit code 10 when `--fail-on-static-code-analysis-errors <level>` is set and findings at/above that level exist.

This check must run **after** report generation and output writing.

### 5) Templates

Update built-in templates to:
- Render the new “🔒 Code Analysis Summary” section in the Summary area only when code analysis input is present.
- Render the Variant C per-resource metadata line and findings table when a resource has findings.
- Keep markdown validity rules (blank lines around headings/tables; no raw newlines in table cells).

Also review and update **provider-specific templates** under `src/Oocx.TfPlan2Md/Providers/*/Templates/**` as needed.

Rationale: many provider templates implement their own per-resource rendering (custom summary lines, custom attribute tables, semantic diff sections) and therefore bypass the core `_resource.sbn` layout. If per-resource code analysis rendering is only added to core templates, those provider templates will not show findings.

To minimize duplication and keep output consistent, prefer adding a shared partial for code analysis rendering (e.g., a `_code_analysis_findings.sbn` include) that is invoked from both:
- the core resource template (`_resource.sbn`), and
- any provider-specific resource templates that override the standard layout.

### Provider Template Impact Checklist

When implementing Variant C, verify these behaviors across both core templates and any provider-specific resource templates:

- Per-resource metadata line: shows severity counts / indicators when findings exist.
- Findings table: renders consistently (headings, columns, escaping) and is suppressed when no findings exist.
- “Findings-only resources”: still render findings even when no attribute changes / custom semantic sections exist.
- Unchanged behavior when no SARIF inputs are provided.
- Markdown rules: no broken tables from unescaped content; keep `<details>/<summary>` structure valid.

If a provider template renders resources without going through the core `_resource.sbn`, ensure it explicitly includes the shared code analysis partial.

### 6) Testing

- Add test data using real SARIF outputs from Checkov, Trivy, and TFLint.
- Add integration tests for:
  - multi-file input and wildcard expansion
  - mapping accuracy (resource and best-effort attribute)
  - severity mapping and ordering
  - invalid SARIF file warnings
  - exit code 10 behavior while still producing a complete report

