# Architecture: Azure DevOps Build Definition Nested Block Tables

## Status

Proposed

## Context

Feature specification: [specification.md](specification.md)

The `azuredevops_build_definition` Terraform resource currently displays nested blocks (especially `variable` blocks containing secret values) as opaque "sensitive block" messages. This feature implements specialized table rendering for build definition nested blocks, following the exact pattern established by `azuredevops_variable_group` (Feature 027/039).

### Existing Pattern: Variable Group Template

The `azuredevops_variable_group` resource provides the complete reference implementation:

| Component | Location | Purpose |
|-----------|----------|---------|
| **ViewModel** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupViewModel.cs` | Defines typed view models for variables and Key Vault blocks |
| **Factory** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupViewModelFactory.cs` | Creates view models from ResourceChange, handles create/update/delete |
| **Extractors** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupExtractors.cs` | Extracts data from Terraform JSON (JsonElement parsing) |
| **Formatters** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupFormatters.cs` | Formats values for display, handles secret masking |
| **Change Builders** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/VariableGroupChangeBuilders.cs` | Builds semantic diffs (added/modified/removed/unchanged) |
| **Mapper** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Mappers/VariableGroupMapper.cs` | Implements `IResourceModelMapper`, enriches ScriptObject |
| **Template** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/variable_group.sbn` | Scriban template with conditional table rendering |
| **Registration** | `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs` | Registers factory and mapper |

**Key Pattern Elements:**

1. **Separation of Concerns**: Factory creates view models, Formatters handle string formatting, Change Builders compute semantic diffs
2. **Semantic Matching**: Variables matched by `name` attribute (not array index) to correctly identify added/modified/removed
3. **Secret Masking**: `is_secret: true` variables show metadata but display `(sensitive / hidden)` for values
4. **Large Value Handling**: Values >100 chars or multi-line are flagged with `IsLargeValue` for separate rendering
5. **Conditional Rendering**: Template only shows tables when data exists (e.g., `if change.variable_group.after_variables.size > 0`)
6. **Create/Update/Delete Paths**: Different table structures based on action type

### Build Definition Data Structure

From `examples/azuredevops/terraform_plan2.json`:

```json
{
  "type": "azuredevops_build_definition",
  "change": {
    "before/after": {
      "name": "example-pipeline",
      "path": "\\Pipelines",
      "project_id": "0f0b93a6-f450-49b2-ad52-fe3303c2f9aa",
      "agent_pool_name": "Azure Pipelines",
      "queue_status": "enabled",
      "variable": [
        {
          "name": "BUILD_CONFIGURATION",
          "value": "Release",
          "is_secret": false,
          "allow_override": true,
          "secret_value": ""
        }
      ],
      "ci_trigger": [
        {
          "use_yaml": true,
          "override": []
        }
      ],
      "pull_request_trigger": [],
      "schedules": [],
      "repository": [
        {
          "repo_type": "TfsGit",
          "repo_id": "80128bc2-17ff-45f8-ad59-d7609a605c75",
          "branch_name": "refs/heads/master",
          "yml_path": "azure-pipelines.yml",
          "report_build_status": true,
          "github_enterprise_url": "",
          "service_connection_id": ""
        }
      ],
      "jobs": []
    }
  }
}
```

**Nested Blocks to Display:**

1. **variables** - Array of variable objects (name, value, is_secret, allow_override, secret_value)
2. **ci_trigger** - Array (typically single element) with use_yaml and override (branch filters)
3. **pull_request_trigger** - Array with use_yaml, override, forks settings
4. **schedules** - Array of schedule configurations
5. **repository** - Array (typically single element) with repo connection details
6. **jobs** - Array of job definitions (typically empty for YAML pipelines)

## Analysis

### Similarities with Variable Group

| Aspect | Variable Group | Build Definition |
|--------|---------------|------------------|
| **Secret Handling** | `secret_variable` blocks mask values | `variable` blocks with `is_secret: true` mask values |
| **Semantic Matching** | Match by `name` attribute | Match variables by `name` attribute |
| **Large Values** | Support for >100 char values | Same requirement for variable values |
| **Conditional Rendering** | Only show tables when data exists | Same requirement for all nested blocks |
| **Create/Update/Delete** | Different table layouts per action | Same requirement |

### Differences from Variable Group

| Aspect | Variable Group | Build Definition |
|--------|---------------|------------------|
| **Multiple Block Types** | Only variables and Key Vault blocks | Variables, triggers, schedules, repository, jobs |
| **Secret Detection** | Separate `secret_variable` array | Single `variable` array with `is_secret` boolean |
| **Trigger Blocks** | N/A | CI trigger, PR trigger, schedules |
| **Repository Block** | N/A | Repository connection details |

### Complexity Assessment

**Low Complexity** - This is a straightforward application of the existing pattern:
- Same ViewModel → Factory → Extractor → Formatter → Change Builder → Mapper → Template flow
- Existing secret masking logic applies directly
- Semantic diffing logic identical (match by name)
- Template conditional rendering pattern reused

**New Elements:**
- Additional block types (triggers, repository, schedules) - but these are simpler than variables (no semantic diffing needed)
- Single `variable` array with `is_secret` flag instead of separate arrays - minor difference in extraction logic

## Design

### Architecture Decision

**No new ADR required** - this feature follows the established `azuredevops_variable_group` pattern exactly. The pattern is well-proven and documented.

### Component Structure

Following the variable group pattern, create these components:

```
src/Oocx.TfPlan2Md/Providers/AzureDevOps/
├── Models/
│   ├── BuildDefinitionViewModel.cs          # View models for all nested blocks
│   ├── BuildDefinitionViewModelFactory.cs   # Factory: create view models from ResourceChange
│   ├── BuildDefinitionExtractors.cs         # Extract data from Terraform JSON
│   ├── BuildDefinitionFormatters.cs         # Format values for display (secret masking, etc.)
│   ├── BuildDefinitionChangeBuilders.cs     # Build semantic diffs for variables
│   └── Factories.cs                         # Add BuildDefinitionFactory class
├── Mappers/
│   └── BuildDefinitionMapper.cs             # IResourceModelMapper implementation
├── Templates/azuredevops/
│   └── build_definition.sbn                 # Scriban template
└── AzureDevOpsModule.cs                     # Update registration
```

### View Models

```csharp
// BuildDefinitionViewModel.cs

