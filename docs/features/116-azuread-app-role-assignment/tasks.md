# Tasks: Azure AD App Role Assignment Support

## Overview

This document breaks down the implementation of Feature 116 — `azuread_app_role_assignment` support — into ordered, independently testable tasks. The feature adds human-readable summary display and GUID-to-name resolution for Azure AD app role assignments in Terraform plan reports.

**Specification:** `docs/features/116-azuread-app-role-assignment/specification.md`
**Architecture:** `docs/features/116-azuread-app-role-assignment/architecture.md`
**Test Plan:** `docs/features/116-azuread-app-role-assignment/test-plan.md`

## Tasks

### Task 1: Embedded JSON Data File and Build Configuration

**Priority:** High

**Description:**
Create the `MicrosoftGraphAppRoles.json` embedded data file containing Microsoft Graph application permission GUID-to-name mappings, and wire it into the build system so the source generator produces the `EmbeddedJsonResources.MicrosoftGraphAppRoles` class. Also add the `EmbeddedJsonPayloads` accessor and the AOT-safe JSON serialization context.

**Files to Create:**
- `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoles.json` — GUID→name mapping (comprehensive list sourced from Microsoft Graph service principal, `{ "guid": "PermissionName" }` format, matching `AzureRoleDefinitions.json` style)
- `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesJsonContext.cs` — `[JsonSerializable(typeof(Dictionary<string, string>))]` context class (pattern: `AzureRoleDefinitionsJsonContext.cs`)

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Oocx.TfPlan2Md.csproj` — Add `<AdditionalFiles Include="Platforms/Azure/MicrosoftGraphAppRoles.json" EmbedAsJson="true" />` to the existing `AdditionalFiles` ItemGroup
- `src/Oocx.TfPlan2Md/EmbeddedJsonPayloads.cs` — Add `MicrosoftGraphAppRoles` property returning `global::EmbeddedJsonResources.MicrosoftGraphAppRoles.GetBytes()`

**Acceptance Criteria:**
- [ ] `MicrosoftGraphAppRoles.json` exists with comprehensive Microsoft Graph app role mappings (GUID→permission name)
- [ ] JSON file includes commonly used permissions (at minimum: `User.Read.All`, `Group.Read.All`, `Directory.Read.All`, `Directory.ReadWrite.All`, `User.ReadWrite.All`, `Group.ReadWrite.All`, `AppRoleAssignment.ReadWrite.All`, `Application.ReadWrite.All`, `Application.Read.All`)
- [ ] `MicrosoftGraphAppRolesJsonContext` is defined with `[JsonSerializable(typeof(Dictionary<string, string>))]`
- [ ] `.csproj` contains the `AdditionalFiles` entry with `EmbedAsJson="true"`
- [ ] `EmbeddedJsonPayloads.MicrosoftGraphAppRoles` property compiles and returns the embedded JSON bytes
- [ ] Project builds successfully with `dotnet build`

**Dependencies:** None

**Notes:**
The full list of Microsoft Graph app roles can be sourced via `az ad sp show --id 00000003-0000-0000-c000-000000000000 --query "appRoles[].{id:id, value:value}" -o json`. Follow the same comprehensiveness as `AzureRoleDefinitions.json` (~475 entries). The JSON file has no runtime cost thanks to the frozen dictionary pattern.

---

### Task 2: App Role Resolver Interface, Implementation, and Registry

**Priority:** High

**Description:**
Create the `IAppRoleResolver` interface and its `MicrosoftGraphAppRoleResolver` implementation using the frozen dictionary pattern established by `IRoleDefinitionResolver`/`AzureRoleDefinitionResolver`. Also create the `MicrosoftGraphAppRolesRegistry` that loads the embedded JSON into a `FrozenDictionary`. Write unit tests for the resolver (TC-01 through TC-04).

**Files to Create:**
- `src/Oocx.TfPlan2Md/Platforms/Azure/IAppRoleResolver.cs` — Interface with `GetAppRole(string? appRoleId)` returning `RoleDefinitionInfo` and `GetAppRoleName(string? appRoleId)` returning `string`
- `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRoleResolver.cs` — Implementation using `FrozenDictionary<string, string>` with `OrdinalIgnoreCase` comparer; includes static `CreateBuiltIn()` factory method
- `src/Oocx.TfPlan2Md/Platforms/Azure/MicrosoftGraphAppRolesRegistry.cs` — Static class with `Load()` method returning `FrozenDictionary<string, string>` from `EmbeddedJsonPayloads.MicrosoftGraphAppRoles`
- `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/MicrosoftGraphAppRoleResolverTests.cs` — Unit tests (TC-01 through TC-04)

**Acceptance Criteria:**
- [ ] `IAppRoleResolver` interface defined with `GetAppRole()` and `GetAppRoleName()` methods
- [ ] `MicrosoftGraphAppRoleResolver` resolves known GUIDs to `RoleDefinitionInfo(name, guid, "name (guid)")` (TC-01)
- [ ] Unknown GUIDs return `RoleDefinitionInfo(guid, guid, guid)` — raw GUID as all fields (TC-02)
- [ ] Null/empty/whitespace inputs handled gracefully without exception (TC-03)
- [ ] GUID matching is case-insensitive using `OrdinalIgnoreCase` comparer (TC-04)
- [ ] `CreateBuiltIn()` factory returns a working resolver instance
- [ ] `MicrosoftGraphAppRolesRegistry.Load()` returns a non-empty `FrozenDictionary`
- [ ] Reuses existing `RoleDefinitionInfo` record (no new result type created)
- [ ] All 4 unit tests pass

**Dependencies:** Task 1

**Notes:**
This resolver is simpler than `AzureRoleDefinitionResolver`: no custom-role merging, no `/subscriptions/.../roleDefinitions/` GUID extraction. Microsoft Graph app role IDs are bare GUIDs that are globally stable across tenants.

---

### Task 3: AppRoleIdFormatter Value Formatter

**Priority:** High

**Description:**
Create the `AppRoleIdFormatter` that formats `app_role_id` GUID values in detail tables using the app role resolver. Write unit tests (TC-05 through TC-08).

**Files to Create:**
- `src/Oocx.TfPlan2Md/Platforms/Azure/AppRoleIdFormatter.cs` — Implements `IValueFormatter`; uses `IAppRoleResolver` to resolve GUIDs, formats as `` `🔑 {Name} ({GUID})` `` using `MarkdownHelpers.FormatCodeTable()` (pattern: `RoleDefinitionFormatter`)
- `src/tests/Oocx.TfPlan2Md.TUnit/Platforms/Azure/AppRoleIdFormatterTests.cs` — Unit tests (TC-05 through TC-08)

**Acceptance Criteria:**
- [ ] `AppRoleIdFormatter` implements `IValueFormatter` with `TryFormat()` method
- [ ] Known app role GUIDs formatted with 🔑 icon, non-breaking space, name, and GUID in parentheses (TC-05)
- [ ] Unknown GUIDs return `null` (fallback to raw display) (TC-06)
- [ ] Null/empty/whitespace values return `null` without exception (TC-07)
- [ ] Multiple known GUIDs resolve correctly (TC-08 verifies a second GUID like `Directory.Read.All`)
- [ ] Constructor defaults to `MicrosoftGraphAppRoleResolver.CreateBuiltIn()` when no resolver provided
- [ ] All 4 unit tests pass

**Dependencies:** Task 2

**Notes:**
Follow the `RoleDefinitionFormatter` pattern exactly. The icon is 🔑 (key) to indicate permissions, matching the architecture document's specification.

---

### Task 4: Summary Builder for App Role Assignments

**Priority:** High

**Description:**
Create the `AzureAdSummaryBuilder.AppRoleAssignments.cs` partial class that generates summary HTML for `azuread_app_role_assignment` resources. Add the dispatch case in the main `AzureAdSummaryBuilder.cs` and update `AzureAdSummaryFactory` to accept and pass `IAppRoleResolver`. Write unit tests (TC-09 through TC-15).

**Files to Create:**
- `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.AppRoleAssignments.cs` — Partial class with `BuildAppRoleAssignmentSummaryHtml()` method; implements resolution order: app_role_id via `IAppRoleResolver`, principal_object_id and resource_object_id via `IPrincipalMapper` with computed attribute fallbacks
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/AzureAdAppRoleAssignmentSummaryTests.cs` — Unit tests (TC-09 through TC-15)

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryBuilder.cs` — Add `IAppRoleResolver` parameter to `BuildSummaryHtml()` signature; add dispatch case for `azuread_app_role_assignment` resource type
- `src/Oocx.TfPlan2Md/Providers/AzureAD/Models/AzureAdSummaryFactory.cs` — Add `IAppRoleResolver` field (constructor-injected); pass to `BuildSummaryHtml()`

**Acceptance Criteria:**
- [ ] Summary format: `{action} azuread_app_role_assignment <b><code>{name}</code></b> — <code>{role}</code> → <code>{principal}</code> on <code>{resource}</code>`
- [ ] All GUIDs mapped: full summary with resolved names (TC-09)
- [ ] No mappings: raw GUIDs displayed for all three components (TC-10)
- [ ] Delete action uses ❌ icon prefix (TC-11)
- [ ] Partial mapping: mix of resolved names and raw GUIDs (TC-12)
- [ ] Principal and resource resolved via `IPrincipalMapper` (TC-13)
- [ ] Computed attribute fallbacks (`principal_display_name`, `resource_display_name`) used when mapper lookup fails (TC-14)
- [ ] Missing attributes handled gracefully without exception (TC-15)
- [ ] `AzureAdSummaryFactory` constructor accepts optional `IAppRoleResolver?` parameter
- [ ] Existing resource type summaries continue to work unchanged (backward compatibility)
- [ ] All 7 unit tests pass

**Dependencies:** Task 2

**Notes:**
Resolution order per specification: `{role}` = 1. IAppRoleResolver → 2. raw GUID; `{principal}` = 1. IPrincipalMapper → 2. computed `principal_display_name` → 3. raw GUID; `{resource}` = 1. IPrincipalMapper → 2. computed `resource_display_name` → 3. raw GUID.

---

### Task 5: Resource Renderer and Icon Registration

**Priority:** Medium

**Description:**
Add the `AppRoleAssignmentRenderer` to the existing resource renderers file and add the 🔑 icon rule for `azuread_app_role_assignment` to the icons JSON.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureAD/Renderers/AzureAdResourceRenderers.cs` — Add `AppRoleAssignmentRenderer` class extending `AzureAdDelegatingRenderer` with resource type `"azuread_app_role_assignment"`
- `src/Oocx.TfPlan2Md/Providers/AzureAD/Icons/azuread-icons.json` — Add icon rule: `resourceTypePattern: "(?i)^azuread_app_role_assignment$"`, `attributeNamePattern: "(?i)^app_role_id$"`, `icon: "🔑"`

