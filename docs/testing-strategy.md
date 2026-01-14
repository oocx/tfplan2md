# Testing Strategy

## Overview

The tfplan2md project uses a comprehensive testing strategy with **TUnit v1.9.26** as the test framework. All tests are located in the `Oocx.TfPlan2Md.TUnit` project.

## Test Infrastructure

- **Test Framework**: TUnit 1.9.26 (async-first with real-time progress reporting)
- **Test Location**: `tests/Oocx.TfPlan2Md.TUnit/`
- **Test Execution**: `dotnet test tests/Oocx.TfPlan2Md.TUnit/` or use `scripts/test-with-timeout.sh`

### TUnit CLI Syntax

TUnit uses different CLI arguments compared to traditional test frameworks. All TUnit-specific flags must be passed after `--`:

#### Filtering Tests
```bash
# Filter by class name (hierarchical pattern)
dotnet test -- --treenode-filter /*/*/LoginTests/*

# Filter by test name
dotnet test -- --treenode-filter /*/*/*/AcceptCookiesTest

# Filter by property/category
dotnet test -- --treenode-filter /**[Category=Unit]

# Exclude by category
dotnet test -- --treenode-filter /**[Category!=Integration]

# Multiple conditions (AND)
dotnet test -- --treenode-filter /**[Category=Unit]&[Priority=High]
```

**Note**: TUnit uses `--treenode-filter` with hierarchical patterns (`/Assembly/Namespace/ClassName/TestName`), unlike traditional test frameworks.

#### Output Control
```bash
# Normal output (only failures shown, buffered)
dotnet test -- --output Normal

# Detailed output (all tests shown, real-time)
dotnet test -- --output Detailed

# Disable progress reporting
dotnet test -- --no-progress

# Disable ANSI colors
dotnet test -- --no-ansi
```

#### Log Levels
```bash
# Set log level for diagnostics
dotnet test -- --log-level Trace      # Maximum detail
dotnet test -- --log-level Debug      # Debug information
dotnet test -- --log-level Information # Default
dotnet test -- --log-level Warning    # Warnings and errors only
dotnet test -- --log-level Error      # Errors only

# Combine with output control
dotnet test -- --output Detailed --log-level Debug
```

#### Common Examples
```bash
# Run all tests with detailed output
scripts/test-with-timeout.sh -- dotnet test -- --output Detailed

# Run specific test class (from repo root, use --project)
scripts/test-with-timeout.sh -- dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /*/*/MarkdownRendererTests/*

# Run tests with category, detailed output, and debug logging
dotnet test --project tests/Oocx.TfPlan2Md.TUnit/ -- --treenode-filter /**[Category=Unit] --output Detailed --log-level Debug
```

### Why TUnit?

Based on comprehensive analysis documented in `docs/test-framework-reliability.md`:

**Performance**:
- **7.8x faster** than traditional test frameworks
- Consistent execution time (σ=0.06s)
- 100% test coverage maintained (370 tests)

**Hang Detection & Diagnostics**:
- **Real-time progress reporting** with test name + elapsed time during execution
- Detects hangs in **30-60 seconds**
- **Time saved: ~2-4 hours per hang incident**
- Full async stack traces with context
- Reliability score: **10/10**

**Architecture**:
- **Async-first design**: All tests use `async Task`, prevents sync-over-async deadlocks
- **Source generators**: Compile-time test discovery eliminates reflection overhead
- **Built on Microsoft.Testing.Platform**: Modern, efficient test infrastructure
- **Native AOT compatible**: Future-proof architecture

### Test Configuration

TUnit uses source-generator based discovery with the following configuration:

```csharp
// AssemblyInfo.cs - Parallelization at class level
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

// .csproj - Suppressed analyzer warnings for test patterns
<NoWarn>TUnit0055;TUnitAssertions0003;TUnitAssertions0005;TUnit0038;CA2201;IDE1006</NoWarn>
```

## Test Types

### Unit Tests

Test individual components in isolation to verify correct behavior of parsing, model building, markdown rendering, and CLI argument parsing.

