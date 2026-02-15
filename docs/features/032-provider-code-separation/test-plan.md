# Test Plan: Provider Code Separation

## Overview

This test plan covers the structural reorganization of the codebase to separate provider-specific and platform-specific code. The goal is to ensure that while the code is rearranged, all existing functionality remains identical and the application remains compatible with Native AOT and trimming.

Reference: [specification.md](specification.md)

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Provider Separation | TC-01, TC-02, TC-03 | Unit / Integration |
| Platform (RenderTarget) Separation | TC-04, TC-05, TC-11 | Unit |
| Namespace Alignment | TC-02, TC-05 | Unit |
| Explicit Registration| TC-06, TC-07 | Unit |
| Behavioral Preservation| TC-08, TC-09 | Regression |
| AOT & Trimming Support| TC-10 | Integration |
| CLI `--render-target` | TC-12, TC-13 | Unit / Integration |

## User Acceptance Scenarios

> **Purpose**: Verify that moving templates and logic hasn't introduced rendering regressions on real platforms.

### Scenario 1: Multi-Provider Report Rendering

**User Goal**: Generate a report containing resources from multiple providers (azurerm, azapi, azuredevops) and verify it renders correctly in both GitHub and Azure DevOps.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description/comments.

**Expected Output**:
- Summary table shows counts for all 3 providers.
- Custom templates for `azurerm_network_security_group`, `azapi_resource`, and `azuredevops_variable_group` are applied correctly.
- Principal mapping (Azure feature) still works for azurerm/azapi resources.

**Success Criteria**:
- [ ] Report renders successfully without `MarkdownRenderException`.
- [ ] Custom templates are found and used (not falling back to `_resource.sbn`).
- [ ] Icons and semantic formatting are preserved.

---

### Scenario 2: Platform-Specific Artifact Generation

**User Goal**: Ensure that using the `--output-platform` flag (if applicable, even as a placeholder) doesn't cause errors and respects the new folder structure.

**Expected Output**:
- The command executes successfully for both `github` and `azuredevops`.

---

## Test Cases

### TC-01: Provider Folder Structure

**Type:** Unit

**Description:**
Verify that all provider-specific files have been moved to their respective folders under `src/Oocx.TfPlan2Md/Providers/`.

**Preconditions:**
- Refactoring complete.

**Test Steps:**
1. Inspect `src/Oocx.TfPlan2Md/Providers/AzureRM/` for templates and helpers.
2. Inspect `src/Oocx.TfPlan2Md/Providers/AzApi/` for templates and helpers.
3. Inspect `src/Oocx.TfPlan2Md/Providers/AzureDevOps/` for templates and helpers.

**Expected Result:**
Each folder contains the expected templates, helpers, and registration code.

---

### TC-02: Namespace Alignment Verification

**Type:** Unit

**Description:**
Verify that the namespaces of the moved classes match their new folder structure.

**Test Steps:**
1. Check namespace of classes in `Providers/AzureRM/` (should be `Oocx.TfPlan2Md.Providers.AzureRM`).
2. Check namespace of classes in `Platforms/Azure/` (should be `Oocx.TfPlan2Md.Platforms.Azure`).

**Expected Result:**
All namespaces are aligned with folders.

---

### TC-03: Provider Template Loading

**Type:** Integration

**Description:**
Verify that `ScribanTemplateLoader` accurately finds templates in their new provider-specific embedded resource locations.

**Test Steps:**
1. Use `ScribanTemplateLoader` to load a core template (e.g., `default`).
2. Use `ScribanTemplateLoader` to load a provider template (e.g., `azurerm/role_assignment`).

**Expected Result:**
Both templates are loaded successfully.

---

### TC-04: Platform/RenderTarget Folder Structure

**Type:** Unit

**Description:**
Verify that platform-specific code (GitHub/AzureDevOps) is moved to `src/Oocx.TfPlan2Md/RenderTargets/`.

