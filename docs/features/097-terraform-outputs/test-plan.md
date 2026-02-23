# Test Plan: Terraform Outputs

## Overview

This test plan validates the Terraform Outputs support feature (Feature 097), which adds rendering of Terraform outputs in tfplan2md reports. The feature parses output changes from the Terraform plan JSON, correlates them with configuration metadata, and renders them as tables with intelligent positioning (module outputs within modules, global outputs at the end).

**References:**
- Feature specification: `docs/features/097-terraform-outputs/specification.md`
- Architecture design: `docs/features/097-terraform-outputs/architecture.md`
- Testing strategy: `docs/testing-strategy.md`

## Test Coverage Matrix

| Acceptance Criterion | Test Case(s) | Test Type |
|---------------------|--------------|-----------|
| Outputs are parsed from Terraform plan JSON | TC-01, TC-02 | Unit |
| Module outputs appear immediately after their module's resource changes | TC-17, TC-18 | Integration |
| Global/root outputs appear in a dedicated section after all resource changes | TC-19, TC-20 | Integration |
| Output tables have 4 columns: Name, Description, Sensitive, Value | TC-03, TC-21 | Unit, Integration |
| Output names are code-formatted (backticks) | TC-21 | Snapshot |
| Output descriptions are plain text (or `-` if absent) | TC-04, TC-21 | Unit, Snapshot |
| "Sensitive" column shows `Yes` for sensitive outputs, `-` otherwise | TC-05, TC-21 | Unit, Snapshot |
| Output values are code-formatted (backticks) when displayed | TC-21 | Snapshot |
| Sensitive output values show `(sensitive value)` by default | TC-06, TC-22 | Unit, Snapshot |
| Computed values show `(known after apply)` when `after_unknown` is `true` | TC-07, TC-23 | Unit, Snapshot |
| `--show-sensitive` flag reveals actual sensitive values | TC-08, TC-22 | Unit, Snapshot |
| Display name mappings apply to output values automatically | TC-09, TC-24 | Unit, Snapshot |
| Outputs are ordered alphabetically by name within each section | TC-10, TC-21 | Unit, Snapshot |
| `create` action outputs show `after` value | TC-11, TC-25 | Unit, Snapshot |
| `update` action outputs show `after` value | TC-12, TC-25 | Unit, Snapshot |
| `delete` action outputs show `before` value | TC-13, TC-25 | Unit, Snapshot |
| `no-op` action outputs show current value | TC-14, TC-25 | Unit, Snapshot |
| Plans with no outputs omit the Outputs section entirely | TC-15, TC-26 | Unit, Snapshot |
| Module outputs use 4th-level header (`#### Outputs`) | TC-18, TC-21 | Integration, Snapshot |
| Global outputs use 2nd-level header (`## Outputs`) | TC-20, TC-21 | Integration, Snapshot |

## User Acceptance Scenarios

> **Purpose**: For user-facing features (especially rendering changes), define scenarios for manual Maintainer review via Test PRs in GitHub and Azure DevOps. These help catch rendering bugs and validate real-world usage before merge.

### Scenario 1: View Module and Global Outputs with Sensitive Masking

**User Goal**: Review Terraform plan outputs to understand what data will be exported, with sensitive values properly masked to prevent credential leaks in PR reviews.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments/description.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- Module outputs appear immediately after their module's resource changes
- Global outputs appear in a dedicated section after all resources
- Tables have 4 clear columns: Name, Description, Sensitive, Value
- Sensitive values display as `(sensitive value)` (not code-formatted)
- Non-sensitive values display with code formatting (backticks)
- Computed values display as `(known after apply)` (not code-formatted)
- Outputs are alphabetically sorted within each section
- Azure resource IDs in output values are formatted with display names

**Success Criteria**:
- [ ] Output tables render correctly in GitHub Markdown
- [ ] Output tables render correctly in Azure DevOps Markdown
- [ ] Sensitive values are properly masked by default
- [ ] Module outputs are positioned within their module sections
- [ ] Global outputs are positioned after all resource changes
- [ ] Computed values are clearly indicated
- [ ] Display name mappings apply to output values

**Feedback Opportunities**:
- Is the 4-column table format clear and easy to scan?
- Does the positioning (module vs global) make sense?
- Is it obvious which outputs are sensitive and which are computed?
- Are the display name mappings helpful in output values?

---

### Scenario 2: Review Sensitive Outputs with `--show-sensitive` Flag

**User Goal**: When working in a secure environment, optionally reveal sensitive output values to verify their correctness before applying the plan.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments with `--show-sensitive` flag.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description with `--show-sensitive` flag.

**Expected Output**:
- Same structure as Scenario 1, but with actual sensitive values displayed
- Sensitive values are code-formatted (backticks) when revealed
- "Sensitive" column still shows `Yes` for transparency
- Non-sensitive outputs unchanged

