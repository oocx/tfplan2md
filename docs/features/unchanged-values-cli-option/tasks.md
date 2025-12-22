# Tasks: CLI Option for Unchanged Value Filtering

## Overview

This document outlines the tasks required to implement the `--show-unchanged-values` CLI option, as specified in the [specification](specification.md) and [architecture](architecture.md) documents. The goal is to hide unchanged attributes in the generated Markdown report by default, while providing an opt-in flag to show them.

## Tasks

### Task 1: Update CLI Options and Parser

**Priority:** High

**Description:**
Add the `--show-unchanged-values` flag to the CLI options and update the parser to handle it.

**Acceptance Criteria:**
- [ ] `CliOptions` record includes a `bool ShowUnchangedValues` property.
- [ ] `CliParser` correctly parses `--show-unchanged-values` and sets the property to `true`.
- [ ] `CliParser` sets `ShowUnchangedValues` to `false` by default when the flag is absent.
- [ ] `HelpTextProvider` is updated to include `--show-unchanged-values` in the help output.

**Dependencies:** None

---

### Task 2: Update Report Model and Builder

**Priority:** High

**Description:**
Update the `ReportModel` to store the setting and `ReportModelBuilder` to implement the filtering logic.

**Acceptance Criteria:**
- [ ] `ReportModel` includes a `bool ShowUnchangedValues` property.
- [ ] `ReportModelBuilder` constructor is updated to accept `bool showUnchangedValues`.
- [ ] `ReportModelBuilder.BuildAttributeChanges` filters out attributes where `before == after` when `showUnchangedValues` is `false`.
- [ ] `ReportModelBuilder.BuildAttributeChanges` includes all attributes when `showUnchangedValues` is `true`.
- [ ] Filtering logic correctly handles `null` values (e.g., `null == null` is unchanged).
- [ ] Filtering logic correctly handles sensitive values (if masked, they are still compared based on their masked state or original state as per architecture). *Note: Architecture says filter in Builder, so we compare the values that would be displayed.*

**Dependencies:** Task 1

---

### Task 3: Wire up in Program.cs

**Priority:** Medium

**Description:**
Pass the parsed CLI option to the `ReportModelBuilder` in the main application loop.

**Acceptance Criteria:**
- [ ] `Program.cs` passes `options.ShowUnchangedValues` to the `ReportModelBuilder` constructor.

**Dependencies:** Task 1, Task 2

---

### Task 4: Implement Unit Tests

**Priority:** High

**Description:**
Implement the unit tests defined in the [test plan](test-plan.md) to verify the filtering logic and CLI parsing.

**Acceptance Criteria:**
- [ ] TC-01: `ReportModelBuilder_Build_Default_HidesUnchangedValues` passes.
- [ ] TC-02: `ReportModelBuilder_Build_WithShowUnchangedValues_ShowsAllValues` passes.
- [ ] TC-03: `CliParser_Parse_ShowUnchangedValuesFlag` passes.
- [ ] All existing tests continue to pass.

**Dependencies:** Task 1, Task 2

---

### Task 5: Update Documentation

**Priority:** Medium

**Description:**
Update the project documentation to reflect the new CLI option and the change in default behavior.

**Acceptance Criteria:**
- [ ] `README.md` is updated with the new `--show-unchanged-values` flag and its description.
- [ ] Any other relevant documentation (e.g., usage guides) is updated.

**Dependencies:** Task 1

## Implementation Order

1. **Task 1 & Task 2** - Foundational work for the feature.
2. **Task 4** - Verify the logic with tests before wiring it up.
3. **Task 3** - Wire up the feature in the main application.
4. **Task 5** - Finalize documentation.

## Open Questions

None.
