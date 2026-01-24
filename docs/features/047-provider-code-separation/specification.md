# Feature: Provider Code Separation

## Overview

Currently, provider-specific code (for Terraform providers like azurerm, azapi, azuredevops) and output platform-specific code (GitHub PR vs Azure DevOps PR rendering) are mixed with core, generic code throughout the codebase. This makes it difficult for developers to:
- Identify which code is provider/platform-specific vs. generic
- Understand the boundaries and dependencies between components
- Add support for new providers without touching unrelated code

This feature restructures the codebase to clearly separate provider-specific and platform-specific code into dedicated folders with matching namespaces, while keeping all code in a single assembly.

## User Goals

**Target Users**: Developers working on the tfplan2md codebase (maintainers and contributors)

**Goals**:
- Quickly identify which code/files belong to which provider or output platform
- Easily add support for new Terraform providers by following a clear pattern
- Understand code organization without navigating through mixed concerns
- Maintain AOT compilation and trimming compatibility

## Scope

### In Scope

**Provider Separation (Terraform Providers)**:
- Create dedicated folders for each Terraform provider: azurerm, azapi, azuredevops
- Each provider folder contains:
  - Templates (Scriban .sbn files)
  - Scriban helper functions specific to that provider
  - View model factories for provider-specific resources
  - Data/services (e.g., role definition mappers, principal mappers)
  - Icons or other provider-specific assets
- Namespaces match folder structure (e.g., `Oocx.TfPlan2Md.Providers.AzureRM`)
- Explicit registration of providers in core startup code (no reflection)

**Output Platform Separation** (GitHub vs Azure DevOps):
- Create dedicated folders for output platform-specific rendering logic
- Separate GitHub and Azure DevOps markdown formatting code
- Namespaces match folder structure (e.g., `Oocx.TfPlan2Md.OutputPlatforms.GitHub`)
- Explicit registration approach

**Core/Shared Code** (remains in current location or dedicated "Core" area):
- Generic diff computation and formatting
- JSON parsing utilities
- Base interfaces (`IResourceViewModelFactory`, `IMetadataProvider`, etc.)
- Template loading infrastructure
- General Scriban helpers (markdown escaping, code formatting, etc.)
- Report model building
- Resource type parsing

**Documentation**:
- Update developer documentation to explain the new structure
- Provide clear guidance on adding new providers
- Document registration requirements

### Out of Scope

- Creating separate assemblies or NuGet packages per provider
- Dynamic plugin system or runtime provider loading
- Using reflection for provider discovery (must remain AOT-compatible)
- Changing the public API or CLI interface
- Modifying existing template functionality (only moving files)
- Performance optimizations beyond structural improvements

## User Experience

### For Developers Working with Providers

**Before**: 
- Provider-specific code scattered across multiple folders
- Unclear which files belong to which provider
- Hard to identify all components needed for a provider

**After**:
- Navigate to `src/Oocx.TfPlan2Md/Providers/AzureRM/` to see all azurerm-specific code
- Clear folder structure shows what components exist for each provider
- Adding a new provider means creating a new folder with the standard components

### For Developers Working with Output Platforms

**Before**:
- GitHub and Azure DevOps rendering logic mixed together
- Unclear which code affects which platform

**After**:
- Navigate to `src/Oocx.TfPlan2Md/OutputPlatforms/GitHub/` for GitHub-specific code
- Clear separation between platforms
- Easy to identify platform-specific behavior

### Adding a New Provider

Developer workflow:
1. Create new folder: `src/Oocx.TfPlan2Md/Providers/[ProviderName]/`
2. Add required components:
   - Templates folder with .sbn files
   - Scriban helpers class (if needed)
   - View model factories (if needed)
   - Data/services (if needed)
3. Register components in core registration code
4. Provider is now fully integrated

## Success Criteria

- [ ] All azurerm-specific code moved to `Providers/AzureRM/` folder
- [ ] All azapi-specific code moved to `Providers/AzApi/` folder
- [ ] All azuredevops-specific code moved to `Providers/AzureDevOps/` folder
- [ ] GitHub-specific rendering code moved to `OutputPlatforms/GitHub/` folder
- [ ] Azure DevOps-specific rendering code moved to `OutputPlatforms/AzureDevOps/` folder
- [ ] Namespaces match folder structure consistently
- [ ] Core/shared code clearly separated from provider-specific code
- [ ] Explicit registration system implemented (no reflection)
- [ ] All existing tests pass after restructuring
- [ ] All existing functionality works identically
- [ ] AOT compilation and trimming still work correctly
- [ ] Developer documentation updated with new structure
- [ ] Clear guidance provided for adding new providers

## Open Questions

None at this time - all requirements have been clarified.