**Success Criteria**:
- [ ] `--show-sensitive` flag reveals actual sensitive values
- [ ] Sensitive values are code-formatted when revealed
- [ ] "Sensitive" column still shows `Yes` for transparency
- [ ] Feature doesn't accidentally mask non-sensitive values

**Feedback Opportunities**:
- Is the flag behavior intuitive?
- Is it clear which outputs remain sensitive vs non-sensitive?
- Does revealing sensitive values pose any security concerns in the test environment?

---

### Scenario 3: Outputs with Display Name Mappings

**User Goal**: See Azure resource IDs, principal IDs, and other identifiers in output values with human-readable display names automatically applied.

**Test PR Context**:
- **GitHub**: Verify rendering in GitHub PR comments.
- **Azure DevOps**: Verify rendering in Azure DevOps PR description.

**Expected Output**:
- Output values containing Azure resource IDs show formatted display names
- Output values with principal IDs show user/group/service principal names
- Output values with subscription GUIDs show subscription display names
- Output values with role definition IDs show role names
- Formatting matches the same style used for resource attributes

**Success Criteria**:
- [ ] Azure resource ID formatting applies to output values
- [ ] Principal mappings work for outputs
- [ ] Subscription display names work for outputs
- [ ] Role display names work for outputs
- [ ] Formatting is consistent with resource attributes

**Feedback Opportunities**:
- Do the display name mappings make outputs more readable?
- Are there any identifiers that should be mapped but aren't?
- Is the formatting consistent with expectations?

## Test Cases

### TC-01: Parse Output Changes from Plan JSON

**Type:** Unit

**Description:**
Verify that `TerraformPlanParser` correctly parses `output_changes` from the plan JSON into `OutputChange` records.

**Preconditions:**
- Valid Terraform plan JSON with `output_changes` section

**Test Steps:**
1. Create test plan JSON with `output_changes` containing multiple outputs with different actions
2. Parse the JSON using `TerraformPlanParser`
3. Verify `plan.OutputChanges` is not null
4. Verify correct number of outputs parsed
5. Verify each output has correct properties (actions, before, after, after_unknown, sensitivity markers)

**Expected Result:**
- `OutputChanges` dictionary contains correct number of entries
- Output names are dictionary keys
- Each `OutputChange` has expected properties populated correctly

**Test Data:**
- `TestData/outputs-basic-plan.json` - Plan with simple outputs (create, update, delete, no-op actions)

---

### TC-02: Correlate Output Metadata from Configuration

**Type:** Unit

**Description:**
Verify that output metadata (description, sensitive flag) is correctly extracted from the `configuration.root_module.outputs` and `configuration.root_module.modules[].outputs` sections.

**Preconditions:**
- Plan JSON with both `output_changes` and `configuration` sections

**Test Steps:**
1. Create test plan with outputs that have descriptions and sensitive flags
2. Build `ReportModel` using `ReportModelBuilder`
3. Verify output models include correct descriptions
4. Verify sensitive flags are correctly detected
5. Verify module address correlation is correct

**Expected Result:**
- Output descriptions match configuration metadata
- Sensitive flags are correctly derived from configuration or `after_sensitive`
- Module outputs are associated with correct module addresses
- Global outputs have empty module address

**Test Data:**
- `TestData/outputs-with-metadata-plan.json` - Plan with outputs containing descriptions and sensitivity

---

### TC-03: Build OutputChangeModel with All Properties

**Type:** Unit

**Description:**
Verify that `ReportModelBuilder` creates `OutputChangeModel` instances with all required properties correctly populated.

**Preconditions:**
- Parsed `TerraformPlan` with outputs

**Test Steps:**
1. Create test plan with diverse outputs (different actions, some sensitive, some computed)
2. Build report model
3. Verify each `OutputChangeModel` has:
   - Correct name
   - Correct description (or null)
   - Correct sensitivity flag
   - Correct action (create, update, delete, no-op)
   - Correct value (before or after depending on action)
   - Correct `IsComputed` flag
   - Correct `IsMasked` flag (based on sensitivity and `showSensitive` setting)
   - Correct module address

**Expected Result:**
- All `OutputChangeModel` properties are correctly populated
- Value selection logic works correctly (after for create/update/no-op, before for delete)
- Masking flag is pre-computed correctly

**Test Data:**
- `TestData/outputs-diverse-actions-plan.json` - Plan with various output configurations

---

### TC-04: Handle Missing Output Descriptions

**Type:** Unit

**Description:**
Verify that outputs without descriptions in the configuration result in `null` description in the model (rendered as `-` in template).

**Preconditions:**
- Plan with outputs missing description field in configuration

**Test Steps:**
1. Create test plan where some outputs have descriptions, others don't
2. Build report model
3. Verify outputs without descriptions have `null` in `Description` property

