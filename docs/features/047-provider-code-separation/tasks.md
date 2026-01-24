# Tasks: Provider Code Separation

## Overview

This feature restructures the codebase to clearly separate provider-specific (Terraform providers) and platform-specific (GitHub vs Azure DevOps) code into dedicated folders and namespaces. This improves discoverability, modularity, and maintainability while preserving Native AOT compatibility and existing functionality. 

Reference: [specification.md](specification.md) and [architecture.md](architecture.md).

## Tasks

### Task 1: Define Provider Registration & Core Infrastructure

**Priority:** High

**Status:** ✅ Complete

**Description:**
Establish the foundation for explicit provider registration and multi-prefix template loading.

**Acceptance Criteria:**
- [x] `IProviderModule` interface created in `Oocx.TfPlan2Md.Providers`.
- [x] `ProviderRegistry` class created for managing explicit registration (no reflection).
- [x] `TemplateLoader` updated to support multiple embedded resource prefixes (checking Core first, then Provider locations).
- [x] `ReportModelBuilder` refactored to use `ProviderRegistry` for helper registration and factory lookup.
- [x] Unit test verifies `TemplateLoader` can load from multiple prefixes.

**Dependencies:** None

---

### Task 2: Implement RenderTargets and Diff Formatter Dispatching

**Priority:** High

**Status:** ✅ Complete

**Description:**
Extract platform-specific rendering logic (GitHub vs Azure DevOps) from generic helpers into a dedicated `RenderTargets` structure.

**Acceptance Criteria:**
- [x] `IDiffFormatter` interface created in `Oocx.TfPlan2Md.RenderTargets`.
- [x] `GitHubDiffFormatter` implemented (extracting "Simple Diff" logic).
- [x] `AzureDevOpsDiffFormatter` implemented (extracting "Inline Diff" logic).
- [x] `ScribanHelpers.DiffFormatting.cs` refactored to delegate formatting to the injected `IDiffFormatter`.
- [x] TC-04, TC-05, and TC-11 from the test plan are satisfied.

**Dependencies:** None

---

### Task 3: Update CLI for `--render-target`

**Priority:** High

**Status:** ✅ Complete

**Description:**
Introduce the `--render-target` flag and remove the `--large-value-format` flag as part of the CLI cleanup.

**Acceptance Criteria:**
- [x] `CliOptions` includes `RenderTarget` property with values `github`, `azuredevops` (alias `azdo`).
- [x] `CliOptions` no longer contains `LargeValueFormat`.
- [x] `CliParser` correctly maps `--render-target` and handles error for deprecated `--large-value-format`.
- [x] Selected `IDiffFormatter` is correctly injected into the rendering pipeline based on CLI choice.
- [x] TC-12 from the test plan is satisfied.

**Dependencies:** Task 2

---

### Task 4: Restructure Shared Platform Utilities

**Priority:** Medium

**Status:** ✅ Complete

**Description:**
Move generic cloud platform utilities (Azure-specific but provider-agnostic) to a new `Platforms/` folder.

**Acceptance Criteria:**
- [x] `src/Oocx.TfPlan2Md/Azure/` moved to `src/Oocx.TfPlan2Md/Platforms/Azure/`.
- [x] Namespaces updated to `Oocx.TfPlan2Md.Platforms.Azure`.
- [x] All code references updated and compilation succeeds.
- [x] TC-02 (partially) satisfied.

**Dependencies:** None

---

### Task 5: Migrate AzApi Provider

**Priority:** Medium

**Status:** ✅ Complete

**Description:**
Consolidate all AzApi-specific logic and templates into the `Providers/AzApi/` folder.

**Acceptance Criteria:**
- [x] Folder `src/Oocx.TfPlan2Md/Providers/AzApi/` created.
- [x] Templates moved from `MarkdownGeneration/Templates/azapi/` (and resource files updated).
- [x] Helpers moved from `MarkdownGeneration/Helpers/ScribanHelpers.AzApi.*.cs`.
- [x] `AzApiModule` implemented and registered in `ProviderRegistry`.
- [x] Namespaces updated to `Oocx.TfPlan2Md.Providers.AzApi`.

