# Tasks: Case-Insensitive Attribute Change Filter

## Overview

Implements the `--ignore-case-changes` CLI flag (feature 103) that suppresses attribute change rows where before and after values differ only in letter casing. This eliminates Azure ARM API ID-casing noise from Terraform plan reports. The implementation follows the exact same pipeline as the existing `--show-unchanged-values` flag (feature 014).

Reference: [specification.md](./specification.md) | [architecture.md](./architecture.md) | [test-plan.md](./test-plan.md)

---

## Tasks

### Task 1: Create test data file `azurerm-case-only-ids-plan.json`

**Priority:** High

**Description:**
Create a minimal but complete Terraform plan JSON fixture covering all test scenarios defined in the test plan. This file must exist before any unit tests can be written and run.

**File to create:**
`src/tests/Oocx.TfPlan2Md.TUnit/TestData/azurerm-case-only-ids-plan.json`

**Acceptance Criteria:**
- [ ] File is valid Terraform plan JSON (format_version `"1.2"`, terraform_version `"1.9.0"` or similar).
- [ ] Contains resource `azurerm_role_assignment.casing_only` (action `["update"]`) with:
  - `scope` before: `/subscriptions/ABC123/resourceGroups/my-rg`, after: `/subscriptions/abc123/resourceGroups/my-rg` (casing-only)
  - `role_definition_id` before: `/providers/Microsoft.Authorization/roleDefinitions/XYZ`, after: `/providers/Microsoft.Authorization/roleDefinitions/xyz` (casing-only)
- [ ] Contains resource `azurerm_role_assignment.mixed_changes` (action `["update"]`) with:
  - `scope` before/after same casing-only difference as above
  - `display_name` before: `"My App"`, after: `"My Application"` (genuine change)
- [ ] Contains resource `azurerm_key_vault.null_before` (action `["update"]`) with:
  - `tenant_id` before: `null`, after: `"tenant-abc"` (null before)
- [ ] Contains resource `azurerm_key_vault.null_after` (action `["update"]`) with:
  - `tenant_id` before: `"tenant-abc"`, after: `null` (null after)
- [ ] Contains resource `azurerm_key_vault.numeric_change` (action `["update"]`) with:
  - `soft_delete_retention_days` before: `7`, after: `14` (numeric genuine change)
- [ ] Contains resource `azurerm_key_vault.unchanged` (action `["update"]`) with:
  - `name` before: `"my-vault"`, after: `"my-vault"` (ordinal-equal, unchanged)
- [ ] File is loadable by the existing `TerraformPlanParser` without errors.

**Dependencies:** None

**Notes:**
Follow the structure of an existing test data file such as `src/tests/Oocx.TfPlan2Md.TUnit/TestData/` to match the expected JSON shape. Each resource must appear in `resource_changes` with `change.before`, `change.after`, `change.actions`, and `type`/`address`/`name` fields. Attributes not relevant to a test scenario can be omitted or set to stable values. The `before_sensitive` and `after_sensitive` fields should be set to `false` for all attributes (no sensitive masking needed).

---

### Task 2: Add `IgnoreCaseChanges` to `CliOptions` record and `CliParser.Parse()`

**Priority:** High

**Description:**
Extend the CLI layer to recognise the `--ignore-case-changes` flag and store its value in the parsed options object. This is the entry point for the entire feature.

**File to modify:**
`src/Oocx.TfPlan2Md/CLI/CliParser.cs`

**Acceptance Criteria:**
- [ ] `CliOptions` record has a new property:
  ```csharp
  /// <summary>
  /// Gets a value indicating whether attribute change rows where before and after values
  /// differ only in casing are suppressed.
  /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
  /// </summary>
  public bool IgnoreCaseChanges { get; init; }
  ```
  Positioned after the `ShowUnchangedValues` property (line ~76).
- [ ] `CliParser.Parse()` declares a local `var ignoreCaseChanges = false;` alongside the other boolean locals.
- [ ] The `switch` statement inside `Parse()` contains a new case after `"--show-unchanged-values"`:
  ```csharp
  case "--ignore-case-changes":
      ignoreCaseChanges = true;
      break;
  ```
- [ ] The `return new CliOptions { ... }` initializer includes `IgnoreCaseChanges = ignoreCaseChanges`.
- [ ] Passing an unknown flag still throws `CliParseException` (default case untouched).
- [ ] Default value of `IgnoreCaseChanges` is `false` (no flag → unchanged behavior).

**Dependencies:** None

---

### Task 3: Add `--ignore-case-changes` to `HelpTextProvider`

