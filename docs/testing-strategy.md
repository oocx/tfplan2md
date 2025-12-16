# Testing Strategy

## Overview

The tfplan2md project uses a comprehensive testing strategy with **xUnit** as the test framework. All tests are located in the `Oocx.TfPlan2Md.Tests` project.

## Test Types

### Unit Tests

Test individual components in isolation to verify correct behavior of parsing, model building, markdown rendering, and CLI argument parsing.

### Integration Tests

Test JSON parsing and markdown generation end-to-end. As the application is distributed via Docker, Docker-based integration tests verify the final CLI behavior in a containerized environment.

## Test Infrastructure

- **Framework**: xUnit 2.9.3
- **Assertion Library**: AwesomeAssertions (fluent-style assertions)
- **Test SDK**: Microsoft.NET.Test.Sdk 17.14.1
- **Coverage**: Coverlet 6.0.4
- **Skippable Tests**: Xunit.SkippableFact 1.5.23 (for conditional test execution when Docker is unavailable)
- **Shared Fixtures**: `DockerFixture` provides a shared Docker container context for integration tests using xUnit's `IAsyncLifetime` and `ICollectionFixture`

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
| `RenderResourceChange_FirewallRuleCollection_ShowsAddedRules` | Verifies that added rules are shown with ➕ indicator |
| `RenderResourceChange_FirewallRuleCollection_ShowsModifiedRules` | Verifies that modified rules are shown with 🔄 indicator |
| `RenderResourceChange_FirewallRuleCollection_ShowsRemovedRules` | Verifies that removed rules are shown with ❌ indicator |
| `RenderResourceChange_FirewallRuleCollection_ShowsUnchangedRules` | Verifies that unchanged rules are shown with ⏺️ indicator |
| `RenderResourceChange_FirewallRuleCollection_Create_ShowsAllRules` | Verifies that new rule collections show all rules being created |
| `RenderResourceChange_FirewallRuleCollection_Delete_ShowsAllRulesBeingDeleted` | Verifies that deleted rule collections show all rules being removed |
| `RenderResourceChange_NonFirewallResource_ReturnsNull` | Verifies that resources without specific templates return null for default handling |
| `RenderResourceChange_FirewallRuleCollection_ContainsRuleDetailsTable` | Verifies the table contains expected columns (Rule Name, Description, Protocols, etc.) and description content |
| `RenderResourceChange_FirewallRuleCollection_ModifiedDetailsInCollapsible` | Verifies that modified rule details are in a collapsible section |

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
