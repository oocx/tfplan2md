# UAT Test Plan: Azure DevOps Build Definition Nested Block Tables

## Goal

Verify that `azuredevops_build_definition` nested blocks (variables, triggers, repository, schedules) render correctly as structured tables in GitHub and Azure DevOps PR comments, with proper secret masking and semantic diffing.

## Artifacts

### Feature-Specific Test Artifact (REQUIRED)

**Purpose:** Focus testing on the specific changes in this feature (build definition table rendering).

**Source Plan Path:** `docs/features/094-build-definition-tables/uat-plan.json`

**Rendered Output Path:** `docs/features/094-build-definition-tables/uat-plan.md`

**Plan Requirements:**
- **MUST be a real Terraform plan JSON** containing `azuredevops_build_definition` resources that exercise all table rendering features
- **MUST cover all nested blocks:** variables (regular and secret), CI trigger, pull request trigger, schedules, repository, and conditional rendering (empty blocks)
- **MUST include all operation types:** create, update, and delete operations
- **MUST demonstrate semantic diffing:** added, modified, removed, and unchanged variables
- **MUST demonstrate secret masking:** secret variables show metadata but display `(sensitive / hidden)` for values
- **MUST demonstrate large value handling:** at least one regular variable with >100 characters or multi-line value
- **Rationale:** This specific plan demonstrates all aspects of Feature 094 - build definition table rendering, secret masking, and semantic variable diffing

**Key Resources:**
1. `azuredevops_build_definition.create_comprehensive` - Create operation with all nested blocks (variables, CI trigger, repository)
2. `azuredevops_build_definition.update_variables` - Update operation demonstrating semantic diffing (added/modified/removed variables)
3. `azuredevops_build_definition.delete_with_secrets` - Delete operation with secret variables to verify masking

**Coverage:**
- ✅ Variables table rendering (regular and secret)
- ✅ Secret variable masking (`(sensitive / hidden)`)
- ✅ Semantic diffing (added ➕, modified 🔄, removed ❌, unchanged ⏺️)
- ✅ Before/after diffs for modified variables with `-` and `+` prefixes
- ✅ CI Trigger table rendering
- ✅ Repository table rendering
- ✅ Pull Request Trigger table rendering (if included)
- ✅ Schedules table rendering (if included)
- ✅ Conditional rendering (no empty tables shown)
- ✅ Create/Update/Delete operation layouts
- ✅ Large value handling for regular variables
- ✅ Empty/null attribute handling (displayed as `-`)

**Example Creation Command:**
```bash
# After Developer creates uat-plan.json based on this specification
cd docs/features/094-build-definition-tables/
tfplan2md uat-plan.json > uat-plan.md
```

### Comprehensive Demo (Regression Test)

**Purpose:** Ensure no unintended side effects in other areas, especially existing `azuredevops_variable_group` rendering.

**Artifact Path:** 
- GitHub: `artifacts/comprehensive-demo-simple-diff.md`
- Azure DevOps: `artifacts/comprehensive-demo.md`

**Note:** This artifact is generated automatically by the Developer using the `generate-demo-artifacts` skill.

## Test Steps

1. Developer creates `uat-plan.json` based on this specification
2. Developer generates `uat-plan.md` from the plan using tfplan2md
3. Code Reviewer validates both files exist and are complete
4. UAT Tester uses `uat-plan.md` for testing
5. UAT will post TWO separate PR comments:
   - **Feature-Specific Report**: Tests the specific changes using `uat-plan.md`
   - **Comprehensive Demo**: Regression test for side effects
6. Verify both reports on GitHub and Azure DevOps

## Validation Instructions (Test Description)

**Feature-Specific Validation:**

In the **feature-specific report** (first comment, labeled "🎯 Feature Test"):

### 1. Create Operation - `azuredevops_build_definition.create_comprehensive`

**Summary Line:**
- Verify shows ➕ icon, resource type, resource name, and pipeline name

**Metadata Section:**
- **Pipeline Name:** Should display as `<code>comprehensive-pipeline</code>`
- **Path:** Should display as `<code>\\Pipelines\\Example</code>`
- **Agent Pool:** Should display as `<code>Azure Pipelines</code>`

**Variables Table:**
- **Table Structure:** Verify table has columns: `Name | Value | Is Secret | Allow Override`
- **Regular Variables:**
  - `BUILD_CONFIGURATION` should show value `` `Release` ``, `is_secret` as `` `false` ``, `allow_override` as `` `true` ``
  - `BUILD_PLATFORM` should show value `` `Any CPU` ``, `is_secret` as `` `false` ``, `allow_override` as `` `true` ``
- **Secret Variables:**
  - `API_TOKEN` should show value `` `(sensitive / hidden)` `` (NOT the actual secret), `is_secret` as `` `true` ``, `allow_override` as `` `true` ``
  - Verify NO actual secret values are exposed anywhere in the output
