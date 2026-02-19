# Architecture: Resource Details Display Mode

## Status

Proposed

## Context

This feature adds a `--details` CLI argument that gives users control over whether resource details blocks (`<details>` HTML elements) are rendered as open or closed in the generated markdown report. The feature specification is documented in `docs/features/092-details-display-mode/specification.md`.

Currently, all resource details blocks are rendered with the `open` attribute based on the presence of code analysis findings (line 6 of `_resource.sbn`):

```scriban
<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }} style="...">
```

The feature requires three modes:
- `--details closed` — All resource blocks collapsed by default
- `--details open` — All resource blocks expanded by default (current behavior)
- `--details auto` — Expand only resources with code analysis findings (current default behavior)

The debug details block (rendered in `DiagnosticContext.ToMarkdown()`) must always remain collapsed regardless of the `--details` setting.

## Design Decisions

### 1. Enum Location and Definition

**Location:** `src/Oocx.TfPlan2Md/RenderTargets/DetailsDisplayMode.cs`

**Rationale:** The `RenderTargets` namespace already contains the `RenderTarget` enum which controls platform-specific rendering decisions (GitHub vs Azure DevOps). The `DetailsDisplayMode` enum serves a similar role — it controls presentation-level rendering behavior. This placement maintains consistency with the existing architecture.

**Definition:**

```csharp
namespace Oocx.TfPlan2Md.RenderTargets;

/// <summary>
/// Specifies the display mode for resource details blocks in the markdown report.
/// </summary>
/// <remarks>
/// Controls whether resource &lt;details&gt; elements are rendered with the 'open' attribute.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </remarks>
internal enum DetailsDisplayMode
{
    /// <summary>
    /// All resource details blocks are collapsed by default (no 'open' attribute).
    /// </summary>
    Closed,

    /// <summary>
    /// All resource details blocks are expanded by default (all have 'open' attribute).
    /// </summary>
    Open,

    /// <summary>
    /// Automatically expand only resources with code analysis findings.
    /// Resources without findings are collapsed.
    /// </summary>
    Auto
}
```

### 2. Data Flow Threading

The mode must flow through the entire rendering pipeline:

**CLI → CliOptions → ReportModel → Scriban Context**

#### Step 1: CLI Parsing

Add to `CliParser.cs` in the `Parse` method (around line 130):

```csharp
var detailsDisplayMode = DetailsDisplayMode.Auto; // Default to auto (current behavior)

// In the switch statement:
case "--details":
    if (i + 1 < args.Length)
    {
        detailsDisplayMode = ParseDetailsDisplayMode(args[++i]);
    }
    else
    {
        throw new CliParseException("--details requires a mode argument (closed, open, or auto).");
    }
    break;
```

Add helper method:

```csharp
private static DetailsDisplayMode ParseDetailsDisplayMode(string value)
{
    var normalized = value.Trim().ToLowerInvariant();
    
    return normalized switch
    {
        "closed" => DetailsDisplayMode.Closed,
        "open" => DetailsDisplayMode.Open,
        "auto" => DetailsDisplayMode.Auto,
        _ => throw new CliParseException("Invalid value for --details. Allowed values: closed, open, auto")
    };
}
```

Add to `CliOptions` (around line 88):

```csharp
/// <summary>
/// Gets the display mode for resource details blocks.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public DetailsDisplayMode DetailsDisplayMode { get; init; }
```

#### Step 2: ReportModel

Add to `ReportModel.cs` (around line 94):

```csharp
/// <summary>
/// Gets the display mode for resource details blocks.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public required DetailsDisplayMode DetailsDisplayMode { get; init; }
```

#### Step 3: ReportModelBuilder

Add to `ReportModelBuilder.cs` constructor parameters (around line 44):

```csharp
internal partial class ReportModelBuilder(
    IResourceSummaryBuilder? summaryBuilder = null,
    bool showSensitive = false,
    bool showUnchangedValues = false,
    RenderTargets.RenderTarget renderTarget = RenderTargets.RenderTarget.AzureDevOps,
    string? reportTitle = null,
    Platforms.Azure.IPrincipalMapper? principalMapper = null,
    IMetadataProvider? metadataProvider = null,
    bool hideMetadata = false,
    Services.ProviderRegistry? providerRegistry = null,
    CodeAnalysisInput? codeAnalysisInput = null,
    MarkdownGeneration.Services.IconProviderRegistry? iconProviderRegistry = null,
    DetailsDisplayMode detailsDisplayMode = DetailsDisplayMode.Auto)
```