**Expected Result:**
- Outputs with descriptions have non-null `Description`
- Outputs without descriptions have `null` `Description`
- No exceptions thrown for missing descriptions

**Test Data:**
- `TestData/outputs-no-description-plan.json` - Plan with outputs lacking descriptions

---

### TC-05: Detect Sensitive Outputs from Multiple Sources

**Type:** Unit

**Description:**
Verify sensitivity detection follows the correct precedence: `after_sensitive` > `before_sensitive` > `configuration.sensitive` > default `false`.

**Preconditions:**
- Plan with outputs having sensitivity markers in different locations

**Test Steps:**
1. Create test plan with:
   - Output with `after_sensitive: true`
   - Output with `before_sensitive: true` (delete action)
   - Output with `configuration.*.sensitive: true`
   - Output with no sensitivity markers (default false)
2. Build report model
3. Verify `IsSensitive` flags are set correctly based on precedence

**Expected Result:**
- Runtime sensitivity (`after_sensitive`, `before_sensitive`) takes precedence over configuration
- Configuration sensitivity is used as fallback
- Default is `false` when no sensitivity information exists

**Test Data:**
- `TestData/outputs-sensitivity-sources-plan.json` - Plan with various sensitivity configurations

---

### TC-06: Mask Sensitive Values by Default

**Type:** Unit

**Description:**
Verify that sensitive output values result in `IsMasked = true` when `showSensitive` flag is `false` (default).

**Preconditions:**
- Plan with sensitive outputs
- `ReportModelBuilder` with `showSensitive: false`

**Test Steps:**
1. Create test plan with sensitive outputs
2. Build report model with `showSensitive: false`
3. Verify sensitive outputs have `IsMasked = true`
4. Verify non-sensitive outputs have `IsMasked = false`

**Expected Result:**
- Sensitive outputs have `IsMasked = true`
- Non-sensitive outputs have `IsMasked = false`
- Computed sensitive outputs have `IsMasked = true` (sensitivity takes precedence)

**Test Data:**
- `TestData/outputs-sensitive-plan.json` - Plan with mix of sensitive and non-sensitive outputs

---

### TC-07: Detect Computed (Known After Apply) Values

**Type:** Unit

**Description:**
Verify that outputs with `after_unknown: true` have `IsComputed = true` in the model.

**Preconditions:**
- Plan with computed output values

**Test Steps:**
1. Create test plan with outputs where `after_unknown: true`
2. Build report model
3. Verify computed outputs have `IsComputed = true`
4. Verify non-computed outputs have `IsComputed = false`

**Expected Result:**
- Outputs with `after_unknown: true` have `IsComputed = true`
- Outputs with known values have `IsComputed = false`

**Test Data:**
- `TestData/outputs-computed-plan.json` - Plan with computed output values

---

### TC-08: Reveal Sensitive Values with `--show-sensitive` Flag

**Type:** Unit

**Description:**
Verify that sensitive output values have `IsMasked = false` when `showSensitive` flag is `true`.

**Preconditions:**
- Plan with sensitive outputs
- `ReportModelBuilder` with `showSensitive: true`

**Test Steps:**
1. Create test plan with sensitive outputs
2. Build report model with `showSensitive: true`
3. Verify sensitive outputs have `IsMasked = false`
4. Verify sensitive outputs still have `IsSensitive = true` (for transparency)

**Expected Result:**
- Sensitive outputs have `IsMasked = false` when `showSensitive: true`
- `IsSensitive` flag remains `true` for transparency
- Non-sensitive outputs unchanged

**Test Data:**
- `TestData/outputs-sensitive-plan.json` - Plan with sensitive outputs

---

### TC-09: Apply Display Name Mappings to Output Values

**Type:** Unit

**Description:**
Verify that output values go through the existing value formatting pipeline and receive display name mappings (Azure resource IDs, principals, etc.).

**Preconditions:**
- Plan with outputs containing Azure resource IDs, principal IDs, subscription IDs
- Principal mapper configured with test mappings
- Value formatter registry with Azure formatters

**Test Steps:**
1. Create test plan with outputs containing:
   - Azure resource ID (`/subscriptions/.../resourceGroups/.../providers/...`)
   - Principal ID (user/group/service principal GUID)
   - Subscription ID (GUID)
   - Role definition ID (GUID)
2. Build report model with principal mapper and value formatter registry
3. Render markdown
4. Verify output values are formatted with display names

**Expected Result:**
- Azure resource IDs show formatted display names (e.g., "Key Vault `kv-name` in resource group `rg-name`")
- Principal IDs show user/group/service principal names
- Subscription IDs show subscription display names
- Role definition IDs show role names
- Formatting matches resource attribute formatting

**Test Data:**
- `TestData/outputs-with-azure-ids-plan.json` - Plan with outputs containing Azure identifiers
- `TestData/test-principals.json` - Principal mappings for testing