### Integration Tests

Test JSON parsing and markdown generation end-to-end. As the application is distributed via Docker, Docker-based integration tests verify the final CLI behavior in a containerized environment.

### User Acceptance Testing (UAT)

For user-facing changes (especially markdown rendering), run UAT in real environments using **temporary pull requests** in:

- GitHub PRs in `oocx/tfplan2md`
- Azure DevOps PRs in `https://dev.azure.com/oocx` (project `test`, repository `test`)

#### Key Principles

1. **Markdown as PR Comment**: The generated markdown report is posted as a **PR comment** (not PR description) so the actual rendering can be validated in the real PR UI.
2. **Incremental Updates**: Each fix/update is posted as a **new comment** so Maintainer can see progression.
3. **Autonomous Polling**: Agent polls automatically every 30 seconds without requiring Maintainer prompts.
4. **Automatic Cleanup**: UAT PRs are closed/abandoned and branches deleted after approval or abort.

#### Approval Criteria

| Platform | Approval Detected When |
|----------|----------------------|
| **GitHub** | Maintainer comments "approved", "passed", "lgtm", "accept" **OR** closes the PR |
| **Azure DevOps** | Maintainer comments "approved"/"passed"/etc. **OR** marks the latest comment thread as "Resolved" |

#### Helper Scripts

Use the scripts in `scripts/` for simplified UAT workflow:

**GitHub** (`scripts/uat-github.sh`):
```bash
# Create PR and post markdown as comment
scripts/uat-github.sh create artifacts/<file>.md

# Poll for approval (exit 0 = approved, exit 1 = waiting)
scripts/uat-github.sh poll <pr-number>

# Post updated markdown after fix
scripts/uat-github.sh comment <pr-number> artifacts/<file>.md

# Clean up after approval
scripts/uat-github.sh cleanup <pr-number>
```

**Azure DevOps** (`scripts/uat-azdo.sh`):
```bash
# One-time setup (verifies auth, configures defaults)
scripts/uat-azdo.sh setup

# Create PR and post markdown as comment
scripts/uat-azdo.sh create artifacts/<file>.md

# Poll for approval (exit 0 = approved/resolved, exit 1 = waiting)
scripts/uat-azdo.sh poll <pr-id>

# Post updated markdown after fix
scripts/uat-azdo.sh comment <pr-id> artifacts/<file>.md

# Clean up after approval
scripts/uat-azdo.sh cleanup <pr-id>
```

#### Autonomous Polling Loop

After creating PRs, run polling automatically:
```bash
# Save original branch
ORIGINAL_BRANCH=$(git branch --show-current)

while true; do
    scripts/uat-github.sh poll "$GH_PR" && GH_OK=true
    scripts/uat-azdo.sh poll "$AZDO_PR" && AZDO_OK=true
    
    [[ "${GH_OK:-}" == "true" && "${AZDO_OK:-}" == "true" ]] && break
    sleep 15
done

# Cleanup and restore branch
scripts/uat-github.sh cleanup "$GH_PR"
scripts/uat-azdo.sh cleanup "$AZDO_PR"
git checkout "$ORIGINAL_BRANCH"
```

## Test Data

Tests use a shared test data file `TestData/azurerm-azuredevops-plan.json` containing a realistic Terraform plan with:

- Azure Resource Manager resources (resource group, storage account, key vault, virtual network)
- Azure DevOps resources (project, git repository)
- Various change actions (create, update, delete, replace)
- Sensitive values for testing masking behavior

Additional test data files for edge cases:

- `empty-plan.json` - Plan with no resource changes (empty `resource_changes` array)
- `no-op-plan.json` - Plan with only no-op changes (resources with no modifications)
- `minimal-plan.json` - Minimal valid plan with null before/after values
- `create-only-plan.json` - Plan with only create operations (new infrastructure deployment)
- `delete-only-plan.json` - Plan with only delete operations (infrastructure teardown)
- `firewall-rule-changes.json` - Firewall rule collection with semantic diff scenarios (add, modify, remove rules)
- `multi-module-plan.json` - Plan with multiple modules and nested modules to validate module grouping, ordering, and heading hierarchy

