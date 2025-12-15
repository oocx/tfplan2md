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

        // Assert
        Assert.Contains("## Summary", markdown);
        Assert.Contains("azurerm_resource_group.main", markdown);
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
}