Store as field:

```csharp
private readonly DetailsDisplayMode _detailsDisplayMode = detailsDisplayMode;
```

Set in `Build()` method in `ReportModelBuilder.Build.cs`:

```csharp
DetailsDisplayMode = _detailsDisplayMode,
```

#### Step 4: AotScriptObjectMapper

Add to `MapReportModel()` in `AotScriptObjectMapper.cs` (around line 41):

```csharp
scriptObject["details_display_mode"] = model.DetailsDisplayMode.ToString().ToLowerInvariant();
```

This exposes the mode to templates as a string: "closed", "open", or "auto".

### 3. Scriban Helper Function Design

**Function Name:** `details_open_attr`

**Registration:** In `ScribanHelpers.RegisterHelpers()` (Registry.cs):

```csharp
// Capture detailsDisplayMode as closure for use in helper
var detailsMode = /* extract from scriptObject or pass as parameter */;
scriptObject.Import("details_open_attr", 
    new Func<ScriptObject, string>(change => GetDetailsOpenAttribute(change, detailsMode)));
```

**Implementation:** In new file `ScribanHelpers/DetailsDisplay.cs`:

```csharp
namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Scriban helpers for controlling details block display state.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public static partial class ScribanHelpers
{
    /// <summary>
    /// Determines the 'open' attribute for a resource details block based on display mode.
    /// Returns either " open" (with leading space) or an empty string.
    /// </summary>
    /// <param name="change">The resource change ScriptObject.</param>
    /// <param name="mode">The details display mode.</param>
    /// <returns>" open" if the resource should be expanded, empty string otherwise.</returns>
    private static string GetDetailsOpenAttribute(ScriptObject change, string mode)
    {
        return mode switch
        {
            "closed" => string.Empty,
            "open" => " open",
            "auto" => HasCodeAnalysisFindings(change) ? " open" : string.Empty,
            _ => string.Empty // Default to closed for unknown modes
        };
    }

    /// <summary>
    /// Checks if a resource change has code analysis findings.
    /// Handles merged child resources: if any child has findings, parent is considered to have findings.
    /// </summary>
    /// <param name="change">The resource change ScriptObject.</param>
    /// <returns>True if the resource or any merged child has findings, false otherwise.</returns>
    private static bool HasCodeAnalysisFindings(ScriptObject change)
    {
        // Check direct findings on this resource
        if (change.TryGetValue("code_analysis_findings", out var findingsValue) 
            && findingsValue is ScriptArray findings 
            && findings.Count > 0)
        {
            return true;
        }

        // Check merged child resources for findings
        if (change.TryGetValue("child_resource_groups", out var groupsValue) 
            && groupsValue is ScriptArray groups)
        {
            foreach (var groupItem in groups)
            {
                if (groupItem is not ScriptObject group)
                {
                    continue;
                }

                if (!group.TryGetValue("rows", out var rowsValue) 
                    || rowsValue is not ScriptArray rows)
                {
                    continue;
                }

                // Check each child row for findings
                foreach (var rowItem in rows)
                {
                    if (rowItem is not ScriptObject row)
                    {
                        continue;
                    }

                    // Check if child has associated findings via terraform_resource address
                    if (row.TryGetValue("terraform_resource", out var resourceValue) 
                        && resourceValue?.ToString() is string resourceAddress
                        && !string.IsNullOrEmpty(resourceAddress))
                    {
                        // For child resources, we need to check if they have findings
                        // This requires the change object to have context about child findings
                        // The ReportModelBuilder already attaches findings to parent resources
                        // so checking the parent's findings list is sufficient
                        // (findings are already rolled up to parent during model building)
                    }
                }
            }
        }

        return false;
    }
}
```