**Priority:** High

**Description:**
Add an entry for `--ignore-case-changes` in the options array so it appears in the CLI help output.

**File to modify:**
`src/Oocx.TfPlan2Md/CLI/HelpTextProvider.cs`

**Acceptance Criteria:**
- [ ] A new tuple is added to the `options` array after the `"--show-unchanged-values"` entry:
  ```csharp
  ("--ignore-case-changes", "Suppress attribute changes where before/after values differ only in casing."),
  ```
- [ ] `HelpTextProvider.GetHelpText()` output contains the string `"--ignore-case-changes"`.
- [ ] `HelpTextProvider.GetHelpText()` output contains the word `"casing"` (description references casing).

**Dependencies:** None

---

### Task 4: Add `IgnoreCaseChanges` property to `ReportModel`

**Priority:** High

**Description:**
Add the flag value to the report data model so templates can read it (e.g., to conditionally render a banner or tooltip).

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModel.cs`

**Acceptance Criteria:**
- [ ] `ReportModel` class has a new property after `ShowUnchangedValues` (line ~88):
  ```csharp
  /// <summary>
  /// Gets a value indicating whether attribute change rows where before and after values
  /// differ only in casing are suppressed.
  /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
  /// </summary>
  public required bool IgnoreCaseChanges { get; init; }
  ```
- [ ] Property is `required` and `bool`, consistent with `ShowUnchangedValues` and `HideMetadata`.

**Dependencies:** None

---

### Task 5: Add `ignoreCaseChanges` parameter and backing field to `ReportModelBuilder`

**Priority:** High

**Description:**
Extend the `ReportModelBuilder` primary constructor to accept and store the new option, making it available to the filtering logic in the partial class files.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.cs`

**Acceptance Criteria:**
- [ ] The primary constructor parameter list has a new parameter after `showUnchangedValues`:
  ```csharp
  bool ignoreCaseChanges = false,
  ```
- [ ] A backing field is declared after `_showUnchangedValues`:
  ```csharp
  /// <summary>
  /// Indicates whether attribute change rows differing only in casing should be suppressed.
  /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
  /// </summary>
  private readonly bool _ignoreCaseChanges = ignoreCaseChanges;
  ```
- [ ] The XML doc comment on the constructor (the `<param>` block or `<remarks>`) is updated to reference `ignoreCaseChanges` and feature 103.
- [ ] All existing constructor parameters and backing fields are untouched.

**Dependencies:** Task 4 (ReportModel property added; builder must eventually populate it)

---

### Task 6: Implement the casing filter guard in `ReportModelBuilder.ResourceChanges.cs`

**Priority:** High

**Description:**
Add the `isCasingOnlyChange` guard inside `BuildAttributeChanges()` immediately after the `valuesEqual` computation. This is the core filtering logic of the feature.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.ResourceChanges.cs`

**Acceptance Criteria:**
- [ ] After the line `var valuesEqual = string.Equals(beforeValue, afterValue, StringComparison.Ordinal);` (currently line ~121), and after any existing call that mutates `valuesEqual` (the `ref bool valuesEqual` call at ~130), insert:
  ```csharp
  var isCasingOnlyChange = _ignoreCaseChanges
      && beforeValue is not null
      && afterValue is not null
      && !valuesEqual
      && string.Equals(beforeValue, afterValue, StringComparison.OrdinalIgnoreCase);

  if (isCasingOnlyChange)
  {
      continue;
  }
  ```
- [ ] The existing `if (!_showUnchangedValues && valuesEqual) { continue; }` guard remains **unchanged** and appears **after** the new `isCasingOnlyChange` block.
- [ ] When `_ignoreCaseChanges` is `false`, `isCasingOnlyChange` is always `false` and behavior is identical to pre-change (no regression).
- [ ] When `_ignoreCaseChanges` is `true`, a row where `beforeValue` and `afterValue` differ only in casing is skipped unconditionally (even if `_showUnchangedValues` is `true`).
- [ ] When `beforeValue` or `afterValue` is `null`, `isCasingOnlyChange` is `false` and the row is processed normally.
- [ ] Uses `StringComparison.OrdinalIgnoreCase` for the case-insensitive comparison (not `CurrentCultureIgnoreCase`).

**Dependencies:** Task 5

---

### Task 7: Populate `IgnoreCaseChanges` in `ReportModelBuilder.Build.cs`

**Priority:** High

**Description:**
Set the `IgnoreCaseChanges` property on the `ReportModel` returned from `Build()`.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/ReportModelBuilder.Build.cs`

