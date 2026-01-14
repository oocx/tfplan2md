# Architecture: Debug Output

## Status

Implemented

## Context

tfplan2md currently provides no diagnostic output when processing Terraform plans. Users encountering issues such as principal mapping failures or unexpected template behavior have no way to troubleshoot or understand what the tool is doing internally.

This feature adds comprehensive debug output capabilities by introducing:
- A `--debug` CLI flag to enable debug mode
- Debug information appended to the markdown report as a new section
- Principal mapping diagnostics (load status, counts by type, failed resolutions with context)
- Template resolution logging (custom vs built-in template usage)

**Reference:** Feature specification at `docs/features/038-debug-output/specification.md`

## Options Considered

### Option 1: Diagnostic Context Pattern (Recommended)

**Description:**

Introduce a `DiagnosticContext` class that collects debug information throughout the processing pipeline. This context is passed through the major components (PrincipalMapper, MarkdownRenderer) and accumulated as processing occurs. The context is then used to generate a debug section at the end of the report.

**Architecture:**

```
┌─────────────────────────────────────────────────────────────────┐
│                         Program.cs                               │
│  1. Parse CLI args (including --debug flag)                      │
│  2. Create DiagnosticContext (if debug enabled)                  │
│  3. Pass context through pipeline:                               │
│     • PrincipalMapper.LoadWithDiagnostics(file, context)        │
│     • MarkdownRenderer.Render(model, context)                    │
│  4. Append debug section to markdown output                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DiagnosticContext                             │
│  • PrincipalMappingInfo (load status, counts, failures)         │
│  • TemplateResolutionInfo (template sources)                     │
│  • Methods: AddPrincipalMapping(), AddTemplateResolution()       │
│  • Method: GenerateMarkdownSection()                             │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌──────────────┐    ┌──────────────────┐    ┌─────────────────┐
│PrincipalMapper│    │MarkdownRenderer  │    │ReportModelBuilder│
│              │    │                  │    │                 │
│Records:      │    │Records:          │    │(no changes)     │
│• Load success│    │• Template source │    │                 │
│• Type counts │    │• Custom vs built-│    │                 │
│• Failed IDs  │    │  in templates    │    │                 │
│  with refs   │    │                  │    │                 │
└──────────────┘    └──────────────────┘    └─────────────────┘
```

**Component Changes:**

1. **New: `DiagnosticContext` class**
   - Properties for principal mapping diagnostics
   - Properties for template resolution diagnostics
   - Method to generate markdown section

2. **CliOptions**: Add `bool Debug { get; init; }`

3. **CliParser**: Parse `--debug` flag

4. **PrincipalMapper**: 
   - Add optional `DiagnosticContext` parameter to constructor
   - Record load success/failure, principal type counts, and failed resolutions with context
   - Track which resource referenced each failed principal ID

5. **MarkdownRenderer**:
   - Add optional `DiagnosticContext` parameter to `Render()` methods
   - Record template resolution decisions (custom vs built-in)

6. **Program.cs**:
   - Create `DiagnosticContext` if `--debug` flag is present
   - Pass context through PrincipalMapper and MarkdownRenderer
   - Append debug section from context to final markdown output

**Pros:**
- Clean separation of concerns (diagnostics collected separately from business logic)
- Non-intrusive: components remain usable without diagnostics
- Flexible: easy to add new diagnostic categories
- Testable: can verify diagnostic collection independently
- Matches existing architecture patterns (immutable models, pure functions)

**Cons:**
- Requires passing context through multiple components
- Slightly more complex than logging to stderr

### Option 2: Structured Logging to Separate Stream

**Description:**

Use a structured logging approach where debug information is written to stderr or a separate file during processing. At the end, this log is formatted as markdown and appended to the report.

**Architecture:**

```
Program.cs creates IDebugLogger (console or null implementation)
  ↓
Pass logger to PrincipalMapper and MarkdownRenderer
  ↓
Components log debug events as they occur
  ↓
Collect log entries and format as markdown section
  ↓
Append to final output
```

**Pros:**
- Familiar logging pattern
- Could support streaming logs in future
- Natural fit for real-time diagnostics

**Cons:**
- More complex infrastructure (logger interface, formatters)
- Requires buffering/collecting logs before appending to report
- Introduces mutable state (log collection)
- Over-engineered for current requirements (single debug level, markdown-only output)
- Deviates from existing immutable architecture patterns

### Option 3: Extended ReportModel with Debug Section

**Description:**

Add debug information directly to the `ReportModel` class. `ReportModelBuilder` collects diagnostics during the build process and includes them in the model. The template renders the debug section based on model properties.

**Architecture:**

