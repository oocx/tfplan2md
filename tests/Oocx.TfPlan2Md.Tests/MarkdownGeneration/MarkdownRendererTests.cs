using System;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    private static string Escape(string value) => ScribanHelpers.EscapeMarkdown(value);

    private static string Heading(string actionSymbol, string address) => $"#### {actionSymbol} {Escape(address)}";

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
        markdown.Should().Contain("Module: root")
            .And.Contain("Module:")
            .And.Contain(Escape("azurerm_resource_group.main"))
            .And.Contain(Escape("azurerm_storage_account.main"))
            .And.Contain(Escape("azurerm_key_vault.main"))
            .And.Contain(Escape("azuredevops_project.main"));
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
        markdown.Should().Contain($"➕ {Escape("azurerm_resource_group.main")}")
            .And.Contain($"🔄 {Escape("azurerm_key_vault.main")}")
            .And.Contain($"❌ {Escape("azurerm_virtual_network.old")}")
            .And.Contain($"♻️ {Escape("azuredevops_git_repository.main")}");
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
        markdown.Should().Contain(Escape("null_resource.test"))
            .And.Contain($"➕ {Escape("null_resource.test")}")
            // Should not contain the Attribute Changes details section since there are no changes
            .And.NotContain("<details>");
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
        markdown.Should().Contain($"➕ {Escape("azurerm_resource_group.main")}")
            .And.Contain($"➕ {Escape("azurerm_storage_account.main")}")
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
        var rgSection = markdown.Split(Heading("➕", "azurerm_resource_group.main"))[1].Split("###")[0];
        rgSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| `{Escape("name")}` | {Escape("rg-new-project")} |")
            .And.Contain($"| `{Escape("location")}` | {Escape("westeurope")} |");

        var stSection = markdown.Split(Heading("➕", "azurerm_storage_account.main"))[1].Split("###")[0];
        stSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| `{Escape("account_tier")}` | {Escape("Standard")} |");
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
        markdown.Should().Contain($"❌ {Escape("azurerm_storage_account.old")}")
            .And.Contain($"❌ {Escape("azurerm_resource_group.old")}")
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
        var stSection = markdown.Split(Heading("❌", "azurerm_storage_account.old"))[1].Split("###")[0];
        stSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| `{Escape("account_tier")}` | {Escape("Standard")} |")
            .And.Contain($"| `{Escape("name")}` | {Escape("stoldproject")} |");

        var rgSection = markdown.Split(Heading("❌", "azurerm_resource_group.old"))[1].Split("###")[0];
        rgSection.Should().Contain("| Attribute | Value |")
            .And.Contain($"| `{Escape("name")}` | {Escape("rg-old-project")} |")
            .And.Contain($"| `{Escape("location")}` | {Escape("westeurope")} |");
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
        var section = markdown.Split(Heading("♻️", "example_resource.replace_me"))[1].Split("###")[0];
        section.Should().Contain("| Attribute | Before | After |")
            .And.Contain($"| `{Escape("name")}` | {Escape("old")} | {Escape("new")} |")
            .And.Contain($"| `{Escape("size")}` | {Escape("small")} | {Escape("large")} |");
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
        var section = markdown.Split(Heading("➕", "example_resource.sensitive"))[1].Split("###")[0];
        section.Should().Contain("| Attribute | Value |")
            .And.Contain("| `api_key` | (sensitive) |")
            .And.Contain("| `name` | sensitive_resource |");
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
        var section = markdown.Split(Heading("➕", "example_resource.partial"))[1].Split("###")[0];
        section.Should().Contain("| Attribute | Value |")
            .And.Contain($"| `{Escape("location")}` | {Escape("westeurope")} |")
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
        var section = markdown.Split(Heading("❌", "example_resource.partial_delete"))[1].Split("###")[0];
        section.Should().Contain("| Attribute | Value |")
            .And.Contain($"| `{Escape("name")}` | {Escape("rg-old-project")} |")
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
        var keyVaultSection = markdown.Split(Heading("🔄", "azurerm_key_vault.main"))[1].Split("###")[0];

        // FIXED: The table should NOT have the pattern of "|\n\n|" which indicates blank lines between rows
        keyVaultSection.Should().NotContain("|\n\n|");

        // Verify the table exists and has the expected structure
        keyVaultSection.Should().Contain("| Attribute | Before | After |")
            .And.Contain($"| `{Escape("sku_name")}` |")
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

        // Also assert resources are within their modules
        markdown.Should().Contain(Escape("azurerm_resource_group.rg_root"))
            .And.Contain(Escape("azurerm_virtual_network.vnet"))
            .And.Contain(Escape("azurerm_subnet.subnet1"))
            .And.Contain(Escape("azurerm_app_service.app"))
            .And.Contain(Escape("azurerm_postgresql_server.db"));
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

        // Assert - module headers are H3 and resource headings are H4 and resources live under their module
        var moduleHeaders = new[]
        {
            "### Module: root",
            "### Module: `module.network`",
            "### Module: `module.network.module.subnet`",
            "### Module: `module.app`",
            "### Module: `module.app.module.database`"
        };

        var resourceHeadings = new[]
        {
            Heading("➕", "azurerm_resource_group.rg_root"),
            Heading("➕", "module.network.azurerm_virtual_network.vnet"),
            Heading("➕", "module.network.module.subnet.azurerm_subnet.subnet1"),
            Heading("🔄", "module.app.azurerm_app_service.app"),
            Heading("➕", "module.app.module.database.azurerm_postgresql_server.db")
        };

        for (var i = 0; i < moduleHeaders.Length; i++)
        {
            var headerIndex = markdown.IndexOf(moduleHeaders[i], StringComparison.Ordinal);
            headerIndex.Should().BeGreaterThanOrEqualTo(0, $"Module header not found: {moduleHeaders[i]}");
            var nextHeaderIndex = i + 1 < moduleHeaders.Length ? markdown.IndexOf(moduleHeaders[i + 1], StringComparison.Ordinal) : int.MaxValue;
            var resourceIndex = markdown.IndexOf(resourceHeadings[i], StringComparison.Ordinal);
            resourceIndex.Should().BeGreaterThan(headerIndex, $"Resource heading {resourceHeadings[i]} should appear after its module header");
            resourceIndex.Should().BeLessThan(nextHeaderIndex, $"Resource heading {resourceHeadings[i]} should appear before the next module header");
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
        result.Should().Contain("➕").And.Contain("allow-dns");
        result.Should().Contain("10.0.1.0/24, 10.0.2.0/24");
        result.Should().Contain("168.63.129.16");
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
        result.Should().Contain("🔄").And.Contain("allow-http");
        result.Should().Contain("10.0.1.0/24, 10.0.3.0/24");
        result.Should().Contain("Allow HTTP traffic from web and API tiers");
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