---

## Test Catalog

### CLI Parser Tests (`CLI/CliParserTests.cs`)

Tests for command-line argument parsing logic.

For user-facing changes (especially markdown rendering), run UAT in real environments using **temporary pull requests** in:

- GitHub PRs in `oocx/tfplan2md`
- Azure DevOps PRs in `https://dev.azure.com/oocx` (project `test`, repository `test`)

The UAT loop is **comment-driven**:

1. Create a UAT PR and post the generated markdown as a PR **comment**.
2. Maintainer reviews in the real PR UI and leaves feedback as PR comments/threads.
3. Apply fixes, push updates, and post an updated PR comment.
4. Repeat until Maintainer explicitly says **approve** or **abort**.

**Rules**:
- Do not close/abandon UAT PRs unless the Maintainer explicitly says **approve** or **abort**.
- Poll for new feedback until explicit approval/abort.

**Preferred: repo wrapper scripts (recommended)**

Use the stable wrapper scripts to minimize terminal approvals and to avoid brittle CLI output parsing:

```bash
# End-to-end UAT (creates PRs, posts comment(s), polls, and cleans up)
scripts/uat-run.sh run artifacts/<uat-file>.md

# Targeted GitHub operations
scripts/uat-github.sh create artifacts/<uat-file>.md
scripts/uat-github.sh poll <pr-number>
scripts/uat-github.sh comment <pr-number> artifacts/<uat-file>.md
scripts/uat-github.sh cleanup <pr-number>

# Targeted Azure DevOps operations
scripts/uat-azdo.sh setup
scripts/uat-azdo.sh create artifacts/<uat-file>.md
scripts/uat-azdo.sh poll <pr-id>
scripts/uat-azdo.sh comment <pr-id> artifacts/<uat-file>.md
scripts/uat-azdo.sh cleanup <pr-id>
```

**Manual fallbacks (only for debugging)**:

**GitHub (preferred in VS Code chat)**:
- Use GitHub chat tools to fetch PR conversation comments and review comments.

**GitHub (CLI fallback)**:
```bash
PAGER=cat gh pr view <pr-number> --comments
```

**Azure DevOps (threads via az devops invoke)**:
```bash
az account show >/dev/null || az login
az devops configure --defaults organization=https://dev.azure.com/oocx project=test

# Poll PR threads (repeat until approval/abort)
az devops invoke --area git --resource pullrequestthreads \
  --route-parameters project=test repositoryId=test pullRequestId=<pr-id> \
  --api-version 7.1
```
| Test Name | Description |
|-----------|-------------|
| `Build_ValidPlan_ReturnsCorrectSummary` | Verifies that the summary correctly counts: 3 to add, 1 to change, 1 to destroy, 1 to replace, 6 total |
| `Build_ValidPlan_ReturnsCorrectActionSymbols` | Verifies that action symbols are correctly assigned: `➕` for create, `🔄` for update, `❌` for delete, `♻️` for replace |
| `Build_WithSensitiveValues_MasksByDefault` | Verifies that sensitive values are masked with "(sensitive)" by default |
| `Build_WithShowSensitiveTrue_DoesNotMask` | Verifies that sensitive values are shown when `showSensitive` is true |
| `Build_ValidPlan_PreservesTerraformVersion` | Verifies that Terraform and format versions are preserved in the model |
| `Build_EmptyPlan_ReturnsZeroSummary` | Verifies that an empty plan returns zero counts for all summary fields |
| `Build_NoOpPlan_CountsNoOpCorrectly` | Verifies that no-op resources are counted correctly in the summary |
| `Build_MinimalPlan_HandlesNullBeforeAndAfter` | Verifies that plans with null before/after values produce empty attribute changes |
| `Build_CreateOnlyPlan_CountsCreatesCorrectly` | Verifies that create-only plans are summarized correctly |
| `Build_DeleteOnlyPlan_CountsDeletesCorrectly` | Verifies that delete-only plans are summarized correctly |

