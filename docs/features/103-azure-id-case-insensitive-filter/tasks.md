# Tasks: Case-Insensitive Attribute Change Filter

## Overview

Implements the `--ignore-case-changes` CLI flag (feature 103) that suppresses attribute change rows where before and after values are **Azure resource IDs** that differ only in letter casing. This eliminates Azure ARM API ID-casing noise from Terraform plan reports.

The implementation uses a new `IAttributeChangeFilter` / `AttributeChangeFilterRegistry` extension point in the core (mirroring the existing `IValueFormatter` / `ValueFormatterRegistry` pattern). The Azure-specific filter logic lives entirely in `Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs`. The core pipeline gains only a single delegate call to the filter registry — no Azure knowledge in `MarkdownGeneration/`.

Reference: [specification.md](./specification.md) | [architecture.md](./architecture.md) | [test-plan.md](./test-plan.md)

---

## Tasks

### Task 1: Create test data file `azurerm-case-only-ids-plan.json`

**Priority:** High

**Description:**
Create a minimal but complete Terraform plan JSON fixture covering all integration test scenarios defined in the test plan. This file must exist before any integration tests can be written and run.

**File to create:**
`src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-case-only-ids-plan.json`

**Acceptance Criteria:**
- [x] File is valid Terraform plan JSON (format_version `"1.2"`, terraform_version `"1.9.0"` or similar).
- [x] Contains `azurerm_role_assignment.casing_only` (provider `registry.terraform.io/hashicorp/azurerm`, action `["update"]`) with:
  - `scope` before: `/subscriptions/ABC123/resourceGroups/my-rg`, after: `/subscriptions/abc123/resourceGroups/my-rg` (Azure ID casing-only)
  - `role_definition_id` before: `/providers/Microsoft.Authorization/roleDefinitions/XYZ`, after: `/providers/Microsoft.Authorization/roleDefinitions/xyz` (Azure ID casing-only)
- [x] Contains `azurerm_role_assignment.mixed_changes` (provider `registry.terraform.io/hashicorp/azurerm`, action `["update"]`) with:
  - `scope` before/after same casing-only Azure ID difference as above
  - `display_name` before: `"My App"`, after: `"My Application"` (genuine non-ID change)
- [x] Contains `azurerm_key_vault.null_before` (action `["update"]`) with `tenant_id` before: `null`, after: `"tenant-abc"`.
- [x] Contains `azurerm_key_vault.null_after` (action `["update"]`) with `tenant_id` before: `"tenant-abc"`, after: `null`.
- [x] Contains `azurerm_key_vault.numeric_change` (action `["update"]`) with `soft_delete_retention_days` before: `7`, after: `14`.
- [x] Contains `azurerm_key_vault.unchanged` (action `["update"]`) with `name` before: `"my-vault"`, after: `"my-vault"` (ordinal-equal, unchanged).
- [x] Contains `azurerm_role_assignment.display_name_casing` (action `["update"]`) with `display_name` before: `"MyApp"`, after: `"myapp"` (casing-only but **not** an Azure resource ID — must NOT be suppressed, for TC-15).
- [x] Contains `random_string.non_azurerm` (provider `registry.terraform.io/hashicorp/random`, action `["update"]`) with `result` before: `/subscriptions/ABC123/resourceGroups/my-rg`, after: `/subscriptions/abc123/resourceGroups/my-rg` (Azure-ID-shaped values in a non-azurerm provider — must NOT be suppressed, for TC-16).
- [x] File is loadable by the existing `TerraformPlanParser` without errors.

**Dependencies:** None

