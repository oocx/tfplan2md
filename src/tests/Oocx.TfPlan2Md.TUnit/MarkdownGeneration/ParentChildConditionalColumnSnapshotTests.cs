using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Integration snapshot tests for conditional Terraform Resource column visibility.
/// </summary>
/// <remarks>
/// Related feature: docs/features/068-parent-child-resource-grouping/specification.md.
/// 
/// These tests verify that the Terraform Resource column appears/disappears correctly
/// in the rendered markdown based on whether external resources are present.
/// 
/// The column should only appear when at least one child resource is external (separate),
/// not when all children are inline (defined within the parent resource).
/// </remarks>
public class ParentChildConditionalColumnSnapshotTests
{
    /// <summary>
    /// Tests that VNet with only inline subnets hides the Terraform Resource column.
    /// </summary>
    /// <remarks>
    /// When all subnets are defined inline (within the azurerm_virtual_network resource),
    /// the TerraformResource values will all contain "attribute", so HasExternalResources
    /// should be false and the column should not appear in the table header.
    /// </remarks>
    [Test]
    public void VNetWithOnlyInlineSubnets_HidesResourceColumn()
    {
        // Arrange & Act
        var markdown = RenderPlan("TestData/azurerm-vnet-inline-subnets-plan.json");

        // Assert - Column should NOT appear in any table header
        markdown.Should().NotContain("| Terraform Resource |",
            "Terraform Resource column should be hidden when all subnets are inline");

        // Verify that subnet content is present (sanity check)
        markdown.Should().Contain("Subnets", "Should contain subnet child table");
    }

    /// <summary>
    /// Tests that VNet with only separate subnet resources shows the Terraform Resource column.
    /// </summary>
    /// <remarks>
    /// When all subnets are separate azurerm_subnet resources, the TerraformResource values
    /// will be resource addresses (e.g., "azurerm_subnet.example") that don't contain "attribute",
    /// so HasExternalResources should be true and the column should appear.
    /// </remarks>
    [Test]
    public void VNetWithOnlySeparateSubnets_ShowsResourceColumn()
    {
        // Arrange & Act
        var markdown = RenderPlan("TestData/azurerm-vnet-separate-subnets-plan.json");

        // Assert - Column SHOULD appear in the table header
        markdown.Should().Contain("| Terraform Resource |",
            "Terraform Resource column should be visible when subnets are separate resources");

        // Verify that actual resource addresses appear in the table
        markdown.Should().Contain("azurerm_subnet.",
            "Should contain actual subnet resource addresses");
    }

    /// <summary>
    /// Tests that VNet with mixed inline and separate subnets shows the Terraform Resource column.
    /// </summary>
    /// <remarks>
    /// When there's a mix of inline subnets (containing "attribute") and separate subnets
    /// (resource addresses), HasExternalResources should be true because at least one external
    /// resource exists. The column should be shown to display the external resource addresses.
    /// </remarks>
    [Test]
    public void VNetWithMixedSubnets_ShowsResourceColumn()
    {
        // Arrange & Act
        var markdown = RenderPlan("TestData/azurerm-vnet-mixed-subnets-plan.json");

        // Assert - Column SHOULD appear because there's at least one external resource
        markdown.Should().Contain("| Terraform Resource |",
            "Terraform Resource column should be visible when there are mixed subnet sources");

        // Verify that both inline and separate resources are present
        markdown.Should().Contain("subnet attribute",
            "Should contain inline subnet indicators");
        markdown.Should().Contain("azurerm_subnet.",
            "Should contain separate subnet resource addresses");
    }

    /// <summary>
    /// Tests that NSG with only inline rules hides the Terraform Resource column.
    /// </summary>
    /// <remarks>
    /// Similar to VNets with inline subnets, NSGs with inline security rules should not
    /// show the column because all TerraformResource values contain "attribute".
    /// </remarks>
    [Test]
    public void NsgWithOnlyInlineRules_HidesResourceColumn()
    {
        // Arrange & Act
        var markdown = RenderPlan("TestData/azurerm-nsg-inline-rules-plan.json");

        // Assert - Column should NOT appear
        markdown.Should().NotContain("| Terraform Resource |",
            "Terraform Resource column should be hidden when all security rules are inline");

        // Verify that security rule content is present (sanity check)
        markdown.Should().Contain("Security Rules", "Should contain security rules child table");
    }

    /// <summary>
    /// Tests that Route Table with only inline routes hides the Terraform Resource column.
    /// </summary>
    /// <remarks>
    /// Route tables with inline routes should not show the column because all
    /// TerraformResource values contain "attribute".
    /// </remarks>
    [Test]
    public void RouteTableWithOnlyInlineRoutes_HidesResourceColumn()
    {
        // Arrange & Act
        var markdown = RenderPlan("TestData/azurerm-route-table-inline-routes-plan.json");

        // Assert - Column should NOT appear
        markdown.Should().NotContain("| Terraform Resource |",
            "Terraform Resource column should be hidden when all routes are inline");

        // Verify that route content is present (sanity check)
        markdown.Should().Contain("Routes", "Should contain routes child table");
    }

    /// <summary>
    /// Tests that the UAT plan with mixed sources shows the Terraform Resource column.
    /// </summary>
    /// <remarks>
    /// The UAT plan contains Azure AD groups with both inline and separate members,
    /// so the column should be visible.
    /// </remarks>
    [Test]
    public void ParentChildUatPlan_WithMixedSources_ShowsResourceColumn()
    {
        // Arrange & Act
        var markdown = RenderPlan("TestData/parent-child-resource-grouping-uat-plan.json");

        // Assert - Column SHOULD appear because there are separate members
        markdown.Should().Contain("| Terraform Resource |",
            "Terraform Resource column should be visible for mixed member sources");

        // Verify both inline and separate members are present
        markdown.Should().Contain("members attribute",
            "Should contain inline member indicators");
        markdown.Should().Contain("azuread_group_member.",
            "Should contain separate member resource addresses");
    }

    /// <summary>
    /// Renders a Terraform plan from a test data file and returns the markdown output.
    /// </summary>
    /// <param name="testDataPath">The path to the test plan JSON file.</param>
    /// <returns>The rendered markdown output.</returns>
    private static string RenderPlan(string testDataPath)
    {
        var json = File.ReadAllText(testDataPath);
        var plan = new TerraformPlanParser().Parse(json);

        var providerRegistry = new ProviderRegistry();
        // Register AzureRM provider for VNet/NSG/Route Table tests
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.SimpleDiff,
            principalMapper: new NullPrincipalMapper()));
        // Register Azure AD provider for parent-child UAT test
        providerRegistry.RegisterProvider(new AzureADModule());

        var valueFormatterRegistry = new ValueFormatterRegistry();
        providerRegistry.RegisterAllValueFormatters(valueFormatterRegistry);

        var iconProviderRegistry = new IconProviderRegistry();
        providerRegistry.RegisterAllIconProviders(iconProviderRegistry);

        var model = new ReportModelBuilder(
            metadataProvider: TestMetadataProvider.Instance,
            providerRegistry: providerRegistry,
            codeAnalysisInput: null,
            iconProviderRegistry: iconProviderRegistry).Build(plan);

        var renderer = new MarkdownRenderer(
            providerRegistry: providerRegistry,
            valueFormatterRegistry: valueFormatterRegistry,
            iconProviderRegistry: iconProviderRegistry);

        return renderer.Render(model);
    }
}