---

### TC-10: Order Outputs Alphabetically

**Type:** Unit

**Description:**
Verify that outputs are sorted alphabetically by name within each section (module vs global).

**Preconditions:**
- Plan with multiple outputs in non-alphabetical order

**Test Steps:**
1. Create test plan with outputs named: `zebra`, `apple`, `mango`
2. Build report model
3. Verify outputs are ordered: `apple`, `mango`, `zebra`
4. Verify ordering is case-insensitive (ordinal)

**Expected Result:**
- Outputs are sorted alphabetically by name
- Ordering uses `StringComparer.Ordinal` (case-sensitive, consistent)
- Module outputs and global outputs each sorted independently

**Test Data:**
- `TestData/outputs-unordered-plan.json` - Plan with outputs in non-alphabetical order

---

### TC-11: Create Action Shows After Value

**Type:** Unit

**Description:**
Verify that outputs with `create` action use the `after` value in the model.

**Preconditions:**
- Plan with output having `actions: ["create"]`

**Test Steps:**
1. Create test plan with create action output
2. Build report model
3. Verify model's `Value` property equals `after` value from plan
4. Verify `before` value is ignored

**Expected Result:**
- `OutputChangeModel.Value` equals `after` value
- Action is `"create"`

**Test Data:**
- `TestData/outputs-create-action-plan.json` - Plan with create action output

---

### TC-12: Update Action Shows After Value

**Type:** Unit

**Description:**
Verify that outputs with `update` action use the `after` value in the model.

**Preconditions:**
- Plan with output having `actions: ["update"]`

**Test Steps:**
1. Create test plan with update action output (different before and after values)
2. Build report model
3. Verify model's `Value` property equals `after` value from plan
4. Verify `before` value is not used

**Expected Result:**
- `OutputChangeModel.Value` equals `after` value (not `before`)
- Action is `"update"`

**Test Data:**
- `TestData/outputs-update-action-plan.json` - Plan with update action output

---

### TC-13: Delete Action Shows Before Value

**Type:** Unit

**Description:**
Verify that outputs with `delete` action use the `before` value in the model.

**Preconditions:**
- Plan with output having `actions: ["delete"]`

**Test Steps:**
1. Create test plan with delete action output
2. Build report model
3. Verify model's `Value` property equals `before` value from plan
4. Verify `after` value is ignored (typically null for delete)

**Expected Result:**
- `OutputChangeModel.Value` equals `before` value
- Action is `"delete"`

**Test Data:**
- `TestData/outputs-delete-action-plan.json` - Plan with delete action output

---

### TC-14: No-op Action Shows Current Value

**Type:** Unit

**Description:**
Verify that outputs with `no-op` action show the current value (before and after are identical).

**Preconditions:**
- Plan with output having `actions: ["no-op"]`

**Test Steps:**
1. Create test plan with no-op action output (before equals after)
2. Build report model
3. Verify model's `Value` property equals the value (either before or after, they're the same)
4. Verify action is `"no-op"`

**Expected Result:**
- `OutputChangeModel.Value` equals the unchanged value
- Action is `"no-op"`
- No-op outputs are included (unlike resources, users want to see all outputs)

**Test Data:**
- `TestData/outputs-noop-action-plan.json` - Plan with no-op action output

---

### TC-15: No Outputs Results in Empty Collections

**Type:** Unit

**Description:**
Verify that plans without any outputs result in empty output collections (not null, no errors).

**Preconditions:**
- Plan JSON without `output_changes` section or with empty `output_changes`

**Test Steps:**
1. Create test plan without outputs
2. Build report model
3. Verify `GlobalOutputs` is empty list (not null)
4. Verify all `ModuleChangeGroup.Outputs` are empty lists (not null)
5. Verify no exceptions thrown

**Expected Result:**
- `ReportModel.GlobalOutputs` is empty list
- All `ModuleChangeGroup.Outputs` are empty lists
- No exceptions or errors
- Template rendering omits output sections entirely

**Test Data:**
- `TestData/no-outputs-plan.json` - Plan with no outputs

---

### TC-16: Handle Modules with Only Outputs (No Resources)

**Type:** Unit

**Description:**
Verify that modules containing only outputs (no resource changes) still create `ModuleChangeGroup` entries so outputs are rendered.

**Preconditions:**
- Plan with module that has outputs but no resource changes

**Test Steps:**
1. Create test plan where `module.example` has outputs but no resources in `resource_changes`
2. Build report model
3. Verify `ModuleChangeGroup` exists for `module.example`
4. Verify `ModuleChangeGroup.Changes` is empty
5. Verify `ModuleChangeGroup.Outputs` contains the outputs

**Expected Result:**
- Module with only outputs creates `ModuleChangeGroup`
- `Changes` property is empty list
- `Outputs` property contains module outputs
- Module section will render with outputs

