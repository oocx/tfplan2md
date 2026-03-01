# Tasks: Remove Scriban and Replace with Pure C# Rendering

## Overview

Remove the Scriban template engine and replace all `.sbn` templates with pure C# rendering. This refactoring eliminates the sole third-party dependency, improves performance, and enables compile-time safety for all rendering logic.

## Tasks

### Task 1: Core Rendering Infrastructure

**Priority:** High

**Description:**
Implement the foundational classes for C# rendering as defined in the architecture. This includes the `MarkdownWriter` for stream-based markdown generation and the `IResourceRenderer` interface.

**Acceptance Criteria:**
- [x] `MarkdownWriter` implemented with support for common markdown elements (headings, tables, code blocks, lists).
- [x] `IResourceRenderer` and `IRenderContext` interfaces defined.
- [x] `ResourceRendererRegistry` implemented for dispatching resource-specific renderers.
- [x] `RenderContext` implemented to carry global state (options, icons, formatters) through the rendering tree.
- [x] Unit tests for `MarkdownWriter` (TC-MW-01 to TC-MW-10) pass.

**Dependencies:** None

---

### Task 2: Port Global Renderers and Core Helpers

**Priority:** High

**Description:**
Port the top-level report structure renderers and move common logic from `ScribanHelpers/` to regular C# utility classes.

**Acceptance Criteria:**
- [x] `ReportRenderer`, `HeaderRenderer`, and `SummaryRenderer` implemented.
- [x] `DefaultResourceRenderer` implemented as the fallback for unmapped resources.
- [x] Core logic from `ScribanHelpers/` (Diff computation, Large values, etc.) decoupled from Scriban types (`ScriptObject`).
- [x] `MarkdownRenderer` refactored to use the new C# rendering pipeline instead of Scriban.
- [x] Unit tests for new renderers pass (TC-RR, TC-HR, TC-SR, TC-DR).

**Dependencies:** Task 1

---

### Task 3: Port Provider-Specific Renderers

**Priority:** Medium

**Description:**
Implement `IResourceRenderer` for all specialized resource types currently handled by `.sbn` templates across all providers (AzureRM, AzApi, AzureAD, AzureDevOps).

**Acceptance Criteria:**
- [x] AzureRM renderers: `RoleAssignmentRenderer`, `NsgRenderer`, `FirewallNetworkRuleRenderer`, `FirewallAppRuleRenderer`.
- [x] AzApi renderers: `AzApiResourceRenderer`, `AzApiUpdateResourceRenderer`, `AzApiOutputValuesRenderer`.
- [x] AzureAD renderers: `UserRenderer`, `GroupRenderer`, `ServicePrincipalRenderer`, etc.
- [x] AzureDevOps renderers: `VariableGroupRenderer`, `BuildDefinitionRenderer`.
- [x] All unit tests for specific renderers pass (TC-ARM, TC-API, TC-AD, TC-ADO).

**Dependencies:** Task 2

---

### Task 4: Removal of Scriban Infrastructure and Cleanup

**Priority:** Medium

**Description:**
Remove the Scriban NuGet package and all associated glue code that is no longer needed.

**Acceptance Criteria:**
- [x] `Scriban` NuGet package removed from `Oocx.TfPlan2Md.csproj`.
- [x] All 27 `.sbn` files deleted.
- [x] `AotScriptObjectMapper`, `TemplateLoader`, `TemplateResolver`, and `ScribanHelperException` deleted.
- [x] `TrimmerRootDescriptor.xml` cleaned of Scriban entries.
- [x] All `using Scriban;` imports removed from the project.
- [x] Project builds successfully in all configurations (including NativeAOT).

**Dependencies:** Task 3

---

### Task 5: Verification and Final Polish

**Priority:** High

**Description:**
Ensure that all existing snapshot tests pass and that the new architecture meets all structural requirements.

**Acceptance Criteria:**
- [x] All existing snapshot tests in `TestData/Snapshots/` pass without modification (TC-S05).
- [x] Structural architecture tests (TC-S01, TC-S04, TC-S07, TC-S08, TC-S09) pass.
- [x] `docs/features.md` updated to reflect the new rendering architecture.

**Dependencies:** Task 4

## Implementation Order

1. **Foundation (Task 1)**: Establish the basic "writing" API.
2. **Orchestration (Task 2)**: Connect the top-level report model to the new writer.
3. **Specialization (Task 3)**: Port all the actual rendering logic resource-by-resource.
4. **Cleanup (Task 4)**: Once everything is ported, pull the plug on Scriban.
5. **Verification (Task 5)**: Final audit and documentation.

## Open Questions

- Should we keep the `MarkdownRenderer` name or rename it to `ReportRenderer` to avoid confusion with the new component roles? (Recommended: Keep for now to minimize changes in `CompositionRoot`, but mark as candidate for cleanup).