**Acceptance Criteria:**
- [ ] `AppRoleAssignmentRenderer` class added, extending `AzureAdDelegatingRenderer` with `"azuread_app_role_assignment"` resource type
- [ ] `azuread-icons.json` contains a rule for `azuread_app_role_assignment` with 🔑 icon on `app_role_id` attribute
- [ ] Existing icon rules remain unchanged
- [ ] Project builds successfully

**Dependencies:** None (can be done in parallel with Tasks 1–3)

**Notes:**
The renderer follows the simple delegation pattern — just instantiate `AzureAdDelegatingRenderer` with the resource type string. No custom render logic needed.

---

### Task 6: AzureADModule Wiring and CompositionRoot Integration

**Priority:** High

**Description:**
Update `AzureADModule` to accept `IAppRoleResolver` and `IPrincipalMapper`, register the summary factory (with resolver), value formatters, and resource renderer for `azuread_app_role_assignment`. Update `CompositionRoot` to create and wire the app role resolver. Write integration test TC-16.

**Files to Modify:**
- `src/Oocx.TfPlan2Md/Providers/AzureAD/AzureADModule.cs`:
  - Add `IAppRoleResolver?` and `IPrincipalMapper?` constructor parameters (optional, defaulting to null)
  - Store as fields `_appRoleResolver`, `_principalMapper`
  - In `RegisterFactories()`: pass `_appRoleResolver` to `AzureAdSummaryFactory`; register `"azuread_app_role_assignment"` with the factory
  - In `RegisterValueFormatters()`: register `AppRoleIdFormatter` for `app_role_id` attribute; register `PrincipalIdFormatter` for `principal_object_id` and `resource_object_id` attributes (scoped to `azuread_app_role_assignment`)
  - In `RegisterResourceRenderers()`: register `AppRoleAssignmentRenderer`