**Test Data:**
- `TestData/outputs-only-module-plan.json` - Plan with module having only outputs

---

### TC-17: Module Outputs Positioned After Module Resources

**Type:** Integration

**Description:**
Verify that module outputs appear immediately after the module's resource changes in the rendered markdown.

**Preconditions:**
- Plan with module containing both resource changes and outputs

**Test Steps:**
1. Create test plan with `module.database` containing:
   - Resource: `azurerm_postgresql_server.main` (create)
   - Outputs: `connection_string`, `database_id`
2. Render full markdown report
3. Verify markdown structure:
   - Module header (`### Module: \`module.database\``)
   - Resource details (collapsible section)
   - Output section header (`#### Outputs`)
   - Output table with module outputs
   - No global outputs section interference

**Expected Result:**
- Module outputs appear immediately after module's last resource
- Output section uses 4th-level header (`####`)
- Module outputs are separate from global outputs
- Markdown structure is valid and readable

**Test Data:**
- `TestData/module-with-outputs-plan.json` - Plan with module outputs

---

### TC-18: Multiple Modules Each with Outputs

**Type:** Integration

**Description:**
Verify that multiple modules each show their own outputs in their respective module sections.

**Preconditions:**
- Plan with multiple modules, each having outputs

**Test Steps:**
1. Create test plan with:
   - `module.network` with outputs: `vnet_id`, `subnet_ids`
   - `module.database` with outputs: `connection_string`, `fqdn`
   - `module.app` with outputs: `app_url`, `app_id`
2. Render full markdown report
3. Verify each module section contains its own outputs
4. Verify outputs don't leak between modules

**Expected Result:**
- Each module section shows only its own outputs
- Outputs appear after each module's resources
- No cross-contamination between modules
- Alphabetical ordering maintained within each module

**Test Data:**
- `TestData/multi-module-outputs-plan.json` - Plan with multiple modules having outputs

---

### TC-19: Global Outputs Positioned After All Modules

**Type:** Integration

**Description:**
Verify that global/root outputs appear in a dedicated section after all resource changes and module sections.

**Preconditions:**
- Plan with global outputs and module resource changes

**Test Steps:**
1. Create test plan with:
   - Module resources and outputs
   - Global outputs: `project_id`, `repository_url`
2. Render full markdown report
3. Verify global outputs section:
   - Appears after all module sections
   - Uses 2nd-level header (`## Outputs`)
   - Contains only root-level outputs (not module outputs)

**Expected Result:**
- Global outputs section at end of report
- Section uses 2nd-level header (`## Outputs`)
- Contains only global outputs
- Positioned before debug section (if present)

**Test Data:**
- `TestData/global-outputs-plan.json` - Plan with global outputs

---

### TC-20: Mixed Module and Global Outputs

**Type:** Integration

**Description:**
Verify correct positioning when plan contains both module outputs and global outputs.

**Preconditions:**
- Plan with both module outputs and global outputs

**Test Steps:**
1. Create test plan with:
   - `module.database` with outputs
   - Global outputs: `environment`, `deployment_url`
2. Render full markdown report
3. Verify:
   - Module outputs appear within module section (4th-level header)
   - Global outputs appear at end (2nd-level header)
   - Both sections use correct headers and table formats

**Expected Result:**
- Module outputs positioned within module sections
- Global outputs positioned after all modules
- No confusion between module and global outputs
- Correct header levels for each section

**Test Data:**
- `TestData/mixed-outputs-plan.json` - Plan with both module and global outputs

---

### TC-21: Full Snapshot Test - Basic Outputs

**Type:** Snapshot

**Description:**
Snapshot test to detect unexpected changes in output table rendering format and structure.

**Preconditions:**
- Plan with diverse outputs (global and module, various actions)

**Test Steps:**
1. Create comprehensive test plan with:
   - Global outputs: create, update, delete, no-op
   - Module outputs with descriptions
   - Mix of sensitive and non-sensitive
   - Some computed values
2. Render markdown with default settings (`showSensitive: false`)
3. Compare against stored snapshot

**Expected Result:**
- Output matches stored snapshot baseline
- Table format is correct (4 columns with proper headers)
- Code formatting applied to names and values
- Descriptions show as plain text or `-`
- Sensitive values masked as `(sensitive value)`
- Computed values show `(known after apply)`
- Headers use correct levels (2nd for global, 4th for module)

**Test Data:**
- `TestData/outputs-snapshot-plan.json` - Comprehensive plan for snapshot
- `TestData/Snapshots/outputs-basic.md` - Expected baseline snapshot

---

### TC-22: Snapshot Test - Sensitive Values Revealed

**Type:** Snapshot

**Description:**
Snapshot test for `--show-sensitive` flag behavior.

**Preconditions:**
- Plan with sensitive outputs
- Renderer with `showSensitive: true`