### Markdown Renderer Tests (`MarkdownGeneration/MarkdownRendererTests.cs`)

Tests for rendering the report model to Markdown output.

| Test Name | Description |
|-----------|-------------|
| `Render_ValidPlan_ContainsSummarySection` | Verifies that the rendered output contains a summary section with add/change/destroy indicators and emoji symbols |
| `Render_ValidPlan_ContainsResourceChanges` | Verifies that all resource addresses appear in the rendered output |
| `Render_ValidPlan_ContainsTerraformVersion` | Verifies that the Terraform version (1.14.0) appears in the rendered output |
| `Render_ValidPlan_ContainsActionSymbols` | Verifies that action symbols with resource addresses appear correctly (`➕`, `🔄`, `❌`, `♻️`) |
| `Render_EmptyPlan_ProducesValidMarkdown` | Verifies that an empty plan renders without errors and shows zero counts |
| `Render_NoOpPlan_ProducesValidMarkdown` | Verifies that a no-op plan renders correctly with the no-op action displayed |
| `Render_EmptyPlan_ShowsNoChangesMessage` | Verifies that an empty plan shows "No changes" message in the output |
| `Render_MinimalPlan_HandlesNullAttributes` | Verifies that resources with null before/after render without attribute details section |
| `Render_CreateOnlyPlan_ShowsAllCreates` | Verifies that create-only plans render all create operations with correct symbols |
| `Render_DeleteOnlyPlan_ShowsAllDeletes` | Verifies that delete-only plans render all delete operations with correct symbols |
| `Render_WithInvalidTemplate_ThrowsMarkdownRenderException` | Verifies that invalid template syntax throws a `MarkdownRenderException` |
| `Render_AttributeChangesTable_DoesNotContainExtraNewlines` | Verifies that attribute changes table rows are consecutive without blank lines |
| `Render_CreateOnlyPlan_ShowsAttributeValueTable` | Verifies that create-only plans render two-column `Attribute | Value` tables showing after values |
| `Render_DeleteOnlyPlan_ShowsAttributeValueTable` | Verifies that delete-only plans render two-column `Attribute | Value` tables showing before values |
| `Render_ReplacePlan_ShowsBeforeAndAfterColumns` | Verifies that replace operations (create+delete) render a 3-column `Attribute | Before | After` table |
| `Render_CreatePlan_MasksSensitiveAttributes` | Verifies that sensitive attributes are masked in the create `Value` column by default |
| `Render_Create_OmitsNullAndUnknownAttributes` | Verifies that null and unknown attributes are omitted from create tables |
| `Render_Delete_OmitsNullAttributes` | Verifies that null attributes are omitted from delete tables |
| `Render_LargePlanWithManyNoOpResources_DoesNotExceedIterationLimit` | Verifies that large plans don't exceed Scriban's iteration limit of 1000 |
| `RenderResourceChange_FirewallRuleCollection_ReturnsResourceSpecificMarkdown` | Verifies that firewall rule collections use the resource-specific template |
| `Render_FirewallRuleCollection_UsesResourceSpecificTemplate` | Verifies that the full `Render` applies resource-specific templates automatically when available |
| `RenderResourceChange_FirewallRuleCollection_ShowsAddedRules` | Verifies that added rules are shown with ➕ indicator |
| `RenderResourceChange_FirewallRuleCollection_ShowsModifiedRules` | Verifies that modified rules are shown with 🔄 indicator |
| `RenderResourceChange_FirewallRuleCollection_ShowsRemovedRules` | Verifies that removed rules are shown with ❌ indicator |
| `RenderResourceChange_FirewallRuleCollection_ShowsUnchangedRules` | Verifies that unchanged rules are shown with ⏺️ indicator |
| `RenderResourceChange_FirewallRuleCollection_Create_ShowsAllRules` | Verifies that new rule collections show all rules being created |
| `RenderResourceChange_FirewallRuleCollection_Delete_ShowsAllRulesBeingDeleted` | Verifies that deleted rule collections show all rules being removed |
| `RenderResourceChange_NonFirewallResource_ReturnsNull` | Verifies that resources without specific templates return null for default handling |
| `RenderResourceChange_FirewallRuleCollection_ContainsRuleDetailsTable` | Verifies the table contains expected columns (Rule Name, Description, Protocols, etc.) and description content |
| `RenderResourceChange_FirewallRuleCollection_ModifiedDetailsInCollapsible` | Verifies that modified rule details are in a collapsible section |
| `Render_MultiModulePlan_GroupsModulesAndPreservesOrder` | Verifies that plans with multiple and nested modules are grouped into module sections and that module ordering follows the hierarchy (root first, children after parents) |
| `Render_MultiModulePlan_HeadingsAndHierarchyAreCorrect` | Verifies module headers use H3 (`### Module: ...`) and resources inside modules use H4 headings (`#### <action> <address>`), and that each resource appears under its module section |
| `Render_FirewallModifiedRules_ShowsDiffForChangedAttributes` | Verifies that modified firewall rules show before/after diff format with `-` and `+` prefixes for changed attributes |
| `Render_FirewallModifiedRules_ShowsSingleValueForUnchangedAttributes` | Verifies that unchanged attributes in modified firewall rules show single values without diff formatting |
| `Render_FirewallNonModifiedRules_DisplayAsExpected` | Verifies that added, removed, and unchanged firewall rules display correctly without diff formatting |

