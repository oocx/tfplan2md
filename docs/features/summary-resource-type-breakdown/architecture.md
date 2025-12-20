# Architecture: Summary Resource Type Breakdown

## Status

Proposed

## Context

The current summary table in the generated markdown report only shows the count of resources for each action (Add, Change, Replace, Destroy). Users want to see a breakdown of resource types for each action to quickly understand what is changing without reading the detailed section.

The product has not yet been released, so backward compatibility is not a constraint.

Reference: [Feature Specification](specification.md)

## Options Considered

### Option 1: Add Breakdown Properties to SummaryModel
Add specific properties for each action's breakdown to the existing `SummaryModel`.
- `IReadOnlyList<ResourceTypeBreakdown> ToAddBreakdown`
- `IReadOnlyList<ResourceTypeBreakdown> ToChangeBreakdown`
- ... etc.

**Pros:**
- Simple to implement.
- Easy to access in Scriban templates (`summary.to_add_breakdown`).

**Cons:**
- Adds 4 new properties to `SummaryModel`, making it slightly larger and less cohesive.
- Separates the count from the breakdown data for the same action.

### Option 2: Refactor SummaryModel to use Objects
Change `ToAdd`, `ToChange`, etc. from `int` to an object containing both count and breakdown.
- `ActionSummary ToAdd`
- `ActionSummary ToChange`
where `ActionSummary` contains `int Count` and `IReadOnlyList<ResourceTypeBreakdown> Breakdown`.

**Pros:**
- **Cleaner Object Model**: Groups related data (count and breakdown) together logically.
- **Extensible**: Easier to add more properties to an action summary in the future without polluting `SummaryModel`.

**Cons:**
- Requires template updates to use `summary.to_add.count` instead of `summary.to_add`.

### Option 3: Generic Breakdown Dictionary
Add a single dictionary `Breakdowns` to `SummaryModel` keyed by action.

**Pros:**
- Extensible.

**Cons:**
- Slightly more verbose template syntax.

## Decision

**Option 2: Refactor SummaryModel to use Objects**

We will refactor `SummaryModel` to use a structured object (`ActionSummary`) for each action property, containing both the count and the breakdown.

## Rationale

- **Cohesion**: Since backward compatibility is not a constraint, we prefer a cleaner domain model where the count and breakdown for an action are grouped together.
- **Extensibility**: This structure allows us to easily add more metadata to action summaries in the future (e.g., "sensitive count") without adding top-level properties to `SummaryModel`.

## Implementation Notes

### 1. Data Model Changes (`ReportModel.cs`)

Introduce new records:

```csharp
public record ResourceTypeBreakdown(string Type, int Count);

public record ActionSummary(int Count, IReadOnlyList<ResourceTypeBreakdown> Breakdown);
```

Update `SummaryModel` to use `ActionSummary`:

```csharp
public class SummaryModel
{
    public required ActionSummary ToAdd { get; init; }
    public required ActionSummary ToChange { get; init; }
    public required ActionSummary ToDestroy { get; init; }
    public required ActionSummary ToReplace { get; init; }
    public required ActionSummary NoOp { get; init; }
    public int Total { get; init; }
}
```

### 2. Logic Changes (`ReportModelBuilder.cs`)

Update `ReportModelBuilder.Build` method:
- Create a helper method `BuildActionSummary(IEnumerable<ResourceChangeModel> changes)` that:
  - Calculates the total count.
  - Groups by `Type`, counts, and sorts alphabetically to create the breakdown list.
  - Returns an `ActionSummary`.
- Use this helper to populate `SummaryModel`.

### 3. Template Changes (`default.sbn`)

Update the summary table to reflect the new structure:

```scriban
| Action | Count | Resource Types |
|--------|-------|----------------|
| ➕ Add | {{ summary.to_add.count }} | {{ for type in summary.to_add.breakdown }}{{ type.count }} {{ type.type }}<br/>{{ end }} |
| 🔄 Change | {{ summary.to_change.count }} | {{ for type in summary.to_change.breakdown }}{{ type.count }} {{ type.type }}<br/>{{ end }} |
| ♻️ Replace | {{ summary.to_replace.count }} | {{ for type in summary.to_replace.breakdown }}{{ type.count }} {{ type.type }}<br/>{{ end }} |
| ❌ Destroy | {{ summary.to_destroy.count }} | {{ for type in summary.to_destroy.breakdown }}{{ type.count }} {{ type.type }}<br/>{{ end }} |
| **Total** | **{{ summary.total }}** | |
```

### 4. Testing
- Update `ReportModelBuilderTests` to verify the new object structure and breakdown logic.
- Update `MarkdownRendererTests` to verify the template renders correctly with the new model.
