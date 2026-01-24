using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.RenderTargets;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the azuredevops_variable_group Scriban template.
/// Verifies template structure, table layouts, and proper handling of different operations.
/// Related feature: docs/features/039-azdo-variable-group-template.
/// </summary>
public class VariableGroupTemplateTests
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

    [Test]
    public void Create_RendersSummaryAndVariablesTable()
    {
        // TC-14: Template renders create operation layout (no Change column)
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.create_basic");
        // Verify summary line
        section.Should().Contain($"<summary>➕{Nbsp}azuredevops_variable_group <b><code>create_basic</code></b>");
        // Verify variable group name and description
        section.Should().Contain("**Variable Group:** <code>basic-vars</code>");
        section.Should().Contain("**Description:** <code>Basic variable group for testing</code>");
        // Verify table structure (NO Change column for create)
        section.Should().Contain("| Name | Value | Enabled | Content Type | Expires |");
        section.Should().Contain("| ---- | ----- | ------- | ------------ | ------- |");
        // Verify regular variable displayed
        section.Should().Contain("| `ENV` | `Development` | - | - | - |");
        // Verify secret variable value is masked
        section.Should().Contain("| `API_KEY` | `(sensitive / hidden)` | - | - | - |");
    }

    [Test]
    public void Update_RendersChangeIndicatorsAndDiffs()
    {
        // TC-15: Template renders update operation layout with change indicators
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.update_mixed");
        // Verify summary line
        section.Should().Contain($"<summary>♻️{Nbsp}azuredevops_variable_group <b><code>update_mixed</code></b>");
        // Verify table structure (WITH Change column for update)
        section.Should().Contain("| Change | Name | Value | Enabled | Content Type | Expires |");
        section.Should().Contain("| ------ | ---- | ----- | ------- | ------------ | ------- |");
        // Verify added variable (➕)
        section.Should().Contain("| ➕ | `NEW_VAR` |");
        // Verify modified variable with diff (🔄)
        section.Should().Contain("| 🔄 | `APP_VERSION` |");
        // Should contain before/after diff with HTML styling
        section.Should().Contain("<span style=\"background-color: #fff5f5");
        section.Should().Contain("<span style=\"background-color: #f0fff4");
        // Verify removed variable (❌)
        section.Should().Contain("| ❌ | `OLD_VAR` |");
    }

    [Test]
    public void Update_SecretVariableMetadataChanges_ValueRemainsMasked()
    {
        // Verifies secret variable metadata changes display correctly while value remains masked
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.update_secret_metadata");
        // Verify secret variable row exists
        section.Should().Contain("| 🔄 | `SECRET_KEY` |");
        // Verify value is masked (no diff shown for secret value)
        section.Should().Contain("`(sensitive / hidden)`");
        // Verify metadata changes are shown (enabled changed from false to true)
        // The diff formatting includes HTML for styling
        section.Should().Contain("false");
        section.Should().Contain("<br>");
    }

    [Test]
    public void Delete_RendersBeingDeletedVariablesTable()
    {
        // TC-16: Template renders delete operation layout
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.delete_basic");
        // Verify summary line
        section.Should().Contain($"<summary>❌{Nbsp}azuredevops_variable_group <b><code>delete_basic</code></b>");
        // Verify section header for delete
        section.Should().Contain("#### Variables (being deleted)");
        // Verify table structure (NO Change column for delete)
        section.Should().Contain("| Name | Value | Enabled | Content Type | Expires |");
        // Verify variables are displayed
        section.Should().Contain("| `ENV` | `Staging` |");
        section.Should().Contain("| `SECRET_TOKEN` | `(sensitive / hidden)` |");
    }

    [Test]
    public void KeyVaultIntegration_RendersKeyVaultTable()
    {
        // TC-22: Template renders Key Vault section when blocks present
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.with_keyvault");
        // Verify Key Vault section exists
        section.Should().Contain("#### Key Vault Integration");
        // Verify Key Vault table structure
        section.Should().Contain("| Name | Service Endpoint ID | Search Depth |");
        section.Should().Contain("| ---- | ------------------- | ------------ |");
        // Verify Key Vault data
        section.Should().Contain("prod-keyvault");
        section.Should().Contain("aaaaaaaa-1111-1111-1111-111111111111");
        section.Should().Contain("| `1` |");
    }

    [Test]
    public void Template_FollowsReportStyleGuide()
    {
        // TC-21: Template follows Report Style Guide (inline code for values, plain text for labels)
        var markdown = Render();
        // Verify <code> tags used in summary (Azure DevOps compatibility)
        markdown.Should().Contain("<code>create_basic</code>");
        markdown.Should().Contain("<code>basic-vars</code>");
        // Verify backticks used for inline code in table cells
        markdown.Should().Contain("| `ENV` |");
        markdown.Should().Contain("| `Development` |");
        // Verify plain text for labels (no backticks)
        markdown.Should().Contain("**Variable Group:**");
        markdown.Should().Contain("**Description:**");
        // Verify table headers are plain text
        markdown.Should().Contain("| Name | Value | Enabled |");
    }

    [Test]
    public void Template_HandlesEmptyCollections()
    {
        // Edge case: Template should not error when arrays are empty
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.empty_variables");
        // Verify summary still renders
        section.Should().Contain($"<summary>➕{Nbsp}azuredevops_variable_group <b><code>empty_variables</code></b>");
        // Verify variable group name renders
        section.Should().Contain("**Variable Group:** <code>empty-vars</code>");
        // Should not have variable table section (no variables to display)
        section.Should().NotContain("#### Variables");
    }

    [Test]
    public void Template_HandlesNullDescription()
    {
        // Edge case: Template should handle null/missing description gracefully
        var markdown = Render();
        var section = ExtractSection(markdown, "azuredevops_variable_group.no_description");
        // Verify summary and name render
        section.Should().Contain("**Variable Group:** <code>no-desc-vars</code>");
        // Should not have description line when description is null/empty
        section.Should().NotContain("**Description:**");
    }

    private string Render()
    {
        var plan = _parser.Parse(File.ReadAllText(DemoPaths.AzureDevOpsVariableGroupPlanPath));
        var builder = CreateBuilder();
        var model = builder.Build(plan);
        var renderer = CreateRenderer();
        return renderer.Render(model);
    }

    /// <summary>
    /// Extracts a resource section from markdown based on the resource address.
    /// </summary>
    /// <param name="markdown">The full markdown document.</param>
    /// <param name="address">The terraform resource address (e.g., "azuredevops_variable_group.create_basic").</param>
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
}
