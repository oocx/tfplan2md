# Architecture: Universal Azure Resource ID Formatting

## Status

Implemented (model-driven large attribute classification)

## Context

The `azurerm` provider often outputs long Azure Resource IDs (e.g., `/subscriptions/...`). These IDs are currently treated as "large values" because they exceed the 100-character threshold, moving them out of the main change table into a collapsible section. This makes the report harder to scan.

We already have logic in `AzureScopeParser` to format these IDs into readable strings (e.g., `Key Vault **kv** in ...`) for role assignments. We want to apply this formatting universally to all `azurerm` resource attributes that contain Azure Resource IDs, and force them to appear in the main change table.

## Options Considered

### Option 1: Attribute Name Heuristics
- **Description**: Format values only for attributes ending in `_id` or named `scope`.
- **Pros**: Less risk of false positives.
- **Cons**: brittle; misses attributes that don't follow naming conventions; requires maintenance of attribute lists.

### Option 2: Pattern-Based Detection (Selected)
- **Description**: Detect Azure Resource IDs by their structure (starting with `/subscriptions/` or `/providers/Microsoft.Management/`).
- **Pros**: Universal application; zero maintenance for new resources; consistent user experience.
- **Cons**: Theoretical risk of false positives (low, given the specific structure).

## Decision

We will implement **Option 2: Pattern-Based Detection**.

We will modify the rendering pipeline to:
1. Detect if a value is an Azure Resource ID using `AzureScopeParser`.
2. If detected (and provider is `azurerm`), exempt it from "Large Value" classification so it stays in the main table.
3. Format the value using `AzureScopeParser.ParseScope` instead of displaying the raw string.
4. Compute a per-attribute `IsLarge` flag in C# (considering provider-aware rules) and expose it on `AttributeChangeModel`, so templates no longer need to call `is_large_value` with provider context.

## Rationale

- **Readability**: Readable strings are much shorter and easier to understand than raw GUID-filled paths.
- **Consistency**: Users expect Azure IDs to look the same everywhere in the report.
- **Robustness**: The existing parser is already tested and handles various scope types correctly.

## Consequences

### Positive
- Azure Resource IDs will be readable and visible in the main table.
- Reports will be more concise.

### Negative
- Very long formatted strings (e.g., deeply nested resources with long names) might widen the table columns, but this is preferable to hiding the ID.

## Implementation Notes

### 1. `AzureScopeParser` Updates
- Add `public static bool IsAzureResourceId(string? scope)` method.
- Implementation should rely on `Parse(scope).Level != ScopeLevel.Unknown`.

### 2. `ScribanHelpers` Updates
- `IsLargeValue` remains available for custom templates but is now consumed by the model builder to set `AttributeChangeModel.IsLarge`.
- `FormatValue(string? value, string? providerName)` remains the canonical formatter used by templates for provider-aware rendering (Azure IDs → readable scopes; others → backticked).

### 3. Model Updates (`AttributeChangeModel` and `ReportModelBuilder`)
- Add `IsLarge` flag to `AttributeChangeModel`.
- Compute `IsLarge` in `ReportModelBuilder.BuildAttributeChanges` using `IsLargeValue` with provider context and displayed values (after sensitivity masking).
- This moves provider-aware large-value classification out of Scriban and into C#.

### 4. Template Updates (`default.sbn`, `azurerm/role_assignment.sbn`)
- Templates classify attributes using `attr.is_large` instead of calling `is_large_value` or passing `provider_name`.
- Tables still use `format_value` with `change.provider_name` for provider-aware display, but large-value routing is now model-driven.

### 4. Testing
- Verify `IsLargeValue` returns false for long Azure IDs.
- Verify `FormatValue` returns readable string for Azure IDs and backticked string for others.
- Verify template renders correctly.
