# Tasks: Extensible Provider Registry System

## Overview

This feature implements a flexible provider registry system for resource view model factories, value formatters, and icon providers. It replaces hardcoded logic in `ScribanHelpers` with a pattern-matching engine that supports specificity-based resolution across four dimensions: Provider, Resource Type, Attribute Name, and Value.

Reference: [specification.md](specification.md), [architecture.md](architecture.md).

## Tasks

### Task 1: Core Registry Infrastructure

**Priority:** High

**Description:**
Implement the foundational pattern-matching engine and its supporting data structures as defined in the architecture.

**Acceptance Criteria:**
- [x] `MatchPattern` value object correctly compiles Regex for the 4 dimensions and calculates specificity score.
- [x] `ServiceResolutionContext` record created for passing resolution data.
- [x] `ServiceRegistration<T>` record created to pair services with patterns.
- [x] `PatternMatchingRegistry<T>` generic class implemented with `Register(MatchPattern, T)` and `ResolveAll(ServiceResolutionContext)` methods.
- [x] Resolution algorithm correctly sorts by:
    1. Specificity score (descending)
    2. Dimension priority: Value (8) > Attribute (4) > Resource Type (2) > Provider (1)
    3. Registration order (ascending) for tie-breaking.
- [x] Unit tests cover regex matching, wildcard support, and specificity sorting.

**Dependencies:** None

---

### Task 2: Service Interfaces and Typed Registries

**Priority:** High

**Description:**
Define the interfaces for value formatting and icon provision, and create the typed registry wrappers.

**Acceptance Criteria:**
- [x] `IValueFormatter` interface with `TryFormat(ServiceResolutionContext)` method.
- [x] `IIconProvider` interface with `TryGetIcon(ServiceResolutionContext)` method.
- [x] `ValueFormatterRegistry` wrapper implemented.
- [x] `IconProviderRegistry` wrapper implemented.
- [x] Unit tests for `ValueFormatterRegistry` and `IconProviderRegistry` verifying they correctly delegate to the generic registry and handle fallback (iterator over resolved services).

**Dependencies:** Task 1

---

### Task 3: JSON-Based Icon Provider

**Priority:** Medium

**Description:**
Implement the `FileBasedIconProvider` that loads rules from a JSON file, enabling declarative icon management.

**Acceptance Criteria:**
- [x] `IconRule` model and `IconRulesJsonContext` (AOT source generator) implemented.
- [x] `FileBasedIconProvider` implemented, reading JSON and populating an internal registry.
- [x] `ServiceRegistrationException` thrown for invalid regex patterns in JSON.
- [x] Unit tests verify correct parsing of various JSON rule combinations.

**Dependencies:** Task 2

---

### Task 4: Integration with Provider System

**Priority:** High

**Description:**
Wire the new registry system into the existing provider module infrastructure and application startup.

**Acceptance Criteria:**
- [x] `IProviderModule` extended with `RegisterValueFormatters()` and `RegisterIconProviders()` (default empty implementations).
- [x] `ProviderRegistry` updated to support `RegisterAllValueFormatters()` and `RegisterAllIconProviders()`.
- [x] `ProgramEntry.cs` updated to initialize registries and populate them from the provider modules.
- [x] Registries passed to `ReportModelBuilder` and `MarkdownRenderer`.

**Dependencies:** Task 2, Task 3

---

### Task 5: Scriban Helper Integration

**Priority:** High

**Description:**
Update `ScribanHelpers` to use the new registries before falling back to existing hardcoded logic.

**Acceptance Criteria:**
- [x] `ScribanHelpers.Registry` updated to accept and store the new registries.
- [x] `format_value` helper updated to try `ValueFormatterRegistry` first.
- [x] `format_attribute_value_*` and related icon helpers updated to try `IconProviderRegistry` first.
- [x] New `get_icon` helper introduced for explicit icon resolution in templates (removed in Task 8d).

**Dependencies:** Task 4

---

### Task 6: Migration of Existing Rules

**Priority:** Medium

**Description:**
Migrate existing hardcoded icon and formatting logic for AzureRM, AzApi, AzureAD, and AzureDevOps to the new registry system.

**Acceptance Criteria:**
- [x] AzureRM icons (locations, NSG protocols, etc.) migrated to JSON or code registrations.
- [x] Azure AD and Azure DevOps icons (users, groups, etc.) implemented.
- [x] Azure resource ID formatting migrated to `AzureResourceIdFormatter`.
- [x] Existing functionality confirmed via regression tests.

**Dependencies:** Task 5

---

### Task 7: Comprehensive Testing and UAT

**Priority:** Medium

**Description:**
Create new snapshot tests for Azure AD and Azure DevOps.

**Acceptance Criteria:**
- [x] `AzureAdSnapshotTests.cs` created with a comprehensive plan covering users, groups, and service principals.
- [x] `AzureDevOpsSnapshotTests.cs` created covering variable groups and projects.
- [ ] UAT is performed after code review (out of scope for this task).

**Dependencies:** Task 6

---

### Task 8: Eliminate `get_icon` from All Templates