/// <summary>
/// Provides precomputed data for azuredevops_build_definition template.
/// Related feature: docs/features/094-build-definition-tables/specification.md.
/// </summary>
public sealed class BuildDefinitionViewModel
{
    // Metadata
    public string? Name { get; init; }
    public string? Path { get; init; }
    public string? AgentPoolName { get; init; }
    public string? QueueStatus { get; init; }
    
    // Variables (semantic diffing like variable group)
    public IReadOnlyList<BuildDefinitionVariableChangeRowViewModel> VariableChanges { get; init; } = Array.Empty<...>();
    public IReadOnlyList<BuildDefinitionVariableRowViewModel> AfterVariables { get; init; } = Array.Empty<...>();
    public IReadOnlyList<BuildDefinitionVariableRowViewModel> BeforeVariables { get; init; } = Array.Empty<...>();
    
    // Other nested blocks (before/after display, no semantic diffing)
    public IReadOnlyList<CiTriggerRowViewModel> AfterCiTriggers { get; init; } = Array.Empty<...>();
    public IReadOnlyList<CiTriggerRowViewModel> BeforeCiTriggers { get; init; } = Array.Empty<...>();
    
    public IReadOnlyList<PullRequestTriggerRowViewModel> AfterPullRequestTriggers { get; init; } = Array.Empty<...>();
    public IReadOnlyList<PullRequestTriggerRowViewModel> BeforePullRequestTriggers { get; init; } = Array.Empty<...>();
    
    public IReadOnlyList<ScheduleRowViewModel> AfterSchedules { get; init; } = Array.Empty<...>();
    public IReadOnlyList<ScheduleRowViewModel> BeforeSchedules { get; init; } = Array.Empty<...>();
    
    public IReadOnlyList<RepositoryRowViewModel> AfterRepositories { get; init; } = Array.Empty<...>();
    public IReadOnlyList<RepositoryRowViewModel> BeforeRepositories { get; init; } = Array.Empty<...>();
}

