# Tasks: Terraform Show Output Approximation Tool

## Overview

Create a standalone development tool `tools/Oocx.TfPlan2Md.TerraformShowRenderer` that generates output approximating `terraform show` from Terraform plan JSON files. This tool is used to provide authentic "before tfplan2md" examples for the website.

Reference: [specification.md](specification.md), [architecture.md](architecture.md).

## Tasks

### Task 1: Project Setup and CLI Infrastructure

**Priority:** High

**Description:**
Create the new tool project and implement the command-line interface following the project's established patterns.

**Acceptance Criteria:**
- [x] New project created at `tools/Oocx.TfPlan2Md.TerraformShowRenderer/`.
- [x] Project added to `tfplan2md.slnx`.
- [x] `CLI/CliOptions.cs` defines `--input`, `--output`, `--no-color`, `--help`, and `--version`.
- [x] `CLI/CliParser.cs` correctly parses arguments and handles errors.
- [x] `CLI/HelpTextProvider.cs` provides clear usage instructions.
- [x] `Program.cs` and `TerraformShowRendererApp.cs` implemented with basic flow and exit codes (0-4).
- [x] `VersionProvider.cs` implemented to show the tool version.
- [x] Tool can be run with `--help` and `--version`.

**Dependencies:** None

---

### Task 2: ANSI Styling and Rendering Pipeline

**Priority:** High

**Description:**
Implement the core rendering infrastructure, including ANSI color support and the high-level rendering sequence.

**Acceptance Criteria:**
- [x] `Rendering/AnsiTextWriter.cs` implemented to support `Bold`, `Green`, `Yellow`, `Red`, `Cyan`, `Dim`, and `Reset`.
- [x] `AnsiTextWriter` respects the `--no-color` flag (strips all escape sequences).
- [x] `Rendering/TerraformShowRenderer.cs` implemented with the main rendering loop.
- [x] Static Legend section implemented matching real Terraform output.
- [x] "Terraform will perform the following actions:" header implemented.

**Dependencies:** Task 1

---

### Task 3: Resource Header and Action Mapping

**Priority:** Medium

**Description:**
Implement the logic to map Terraform plan actions to human-readable headers and markers.

**Acceptance Criteria:**
- [x] Correct mapping of `change.actions` to: create (`+`), update (`~`), delete (`-`), replace (`-/+`), and read (`<=`).
- [x] Resource header comment lines implemented: `  # <address> will be <action phrase>`.
- [x] Action phrases match baseline: `will be created`, `will be updated in-place`, `will be destroyed`, `will be read during apply`, `must be replaced`.
- [x] Bold and color styling applied to action phrases (e.g., red+bold for `destroyed` and `replaced`).
- [x] `action_reason` from JSON is rendered in parentheses if present.

**Dependencies:** Task 2

---

### Task 4: Attribute and Diff Rendering

**Priority:** High

**Description:**
Implement the complex logic for rendering attribute changes, including diffs, unknown values, and sensitive data.

**Acceptance Criteria:**
- [ ] `Rendering/ValueRenderer.cs` implemented for formatting JSON values (strings, numbers, booleans, nulls).
- [ ] `Rendering/DiffRenderer.cs` implemented to handle different action types:
    - **Create**: All attributes prefixed with `+`.
    - **Delete**: All attributes prefixed with `-`.
    - **Update**: Changed attributes show `~` and `old -> new`.
    - **Replace**: Shows `-/+` and attribute changes.
    - **Read**: Attributes prefixed with `<=`.
- [ ] `(known after apply)` rendered for `after_unknown` values.
- [ ] `(sensitive value)` rendered for sensitive attributes.
- [ ] "unchanged attributes hidden" comment lines rendered where appropriate.
- [ ] Property ordering from JSON is preserved.

**Dependencies:** Task 3

---

### Task 5: Plan Summary and Output Changes

**Priority:** Medium

**Description:**
Implement the final sections of the output: the plan summary and output changes.

**Acceptance Criteria:**
- [ ] `Plan: X to add, Y to change, Z to destroy.` summary line implemented with correct counts.
- [ ] `Changes to Outputs:` section implemented for non-no-op output changes.
- [ ] Output changes follow similar diff formatting as resources.

**Dependencies:** Task 4

---

### Task 6: Integration and Regression Testing

**Priority:** High

**Description:**
Add automated tests to ensure the tool's output matches the established baselines.

**Acceptance Criteria:**
- [ ] Integration tests added in `tests/Oocx.TfPlan2Md.Tests/TerraformShowRendererTests.cs`.
- [ ] `TC-06`: Output for `plan1.json` matches `plan1.txt`.
- [ ] `TC-07`: Output for `plan2.json` matches `plan2.txt`.
- [ ] `TC-08`: Custom plan with all operations (create, update, delete, replace, read, no-op) verified.
- [ ] `TC-13`: `--no-color` flag verified to produce plain text.
- [ ] `TC-04`, `TC-05`, `TC-10`, `TC-11`, `TC-12`: Error cases and exit codes verified.

**Dependencies:** Task 5

---

### Task 7: Documentation and UAT

**Priority:** Low

**Description:**
Finalize the feature with documentation and manual verification.

**Acceptance Criteria:**
- [ ] Tool usage documented in `README.md` or relevant documentation file.
- [ ] UAT performed as per `docs/features/030-terraform-show-approximation/uat-test-plan.md`.
- [ ] UAT artifact `artifacts/uat-terraform-show-approximation.txt` generated and verified.

**Dependencies:** Task 6

## Implementation Order

1. **Task 1** - Foundational CLI structure.
2. **Task 2** - Core rendering and ANSI support.
3. **Task 3** - Resource headers (easier than full diffs).
4. **Task 4** - Core diff rendering (most complex part).
5. **Task 5** - Finalizing output sections.
6. **Task 6** - Verification against baselines.
7. **Task 7** - Documentation and UAT.

## Open Questions

None.
