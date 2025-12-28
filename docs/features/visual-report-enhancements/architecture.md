# Architecture: Visual Report Enhancements

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

The feature changes the **default, built-in markdown output** to:
- Separate modules with horizontal rules and a 📦 icon
- Render each resource change as a single `<details>` block with a rich `<summary>` line
- Apply consistent “semantic value formatting” (✅/❌ booleans, ✅ Allow / ⛔ Deny, protocol/direction icons, 🌐 for CIDRs/IPs, tags as inline badges)

Key existing architecture constraints:
- Reports are generated via **Scriban templates** (see [docs/architecture.md](../../architecture.md) and [docs/adr-001-scriban-templating.md](../../adr-001-scriban-templating.md))
- The renderer currently renders the default template, then **regex-replaces** per-resource blocks when a resource-specific template exists
- Azure DevOps markdown has known quirks: **markdown backticks inside `<summary>` are not reliably rendered**, so summary lines must use HTML `<code>` for code formatting

## Problem to Solve

We need to implement the new UX while preserving:
- Cross-platform rendering (GitHub + Azure DevOps)
- Resource-specific templates support (e.g., NSG/firewall semantic diffs)
- Markdown validity (tables must not break, spacing rules must hold)

## Options Considered

### Option 1: Template-only change (no C# changes)
Modify only `default.sbn` and embedded resource templates to output `<details>/<summary>` and icons.

- Pros
  - Fastest to prototype
  - No runtime behavior changes
- Cons
  - Breaks resource-specific template replacement: the current replacement anchor is the `#### {action} {address}` heading
  - Summary-line code formatting requires `<code>`, but existing summary strings and helpers produce markdown backticks
  - Duplicates formatting logic in templates (hard to keep consistent)

### Option 2: Minimal renderer + helper extensions (recommended)
Keep the template architecture, but add:
1) **Stable, non-visual anchors** for per-resource replacement
2) **Context-aware formatting helpers** for “table markdown” vs “summary HTML”

- Pros
  - Preserves current template system and resource-specific overrides
  - Centralizes formatting rules for consistency (one place to maintain)
  - Makes Azure DevOps compatibility explicit (HTML code in summary)
- Cons
  - Requires small C# changes in `MarkdownRenderer` and `ScribanHelpers`
  - Requires updating resource-specific templates to use new anchors

### Option 3: Refactor rendering pipeline to avoid regex replacement
Render the full report by selecting resource templates during a single pass, rather than render+replace.

- Pros
  - Cleaner architecture long-term
  - No regex post-processing of the output
- Cons
  - Larger change surface area
  - Higher regression risk (templating behavior, spacing normalization)

## Decision

Choose **Option 2** (minimal renderer + helper extensions).

## Rationale

This feature primarily changes presentation, but it intersects with:
- Existing **template replacement mechanism** (currently keyed by headings)
- Azure DevOps limitations for code rendering inside `<summary>`

Option 2 is the smallest approach that:
- Delivers the requested UX
- Keeps resource-specific templates working
- Avoids template duplication for value formatting

## Proposed Technical Design

## Guiding Principle

Keep Scriban templates **layout-focused** (loops + conditionals + printing prepared values). Put all non-trivial string composition and formatting logic into **C# helpers** and/or **precomputed model properties**.

### 1) Introduce stable resource block anchors

**Goal:** Allow `MarkdownRenderer` to replace a resource’s rendered block without relying on visible headings.

**Approach:** Wrap each resource block with HTML comment markers that do not render visually.

Example (conceptual):
```markdown
<!-- tfplan2md:resource-start address=module.net.azurerm_subnet.app[0] -->
<details>
<summary>…</summary>
<br>
…
</details>
<!-- tfplan2md:resource-end address=module.net.azurerm_subnet.app[0] -->
```

**Renderer change:** Update the replacement regex to match from `resource-start` to `resource-end` for a given `change.Address`.

**Template change:**
- `default.sbn` must emit these markers for every resource.
- Resource-specific templates must emit markers too, so the replacement produces a structurally identical unit.

### 2) Add context-aware formatting helpers

We need two distinct formatting contexts:
- **Table context:** markdown tables with backtick code spans (current behavior)
- **Summary context:** HTML `<summary>` where code spans must be `<code>`