**Notes:**
Follow the structure of an existing test data file in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`. Each resource must appear in `resource_changes` with `change.before`, `change.after`, `change.actions`, and `type`/`address`/`name` fields. The `provider_name` field (in `resource_changes[].provider_name`) must be set correctly for TC-16 to work. The `before_sensitive` and `after_sensitive` fields should be set to `false` for all attributes. TC-17 through TC-24 use inline values in test code itself and do not require additional JSON files.

---

### Task 2: Add `IgnoreCaseChanges` to `CliOptions` record and `CliParser.Parse()`

**Priority:** High

**Description:**
Extend the CLI layer to recognise the `--ignore-case-changes` flag and store its value in the parsed options object. This is the entry point for the entire feature.

**File to modify:**
`src/Oocx.TfPlan2Md/CLI/CliParser.cs`

**Acceptance Criteria:**
- [x] `CliOptions` record has a new property after `ShowUnchangedValues`:
  ```csharp
  /// <summary>
  /// Gets a value indicating whether attribute change rows where before and after values
  /// are Azure resource IDs that differ only in casing are suppressed.
  /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
  /// </summary>
  public bool IgnoreCaseChanges { get; init; }
  ```
- [x] `CliParser.Parse()` declares a local `var ignoreCaseChanges = false;` alongside the other boolean locals.
- [x] The `switch` statement inside `Parse()` has a new case after `"--show-unchanged-values"`:
  ```csharp
  case "--ignore-case-changes":
      ignoreCaseChanges = true;
      break;
  ```
- [x] The `return new CliOptions { ... }` initializer includes `IgnoreCaseChanges = ignoreCaseChanges`.
- [x] Passing an unknown flag still throws `CliParseException` (default case untouched).
- [x] Default value of `IgnoreCaseChanges` is `false` (no flag → unchanged behavior).

**Dependencies:** None

---

### Task 3: Add `--ignore-case-changes` to `HelpTextProvider`

**Priority:** High

**Description:**
Add an entry for `--ignore-case-changes` in the options array so it appears in the CLI help output.

**File to modify:**
`src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs`

**Acceptance Criteria:**
- [x] A new tuple is added to the `options` array after the `"--show-unchanged-values"` entry:
  ```csharp
  ("--ignore-case-changes", "Suppress attribute changes where before/after values differ only in casing."),
  ```
- [x] `HelpTextProvider.GetHelpText()` output contains the string `"--ignore-case-changes"`.
- [x] `HelpTextProvider.GetHelpText()` output contains the word `"casing"` (description references casing).

**Dependencies:** None

---

### Task 4: Add `IgnoreCaseChanges` property to `ReportModel`

**Priority:** High

**Description:**
Add the flag value to the report data model so templates can read it.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`

**Acceptance Criteria:**
- [x] `ReportModel` has a new property after `ShowUnchangedValues`:
  ```csharp
  /// <summary>
  /// Gets a value indicating whether attribute change rows where before and after values
  /// are Azure resource IDs that differ only in casing are suppressed.
  /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
  /// </summary>
  public required bool IgnoreCaseChanges { get; init; }
  ```
- [x] Property is `required` and `bool`, consistent with `ShowUnchangedValues` and `HideMetadata`.

**Dependencies:** None

---

### Task 5: Create core filter infrastructure (`IAttributeChangeFilter`, `AttributeChangeFilterContext`, `AttributeChangeFilterRegistry`)

**Priority:** High

**Description:**
Add three new source files to `MarkdownGeneration/Services/` that define the generic filter extension point used by the core pipeline. These types contain **no Azure-specific logic** — they are pure infrastructure.

**Files to create:**

- `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AttributeChangeFilterContext.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/IAttributeChangeFilter.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/AttributeChangeFilterRegistry.cs`

**Acceptance Criteria:**

`AttributeChangeFilterContext.cs`:
- [x] Declares `internal sealed record AttributeChangeFilterContext` with four positional parameters:
  ```csharp
  internal sealed record AttributeChangeFilterContext(
      string? ProviderName,
      string? AttributeName,
      string? BeforeValue,
      string? AfterValue);
  ```

`IAttributeChangeFilter.cs`:
- [x] Declares `internal interface IAttributeChangeFilter` with a single method:
  ```csharp
  bool ShouldSuppress(AttributeChangeFilterContext context);
  ```

`AttributeChangeFilterRegistry.cs`:
- [x] Declares `internal sealed class AttributeChangeFilterRegistry`.
- [x] Has a `Register(IAttributeChangeFilter filter)` method that adds the filter to an internal list.
- [x] Has a `ShouldSuppress(AttributeChangeFilterContext context)` method that iterates all registered filters and returns `true` if **any** filter returns `true` (OR semantics).
- [x] An empty registry returns `false` from `ShouldSuppress()` for any context.
- [x] No Azure-specific or provider-specific code is present in any of these three files.