**Test Steps:**
1. Create test plan with sensitive outputs
2. Render markdown with `showSensitive: true`
3. Compare against stored snapshot
4. Verify sensitive values are revealed (code-formatted)
5. Verify "Sensitive" column still shows `Yes`

**Expected Result:**
- Sensitive values are displayed (not masked)
- Sensitive values are code-formatted
- "Sensitive" column shows `Yes` for transparency
- Non-sensitive outputs unchanged

**Test Data:**
- `TestData/outputs-sensitive-plan.json` - Plan with sensitive outputs
- `TestData/Snapshots/outputs-sensitive-revealed.md` - Expected baseline with revealed values

---

### TC-23: Snapshot Test - Computed Values

**Type:** Snapshot

**Description:**
Snapshot test for computed (known after apply) output values.

**Preconditions:**
- Plan with computed outputs (`after_unknown: true`)

**Test Steps:**
1. Create test plan with outputs where values are computed
2. Render markdown
3. Compare against stored snapshot
4. Verify computed values show `(known after apply)` (plain text, not code-formatted)

**Expected Result:**
- Computed outputs show `(known after apply)`
- Text is plain (not code-formatted)
- Non-computed outputs show actual values

**Test Data:**
- `TestData/outputs-computed-plan.json` - Plan with computed outputs
- `TestData/Snapshots/outputs-computed.md` - Expected baseline

---

### TC-24: Snapshot Test - Display Name Mappings

**Type:** Snapshot

**Description:**
Snapshot test for display name mappings applied to output values.

**Preconditions:**
- Plan with outputs containing Azure resource IDs, principal IDs, etc.
- Principal mapper and value formatters configured

**Test Steps:**
1. Create test plan with outputs containing:
   - Azure resource ID (Key Vault)
   - Principal ID (user GUID)
   - Subscription ID
   - Role definition ID
2. Render markdown with all formatters enabled
3. Compare against stored snapshot
4. Verify display names are applied

**Expected Result:**
- Azure resource IDs show formatted display names
- Principal IDs show mapped names
- Subscription IDs show display names
- Role IDs show role names
- Formatting matches resource attribute formatting

**Test Data:**
- `TestData/outputs-with-azure-ids-plan.json` - Plan with Azure identifiers
- `TestData/test-principals.json` - Principal mappings
- `TestData/Snapshots/outputs-display-mappings.md` - Expected baseline with mappings

---

### TC-25: Snapshot Test - All Output Actions

**Type:** Snapshot

**Description:**
Snapshot test covering all output actions (create, update, delete, no-op).

**Preconditions:**
- Plan with outputs having different actions

**Test Steps:**
1. Create test plan with:
   - Output with `create` action (shows `after`)
   - Output with `update` action (shows `after`)
   - Output with `delete` action (shows `before`)
   - Output with `no-op` action (shows current)
2. Render markdown
3. Compare against stored snapshot

**Expected Result:**
- Create/update/no-op outputs show `after` value
- Delete outputs show `before` value
- All actions render correctly in table
- No-op outputs are included (not filtered out)

**Test Data:**
- `TestData/outputs-all-actions-plan.json` - Plan with all output actions
- `TestData/Snapshots/outputs-all-actions.md` - Expected baseline

---

### TC-26: Snapshot Test - No Outputs

**Type:** Snapshot

**Description:**
Snapshot test for plan with no outputs to verify section omission.

**Preconditions:**
- Plan with no `output_changes` or empty `output_changes`

**Test Steps:**
1. Create test plan without any outputs
2. Render markdown
3. Compare against stored snapshot
4. Verify no "Outputs" section exists in markdown

**Expected Result:**
- No `## Outputs` section in markdown
- No `#### Outputs` sections in module sections
- No placeholder or "No outputs" message
- Report renders normally otherwise

**Test Data:**
- `TestData/no-outputs-plan.json` - Plan without outputs
- `TestData/Snapshots/no-outputs.md` - Expected baseline (no output sections)

---

### TC-27: Integration Test - Nested Sensitivity Detection

**Type:** Integration

**Description:**
Verify that nested sensitivity markers (objects instead of booleans) are correctly detected.

**Preconditions:**
- Plan where `after_sensitive` is an object (nested structure)

**Test Steps:**
1. Create test plan with output where `after_sensitive` is `{ "nested": true }` (object)
2. Build report model
3. Verify output is detected as sensitive
4. Verify masking is applied

**Expected Result:**
- Nested sensitivity objects are correctly detected
- Output is marked as sensitive
- Value is masked when `showSensitive: false`

**Test Data:**
- `TestData/outputs-nested-sensitivity-plan.json` - Plan with nested sensitivity markers

---

### TC-28: Integration Test - Complex Output Values

**Type:** Integration

**Description:**
Verify that complex output values (arrays, objects) are rendered correctly.

**Preconditions:**
- Plan with outputs containing complex JSON structures