/// <summary>
/// Represents a variable row with change indicator for update tables.
/// </summary>
public sealed class BuildDefinitionVariableChangeRowViewModel
{
    public required string Change { get; init; }        // "add", "update", "remove", "unchanged"
    public required string ChangeIcon { get; init; }    // ➕, 🔄, ❌, ⏺️
    public required string Name { get; init; }
    public required string Value { get; init; }         // "(sensitive / hidden)" if is_secret: true
    public required string IsSecret { get; init; }      // Formatted boolean or diff
    public required string AllowOverride { get; init; } // Formatted boolean or diff
    public bool IsLargeValue { get; init; }
}

/// <summary>
/// Represents a variable row for create/delete tables.
/// </summary>
public sealed class BuildDefinitionVariableRowViewModel
{
    public required string Name { get; init; }
    public required string Value { get; init; }         // "(sensitive / hidden)" if is_secret
    public required string IsSecret { get; init; }
    public required string AllowOverride { get; init; }
    public bool IsLargeValue { get; init; }
}

/// <summary>
/// Represents a CI trigger block.
/// </summary>
public sealed class CiTriggerRowViewModel
{
    public required string UseYaml { get; init; }
    public required string Override { get; init; }      // Comma-separated branch filters or "-"
}

// Similar row view models for PullRequestTriggerRowViewModel, ScheduleRowViewModel, RepositoryRowViewModel
```

### Factory Logic

```csharp
// BuildDefinitionViewModelFactory.cs

internal static class BuildDefinitionViewModelFactory
{
    public static BuildDefinitionViewModel Build(
        ResourceChange change, 
        string providerName, 
        LargeValueFormat largeValueFormat)
    {
        // Extract metadata
        var name = ExtractName(change.Change.After) ?? ExtractName(change.Change.Before);
        var path = ExtractPath(change.Change.After) ?? ExtractPath(change.Change.Before);
        
        // Extract variables
        var beforeVariables = ExtractVariables(change.Change.Before);
        var afterVariables = ExtractVariables(change.Change.After);
        
        // Extract other blocks
        var beforeCiTriggers = ExtractCiTriggers(change.Change.Before);
        var afterCiTriggers = ExtractCiTriggers(change.Change.After);
        // ... similar for other blocks
        
        // Determine action
        var isCreate = actions.Contains("create") && !actions.Contains("delete");
        var isDelete = actions.Contains("delete") && !actions.Contains("create");
        
        // Build view model based on action
        if (isCreate) {
            return new BuildDefinitionViewModel {
                Name = name,
                Path = path,
                AfterVariables = FormatVariableRows(afterVariables),
                AfterCiTriggers = FormatCiTriggerRows(afterCiTriggers),
                // ... other after blocks
            };
        }
        // ... similar for delete and update
    }
}
```

### Extractors

```csharp
// BuildDefinitionExtractors.cs

internal static class BuildDefinitionExtractors
{
    // Extract metadata
    public static string? ExtractName(object? state) { /* JsonElement parsing */ }
    public static string? ExtractPath(object? state) { /* JsonElement parsing */ }
    
    // Extract variables (similar to VariableGroupExtractors)
    public static IReadOnlyList<BuildDefinitionVariableValues> ExtractVariables(object? state)
    {
        // Parse variable array
        // Each variable has: name, value, is_secret, allow_override, secret_value
    }
    
    // Extract triggers, repository, schedules
    public static IReadOnlyList<CiTriggerValues> ExtractCiTriggers(object? state) { /* ... */ }
    public static IReadOnlyList<PullRequestTriggerValues> ExtractPullRequestTriggers(object? state) { /* ... */ }
    public static IReadOnlyList<ScheduleValues> ExtractSchedules(object? state) { /* ... */ }
    public static IReadOnlyList<RepositoryValues> ExtractRepositories(object? state) { /* ... */ }
}
```

### Formatters

```csharp
// BuildDefinitionFormatters.cs

internal static class BuildDefinitionFormatters
{
    // Format variable value - SECRET MASKING LOGIC
    private static string FormatVariableValue(BuildDefinitionVariableValues variable)
    {
        if (variable.IsSecret)
        {
            return "(sensitive / hidden)";  // NEVER show actual value
        }
        
        // For non-secret variables, show the value
        return FormatOptionalString(variable.Value);
    }
    
    // Format boolean values
    private static string FormatBoolean(bool? value)
    {
        return value.HasValue ? $"`{(value.Value ? "true" : "false")}`" : "`false`";
    }
    