**Expected Result:**
- `RenderTargets/GitHub/GitHubDiffFormatter.cs` exists.
- `RenderTargets/AzureDevOps/AzureDevOpsDiffFormatter.cs` exists.
- `RenderTargets/IDiffFormatter.cs` exists.

---

### TC-05: Platform Namespace Alignment

**Type:** Unit

**Description:**
Verify that the namespaces of the moved classes match their new folder structure.

**Expected Result:**
- `Oocx.TfPlan2Md.RenderTargets.GitHub` for GitHub classes.
- `Oocx.TfPlan2Md.RenderTargets.AzureDevOps` for Azure DevOps classes.

---

### TC-06: Explicit Registration (No Reflection)

**Type:** Unit

**Description:**
Verify that providers are registered explicitly without using reflection.

**Test Steps:**
1. Inspect `ProviderRegistry.cs` and module implementations.
2. Ensure no use of `Assembly.GetTypes()` or similar reflection-based discovery for providers.

---

### TC-07: Resource Factory Registration Works

**Type:** Unit

**Description:**
Verify that `IProviderModule` correctly registers resource view model factories.

**Test Steps:**
1. Call registration logic.
2. Verify that a factory for a provider-specific resource (e.g., `azurerm_role_assignment`) is available in the registry.

---

### TC-08: Full Regression Suite Pass

**Type:** Regression

**Description:**
Run the entire TUnit test suite to ensure no functional regressions.

**Command:**
`scripts/test-with-timeout.sh -- dotnet test --solution src/tfplan2md.slnx`

---

### TC-09: Snapshot Comparison

**Type:** Regression

**Description:**
Verify that the generated markdown for comprehensive demos is identical to the current snapshots.

**Command:**
`dotnet test --project src/tests/Oocx.TfPlan2Md.TUnit/ --treenode-filter /*/*/MarkdownSnapshotTests/*`

---

### TC-10: Native AOT Build & Run

**Type:** Integration

**Description:**
Verify that the application can still be compiled as a Native AOT executable and run successfully.

**Test Steps:**
1. Publish the project with `-r linux-x64 -c Release /p:PublishAot=true`.
2. Run the resulting binary with a sample plan.

**Expected Result:**
No trimming warnings during build, and binary produces correct output.

---

### TC-11: Diff Formatter Dispatching

**Type:** Unit

**Description:**
Verify that the correct `IDiffFormatter` implementation is selected based on the specified render target.

**Test Steps:**
1. Call the component responsible for choosing the formatter with `github`.
2. Call with `azuredevops` (or `azdo`).

**Expected Result:**
- Returns `GitHubDiffFormatter` for `github`.
- Returns `AzureDevOpsDiffFormatter` for `azuredevops`.

---

### TC-12: CLI `--render-target` Argument Parsing

**Type:** Unit

**Description:**
Verify that the CLI correctly parses the `--render-target` option.

**Test Steps:**
1. Run `CliParser` with `--render-target github`.
2. Run `CliParser` with `--render-target azuredevops`.

**Expected Result:**
Option is correctly mapped to the internal `RenderTarget` enum or equivalent.

---

### TC-13: CLI Backward Compatibility

**Type:** Unit

**Description:**
Verify that `--large-value-format` still works as expected (possibly as a hidden alias).

**Test Steps:**
1. Run `CliParser` with `--large-value-format simple-diff`.
2. Run `CliParser` with `--large-value-format inline-diff`.

**Expected Result:**
Maps to `github` and `azuredevops` render targets respectively.

## Test Data Requirements

- Existing `azurerm-azuredevops-plan.json`
- Existing `azapi-update-plan.json`
- Existing `comprehensive-demo.md` snapshots

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| Resource type with unknown provider | Falls back to generic `_resource.sbn` | TC-03 (Fallback) |
| Multiple providers registered | All templates/helpers are available | TC-07 |

## Non-Functional Tests

- **Binary Size**: Monitor if AOT binary size changes significantly (though not a hard requirement, it's good for AOT health).

## Open Questions

1. Should we also move the TUnit tests for providers into matching folder structures? (Recommendation: Yes, for consistency).