**Acceptance Criteria:**
- [ ] Inside the `return new ReportModel { ... }` initializer in `Build()`, add:
  ```csharp
  IgnoreCaseChanges = _ignoreCaseChanges,
  ```
  Positioned near the other boolean flag properties (`ShowUnchangedValues`, `ShowSensitive`, `HideMetadata`).
- [ ] The build compiles without error (the `required` `IgnoreCaseChanges` property is now satisfied).

**Dependencies:** Task 4 (`IgnoreCaseChanges` property on `ReportModel`), Task 5 (`_ignoreCaseChanges` backing field)

---

### Task 8: Expose `ignore_case_changes` as a Scriban template variable

**Priority:** High

**Description:**
Map `ReportModel.IgnoreCaseChanges` to the Scriban script object so custom templates can conditionally use it.

**File to modify:**
`src/Oocx.TfPlan2Md/MarkdownGeneration/AotScriptObjectMapper.cs`

**Acceptance Criteria:**
- [ ] In `MapReportModel()`, add the following line after the `show_unchanged_values` entry (around line 40):
  ```csharp
  scriptObject["ignore_case_changes"] = model.IgnoreCaseChanges;
  ```
- [ ] The key is `"ignore_case_changes"` (snake_case, matching the convention of `show_unchanged_values`, `show_sensitive`, etc.).
- [ ] A Scriban template using `{{ ignore_case_changes }}` will render `"true"` when the flag is active.

**Dependencies:** Task 4, Task 7

---

### Task 9: Wire up `IgnoreCaseChanges` in `CompositionRoot.cs`

**Priority:** High

**Description:**
Pass the parsed CLI option to `ReportModelBuilder` so the end-to-end pipeline is complete.

**File to modify:**
`src/Oocx.TfPlan2Md/CompositionRoot.cs`

**Acceptance Criteria:**
- [ ] In `CreateReportModelBuilder()`, the `ReportModelBuilder` constructor call includes:
  ```csharp
  ignoreCaseChanges: options.IgnoreCaseChanges,
  ```
  Positioned after the `showUnchangedValues: options.ShowUnchangedValues` named argument.
- [ ] No other changes to `CompositionRoot.cs`.
- [ ] The full CLI pipeline (`tfplan2md plan.json --ignore-case-changes`) correctly propagates the flag through to `BuildAttributeChanges()`.

**Dependencies:** Task 2 (`CliOptions.IgnoreCaseChanges`), Task 5 (`ReportModelBuilder` constructor parameter)

---

### Task 10: Write unit tests

**Priority:** High

**Description:**
Implement all 14 test cases defined in the test plan. Create a new test class and update two existing test files.

**Files to create / modify:**

**Create:**
`src/tests/Oocx.TfPlan2Md.TUnit/MarkdownGeneration/ReportModelBuilderIgnoreCaseChangesTests.cs`

**Modify:**
- `src/tests/Oocx.TfPlan2Md.TUnit/CLI/CliParserTests.cs`
- `src/tests/Oocx.TfPlan2Md.TUnit/CLI/HelpTextProviderTests.cs`

**Acceptance Criteria:**

*New file `ReportModelBuilderIgnoreCaseChangesTests.cs` contains:*
- [ ] **TC-01** `Build_IgnoreCaseChangesFalse_IncludesCasingOnlyRows` — `ignoreCaseChanges: false` → casing-only rows present in `AttributeChanges`.
- [ ] **TC-02** `Build_IgnoreCaseChangesTrue_AllCasingOnly_AttributeChangesEmpty` — `ignoreCaseChanges: true` on `azurerm_role_assignment.casing_only` → `AttributeChanges.Count == 0`.
- [ ] **TC-03** `Build_IgnoreCaseChangesTrue_MixedChanges_RetainsGenuineChanges` — `ignoreCaseChanges: true` on `azurerm_role_assignment.mixed_changes` → `display_name` present, `scope` absent.
- [ ] **TC-04** `Build_IgnoreCaseChangesTrue_NullBeforeValue_RowIsShown` — `azurerm_key_vault.null_before` → `tenant_id` row present (not suppressed).
- [ ] **TC-05** `Build_IgnoreCaseChangesTrue_NullAfterValue_RowIsShown` — `azurerm_key_vault.null_after` → `tenant_id` row present (not suppressed).
- [ ] **TC-06** `Build_IgnoreCaseChangesTrue_NumericAttributeChange_RowIsShown` — `azurerm_key_vault.numeric_change` → `soft_delete_retention_days` row present.
- [ ] **TC-07** `Build_IgnoreCaseChangesTrue_AndShowUnchangedValues_CasingRowsStillSuppressed` — `ignoreCaseChanges: true, showUnchangedValues: true` → casing-only rows absent, ordinal-equal rows present (from `showUnchangedValues`), genuine changes present.
- [ ] **TC-11** `Build_Default_IgnoreCaseChangesFalseInModel` — `model.IgnoreCaseChanges.Should().BeFalse()` when no `ignoreCaseChanges` arg.
- [ ] **TC-12** `Build_WithIgnoreCaseChangesTrue_ModelReflectsFlag` — `model.IgnoreCaseChanges.Should().BeTrue()`.
- [ ] **TC-13** `Build_IgnoreCaseChangesTrue_OrdinallyEqualValues_BehavesLikeUnchanged` — ordinal-equal row in `azurerm_key_vault.unchanged` absent when `showUnchangedValues: false`.
- [ ] **TC-14** `Render_IgnoreCaseChangesTrue_ScribanVariableIsTrue` — Scriban template `{{ ignore_case_changes }}` renders `"true"` when flag is active.