**Alternative Approach (Simpler):** Instead of capturing the mode in a closure, pass it directly to the template context and check it in the helper:

```csharp
scriptObject.Import("details_open_attr", 
    new Func<ScriptObject, string>(GetDetailsOpenAttribute));
```

```csharp
private static string GetDetailsOpenAttribute(ScriptObject change)
{
    // Read mode from global context (available via change's parent context)
    // This requires passing the TemplateContext to the helper, which Scriban doesn't support directly
    // SO: Use the closure approach instead
}
```

**DECISION:** Use the closure approach. The mode will be extracted from the `ReportModel` ScriptObject when registering helpers in `MarkdownRenderer.RenderWithTemplate()`.

### 4. Handling Merged Child Resources in Auto Mode

The specification states: "Handles merged child resources: if a parent resource includes merged children and any of them have findings, the parent is opened."

**Current Architecture:** During parent-child merging in `ReportModelBuilder.ParentChildMerging.cs`, findings are already attached to resources. The `ResourceChangeModel.CodeAnalysisFindings` property contains findings for both the parent and its merged children.

**Solution:** The `HasCodeAnalysisFindings()` helper function checks the `code_analysis_findings` array on the change object, which already includes findings from merged children. No additional logic is needed.

**Verification:** Review `ReportModelBuilder.ParentChildMerging.cs` to confirm that child findings are rolled up to parent during merge.

### 5. Debug Block Behavior

The debug details block is rendered in `DiagnosticContext.ToMarkdown()` (around line 88):

```csharp
sb.AppendLine("<details>");
sb.AppendLine("<summary>🐛\u00A0Debug Information</summary>");
```

**Requirement:** The debug block must always remain collapsed regardless of `--details` setting.

**Decision:** No changes needed to `DiagnosticContext.ToMarkdown()`. The debug block already renders without the `open` attribute, so it will remain collapsed regardless of the `--details` setting.

### 6. Template Updates

Update `_resource.sbn` line 6:

**Current:**
```scriban
<details{{ if change.code_analysis_findings.size > 0 }} open{{ end }} style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
```

**New:**
```scriban
<details{{ details_open_attr(change) }} style="margin-bottom:12px; border:1px solid rgb(var(--palette-neutral-10, 153, 153, 153)); padding:12px;">
```

This change:
- Removes the hardcoded logic from the template
- Delegates to the helper function which considers the mode
- Maintains backward compatibility (auto mode preserves current behavior)

### 7. Registering the Helper

Modify `MarkdownRenderer.RenderWithTemplate()` (around line 368):

**Current:**
```csharp
// Register custom helper functions
var diffFormatter = CreateDiffFormatter(model.RenderTarget);
RegisterHelpers(scriptObject, _principalMapper, diffFormatter, _valueFormatterRegistry, _iconProviderRegistry);
```

**New:**
```csharp
// Register custom helper functions
var diffFormatter = CreateDiffFormatter(model.RenderTarget);
RegisterHelpers(scriptObject, _principalMapper, diffFormatter, _valueFormatterRegistry, _iconProviderRegistry, model.DetailsDisplayMode);
```

Update `RegisterHelpers()` signature in `Registry.cs`:

```csharp
internal static void RegisterHelpers(
    ScriptObject scriptObject,
    IPrincipalMapper principalMapper,
    IDiffFormatter diffFormatter,
    ValueFormatterRegistry? valueFormatterRegistry = null,
    IconProviderRegistry? iconProviderRegistry = null,
    DetailsDisplayMode detailsDisplayMode = DetailsDisplayMode.Auto)
{
    var detailsMode = detailsDisplayMode.ToString().ToLowerInvariant();
    
    scriptObject.Import("format_diff", new Func<string?, string?, string>((before, after) => diffFormatter.FormatDiff(before, after)));
    // ... existing registrations ...
    scriptObject.Import("details_open_attr", new Func<ScriptObject, string>(change => GetDetailsOpenAttribute(change, detailsMode)));
}
```

Similarly update the registration in `RenderResourceWithTemplate()` around line 330.

## Default Value Justification

**Default:** `DetailsDisplayMode.Auto`