```
ReportModel extended with:
  • DebugInfo property (optional)
  • PrincipalMappingDiagnostics
  • TemplateResolutionDiagnostics

ReportModelBuilder collects diagnostics while building model
  ↓
Template renders debug section if DebugInfo is present
```

**Pros:**
- Consistent with existing architecture (all data in model)
- Simpler data flow (single model object)
- Template handles rendering naturally

**Cons:**
- Couples debug information with report data model
- `ReportModelBuilder` doesn't have visibility into template resolution
- Would require passing diagnostic context into builder, then extracting again for renderer
- Mixing concerns (report data vs diagnostics)

## Decision

**Choose Option 1: Diagnostic Context Pattern**

This option provides the cleanest architecture that:
- Maintains separation of concerns (diagnostics separate from core business logic)
- Follows existing patterns (optional parameters, pure functions)
- Is non-intrusive (components work with or without diagnostics)
- Is extensible (easy to add new diagnostic categories)
- Maintains immutability (context is mutable but isolated to diagnostic concerns)

The slight complexity of passing context through components is justified by the architectural benefits and future extensibility.

## Rationale

### Why Diagnostic Context over Structured Logging?

While structured logging is a proven pattern, it introduces unnecessary complexity for this use case:
- The specification requires markdown output only (not streaming logs)
- The tool has a single debug level (not multiple verbosity levels)
- The existing architecture favors immutable data models over stateful logging infrastructure
- The diagnostic context pattern is simpler and sufficient for current requirements

### Why Diagnostic Context over Extended ReportModel?

The diagnostic context pattern better separates concerns:
- Debug information is orthogonal to report data
- Template resolution happens in `MarkdownRenderer`, not `ReportModelBuilder`
- Keeping diagnostics separate makes the codebase easier to understand
- The `ReportModel` remains focused on representing report content

### Principal Mapping Diagnostic Context

The `PrincipalMapper` already has error handling that writes to `Console.Error` (line 74 of `PrincipalMapper.cs`). With the diagnostic context pattern:
- The mapper records load success/failure in the context
- Failed ID resolutions are tracked with the resource that referenced them
- Type counts are accumulated during resolution calls
- This information is later formatted as markdown

### Template Resolution Tracking

The `MarkdownRenderer` currently uses `TemplateResolver` to determine which template to use for each resource. With diagnostics:
- Each template resolution decision is recorded in the context
- The context distinguishes between custom templates, built-in resource-specific templates, and the default fallback
- This gives users visibility into the template system's behavior

## Consequences

### Positive

- Users can troubleshoot principal mapping failures with detailed context
- Users can verify which templates are being used
- Non-intrusive: no impact on existing functionality when debug is disabled
- Extensible: new diagnostic categories can be added easily
- Testable: diagnostic collection can be tested independently
- Maintains architectural consistency with existing patterns

### Negative

- Adds a new class (`DiagnosticContext`) to the codebase
- Requires passing context parameter through multiple method calls
- Developers must remember to update diagnostic context when adding relevant features

### Risks and Mitigation

| Risk | Mitigation |
|------|------------|
| Context not passed through correctly | Comprehensive tests for diagnostic collection |
| Performance impact of collecting diagnostics | Context is only created when `--debug` flag is present; operations are lightweight |
| Future maintenance burden | Good documentation and clear separation of concerns |

## Implementation Notes

### High-Level Component Changes

1. **New Component: `Diagnostics/DiagnosticContext.cs`**
   - Class with properties for each diagnostic category
   - Methods to add diagnostic information
   - Method to generate markdown section

2. **CLI: `CliOptions` and `CliParser`**
   - Add `bool Debug { get; init; }` to `CliOptions`
   - Parse `--debug` flag in `CliParser`
   - Update help text to document the flag

3. **Azure: `PrincipalMapper.cs`**
   - Add optional `DiagnosticContext?` constructor parameter
   - Record load status, type counts, and failed resolutions
   - Track resource address for each failed principal ID lookup

4. **MarkdownGeneration: `MarkdownRenderer.cs`**
   - Add optional `DiagnosticContext?` parameter to `Render()` methods
   - Record template resolution decisions (built-in vs custom, resource-specific vs default)

5. **Program.cs**
   - Create `DiagnosticContext` when `options.Debug` is true
   - Pass context to `PrincipalMapper` constructor
   - Pass context to `MarkdownRenderer.Render()`
   - Append `context.GenerateMarkdownSection()` to markdown output

### DiagnosticContext Structure

