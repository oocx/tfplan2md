# Architecture: Report Presentation Enhancements

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

This feature combines 4 changes that span **Markdown generation**, **HTML rendering fidelity**, and **developer tooling**:

1. **Resource visual enhancement**: ensure every resource is rendered in a `<details>` block with a light border style (inline styles).
2. **Screenshot tool partial capture**: extend the Playwright-based screenshot generator to capture a specific element (resource-by-address or CSS selector).
3. **Report metadata display**: include tfplan2md version + commit hash + generation timestamp next to Terraform version, with `--hide-metadata`.
4. **Name icons**: semantic icons for `name` and `resource_group_name`, using existing semantic formatting conventions.

Relevant existing architecture/patterns:

- Markdown output is generated via **Scriban templates** (see [docs/adr-001-scriban-templating.md](../../adr-001-scriban-templating.md)).
- Semantic formatting is centralized in **C# helpers** (`ScribanHelpers.FormatAttributeValue*`) and consumed by templates (see feature 024’s approach).
- HTML renderer has two flavors:
  - GitHub flavor strips `style=` attributes (to mirror GitHub sanitization).
  - Azure DevOps flavor preserves and normalizes `style=` (so borders can be visible) (see feature 027).

## Problem to Solve

Implement the presentation enhancements while preserving:
- Existing template architecture (built-in + resource-specific templates)
- Cross-platform behavior (GitHub strips styles; AzDO shows them)
- Snapshot test stability (new metadata fields are inherently variable)
- Screenshot generation that reliably targets resource blocks

## Options Considered

### Option 1: Template-only changes for everything
Implement borders, metadata line, and name icons purely in Scriban templates.

- Pros
  - Smallest code change surface area
  - Fast iteration
- Cons
  - Hard to keep formatting consistent across templates and contexts (table vs `<summary>`)
  - Metadata requires accessing version/commit/time, which templates cannot reliably compute
  - Snapshot stabilization becomes awkward (string filtering would live in tests only)

### Option 2: Keep templates layout-focused; add minimal model/helper support (recommended)
Use templates for structure and rely on C# for:
- stable metadata values
- semantic formatting (icons + non-breaking space)
- deterministic time/version for tests

- Pros
  - Matches existing project philosophy (helpers/model do “smart formatting”)
  - Makes snapshot-stability a first-class, testable design
  - Avoids duplicating name/icon rules in multiple templates
- Cons
  - Requires small cross-cutting changes (CLI → model → templates → tests)

### Option 3: Push presentation into HTML renderer/tooling
Try to apply borders/metadata via the HTML renderer post-processors or CSS only.

- Pros
  - Would avoid changing markdown templates in some cases
- Cons
  - Spec explicitly requires inline styles in markdown and applies to Markdown output, not only HTML
  - Would not solve GitHub markdown rendering (GitHub strips `style=` anyway)

## Decision

Choose **Option 2**.

## Proposed Technical Design

### 1) Resource visual enhancement: consistent `<details>` wrapper styling

**Decision:** Apply the border/padding style to the *outermost resource* `<details>` only.

**Template changes (conceptual):**
- Update the shared resource wrapper (default resource template) to use:

```html
<details style="margin-bottom:12px; border:1px solid #f0f0f0; padding:12px;">
```

- For resources that currently render without `<details>` (e.g., firewall/NSG resource-specific templates), wrap the whole resource block with:

```html
<details open style="margin-bottom:12px; border:1px solid #f0f0f0; padding:12px;">
```

**Rationale:**
- Meets the spec’s “all resources in `<details>`” requirement.
- Preserves existing UX for those templates (they were effectively “always expanded”; `open` maintains that).
- Avoids accidentally styling nested `<details>` blocks used for large attributes.

**HTML renderer impact:**
- No new renderer behavior required:
  - GitHub flavor already strips inline styles.
  - Azure DevOps flavor preserves and normalizes inline styles.

### 2) Screenshot tool: partial capture by resource address or CSS selector

#### 2.1 Resource targeting contract

To make “by Terraform resource id/address” reliable, we need a targeting mechanism that survives both:
- GitHub HTML sanitization
- Azure DevOps HTML sanitization

Constraints (confirmed):
- **Do not rely on `class=` or `data-*` attributes**, as they are stripped.
- **Do not rely on HTML comment anchors**, as they are also stripped.

**Decision:** Locate the target resource by **visible text** that already exists in the rendered report:
- The module heading: `### 📦 Module: <module_address>` (or `root`)
- The resource `<summary>` line, which includes the Terraform resource type and the **local Terraform resource name** (the `name` field from the plan, not necessarily the resource’s `name` attribute).

**Lookup strategy (high-level):**
1) Parse `--target-terraform-resource-id` into:
  - `module_address` (or `root` when absent)
  - resource `type`
  - local Terraform resource `name`