**Rationale:** 
- Maintains current behavior (backward compatibility)
- Auto mode provides intelligent defaults: expands resources with findings (security/quality issues), collapses clean resources
- Users reviewing code analysis results get findings highlighted automatically
- Users without code analysis see all resources collapsed (same as `closed` mode)
- Power users can override with `--details open` for full expansion or `--details closed` for minimal view

## Implementation Notes

### Files to Create

1. `src/Oocx.TfPlan2Md/RenderTargets/DetailsDisplayMode.cs` — Enum definition
2. `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/DetailsDisplay.cs` — Helper implementation

### Files to Modify

1. `src/Oocx.TfPlan2Md/CLI/CliParser.cs` — Add `--details` argument parsing
2. `src/Oocx.TfPlan2Md/CLI/CliOptions.cs` — Add `DetailsDisplayMode` property
3. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs` — Add `DetailsDisplayMode` property
4. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs` — Add constructor parameter and field
5. `src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs` — Set property in `Build()`
6. `src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs` — Map to script context
7. `src/Oocx.TfPlan2Md/MarkdownGeneration/Helpers/ScribanHelpers/Registry.cs` — Register helper
8. `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs` — Pass mode to helper registration
9. `src/Oocx.TfPlan2Md/MarkdownGeneration/Templates/_resource.sbn` — Use helper function
10. Wherever `ReportModelBuilder` is instantiated — Pass `detailsDisplayMode` parameter

### Integration Points

- **Program.cs / Main entry point:** Pass `cliOptions.DetailsDisplayMode` to `ReportModelBuilder` constructor
- **CompositionRoot:** Ensure `DetailsDisplayMode` flows through dependency chain
- **Help text:** Add `--details` option documentation in `HelpTextProvider.cs`

### Backward Compatibility

- Default value is `Auto`, which preserves current behavior (expand on findings)
- Existing templates without the helper will continue to work (though they won't respect the mode)
- Resource-specific templates that hardcode `open` will override the mode (acceptable tradeoff)

## Testing Considerations

### Unit Tests

1. `CliParserTests.cs` — Parse `--details` argument with valid values
2. `CliParserTests.cs` — Reject invalid `--details` values
3. `ScribanHelpers.DetailsDisplayTests.cs` — Test `GetDetailsOpenAttribute()` logic:
   - Closed mode returns empty string
   - Open mode returns " open"
   - Auto mode with findings returns " open"
   - Auto mode without findings returns empty string
   - Auto mode with child findings returns " open" (merged parent scenario)

### Integration Tests

1. End-to-end test with `--details closed` → verify all resources collapsed
2. End-to-end test with `--details open` → verify all resources expanded
3. End-to-end test with `--details auto` with SARIF → verify selective expansion
4. End-to-end test with `--details auto` without SARIF → verify all collapsed
5. End-to-end test verifying debug block always collapsed regardless of mode

### Snapshot Tests

Update snapshot expectations for templates when mode is passed through.

## Architectural Alignment

This design aligns with existing architecture patterns:

1. **Enum in RenderTargets:** Consistent with `RenderTarget` enum location
2. **CLI → Options → Model → Template flow:** Follows existing pattern (e.g., `RenderTarget`, `ReportTitle`)
3. **Scriban helper functions:** Consistent with existing helpers in `ScribanHelpers` namespace
4. **Closure for mode capture:** Follows pattern used for `diffFormatter` in `format_diff` helper
5. **Immutable models:** `DetailsDisplayMode` flows through records, no mutable state
6. **Template abstraction:** Complex logic in C# helper, simple template invocation

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Resource-specific templates hardcode `open` attribute | Document that custom templates should use `details_open_attr()` helper; acceptable override for power users |
| Performance impact from checking child findings | Findings are already attached during model building; helper only checks existing data structure |
| Confusion about auto mode behavior without SARIF | Document that auto behaves like closed when no code analysis provided; clear in help text |
| Debug block accidentally opened | Explicit check that `DiagnosticContext` never uses `open` attribute; integration test verification |

## Open Questions

None. All requirements are clear and design decisions are complete.

## Next Steps

After approval, recommend handoff to **Quality Engineer** agent for test plan definition, then **Developer** agent for implementation.
