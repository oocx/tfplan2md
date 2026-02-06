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
- [ ] `IProviderModule` extended with `RegisterValueFormatters()` and `RegisterIconProviders()` (default empty implementations).
- [ ] `ProviderRegistry` updated to support `RegisterAllValueFormatters()` and `RegisterAllIconProviders()`.
- [ ] `ProgramEntry.cs` updated to initialize registries and populate them from the provider modules.
- [ ] Registries passed to `ReportModelBuilder` and `MarkdownRenderer`.

**Dependencies:** Task 2, Task 3

---

### Task 5: Scriban Helper Integration

**Priority:** High

**Description:**
Update `ScribanHelpers` to use the new registries before falling back to existing hardcoded logic.

**Acceptance Criteria:**
- [ ] `ScribanHelpers.Registry` updated to accept and store the new registries.
- [ ] `format_value` helper updated to try `ValueFormatterRegistry` first.
- [ ] `format_attribute_value_*` and related icon helpers updated to try `IconProviderRegistry` first.
- [ ] New `get_icon` helper introduced for explicit icon resolution in templates.

**Dependencies:** Task 4

---

### Task 6: Migration of Existing Rules

**Priority:** Medium

**Description:**
Migrate existing hardcoded icon and formatting logic for AzureRM, AzApi, AzureAD, and AzureDevOps to the new registry system.

**Acceptance Criteria:**
- [ ] AzureRM icons (locations, NSG protocols, etc.) migrated to JSON or code registrations.
- [ ] Azure AD and Azure DevOps icons (users, groups, etc.) implemented.
- [ ] Azure resource ID formatting migrated to `AzureResourceIdFormatter`.
- [ ] Existing functionality confirmed via regression tests.

**Dependencies:** Task 5

---

### Task 7: Comprehensive Testing and UAT

**Priority:** Medium

**Description:**
Create new snapshot tests for Azure AD and Azure DevOps, and perform UAT.

**Acceptance Criteria:**
- [ ] `AzureAdSnapshotTests.cs` created with a comprehensive plan covering users, groups, and service principals.
- [ ] `AzureDevOpsSnapshotTests.cs` created covering variable groups and projects.
- [ ] UAT artifact `extensible-registry-uat.md` generated and validated using the `uat-run.sh` script.

**Dependencies:** Task 6

---

## Implementation Order

Recommended sequence for implementation:
1. **Task 1 & 2**: Foundational infrastructure.
2. **Task 3**: Enable file-based configuration.
3. **Task 4 & 5**: Integration and wiring.
4. **Task 6**: Migration (core value delivery).
5. **Task 7**: Quality assurance and verification.

## Open Questions

1. Should we migrate **all** existing factories to the new registry in this phase? (Recommendation: Start with formatters and icons, factories can stay in the old registry for now as they use exact matching).
2. Are there any performance concerns with many regex evaluations per resource? (Recommendation: Caching compiled regexes per Task 1 should be sufficient for current scale).
