# Architecture: Replacement Reasons and Resource Summaries

## Status

Proposed

## Context

Users currently have to expand the `<details>` section of every resource change to understand what is happening. This is inefficient for large plans. Additionally, when a resource is being replaced, it is not immediately obvious *why* (which attribute triggered the replacement).

The Terraform plan JSON (v1.2+) includes a `replace_paths` field that identifies the attributes forcing a replacement.

We need to:
1.  Display a concise summary line for every resource change.
2.  Parse and display the replacement reasons.

## Options Considered

### Option 1: Logic in Template
- **Description**: Implement all summary logic (attribute selection, formatting) inside the Scriban templates.
- **Pros**: No C# code changes for formatting logic.
- **Cons**: Scriban is not well-suited for complex logic (fallback chains, parsing paths). Hard to test. Hard to maintain.

### Option 2: Logic in `ReportModelBuilder`
- **Description**: Add private methods to `ReportModelBuilder` to generate summaries.
- **Pros**: Simple, keeps everything in one place.
- **Cons**: `ReportModelBuilder` is already doing a lot. Violates Single Responsibility Principle. Harder to unit test summary logic in isolation.

### Option 3: Dedicated `ResourceSummaryBuilder` Service (Selected)
- **Description**: Create a new `IResourceSummaryBuilder` service responsible for generating summary strings.
- **Pros**: Clean separation of concerns. Easy to unit test. Extensible.
- **Cons**: Adds a new class/file.

## Decision

We will implement **Option 3: Dedicated `ResourceSummaryBuilder` Service**.

This service will encapsulate:
- Resource-specific attribute mappings.
- Provider-level fallback rules.
- Generic fallback logic.
- Replacement reason parsing and formatting.
- Action-specific summary formatting (CREATE, UPDATE, REPLACE, DELETE).

## Rationale

- **Testability**: We can write focused unit tests for `ResourceSummaryBuilder` covering all edge cases (missing attributes, different action types, fallback scenarios) without setting up a full `TerraformPlan`.
- **Maintainability**: Adding new resource mappings will be done in a dedicated registry class, keeping the core logic clean.
- **Performance**: Summary generation happens once during model building, not during rendering.

## Implementation Details

### 1. Data Model Changes

**`src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`**
Update `Change` record to include `replace_paths`:

```csharp
public record Change(
    // ... existing properties ...
    [property: JsonPropertyName("replace_paths")] IReadOnlyList<IReadOnlyList<object>>? ReplacePaths = null
);
```

**`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`**
Update `ResourceChangeModel` to include summary data:

```csharp
public class ResourceChangeModel
{
    // ... existing properties ...
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; init; }
    public string? Summary { get; init; }
}
```

### 2. New Service: `ResourceSummaryBuilder`

Create `src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/IResourceSummaryBuilder.cs`:

```csharp
public interface IResourceSummaryBuilder
{
    string BuildSummary(ResourceChangeModel change);
}
```

Create `src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/ResourceSummaryBuilder.cs`:
- Implements `IResourceSummaryBuilder`.
- Contains the logic defined in the specification.
- Uses a private `ResourceAttributeMappingRegistry` to look up keys.

### 3. Integration

**`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`**
Update `ReportModelBuilder` to use the new service:

```csharp
public class ReportModelBuilder(IResourceSummaryBuilder summaryBuilder, bool showSensitive = false, bool showUnchangedValues = false)
{
    // ...
    private ResourceChangeModel BuildResourceChangeModel(ResourceChange change)
    {
        // ... build model ...
        var model = new ResourceChangeModel { ... };
        
        // Generate summary
        var summary = _summaryBuilder.BuildSummary(model);
        
        return model with { Summary = summary };
    }
}
```

**`src/Oocx.TfPlan2Md/Program.cs`**
- Instantiate `ResourceSummaryBuilder`.
- Pass it to `ReportModelBuilder`.

### 4. Template Updates

**`src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`**
Add the summary line:

```scriban
{{ if change.summary }}
**Summary:** {{ change.summary }}

{{ end }}
<details>
```

## Components Affected

- `src/Oocx.TfPlan2Md/Parsing/TerraformPlan.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Summaries/` (New folder)
- `src/Oocx.TfPlan2Md/Program.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/default.sbn`
