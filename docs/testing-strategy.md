# Testing Strategy

## Overview

The tfplan2md project uses a comprehensive testing strategy with **xUnit** as the test framework. All tests are located in the `Oocx.TfPlan2Md.Tests` project.

## Test Types

### Unit Tests

Test individual components in isolation to verify correct behavior of parsing, model building, markdown rendering, and CLI argument parsing.

### Integration Tests

Test JSON parsing and markdown generation end-to-end. As the application is distributed via Docker, Docker-based integration tests verify the final CLI behavior in a containerized environment.

### User Acceptance Testing (UAT)

For features that affect markdown rendering (e.g., changes to templates, snapshots, or examples), we perform manual verification in real environments to ensure compatibility with different markdown renderers.

**Workflow:**
1. **Trigger**: Feature changes rendering output.
2. **GitHub UAT**:
   - Code Reviewer agent creates a test PR in `oocx/tfplan2md`.
   - Maintainer reviews the PR comment/description rendering.
   - **Feedback Loop**: Maintainer provides feedback via PR comments. Agent polls for new comments to identify issues.
   - Feedback is addressed until approved.
3. **Azure DevOps UAT**:
   - Code Reviewer agent creates a test PR in Azure DevOps (`oocx` organization, `test` project).
   - Maintainer reviews the PR description rendering.
   - **Feedback Loop**: Maintainer provides feedback via PR comments. Agent polls for new comments to identify issues.
   - Feedback is addressed until approved.

**Technical Instructions:**

**GitHub:**
- **Create PR**: `gh pr create --title "UAT: <Feature Name>" --body-file artifacts/comprehensive-demo.md --base main --head <feature-branch>`
- **Poll Comments**: `PAGER=cat gh pr view <pr-number> --comments` (Always use PAGER=cat to avoid blocking)
- **Update PR**: Push changes to the feature branch.

**Azure DevOps:**
- **Authenticate**: Check `az account show`. If failed, ask user to run `az login`.
- **Push Branch**:
  ```bash
  git remote add azdo https://oocx@dev.azure.com/oocx/test/_git/test || true
  git push azdo HEAD:<feature-branch>
  ```
- **Create PR**:
  ```bash
  az repos pr create --organization https://dev.azure.com/oocx --project test --repository test --source-branch <feature-branch> --target-branch main --title "UAT: <Feature Name>" --description "$(cat artifacts/comprehensive-demo.md)"
  ```
- **Poll Comments**: `az repos pr thread list --organization https://dev.azure.com/oocx --project test --repository test --pull-request-id <pr-id>`
- **Update PR**: Push changes to the feature branch.

## Test Infrastructure

- **Framework**: xUnit 2.9.3
- **Assertion Library**: AwesomeAssertions (fluent-style assertions)
- **Test SDK**: Microsoft.NET.Test.Sdk 17.14.1
- **Coverage**: Coverlet 6.0.4
- **Skippable Tests**: Xunit.SkippableFact 1.5.23 (for conditional test execution when Docker is unavailable)
- **Shared Fixtures**: `DockerFixture` provides a shared Docker container context for integration tests using xUnit's `IAsyncLifetime` and `ICollectionFixture`
- **Shell compatibility**: Release script tests include a POSIX-only run (`POSIXLY_CORRECT=1`) to ensure no GNU awk extensions are required.

> Note: Tests were migrated from `xUnit.Assert` to `AwesomeAssertions` to improve readability and expressiveness of test assertions.

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

