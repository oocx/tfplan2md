using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

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
        Assert.Contains("## Summary", markdown);
        Assert.Contains("➕ Add", markdown);
        Assert.Contains("🔄 Change", markdown);
        Assert.Contains("❌ Destroy", markdown);
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

        // Assert
        Assert.Contains("azurerm_resource_group.main", markdown);
        Assert.Contains("azurerm_storage_account.main", markdown);
        Assert.Contains("azurerm_key_vault.main", markdown);
        Assert.Contains("azuredevops_project.main", markdown);
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

        // Assert
        Assert.Contains("1.14.0", markdown);
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

        // Assert
        Assert.Contains("➕ azurerm_resource_group.main", markdown);
        Assert.Contains("🔄 azurerm_key_vault.main", markdown);
        Assert.Contains("❌ azurerm_virtual_network.old", markdown);
        Assert.Contains("♻️ azuredevops_git_repository.main", markdown);
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
        Assert.Contains("## Summary", markdown);
        Assert.Contains("**Terraform Version:** 1.14.0", markdown);
        Assert.Contains("➕ Add | 0", markdown);
        Assert.Contains("🔄 Change | 0", markdown);
        Assert.Contains("❌ Destroy | 0", markdown);
        Assert.Contains("**Total** | **0**", markdown);
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
        Assert.Contains("## Summary", markdown);
        Assert.DoesNotContain("azurerm_resource_group.main", markdown); // no-op resources are filtered
        Assert.Contains("No changes", markdown); // Should show "No changes" when all resources are no-op
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
        Assert.Contains("## Resource Changes", markdown);
        Assert.Contains("No changes", markdown);
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

        // Assert
        Assert.Contains("null_resource.test", markdown);
        Assert.Contains("➕ null_resource.test", markdown);
        // Should not contain the Attribute Changes details section since there are no changes
        Assert.DoesNotContain("<details>", markdown);
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
        Assert.Contains("➕ azurerm_resource_group.main", markdown);
        Assert.Contains("➕ azurerm_storage_account.main", markdown);
        Assert.Contains("➕ Add | 2", markdown);
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
        Assert.Contains("❌ azurerm_storage_account.old", markdown);
        Assert.Contains("❌ azurerm_resource_group.old", markdown);
        Assert.Contains("❌ Destroy | 2", markdown);
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
            Summary = new SummaryModel()
        };
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "{{ invalid template syntax }}{{");

        try
        {
            // Act & Assert
            Assert.Throws<MarkdownRenderException>(() => _renderer.Render(model, tempFile));
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
        // The fix produces:
        // | Attribute | Before | After |
        // |-----------|--------|-------|
        // | `location` | westeurope | westeurope |
        // | `sku_name` | standard | premium |
        //
        // (no blank lines between rows)

        // Extract the attribute changes table section for azurerm_key_vault.main (which has multiple attributes)
        var keyVaultSection = markdown.Split("### 🔄 azurerm_key_vault.main")[1].Split("###")[0];

        // FIXED: The table should NOT have the pattern of "|\n\n|" which indicates blank lines between rows
        Assert.DoesNotContain("|\n\n|", keyVaultSection);

        // Verify the table exists and has the expected structure
        Assert.Contains("| Attribute | Before | After |", keyVaultSection);
        Assert.Contains("| `location` |", keyVaultSection);
        Assert.Contains("| `sku_name` |", keyVaultSection);
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
        var exception = Record.Exception(() => _renderer.Render(model));
        Assert.Null(exception);
    }
}