    // Format optional strings
    private static string FormatOptionalString(string? value)
    {
        return string.IsNullOrEmpty(value) ? "`-`" : $"`{EscapeMarkdown(value)}`";
    }
    
    // Format branch filter arrays
    private static string FormatBranchFilters(IReadOnlyList<string>? filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return "`-`";
        }
        return string.Join(", ", filters.Select(f => $"`{EscapeMarkdown(f)}`"));
    }
    
    // Create formatted rows for variables (semantic diffing)
    public static BuildDefinitionVariableRowViewModel CreateVariableRow(BuildDefinitionVariableValues variable) { /* ... */ }
    public static BuildDefinitionVariableChangeRowViewModel CreateAddedVariableRow(BuildDefinitionVariableValues variable) { /* ... */ }
    public static BuildDefinitionVariableChangeRowViewModel CreateRemovedVariableRow(BuildDefinitionVariableValues variable) { /* ... */ }
    public static BuildDefinitionVariableChangeRowViewModel CreateModifiedVariableRow(
        BuildDefinitionVariableValues before, 
        BuildDefinitionVariableValues after, 
        LargeValueFormat format) { /* ... */ }
    
    // Create formatted rows for other blocks (no semantic diffing)
    public static CiTriggerRowViewModel CreateCiTriggerRow(CiTriggerValues trigger) { /* ... */ }
    public static RepositoryRowViewModel CreateRepositoryRow(RepositoryValues repo) { /* ... */ }
    // ... similar for other blocks
}
```

### Change Builders

```csharp
// BuildDefinitionChangeBuilders.cs

internal static class BuildDefinitionChangeBuilders
{
    // Semantic diffing for variables (match by name)
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildAdded(
        IReadOnlyList<BuildDefinitionVariableValues> afterVariables,
        IReadOnlyList<BuildDefinitionVariableValues> beforeVariables)
    {
        var beforeNames = new HashSet<string>(beforeVariables.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        return afterVariables
            .Where(variable => !beforeNames.Contains(variable.Name))
            .OrderBy(variable => variable.Name, StringComparer.Ordinal)
            .Select(BuildDefinitionFormatters.CreateAddedVariableRow)
            .ToList();
    }
    
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildRemoved(...) { /* Similar logic */ }
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildModified(...) { /* Similar logic */ }
    public static List<BuildDefinitionVariableChangeRowViewModel> BuildUnchanged(...) { /* Similar logic */ }
}
```

### Mapper

```csharp
// BuildDefinitionMapper.cs

internal sealed class BuildDefinitionMapper : IResourceModelMapper
{
    private readonly BuildDefinitionFactory _factory;
    
    public BuildDefinitionMapper(BuildDefinitionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }
    
    public bool CanMap(ResourceChangeModel resource)
    {
        return resource.Type == "azuredevops_build_definition";
    }
    
    public void EnrichScriptObject(ResourceChangeModel resource, ScriptObject scriptObject)
    {
        if (resource.ResourceChange == null)
        {
            return;
        }
        
        var viewModel = _factory.CreateViewModel(resource.ResourceChange);
        scriptObject["build_definition"] = MapBuildDefinition(viewModel);
    }
    