**Test Steps:**
1. Create test plan with outputs containing:
   - Array value: `["item1", "item2", "item3"]`
   - Object value: `{ "key": "value", "nested": { "a": 1 } }`
2. Render markdown
3. Verify complex values are serialized to JSON strings
4. Verify JSON is code-formatted

**Expected Result:**
- Array values rendered as JSON string
- Object values rendered as JSON string
- Values are code-formatted (backticks)
- JSON is properly escaped for markdown

**Test Data:**
- `TestData/outputs-complex-values-plan.json` - Plan with complex output values

---

### TC-29: Architecture Test - No Circular Dependencies

**Type:** Architecture

**Description:**
Verify that output-related code doesn't introduce circular dependencies between layers.

**Preconditions:**
- Output feature implemented in parsing, model, and rendering layers

**Test Steps:**
1. Run NetArchTest architecture tests
2. Verify parsing layer doesn't depend on markdown generation
3. Verify model layer dependencies are correct
4. Verify rendering layer can depend on parsing and model

**Expected Result:**
- No new circular dependencies introduced
- Layer boundaries respected
- Architecture tests pass

**Test Data:**
- N/A (architecture analysis of compiled assemblies)

---

### TC-30: End-to-End Docker Test - Full Pipeline

**Type:** End-to-End (Docker)

**Description:**
Verify outputs feature works in the Docker container with real CLI invocation.

**Preconditions:**
- Docker image built with outputs feature
- Test plan JSON file

**Test Steps:**
1. Build Docker image
2. Run container with test plan containing outputs:
   ```bash
   docker run --rm -i tfplan2md < test-outputs-plan.json
   ```
3. Verify output markdown contains:
   - Global outputs section
   - Module outputs sections (if modules exist)
   - Proper table formatting
4. Run with `--show-sensitive` flag:
   ```bash
   docker run --rm -i tfplan2md --show-sensitive < test-outputs-plan.json
   ```
5. Verify sensitive values are revealed

**Expected Result:**
- Outputs render correctly in Docker environment
- CLI flag (`--show-sensitive`) works as expected
- No crashes or errors
- Markdown output is valid

**Test Data:**
- `TestData/outputs-docker-test-plan.json` - Plan for Docker testing

## Test Data Requirements