| Test Name | Description |
|-----------|-------------|
| `Parse_NoArgs_ReturnsDefaultOptions` | Verifies that parsing empty arguments returns default option values (all nulls and false flags) |
| `Parse_InputFileArg_SetsInputFile` | Verifies that a positional argument is correctly parsed as the input file path |
| `Parse_OutputFlag_SetsOutputFile` | Verifies that `--output` flag correctly sets the output file path |
| `Parse_ShortOutputFlag_SetsOutputFile` | Verifies that `-o` short flag correctly sets the output file path |
| `Parse_TemplateFlag_SetsTemplatePath` | Verifies that `--template` flag correctly sets the custom template path |
| `Parse_ShortTemplateFlag_SetsTemplatePath` | Verifies that `-t` short flag correctly sets the custom template path |
| `Parse_ShowSensitiveFlag_SetsShowSensitive` | Verifies that `--show-sensitive` flag enables display of sensitive values |
| `Parse_HelpFlag_SetsShowHelp` | Verifies that `--help` flag sets the help display option |
| `Parse_ShortHelpFlag_SetsShowHelp` | Verifies that `-h` short flag sets the help display option |
| `Parse_VersionFlag_SetsShowVersion` | Verifies that `--version` flag sets the version display option |
| `Parse_ShortVersionFlag_SetsShowVersion` | Verifies that `-v` short flag sets the version display option |
| `Parse_AllOptions_ParsesCorrectly` | Verifies that combining multiple options (input file, output, template, show-sensitive) parses correctly |
| `Parse_UnknownOption_ThrowsCliParseException` | Verifies that unrecognized options throw a `CliParseException` |
| `Parse_OutputWithoutValue_ThrowsCliParseException` | Verifies that `--output` without a value throws a `CliParseException` |
| `Parse_TemplateWithoutValue_ThrowsCliParseException` | Verifies that `--template` without a value throws a `CliParseException` |

### Terraform Plan Parser Tests (`Parsing/TerraformPlanParserTests.cs`)

Tests for parsing Terraform plan JSON files into the internal model.

| Test Name | Description |
|-----------|-------------|
| `Parse_ValidPlan_ReturnsCorrectVersion` | Verifies that the parser correctly extracts Terraform version (1.14.0) and format version (1.2) |
| `Parse_ValidPlan_ReturnsCorrectResourceCount` | Verifies that all 6 resource changes are parsed from the test plan |
| `Parse_ValidPlan_ParsesCreateAction` | Verifies that resources with "create" action are correctly identified (e.g., `azurerm_resource_group.main`) |
| `Parse_ValidPlan_ParsesUpdateAction` | Verifies that resources with "update" action are correctly identified (e.g., `azurerm_key_vault.main`) |
| `Parse_ValidPlan_ParsesDeleteAction` | Verifies that resources with "delete" action are correctly identified (e.g., `azurerm_virtual_network.old`) |
| `Parse_ValidPlan_ParsesReplaceAction` | Verifies that resources with replace (create+delete) action are correctly identified (e.g., `azuredevops_git_repository.main`) |
| `Parse_ValidPlan_ParsesAzureDevOpsProvider` | Verifies that provider names are correctly extracted (e.g., `registry.terraform.io/microsoft/azuredevops`) |
| `Parse_InvalidJson_ThrowsTerraformPlanParseException` | Verifies that malformed JSON throws a `TerraformPlanParseException` |
| `Parse_EmptyJson_ThrowsTerraformPlanParseException` | Verifies that empty input throws a `TerraformPlanParseException` |
| `Parse_EmptyPlan_ReturnsEmptyResourceChanges` | Verifies that a plan with empty `resource_changes` array is parsed correctly |
| `Parse_NoOpPlan_ParsesNoOpAction` | Verifies that resources with "no-op" action are correctly identified |
| `Parse_MinimalPlan_HandlesNullBeforeAndAfter` | Verifies that plans with null before/after values are parsed without errors |
| `Parse_CreateOnlyPlan_ParsesMultipleCreates` | Verifies that plans with only create operations are parsed correctly |
| `Parse_DeleteOnlyPlan_ParsesMultipleDeletes` | Verifies that plans with only delete operations are parsed correctly |

### Report Model Builder Tests (`MarkdownGeneration/ReportModelBuilderTests.cs`)

Tests for building the report model from a parsed Terraform plan.

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