- **Large Value Variable (if included):**
  - Variable with >100 character value should NOT show value in table
  - Large value should appear in "Large values" expandable section below the table
- **No Change Column:** Create operation tables should NOT have a "Change" column

**CI Trigger Table:**
- **Section Header:** `#### CI Trigger`
- **Table Structure:** Verify table has columns: `Use YAML | Override (Branch Filters)`
- **Content:** Verify `use_yaml` displays as `` `true` `` or `` `false` ``
- **Empty Override:** If override array is empty, verify displays as `` `-` ``

**Repository Table:**
- **Section Header:** `#### Repository`
- **Table Structure:** Verify table has columns: `Type | Repo ID | Branch | YAML Path | Report Build Status`
- **Content:** Verify all repository details are displayed with proper code formatting (backticks)
- **Empty Values:** If `github_enterprise_url` or `service_connection_id` is empty, verify displays as `-`

**Pull Request Trigger Table (if included):**
- **Section Header:** `#### Pull Request Trigger`
- **Table Structure:** Verify table exists with appropriate columns
- **Content:** Verify trigger settings are displayed correctly

**Schedules Table (if included):**
- **Section Header:** `#### Schedules`
- **Table Structure:** Verify table exists with appropriate columns
- **Content:** Verify schedule configuration is displayed correctly (days as comma-separated, time formatted)

---

### 2. Update Operation - `azuredevops_build_definition.update_variables`

**Summary Line:**
- Verify shows 🔄 icon, resource name, and optionally variable change count (e.g., "3 🔧 variables")

**Variables Table with Change Column:**
- **Table Structure:** Verify table has columns: `Change | Name | Value | Is Secret | Allow Override`
- **Added Variable (➕):**
  - Find row with ➕ icon in Change column
  - Variable name: `NEW_VARIABLE`
  - Verify value, is_secret, and allow_override are displayed
- **Modified Variable (🔄):**
  - Find row with 🔄 icon in Change column
  - Variable name: `BUILD_CONFIGURATION`
  - **Value Column:** Verify shows before/after diff:
    - Contains `- \`Debug\`` (before value with minus prefix)
    - Contains `+ \`Release\`` (after value with plus prefix)
    - Contains `<br>` (line break between before and after)
  - **Unchanged Attributes:** If `is_secret` and `allow_override` didn't change, verify they show single value without prefix (e.g., `` `false` ``, `` `true` ``)
  - **Changed Attributes:** If `allow_override` changed, verify shows `- \`true\`<br>+ \`false\``
- **Removed Variable (❌):**
  - Find row with ❌ icon in Change column
  - Variable name: `OLD_VARIABLE`
  - Verify shows before state values
- **Unchanged Variable (⏺️) (if included):**
  - Find row with ⏺️ icon in Change column
  - Verify shows single values without before/after diffs

**Secret Variable Transition (if included):**
- **Scenario:** Variable changes from `is_secret: false` to `is_secret: true`
- **Expected:**
  - Value column shows `` `(sensitive / hidden)` `` (NOT the before value, even though it was non-secret)
  - IsSecret column shows diff: `- \`false\`<br>+ \`true\``

---

### 3. Delete Operation - `azuredevops_build_definition.delete_with_secrets`

**Summary Line:**
- Verify shows ❌ icon, resource name

**Section Headers:**
- Verify headers indicate deletion (e.g., `#### Variables (being deleted)`, `#### Repository (being deleted)`)

**Variables Table:**
- **No Change Column:** Delete operation tables should NOT have a "Change" column
- **Table Structure:** Verify table has columns: `Name | Value | Is Secret | Allow Override`
- **Secret Variables:**
  - Verify secret variable values show `` `(sensitive / hidden)` `` even in delete operation
  - Verify NO actual secret values are exposed

**Before/After Context:**
- Before this feature: Build definitions displayed as "At least one attribute in this block is (or was) sensitive, so its contents will not be displayed."
- After this feature: Tables clearly show variable names, metadata, and structure while protecting secret values

---

### 4. Conditional Rendering - Empty Blocks

**Scenario:** Build definition with some empty nested blocks (e.g., no pull request triggers, no schedules)

**Expected:**
- **Repository section shown:** If repository block is present
- **Variables section shown:** If variables exist
- **CI Trigger section shown:** If ci_trigger block is present
- **NO section for empty blocks:** If `pull_request_trigger`, `schedules`, or `jobs` are empty arrays, verify those sections do NOT appear (no empty tables)

---

### 5. Formatting Consistency (Report Style Guide)

**Summary Line:**
- Verify uses `<code>` tags for values (Azure DevOps compatibility): `<code>example-pipeline</code>`, `<code>example</code>`

**Metadata Labels:**
- Verify labels are plain text with bold: `**Pipeline Name:**`, `**Path:**`, `**Agent Pool:**`
- Verify values use `<code>` tags: `<code>pipeline-name</code>`