**Dependencies:** None

---

### Task 6: Add `RegisterAttributeChangeFilters()` to `IProviderModule` and `ProviderRegistry`

**Priority:** High

**Description:**
Extend the provider module contract to allow each provider module to register its attribute change filters, then wire up the bulk-registration method in `ProviderRegistry`. This mirrors the existing `RegisterValueFormatters` / `RegisterAllValueFormatters` pattern.

**Files to modify:**

- `src/Oocx.TfPlan2Md/Providers/IProviderModule.cs`
- `src/Oocx.TfPlan2Md/MarkdownGeneration/Services/ProviderRegistry.cs`

**Acceptance Criteria:**

`IProviderModule.cs`:
- [x] A new method is added to `IProviderModule` with a default no-op implementation so all existing provider modules remain source-compatible without changes:
  ```csharp
  void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
  {
      // Default no-op
  }
  ```

`ProviderRegistry.cs`:
- [x] A new `RegisterAllAttributeChangeFilters(AttributeChangeFilterRegistry registry)` method is added:
  ```csharp
  public void RegisterAllAttributeChangeFilters(AttributeChangeFilterRegistry registry)
  {
      foreach (var provider in _providers)
          provider.RegisterAttributeChangeFilters(registry);
  }
  ```
  (Consistent with the existing `RegisterAllValueFormatters`, `RegisterAllIconProviders`, etc. pattern.)
- [x] No existing methods in `ProviderRegistry` are changed.

**Dependencies:** Task 5 (`AttributeChangeFilterRegistry` type must exist)

---

### Task 7: Create `AzureResourceIdCaseChangeFilter` in `Providers/AzureRM/`

**Priority:** High

**Description:**
Implement the Azure-specific filter that detects and suppresses attribute change rows where both values are Azure resource IDs that differ only in casing. This class lives entirely in the AzureRM provider folder and uses the existing `AzureScopeParser.IsAzureResourceId()` method from `Platforms/Azure/`.

**File to create:**
`src/Oocx.TfPlan2Md/Providers/AzureRM/AzureResourceIdCaseChangeFilter.cs`

**Acceptance Criteria:**
- [x] Class is `internal sealed` and implements `IAttributeChangeFilter`.
- [x] Contains a compiled `Regex` field matching the azurerm provider pattern `(^azurerm$|.*/azurerm$)` with a 1-second timeout (consistent with other regex usage in the codebase).
- [x] `ShouldSuppress(AttributeChangeFilterContext context)` implements the following guards in order:
  1. Return `false` if `context.BeforeValue` or `context.AfterValue` is `null`.
  2. Return `false` if `context.ProviderName` does not match the azurerm provider regex.
  3. Return `false` if **neither** `context.BeforeValue` nor `context.AfterValue` is an Azure resource ID (both `AzureScopeParser.IsAzureResourceId(BeforeValue)` and `AzureScopeParser.IsAzureResourceId(AfterValue)` return `false`).
  4. Return `true` if `string.Equals(context.BeforeValue, context.AfterValue, StringComparison.OrdinalIgnoreCase)` (casing-only change on an Azure resource ID — suppress it).
  5. Otherwise return `false` (values differ by more than casing).
- [x] The fully-qualified provider name `"registry.terraform.io/hashicorp/azurerm"` is matched by the pattern (step 2 returns `true` for it).
- [x] The short provider name `"azurerm"` is also matched.
- [x] Non-azurerm provider names (e.g., `"registry.terraform.io/hashicorp/aws"`, `"azapi"`) are NOT matched (step 2 returns `false`).
- [x] Uses `AzureScopeParser.IsAzureResourceId()` from `Platforms/Azure/` — no duplicate detection logic.

**Dependencies:** Task 5 (`IAttributeChangeFilter`, `AttributeChangeFilterContext`)

---

### Task 8: Override `RegisterAttributeChangeFilters()` in `AzureRMModule`

**Priority:** High

**Description:**
Override the new `IProviderModule.RegisterAttributeChangeFilters()` method in `AzureRMModule` to register `AzureResourceIdCaseChangeFilter` with the registry.

**File to modify:**
`src/Oocx.TfPlan2Md/Providers/AzureRM/AzureRMModule.cs`