- `src/Oocx.TfPlan2Md/CompositionRoot.cs`:
  - Add `CreateAppRoleResolver()` method returning `IAppRoleResolver`
  - Update `CreateProviderRegistry()` to accept `IAppRoleResolver` parameter
  - Pass `appRoleResolver` and `principalMapper` to `new AzureADModule(entityMapper, principalMapper, appRoleResolver)`
  - Update `ComposeServices()` to create `appRoleResolver` and pass to `CreateProviderRegistry()`

**Files to Create:**
- Integration test for TC-16 (in existing or new test file under `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureAD/`)

**Acceptance Criteria:**
- [ ] `AzureADModule` constructor accepts optional `IAppRoleResolver?` and `IPrincipalMapper?` parameters
- [ ] `azuread_app_role_assignment` registered as a known resource type in `RegisterFactories()`
- [ ] `AppRoleIdFormatter` registered for `app_role_id` attribute on `azuread_app_role_assignment` resources
- [ ] `PrincipalIdFormatter` registered for `principal_object_id` and `resource_object_id` on `azuread_app_role_assignment`
- [ ] `AppRoleAssignmentRenderer` registered in `RegisterResourceRenderers()`
- [ ] `CompositionRoot` creates `IAppRoleResolver` and passes it through the dependency chain
- [ ] Existing resource type registrations remain unchanged (backward compatibility)
- [ ] Integration test TC-16 verifies module registration completes without errors
- [ ] Full project builds and all existing tests pass