    private static ScriptObject MapBuildDefinition(BuildDefinitionViewModel bd)
    {
        var obj = new ScriptObject
        {
            ["name"] = bd.Name,
            ["path"] = bd.Path,
            ["agent_pool_name"] = bd.AgentPoolName
        };
        
        // Map variable changes (for update)
        var variableChanges = new ScriptArray();
        foreach (var variable in bd.VariableChanges)
        {
            variableChanges.Add(MapVariableChangeRow(variable));
        }
        obj["variable_changes"] = variableChanges;
        
        // Map after/before variables (for create/delete)
        var afterVariables = new ScriptArray();
        foreach (var variable in bd.AfterVariables)
        {
            afterVariables.Add(MapVariableRow(variable));
        }
        obj["after_variables"] = afterVariables;
        
        // ... similar for before_variables and all other block types
        
        return obj;
    }
}
```

### Template Structure

```scriban
{{~## Template for azuredevops_build_definition
     Uses BuildDefinitionViewModel for pre-computed formatting.
~}}
<details{{ details_open_attr(change) }}>
<summary>{{ change.summary_html }}</summary>
<br>

{{ if change.build_definition && change.build_definition.name ~}}
**Pipeline Name:** {{ format_code_summary(change.build_definition.name) }}

{{ end ~}}
{{ if change.build_definition && change.build_definition.path ~}}
**Path:** {{ format_code_summary(change.build_definition.path) }}

{{ end ~}}

{{~ ## Variables Section ~}}
{{ if change.action == "create" && change.build_definition && change.build_definition.after_variables.size > 0 ~}}
#### Variables

| Name | Value | Is Secret | Allow Override |
| ---- | ----- | --------- | -------------- |
{{ for var in change.build_definition.after_variables ~}}
| {{ var.name }} | {{ var.value }} | {{ var.is_secret }} | {{ var.allow_override }} |
{{ end ~}}

{{ else if change.action == "delete" && change.build_definition && change.build_definition.before_variables.size > 0 ~}}
#### Variables (being deleted)

| Name | Value | Is Secret | Allow Override |
| ---- | ----- | --------- | -------------- |
{{ for var in change.build_definition.before_variables ~}}
| {{ var.name }} | {{ var.value }} | {{ var.is_secret }} | {{ var.allow_override }} |
{{ end ~}}

{{ else if change.build_definition && change.build_definition.variable_changes.size > 0 ~}}
#### Variables

| Change | Name | Value | Is Secret | Allow Override |
| ------ | ---- | ----- | --------- | -------------- |
{{ for var in change.build_definition.variable_changes ~}}
| {{ var.change_icon }} | {{ var.name }} | {{ var.value }} | {{ var.is_secret }} | {{ var.allow_override }} |
{{ end ~}}

{{ end ~}}

{{~ ## CI Trigger Section ~}}
{{ if change.build_definition && change.build_definition.after_ci_triggers.size > 0 ~}}
#### CI Trigger

| Use YAML | Override (Branch Filters) |
| -------- | ------------------------- |
{{ for trigger in change.build_definition.after_ci_triggers ~}}
| {{ trigger.use_yaml }} | {{ trigger.override }} |
{{ end ~}}

{{ end ~}}

{{~ ## Repository Section ~}}
{{ if change.build_definition && change.build_definition.after_repositories.size > 0 ~}}
#### Repository

| Type | Repo ID | Branch | YAML Path | Report Build Status |
| ---- | ------- | ------ | --------- | ------------------- |
{{ for repo in change.build_definition.after_repositories ~}}
| {{ repo.repo_type }} | {{ repo.repo_id }} | {{ repo.branch_name }} | {{ repo.yml_path }} | {{ repo.report_build_status }} |
{{ end ~}}

{{ end ~}}

{{~ ## Similar sections for pull_request_trigger, schedules, jobs ~}}

</details>
```

### Registration

```csharp
// AzureDevOpsModule.cs

public void RegisterFactories(IResourceViewModelFactoryRegistry registry)
{
    registry.RegisterFactory("azuredevops_variable_group", new VariableGroupFactory(_largeValueFormat));
    registry.RegisterFactory("azuredevops_build_definition", new BuildDefinitionFactory(_largeValueFormat));  // ADD THIS
}

public void RegisterResourceModelMappers(ResourceModelMapperRegistry registry)
{
    var variableGroupFactory = new VariableGroupFactory(_largeValueFormat);
    registry.Register(new Mappers.VariableGroupMapper(variableGroupFactory));
    
    var buildDefinitionFactory = new BuildDefinitionFactory(_largeValueFormat);  // ADD THIS
    registry.Register(new Mappers.BuildDefinitionMapper(buildDefinitionFactory));  // ADD THIS
}
```

### Secret Masking Logic

**Critical Security Requirement**: Variables with `is_secret: true` MUST display `(sensitive / hidden)` instead of actual values.

```csharp
// In BuildDefinitionFormatters.cs

private static string FormatVariableValue(BuildDefinitionVariableValues variable)
{
    // SECURITY: Never display secret values
    if (variable.IsSecret)
    {
        return "(sensitive / hidden)";
    }
    
    // For non-secret variables, show the value
    var value = variable.Value;
    if (string.IsNullOrEmpty(value))
    {
        return "`-`";
    }
    
    return $"`{EscapeMarkdown(value)}`";
}
```

For **modified** variables where `is_secret` changes from false to true:
- Show before/after diff for `is_secret` attribute: `- \`false\`<br>+ \`true\``
- Always show `(sensitive / hidden)` in Value column (never show the before value even if it was non-secret)

### Large Value Handling

For variable values that exceed 100 characters or contain line breaks:
- Set `IsLargeValue = true` in the view model
- Display metadata in the main table (Name, Is Secret, Allow Override)
- Omit Value column from the table
- Include value in the large values section (existing mechanism)

**Important**: Only regular variables (`is_secret: false`) can have large values displayed. Secret variables always show `(sensitive / hidden)` regardless of length.

## Implementation Notes

### Component Creation Order

1. **BuildDefinitionViewModel.cs** - Define all view model classes
2. **BuildDefinitionExtractors.cs** - Extract data from Terraform JSON
3. **BuildDefinitionFormatters.cs** - Format values, handle secret masking
4. **BuildDefinitionChangeBuilders.cs** - Semantic diffing for variables
5. **BuildDefinitionViewModelFactory.cs** - Create view models from ResourceChange
6. **Factories.cs** - Add BuildDefinitionFactory adapter class
7. **BuildDefinitionMapper.cs** - Implement IResourceModelMapper
8. **build_definition.sbn** - Scriban template
9. **AzureDevOpsModule.cs** - Register factory and mapper

### Testing Strategy

Follow the variable group testing pattern:

1. **Create operation** - New build definition with variables (regular and secret)
2. **Update operation** - Added/modified/removed variables, trigger changes
3. **Delete operation** - Deleting build definition with variables
4. **Secret masking** - Verify secret variables show metadata but `(sensitive / hidden)` for values
5. **Large values** - Verify large variable values handled correctly (regular variables only)
6. **Empty blocks** - Verify conditional rendering (no empty tables)
7. **Conditional rendering** - Only show sections when data exists

### Files to Create/Modify

**New Files** (8 files):
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionViewModel.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionViewModelFactory.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionExtractors.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionFormatters.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/BuildDefinitionChangeBuilders.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Mappers/BuildDefinitionMapper.cs`
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Templates/azuredevops/build_definition.sbn`
- Test files (TBD by Quality Engineer)

**Modified Files** (2 files):
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/Models/Factories.cs` - Add BuildDefinitionFactory
- `src/Oocx.TfPlan2Md/Providers/AzureDevOps/AzureDevOpsModule.cs` - Register factory and mapper

### Alignment with Project Architecture

This design fully adheres to the architecture documented in `docs/architecture.md`:

- **Provider Isolation**: All code in `Providers/AzureDevOps/` folder (no leakage into core MarkdownGeneration)
- **AOT Compatible**: Explicit registration, no reflection
- **Scriban Templates**: Resource-specific template following existing patterns
- **View Model Pattern**: Precomputed formatting in view models, templates remain simple
- **Security**: Secret masking enforced at formatter level
- **Maintainability**: Follows established patterns (variable group), clear separation of concerns

## Consequences

### Positive

- Users can review build definition changes transparently in PR comments
- Secret variable values remain protected (show metadata only)
- Consistent pattern with variable group (maintainability)
- No new architectural patterns needed (low risk)
- Clear table rendering improves DevOps workflow visibility

### Negative

- Adds ~8 new files to the codebase (manageable, follows proven pattern)
- Need to maintain two similar but distinct nested block renderers (variable_group and build_definition)
- Future build definition schema changes require updates to extractors/formatters

### Risks

| Risk | Mitigation |
|------|------------|
| Secret values leaked in output | Security tests verify `(sensitive / hidden)` for all is_secret: true variables |
| Template rendering fails | Integration tests verify template rendering for create/update/delete |
| Large values break table layout | Existing large value mechanism handles this; unit tests verify IsLargeValue flagging |
| Missing nested blocks | Conditional rendering ensures no empty tables; comprehensive test coverage |

## References

- [Feature Specification](specification.md)
- [Variable Group Pattern](../../039-azdo-variable-group-template/specification.md)
- [Provider Code Separation](../../032-provider-code-separation/architecture.md)
- [docs/architecture.md](../../../architecture.md)
- [ADR-001: Scriban Templating](../../../adr-001-scriban-templating.md)