2) Find the matching module section by locating the `📦 Module:` heading for the parsed module.
3) Within that module section, find resource `<details>` blocks whose `<summary>` matches:
  - contains the resource type string, and
  - contains the local Terraform name (bold+code in the current summary format).

Rationale:
- Uses text that is already present and visible, so it is less likely to be stripped.
- Avoids introducing additional presentation-only artifacts (extra headings/IDs) solely for tooling.

#### 2.2 Capture strategy (Playwright)

When a target is specified, the screenshot generator should:
- Load the HTML as today.
- Locate the element(s) based on:
  - `--target-terraform-resource-id` → module heading + summary text match (see 2.1)
  - `--target-selector` → provided selector
- Fail with a clear error if nothing matches.

**Decision:** If a selector matches multiple elements, capture a single screenshot of the **union bounding-box** of all matched elements.

Rationale:
- Matches the spec wording “element(s)” while still producing a single output file.
- Avoids surprising “first match” behavior and avoids forcing users to over-specify selectors.

Notes:
- The tool should still fail with a clear error if there are **zero** matches.
- If the union bounding-box is empty (e.g., elements are `display:none`), treat it as “not capturable” and fail clearly.

### 3) Report metadata line + `--hide-metadata`

#### 3.1 Data model / templating

The spec requires a single line:

`Generated by tfplan2md X.Y.Z (abc1234) on YYYY-MM-DD HH:MM:SS UTC | Terraform 1.6.0`

**Decision:** Treat metadata as a *render-time concern* and provide template-ready fields on the model:
- tfplan2md version number
- short commit hash (7 chars)
- report generation timestamp (UTC, formatted)
- `hide_metadata` boolean

Templates then become simple:
- if not hidden, print exactly one line in the required format.
- if hidden, print nothing (and do not print terraform version elsewhere).

#### 3.2 Version + commit sourcing

**Decision:** Source version/commit primarily from assembly metadata (build-time embedded).

Recommended implementation approach:
- Version: use the existing assembly informational version (or the existing version infrastructure already used for `--version`).
- Commit: embed the source revision into assembly metadata during build and read it at runtime.

**Rationale:**
- Works in Docker/release scenarios where `.git` is not present.
- Avoids runtime dependency on git.

**Fallback behavior:** If commit is unavailable, use a placeholder like `unknown` (or omit parentheses). The spec prefers always showing a 7-char hash; ensuring build embeds it is strongly recommended.

#### 3.3 Snapshot test stability

The current snapshot tests do exact string comparisons (with line-ending normalization), so variable metadata must be controlled.

Options:
- **Option 1 (recommended): Inject deterministic metadata provider in tests**
  - Provide fixed `generated_at`, `version`, and `commit` values when constructing the model in tests.
  - Pros: keeps snapshots meaningful; no “regex filtering” hiding real regressions.
  - Cons: requires a small change to how src/tests/builders get metadata.
- **Option 2: Filter the metadata line during snapshot comparison**
  - Pros: minimal product code changes.
  - Cons: risks masking real formatting regressions in the metadata line.

**Decision:** Prefer Option 1.

### 4) Name attribute icons (semantic formatting)

The spec requires:
- `resource_group_name` → 📁
- `name` → 🆔

**Decision:** Implement as part of existing semantic formatting helpers (not template-local string hacks):
- `FormatAttributeValueTable("name", value, provider)` → returns code-wrapped icon+value with non-breaking space.
- `FormatAttributeValueSummary("name", value, provider)` → returns summary-safe (HTML `<code>`) icon+value with non-breaking space.
- Same for `resource_group_name`.

**Additional alignment:** `ResourceChangeModel.SummaryHtml` is currently built in C# and formats `name`/`resource_group_name` without these new icons. The summary builder should switch to using the semantic formatting helpers for those attributes to keep summary and tables consistent.

## Consequences

### Positive
- Borders become consistent across all resources in AzDO HTML output.
- Screenshot tool can reliably target resources by address.
- Metadata line is deterministic in tests and configurable for users.
- Name icons follow existing semantic formatting patterns across contexts.

### Negative / Risks
- Requires touching multiple layers (CLI options, model, templates, tools, tests).
- Embedding commit hash may require a small build configuration change; if omitted, runtime must fall back.

## Implementation Notes (for Developer agent)

- Apply the `<details>` style string consistently:
  - Default resource wrapper (`_resource.sbn`)
  - Resource-specific templates that currently use `<div>` wrappers (wrap with `<details open>`)
- Add stable selectors on the outer resource `<details>` for screenshot targeting.
- Extend screenshot generator CLI with `--target` and `--target-selector` and implement partial capture logic.
- Add `--hide-metadata` to tfplan2md CLI and propagate to templates via the model.
- Implement deterministic metadata in snapshot tests via injection (preferred) rather than filtering.
- Extend semantic formatting helpers for `name` and `resource_group_name`, and ensure `SummaryHtml` construction uses those helpers.
