using System.IO;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.RenderTargets;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the azuredevops_build_definition Scriban template.
/// Verifies template structure, table layouts, and proper handling of different operations.
/// Related feature: docs/features/094-build-definition-tables.
/// </summary>
public class BuildDefinitionTemplateTests
{
    private const string Nbsp = "\u00A0";
    private readonly TerraformPlanParser _parser = new();

    private static ReportModelBuilder CreateBuilder()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureDevOpsModule(
            largeValueFormat: LargeValueFormat.InlineDiff));
        return new ReportModelBuilder(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);
    }

    private static MarkdownRenderer CreateRenderer()
    {
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureDevOpsModule(
            largeValueFormat: LargeValueFormat.InlineDiff));
        return new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);
    }

    #region TC-14: Create Operation Layout

    /// <summary>
    /// TC-14: Verifies template renders create operation with metadata and variables table.
    /// </summary>
    [Test]
    public void Create_RendersSummaryMetadataAndVariablesTable()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.create_basic");

        // Assert - Summary line
        section.Should().Contain($"<summary>{ActionIcons.Add}{Nbsp}azuredevops_build_definition <b><code>create_basic</code></b>");

        // Assert - Metadata
        section.Should().Contain("**Pipeline Name:** <code>My Pipeline</code>");
        section.Should().Contain("**Path:** <code>\\Pipelines\\CI</code>");
        section.Should().Contain("**Agent Pool:** <code>Default</code>");

        // Assert - Variables section header
        section.Should().Contain("#### Variables");

        // Assert - Table structure (NO Change column for create)
        section.Should().Contain("| Name | Value | Is Secret | Allow Override |");
        section.Should().Contain("| ---- | ----- | --------- | -------------- |");

        // Assert - Variable data
        section.Should().Contain("| `BUILD_CONFIGURATION` | `Release` | `false` | `true` |");
        section.Should().Contain("| `BUILD_PLATFORM` | `Any CPU` | `false` | `false` |");
    }

    /// <summary>
    /// TC-14: Verifies CI trigger and repository sections render for create operation.
    /// </summary>
    [Test]
    public void Create_RendersCiTriggerAndRepositoryTables()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.create_basic");

        // Assert - CI Trigger section
        section.Should().Contain("#### CI Trigger");
        section.Should().Contain("| Use YAML | Override (Branch Filters) |");
        section.Should().Contain("| `true` |");
        section.Should().Contain("main");
        section.Should().Contain("develop");

        // Assert - Repository section
        section.Should().Contain("#### Repository");
        section.Should().Contain("| Type | Repo ID | Branch | YAML Path | Report Build Status |");
        section.Should().Contain("| `TfsGit` |");
        section.Should().Contain("| `azure-pipelines.yml` |");
        section.Should().Contain("| `true` |");
    }

    #endregion

    #region TC-15: Update Operation with Change Indicators

    /// <summary>
    /// TC-15: Verifies template renders update operation with change indicators and diffs.
    /// </summary>
    [Test]
    public void Update_RendersChangeIndicatorsAndDiffs()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.update_variables");

        // Assert - Summary line - should use Replace icon (🔄 = update, ♻️ = replace in ActionIcons)
        section.Should().Contain($"<summary>{ActionIcons.Update}{Nbsp}azuredevops_build_definition <b><code>update_variables</code></b>");

        // Assert - Variables section header
        section.Should().Contain("#### Variables");

        // Assert - Table structure (WITH Change column for update)
        section.Should().Contain("| Change | Name | Value | Is Secret | Allow Override |");
        section.Should().Contain("| ------ | ---- | ----- | --------- | -------------- |");

        // Assert - Added variable (➕)
        section.Should().Contain($"| {ActionIcons.Add} | `NEW_VAR` |");
        section.Should().Contain("| `new-value` |");

        // Assert - Modified variable (🔄)
        section.Should().Contain($"| {ActionIcons.Update} | `ENV` |");

        // Assert - Should contain before/after diff
        section.Should().Contain("<code style=\"display:block;"); // HTML code block for diff
        section.Should().Contain("- "); // Minus prefix
        section.Should().Contain("+ "); // Plus prefix

        // Assert - Removed variable (❌)
        section.Should().Contain($"| {ActionIcons.Delete} | `OLD_VAR` |");
    }

    /// <summary>
    /// TC-15: Verifies update operation shows before/after for CI trigger and repository.
    /// </summary>
    [Test]
    public void Update_ShowsBeforeAfterForNestedBlocks()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.update_variables");

        // Assert - CI Trigger section exists (after state)
        section.Should().Contain("#### CI Trigger");
        section.Should().Contain("| `true` |");

        // Assert - Repository section exists (after state)
        section.Should().Contain("#### Repository");
        section.Should().Contain("| `pipelines/ci.yml` |");
    }

    #endregion

    #region TC-16: Delete Operation Layout

    /// <summary>
    /// TC-16: Verifies template renders delete operation with "being deleted" label.
    /// </summary>
    [Test]
    public void Delete_RendersBeingDeletedVariablesTable()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.delete_basic");

        // Assert - Summary line
        section.Should().Contain($"<summary>{ActionIcons.Delete}{Nbsp}azuredevops_build_definition <b><code>delete_basic</code></b>");

        // Assert - Section header for delete
        section.Should().Contain("#### Variables (being deleted)");

        // Assert - Table structure (NO Change column for delete)
        section.Should().Contain("| Name | Value | Is Secret | Allow Override |");

        // Assert - Variables displayed
        section.Should().Contain("| `LEGACY_VAR` | `legacy-value` |");
        section.Should().Contain("| `SECRET_TOKEN` | `(sensitive / hidden)` |");
    }

    #endregion

    #region TC-17: Secret Variables Never Leaked

    /// <summary>
    /// TC-17: Verifies secret variables always show masked value.
    /// </summary>
    [Test]
    public void Create_SecretVariables_ValueAlwaysMasked()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.create_with_secrets");

        // Assert - Secret variables show masked value
        section.Should().Contain("| `API_KEY` | `(sensitive / hidden)` | `true` | `true` |");
        section.Should().Contain("| `DB_PASSWORD` | `(sensitive / hidden)` | `true` | `false` |");

        // Assert - Actual secret values NEVER appear
        section.Should().NotContain("super-secret-key-123");
        section.Should().NotContain("p@ssw0rd");

        // Assert - Regular variable still shows value
        section.Should().Contain("| `ENV` | `production` | `false` | `true` |");
    }

    /// <summary>
    /// TC-17: Verifies secret variables remain masked in delete operations.
    /// </summary>
    [Test]
    public void Delete_SecretVariables_ValueAlwaysMasked()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.delete_basic");

        // Assert - Secret variable masked in delete
        section.Should().Contain("| `SECRET_TOKEN` | `(sensitive / hidden)` |");

        // Assert - Actual secret value NEVER appears
        section.Should().NotContain("secret-token-value");
    }

    #endregion

    #region TC-18: CI Trigger and Repository Blocks

    /// <summary>
    /// TC-18: Verifies CI trigger and repository blocks render correctly.
    /// </summary>
    [Test]
    public void Create_CiTriggerAndRepository_DisplayCorrectly()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.create_basic");

        // Assert - CI Trigger table
        section.Should().Contain("#### CI Trigger");
        section.Should().Contain("| Use YAML | Override (Branch Filters) |");
        section.Should().Contain("| `true` |");

        // Assert - Repository table
        section.Should().Contain("#### Repository");
        section.Should().Contain("| Type | Repo ID | Branch | YAML Path | Report Build Status |");
        section.Should().Contain("| `TfsGit` |");
        section.Should().Contain("refs/heads/main");
    }

    #endregion

    #region TC-19: Pull Request Trigger Block

    /// <summary>
    /// TC-19: Verifies pull request trigger block displays correctly.
    /// </summary>
    [Test]
    public void Create_PullRequestTrigger_DisplaysCorrectly()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.with_all_triggers");

        // Assert - Pull Request Trigger section exists
        section.Should().Contain("#### Pull Request Trigger");
        section.Should().Contain("| Use YAML | Override (Branch Filters) | Forks Enabled | Forks Comment Requirement |");
        section.Should().Contain("| `false` |");
        section.Should().Contain("| `true` |");
        section.Should().Contain("| - |"); // Comment requirement can be dash
    }

    #endregion

    #region TC-20: Schedules and Repository Display

    /// <summary>
    /// TC-20: Verifies schedules block displays correctly.
    /// </summary>
    [Test]
    public void Create_Schedules_DisplayCorrectly()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.with_all_triggers");

        // Assert - Schedules section exists
        section.Should().Contain("#### Schedules");
        section.Should().Contain("| Branch Filters | Days to Build | Schedule Only With Changes | Start Time | Time Zone |");
        section.Should().Contain("Mon");
        section.Should().Contain("Wed");
        section.Should().Contain("Fri");
        section.Should().Contain("| `09:30` |");
        section.Should().Contain("| `true` |");
    }

    /// <summary>
    /// TC-20: Verifies repository with GitHub displays service connection (if column exists).
    /// </summary>
    [Test]
    public void Create_RepositoryWithGitHub_ShowsRepositoryInfo()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.with_all_triggers");

        // Assert - Repository section with GitHub details
        section.Should().Contain("#### Repository");
        section.Should().Contain("| `GitHub` |");
        section.Should().Contain("myorg/myrepo");
        // Note: service_connection_id may not be in table columns depending on template design
    }

    #endregion

    #region TC-21: Conditional Rendering - No Empty Tables

    /// <summary>
    /// TC-21: Verifies template does not render empty tables for nested blocks.
    /// </summary>
    [Test]
    public void Create_EmptyNestedBlocks_DoesNotRenderEmptyTables()
    {
        // Arrange & Act
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_build_definition.empty_nested_blocks");

        // Assert - Variables section exists (has data)
        section.Should().Contain("#### Variables");
        section.Should().Contain("| `SINGLE_VAR` |");

        // Assert - Empty sections do NOT render
        // We check that there's no CI Trigger section at all
        var ciTriggerExists = section.Contains("#### CI Trigger");
        ciTriggerExists.Should().BeFalse("CI Trigger section should not render when empty");

        var prTriggerExists = section.Contains("#### Pull Request Trigger");
        prTriggerExists.Should().BeFalse("Pull Request Trigger section should not render when empty");

        var schedulesExists = section.Contains("#### Schedules");
        schedulesExists.Should().BeFalse("Schedules section should not render when empty");

        var repoExists = section.Contains("#### Repository");
        repoExists.Should().BeFalse("Repository section should not render when empty");
    }

    #endregion

    #region TC-22: Report Style Guide Compliance

    /// <summary>
    /// TC-22: Verifies template follows Report Style Guide for formatting.
    /// </summary>
    [Test]
    public void Template_FollowsReportStyleGuide()
    {
        // Arrange & Act
        var markdown = Render();

        // Assert - <code> tags used in summary (Azure DevOps compatibility)
        markdown.Should().Contain("<code>create_basic</code>");
        markdown.Should().Contain("<code>My Pipeline</code>");

        // Assert - Backticks used for inline code in table cells
        markdown.Should().Contain("| `BUILD_CONFIGURATION` |");
        markdown.Should().Contain("| `Release` |");

        // Assert - Plain text for labels (no backticks)
        markdown.Should().Contain("**Pipeline Name:**");
        markdown.Should().Contain("**Path:**");
        markdown.Should().Contain("**Agent Pool:**");

        // Assert - Table headers are plain text
        markdown.Should().Contain("| Name | Value | Is Secret | Allow Override |");
    }

    #endregion

    #region TC-23: Mapper Registration

    /// <summary>
    /// TC-23: Verifies mapper is registered and template integration works end-to-end.
    /// </summary>
    [Test]
    public void Template_MapperRegistered_IntegrationWorks()
    {
        // Arrange & Act
        var markdown = Render();

        // Assert - At least one build definition section renders
        markdown.Should().Contain("azuredevops_build_definition");

        // Assert - Template successfully accesses view model data
        // (If mapper wasn't registered, the template would fail or show no data)
        markdown.Should().Contain("**Pipeline Name:**");
        markdown.Should().Contain("#### Variables");
    }

    #endregion

    #region Helper Methods

    private string Render()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureDevOpsBuildDefinitionPlanPath));
        var builder = CreateBuilder();
        var model = builder.Build(plan);
        var renderer = CreateRenderer();
        return renderer.Render(model);
    }

    /// <summary>
    /// Extracts a resource section from markdown based on the resource address.
    /// </summary>
    /// <param name="markdown">The full markdown document.</param>
    /// <param name="address">The terraform resource address (e.g., "azuredevops_build_definition.create_basic").</param>
    /// <returns>The content of the resource section.</returns>
    private static string ExtractSection(string markdown, string address)
    {
        // Parse address to get resource type and name
        var parts = address.Split('.');
        var resourceType = parts[0];
        var resourceName = parts.Length > 1 ? parts[1] : parts[0];
        // Look for a <details> block containing the resource name in <b><code>{name}</code></b>
        var pattern = $@"(?s)<details[^>]*>\s*<summary>[^<]*{Regex.Escape(resourceType)}\s+<b><code>{Regex.Escape(resourceName)}</code></b>(.*?)</details>";
        var match = Regex.Match(markdown, pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        return match.Success ? match.Value : string.Empty;
    }

    #endregion
}