The following test data files need to be created in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/`:

1. **`outputs-basic-plan.json`** - Simple plan with global outputs having various actions (create, update, delete, no-op)
2. **`outputs-with-metadata-plan.json`** - Plan with outputs containing descriptions and sensitivity flags
3. **`outputs-diverse-actions-plan.json`** - Plan with outputs covering all edge cases for model building
4. **`outputs-no-description-plan.json`** - Plan with some outputs missing descriptions
5. **`outputs-sensitivity-sources-plan.json`** - Plan with sensitivity markers in different locations (testing precedence)
6. **`outputs-sensitive-plan.json`** - Plan with mix of sensitive and non-sensitive outputs
7. **`outputs-computed-plan.json`** - Plan with computed output values (`after_unknown: true`)
8. **`outputs-with-azure-ids-plan.json`** - Plan with outputs containing Azure resource IDs, principal IDs, etc.
9. **`outputs-unordered-plan.json`** - Plan with outputs in non-alphabetical order
10. **`outputs-create-action-plan.json`** - Plan with create action output
11. **`outputs-update-action-plan.json`** - Plan with update action output
12. **`outputs-delete-action-plan.json`** - Plan with delete action output
13. **`outputs-noop-action-plan.json`** - Plan with no-op action output
14. **`no-outputs-plan.json`** - Plan with no outputs (empty or missing `output_changes`)
15. **`outputs-only-module-plan.json`** - Plan with module containing only outputs (no resources)
16. **`module-with-outputs-plan.json`** - Plan with single module having both resources and outputs
17. **`multi-module-outputs-plan.json`** - Plan with multiple modules each having outputs
18. **`global-outputs-plan.json`** - Plan with global outputs and module resources
19. **`mixed-outputs-plan.json`** - Plan with both module and global outputs
20. **`outputs-snapshot-plan.json`** - Comprehensive plan for snapshot testing
21. **`outputs-nested-sensitivity-plan.json`** - Plan with nested sensitivity objects
22. **`outputs-complex-values-plan.json`** - Plan with array and object output values
23. **`outputs-docker-test-plan.json`** - Plan for Docker integration testing

**Snapshot baselines** (in `src/tests/Oocx.TfPlan2Md.TUnit/TestData/Snapshots/`):

1. **`outputs-basic.md`** - Expected markdown for basic outputs
2. **`outputs-sensitive-revealed.md`** - Expected markdown with `--show-sensitive`
3. **`outputs-computed.md`** - Expected markdown for computed values
4. **`outputs-display-mappings.md`** - Expected markdown with display name mappings
5. **`outputs-all-actions.md`** - Expected markdown for all output actions
6. **`no-outputs.md`** - Expected markdown when no outputs exist

## Edge Cases

| Scenario | Expected Behavior | Test Case |
|----------|-------------------|-----------|
| No outputs in plan | Outputs sections omitted entirely | TC-15, TC-26 |
| Module with only outputs (no resources) | Create `ModuleChangeGroup` with empty `Changes` | TC-16 |
| Output without description | Show `-` in description column | TC-04 |
| Nested sensitive values (object instead of boolean) | Detect sensitivity recursively | TC-27 |
| Complex output values (arrays, objects) | Serialize to JSON string, code-format | TC-28 |
| Missing configuration metadata | Graceful degradation (no description, assume not sensitive) | TC-02 |
| Computed sensitive values | Show `(sensitive value)` not `(known after apply)` (masking takes precedence) | TC-06 |
| Empty module address (root outputs) | Treated as global outputs | TC-19 |
| Output action with multiple actions (e.g., replace) | Use primary action (first in array) | TC-25 |
| `before` and `after` both null | Handle gracefully (rare edge case) | TC-28 |

## Non-Functional Tests

### Performance

**Scenario:** Rendering plan with 100+ outputs should complete in reasonable time.

**Acceptance Criteria:**
- Parsing 100 outputs: < 100ms
- Building models for 100 outputs: < 200ms
- Rendering 100 outputs: < 500ms
- Total overhead: < 1 second for 100 outputs

**Test:** Create synthetic plan with 100 outputs, measure parse/build/render times.

### Error Handling

**Scenario:** Gracefully handle malformed or unexpected output data.

**Test Cases:**
- Missing `actions` array → Default to `["no-op"]` or log warning
- Invalid JSON in output value → Show error message in value cell or serialize as string
- Null output change → Skip output, log warning
- Missing `output_changes` key → Treat as no outputs
- Corrupted configuration JSON → Fall back to output_changes data only (no descriptions)

### Compatibility

**Scenario:** Ensure outputs feature works across all supported Terraform versions and plan formats.

**Test Cases:**
- Terraform 1.0.x plan format → Outputs render correctly
- Terraform 1.5.x plan format → Outputs render correctly
- Terraform 1.9.x plan format → Outputs render correctly
- Plans without `configuration` section → Outputs render without descriptions

## UAT Test Plan

**CRITICAL**: This feature requires a UAT Test Plan to validate rendering in GitHub and Azure DevOps. The UAT plan is defined in a separate document:

**UAT Test Plan Location:** `docs/features/097-terraform-outputs/uat-test-plan.md`

The UAT plan will specify:
1. Feature-specific test artifact (`uat-plan.json` and `uat-plan.md`)
2. Comprehensive demo (regression test)
3. Validation instructions for the UAT Tester agent

**Note:** The UAT test plan document must be created by the Quality Engineer as part of this feature's test planning.

## Open Questions

1. **Value Formatting for Non-JSON Values:** If an output value is a plain string (not JSON), should we still code-format it? **Answer:** Yes, all non-masked, non-computed values are code-formatted.

2. **Update Action Before/After Display:** Current plan shows only `after` value for updates. Should we consider showing both before and after in future? **Answer:** Out of scope for this feature (spec explicitly defers before→after diff to future enhancement).

3. **Output Summary Counts:** Should we add output counts to the summary section? **Answer:** No, per specification "Out of Scope: Module output summary counts".

4. **Empty String vs Null Description:** How should we distinguish between explicit empty string description and missing description? **Answer:** Treat both as "no description" (render as `-`).

## Test Execution Order

1. **Unit Tests First** (TC-01 to TC-16)
   - Parse layer tests
   - Model building tests
   - Edge case handling
   
2. **Integration Tests** (TC-17 to TC-20, TC-27 to TC-28)
   - Module output positioning
   - Global output positioning
   - Mixed scenarios
   - Complex values and nested sensitivity
   
3. **Snapshot Tests** (TC-21 to TC-26)
   - Full rendering validation
   - Baseline comparison
   
4. **Architecture Tests** (TC-29)
   - Layer boundary validation
   
5. **End-to-End Tests** (TC-30)
   - Docker container validation
   - CLI flag testing

6. **UAT** (separate plan)
   - Real-world rendering in GitHub and Azure DevOps

## Definition of Done

The test plan is complete when:
- [ ] All acceptance criteria have mapped test cases
- [ ] Edge cases and error scenarios are covered
- [ ] Test cases follow TUnit conventions (async methods, AwesomeAssertions)
- [ ] Test data requirements are documented
- [ ] Snapshot baseline files are identified
- [ ] UAT test plan reference is included
- [ ] Non-functional requirements (performance, error handling, compatibility) are addressed
- [ ] Test execution order is defined
- [ ] The Maintainer has approved this test plan