**Acceptance Criteria:**
- [x] `AzureRMModule` overrides `RegisterAttributeChangeFilters()`:
  ```csharp
  public override void RegisterAttributeChangeFilters(AttributeChangeFilterRegistry registry)
  {
      registry.Register(new AzureResourceIdCaseChangeFilter());
  }
  ```
  (Or `public void` if `IProviderModule` uses interface default methods, matching the existing pattern in the class.)
- [x] No other changes to `AzureRMModule` are needed.

**Dependencies:** Task 6 (`RegisterAttributeChangeFilters()` on `IProviderModule`), Task 7 (`AzureResourceIdCaseChangeFilter`)

---

### Task 9: Update `ReportModelBuilder` to accept and store `AttributeChangeFilterRegistry`

**Priority:** High

**Description:**
Extend the `ReportModelBuilder` primary constructor to accept the `AttributeChangeFilterRegistry` dependency (alongside the existing `ignoreCaseChanges` parameter), making both available to `BuildAttributeChanges()`.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs`

**Acceptance Criteria:**
- [x] The primary constructor parameter list has two new parameters after `showUnchangedValues`:
  ```csharp
  bool ignoreCaseChanges = false,
  AttributeChangeFilterRegistry? attributeChangeFilterRegistry = null,
  ```
- [x] Two new backing fields are declared:
  ```csharp
  /// <summary>
  /// Indicates whether the attribute change filter registry should be consulted.
  /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
  /// </summary>
  private readonly bool _ignoreCaseChanges = ignoreCaseChanges;

  private readonly AttributeChangeFilterRegistry _attributeChangeFilterRegistry =
      attributeChangeFilterRegistry ?? new AttributeChangeFilterRegistry();
  ```
- [x] The null-coalescing fallback (`?? new AttributeChangeFilterRegistry()`) ensures that tests that construct `ReportModelBuilder` without providing a registry still compile and run without null-reference exceptions (an empty registry never suppresses anything).
- [x] All existing constructor parameters and backing fields are untouched.

**Dependencies:** Task 4 (model property added), Task 5 (`AttributeChangeFilterRegistry` type exists)

---

### Task 10: Add filter registry call in `ReportModelBuilder.ResourceChanges.cs`

**Priority:** High

**Description:**
Add a single delegate call to the filter registry inside `BuildAttributeChanges()`, immediately after the `valuesEqual` computation. This is the only change to the core pipeline — it contains **no Azure-specific logic**.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

**Acceptance Criteria:**
- [x] The method `BuildAttributeChanges()` receives the provider name (e.g., passed in from the calling context or retrieved from the resource change). Confirm how `providerName` is currently available in this method before implementing.
- [x] After the `valuesEqual` computation (and after any existing call that mutates `valuesEqual`), insert:
  ```csharp
  if (_ignoreCaseChanges
      && !valuesEqual
      && _attributeChangeFilterRegistry.ShouldSuppress(
             new AttributeChangeFilterContext(providerName, key, beforeValue, afterValue)))
  {
      continue;   // filter (e.g. Azure ID casing-only change) — suppress row
  }
  ```
- [x] The existing `if (!_showUnchangedValues && valuesEqual) { continue; }` guard remains **unchanged** and appears **after** the new registry call.
- [x] `_attributeChangeFilterRegistry.ShouldSuppress()` is called only when `_ignoreCaseChanges` is `true` AND `valuesEqual` is `false` (short-circuit evaluation keeps the happy path fast).
- [x] When `_ignoreCaseChanges` is `false`, the registry is never consulted and behavior is identical to pre-change (no regression).
- [x] No Azure-specific code, no hardcoded strings, no regex patterns are present in this method.

**Dependencies:** Task 9 (`_ignoreCaseChanges` and `_attributeChangeFilterRegistry` backing fields)

---

### Task 11: Populate `IgnoreCaseChanges` in `ReportModelBuilder.Build.cs`

**Priority:** High

**Description:**
Set the `IgnoreCaseChanges` property on the `ReportModel` returned from `Build()`.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`

**Acceptance Criteria:**
- [x] Inside the `return new ReportModel { ... }` initializer in `Build()`, add:
  ```csharp
  IgnoreCaseChanges = _ignoreCaseChanges,
  ```
  Positioned near the other boolean flag properties (`ShowUnchangedValues`, `ShowSensitive`, `HideMetadata`).