### Scriban Helpers Tests (`MarkdownGeneration/ScribanHelpersTests.cs`)

Tests for the custom Scriban helper functions used in templates.

| Test Name | Description |
|-----------|-------------|
| `DiffArray_WithAddedItems_ReturnsAddedCollection` | Verifies that items present only in the after array are returned as added |
| `DiffArray_WithRemovedItems_ReturnsRemovedCollection` | Verifies that items present only in the before array are returned as removed |
| `DiffArray_WithModifiedItems_ReturnsModifiedCollectionWithBeforeAndAfter` | Verifies that items with changed values are returned with both before and after states |
| `DiffArray_WithUnchangedItems_ReturnsUnchangedCollection` | Verifies that identical items are returned as unchanged |
| `DiffArray_WithMixedChanges_ReturnsAllCategories` | Verifies that mixed add/remove/modify/unchanged scenarios are handled correctly |
| `DiffArray_WithEmptyBeforeArray_ReturnsAllAsAdded` | Verifies that all items are added when before array is empty |
| `DiffArray_WithEmptyAfterArray_ReturnsAllAsRemoved` | Verifies that all items are removed when after array is empty |
| `DiffArray_WithNullBeforeArray_ReturnsAllAsAdded` | Verifies that null before array is handled as empty |
| `DiffArray_WithNullAfterArray_ReturnsAllAsRemoved` | Verifies that null after array is handled as empty |
| `DiffArray_WithMissingKeyProperty_ThrowsScribanHelperException` | Verifies that missing key property throws descriptive exception |
| `DiffArray_WithNestedArrays_ComparesCorrectly` | Verifies that nested array changes are detected |
| `DiffArray_WithNestedObjects_ComparesCorrectly` | Verifies that nested object changes are detected |
| `RegisterHelpers_AddsDiffArrayFunction` | Verifies that `diff_array` function is registered with ScriptObject |

### Scriban Helpers Format Diff Tests (`MarkdownGeneration/ScribanHelpersFormatDiffTests.cs`)

Tests for the `format_diff` helper function used to display before/after values in templates.

| Test Name | Description |
|-----------|-------------|
| `FormatDiff_EqualStrings_ReturnsSingleValue` | Verifies that equal before and after values return the value as-is without diff formatting |
| `FormatDiff_DifferentStrings_ReturnsDiffFormat` | Verifies that different values return `"- before<br>+ after"` format |
| `FormatDiff_NullBefore_ReturnsDiffFormat` | Verifies that null before value is treated as empty string in diff format |
| `FormatDiff_NullAfter_ReturnsDiffFormat` | Verifies that null after value is treated as empty string in diff format |
| `FormatDiff_BothNull_ReturnsEmptyString` | Verifies that both null values return empty string |
| `FormatDiff_EmptyStrings_HandledCorrectly` | Verifies that empty strings are handled correctly and distinguished from null |

