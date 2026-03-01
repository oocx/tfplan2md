# tfplan2md Architecture — Without Scriban (Pure C# Rendering)

This document describes the target architecture for tfplan2md after removing the Scriban template engine in favor of pure C# rendering. It serves as both a design reference for the migration and a replacement for the Scriban-related sections in the current [architecture.md](../../../docs/architecture.md).

**Prerequisite:** [ADR-010: Evaluate Removing Scriban in Favor of Pure C# Rendering](../../../docs/adr-010-scriban-removal-evaluation.md)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Motivation and Constraints](#2-motivation-and-constraints)
3. [System Overview](#3-system-overview)
4. [Component Architecture](#4-component-architecture)
5. [Rendering Pipeline](#5-rendering-pipeline)
6. [Provider Architecture](#6-provider-architecture)
7. [Service Registry Architecture](#7-service-registry-architecture)
8. [Data Model](#8-data-model)
9. [Data Flow](#9-data-flow)
10. [Composition and Dependency Injection](#10-composition-and-dependency-injection)
11. [Error Handling](#11-error-handling)
12. [Cross-Cutting Concerns](#12-cross-cutting-concerns)
13. [Migration Impact Analysis](#13-migration-impact-analysis)
14. [Quality Attributes](#14-quality-attributes)
15. [Glossary](#15-glossary)

---

## 1. Executive Summary

tfplan2md is a CLI tool that converts Terraform plan JSON into human-readable Markdown reports for GitHub and Azure DevOps pull request comments. The current architecture uses Scriban templates for rendering, which requires ~10,000 lines of C# glue code to support ~1,600 lines of template syntax. Since user-customizable templates are no longer a requirement, this document describes a streamlined architecture where **all rendering is done in pure C#**, eliminating the sole third-party dependency and enabling full compile-time safety.

### Key Architectural Changes

| Aspect | Current (Scriban) | Target (Pure C#) |
|--------|-------------------|-------------------|
| **Rendering engine** | Scriban template interpreter | C# `MarkdownWriter` + `IResourceRenderer` |
| **Data model** | Dual: C# models + `ScriptObject` trees | Single: C# models only |
| **Error detection** | Runtime (template variable typos are silent) | Compile-time (property access verified by compiler) |
| **Third-party deps** | Scriban 6.5.2 (sole `PackageReference`) | Zero third-party dependencies |
| **NativeAOT support** | Requires `TrimmerRootDescriptor.xml` to preserve entire Scriban assembly | No trimmer workarounds needed |
| **Provider extension** | `RegisterHelpers(ScriptObject)` + `.sbn` templates | `IResourceRenderer` implementations |
| **Template resolution** | File-based: `{provider}/{resource}.sbn` → `_resource.sbn` | Type-based: `IResourceRenderer` registry → `DefaultResourceRenderer` |
| **Sensitivity/unknown handling** | Template-level access to `before_sensitive`, `after_sensitive`, `after_unknown` via `ScriptObject` | C# model properties accessed directly |

---

## 2. Motivation and Constraints

### 2.1 Why Remove Scriban

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
pie title Scriban-Related Code Distribution
    "Scriban Templates (.sbn)" : 1649
    "ScribanHelpers — core (20 C# files)" : 3480
    "ScribanHelpers — AzApi (15 C# files)" : 6320
    "AotScriptObjectMapper" : 692
    "Provider Mappers (ScriptObject enrichment)" : 957
    "Template Infrastructure" : 850
```

The pie chart above shows the fundamental imbalance: ~1,650 lines of template syntax across 28 `.sbn` files require ~12,300 lines of C# infrastructure. With user-customizable templates no longer required, this overhead provides no user-facing value.

**Updated metrics (post-PR 569):**

| Category | Files | Lines |
|----------|------:|------:|
| Scriban templates (`.sbn`) | 28 | ~1,650 |
| ScribanHelpers — core (`MarkdownGeneration/Helpers/ScribanHelpers/`) | 20 | ~3,480 |
| ScribanHelpers — AzApi (`Providers/AzApi/Helpers/ScribanHelpers/`) | 15 | ~6,320 |
| AotScriptObjectMapper | 1 | ~692 |
| TemplateLoader + TemplateResolver | 2 | ~264 |
| MarkdownRenderer (Scriban orchestration) | 1 | ~542 |
| Provider model mappers (ScriptObject enrichment) | 6 | ~957 |
| C# files importing `using Scriban` | 38 | — |
| C# files referencing `ScriptObject`/`ScriptArray` | 37 | — |
| **Total C# support code** | — | **~12,300** |

### 2.2 Constraints Preserved

| Constraint | How It's Maintained |
|------------|---------------------|
| **.NET 10 / C# 13** | No change |
| **NativeAOT** | Simplified (no trimmer workarounds) |
| **No external APIs** | No change (tool remains offline) |
| **Pure DI (ADR-006)** | No change (`CompositionRoot` pattern retained) |
| **Architecture boundaries (ADR-007)** | Strengthened (compile-time enforcement replaces runtime string matching) |
| **Security by default** | Simplified (sensitivity masking on C# models instead of `ScriptObject` trees) |
| **Zero third-party dependencies** | Achieved (Scriban was the sole dependency) |

---

## 3. System Overview

### 3.1 High-Level Architecture

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef cliNode fill:#f59e0b,stroke:#fbbf24,stroke-width:3px,color:#ffffff
    classDef parseNode fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef modelNode fill:#8b5cf6,stroke:#a78bfa,stroke-width:3px,color:#ffffff
    classDef renderNode fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff
    classDef providerNode fill:#ec4899,stroke:#f472b6,stroke-width:2px,color:#ffffff
    classDef outputNode fill:#06b6d4,stroke:#22d3ee,stroke-width:3px,color:#ffffff

    subgraph Input
        JSON["📦 Terraform Plan JSON"]
        SARIF["📋 SARIF Files (optional)"]
    end

    subgraph CLI["CLI Layer"]
        Parser["CliParser"]
        Options["CliOptions"]
    end

    subgraph Parsing["Parsing Layer"]
        TFParser["TerraformPlanParser"]
        Plan["TerraformPlan (record)"]
    end

    subgraph ModelBuilding["Model Building Layer"]
        Builder["ReportModelBuilder"]
        Model["ReportModel"]
        CodeAnalysis["CodeAnalysisLoader"]
    end

    subgraph Rendering["Rendering Layer (NEW — Pure C#)"]
        Writer["MarkdownWriter"]
        ReportRenderer["ReportRenderer"]
        ResourceRenderers["IResourceRenderer\nimplementations"]
        Helpers["RenderingHelpers\n(formerly ScribanHelpers)"]
    end

    subgraph Providers["Provider Layer"]
        AzureRM["AzureRM\nProvider"]
        AzApi["AzApi\nProvider"]
        AzureAD["AzureAD\nProvider"]
        AzureDevOps["AzureDevOps\nProvider"]
    end

    subgraph Output
        MD["📝 Markdown Output"]
        PR["GitHub / Azure DevOps PR"]
    end

    JSON --> TFParser
    SARIF --> CodeAnalysis
    Parser --> Options
    Options --> Builder
    TFParser --> Plan
    Plan --> Builder
    CodeAnalysis --> Builder
    Builder --> Model
    Model --> ReportRenderer
    ReportRenderer --> Writer
    ResourceRenderers --> Writer
    Helpers --> Writer
    Providers --> ResourceRenderers
    Writer --> MD
    MD --> PR

    class JSON,SARIF cliNode
    class Parser,Options cliNode
    class TFParser,Plan parseNode
    class Builder,Model,CodeAnalysis modelNode
    class Writer,ReportRenderer,ResourceRenderers,Helpers renderNode
    class AzureRM,AzApi,AzureAD,AzureDevOps providerNode
    class MD,PR outputNode
```

### 3.2 Layer Dependencies

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TB
    classDef layer fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef crosscut fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff

    CLI["CLI\n(entry point, argument parsing)"]
    Parsing["Parsing\n(JSON → domain models)"]
    ModelBuilding["Model Building\n(domain → report model)"]
    Rendering["Rendering\n(report model → markdown)"]
    Providers["Providers\n(resource-specific renderers)"]
    RenderTargets["RenderTargets\n(GitHub vs Azure DevOps)"]
    Platforms["Platforms\n(Azure utilities)"]
    CodeAnalysis["CodeAnalysis\n(SARIF integration)"]
    Diagnostics["Diagnostics\n(debug mode)"]

    CLI --> Parsing
    CLI --> ModelBuilding
    CLI --> Rendering
    CLI --> CodeAnalysis
    Parsing --> ModelBuilding
    ModelBuilding --> Providers
    Rendering --> Providers
    Rendering --> RenderTargets
    Providers -.-> Platforms
    ModelBuilding -.-> Platforms
    CLI -.-> Diagnostics

    class CLI,Parsing,ModelBuilding,Rendering layer
    class Providers,RenderTargets,Platforms,CodeAnalysis,Diagnostics crosscut
```

**Key dependency rule:** Providers depend on Rendering interfaces (e.g., `IResourceRenderer`), but the Rendering layer does NOT depend on specific Providers. This inversion enables modular provider registration.

---

## 4. Component Architecture

### 4.1 Directory Structure (Target State)

```
src/Oocx.TfPlan2Md/
├── CLI/                              # Unchanged
│   ├── CliParser.cs
│   ├── CliOptions.cs
│   └── HelpTextProvider.cs
│
├── Parsing/                          # Unchanged
│   ├── TerraformPlanParser.cs
│   ├── TerraformPlan.cs
│   ├── TfPlanJsonContext.cs
│   ├── ConfigurationReferenceResolver.cs
│   └── ReplacePathsConverter.cs
│
├── MarkdownGeneration/               # RESTRUCTURED
│   ├── ReportModel.cs                # Unchanged
│   ├── ResourceChangeModel.cs        # Unchanged
│   ├── AttributeChangeModel.cs       # Unchanged
│   ├── SummaryModel.cs               # Unchanged
│   ├── ModuleChangeGroup.cs          # Unchanged
│   ├── OutputChangeModel.cs          # Unchanged
│   ├── ActionIcons.cs                # Unchanged
│   ├── ActionSummary.cs              # Unchanged
│   │
│   ├── ReportModelBuilder.cs         # Unchanged (partial class, 7 files)
│   ├── ReportModelBuilder.Build.cs
│   ├── ReportModelBuilder.ResourceChanges.cs
│   ├── ReportModelBuilder.CodeAnalysis.cs
│   ├── ReportModelBuilder.Summaries.cs
│   ├── ReportModelBuilder.ParentChildMerging.cs
│   ├── ReportModelBuilder.Outputs.cs
│   │
│   ├── Rendering/                    # NEW — Pure C# rendering
│   │   ├── MarkdownWriter.cs         # Fluent markdown construction
│   │   ├── ReportRenderer.cs         # Full report orchestrator
│   │   ├── SummaryRenderer.cs        # Summary table rendering
│   │   ├── HeaderRenderer.cs         # Report header/metadata
│   │   ├── DefaultResourceRenderer.cs # Fallback resource renderer
│   │   ├── ChildResourceRenderer.cs  # Child resource tables
│   │   ├── CodeAnalysisRenderer.cs   # Code analysis sections
│   │   ├── RefactoringRenderer.cs    # Import/move operations
│   │   ├── OutputRenderer.cs         # Terraform outputs
│   │   ├── IResourceRenderer.cs      # Provider extension point
│   │   └── ResourceRendererRegistry.cs # Renderer dispatch
│   │
│   ├── Helpers/                      # RENAMED from ScribanHelpers
│   │   ├── RenderingHelpers/         # Renamed from ScribanHelpers/
│   │   │   ├── AttributeCollection.cs
│   │   │   ├── CodeAnalysis.cs
│   │   │   ├── CodeFormatting.cs
│   │   │   ├── DetailsDisplay.cs
│   │   │   ├── DiffArray.cs
│   │   │   ├── DiffComputation.cs
│   │   │   ├── DiffFormatting.cs
│   │   │   ├── DiffUtilities.cs
│   │   │   ├── Json.cs
│   │   │   ├── LargeValues.cs
│   │   │   ├── LargeValueSummary.cs
│   │   │   ├── Markdown.cs
│   │   │   ├── Registry.cs           # SIMPLIFIED (no ScriptObject.Import)
│   │   │   ├── SemanticFormatting.cs
│   │   │   ├── SemanticFormatting.Helpers.cs
│   │   │   ├── SemanticFormatting.Identity.cs
│   │   │   ├── SemanticFormatting.Registry.cs
│   │   │   ├── ValueFormatting.cs
│   │   │   └── AzApi.Metadata.cs     # Moved from provider-specific helpers
│   │   ├── AfterUnknownHelper.cs     # Unchanged
│   │   ├── JsonFlattener.cs          # Unchanged
│   │   └── ResourceSummaryHtmlBuilder.cs  # Unchanged
│   │
│   ├── Models/                       # Mostly unchanged
│   │   ├── IResourceViewModelFactory.cs
│   │   ├── IResourceViewModelFactoryRegistry.cs
│   │   ├── IParentChildRelationshipRegistry.cs
│   │   ├── ParentChildRelationship.cs
│   │   └── ...
│   │
│   ├── Summaries/                    # Unchanged
│   │   ├── ResourceSummaryBuilder.cs
│   │   └── ...
│   │
│   ├── Services/                     # SIMPLIFIED
│   │   ├── ProviderRegistry.cs       # Simplified (no RegisterAllHelpers)
│   │   ├── ValueFormatterRegistry.cs # Unchanged
│   │   ├── IconProviderRegistry.cs   # Unchanged
│   │   ├── AttributeChangeFilterRegistry.cs # Unchanged
│   │   ├── PatternMatchingRegistry.cs # Unchanged
│   │   ├── ResourceRendererRegistry.cs # NEW — replaces template dispatch
│   │   └── REMOVED: ResourceModelMapperRegistry.cs
│   │
│   ├── REMOVED: TemplateLoader.cs
│   ├── REMOVED: TemplateResolver.cs
│   ├── REMOVED: AotScriptObjectMapper.cs
│   ├── REMOVED: ScribanHelperException.cs
│   └── REMOVED: Templates/            # All 10 core .sbn files removed
│
├── Providers/                        # SIMPLIFIED
│   ├── IProviderModule.cs            # Simplified interface
│   ├── Shared/
│   │   └── Icons/
│   │       └── azure-common-icons.json  # Unchanged (shared icon config)
│   ├── AzureRM/
│   │   ├── AzureRMModule.cs          # No RegisterHelpers(ScriptObject)
│   │   ├── Renderers/                # NEW — C# renderers replace .sbn templates
│   │   │   ├── RoleAssignmentRenderer.cs
│   │   │   ├── FirewallNetworkRuleRenderer.cs
│   │   │   ├── FirewallAppRuleRenderer.cs
│   │   │   └── NsgRenderer.cs
│   │   ├── Models/                   # Unchanged (ViewModels)
│   │   ├── RowExtractors/            # Unchanged
│   │   ├── Formatters/               # Unchanged
│   │   ├── Registration/             # Unchanged
│   │   ├── REMOVED: Mappers/         # ScriptObject mappers removed
│   │   └── REMOVED: Templates/       # .sbn files removed
│   ├── AzApi/
│   │   ├── AzApiModule.cs
│   │   ├── AzureApiDocumentationMapper.cs     # Unchanged (display name mapping)
│   │   ├── AzureApiDocumentationMapper.Loader.cs
│   │   ├── AzureApiDocumentationMappingsModel.cs
│   │   ├── AzureApiDocumentationMappingsJsonContext.cs
│   │   ├── Renderers/                # NEW — replaces templates + 15 ScribanHelpers files
│   │   │   ├── AzApiResourceRenderer.cs       # Handles azapi_resource
│   │   │   ├── AzApiUpdateResourceRenderer.cs # Handles azapi_update_resource
│   │   │   └── AzApiOutputValuesRenderer.cs   # Output values section (feature 106)
│   │   ├── Helpers/                  # SIMPLIFIED
│   │   │   └── RenderingHelpers/     # Renamed; retains grouping/flattening/rendering logic
│   │   ├── Data/                     # Unchanged (documentation mappings JSON)
│   │   ├── REMOVED: Helpers/ScribanHelpers/  # 15 files, ~6,320 lines eliminated
│   │   └── REMOVED: Templates/       # .sbn files removed
│   ├── AzureAD/
│   │   ├── AzureADModule.cs
│   │   ├── Renderers/                # NEW
│   │   │   ├── UserRenderer.cs
│   │   │   ├── GroupRenderer.cs
│   │   │   ├── GroupWithoutMembersRenderer.cs
│   │   │   ├── GroupMemberRenderer.cs
│   │   │   ├── ServicePrincipalRenderer.cs
│   │   │   └── InvitationRenderer.cs
│   │   └── REMOVED: Templates/
│   └── AzureDevOps/
│       ├── AzureDevOpsModule.cs
│       ├── Renderers/                # NEW
│       │   ├── VariableGroupRenderer.cs
│       │   └── BuildDefinitionRenderer.cs  # Replaces build_definition.sbn + 3 partials
│       ├── Mappers/                  # REMOVED (ScriptObject enrichment)
│       ├── Models/                   # Unchanged (ViewModels)
│       └── REMOVED: Templates/
│
├── RenderTargets/                    # Unchanged
│   ├── IDiffFormatter.cs
│   ├── RenderTarget.cs
│   ├── DetailsDisplayMode.cs
│   ├── GitHub/
│   │   └── GitHubDiffFormatter.cs
│   └── AzureDevOps/
│       └── AzureDevOpsDiffFormatter.cs
│
├── Platforms/                        # SIMPLIFIED
│   └── Azure/
│       ├── IPrincipalMapper.cs       # Unchanged
│       ├── AzureScopeParser.cs       # Unchanged
│       ├── EnrichedAzureScopeFormatter.cs # Unchanged
│       ├── AzureEntityMapper.cs      # Unchanged
│       └── REMOVED: ScribanHelpers.Azure.cs  # Merged into RenderingHelpers
│
├── CodeAnalysis/                     # Unchanged
│   ├── CodeAnalysisLoader.cs
│   ├── SarifParser.cs
│   └── ...
│
├── Diagnostics/                      # Unchanged
│   ├── DiagnosticContext.cs
│   └── ...
│
├── CompositionRoot.cs                # Simplified (no template loader creation)
├── Program.cs                        # Unchanged
├── ProgramEntry.cs                   # Simplified (no template path handling)
├── Oocx.TfPlan2Md.csproj            # No PackageReference entries
├── GlobalSuppressions.cs             # SIMPLIFIED (no Scriban-related suppressions)
└── REMOVED: TrimmerRootDescriptor.xml  # No longer needed
```

### 4.2 Component Responsibility Matrix

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
block-beta
    columns 3
    
    block:CLI["CLI"]:1
        CliParser
        CliOptions
        HelpText
    end
    
    block:Parse["Parsing"]:1
        TFParser["TerraformPlanParser"]
        TFPlan["TerraformPlan"]
        JsonCtx["TfPlanJsonContext"]
    end
    
    block:Analysis["CodeAnalysis"]:1
        SarifParser
        CodeLoader["CodeAnalysisLoader"]
        ResMapper["ResourceMapper"]
    end
    
    block:Model["Model Building"]:1
        ReportBuilder["ReportModelBuilder\n(7 partial files)"]
        SummaryBuilder["ResourceSummaryBuilder"]
        space
    end
    
    block:Render["Rendering (NEW)"]:1
        MarkdownWriter
        ReportRenderer
        RendererRegistry["ResourceRendererRegistry"]
    end
    
    block:Providers2["Providers"]:1
        IProviderModule
        IResourceRenderer
        Implementations["4 Provider Modules\n+ Shared Icons\n+ Renderers"]
    end

    style CLI fill:#f59e0b,stroke:#fbbf24,color:#ffffff
    style Parse fill:#3b82f6,stroke:#60a5fa,color:#ffffff
    style Analysis fill:#ef4444,stroke:#f87171,color:#ffffff
    style Model fill:#8b5cf6,stroke:#a78bfa,color:#ffffff
    style Render fill:#10b981,stroke:#34d399,color:#ffffff
    style Providers2 fill:#ec4899,stroke:#f472b6,color:#ffffff
```

### 4.3 Current Template Inventory (28 files)

For migration planning, here is the complete list of `.sbn` templates to convert:

**Core templates (10 files, `MarkdownGeneration/Templates/`):**

| Template | Purpose | Target Renderer |
|----------|---------|----------------|
| `default.sbn` | Report entry point / orchestrator | `ReportRenderer` |
| `summary.sbn` | Summary-only mode (no resource details) | `SummaryRenderer` |
| `_header.sbn` | Report header with metadata | `HeaderRenderer` |
| `_summary.sbn` | Summary table (creates/updates/deletes/replaces) | `SummaryRenderer` |
| `_resource.sbn` | Generic resource change rendering (fallback) | `DefaultResourceRenderer` |
| `_child_resources.sbn` | Child resource group tables | `ChildResourceRenderer` |
| `_code_analysis_summary.sbn` | Code analysis summary section | `CodeAnalysisRenderer` |
| `_code_analysis_findings.sbn` | Per-resource code analysis findings | `CodeAnalysisRenderer` |
| `_code_analysis_other_findings.sbn` | Unmapped code analysis findings | `CodeAnalysisRenderer` |
| `_code_analysis_metadata.sbn` | Code analysis tool metadata | `CodeAnalysisRenderer` |

**AzureRM templates (4 files, `Providers/AzureRM/Templates/azurerm/`):**

| Template | Purpose | Target Renderer |
|----------|---------|----------------|
| `role_assignment.sbn` | Role assignment rendering | `RoleAssignmentRenderer` |
| `network_security_group.sbn` | NSG rule table | `NsgRenderer` |
| `firewall_network_rule_collection.sbn` | Firewall network rules | `FirewallNetworkRuleRenderer` |
| `firewall_application_rule_collection.sbn` | Firewall app rules | `FirewallAppRuleRenderer` |

**AzApi templates (3 files, `Providers/AzApi/Templates/azapi/`):**

| Template | Purpose | Target Renderer |
|----------|---------|----------------|
| `resource.sbn` | azapi_resource rendering (body + output values) | `AzApiResourceRenderer` |
| `update_resource.sbn` | azapi_update_resource rendering | `AzApiUpdateResourceRenderer` |
| `_output_values.sbn` | Output values partial (feature 106) | `AzApiOutputValuesRenderer` |

**AzureAD templates (6 files, `Providers/AzureAD/Templates/azuread/`):**

| Template | Purpose | Target Renderer |
|----------|---------|----------------|
| `user.sbn` | User rendering | `UserRenderer` |
| `group.sbn` | Group with members rendering | `GroupRenderer` |
| `group_without_members.sbn` | Group without members rendering | `GroupWithoutMembersRenderer` |
| `group_member.sbn` | Group member rendering | `GroupMemberRenderer` |
| `service_principal.sbn` | Service principal rendering | `ServicePrincipalRenderer` |
| `invitation.sbn` | Invitation rendering | `InvitationRenderer` |

**AzureDevOps templates (5 files, `Providers/AzureDevOps/Templates/azuredevops/`):**

| Template | Purpose | Target Renderer |
|----------|---------|----------------|
| `variable_group.sbn` | Variable group rendering | `VariableGroupRenderer` |
| `build_definition.sbn` | Build definition entry point | `BuildDefinitionRenderer` |
| `_build_definition_variables.sbn` | Build definition variables partial | (inlined into `BuildDefinitionRenderer`) |
| `_build_definition_triggers.sbn` | Build definition triggers partial | (inlined into `BuildDefinitionRenderer`) |
| `_build_definition_other_blocks.sbn` | Build definition other blocks partial | (inlined into `BuildDefinitionRenderer`) |

---

## 5. Rendering Pipeline

### 5.1 Core Interfaces

The rendering layer introduces three key abstractions:

#### `MarkdownWriter` — Fluent Markdown Builder

```csharp
internal sealed class MarkdownWriter
{
    // Structural methods
    MarkdownWriter Heading(int level, string text);
    MarkdownWriter Paragraph(string text);
    MarkdownWriter BlankLine();

    // Table methods
    MarkdownWriter TableHeader(params string[] columns);
    MarkdownWriter TableRow(params string[] cells);

    // HTML methods (for GitHub/Azure DevOps compatibility)
    MarkdownWriter DetailsOpen(string summary, bool open = false);
    MarkdownWriter DetailsClose();

    // Content methods
    MarkdownWriter Code(string text);
    MarkdownWriter InlineCode(string text);
    MarkdownWriter Raw(string markdown);

    // Output
    string Build();
}
```

#### `IResourceRenderer` — Provider Extension Point

```csharp
internal interface IResourceRenderer
{
    // The Terraform resource types this renderer handles
    IReadOnlyList<string> SupportedResourceTypes { get; }

    // Render a resource change to markdown
    void Render(ResourceChangeModel change, MarkdownWriter writer, RenderContext context);
}
```

#### `RenderContext` — Shared Rendering State

```csharp
internal sealed record RenderContext(
    RenderTarget RenderTarget,
    IDiffFormatter DiffFormatter,
    ValueFormatterRegistry ValueFormatters,
    IconProviderRegistry IconProviders,
    IPrincipalMapper PrincipalMapper,
    DetailsDisplayMode DetailsDisplayMode,
    bool ShowSensitive,
    bool ShowUnchangedValues,
    bool IgnoreAzureIdCaseChanges);
```

The `RenderContext` consolidates all rendering configuration that is currently spread across `ReportModel` properties and the `MarkdownRenderer` constructor. It is constructed once in `CompositionRoot` and passed through the rendering pipeline.

### 5.2 Rendering Pipeline Flow

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
sequenceDiagram
    participant Caller as ProgramEntry
    participant RR as ReportRenderer
    participant MW as MarkdownWriter
    participant Registry as ResourceRendererRegistry
    participant Renderer as IResourceRenderer
    participant Default as DefaultResourceRenderer
    participant Helpers as RenderingHelpers

    Caller->>RR: Render(reportModel, renderContext)

    RR->>MW: new MarkdownWriter()
    RR->>MW: HeaderRenderer.Render(model, writer)
    RR->>MW: SummaryRenderer.Render(model.Summary, writer)
    RR->>MW: CodeAnalysisRenderer.RenderSummary(model, writer)

    loop For each module in model.ModuleChanges
        RR->>MW: Heading(3, "📦 Module: ...")

        loop For each change in module.Changes
            RR->>Registry: GetRenderer(change.Type)
            alt Resource-specific renderer found
                Registry-->>RR: IResourceRenderer
                RR->>Renderer: Render(change, writer, context)
                Renderer->>Helpers: FormatDiff(), EscapeMarkdown(), etc.
                Renderer->>MW: DetailsOpen(), TableHeader(), TableRow(), etc.
            else No specific renderer
                Registry-->>RR: null
                RR->>Default: Render(change, writer, context)
                Default->>Helpers: FormatDiff(), EscapeMarkdown(), etc.
                Default->>MW: DetailsOpen(), TableHeader(), TableRow(), etc.
            end
        end

        RR->>MW: OutputRenderer.Render(module.Outputs, writer)
    end

    RR->>MW: CodeAnalysisRenderer.RenderFindings(model, writer)
    RR->>MW: RefactoringRenderer.Render(model, writer)
    RR->>MW: OutputRenderer.RenderGlobal(model, writer)

    RR->>MW: Build()
    MW-->>Caller: Markdown string
```

### 5.3 Resource Renderer Dispatch

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef startNode fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff
    classDef processNode fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef decisionNode fill:#f59e0b,stroke:#fbbf24,stroke-width:3px,color:#ffffff
    classDef endNode fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff

    Start["Render resource:\nazurerm_role_assignment"]
    Lookup["ResourceRendererRegistry\n.GetRenderer('azurerm_role_assignment')"]
    Found{Renderer\nregistered?}
    Specific["✅ Use RoleAssignmentRenderer\n(provider-specific C# class)"]
    Default["📄 Use DefaultResourceRenderer\n(generic attribute table)"]
    Render["Call renderer.Render(change, writer, context)"]
    Output["Markdown appended to MarkdownWriter"]

    Start --> Lookup
    Lookup --> Found
    Found -->|Yes| Specific
    Found -->|No| Default
    Specific --> Render
    Default --> Render
    Render --> Output

    class Start startNode
    class Lookup processNode
    class Found decisionNode
    class Specific,Default endNode
    class Render processNode
    class Output endNode
```

**Comparison with current template resolution:**

| Aspect | Scriban (Current) | Pure C# (Target) |
|--------|-------------------|-------------------|
| Dispatch key | String-based file path (`azurerm/role_assignment.sbn`) | Type-based registry lookup by resource type |
| Fallback | `_resource.sbn` (embedded resource) | `DefaultResourceRenderer` (compiled code) |
| Error on typo | Silent empty output at runtime | Compile error or explicit `null` return |
| New renderer | Create `.sbn` file + `ScriptObject` mapper | Implement `IResourceRenderer` |
| IDE support | None (string-based) | Full (Go to Definition, Find References) |

### 5.4 MarkdownWriter Normalization

The `MarkdownWriter` applies the same output normalization currently done by `MarkdownRenderer`:

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart LR
    classDef processNode fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef dataNode fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff

    Raw["Raw markdown\nfrom renderers"]
    N1["Remove blank lines\nbetween table rows"]
    N2["Remove indentation\nfrom table rows"]
    N3["Collapse multiple\nblank lines"]
    N4["Ensure blank line\nbefore headings"]
    N5["Ensure blank line\nafter headings"]
    Final["Normalized\nmarkdown output"]

    Raw --> N1 --> N2 --> N3 --> N4 --> N5 --> Final

    class Raw,Final dataNode
    class N1,N2,N3,N4,N5 processNode
```

These normalizations use the same 5 compiled `Regex` instances currently in `MarkdownRenderer.cs`, ensuring identical output.

---

## 6. Provider Architecture

### 6.1 Simplified Provider Module Interface

The `IProviderModule` interface is simplified by removing Scriban-specific methods:

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
classDiagram
    class IProviderModule {
        <<interface>>
        +string ProviderName
        +RegisterRenderers(ResourceRendererRegistry) void
        +RegisterFactories(IResourceViewModelFactoryRegistry) void
        +RegisterValueFormatters(ValueFormatterRegistry) void
        +RegisterIconProviders(IconProviderRegistry) void
        +RegisterParentChildRelationships(IParentChildRelationshipRegistry) void
        +RegisterAttributeChangeFilters(AttributeChangeFilterRegistry) void
        +RegisterPostMergeCallbacks(ReportModelBuilder) void
    }

    class IResourceRenderer {
        <<interface>>
        +IReadOnlyList~string~ SupportedResourceTypes
        +Render(ResourceChangeModel, MarkdownWriter, RenderContext) void
    }

    class AzureRMModule {
        +ProviderName = "azurerm"
        +RegisterRenderers(registry)
        +RegisterFactories(registry)
        +RegisterValueFormatters(registry)
        +RegisterIconProviders(registry)
        +RegisterParentChildRelationships(registry)
        +RegisterAttributeChangeFilters(registry)
        +RegisterPostMergeCallbacks(builder)
    }

    class RoleAssignmentRenderer {
        +SupportedResourceTypes = ["azurerm_role_assignment"]
        +Render(change, writer, context)
    }

    class NsgRenderer {
        +SupportedResourceTypes = ["azurerm_network_security_group"]
        +Render(change, writer, context)
    }

    class DefaultResourceRenderer {
        +SupportedResourceTypes = ["*"]
        +Render(change, writer, context)
    }

    IProviderModule <|.. AzureRMModule
    IResourceRenderer <|.. RoleAssignmentRenderer
    IResourceRenderer <|.. NsgRenderer
    IResourceRenderer <|.. DefaultResourceRenderer
    AzureRMModule --> RoleAssignmentRenderer : registers
    AzureRMModule --> NsgRenderer : registers

    style IProviderModule fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    style IResourceRenderer fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff
    style AzureRMModule fill:#ec4899,stroke:#f472b6,stroke-width:2px,color:#ffffff
    style RoleAssignmentRenderer fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    style NsgRenderer fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    style DefaultResourceRenderer fill:#f59e0b,stroke:#fbbf24,stroke-width:2px,color:#ffffff
```

**Removed methods (Scriban-specific):**

- ~~`RegisterHelpers(ScriptObject)`~~ — No more Scriban helper registration
- ~~`string TemplateResourcePrefix`~~ — No more embedded `.sbn` templates
- ~~`RegisterResourceModelMappers(ResourceModelMapperRegistry)`~~ — No more `ScriptObject` enrichment

**New method:**

- `RegisterRenderers(ResourceRendererRegistry)` — Register typed C# renderers

**Retained methods (unchanged):**

- `RegisterFactories(IResourceViewModelFactoryRegistry)` — ViewModel factory registration
- `RegisterValueFormatters(ValueFormatterRegistry)` — Provider-specific value formatting
- `RegisterIconProviders(IconProviderRegistry)` — Provider-specific icons
- `RegisterParentChildRelationships(IParentChildRelationshipRegistry)` — Resource grouping
- `RegisterAttributeChangeFilters(AttributeChangeFilterRegistry)` — Attribute filtering (e.g., Azure ID case)
- `RegisterPostMergeCallbacks(ReportModelBuilder)` — Post-merge processing hooks

### 6.2 Provider Module Comparison

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart LR
    classDef oldNode fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff
    classDef newNode fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    classDef sharedNode fill:#3b82f6,stroke:#60a5fa,stroke-width:2px,color:#ffffff

    subgraph Current["Current Provider (Scriban)"]
        direction TB
        C1["RegisterHelpers\n(ScriptObject)"]
        C2["RegisterFactories\n(IResourceViewModelFactoryRegistry)"]
        C3[".sbn Template Files\n(embedded resources)"]
        C4["ScriptObject Mappers\n(C# → ScriptObject)"]
        C5["RegisterValueFormatters"]
        C6["RegisterIconProviders"]
    end

    subgraph Target["Target Provider (Pure C#)"]
        direction TB
        T1["RegisterRenderers\n(ResourceRendererRegistry)"]
        T2["RegisterFactories\n(IResourceViewModelFactoryRegistry)"]
        T3["IResourceRenderer\nimplementations"]
        T5["RegisterValueFormatters"]
        T6["RegisterIconProviders"]
    end

    C1 -.->|"REMOVED"| T1
    C2 -->|"Unchanged"| T2
    C3 -.->|"Replaced by"| T3
    C4 -.->|"REMOVED\n(no ScriptObject needed)"| T3
    C5 -->|"Unchanged"| T5
    C6 -->|"Unchanged"| T6

    class C1,C3,C4 oldNode
    class T1,T3 newNode
    class C2,C5,C6,T2,T5,T6 sharedNode
```

### 6.3 All Provider Renderers

| Provider | Resource Type | Renderer Class | Currently in Template |
|----------|--------------|----------------|----------------------|
| **AzureRM** | `azurerm_role_assignment` | `RoleAssignmentRenderer` | `role_assignment.sbn` |
| **AzureRM** | `azurerm_network_security_group` | `NsgRenderer` | `network_security_group.sbn` |
| **AzureRM** | `azurerm_firewall_network_rule_collection` | `FirewallNetworkRuleRenderer` | `firewall_network_rule_collection.sbn` |
| **AzureRM** | `azurerm_firewall_application_rule_collection` | `FirewallAppRuleRenderer` | `firewall_application_rule_collection.sbn` |
| **AzApi** | `azapi_resource` | `AzApiResourceRenderer` | `resource.sbn` + `_output_values.sbn` |
| **AzApi** | `azapi_update_resource` | `AzApiUpdateResourceRenderer` | `update_resource.sbn` + `_output_values.sbn` |
| **AzureAD** | `azuread_user` | `UserRenderer` | `user.sbn` |
| **AzureAD** | `azuread_group` | `GroupRenderer` | `group.sbn` |
| **AzureAD** | `azuread_group` (without members) | `GroupWithoutMembersRenderer` | `group_without_members.sbn` |
| **AzureAD** | `azuread_group_member` | `GroupMemberRenderer` | `group_member.sbn` |
| **AzureAD** | `azuread_service_principal` | `ServicePrincipalRenderer` | `service_principal.sbn` |
| **AzureAD** | `azuread_invitation` | `InvitationRenderer` | `invitation.sbn` |
| **AzureDevOps** | `azuredevops_variable_group` | `VariableGroupRenderer` | `variable_group.sbn` |
| **AzureDevOps** | `azuredevops_build_definition` | `BuildDefinitionRenderer` | `build_definition.sbn` + 3 partials |
| **Core** | _(all others)_ | `DefaultResourceRenderer` | `_resource.sbn` |

**Note on AzApi output values (feature 106):** The current `_output_values.sbn` partial template renders azapi output data in a dedicated section. In the pure C# architecture, this logic is encapsulated in `AzApiOutputValuesRenderer`, which is called by `AzApiResourceRenderer` and `AzApiUpdateResourceRenderer`. This includes handling of `after_unknown`, `before_sensitive`, and `after_sensitive` maps for output-level sensitivity masking and "known after apply" notices.

### 6.4 AzApi Provider — Complex Rendering Architecture

The AzApi provider is the most complex provider in the codebase. Its rendering logic (currently ~6,320 lines across 15 `ScribanHelpers` files) handles:

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef dataNode fill:#3b82f6,stroke:#60a5fa,stroke-width:2px,color:#ffffff
    classDef processNode fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff
    classDef renderNode fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff

    Input["azapi_resource change"]
    Parse["Parse body JSON\n(AzApi.JsonFlattening)"]
    Group["Group attributes\ninto hierarchical sections\n(AzApi.Grouping)"]
    Metadata["Resolve display names\nfrom Azure API docs\n(AzApi.Metadata)"]
    Sensitive["Apply sensitivity masking\n(before_sensitive / after_sensitive)"]
    LargeVal["Detect large values\n(inline-diff rendering)"]
    RenderBody["Render body attributes\nby action: create/update/delete\n(AzApi.Rendering.*)"]
    RenderOutput["Render output values\nsection if present\n(feature 106)"]
    OutputMd["Markdown tables\nwith grouped H6 sub-sections"]

    Input --> Parse
    Parse --> Group
    Group --> Metadata
    Metadata --> Sensitive
    Sensitive --> LargeVal
    LargeVal --> RenderBody
    LargeVal --> RenderOutput
    RenderBody --> OutputMd
    RenderOutput --> OutputMd

    class Input dataNode
    class Parse,Group,Metadata,Sensitive,LargeVal processNode
    class RenderBody,RenderOutput,OutputMd renderNode
```

**Current AzApi ScribanHelpers files (15 files, ~6,320 lines):**

| File | Responsibility | Lines |
|------|---------------|------:|
| `AzApi.cs` | Registration entry point | ~21 |
| `AzApi.Registration.cs` | Helper function registration into ScriptObject | ~38 |
| `AzApi.Data.cs` | Data structures for flattened attributes | ~320 |
| `AzApi.Grouping.cs` | Hierarchical attribute grouping | ~430 |
| `AzApi.JsonFlattening.cs` | JSON → flat key-value pairs | ~260 |
| `AzApi.Metadata.cs` | Azure API documentation display names | ~100 |
| `AzApi.Resources.cs` | Resource type detection and handling | ~105 |
| `AzApi.Rendering.cs` | Main rendering dispatch | ~120 |
| `AzApi.Rendering.Constants.cs` | Rendering constants | ~22 |
| `AzApi.Rendering.Shared.cs` | Shared rendering utilities | ~130 |
| `AzApi.Rendering.Array.cs` | Array attribute rendering | ~270 |
| `AzApi.Rendering.CreateDelete.cs` | Create/delete action rendering | ~580 |
| `AzApi.Rendering.Update.cs` | Update action rendering (diff tables) | ~720 |

In the pure C# architecture, these become regular C# classes with typed method signatures instead of `ScriptObject.Import(...)` delegates. The core formatting logic is retained — only the Scriban registration glue is eliminated.

---

## 7. Service Registry Architecture

### 7.1 Registry Landscape

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
classDiagram
    class PatternMatchingRegistry~T~ {
        +Register(pattern, service)
        +Resolve(resourceType) T?
    }

    class ValueFormatterRegistry {
        +Register(pattern, formatter)
        +Format(name, value, resourceType) string
    }

    class IconProviderRegistry {
        +Register(pattern, provider)
        +GetIcon(name, value, resourceType) string?
    }

    class ResourceRendererRegistry {
        <<NEW>>
        +Register(resourceType, renderer)
        +GetRenderer(resourceType) IResourceRenderer?
    }

    class AttributeChangeFilterRegistry {
        +Register(pattern, filter)
        +ShouldFilter(context) bool
    }

    class ParentChildRelationshipRegistry {
        +Register(relationship)
        +GetRelationship(parentType, childType) ParentChildRelationship?
    }

    PatternMatchingRegistry~T~ <|-- ValueFormatterRegistry : extends
    PatternMatchingRegistry~T~ <|-- IconProviderRegistry : extends
    PatternMatchingRegistry~T~ <|-- ResourceRendererRegistry : extends

    note for ResourceRendererRegistry "NEW: Replaces template-based\ndispatch (TemplateResolver)"

    style PatternMatchingRegistry~T~ fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    style ValueFormatterRegistry fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    style IconProviderRegistry fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    style ResourceRendererRegistry fill:#f59e0b,stroke:#fbbf24,stroke-width:3px,color:#ffffff
    style AttributeChangeFilterRegistry fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    style ParentChildRelationshipRegistry fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
```

### 7.2 Resource Renderer Registration Flow

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
sequenceDiagram
    participant CR as CompositionRoot
    participant PR as ProviderRegistry
    participant AzRM as AzureRMModule
    participant RRR as ResourceRendererRegistry

    CR->>PR: CreateProviderRegistry()
    CR->>RRR: new ResourceRendererRegistry()

    CR->>PR: RegisterAllRenderers(rendererRegistry)
    PR->>AzRM: RegisterRenderers(rendererRegistry)
    AzRM->>RRR: Register("azurerm_role_assignment", new RoleAssignmentRenderer(...))
    AzRM->>RRR: Register("azurerm_network_security_group", new NsgRenderer(...))
    AzRM->>RRR: Register("azurerm_firewall_network_rule_collection", new FirewallNetworkRuleRenderer(...))
    AzRM->>RRR: Register("azurerm_firewall_application_rule_collection", new FirewallAppRuleRenderer(...))

    Note over CR,RRR: Same pattern repeated for AzApi, AzureAD, AzureDevOps modules
```

---

## 8. Data Model

### 8.1 ReportModel Structure

The `ReportModel` is the immutable data structure that captures all information needed to render a report. It remains unchanged in the target architecture — the difference is that renderers access its properties directly via C# instead of through `ScriptObject` wrappers.

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
classDiagram
    class ReportModel {
        +string TerraformVersion
        +string FormatVersion
        +string TfPlan2MdVersion
        +string CommitHash
        +DateTimeOffset GeneratedAtUtc
        +bool HideMetadata
        +string? Timestamp
        +string? ReportTitle
        +IReadOnlyList~ResourceChangeModel~ Changes
        +IReadOnlyList~ModuleChangeGroup~ ModuleChanges
        +SummaryModel Summary
        +CodeAnalysisReportModel? CodeAnalysis
        +bool ShowUnchangedValues
        +bool IgnoreAzureIdCaseChanges
        +bool ShowSensitive
        +RenderTarget RenderTarget
        +DetailsDisplayMode DetailsDisplayMode
        +IReadOnlyList~RefactoringOperationModel~ RefactoringOperations
        +IReadOnlyList~OutputChangeModel~ GlobalOutputs
        +int FilteredResourceCount
    }

    class ResourceChangeModel {
        +string Address
        +string? ModuleAddress
        +string Type
        +string Name
        +string ProviderName
        +string Action
        +string ActionSymbol
        +IReadOnlyList~AttributeChangeModel~ AttributeChanges
        +object? BeforeJson
        +object? AfterJson
        +IReadOnlyList~IReadOnlyList~object~~? ReplacePaths
        +string? Summary
        +string? SummaryHtml
        +string? ChangedAttributesSummary
        +string? TagsBadges
        +IReadOnlyList~ChildResourceGroup~ ChildResourceGroups
        +IReadOnlyList~CodeAnalysisFindingModel~ CodeAnalysisFindings
        +string? ImportId
        +string? MovedFromAddress
        +bool IsRefactoringAlreadyApplied
        +object? BeforeSensitive
        +object? AfterSensitive
        +object? AfterUnknown
        +bool HasWholeResourceUnknownAfterApply
    }

    class AttributeChangeModel {
        +string Name
        +string? Before
        +string? After
        +string Action
        +string? FindingIndicator
        +bool IsUnknownAfterApply
    }

    class ModuleChangeGroup {
        +string ModuleName
        +IReadOnlyList~ResourceChangeModel~ Changes
        +IReadOnlyList~OutputChangeModel~ Outputs
    }

    class OutputChangeModel {
        +string Name
        +string Action
        +string ActionSymbol
        +string? Before
        +string? After
        +bool Sensitive
    }

    class SummaryModel {
        +int TotalChanges
        +int Creates
        +int Updates
        +int Deletes
        +int Replaces
        +int Reads
        +int Imports
        +int Moves
        +int NoOps
        +IReadOnlyList~ResourceTypeBreakdown~ ResourceTypeBreakdowns
    }

    ReportModel --> ResourceChangeModel : Changes
    ReportModel --> ModuleChangeGroup : ModuleChanges
    ReportModel --> SummaryModel : Summary
    ReportModel --> OutputChangeModel : GlobalOutputs
    ModuleChangeGroup --> ResourceChangeModel : Changes
    ModuleChangeGroup --> OutputChangeModel : Outputs
    ResourceChangeModel --> AttributeChangeModel : AttributeChanges

    style ReportModel fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    style ResourceChangeModel fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff
    style AttributeChangeModel fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    style ModuleChangeGroup fill:#ec4899,stroke:#f472b6,stroke-width:2px,color:#ffffff
    style OutputChangeModel fill:#f59e0b,stroke:#fbbf24,stroke-width:2px,color:#ffffff
    style SummaryModel fill:#06b6d4,stroke:#22d3ee,stroke-width:2px,color:#ffffff
```

### 8.2 Sensitivity and Unknown-After-Apply Data

Three properties on `ResourceChangeModel` carry Terraform plan metadata that provider renderers use for output rendering:

| Property | Type | Purpose |
|----------|------|---------|
| `BeforeSensitive` | `object?` | Terraform's `before_sensitive` map — marks which before-state values are sensitive |
| `AfterSensitive` | `object?` | Terraform's `after_sensitive` map — marks which after-state values are sensitive |
| `AfterUnknown` | `object?` | Terraform's `after_unknown` map — marks which values will only be known after apply |
| `HasWholeResourceUnknownAfterApply` | `bool` | `true` when `after_unknown` is a root boolean `true` (entire resource is computed) |

**In the current architecture**, these are mapped to `ScriptObject` fields by `AotScriptObjectMapper` and accessed via template syntax (e.g., `{{ change.after_unknown.output }}`). This requires the mapper to handle deeply nested JSON structures.

**In the target architecture**, renderers access these directly:

```csharp
// Current: Template accesses after_unknown via ScriptObject string keys
// {{ if change.after_unknown && change.after_unknown.output }}

// Target: C# renderer accesses typed properties directly
if (AfterUnknownHelper.IsAttributeUnknownAfterApply(change.AfterUnknown, "output"))
{
    // Render "known after apply" notice
}
```

The `AfterUnknownHelper` utility class (302 lines) is Scriban-independent and will be retained as-is. It provides navigation through the `after_unknown` JSON tree using flattened attribute keys.

### 8.3 ReportModelBuilder (7 Partial Files)

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef builderNode fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff
    classDef stepNode fill:#3b82f6,stroke:#60a5fa,stroke-width:2px,color:#ffffff

    Builder["ReportModelBuilder\n(7 partial files)"]

    B1["Build.cs\n• Entry point: Build()\n• Configuration reference index\n• Orchestrates all steps"]
    B2["ResourceChanges.cs\n• BuildResourceChanges()\n• BuildAttributeChanges()\n• Sensitivity masking\n• Known-after-apply resolution"]
    B3["ParentChildMerging.cs\n• MergeParentChild()\n• Child resource grouping"]
    B4["Summaries.cs\n• BuildSummary()\n• Action statistics"]
    B5["CodeAnalysis.cs\n• MapCodeAnalysis()\n• SARIF finding attachment"]
    B6["Outputs.cs\n• BuildOutputChanges()\n• Module & global outputs"]
    B7["ReportModelBuilder.cs\n• Constructor\n• Field declarations\n• Post-merge callback registration"]

    Builder --> B1
    Builder --> B2
    Builder --> B3
    Builder --> B4
    Builder --> B5
    Builder --> B6
    Builder --> B7

    class Builder builderNode
    class B1,B2,B3,B4,B5,B6,B7 stepNode
```

The `ReportModelBuilder` is **unchanged** by the Scriban removal. It already produces a complete `ReportModel` — the only difference is that the `AotScriptObjectMapper` translation step after model construction is eliminated.

---

## 9. Data Flow

### 9.1 End-to-End Data Flow

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef inputNode fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff
    classDef processNode fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef modelNode fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff
    classDef renderNode fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff
    classDef outputNode fill:#f59e0b,stroke:#fbbf24,stroke-width:2px,color:#ffffff

    subgraph Input
        JSON["terraform show -json\nplan.tfplan"]
        SARIF["SARIF files\n(optional)"]
        MappingFile["Principal mapping\n(optional)"]
    end

    subgraph Parse["1. Parse"]
        TFParser["TerraformPlanParser.Parse()"]
        Plan["TerraformPlan\n(immutable record)"]
    end

    subgraph Build["2. Build Model"]
        Builder["ReportModelBuilder.Build()"]
        Steps["• Determine actions\n• Build attribute changes\n• Mask sensitive values\n• Generate summaries\n• Group by module\n• Merge parent-child\n• Map code analysis\n• Build outputs"]
        Model["ReportModel\n(immutable, fully computed)"]
    end

    subgraph Render["3. Render (NEW — Pure C#)"]
        RR["ReportRenderer"]
        MW["MarkdownWriter"]
        Dispatch["ResourceRendererRegistry\ndispatches to IResourceRenderer"]
        Normalize["Output normalization\n(blank lines, headings, tables)"]
    end

    subgraph Output
        Markdown["📝 Markdown string"]
        File["File or stdout"]
    end

    JSON --> TFParser
    SARIF -.-> Builder
    MappingFile -.-> Builder
    TFParser --> Plan
    Plan --> Builder
    Builder --> Steps
    Steps --> Model
    Model --> RR
    RR --> MW
    RR --> Dispatch
    MW --> Normalize
    Normalize --> Markdown
    Markdown --> File

    class JSON,SARIF,MappingFile inputNode
    class TFParser processNode
    class Plan,Model modelNode
    class Builder,Steps processNode
    class RR,MW,Dispatch,Normalize renderNode
    class Markdown,File outputNode
```

### 9.2 Data Model (Unchanged)

The `ReportModel` and its component types remain unchanged. The key difference is that renderers consume these types directly via C# property access instead of through `ScriptObject` wrappers:

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart LR
    classDef oldPath fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff
    classDef newPath fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff
    classDef modelNode fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff

    Model["ReportModel\n(C# record)"]

    subgraph OldPath["Current Path (REMOVED)"]
        direction LR
        Mapper["AotScriptObjectMapper\n(683 lines)"]
        ScriptObj["ScriptObject tree"]
        Template["Scriban template\n(.sbn file)"]
    end

    subgraph NewPath["New Path"]
        direction LR
        Renderer["IResourceRenderer\n(C# class)"]
        Writer["MarkdownWriter"]
    end

    Model -->|"model.Changes[0].Address"| Renderer
    Renderer -->|"writer.TableRow(...)"| Writer

    Model -.->|"obj['address'] = change.Address"| Mapper
    Mapper -.-> ScriptObj
    ScriptObj -.->|"{{ change.address }}"| Template

    class Model modelNode
    class Mapper,ScriptObj,Template oldPath
    class Renderer,Writer newPath
```

### 9.3 Sensitivity Handling (Simplified)

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef oldNode fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff
    classDef newNode fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff

    subgraph Current["Current (Scriban) — ADR-009"]
        direction TB
        C1["Map C# model → ScriptObject"]
        C2["Walk ScriptObject tree"]
        C3["Apply sensitivity map to each node"]
        C4["Replace leaves with '(sensitive)'"]
        C5["Pass masked ScriptObject to template"]
    end

    subgraph Target["Target (Pure C#)"]
        direction TB
        T1["ReportModelBuilder masks values\nduring model construction"]
        T2["Renderer accesses model directly"]
        T3["Already-masked values rendered as-is"]
    end

    Current -.->|"Replaced by"| Target

    class C1,C2,C3,C4,C5 oldNode
    class T1,T2,T3 newNode
```

In the target architecture, sensitivity masking happens in `ReportModelBuilder` during model construction (the `AttributeChangeModel.Before`/`After` values are already masked). There is no need for a separate masking pass on intermediate data structures.

---

## 10. Composition and Dependency Injection

### 10.1 Simplified Composition Root

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef entryNode fill:#f59e0b,stroke:#fbbf24,stroke-width:3px,color:#ffffff
    classDef compNode fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef serviceNode fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    classDef removedNode fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff,stroke-dasharray: 5 5

    Entry["ProgramEntry.RunAsync()"]
    Options["CliOptions"]
    Compose["CompositionRoot.ComposeServices()"]

    subgraph Composition["Pure DI Composition"]
        Mappers["Create Azure Mappers\n(Principal, Entity, Scope)"]
        ProvReg["Create ProviderRegistry\n(AzApi, AzureAD, AzureRM, AzureDevOps)"]
        ValFmt["Create ValueFormatterRegistry"]
        IconProv["Create IconProviderRegistry"]
        RendererReg["Create ResourceRendererRegistry (NEW)"]
        Builder["Create ReportModelBuilder"]
        Renderer["Create ReportRenderer (NEW)"]
    end

    subgraph Removed["REMOVED from Composition"]
        TL["TemplateLoader"]
        TR["TemplateResolver"]
        MR["MarkdownRenderer\n(Scriban orchestration)"]
        MM["ResourceModelMapperRegistry\n(ScriptObject enrichment)"]
    end

    AppSvc["ApplicationServices\n(Parser, Builder, Renderer)"]

    Entry --> Options
    Options --> Compose
    Compose --> Mappers
    Mappers --> ProvReg
    ProvReg --> ValFmt
    ProvReg --> IconProv
    ProvReg --> RendererReg
    RendererReg --> Renderer
    Builder --> AppSvc
    Renderer --> AppSvc

    class Entry,Options entryNode
    class Compose,Mappers,ProvReg,ValFmt,IconProv,RendererReg,Builder,Renderer compNode
    class AppSvc serviceNode
    class TL,TR,MR,MM removedNode
```

### 10.2 ApplicationServices Record (Simplified)

```csharp
// Current
internal sealed record ApplicationServices(
    TerraformPlanParser Parser,
    ReportModelBuilder ModelBuilder,
    MarkdownRenderer Renderer,           // Scriban-based
    DiagnosticContext? DiagnosticContext,
    CodeAnalysisInput? CodeAnalysisInput);

// Target
internal sealed record ApplicationServices(
    TerraformPlanParser Parser,
    ReportModelBuilder ModelBuilder,
    ReportRenderer Renderer,             // Pure C# — no Scriban
    DiagnosticContext? DiagnosticContext,
    CodeAnalysisInput? CodeAnalysisInput);
```

---

## 11. Error Handling

### 11.1 Exception Hierarchy (Simplified)

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
classDiagram
    Exception <|-- TerraformPlanParseException
    Exception <|-- MarkdownRenderException
    Exception <|-- CliParseException
    Exception <|-- ServiceRegistrationException

    class Exception {
        <<built-in>>
    }
    class TerraformPlanParseException {
        +string Message
    }
    class MarkdownRenderException {
        +string Message
    }
    class CliParseException {
        +string Message
    }
    class ServiceRegistrationException {
        +string Message
    }

    note for MarkdownRenderException "Now thrown by ReportRenderer\ninstead of Scriban runtime"

    style Exception fill:#8b5cf6,stroke:#a78bfa,stroke-width:2px,color:#ffffff
    style TerraformPlanParseException fill:#ef4444,stroke:#f87171,stroke-width:3px,color:#ffffff
    style MarkdownRenderException fill:#ef4444,stroke:#f87171,stroke-width:3px,color:#ffffff
    style CliParseException fill:#ef4444,stroke:#f87171,stroke-width:3px,color:#ffffff
    style ServiceRegistrationException fill:#ef4444,stroke:#f87171,stroke-width:3px,color:#ffffff
```

**Removed:** `ScribanHelperException` — No longer needed since helper function errors are now regular C# exceptions caught by the rendering pipeline.

**Key improvement:** Template-related runtime errors (missing variables, type mismatches) are eliminated entirely because all rendering is compile-time verified C# code.

---

## 12. Cross-Cutting Concerns

### 12.1 Security

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef secureNode fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff
    classDef dataNode fill:#3b82f6,stroke:#60a5fa,stroke-width:2px,color:#ffffff

    Plan["Terraform Plan\n(with sensitive values)"]
    Builder["ReportModelBuilder\n• Reads before_sensitive / after_sensitive\n• Masks values during model construction\n• No raw secrets in ReportModel"]
    Model["ReportModel\n(already masked)"]
    Renderer["ReportRenderer\n• Consumes masked values directly\n• No sensitivity logic needed\n• No risk of accidental exposure"]
    Output["Markdown Output\n(sensitive values shown as '(sensitive)')"]

    Plan --> Builder
    Builder --> Model
    Model --> Renderer
    Renderer --> Output

    class Builder secureNode
    class Plan,Model,Renderer,Output dataNode
```

**Improvement over current architecture:**

| Concern | Scriban (Current) | Pure C# (Target) |
|---------|-------------------|-------------------|
| Masking location | `AotScriptObjectMapper` (at template boundary) | `ReportModelBuilder` (during model construction) |
| Risk of bypass | Templates can access raw `before_json`/`after_json` | No raw JSON exposed — model values are pre-masked |
| Masking complexity | Recursive `ScriptObject`/`ScriptArray` tree walk | Simple string replacement on model properties |
| ADR-009 overhead | Required recursive masking on dynamic types | Eliminated |
| `after_unknown` handling | Template accesses `change.after_unknown.output` via string keys | C# accesses `AfterUnknownHelper.IsAttributeUnknownAfterApply()` directly |
| Output values sensitivity | `_output_values.sbn` navigates `before_sensitive.output` / `after_sensitive.output` | Renderer reads `BeforeSensitive` / `AfterSensitive` properties directly |

### 12.2 Testing Strategy

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
graph TB
    subgraph " "
        UAT["🧪 UAT\nManual testing in real\nGitHub/Azure DevOps PRs"]
        Snapshot["📸 Snapshot Tests\nGolden files verify\nidentical output after migration"]
        Unit["⚙️ Unit Tests\nDirect testing of IResourceRenderer\nimplementations (no ScriptObject setup)"]
        Arch["🏗️ Architecture Tests\nNetArchTest verifies layer\nboundaries and no Scriban deps"]
    end

    Unit -.-> Snapshot
    Snapshot -.-> UAT
    Arch -.-> Unit

    classDef uatStyle fill:#8b5cf6,stroke:#a78bfa,stroke-width:3px,color:#ffffff
    classDef snapshotStyle fill:#3b82f6,stroke:#60a5fa,stroke-width:3px,color:#ffffff
    classDef unitStyle fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff
    classDef archStyle fill:#f59e0b,stroke:#fbbf24,stroke-width:3px,color:#ffffff

    class UAT uatStyle
    class Snapshot snapshotStyle
    class Unit unitStyle
    class Arch archStyle
```

**Testing simplification:**

- **No more `ScriptObject` construction in tests** — Test renderers by calling `renderer.Render(model, writer, context)` directly
- **Snapshot tests are the migration oracle** — Existing golden files must produce identical output
- **Architecture tests verify Scriban removal** — New rule: no assembly may reference `Scriban`

### 12.3 NativeAOT Impact

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart LR
    classDef currentNode fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff
    classDef targetNode fill:#10b981,stroke:#34d399,stroke-width:3px,color:#ffffff

    subgraph Current["Current NativeAOT Build"]
        C1["TrimmerRootDescriptor.xml\npreserve='all' for Scriban"]
        C2["Scriban assembly\n(~1.5 MB preserved)"]
        C3["AotScriptObjectMapper\n(manual property mapping)"]
        C4["DateTimeOffset preservation\n(for Scriban date functions)"]
    end

    subgraph Target["Target NativeAOT Build"]
        T1["No TrimmerRootDescriptor.xml\n(or minimal: assembly metadata only)"]
        T2["No third-party assemblies"]
        T3["Direct model property access\n(compiler-verified)"]
        T4["Standard .NET date formatting"]
    end

    C1 -.->|"Removed"| T1
    C2 -.->|"Removed"| T2
    C3 -.->|"Removed"| T3
    C4 -.->|"Simplified"| T4

    class C1,C2,C3,C4 currentNode
    class T1,T2,T3,T4 targetNode
```

**Estimated binary size reduction:** ~1.5 MB (Scriban assembly) + ~0.3 MB (trimmer-preserved metadata) = **~1.8 MB smaller NativeAOT binary** (from ~7 MB to ~5.2 MB).

---

## 13. Migration Impact Analysis

### 13.1 Files Affected

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
pie title Files Affected by Migration
    "Deleted (templates, AOT mapper, Scriban infra)" : 40
    "Modified (remove Scriban imports/types)" : 60
    "New (renderers, MarkdownWriter)" : 18
    "Unchanged (models, parsing, CLI)" : 50
```

### 13.2 Migration Phases

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
gantt
    title Migration Phases
    dateFormat X
    axisFormat %s

    section Phase 1: Framework
    Create MarkdownWriter                    :a1, 0, 2
    Create IResourceRenderer                 :a2, 0, 1
    Create ResourceRendererRegistry          :a3, 1, 2
    Create DefaultResourceRenderer           :a4, 2, 4
    Create RenderContext                     :a5, 1, 2
    Rename ScribanHelpers → RenderingHelpers :a6, 2, 4

    section Phase 2: Core Templates
    Convert _header.sbn → HeaderRenderer     :b1, 4, 5
    Convert _summary.sbn → SummaryRenderer   :b2, 4, 5
    Convert _resource.sbn → DefaultResourceRenderer :b3, 4, 6
    Convert _child_resources.sbn → ChildResourceRenderer :b4, 5, 7
    Convert default.sbn → ReportRenderer     :b5, 6, 8
    Convert code_analysis partials           :b6, 6, 8
    Remove AotScriptObjectMapper             :b7, 8, 9
    Validate against snapshot tests          :crit, b8, 8, 9

    section Phase 3: Provider Templates
    Convert azurerm/ templates               :c1, 9, 11
    Convert azuread/ templates               :c2, 9, 11
    Convert azapi/ templates + output values :c3, 10, 13
    Convert azapi ScribanHelpers (15 files)  :c3b, 10, 13
    Convert azuredevops/ templates + partials :c4, 10, 12
    Remove ScriptObject mappers (6 files)    :c4b, 12, 13
    Simplify IProviderModule interface       :c5, 13, 14
    Validate against snapshot tests          :crit, c6, 13, 14

    section Phase 4: Cleanup
    Remove Scriban PackageReference          :d1, 14, 15
    Remove TrimmerRootDescriptor.xml         :d2, 14, 15
    Remove ResourceModelMapperRegistry       :d2b, 14, 15
    Remove ScribanHelpers.Azure.cs           :d2c, 14, 15
    Update architecture docs                :d3, 15, 16
    Update ADR statuses                     :d4, 15, 16
    Add NetArchTest no-Scriban rule          :d4b, 15, 16
    Final snapshot test validation           :crit, d5, 16, 17
```

### 13.3 Lines of Code Impact

| Category | Added | Removed | Net |
|----------|------:|--------:|----:|
| `MarkdownWriter` + `RenderContext` | ~200 | 0 | +200 |
| `ReportRenderer` (replaces `MarkdownRenderer`) | ~300 | ~542 | -242 |
| `DefaultResourceRenderer` (replaces `_resource.sbn`) | ~150 | ~100 | +50 |
| Provider-specific renderers (replace `.sbn` templates) | ~800 | ~1,550 | -750 |
| `ResourceRendererRegistry` (replaces `TemplateResolver`) | ~50 | ~264 | -214 |
| `AotScriptObjectMapper` removal | 0 | ~692 | -692 |
| `ScribanHelperException` removal | 0 | ~30 | -30 |
| Core ScribanHelpers → RenderingHelpers (remove Scriban glue) | 0 | ~200 | -200 |
| AzApi ScribanHelpers → RenderingHelpers (remove Scriban glue) | 0 | ~800 | -800 |
| Provider `ScriptObject` mappers removal | 0 | ~957 | -957 |
| Helper registration code removal (`Registry.cs`) | 0 | ~65 | -65 |
| `TrimmerRootDescriptor.xml` removal | 0 | ~12 | -12 |
| `ResourceModelMapperRegistry` + `IResourceModelMapper` removal | 0 | ~80 | -80 |
| `ScribanHelpers.Azure.cs` (merged into platform-independent helpers) | 0 | ~50 | -50 |
| Test file updates | ~500 | ~2,000 | -1,500 |
| **Total estimated** | **~2,000** | **~7,342** | **~-5,342** |

**Net reduction: ~5,300 lines of code removed.**

---

## 14. Quality Attributes

### 14.1 Comparison Matrix

| Quality Attribute | Scriban (Current) | Pure C# (Target) | Change |
|-------------------|-------------------|-------------------|--------|
| **Compile-time safety** | ❌ Template variable typos are silent | ✅ All property access verified | ⬆️ Better |
| **Binary size** | ~7 MB (NativeAOT) | ~5.2 MB (estimated) | ⬆️ Smaller |
| **Startup time** | < 1 sec | < 1 sec (slightly faster) | ➡️ Same |
| **Third-party deps** | 1 (Scriban) | 0 | ⬆️ Better |
| **IDE support** | ❌ Template variables are opaque strings | ✅ Full Go to Definition, Find References | ⬆️ Better |
| **Rendering performance** | Good (Scriban interprets templates) | Better (compiled C# methods) | ⬆️ Better |
| **Layout readability** | ✅ `.sbn` files visually represent output | ⚠️ C# `StringBuilder` is less visual | ⬇️ Slightly worse |
| **Extension complexity** | Medium (create `.sbn` + mapper) | Low (implement `IResourceRenderer`) | ⬆️ Better |
| **Test setup** | Complex (`ScriptObject` construction) | Simple (call method with model) | ⬆️ Better |
| **Security surface** | Templates can access raw state | Renderers only see masked model | ⬆️ Better |

### 14.2 Architecture Boundary Enforcement

```mermaid
%%{init: {'theme':'dark', 'themeVariables': { 'fontSize':'14px', 'fontFamily':'ui-sans-serif, system-ui, sans-serif'}}}%%
flowchart TD
    classDef allowed fill:#10b981,stroke:#34d399,stroke-width:2px,color:#ffffff
    classDef forbidden fill:#ef4444,stroke:#f87171,stroke-width:2px,color:#ffffff

    subgraph Rules["Architecture Test Rules (NetArchTest)"]
        R1["✅ Providers → Rendering interfaces\n(IResourceRenderer, MarkdownWriter)"]
        R2["✅ Rendering → RenderTargets\n(IDiffFormatter)"]
        R3["✅ Providers → Platforms\n(Azure utilities)"]
        R4["❌ Rendering → Providers\n(no direct dependency)"]
        R5["❌ Parsing → Rendering\n(no direct dependency)"]
        R6["❌ Any assembly → Scriban\n(NEW: zero tolerance)"]
    end

    class R1,R2,R3 allowed
    class R4,R5,R6 forbidden
```

The architecture test suite should add a new rule:

```csharp
// No assembly may reference Scriban after migration
Types.InAssembly(assembly)
    .ShouldNot()
    .HaveDependencyOn("Scriban")
    .GetResult()
    .IsSuccessful
    .Should().BeTrue();
```

---

## 15. Glossary

| Term | Definition |
|------|------------|
| **MarkdownWriter** | New fluent API for constructing well-formed Markdown output in C# |
| **IResourceRenderer** | Interface implemented by provider-specific renderers for Terraform resource types |
| **ResourceRendererRegistry** | Registry that maps Terraform resource types to `IResourceRenderer` implementations |
| **DefaultResourceRenderer** | Fallback renderer for resource types without a provider-specific renderer |
| **RenderContext** | Immutable record carrying shared rendering state (diff formatter, registries, options) |
| **ReportRenderer** | Top-level orchestrator that renders a complete `ReportModel` to Markdown |
| **RenderingHelpers** | Renamed from `ScribanHelpers`; static utility methods for formatting, escaping, and diff computation |
| **AfterUnknownHelper** | Utility class for navigating Terraform's `after_unknown` JSON tree (Scriban-independent, retained as-is) |
| **AotScriptObjectMapper** | Current bridge between C# models and Scriban's `ScriptObject` trees (removed in target architecture) |
| **ResourceModelMapper** | Current interface for provider-specific `ScriptObject` enrichment (removed in target architecture) |
| **NativeAOT** | .NET ahead-of-time compilation producing self-contained native binary |
| **Pure DI** | Dependency injection without a container; services wired explicitly in `CompositionRoot` |
| **Snapshot Tests** | Golden file tests that verify markdown output matches expected baseline |
| **TrimmerRootDescriptor.xml** | NativeAOT configuration that preserves Scriban assembly from trimming (removed in target architecture) |

---

## Appendix A: References

- [ADR-010: Evaluate Removing Scriban](../../../docs/adr-010-scriban-removal-evaluation.md) — Decision record for this architecture change
- [ADR-001: Use Scriban for Markdown Templating](../../../docs/adr-001-scriban-templating.md) — Original decision (superseded by ADR-010)
- [ADR-005: Scriban Template Loop Limit](../../../docs/adr-005-scriban-template-loop-limit.md) — Superseded by removal
- [ADR-006: Pure Dependency Injection](../../../docs/adr-006-dependency-injection.md) — Retained in target architecture
- [ADR-007: Architecture Boundary Enforcement](../../../docs/adr-007-architecture-boundary-enforcement.md) — Strengthened in target architecture
- [ADR-009: Template JSON Sensitivity Masking](../../../docs/adr-009-template-json-sensitivity-masking.md) — Simplified in target architecture
- [Current Architecture](../../../docs/architecture.md) — Existing arc42 documentation (Scriban-based)
