# Architecture: Display Enhancements

## Status

Proposed

## Context

Feature spec: [specification.md](specification.md)

This feature introduces four display improvements that cut across:
- **Large value rendering** (Scriban helper `ScribanHelpers.FormatLargeValue`)
- **Resource `<summary>` rendering** (C# precomputed `ResourceChangeModel.SummaryHtml`)
- **Sensitive value masking** (centralized in `ReportModelBuilder.BuildAttributeChanges`)
- **Semantic formatting** (`ScribanHelpers.FormatAttributeValue*`)

Key existing components / constraints:
- Markdown output is generated via Scriban templates (see [docs/adr-001-scriban-templating.md](../../adr-001-scriban-templating.md)).
- Summary `<summary>` lines are built in C# via `ResourceSummaryHtmlBuilder.BuildSummaryHtml(...)` and exposed to templates as `summary_html`.
- Sensitive masking is performed in `ReportModelBuilder.BuildAttributeChanges(...)` using Terraform’s `before_sensitive` / `after_sensitive` structures.
- Semantic icons are applied via `ScribanHelpers.FormatAttributeValueSummary/Table/Plain` (see name/resource-group icons from feature 029).

## Problem to Solve

1) Large JSON/XML values are currently rendered as plain code blocks or inline diffs, making them hard to review.
2) API Management (APIM) subresources often lack `name`, so current summary lines degrade to just the resource group.
3) `azurerm_api_management_named_value.value` is incorrectly masked even when `secret=false`.
4) Subscription-related attributes should be consistently recognizable across all render locations.

## Options Considered

### Option 1: Template-only formatting
Implement detection, summary enrichment, and icons inside Scriban templates.

- Pros
  - Minimal C# changes
- Cons
  - Duplicates logic across templates and contexts (table vs summary vs providers)
  - Hard to guarantee consistent behavior (and harder to test)
  - Sensitive override is security-sensitive and should not be “string hacked” at render time

### Option 2: Keep templates layout-focused; implement logic in C# helpers/model (recommended)
Centralize detection, summary enrichment, and semantic formatting in existing C# extension points.

- Pros
  - Matches existing architecture (helpers + precomputed `SummaryHtml`)
  - Minimizes duplication; consistent across providers and templates
  - Easier to unit test at the helper/model boundary
- Cons
  - Changes touch cross-cutting code paths (large value formatting, summary builder, semantic formatting)

## Decision

Choose **Option 2**.

## Proposed Technical Design

### 1) Syntax highlighting for large JSON/XML values

**Goal:** When a value is treated as “large”, detect JSON/XML, pretty-print it, and emit a fenced block with the appropriate language marker (`json` / `xml`).

**Primary integration point:** `ScribanHelpers.FormatLargeValue(...)` in `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValues.cs`.

**Approach (recommended):**
- Add a language inference step inside `FormatLargeValue`:
  - Try JSON detection by parsing trimmed content via `System.Text.Json`.
  - Try XML detection by parsing trimmed content via `System.Xml.Linq`.
  - If parsing fails, treat as plain text.
- Pretty-print when:
  - Parsing succeeds, and
  - Input does not already appear “pretty” (heuristic; see below).
- Emit fenced blocks with language marker for non-diff fences:
  - Create/delete large values: use `CodeFence(pretty, "json"/"xml")`.
  - Update large values:
    - `inline-diff` remains HTML (no fenced language). Apply pretty-print **before** diffing so the inline diff is readable.
    - `simple-diff` remains a diff fenced code block. Apply pretty-print **before** diffing so the diff operates on formatted lines.

**Formatting preservation heuristic:**
- If the content parses successfully and already contains newlines + leading indentation on multiple lines, treat it as “already formatted”.
- Optionally: compare the pretty-printed output to the normalized original (line-ending normalized) and keep the original if they match.

**Trade-offs:**
- Pretty-printing before diffing can increase “changed lines” (line-based diffs become noisier), but improves readability and aligns with the feature goal.
- Adding `json`/`xml` to code fences only applies where fences are used; inline-diff stays HTML (platform-neutral and already used).

**Security / reliability:**
- This feature must not bypass sensitivity masking. It operates only on values already passed into rendering (which are already masked as "(sensitive)" when appropriate).

### 2) API Management subresource summary enrichment

**Goal:** Improve `<summary>` lines for APIM subresources so reviewers get immediate context.

**Primary integration point:** `ResourceSummaryHtmlBuilder.BuildSummaryHtml(ResourceChangeModel model)` in `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs`.