### Docker Integration Tests (`Docker/DockerIntegrationTests.cs`)

End-to-end integration tests that run the application in a Docker container. These tests are skippable when Docker is unavailable.

| Test Name | Description |
|-----------|-------------|
| `Docker_WithFileInput_ProducesMarkdownOutput` | Verifies that the container correctly processes a plan file mounted as a volume and produces valid Markdown output |
| `Docker_WithStdinInput_ProducesMarkdownOutput` | Verifies that the container correctly processes plan JSON from stdin and produces valid Markdown output |
| `Docker_WithHelpFlag_DisplaysHelp` | Verifies that the `--help` flag displays usage information in the container |
| `Docker_WithVersionFlag_DisplaysVersion` | Verifies that the `--version` flag displays version information in the container |
| `Docker_WithInvalidInput_ReturnsNonZeroExitCode` | Verifies that invalid JSON input results in a non-zero exit code and error message |
| `Docker_ParsesAllResourceChanges` | Verifies that all expected resources from the test data appear in the container's output |
### Markdown Lint Integration Tests (`MarkdownGeneration/MarkdownLintIntegrationTests.cs`)

Docker-based integration tests that run the actual markdownlint-cli2 tool to validate markdown output. These tests use the `davidanson/markdownlint-cli2:v0.20.0` Docker image to ensure consistent validation across all environments.

| Test Name | Description |
|-----------|-------------|
| `Lint_ComprehensiveDemo_PassesAllRules` | Verifies the comprehensive demo output passes all markdownlint rules |
| `Lint_AllTestPlans_PassAllRules` | Verifies all test plans in TestData produce valid markdown |
| `Lint_SummaryTemplate_PassesAllRules` | Verifies the summary template produces valid markdown |
| `Lint_BreakingPlan_PassesAllRules` | Verifies markdown with special characters passes linting |

### Markdown Invariant Tests (`MarkdownGeneration/MarkdownInvariantTests.cs`)

Property-based tests that verify markdown invariants that must ALWAYS hold, regardless of input. These tests define the "contract" of valid markdown output.

| Test Name | Description |
|-----------|-------------|
| `Invariant_NoConsecutiveBlankLines_AllPlans` | MD012: Verifies no plan produces more than one consecutive blank line |
| `Invariant_NoConsecutiveBlankLines_ComprehensiveDemo` | MD012: Specific check for comprehensive demo |
| `Invariant_AllTablesParseCorrectly_AllPlans` | Verifies all tables parse correctly with Markdig |
| `Invariant_NoBlankLinesBetweenTableRows_AllPlans` | Verifies table rows are consecutive without blank lines |
| `Invariant_NoRawNewlinesInTableCells_AllPlans` | Verifies no raw newlines exist inside table cells |
| `Invariant_PipesEscapedInTableCells_BreakingPlan` | Verifies pipes are escaped in table cells |
| `Invariant_NewlinesConvertedToBr_BreakingPlan` | Verifies newlines are converted to `<br/>` |
| `Invariant_HeadingsSurroundedByBlankLines_AllPlans` | Verifies headings have proper spacing |
| `Invariant_DetailsTagsBalanced_AllPlans` | Verifies all `<details>` tags are properly closed |
| `Invariant_SummaryTagsBalanced_AllPlans` | Verifies all `<summary>` tags are properly closed |
| `Invariant_HasTerraformPlanHeading_AllPlans` | Verifies every plan has a Terraform Plan heading |
| `Invariant_HasSummarySection_NonEmptyPlans` | Verifies non-empty plans have a Summary section |

### Markdown Snapshot Tests (`MarkdownGeneration/MarkdownSnapshotTests.cs`)

Golden file tests that detect unexpected changes in markdown output by comparing against approved baselines.

