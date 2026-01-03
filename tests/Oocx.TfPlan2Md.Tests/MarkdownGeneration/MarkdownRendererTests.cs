using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();
    private const string Nbsp = "\u00A0";

    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    private static string Normalize(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        var decoded = WebUtility.HtmlDecode(markdown);
        var withoutTags = Regex.Replace(decoded, "<.*?>", string.Empty, RegexOptions.Singleline);
        var withoutBackticks = withoutTags.Replace("`", string.Empty, StringComparison.Ordinal);
        return Regex.Replace(withoutBackticks, "\\s+", " ", RegexOptions.Singleline).Trim();
    }

    /// <summary>
    /// Extracts a resource section from markdown based on the resource name.
    /// </summary>
    /// <param name="markdown">The full markdown document.</param>
    /// <param name="address">The terraform resource address (e.g., "azurerm_resource_group.main").</param>
    /// <returns>The content of the resource section.</returns>
    private static string ResourceSection(string markdown, string address)
    {
        // Parse address to get resource type and name
        var parts = address.Split('.');
        var resourceType = parts[0];
        var resourceName = parts.Length > 1 ? parts[1] : parts[0];

        // Look for a <details> or <div> block containing the resource name in <b><code>{name}</code></b>
        // Pattern: look for <details...> or <div...> that contains resourceType and resourceName
        var pattern = $@"(?s)(<details[^>]*>|<div[^>]*>)\s*(?:<summary>)?[^<]*{Regex.Escape(resourceType)}\s+<b><code>{Regex.Escape(resourceName)}</code></b>(.*?)(</details>|</div>)";

        var match = Regex.Match(markdown, pattern, RegexOptions.Singleline);
        match.Success.Should().BeTrue($"Resource section not found for {address}");

        return match.Value;
    }

    [Fact]
    public void Render_ValidPlan_ContainsSummarySection()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("## Summary")
            .And.Contain("➕ Add")
            .And.Contain("🔄 Change")
            .And.Contain("❌ Destroy");
    }

    [Fact]
    public void Render_AzureResourceIds_StayInTableWithReadableFormat()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azure-resource-ids.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("key_vault_id")
            .And.Contain("Key Vault `kv-long-name` in resource group `rg-with-a-very-long-name-that-exceeds-one-hundred-characters-threshold` of subscription `12345678-1234-1234-1234-123456789012`");
        markdown.Should().NotContain("Large attributes");
        markdown.Should().NotContain("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-with-a-very-long-name-that-exceeds-one-hundred-characters-threshold/providers/Microsoft.KeyVault/vaults/kv-long-name");
    }

    [Fact]
    public void Render_WithSummaryTemplateName_RendersSummaryOnly()
    {
        // Arrange
        var json = File.ReadAllText("TestData/timestamp-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model, "summary");

        // Assert
        markdown.Should().Contain("Terraform Plan Summary")
            .And.Contain("2025-12-20T10:00:00Z")
            .And.Contain("| Action | Count | Resource Types |")
            .And.NotContain("Resource Changes");
    }

    [Fact]
    public void Render_WithReportTitle_UsesCustomTitleInDefaultTemplate()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(reportTitle: "Custom Title");
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        var firstLine = markdown.Split('\n', 2)[0];
        firstLine.Should().Be("# Custom Title");
    }

    [Fact]
    public void Render_WithDefaultTemplateName_UsesDefaultBuiltIn()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var viaName = _renderer.Render(model, "default");
        var viaDefault = _renderer.Render(model);

        // Assert
        viaName.Should().Be(viaDefault);
    }

    [Fact]
    public void Render_WithCustomTemplateFile_UsesFile()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sbn");
        File.WriteAllText(tempFile, "Custom: {{ terraform_version }}");

        try
        {
            // Act
            var markdown = _renderer.Render(model, tempFile);

            // Assert
            markdown.Should().Contain("Custom: 1.14.0");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Render_WithCustomTemplate_CanAccessReportTitle()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder(reportTitle: "Custom Report Title");
        var model = builder.Build(plan);

        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sbn");
        File.WriteAllText(tempFile, "# {{ report_title ?? \"Default Title\" }}");

        try
        {
            // Act
            var markdown = _renderer.Render(model, tempFile);

            // Assert
            var firstLine = markdown.Split('\n', 2)[0];
            firstLine.Should().Be("# Custom Report Title");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Render_WithUnknownTemplate_ThrowsHelpfulError()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var act = () => _renderer.Render(model, "nonexistent-template");

        // Assert
        var exception = act.Should().Throw<MarkdownRenderException>()
            .Which;
        exception.Message.Should().Contain("Template 'nonexistent-template' not found")
            .And.Contain("default")
            .And.Contain("summary");
    }

    [Fact]
    public void Render_ValidPlan_ContainsResourceChanges()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - Module headers should be present and resources grouped under them
        // Resources are identified by type and name in <b><code>name</code></b> format
        markdown.Should().Contain("Module: root")
            .And.Contain("Module:")
            .And.Contain("azurerm_resource_group <b><code>main</code></b>")
            .And.Contain("azurerm_storage_account <b><code>main</code></b>")
            .And.Contain("azurerm_key_vault <b><code>main</code></b>")
            .And.Contain("azuredevops_project <b><code>main</code></b>");
    }

    [Fact]
    public void Render_ValidPlan_ContainsTerraformVersion()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - version appears in the header and unchanged
        markdown.Should().Contain("1.14.0");
    }



    [Fact]
    public void Render_ValidPlan_ContainsActionSymbols()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - action symbol lines should exist under module sections
        markdown.Should().Contain("➕ azurerm_resource_group <b><code>main</code></b>")
            .And.Contain("🔄 azurerm_key_vault <b><code>main</code></b>")
            .And.Contain("❌ azurerm_virtual_network <b><code>old</code></b>")
            .And.Contain("♻️ azuredevops_git_repository <b><code>main</code></b>");
    }

    [Fact]
    public void Render_SummaryTable_ShowsResourceTypeBreakdown()
    {
        // Arrange
        var json = File.ReadAllText("TestData/create-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("| Resource Types |");
        markdown.Should().Contain("| ➕ Add | 2 |");
        markdown.Should().Contain($"1 {Escape("azurerm_resource_group")}<br/>");
        markdown.Should().Contain($"1 {Escape("azurerm_storage_account")}");
        markdown.Should().NotContain("<br/> |");

        var addRowIndex = markdown.IndexOf("| ➕ Add |", StringComparison.Ordinal);
        var resourceGroupIndex = markdown.IndexOf(Escape("azurerm_resource_group"), addRowIndex, StringComparison.Ordinal);
        var storageAccountIndex = markdown.IndexOf(Escape("azurerm_storage_account"), addRowIndex, StringComparison.Ordinal);
        resourceGroupIndex.Should().BeGreaterThan(-1);
        storageAccountIndex.Should().BeGreaterThan(-1);
        resourceGroupIndex.Should().BeLessThan(storageAccountIndex);
    }

    [Fact]
    public void Render_SummaryTable_NoTrailingBreakInAnyAction()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - no trailing <br/> at end of any summary row
        markdown.Should().NotContain("<br/> |");

        // And ensure multiple types render on separate lines for Add
        markdown.Should().Contain($"{Escape("azuredevops_project")}<br/>")
            .And.Contain(Escape("azurerm_resource_group"))
            .And.Contain(Escape("azurerm_storage_account"));
    }

    [Fact]
    public void Render_SummaryTable_ShowsEmptyCellWhenNoActionResources()
    {
        // Arrange
        var json = File.ReadAllText("TestData/create-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("| ❌ Destroy | 0 |");
    }

    [Fact]
    public void Render_EmptyPlan_ProducesValidMarkdown()
    {
        // Arrange
        var json = File.ReadAllText("TestData/empty-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("## Summary")
            .And.Contain("**Terraform Version:** 1.14.0")
            .And.Contain("| ➕ Add | 0 |")
            .And.Contain("| 🔄 Change | 0 |")
            .And.Contain("| ❌ Destroy | 0 |")
            .And.Contain("| **Total** | **0** | |");

        // And resource changes section should show No changes (module_changes is empty)
        markdown.Should().Contain("## Resource Changes")
            .And.Contain("No changes");
    }

    [Fact]
    public void Render_NoOpPlan_ProducesValidMarkdown()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-op-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - Summary section should exist, but no-op resources are not displayed
        // in the Resource Changes section to avoid iteration limit issues
        markdown.Should().Contain("## Summary")
            .And.NotContain("azurerm_resource_group.main") // no-op resources are filtered
            .And.Contain("No changes"); // Should show "No changes" when all resources are no-op

        // Also ensure Module: root is not present when there are no changes
        markdown.Should().NotContain("Module: root");
    }

    [Fact]
    public void Render_EmptyPlan_ShowsNoChangesMessage()
    {
        // Arrange
        var json = File.ReadAllText("TestData/empty-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("## Resource Changes")
            .And.Contain("No changes");
    }

    [Fact]
    public void Render_MinimalPlan_HandlesNullAttributes()
    {
        // Arrange
        var json = File.ReadAllText("TestData/minimal-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - ensure resource shows within a module section
        markdown.Should().Contain("➕ null_resource <b><code>test</code></b>")
            .And.Contain("<details");
    }

    [Fact]
    public void Render_CreateOnlyPlan_ShowsAllCreates()
    {
        // Arrange
        var json = File.ReadAllText("TestData/create-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("➕ azurerm_resource_group <b><code>main</code></b>")
            .And.Contain("➕ azurerm_storage_account <b><code>main</code></b>")
            .And.Contain("| ➕ Add | 2 |")
            .And.Contain("Module: root");
    }

    [Fact]
    public void Render_CreateOnlyPlan_ShowsAttributeValueTable()
    {
        // Arrange
        var json = File.ReadAllText("TestData/create-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - For creates we should show a 2-column table (Attribute | Value) and the expected values
        var rgSection = ResourceSection(markdown, "azurerm_resource_group.main");
        rgSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("name")} | `{Escape("rg-new-project")}` |")
            .And.Contain($"| {Escape("location")} | `{Escape($"🌍{Nbsp}westeurope")}` |");

        var stSection = ResourceSection(markdown, "azurerm_storage_account.main");
        stSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("account_tier")} | `{Escape("Standard")}` |");
    }

    [Fact]
    public void Render_DeleteOnlyPlan_ShowsAllDeletes()
    {
        // Arrange
        var json = File.ReadAllText("TestData/delete-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert
        markdown.Should().Contain("❌ azurerm_storage_account <b><code>old</code></b>")
            .And.Contain("❌ azurerm_resource_group <b><code>old</code></b>")
            .And.Contain("❌ Destroy | 2");
    }

    [Fact]
    public void Render_DeleteOnlyPlan_ShowsAttributeValueTable()
    {
        // Arrange
        var json = File.ReadAllText("TestData/delete-only-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - For deletes we should show a 2-column table (Attribute | Value) and the expected values
        var stSection = ResourceSection(markdown, "azurerm_storage_account.old");
        stSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("account_tier")} | `{Escape("Standard")}` |")
            .And.Contain($"| {Escape("name")} | `{Escape("stoldproject")}` |")
            .And.Contain($"| {Escape("location")} | `{Escape($"🌍{Nbsp}westeurope")}` |");

        var rgSection = ResourceSection(markdown, "azurerm_resource_group.old");
        rgSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("name")} | `{Escape("rg-old-project")}` |")
            .And.Contain($"| {Escape("location")} | `{Escape($"🌍{Nbsp}westeurope")}` |");
    }

    [Fact]
    public void Render_ReplacePlan_ShowsBeforeAndAfterColumns()
    {
        // Arrange - construct a replace (create+delete) change
        var before = new Dictionary<string, object?>
        {
            ["name"] = "old",
            ["size"] = "small"
        };

        var after = new Dictionary<string, object?>
        {
            ["name"] = "new",
            ["size"] = "large"
        };

        var beforeElement = System.Text.Json.JsonSerializer.SerializeToElement(before);
        var afterElement = System.Text.Json.JsonSerializer.SerializeToElement(after);
        var emptyElement = System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, object?>());

        var change = new Change(
            Actions: ["create", "delete"],
            Before: beforeElement,
            After: afterElement,
            AfterUnknown: emptyElement,
            BeforeSensitive: emptyElement,
            AfterSensitive: emptyElement
        );

        var resourceChange = new ResourceChange(
            Address: "example_resource.replace_me",
            ModuleAddress: null,
            Mode: "managed",
            Type: "example_resource",
            Name: "replace_me",
            ProviderName: "registry.terraform.io/hashicorp/example",
            Change: change
        );

        var plan = new TerraformPlan(
            FormatVersion: "1.2",
            TerraformVersion: "1.14.0",
            ResourceChanges: new List<ResourceChange> { resourceChange }
        );

        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - replace should use the Before/After table
        var section = ResourceSection(markdown, "example_resource.replace_me");
        section.Should().Contain("| Attribute | Before | After |")
            .And.Contain($"| {Escape("name")} | `{Escape("old")}` | `{Escape("new")}` |")
            .And.Contain($"| {Escape("size")} | `{Escape("small")}` | `{Escape("large")}` |");
    }

    [Fact]
    public void Render_CreatePlan_MasksSensitiveAttributes()
    {
        // Arrange - create with a sensitive attribute in after
        var after = new Dictionary<string, object?>
        {
            ["name"] = "sensitive_resource",
            ["api_key"] = "secret-value"
        };

        var afterSensitive = new Dictionary<string, object?>
        {
            ["api_key"] = true
        };

        var afterElement = System.Text.Json.JsonSerializer.SerializeToElement(after);
        var afterSensitiveElement = System.Text.Json.JsonSerializer.SerializeToElement(afterSensitive);
        var emptyElement = System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, object?>());

        var change = new Change(
            Actions: ["create"],
            Before: null,
            After: afterElement,
            AfterUnknown: emptyElement,
            BeforeSensitive: emptyElement,
            AfterSensitive: afterSensitiveElement
        );

        var resourceChange = new ResourceChange(
            Address: "example_resource.sensitive",
            ModuleAddress: null,
            Mode: "managed",
            Type: "example_resource",
            Name: "sensitive",
            ProviderName: "registry.terraform.io/hashicorp/example",
            Change: change
        );

        var plan = new TerraformPlan(
            FormatVersion: "1.2",
            TerraformVersion: "1.14.0",
            ResourceChanges: new List<ResourceChange> { resourceChange }
        );

        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - sensitive attribute should be masked in the Value column
        var section = ResourceSection(markdown, "example_resource.sensitive");
        section.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("api_key")} | `{Escape("(sensitive)")}` |")
            .And.Contain($"| {Escape("name")} | `{Escape("sensitive_resource")}` |");
    }

    [Fact]
    public void Render_Create_OmitsNullAndUnknownAttributes()
    {
        // Arrange - create with a null attribute and an unknown attribute
        var after = new Dictionary<string, object?>
        {
            ["name"] = null,
            ["location"] = "westeurope"
        };

        var afterUnknown = new Dictionary<string, object?>
        {
            ["id"] = true
        };

        var afterElement = System.Text.Json.JsonSerializer.SerializeToElement(after);
        var afterUnknownElement = System.Text.Json.JsonSerializer.SerializeToElement(afterUnknown);
        var emptyElement = System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, object?>());

        var change = new Change(
            Actions: ["create"],
            Before: null,
            After: afterElement,
            AfterUnknown: afterUnknownElement,
            BeforeSensitive: emptyElement,
            AfterSensitive: emptyElement
        );

        var resourceChange = new ResourceChange(
            Address: "example_resource.partial",
            ModuleAddress: null,
            Mode: "managed",
            Type: "example_resource",
            Name: "partial",
            ProviderName: "registry.terraform.io/hashicorp/example",
            Change: change
        );

        var plan = new TerraformPlan(
            FormatVersion: "1.2",
            TerraformVersion: "1.14.0",
            ResourceChanges: new List<ResourceChange> { resourceChange }
        );

        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - the null `name` and unknown `id` should not be shown; only `location` should appear
        var section = ResourceSection(markdown, "example_resource.partial");
        section.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("location")} | `{Escape($"🌍{Nbsp}westeurope")}` |")
            .And.NotContain($"`{Escape("name")}`")
            .And.NotContain($"`{Escape("id")}`");
    }

    [Fact]
    public void Render_Delete_OmitsNullAttributes()
    {
        // Arrange - delete with a null attribute in before
        var before = new Dictionary<string, object?>
        {
            ["name"] = "rg-old-project",
            ["location"] = null
        };

        var beforeElement = System.Text.Json.JsonSerializer.SerializeToElement(before);
        var emptyElement = System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, object?>());

        var change = new Change(
            Actions: ["delete"],
            Before: beforeElement,
            After: null,
            AfterUnknown: emptyElement,
            BeforeSensitive: emptyElement,
            AfterSensitive: emptyElement
        );

        var resourceChange = new ResourceChange(
            Address: "example_resource.partial_delete",
            ModuleAddress: null,
            Mode: "managed",
            Type: "example_resource",
            Name: "partial_delete",
            ProviderName: "registry.terraform.io/hashicorp/example",
            Change: change
        );

        var plan = new TerraformPlan(
            FormatVersion: "1.2",
            TerraformVersion: "1.14.0",
            ResourceChanges: new List<ResourceChange> { resourceChange }
        );

        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - the null `location` should not be shown; only `name` should appear
        var section = ResourceSection(markdown, "example_resource.partial_delete");
        section.Should().Contain("| Attribute | Value |")
            .And.Contain($"| {Escape("name")} | `{Escape("rg-old-project")}` |")
            .And.NotContain($"`{Escape("location")}`");
    }

    [Fact]
    public void Render_WithInvalidTemplate_ThrowsMarkdownRenderException()
    {
        // Arrange
        var model = new ReportModel
        {
            TerraformVersion = "1.0.0",
            FormatVersion = "1.0",
            TfPlan2MdVersion = "1.0.0",
            CommitHash = "unknown",
            GeneratedAtUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture),
            HideMetadata = false,
            Changes = [],
            ModuleChanges = [],
            Summary = new SummaryModel
            {
                ToAdd = new ActionSummary(0, []),
                ToChange = new ActionSummary(0, []),
                ToDestroy = new ActionSummary(0, []),
                ToReplace = new ActionSummary(0, []),
                NoOp = new ActionSummary(0, []),
                Total = 0
            },
            ShowUnchangedValues = false,
            LargeValueFormat = LargeValueFormat.InlineDiff
        };
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{{ invalid template syntax }}{{");

        try
        {
            // Act
            var act = () => _renderer.Render(model, tempFile);

            // Assert
            act.Should().Throw<MarkdownRenderException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Render_AttributeChangesTable_DoesNotContainExtraNewlines()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - Check that attribute changes table rows are consecutive without blank lines
        // The fix produces (only changed attributes are shown by default):
        // | Attribute | Before | After |
        // |-----------|--------|-------|
        // | `sku_name` | standard | premium |
        // | `soft_delete_retention_days` | 7 | 90 |
        //
        // (no blank lines between rows)

        // Extract the attribute changes table section for azurerm_key_vault.main (which has multiple attributes)
        var keyVaultSection = ResourceSection(markdown, "azurerm_key_vault.main");

        // FIXED: The table should NOT have the pattern of "|\n\n|" which indicates blank lines between rows
        keyVaultSection.Should().NotContain("|\n\n|");

        // Verify the table exists and has the expected structure
        keyVaultSection.Should().Contain("| Attribute | Before | After |")
            .And.Contain($"| {Escape("sku_name")} | `standard` | `premium` |")
            .And.Contain("soft_delete_retention_days")
            .And.NotContain("| `location` |");
    }

    [Fact]
    public void Render_LargePlanWithManyNoOpResources_DoesNotExceedIterationLimit()
    {
        // Arrange - Create a large plan with many no-op resources to test that the template
        // does not exceed Scriban's default iteration limit of 1000
        // See: https://github.com/scriban/scriban/issues/226
        var resourceChanges = new List<ResourceChange>();

        // Create 200 no-op resources, each with 10 attributes
        // This would cause 200 * 10 = 2000 iterations in the nested loop without the fix
        for (var i = 0; i < 200; i++)
        {
            var attributes = new Dictionary<string, object?>();
            for (var j = 0; j < 10; j++)
            {
                attributes[$"attribute_{j}"] = $"value_{j}";
            }

            var attributesElement = System.Text.Json.JsonSerializer.SerializeToElement(attributes);
            var emptyElement = System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, object?>());

            var change = new Change(
                Actions: ["no-op"],
                Before: attributesElement,
                After: attributesElement,
                AfterUnknown: emptyElement,
                BeforeSensitive: emptyElement,
                AfterSensitive: emptyElement
            );

            resourceChanges.Add(new ResourceChange(
                Address: $"azurerm_resource_group.rg_{i}",
                ModuleAddress: null,
                Mode: "managed",
                Type: "azurerm_resource_group",
                Name: $"rg_{i}",
                ProviderName: "registry.terraform.io/hashicorp/azurerm",
                Change: change
            ));
        }

        var plan = new TerraformPlan(
            FormatVersion: "1.2",
            TerraformVersion: "1.14.0",
            ResourceChanges: resourceChanges
        );

        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act & Assert - Should NOT throw "Exceeding number of iteration limit '1000' for loop statement"
        var act = () => _renderer.Render(model);
        act.Should().NotThrow();
    }

    #region Resource-Specific Template Tests

    [Fact]
    public void Render_MultiModulePlan_GroupsModulesAndPreservesOrder()
    {
        // Arrange
        var json = File.ReadAllText("TestData/multi-module-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - module order should be: root, module.network, module.network.module.subnet, module.app, module.app.module.database
        var expectedOrder = new[] { "Module: root", "Module: `module.network`", "Module: `module.network.module.subnet`", "Module: `module.app`", "Module: `module.app.module.database`" };
        var observed = expectedOrder.Select(e => markdown.IndexOf(e, StringComparison.Ordinal)).ToList();
        // All module headers must be present in the document
        observed.All(i => i >= 0).Should().BeTrue();
        // And the indices should be in ascending order
        for (var i = 1; i < observed.Count; i++)
        {
            observed[i].Should().BeGreaterThan(observed[i - 1]);
        }

        // Also assert resources are within their modules (using type <b><code>name</code></b> format)
        markdown.Should().Contain("azurerm_resource_group <b><code>rg_root</code></b>")
            .And.Contain("azurerm_virtual_network <b><code>vnet</code></b>")
            .And.Contain("azurerm_subnet <b><code>subnet1</code></b>")
            .And.Contain("azurerm_app_service <b><code>app</code></b>")
            .And.Contain("azurerm_postgresql_server <b><code>db</code></b>");
    }

    [Fact]
    public void Render_MultiModulePlan_HeadingsAndHierarchyAreCorrect()
    {
        // Arrange
        var json = File.ReadAllText("TestData/multi-module-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);

        // Act
        var markdown = _renderer.Render(model);

        // Assert - module headers are H3 and resources live under their module
        var moduleHeaders = new[]
        {
            "### 📦 Module: root",
            "### 📦 Module: `module.network`",
            "### 📦 Module: `module.network.module.subnet`",
            "### 📦 Module: `module.app`",
            "### 📦 Module: `module.app.module.database`"
        };

        // Resource patterns to find (type <b><code>name</code></b>)
        var resourcePatterns = new[]
        {
            "azurerm_resource_group <b><code>rg_root</code></b>",
            "azurerm_virtual_network <b><code>vnet</code></b>",
            "azurerm_subnet <b><code>subnet1</code></b>",
            "azurerm_app_service <b><code>app</code></b>",
            "azurerm_postgresql_server <b><code>db</code></b>"
        };

        for (var i = 0; i < moduleHeaders.Length; i++)
        {
            var headerIndex = markdown.IndexOf(moduleHeaders[i], StringComparison.Ordinal);
            headerIndex.Should().BeGreaterThanOrEqualTo(0, $"Module header not found: {moduleHeaders[i]}");
            var nextHeaderIndex = i + 1 < moduleHeaders.Length ? markdown.IndexOf(moduleHeaders[i + 1], StringComparison.Ordinal) : int.MaxValue;
            var resourceIndex = markdown.IndexOf(resourcePatterns[i], StringComparison.Ordinal);
            resourceIndex.Should().BeGreaterThan(headerIndex, $"Resource {resourcePatterns[i]} should appear after its module header");
            resourceIndex.Should().BeLessThan(nextHeaderIndex, $"Resource {resourcePatterns[i]} should appear before the next module header");
        }
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ReturnsResourceSpecificMarkdown()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Type == "azurerm_firewall_network_rule_collection" && c.Action == "update");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(Escape("web_tier")).And.Contain("Rule Changes");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_SummaryUsesActionIcons()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert
        result.Should().Contain($"**Action:** `{Escape($"✅{Nbsp}Allow")}`");
    }


    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ShowsAddedRules()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - allow-dns was added
        result.Should().NotBeNull();
        result.Should().Contain("allow-dns").And.Contain("➕");
    }

    [Fact]
    public void RenderResourceChange_Nsg_UsesPlusIconForAddedRules()
    {
        // Arrange
        var json = File.ReadAllText("TestData/nsg-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var nsgChange = model.Changes.First(c => c.Address == "azurerm_network_security_group.app");

        // Act
        var result = _renderer.RenderResourceChange(nsgChange);

        // Assert
        result.Should().NotContain("➥").And.Contain("| ➕ |");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ShowsModifiedRules()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - allow-http was modified (source_addresses changed)
        result.Should().NotBeNull();
        result.Should().Contain("allow-http").And.Contain("🔄");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ShowsRemovedRules()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - allow-ssh-old was removed
        result.Should().NotBeNull();
        result.Should().Contain("allow-ssh-old").And.Contain("❌");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ShowsUnchangedRules()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - allow-https was unchanged
        result.Should().NotBeNull();
        result.Should().Contain("allow-https").And.Contain("⏺️");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_Add_ShowsPerRuleTableAndCombinedAddresses()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - added rule allow-dns is shown in the main rule table with combined Source/Destination
        result.Should().NotBeNull();
        var normalized = Normalize(result);
        normalized.Should().Contain("➕").And.Contain("allow-dns");
        // After Normalize, icons and formatting removed but structure retained
        normalized.Should().Contain("10.0.1.0/24").And.Contain("10.0.2.0/24");
        normalized.Should().Contain("168.63.129.16");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_Modified_ShowsBeforeAfterTableAndCombinedAddresses()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - modified rule allow-http is shown in the main rule table with combined Source values
        result.Should().NotBeNull();
        var normalized = Normalize(result);
        normalized.Should().Contain("🔄").And.Contain("allow-http");
        normalized.Should().Contain("10.0.3.0/24");
        normalized.Should().Contain("from web and API tiers");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_Removed_ShowsPerRuleTable()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - removed rule allow-ssh-old is shown in the main rule table with combined values
        result.Should().NotBeNull();
        result.Should().Contain("❌").And.Contain("allow-ssh-old");
        result.Should().Contain("10.0.0.0/8");
        result.Should().Contain("10.0.2.0/24");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_Create_ShowsAllRules()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.database_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("database-tier-rules").And.Contain("allow-sql").And.Contain("allow-mysql").And.Contain("1433").And.Contain("3306");
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_Delete_ShowsAllRulesBeingDeleted()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.legacy");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("legacy-rules").And.Contain("allow-ftp").And.Contain("being deleted");
    }

    [Fact]
    public void RenderResourceChange_NonFirewallResource_ReturnsNull()
    {
        // Arrange
        var json = File.ReadAllText("TestData/azurerm-azuredevops-plan.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var resourceGroup = model.Changes.First(c => c.Type == "azurerm_resource_group");

        // Act
        var result = _renderer.RenderResourceChange(resourceGroup);

        // Assert - No resource-specific template exists, should return null
        result.Should().BeNull();
    }

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ContainsRuleDetailsTable()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - Should contain table headers including Description
        result.Should().NotBeNull();
        result.Should().Contain("Rule Name").And.Contain("Description").And.Contain("Protocols").And.Contain("Source Addresses").And.Contain("Destination Addresses").And.Contain("Destination Ports");

        // Assert - Should contain actual description content
        result.Should().Contain("Allow HTTPS traffic");
    }


    #endregion
}