- [x] The project compiles without error (the `required` `IgnoreCaseChanges` property on `ReportModel` is now satisfied).

**Dependencies:** Task 4 (`IgnoreCaseChanges` on `ReportModel`), Task 9 (`_ignoreCaseChanges` backing field)

---

### Task 12: Expose `ignore_case_changes` as a Scriban template variable

**Priority:** High

**Description:**
Map `ReportModel.IgnoreCaseChanges` to the Scriban script object so custom templates can conditionally use it.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`

**Acceptance Criteria:**
- [x] In `MapReportModel()`, add after the `show_unchanged_values` entry:
  ```csharp
  scriptObject["ignore_case_changes"] = model.IgnoreCaseChanges;
  ```
- [x] The key is `"ignore_case_changes"` (snake_case, matching `show_unchanged_values`, `show_sensitive`, etc.).
- [x] A Scriban template using `{{ ignore_case_changes }}` renders `"true"` when the flag is active.

**Dependencies:** Task 4, Task 11

---

### Task 13: Wire up `AttributeChangeFilterRegistry` and `IgnoreCaseChanges` in `CompositionRoot.cs`

**Priority:** High

**Description:**
Create the `AttributeChangeFilterRegistry`, populate it via `ProviderRegistry.RegisterAllAttributeChangeFilters()`, and pass both the registry and the parsed CLI flag to `ReportModelBuilder`.

**File to modify:**
`src/Oocx.TfPlan2Md/CompositionRoot.cs`

**Acceptance Criteria:**
- [x] A new helper method is added to `CompositionRoot`:
  ```csharp
  internal AttributeChangeFilterRegistry CreateAttributeChangeFilterRegistry(ProviderRegistry providerRegistry)
  {
      var registry = new AttributeChangeFilterRegistry();
      providerRegistry.RegisterAllAttributeChangeFilters(registry);
      return registry;
  }
  ```
  (Consistent with the existing `CreateValueFormatterRegistry` helper pattern.)
- [x] `CreateReportModelBuilder()` calls `CreateAttributeChangeFilterRegistry(providerRegistry)` and passes the result to the `ReportModelBuilder` constructor:
  ```csharp
  ignoreCaseChanges: options.IgnoreCaseChanges,
  attributeChangeFilterRegistry: CreateAttributeChangeFilterRegistry(providerRegistry),
  ```
  Both named arguments positioned after `showUnchangedValues: options.ShowUnchangedValues`.
- [x] The full CLI pipeline (`tfplan2md plan.json --ignore-case-changes`) correctly propagates the flag through to `BuildAttributeChanges()` with the Azure filter registered.

**Dependencies:** Task 2 (`CliOptions.IgnoreCaseChanges`), Task 6 (`RegisterAllAttributeChangeFilters()`), Task 8 (`AzureRMModule` registration), Task 9 (`ReportModelBuilder` constructor parameters)

---

### Task 14: Write unit tests

**Priority:** High

**Description:**
Implement all 24 test cases defined in the revised test plan. Create three new test files and update two existing files.

**Files to create:**
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderIgnoreCaseChangesTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/Providers/AzureRM/AzureResourceIdCaseChangeFilterTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/AttributeChangeFilterRegistryTests.cs`

**Files to modify:**
- `src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/CLI/HelpTextProviderTests.cs`

**Acceptance Criteria:**