**Add Scriban helpers (C#):**
- `format_code_table(text)` → returns `` `...` `` (escaped)
- `format_code_summary(text)` → returns `<code>...</code>` (escaped)
- `format_attribute_value_table(attr_name, value, provider)` → returns a markdown-safe value applying semantic icons
- `format_attribute_value_summary(attr_name, value, provider)` → returns a summary-safe value applying semantic icons

Additionally, add helpers intended to keep templates simple:
- `format_summary_html(change)` → returns the full `<summary>...` inner content (already using `<code>` where needed)
- `format_changed_attributes_summary(change)` → returns `2 🔧 attr1, attr2, +N more` (or empty when not applicable)
- `format_tags_badges(tags)` → returns `**🏷️ Tags:** ...` (already correctly code-formatted)

**Semantic mappings (per spec):**
- Booleans: `true` → `✅ true`, `false` → `❌ false`
- Access: `Allow` → `✅ Allow`, `Deny` → `⛔ Deny`
- Direction: `Inbound` → `⬇️ Inbound`, `Outbound` → `⬆️ Outbound`
- Protocol: `Tcp` → `🔗 TCP`, `Udp` → `📨 UDP`, `Icmp` → `📡 ICMP`, `*` → `✳️ *`
- CIDR/IP detection: prefix inside code: `🌐 10.0.0.0/16`
- Location formatting: icon inside code, typically wrapped in parentheses in summaries: `(<code>🌍 eastus</code>)`

### 3) Precompute summary-line strings in C# (preferred)

The default template currently displays a visible heading `#### {action} {address}` and an optional `**Summary:** ...` paragraph.

New target structure:
- Outer `<details>` per resource
- `<summary>` contains action icon + type + bold+code Terraform name + human-friendly context

**Data sources:**
- Use `change.Type` and `change.Name` directly (already on the model)
- Prefer precomputed, summary-safe fields on the model (or helper output) rather than composing strings in templates

**Proposed model additions (C#):**
- `ResourceChangeModel.SummaryHtml` (string): the full human context portion, already `<summary>`-safe (uses `<code>`, not backticks)
- `ResourceChangeModel.ChangedAttributesSummary` (string): `2 🔧 attr1, attr2, +N more` (empty when not update)
- `ResourceChangeModel.TagsBadges` (string?): `**🏷️ Tags:** ...` for create/delete when tags are present

**Changed-attributes summary format:** For `action == "update"`
- Compute count + preview list in C# (same truncation rules as today)
- Render as: `2 🔧 attr1, attr2, +N more`

This avoids relying on `ResourceSummaryBuilder`’s current `"Changed:"` phrasing while also keeping templates simple.

### 4) Tags as inline badges (create/delete focus)

Prefer extracting and formatting tags in C# and passing a prepared `TagsBadges` (or equivalent) into the template.

Proposed behavior:
- For `create`/`delete`, extract `tags.*` values and format the inline badge string in C#.
- For `update`, keep tag changes in the Before/After table (because badges cannot represent diffs cleanly without additional data).

This stays within the data available to `default.sbn` without requiring full JSON traversal.

### 5) Module separators and headers

Modify `default.sbn`:
- Emit `---` between module groups (not before the first)
- Change module heading to: `### 📦 Module: ...`

### 6) Resource-specific templates

Resource-specific templates must align with the new “resource block unit” shape:
- Emit the same HTML comment anchors
- Emit outer `<details>/<summary>` structure
- Keep their internal semantic tables (e.g., NSG and firewall rule diffs)

They should prefer calling the new helpers for:
- `Access`, `Direction`, `Protocol`
- CIDR/IP lists

This ensures global consistency across templates.

## Confirmed UX Decision

Location values are formatted as code with the icon inside the code span in summary context: `(<code>🌍 eastus</code>)`.

## Consequences

### Positive
- Stable replacement mechanism that does not leak visible anchors
- Consistent semantic formatting across default and resource-specific templates
- Explicit support for Azure DevOps `<summary>` code formatting

### Negative / Risks
- Requires touching both templates and renderer logic (small but cross-cutting)
- Regex replacement becomes dependent on marker correctness; templates must always emit matching start/end markers

## Implementation Notes (for Developer agent)

- Update template replacement in `MarkdownRenderer` to anchor on HTML comments rather than headings.
- Add summary-safe formatting helpers in `ScribanHelpers` and use them in templates.
- Update `default.sbn` to remove per-resource `####` headings and emit outer `<details>/<summary>`.
- Update the embedded resource-specific templates (`azurerm/network_security_group.sbn`, `azurerm/firewall_network_rule_collection.sbn`, `azurerm/role_assignment.sbn`) to match the new block shape.
- Update/extend tests and snapshots:
  - Prefer keeping `ResourceSummaryBuilder` stable; implement the new `2 🔧 ...` update-summary formatting via new model fields/helpers used by templates.
  - Demo artifacts/snapshots should be regenerated via existing scripts.