**Dependencies:** Tasks 1–5

**Notes:**
This is the integration point where all the new components come together. Ensure backward compatibility by making new constructor parameters optional. The `PrincipalIdFormatter` already exists in `Providers/AzureRM/` and should be reused. Check if it needs to be moved to a shared location or if it can be referenced from its current location.

---

### Task 7: End-to-End Snapshot Test

**Priority:** Medium

**Description:**
Create test data (Terraform plan JSON) and end-to-end snapshot test for `azuread_app_role_assignment` resources, verifying the full rendering pipeline (TC-17).

**Files to Create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/TestData/azuread-app-role-assignment-plan.json` — Terraform plan JSON containing at least one create and one delete action for `azuread_app_role_assignment` resources, with known `app_role_id` GUIDs, `principal_object_id`, `resource_object_id`, and computed attributes (`principal_display_name`, `resource_display_name`)
- End-to-end snapshot test (TC-17) in appropriate test file

**Acceptance Criteria:**
- [ ] Test data JSON is valid Terraform plan format with `azuread_app_role_assignment` resource changes
- [ ] Test data includes a create action with known `app_role_id` GUID (`df021288-bdef-4463-88db-98f22de89214` for `User.Read.All`)
- [ ] Test data includes computed attributes: `principal_display_name`, `resource_display_name`
- [ ] Test data includes at least one delete action for action-icon coverage
- [ ] End-to-end test renders correct markdown with resolved app role name in summary
- [ ] Detail table shows formatted `app_role_id` with 🔑 icon
- [ ] Snapshot output verified and committed
- [ ] No rendering errors or missing sections
- [ ] Existing snapshot tests continue to pass

**Dependencies:** Task 6

**Notes:**
Follow existing snapshot test patterns in the test suite. The test data JSON should be realistic — model it after the Terraform `azuread_app_role_assignment` resource schema. Include both mapped and unmapped GUIDs to cover edge cases in the snapshot output.

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1: Embedded JSON Data File and Build Configuration** — Foundation; the data file and build plumbing that everything else depends on. Must compile first to enable the source generator.
2. **Task 2: App Role Resolver** — Core logic; depends on the embedded JSON from Task 1. Independently testable with its own unit tests.
3. **Task 3: AppRoleIdFormatter** — Value formatter; depends on the resolver from Task 2. Independently testable.
4. **Task 4: Summary Builder** — Depends on the resolver interface from Task 2. Can be developed in parallel with Task 3.
5. **Task 5: Resource Renderer and Icon Registration** — No code dependencies on Tasks 1–4; can be done in parallel. Kept separate for clarity.
6. **Task 6: AzureADModule Wiring and CompositionRoot** — Integration point; requires all components from Tasks 1–5 to be in place.
7. **Task 7: End-to-End Snapshot Test** — Final validation; requires the full pipeline from Task 6 to be wired.

**Parallelism opportunities:** Tasks 3, 4, and 5 can be developed in parallel after Task 2 is complete.

## Open Questions

None — the specification and architecture documents provide sufficient detail for all tasks. The three open questions from the specification (app role list completeness, display format, icon choice) have been resolved in the architecture document:
- **Completeness:** Comprehensive list (matching `AzureRoleDefinitions.json` precedent)
- **Display format:** `Name (GUID)` format (matching `RoleDefinitionFormatter` pattern)
- **Icon:** 🔑 for `app_role_id` attribute