**Priority:** High

**Description:**
Remove all 21 `get_icon` calls from templates by pre-computing icons in C# view models. This simplifies template authoring, prevents forgotten icon calls, and makes icon resolution testable in C#. After this task, the `get_icon` Scriban helper can be removed entirely.

#### Task 8a: Pre-compute Azure AD Summary Lines

**Description:**
Six Azure AD templates currently build their `<summary>` line in Scriban by extracting values from raw `after_json`/`before_json` and calling `get_icon` to prepend icons. Move this logic into C# view model factories that produce a pre-computed `summary_html`, following the pattern already established by `variable_group.sbn`.

**Affected templates (15 `get_icon` calls):**
- `user.sbn` — display_name (👤), user_principal_name (🆔), mail (📧) in summary
- `group.sbn` — member type legend icons (👤👥💻❓) + display_name (👥), mail_nickname (🆔) in summary
- `group_without_members.sbn` — display_name (👥), mail_nickname (🆔) in summary
- `group_member.sbn` — group_name (👥), member_type (👤/👥/💻) in summary
- `service_principal.sbn` — display_name (💻), application_id (🆔) in summary
- `invitation.sbn` — user_email_address (📧) in summary

**Acceptance Criteria:**
- [x] Azure AD resource view model factories pre-compute `summary_html` with icon-decorated values resolved via `IconProviderRegistry`.
- [x] All six Azure AD templates use `change.summary_html` instead of manual `get_icon` + `format_icon_value_summary` chains.
- [x] Summary output is identical to current output (verified by snapshot tests).

**Dependencies:** Task 7

---

#### Task 8e: Centralize Action Icons in C#

**Description:**
Action icons (➕/🔄/❌/⏺️/♻️) are now defined once in C# so templates and tests reference a single source of truth. This prevents drift across view models, summaries, and tests.

**Acceptance Criteria:**
- [x] `ActionIcons` centralizes all action symbols in C#.
- [x] Action icon usage updated to reference `ActionIcons` in code and tests.
- [x] Icon registry is auto-wired when a provider registry is supplied, so Azure AD summaries keep their icons.

**Dependencies:** Task 8d

---

#### Task 8b: Remove Redundant Table Icon Branching from user.sbn

**Description:**
`user.sbn` special-cases `display_name` in table cells: it calls `get_icon` and then `format_icon_value_table`, but `format_attribute_value_table` already resolves icons via the `IconProviderRegistry` for all attributes. The special branching is redundant and produces identical output.

**Affected template (4 `get_icon` calls):**
- `user.sbn` — `display_name` icon in create table (line 58), delete table (line 75), update table before/after (lines 91-92)

**Acceptance Criteria:**
- [x] `user.sbn` table sections use `format_attribute_value_table_resource` uniformly for all attributes, with no `display_name` special-case.
- [x] Table output is identical to current output (verified by snapshot tests).

**Dependencies:** Task 7

---

#### Task 8c: Add `ChangeIcon` to `VariableChangeRowViewModel`

**Description:**
`variable_group.sbn` calls `get_icon` to resolve the change column icon (➕/🔄/❌/⏺️). The `Change` property is a fixed string set at factory construction time, so the icon can be pre-resolved alongside it.

**Affected template (1 `get_icon` call):**
- `variable_group.sbn` — change column icon (line 56)

**Acceptance Criteria:**
- [x] `VariableChangeRowViewModel` has a new `ChangeIcon` property pre-populated by the factory methods.
- [x] `variable_group.sbn` uses `var.change_icon` directly instead of calling `get_icon`.
- [x] Table output is identical to current output (verified by snapshot tests).

**Dependencies:** Task 7

---

#### Task 8d: Remove `get_icon` Scriban Helper

**Description:**
After Tasks 8a-8c eliminate all template usages, remove the `get_icon` helper registration from `ScribanHelpers` and the integration test. Regenerate all snapshot baselines to confirm output is unchanged.

**Acceptance Criteria:**
- [x] `get_icon` helper removed from `ScribanHelpers` registration.
- [x] `ScribanHelpersRegistryIntegrationTests` updated to remove the `get_icon` test case.
- [x] Snapshot tests pass with unchanged output.
- [x] Zero `get_icon` occurrences remain in any `.sbn` template file.

**Dependencies:** Task 8a, 8b, 8c

---

## Implementation Order

Recommended sequence for implementation:
1. **Task 1 & 2**: Foundational infrastructure.
2. **Task 3**: Enable file-based configuration.
3. **Task 4 & 5**: Integration and wiring.
4. **Task 6**: Migration (core value delivery).
5. **Task 7**: Quality assurance and verification.
6. **Task 8b → 8c → 8a → 8d**: Eliminate `get_icon` (simplest changes first, summary pre-computation last, then cleanup).

## Open Questions

1. Should we migrate **all** existing factories to the new registry in this phase? (Recommendation: Start with formatters and icons, factories can stay in the old registry for now as they use exact matching).
2. Are there any performance concerns with many regex evaluations per resource? (Recommendation: Caching compiled regexes per Task 1 should be sufficient for current scale).