*Updates to `CliParserTests.cs`:*
- [ ] **TC-08** `Parse_IgnoreCaseChangesFlag_SetsIgnoreCaseChangesTrue` — `CliParser.Parse(["--ignore-case-changes"]).IgnoreCaseChanges.Should().BeTrue()`.
- [ ] **TC-09** Existing `Parse_NoArgs_ReturnsDefaultOptions` test gains assertion: `options.IgnoreCaseChanges.Should().BeFalse()`.

*Updates to `HelpTextProviderTests.cs`:*
- [ ] **TC-10** `GetHelpText_IncludesIgnoreCaseChangesOption` — `help.Should().Contain("--ignore-case-changes")` and `help.Should().Contain("casing")`.

*General test quality:*
- [ ] All new tests follow the TUnit test pattern used in the existing `ReportModelBuilderUnchangedValuesTests.cs` (attribute-based test runner, `Should()` fluent assertions).
- [ ] Test class for builder tests uses a `[ClassDataSource]` or fixture that loads `azurerm-case-only-ids-plan.json` from the `TestData` folder, consistent with adjacent test classes.
- [ ] All tests pass (green) with no test framework errors.

**Dependencies:** Task 1 (test data), Tasks 2–9 (source changes)

---

### Task 11: Update README documentation

**Priority:** Medium

**Description:**
Document the `--ignore-case-changes` flag in the README so users discover it alongside existing flags.

**File to modify:**
`README.md`

**Acceptance Criteria:**
- [ ] `--ignore-case-changes` is listed in the CLI options table / reference section alongside `--show-unchanged-values`.
- [ ] The description explains that it suppresses attribute change rows where before/after differ only in casing, and mentions Azure ARM API ID-casing as the motivating use case.
- [ ] The interaction with `--show-unchanged-values` (casing-only rows remain hidden even when `--show-unchanged-values` is active) is noted.
- [ ] At least one usage example is provided (e.g., `tfplan2md plan.json --ignore-case-changes`).
- [ ] No existing documentation is removed or broken.

**Dependencies:** None (can be done in parallel with or after source changes)

---

## Implementation Order

Recommended sequence for implementation:

1. **Task 1 — Test data file** — No dependencies; create the JSON fixture first so tests can be written and validated immediately.
2. **Task 2 — CliParser** — Entry point for the feature; enables end-to-end wiring.
3. **Task 3 — HelpTextProvider** — Companion to Task 2; complete the CLI layer.
4. **Task 4 — ReportModel** — Data carrier; must exist before builder changes and the Build.cs propagation.
5. **Task 5 — ReportModelBuilder constructor** — Depends on Task 4 (model property); adds the backing field used by Tasks 6 and 7.
6. **Task 6 — Filter logic (ResourceChanges)** — Core behaviour; depends on Task 5 (`_ignoreCaseChanges` field).
7. **Task 7 — ReportModelBuilder.Build.cs** — Depends on Tasks 4 and 5; completes the model propagation path.
8. **Task 8 — AotScriptObjectMapper** — Depends on Tasks 4 and 7; exposes the flag to Scriban templates.
9. **Task 9 — CompositionRoot** — Depends on Tasks 2 and 5; wires CLI → builder. After this step the full pipeline is working end-to-end.
10. **Task 10 — Unit tests** — Depends on all source tasks (1–9); write and run all 14 test cases.
11. **Task 11 — README** — Can be done any time after the source changes are stable; no code dependency.

## Open Questions

None. All requirements, architecture decisions, and test cases are fully specified.