**Table Cells:**
- Verify values use backticks: `` `Release` ``, `` `true` ``, `` `false` ``
- Verify empty/null values use backticks with dash: `` `-` ``

**Table Headers:**
- Verify headers are plain text (no backticks): `| Name | Value | Is Secret | Allow Override |`

---

## Regression Validation:

In the **comprehensive demo** (second comment, labeled "🔄 Regression Test"):

**Verify:**
- **No unintended changes** to existing `azuredevops_variable_group` rendering (should still show variables table correctly)
- **No unintended changes** to other Azure DevOps resources (project, git repository)
- **No unintended changes** to Azure resources (if comprehensive demo includes Azure resources)
- **Build definition changes appear correctly** in comprehensive context
- **All sections render correctly** (summaries, details, static analysis if included)

**Specific Checks:**
- If comprehensive demo includes `azuredevops_variable_group`, verify it still renders with variables table (Feature 027/039 not broken)
- Verify no rendering errors or broken HTML/Markdown
- Verify proper resource grouping and ordering

---

## Success Criteria

**GitHub:**
- [x] Feature-specific test report renders correctly in GitHub PR comment
- [x] Comprehensive demo renders correctly in GitHub PR comment
- [x] All variables tables display properly
- [x] Secret values are masked (show `(sensitive / hidden)`)
- [x] Change indicators (➕, 🔄, ❌, ⏺️) display correctly
- [x] Before/after diffs use `-` and `+` prefixes with `<br>` line breaks
- [x] Conditional rendering works (no empty tables)
- [x] Code formatting follows style guide (backticks for values, `<code>` tags in summary)

**Azure DevOps:**
- [x] Feature-specific test report renders correctly in Azure DevOps PR comment
- [x] Comprehensive demo renders correctly in Azure DevOps PR comment
- [x] All success criteria from GitHub section also pass in Azure DevOps
- [x] `<code>` tags in summary line render correctly (Azure DevOps HTML support)

**Security:**
- [x] NO actual secret values visible anywhere in rendered output
- [x] All secret variables show `(sensitive / hidden)` in value column
- [x] Secret variable metadata (name, is_secret flag, allow_override) is visible

**Regression:**
- [x] Existing `azuredevops_variable_group` rendering not broken
- [x] Other Azure DevOps resources render correctly
- [x] No unintended formatting changes in comprehensive demo

## Feedback Opportunities

During UAT review, consider:

1. **Variable Table Clarity:**
   - Are variable changes easy to understand?
   - Is the Change column (➕, 🔄, ❌, ⏺️) intuitive?
   - Are before/after diffs readable?

2. **Secret Masking:**
   - Is `(sensitive / hidden)` clear enough?
   - Is metadata (name, is_secret flag) sufficient for review?

3. **Nested Blocks:**
   - Are CI trigger, repository, PR trigger, and schedules tables useful?
   - Should any additional fields be displayed?
   - Are empty values (displayed as `-`) clear?

4. **Formatting:**
   - Does the rendering follow style guide consistently?
   - Are code blocks, backticks, and `<code>` tags used appropriately?
   - Do tables render well in both GitHub and Azure DevOps?

5. **Conditional Rendering:**
   - Is it clear when sections are omitted (no empty tables)?
   - Should there be a note when blocks are empty (e.g., "No schedules configured")?

6. **Before/After Context:**
   - Does the feature solve the stated problem (replacing opaque "sensitive block" messages)?
   - Is the information presented useful for pipeline change review?

## Notes for Developer

When creating `uat-plan.json`, ensure it contains:

1. **At least 3 build definition resources:**
   - 1 create operation with all nested blocks populated
   - 1 update operation with variable changes (added, modified, removed, unchanged)
   - 1 delete operation with secret variables

2. **Variable diversity:**
   - Regular variables with various values
   - Secret variables (is_secret: true)
   - At least one large value (>100 chars or multi-line) for a regular variable
   - Variables with null/empty attributes (to test `-` display)

3. **Nested block coverage:**
   - CI trigger with and without override branch filters
   - Repository block with all relevant fields
   - At least one resource with empty pull_request_trigger and schedules (to test conditional rendering)
   - Optional: pull request trigger and schedules blocks if testing those features

4. **Semantic diffing demonstration:**
   - Update operation must show all 4 change types: added, modified, removed, unchanged
   - At least one modified variable should have changed value but unchanged is_secret/allow_override
   - Optional: one variable changes from is_secret: false to true

5. **Realistic data:**
   - Use realistic variable names (e.g., BUILD_CONFIGURATION, API_TOKEN, CONNECTION_STRING)
   - Use realistic repository IDs (GUIDs)
   - Use realistic branch names (refs/heads/main, refs/heads/develop)

The plan should be minimal but comprehensive - focus on demonstrating all table rendering features without unnecessary resources.