*`ReportModelBuilderIgnoreCaseChangesTests.cs` — integration tests (TC-01 through TC-16):*
- [x] **TC-01** `Build_IgnoreCaseChangesFalse_IncludesCasingOnlyRows` — `ignoreCaseChanges: false` → Azure ID casing-only rows present in `AttributeChanges`.
- [x] **TC-02** `Build_IgnoreCaseChangesTrue_AllAzureIdCasingOnly_AttributeChangesEmpty` — `ignoreCaseChanges: true` on `azurerm_role_assignment.casing_only` → `AttributeChanges.Count == 0`.
- [x] **TC-03** `Build_IgnoreCaseChangesTrue_MixedChanges_RetainsGenuineChanges` — `ignoreCaseChanges: true` on `azurerm_role_assignment.mixed_changes` → `display_name` present, `scope` absent.
- [x] **TC-04** `Build_IgnoreCaseChangesTrue_NullBeforeValue_RowIsShown` — `azurerm_key_vault.null_before` → `tenant_id` row present (not suppressed).
- [x] **TC-05** `Build_IgnoreCaseChangesTrue_NullAfterValue_RowIsShown` — `azurerm_key_vault.null_after` → `tenant_id` row present (not suppressed).
- [x] **TC-06** `Build_IgnoreCaseChangesTrue_NumericAttributeChange_RowIsShown` — `azurerm_key_vault.numeric_change` → `soft_delete_retention_days` row present.
- [x] **TC-07** `Build_IgnoreCaseChangesTrue_AndShowUnchangedValues_CasingRowsStillSuppressed` — `ignoreCaseChanges: true, showUnchangedValues: true` → Azure ID casing-only rows absent; ordinal-equal rows present; genuine changes present.
- [x] **TC-11** `Build_Default_IgnoreCaseChangesFalseInModel` — `model.IgnoreCaseChanges.Should().BeFalse()` when no `ignoreCaseChanges` arg.
- [x] **TC-12** `Build_WithIgnoreCaseChangesTrue_ModelReflectsFlag` — `model.IgnoreCaseChanges.Should().BeTrue()`.
- [x] **TC-13** `Build_IgnoreCaseChangesTrue_OrdinallyEqualValues_BehavesLikeUnchanged` — ordinal-equal row in `azurerm_key_vault.unchanged` absent when `showUnchangedValues: false`.
- [x] **TC-14** `Render_IgnoreCaseChangesTrue_ScribanVariableIsTrue` — Scriban template `{{ ignore_case_changes }}` renders `"true"` when flag is active.
- [x] **TC-15** `Build_IgnoreCaseChangesTrue_NonAzureIdStringCasingChange_RowIsShown` — `azurerm_role_assignment.display_name_casing` → non-Azure-ID casing-only `display_name` row is present (NOT suppressed).
- [x] **TC-16** `Build_IgnoreCaseChangesTrue_NonAzureRmProvider_RowIsShown` — `random_string.non_azurerm` → row with Azure-ID-shaped values is present for non-azurerm provider (NOT filtered).

*`AzureResourceIdCaseChangeFilterTests.cs` — filter unit tests (TC-17 through TC-21):*
- [x] **TC-17** `ShouldSuppress_AzureIdCasingOnlyChange_ReturnsTrue` — fully-qualified azurerm provider, `scope` Azure ID differing only in casing → returns `true`. Parameterised variant: short provider name `"azurerm"` also returns `true`.
- [x] **TC-18** `ShouldSuppress_NonAzureIdStringCasingChange_ReturnsFalse` — azurerm provider, `display_name: "MyApp"` vs `"myapp"` (not an Azure ID) → returns `false`.
- [x] **TC-19** `ShouldSuppress_NonAzureRmProvider_ReturnsFalse` — provider `registry.terraform.io/hashicorp/aws`, Azure-ID-shaped values differing only in casing → returns `false`. Parameterised variant: `"azapi"` also returns `false`.
- [x] **TC-20** `ShouldSuppress_NullBeforeValue_ReturnsFalse` — `BeforeValue: null` → returns `false`.
- [x] **TC-21** `ShouldSuppress_NullAfterValue_ReturnsFalse` — `AfterValue: null` → returns `false`.

*`AttributeChangeFilterRegistryTests.cs` — registry unit tests (TC-22 through TC-24):*
- [x] **TC-22** `ShouldSuppress_EmptyRegistry_ReturnsFalse` — no filters registered → returns `false` for any context.
- [x] **TC-23** `ShouldSuppress_OneFilterReturnsTrue_ReturnsTrue` — one stub returning `false`, one stub returning `true` → registry returns `true`.
- [x] **TC-24** `ShouldSuppress_AllFiltersReturnFalse_ReturnsFalse` — two stubs both returning `false` → registry returns `false`.

