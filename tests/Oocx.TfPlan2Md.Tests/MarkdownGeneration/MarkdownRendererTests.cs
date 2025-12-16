using AwesomeAssertions;
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
        markdown.Should().Contain("## Summary")
            .And.Contain("➕ Add")
            .And.Contain("🔄 Change")
            .And.Contain("❌ Destroy");
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
        markdown.Should().Contain("azurerm_resource_group.main")
            .And.Contain("azurerm_storage_account.main")
            .And.Contain("azurerm_key_vault.main")
            .And.Contain("azuredevops_project.main");
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

        // Assert
        markdown.Should().Contain("➕ azurerm_resource_group.main")
            .And.Contain("🔄 azurerm_key_vault.main")
            .And.Contain("❌ azurerm_virtual_network.old")
            .And.Contain("♻️ azuredevops_git_repository.main");
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
            .And.Contain("➕ Add | 0")
            .And.Contain("🔄 Change | 0")
            .And.Contain("❌ Destroy | 0")
            .And.Contain("**Total** | **0**");
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

        // Assert
        markdown.Should().Contain("null_resource.test")
            .And.Contain("➕ null_resource.test")
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
        markdown.Should().Contain("➕ azurerm_resource_group.main")
            .And.Contain("➕ azurerm_storage_account.main")
            .And.Contain("➕ Add | 2");
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
        markdown.Should().Contain("❌ azurerm_storage_account.old")
            .And.Contain("❌ azurerm_resource_group.old")
            .And.Contain("❌ Destroy | 2");
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
        keyVaultSection.Should().NotContain("|\n\n|");

        // Verify the table exists and has the expected structure
        keyVaultSection.Should().Contain("| Attribute | Before | After |")
            .And.Contain("| `location` |")
            .And.Contain("| `sku_name` |");
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
        result.Should().Contain("web_tier").And.Contain("Rule Changes");
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

    [Fact]
    public void RenderResourceChange_FirewallRuleCollection_ModifiedDetailsInCollapsible()
    {
        // Arrange
        var json = File.ReadAllText("TestData/firewall-rule-changes.json");
        var plan = _parser.Parse(json);
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var firewallChange = model.Changes.First(c => c.Address == "azurerm_firewall_network_rule_collection.web_tier");

        // Act
        var result = _renderer.RenderResourceChange(firewallChange);

        // Assert - Modified rule details should be in collapsible section
        result.Should().NotBeNull();
        result.Should().Contain("<details>").And.Contain("Modified Rule Details").And.Contain("</details>");
    }

    #endregion
}