```csharp
public class DiagnosticContext
{
    // Principal mapping diagnostics
    public bool PrincipalMappingFileProvided { get; set; }
    public bool PrincipalMappingLoadedSuccessfully { get; set; }
    public string? PrincipalMappingFilePath { get; set; }
    public Dictionary<string, int> PrincipalTypeCount { get; } = new();
    public List<FailedPrincipalResolution> FailedResolutions { get; } = new();
    
    // Template resolution diagnostics
    public List<TemplateResolution> TemplateResolutions { get; } = new();
    
    // Methods
    public string GenerateMarkdownSection();
}

public record FailedPrincipalResolution(string PrincipalId, string ResourceAddress);
public record TemplateResolution(string ResourceType, string TemplateSource);
```

### Debug Section Format

The markdown section generated by `DiagnosticContext.GenerateMarkdownSection()` should follow this structure:

```markdown
## Debug Information

### Principal Mapping

Principal Mapping: Loaded successfully from 'principals.json'
- Found 45 users, 12 groups, 8 service principals

Failed to resolve 3 principal IDs:
- `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_role_assignment.example`)
- `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`)

### Template Resolution

- `azurerm_firewall_network_rule_collection`: Built-in resource-specific template
- `azurerm_virtual_network`: Default template
- `azurerm_custom_resource`: Custom template from '/templates/azurerm/custom_resource.sbn'
```

### Integration Points

The diagnostic context flows through the system as follows:

```
Program.cs
  │
  ├─▶ Create DiagnosticContext (if --debug)
  │
  ├─▶ new PrincipalMapper(mappingFile, context)
  │     └─▶ Records: load status, type counts, failed IDs
  │
  ├─▶ new ReportModelBuilder(...)  [no changes]
  │
  ├─▶ renderer.Render(model, templatePath, context)
  │     └─▶ Records: template resolutions
  │
  └─▶ markdown += context.GenerateMarkdownSection()
```

### Testing Strategy

1. **Unit Tests for DiagnosticContext**
   - Test markdown generation with various scenarios
   - Test empty diagnostics (nothing to report)
   - Test principal mapping diagnostics
   - Test template resolution diagnostics

2. **Integration Tests for PrincipalMapper**
   - Test that diagnostics are collected when context is provided
   - Test that mapper works normally when context is null

3. **Integration Tests for MarkdownRenderer**
   - Test that template resolutions are recorded
   - Test that renderer works normally when context is null

4. **End-to-End Tests**
   - Test complete flow with `--debug` flag
   - Verify debug section appears in output
   - Verify debug section is absent without `--debug` flag

### Backward Compatibility

- No breaking changes: `--debug` is an opt-in flag
- All components work normally when `DiagnosticContext` is null
- No changes to `ReportModel` structure or templates

## Components Affected

This feature requires changes to the following components:

### New Files

- `src/Oocx.TfPlan2Md/Diagnostics/DiagnosticContext.cs` - Core diagnostic context class
- `src/Oocx.TfPlan2Md/Diagnostics/FailedPrincipalResolution.cs` - Record for failed mappings
- `src/Oocx.TfPlan2Md/Diagnostics/TemplateResolution.cs` - Record for template decisions

### Modified Files

- `src/Oocx.TfPlan2Md/CLI/CliOptions.cs` - Add `Debug` property
- `src/Oocx.TfPlan2Md/CLI/CliParser.cs` - Parse `--debug` flag
- `src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs` - Document `--debug` flag
- `src/Oocx.TfPlan2Md/Azure/PrincipalMapper.cs` - Add diagnostic context parameter and collection
- `src/Oocx.TfPlan2Md/MarkdownGeneration/MarkdownRenderer.cs` - Add diagnostic context parameter and collection
- `src/Oocx.TfPlan2Md/Program.cs` - Create and pass diagnostic context, append debug section

### No Changes Required

- `ReportModel.cs` - No changes to report model structure
- `ReportModelBuilder.cs` - No changes to model building
- Templates (`*.sbn`) - No template changes needed
- `TerraformPlanParser.cs` - No changes to parsing

## Alternative Approaches Rejected

### Logging Framework Integration

**Approach:** Integrate a logging framework like Serilog or Microsoft.Extensions.Logging.

**Rejected because:**
- Over-engineered for a simple CLI tool with a single debug level
- Adds significant complexity and dependencies
- The diagnostic context pattern is simpler and sufficient
- Deviates from the project's philosophy of minimal dependencies

### Debug Output to Stderr

**Approach:** Write debug information to stderr instead of appending to markdown report.

**Rejected because:**
- Specification explicitly requires debug output in the markdown report
- Stderr output would require separate handling in CI/CD pipelines
- Users expect a single output file containing all information
- Markdown format is consistent and more readable

### Global Static Context

**Approach:** Use a static `Diagnostics` class that components can write to anywhere.

**Rejected because:**
- Introduces global mutable state
- Makes testing difficult (state persists between tests)
- Violates the principle of explicit dependencies
- Harder to understand data flow through the system

## Open Questions

None - all design decisions have been finalized based on the specification and existing architecture patterns.