| Test Name | Description |
|-----------|-------------|
| `Snapshot_ComprehensiveDemo_MatchesBaseline` | Verifies comprehensive demo matches stored snapshot |
| `Snapshot_SummaryTemplate_MatchesBaseline` | Verifies summary template matches stored snapshot |
| `Snapshot_BreakingPlan_MatchesBaseline` | Verifies breaking plan (special chars) matches stored snapshot |
| `Snapshot_RoleAssignments_MatchesBaseline` | Verifies role assignment rendering matches stored snapshot |
| `Snapshot_FirewallRules_MatchesBaseline` | Verifies firewall rule rendering matches stored snapshot |
| `Snapshot_MultiModule_MatchesBaseline` | Verifies multi-module plan matches stored snapshot |

### Template Isolation Tests (`MarkdownGeneration/TemplateIsolationTests.cs`)

Tests that verify each template produces valid markdown with controlled inputs. These tests isolate template behavior from full plan complexity.

| Test Name | Description |
|-----------|-------------|
| `DefaultTemplate_CreateAction_ProducesValidMarkdown` | Verifies default template with create actions |
| `DefaultTemplate_DeleteAction_ProducesValidMarkdown` | Verifies default template with delete actions |
| `DefaultTemplate_EmptyPlan_ProducesValidMarkdown` | Verifies default template with empty plans |
| `DefaultTemplate_SpecialCharacters_EscapesCorrectly` | Verifies special character escaping |
| `RoleAssignmentTemplate_WithPrincipals_ProducesValidMarkdown` | Verifies role assignment template |
| `RoleAssignmentTemplate_WithoutPrincipals_ProducesValidMarkdown` | Verifies role assignment without principal mapping |
| `RoleAssignmentTemplate_NoBlankLinesBetweenTableRows` | Verifies no blank lines in role assignment tables |
| `FirewallTemplate_RuleChanges_ProducesValidMarkdown` | Verifies firewall template |
| `FirewallTemplate_ShowsRuleComparison` | Verifies firewall rule comparison display |
| `SummaryTemplate_ProducesValidMarkdown` | Verifies summary template |
| `SummaryTemplate_EmptyPlan_ProducesValidMarkdown` | Verifies summary template with empty plans |
| `MultiModule_ProperHeadingHierarchy` | Verifies multi-module heading hierarchy |

### Markdown Fuzz Tests (`MarkdownGeneration/MarkdownFuzzTests.cs`)

Fuzz testing with random/edge-case inputs to find escaping bugs and edge cases that break markdown rendering.

| Test Name | Description |
|-----------|-------------|
| `Fuzz_PipeInResourceName_EscapedCorrectly` (Theory) | Verifies pipe escaping in resource names |
| `Fuzz_AsterisksInValues_RenderedCorrectly` (Theory) | Verifies asterisks remain readable while tables stay valid |
| `Fuzz_UnderscoresInValues_EscapedCorrectly` (Theory) | Verifies underscores remain readable in table cells |
| `Fuzz_BracketsInValues_EscapedCorrectly` (Theory) | Verifies bracket escaping |
| `Fuzz_HashInValues_EscapedCorrectly` (Theory) | Verifies hash symbol handling |
| `Fuzz_BackticksInValues_EscapedCorrectly` (Theory) | Verifies backtick escaping |
| `Fuzz_NewlinesInValues_ConvertedToBr` (Theory) | Verifies newline conversion |
| `Fuzz_UnicodeInValues_HandledCorrectly` (Theory) | Verifies Unicode character handling |
| `Fuzz_LongValues_DontBreakTables` (Theory) | Verifies long values don't break tables |
| `Fuzz_LongResourceNames_DontBreakTables` (Theory) | Verifies long names don't break tables |
| `Fuzz_EmptyValues_DontBreakTables` | Verifies empty value handling |
| `Fuzz_WhitespaceValues_DontBreakTables` (Theory) | Verifies whitespace-only value handling |
| `Fuzz_CombinedSpecialChars_AllEscaped` (Theory) | Verifies combined special characters |
| `Fuzz_RandomPlans_ProduceValidMarkdown` | Generates random plans and validates output |