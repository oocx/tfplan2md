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

Architectural constraint (project rule):
- **Provider-specific code must live under `Providers/` and platform-specific code must live under `Platforms/`.**
- `MarkdownGeneration/` is generic and must not contain checks for provider resource types (e.g., `azurerm_*`) or provider-specific attribute semantics.
- Generic code may expose **extension points** (interfaces/registries) that provider/platform modules can plug into, but must not embed provider/platform logic itself.

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
Centralize detection and semantic formatting in generic C# helpers, and implement provider-specific behaviors in `Providers/` using existing extension points.

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

**Primary integration point:** `IResourceViewModelFactory.ApplyViewModel(...)` implementations registered from `Providers/AzureRM/`.

**Rationale for this integration point:**
- APIM is AzureRM provider-specific. Summary enrichment rules based on `azurerm_api_management_*` types must not be implemented in `MarkdownGeneration/`.
- The codebase already supports per-resource-type customization via the view model factory registry (`ReportModelBuilder` calls `_viewModelFactoryRegistry.TryGetFactory(rc.Type, out var factory)` and then `factory.ApplyViewModel(...)`).

**Design:**

#### 2.1 Add AzureRM factory for APIM named values
- Register a factory for `azurerm_api_management_named_value` under `Providers/AzureRM/`.
- Factory reads `api_management_name`, `name`, `resource_group_name`, and `location` from the resource state.
- Factory constructs a provider-specific `model.SummaryHtml` matching the spec’s ordering, including `api_management_name` **before** the `in 📁 resource_group_name` segment.

#### 2.2 Add AzureRM factory for APIM API operations
- Register a factory for `azurerm_api_management_api_operation` under `Providers/AzureRM/`.
- Factory constructs a provider-specific `model.SummaryHtml` matching the spec:
  - Prefix includes the bold Terraform local name and appends `display_name` when present.
  - Details include `operation_id`, `api_name`, `api_management_name`, and `in 📁 resource_group_name`.

**Formatting rules:**
- Provider factories should reuse existing generic helper utilities (e.g., `FormatAttributeValueSummary(...)`) to ensure consistent HTML escaping and icons.

**Note on coverage (“all APIM subresources with api_management_name”):**
- The registry is keyed by resource type; implement the factories for the APIM resource types explicitly in scope for this feature.
- If broader APIM coverage is required later, add additional AzureRM factories rather than embedding pattern matching (e.g., `azurerm_api_management_*`) in generic code.

**Generic code change (allowed, non-provider-specific):**
- Ensure the core pipeline respects provider overrides. Specifically: if a factory sets `model.SummaryHtml`, the generic path must not overwrite it.
- This remains provider-neutral and keeps APIM rules out of `MarkdownGeneration/`.

### 3) Named values: show actual value when `secret=false`

**Goal:** Override provider sensitivity marking for `azurerm_api_management_named_value.value` when the resource’s `secret` attribute is `false`.

**Primary integration point:** `IResourceViewModelFactory.ApplyViewModel(...)` for `azurerm_api_management_named_value` under `Providers/AzureRM/`.

**Why here:**
- This is AzureRM-specific attribute semantics and must not be implemented in `MarkdownGeneration/`.
- The factory is called with both the raw `ResourceChange` (which contains `before`/`after` and sensitivity structures) and the computed `attributeChanges`, and it can update `model.AttributeChanges`.

**Design:**
- In the AzureRM factory:
  - Determine effective `secret` from the resource state (`after` preferred; fall back to `before`).
  - If `secret` is false:
    - Rebuild `model.AttributeChanges` so that the `value` attribute uses the real `before`/`after` values and `IsSensitive = false`.
  - If `secret` is true:
    - Keep the default behavior produced by generic masking.

**Safety:**
- Override is scoped to one AzureRM resource type and one attribute.
- This does not change global sensitive handling and does not weaken `--show-sensitive` semantics.

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
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/SemanticFormatting*.cs`

Provider-specific work is expected in:
- `src/Oocx.TfPlan2Md/Providers/AzureRM/` (new or updated factories registered via `AzureRMModule.RegisterFactories(...)`)

Provider-neutral pipeline work may be required in:
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs` (to ensure it does not overwrite `model.SummaryHtml` when a provider factory sets it)

## Testing Strategy

Add/extend unit tests to cover:
- Large value formatting:
  - JSON detection + pretty-print + fenced code block with language `json` (create/delete)
  - XML detection + pretty-print + fenced code block with language `xml` (create/delete)
  - Update paths: pretty-print before diff for both `inline-diff` and `simple-diff`
- Provider-specific APIM summary HTML (AzureRM factories):
  - API operation summary includes `display_name`, `operation_id`, `api_name`, `api_management_name`, and 📁 resource group
  - Named value summary includes `api_management_name` in the right position
- Provider-specific sensitive override (AzureRM factory):
  - Named value `value` is shown when `secret=false` even if plan marks it sensitive
  - Named value `value` remains masked when `secret=true`
- Semantic formatting:
  - `FormatAttributeValueTable("subscription_id", "...", ...)` prefixes with 🔑
  - Same for `subscription`

## Risks and Mitigations

- **Diff noise from pretty-printing:** Acceptable trade-off; mitigation is to keep existing diff formats (`inline-diff` / `simple-diff`) and only change the input lines.
- **Parsing failures on malformed JSON/XML:** Must fall back to original content without throwing.
- **Security regression from sensitivity override:** Mitigated by strict scoping to `azurerm_api_management_named_value.value` and requiring `secret=false`.
- **Architecture drift (provider logic in generic code):** Mitigated by enforcing that any `azurerm_*` checks live only in `Providers/AzureRM/` (or shared Azure semantics in `Platforms/Azure/`).

## Architecture Compliance Checklist

Use this as a code review checklist for Feature 051 changes.

- No `azurerm_*` (or other provider/platform) resource-type branching in `src/Oocx.TfPlan2Md/MarkdownGeneration/`.
- Provider-specific rules (APIM summary enrichment, named-value sensitivity override) implemented only in `src/Oocx.TfPlan2Md/Providers/AzureRM/` via `IResourceViewModelFactory`.
- Generic code exposes only provider-neutral extension points (registries/interfaces) and does not encode provider semantics.
- `ReportModelBuilder` respects factory overrides (e.g., `model.SummaryHtml`) and does not overwrite provider-computed values.
- Tests validate behavior at the correct layer: generic formatting tests for JSON/XML + 🔑; provider tests for APIM summary and named-value sensitivity.