**Dependencies:** Task 1, Task 4

---

### Task 6: Migrate AzureRM Provider

**Priority:** Medium

**Status:** ✅ Complete

**Description:**
Consolidate all AzureRM-specific logic and templates into the `Providers/AzureRM/` folder.

**Acceptance Criteria:**
- [x] Folder `src/Oocx.TfPlan2Md/Providers/AzureRM/` created.
- [x] Templates moved from `MarkdownGeneration/Templates/azurerm/`.
- [x] Resource view models and factories moved from `MarkdownGeneration/Models/` (e.g., `FirewallNetworkRuleCollectionViewModel.cs`).
- [x] `AzureRMModule` implemented and registered in `ProviderRegistry`.
- [x] Namespaces updated to `Oocx.TfPlan2Md.Providers.AzureRM`.

**Dependencies:** Task 1, Task 4

---

### Task 7: Migrate AzureDevOps Provider

**Priority:** Medium

**Status:** ✅ Complete

**Description:**
Consolidate all AzureDevOps-specific logic and templates into the `Providers/AzureDevOps/` folder.

**Acceptance Criteria:**
- [x] Folder `src/Oocx.TfPlan2Md/Providers/AzureDevOps/` created.
- [x] Templates moved from `MarkdownGeneration/Templates/azuredevops/`.
- [x] VariableGroup view models moved from `MarkdownGeneration/Models/`.
- [x] `AzureDevOpsModule` implemented and registered in `ProviderRegistry`.
- [x] Namespaces updated to `Oocx.TfPlan2Md.Providers.AzureDevOps`.

**Dependencies:** Task 1

**Acceptance Criteria:**
- [ ] Folder `src/Oocx.TfPlan2Md/Providers/AzureDevOps/` created.
- [ ] Templates moved from `MarkdownGeneration/Templates/azuredevops/`.
- [ ] VariableGroup view models moved from `MarkdownGeneration/Models/`.
- [ ] `AzureDevOpsModule` implemented and registered in `ProviderRegistry`.
- [ ] Namespaces updated to `Oocx.TfPlan2Md.Providers.AzureDevOps`.

**Dependencies:** Task 1

---

### Task 8: Cleanup and Test Suite Alignment

**Priority:** Medium

**Description:**
Clean up the remaining core folders and align the test suite structure with the main project structure.

**Acceptance Criteria:**
- [ ] Empty/obsolete folders in `MarkdownGeneration/` removed.
- [ ] TUnit tests for providers moved to matching folder structure in `tests/`.
- [ ] `InternalsVisibleTo` attributes updated if necessary for new namespaces.
- [ ] Full regression suite (TC-08) passes.
- [ ] Snapshot comparison (TC-09) passes with NO changes to existing snapshots.

**Dependencies:** Task 5, Task 6, Task 7

---

### Task 9: AOT Validation and Documentation

**Priority:** Medium

**Description:**
Verify Native AOT compatibility and finalize developer documentation.

**Acceptance Criteria:**
- [ ] Native AOT publish succeeds without new trimming warnings (TC-10).
- [ ] `docs/architecture.md` updated with the new structure.
- [ ] `Providers/README.md` created with guidance on adding new providers.
- [ ] Developer onboarding documentation updated.

**Dependencies:** Task 8

## Implementation Order

1. **Foundational Bridge (Tasks 1, 2, 3)**: Establish the new registration and dispatching mechanisms while everything still works in its old place.
2. **Utilities Move (Task 4)**: Move the shared Azure utilities as they are relied upon by multiple providers.
3. **Provider Migration (Tasks 5, 6, 7)**: Migrate each provider one by one. This is the bulk of the work.
4. **Consolidation & Verification (Tasks 8, 9)**: Clean up, fix test paths, and verify end-to-end.

## Open Questions

- Should we also move the templates for the `default` report and `_resource` fallback? 
  - *Recommendation: No, keep them in `MarkdownGeneration/Templates/` as they are considered "Core" components.*
- Should the `Platforms/Azure` folder be a separate project?
  - *Recommendation: No, per specification, keep everything in a single assembly for now.*