*Updates to `CliParserTests.cs`:*
- [x] **TC-08** `Parse_IgnoreCaseChangesFlag_SetsIgnoreCaseChangesTrue` — `CliParser.Parse(["--ignore-case-changes"]).IgnoreCaseChanges.Should().BeTrue()`.
- [x] **TC-09** Existing `Parse_NoArgs_ReturnsDefaultOptions` gains assertion: `options.IgnoreCaseChanges.Should().BeFalse()`.

*Updates to `HelpTextProviderTests.cs`:*
- [x] **TC-10** `GetHelpText_IncludesIgnoreCaseChangesOption` — `help.Should().Contain("--ignore-case-changes")` and `help.Should().Contain("casing")`.

*General test quality:*
- [x] All new tests follow the TUnit test pattern used in `ReportModelBuilderUnchangedValuesTests.cs` (attribute-based test runner, `Should()` fluent assertions).
- [x] Integration tests (`ReportModelBuilderIgnoreCaseChangesTests.cs`) load `azurerm-case-only-ids-plan.json` from the `TestData` folder, consistent with adjacent test classes.
- [x] Isolation tests for `AzureResourceIdCaseChangeFilter` and `AttributeChangeFilterRegistry` use inline values only — no JSON file dependency.
- [x] All 24 test cases pass (green) with no test framework errors.

**Dependencies:** Task 1 (test data), Tasks 2–13 (source changes)

---

### Task 15: Update README documentation

**Priority:** Medium

**Description:**
Document the `--ignore-case-changes` flag in the README so users discover it alongside existing flags.

**File to modify:**
`README.md`

**Acceptance Criteria:**
- [x] `--ignore-case-changes` is listed in the CLI options table / reference section alongside `--show-unchanged-values`.
- [x] The description explains that it suppresses attribute change rows where before/after are Azure resource IDs that differ only in casing, and mentions Azure ARM API ID-casing noise as the motivating use case.
- [x] The interaction with `--show-unchanged-values` (casing-only Azure ID rows remain hidden even when `--show-unchanged-values` is active) is noted.
- [x] At least one usage example is provided (e.g., `tfplan2md plan.json --ignore-case-changes`).
- [x] The description clarifies that non-Azure-ID attribute values (plain names, numbers, booleans) are unaffected.
- [x] No existing documentation is removed or broken.

**Dependencies:** None (can be done in parallel with or after source changes)

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1 — Test data file** — No dependencies; create the JSON fixture first so integration tests can be validated immediately.
2. **Task 2 — CliParser** — Entry point for the feature; enables end-to-end wiring.
3. **Task 3 — HelpTextProvider** — Companion to Task 2; completes the CLI layer.
4. **Task 4 — ReportModel** — Data carrier; must exist before builder changes and the Build.cs propagation.
5. **Task 5 — Core filter infrastructure** — No dependencies on other tasks; pure infrastructure. Must precede Tasks 6–10.
6. **Task 6 — `IProviderModule` + `ProviderRegistry`** — Depends on Task 5 (`AttributeChangeFilterRegistry`); adds the module registration hook.
7. **Task 7 — `AzureResourceIdCaseChangeFilter`** — Depends on Task 5 (interface/context types); implements the Azure-specific filter logic.
8. **Task 8 — `AzureRMModule` override** — Depends on Tasks 6 and 7; registers the filter into the module.
9. **Task 9 — `ReportModelBuilder` constructor** — Depends on Tasks 4 and 5; adds both backing fields needed by Tasks 10 and 11.
10. **Task 10 — Filter call in `ResourceChanges.cs`** — Depends on Task 9; the single core pipeline change.
11. **Task 11 — `ReportModelBuilder.Build.cs`** — Depends on Tasks 4 and 9; completes model propagation.
12. **Task 12 — `AotScriptObjectMapper`** — Depends on Tasks 4 and 11; exposes the flag to Scriban templates.
13. **Task 13 — `CompositionRoot`** — Depends on Tasks 2, 6, 8, and 9; wires CLI → builder with registered filters. After this step the full pipeline is working end-to-end.
14. **Task 14 — Unit tests** — Depends on all source tasks (1–13); write and run all 24 test cases.
15. **Task 15 — README** — Can be done any time after the source changes are stable; no code dependency.

## Open Questions

None. All requirements, architecture decisions, and test cases are fully specified in the revised documents.