**Rationale for this integration point:**
- The feature spec examples describe the `<details><summary>...` line.
- That line is produced from `ResourceChangeModel.SummaryHtml`, which is precomputed in C#.

**Design:**

#### 2.1 Always include `api_management_name` when present
- If `flatState` contains `api_management_name`, append it to the primary context.
- Place it **before** the `in 📁 resource_group_name` segment to match the spec’s example ordering.

#### 2.2 Special-case `azurerm_api_management_api_operation`
- When `model.Type == "azurerm_api_management_api_operation"`:
  - If present, append `display_name` to the *prefix* (after the bold Terraform local name).
    - This matches the target format: `... this `display_name` — ...`
  - Build detail parts with:
    - `operation_id`
    - `api_name`
    - `api_management_name` (if present)
    - `resource_group_name` (existing)

**Formatting rules:**
- Use existing semantic formatting helpers where appropriate:
  - `resource_group_name` continues to use 📁 icon.
  - Values that are effectively identifiers (`operation_id`, `api_name`, `api_management_name`) should be wrapped using `FormatCodeSummary(...)` or `FormatAttributeValueSummary(...)` where it produces equivalent safe output.

**Note on generic coverage (“all APIM subresources with api_management_name”):**
- Implement `api_management_name` handling generically (presence-based) so other `azurerm_api_management_*` resources benefit without maintaining a large mapping list.

### 3) Named values: show actual value when `secret=false`

**Goal:** Override provider sensitivity marking for `azurerm_api_management_named_value.value` when the resource’s `secret` attribute is `false`.

**Primary integration point:** `ReportModelBuilder.BuildAttributeChanges(Change change, string providerName)` in `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`.

**Why here:**
- Sensitivity is decided centrally using Terraform’s `before_sensitive` / `after_sensitive` flags.
- The incorrect masking occurs *before* rendering; fixing it at render time would be fragile.

**Design:**
- For resources of type `azurerm_api_management_named_value`:
  - Determine the effective `secret` value from before/after dictionaries (prefer `after` on create/update; fall back to `before`).
  - When processing the `value` key:
    - If `secret == "false"` (or boolean false equivalent), force `isSensitive = false`.
    - If `secret == "true"`, keep existing behavior.

**Safety:**
- This override is scoped to a single resource type + single attribute name.
- It does not change global sensitive handling and does not weaken `--show-sensitive` semantics.

### 4) Subscription attribute emoji (🔑)

**Goal:** Add a consistent 🔑 indicator for subscription attributes across all providers and contexts.

**Primary integration point:** `ScribanHelpers.FormatAttributeValue*` in `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting.cs` (and/or a new semantic formatting partial alongside existing identity/name helpers).

**Design:**
- Add a semantic formatter rule:
  - When `attributeName` equals `subscription_id` or `subscription` (case-insensitive), prefix the displayed value with `🔑` and a non-breaking space.
  - Ensure behavior is correct for:
    - Table context (markdown code spans)
    - Summary context (HTML `<code>` spans)
    - Plain context (no wrappers)

**Behavioral note:**
- This approach follows the existing semantic icon model (icons are attached to the **value**). This yields:
  - Table value: `🔑 <id>`
  - Key/value list value: `🔑 <id>`

## Components Affected (Implementation Guidance)

Developer work is expected in:
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/LargeValues.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ResourceSummaryHtmlBuilder.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting*.cs`

## Testing Strategy

Add/extend unit tests to cover:
- Large value formatting:
  - JSON detection + pretty-print + fenced code block with language `json` (create/delete)
  - XML detection + pretty-print + fenced code block with language `xml` (create/delete)
  - Update paths: pretty-print before diff for both `inline-diff` and `simple-diff`
- Summary HTML for APIM:
  - `azurerm_api_management_api_operation` includes `display_name`, `operation_id`, `api_name`, `api_management_name`, and 📁 resource group
  - `azurerm_api_management_named_value` includes `api_management_name` in the right position
- Sensitive override:
  - Named value `value` is shown when `secret=false` even if plan marks it sensitive
  - Named value `value` remains masked when `secret=true`
- Semantic formatting:
  - `FormatAttributeValueTable("subscription_id", "...", ...)` prefixes with 🔑
  - Same for `subscription`

## Risks and Mitigations

- **Diff noise from pretty-printing:** Acceptable trade-off; mitigation is to keep existing diff formats (`inline-diff` / `simple-diff`) and only change the input lines.
- **Parsing failures on malformed JSON/XML:** Must fall back to original content without throwing.
- **Security regression from sensitivity override:** Mitigated by strict scoping to `azurerm_api_management_named_value.value` and requiring `secret=false`.
