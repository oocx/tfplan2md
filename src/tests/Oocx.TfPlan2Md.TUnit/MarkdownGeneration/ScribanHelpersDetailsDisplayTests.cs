using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;
using Oocx.TfPlan2Md.Providers;
using Oocx.TfPlan2Md.Providers.AzureAD;
using Oocx.TfPlan2Md.Providers.AzureDevOps;
using Oocx.TfPlan2Md.Providers.AzureRM;
using Oocx.TfPlan2Md.RenderTargets;
using Scriban.Runtime;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for details display mode helpers used by templates.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public class ScribanHelpersDetailsDisplayTests
{
    [Test]
    public void GetDetailsOpenAttr_OpenMode_ReturnsOpen()
    {
        // Arrange
        var change = new ScriptObject();

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Open);

        // Assert
        result.Should().Be(" open");
    }

    [Test]
    public void GetDetailsOpenAttr_ClosedMode_ReturnsClosed()
    {
        // Arrange
        var change = new ScriptObject();

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Closed);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_AutoMode_NoFindings_ReturnsClosed()
    {
        // Arrange
        var change = new ScriptObject();

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_AutoMode_WithFindings_ReturnsOpen()
    {
        // Arrange
        var change = new ScriptObject();
        var findings = new ScriptArray { new ScriptObject { ["severity"] = "critical" } };
        change["code_analysis_findings"] = findings;

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(" open");
    }

    [Test]
    public void GetDetailsOpenAttr_AutoMode_EmptyFindings_ReturnsClosed()
    {
        // Arrange
        var change = new ScriptObject();
        var findings = new ScriptArray();
        change["code_analysis_findings"] = findings;

        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(change, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_NullChange_ClosedMode_ReturnsClosed()
    {
        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(null, DetailsDisplayMode.Closed);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_NullChange_AutoMode_ReturnsClosed()
    {
        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(null, DetailsDisplayMode.Auto);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Test]
    public void GetDetailsOpenAttr_NullChange_OpenMode_ReturnsOpen()
    {
        // Act
        var result = ScribanHelpers.GetDetailsOpenAttr(null, DetailsDisplayMode.Open);

        // Assert
        result.Should().Be(" open");
    }

    /// <summary>
    /// Integration tests verifying that provider-specific templates honour
    /// <c>--details open</c>. These catch regressions where a template's outer
    /// <c>&lt;details&gt;</c> tag still uses the old hardcoded inline logic
    /// instead of the <c>details_open_attr(change)</c> helper.
    /// </summary>
    [Test]
    [Arguments("azuread-user-plan.json", "azuread_user")]
    [Arguments("azuread-group-plan.json", "azuread_group")]
    [Arguments("azuread-group-without-members-plan.json", "azuread_group_without_members")]
    [Arguments("azuread-invitation-plan.json", "azuread_invitation")]
    [Arguments("azuread-service-principal-plan.json", "azuread_service_principal")]
    [Arguments("azuread-group-member-plan.json", "azuread_group_member")]
    public void ProviderTemplate_DetailsOpen_AzureAD_RendersOpenAttribute(string planFile, string resourceType)
    {
        // Arrange
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        var parser = new TerraformPlanParser();
        var json = File.ReadAllText(Path.Combine("TestData", planFile));
        var plan = parser.Parse(json);
        var model = new ReportModelBuilder(
            providerRegistry: providerRegistry,
            detailsDisplayMode: DetailsDisplayMode.Open).Build(plan);

        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        // Act
        var markdown = renderer.Render(model);

        // Assert – outer resource <details> must carry the open attribute
        markdown.Should().Contain("<details open",
            $"template for {resourceType} must use details_open_attr(change) so --details open is respected");
    }

    [Test]
    [Arguments("azuread-user-plan.json", "azuread_user")]
    [Arguments("azuread-group-plan.json", "azuread_group")]
    [Arguments("azuread-group-without-members-plan.json", "azuread_group_without_members")]
    [Arguments("azuread-invitation-plan.json", "azuread_invitation")]
    [Arguments("azuread-service-principal-plan.json", "azuread_service_principal")]
    [Arguments("azuread-group-member-plan.json", "azuread_group_member")]
    public void ProviderTemplate_DetailsClosed_AzureAD_RendersNoOpenAttribute(string planFile, string resourceType)
    {
        // Arrange
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureADModule());

        var parser = new TerraformPlanParser();
        var json = File.ReadAllText(Path.Combine("TestData", planFile));
        var plan = parser.Parse(json);
        var model = new ReportModelBuilder(
            providerRegistry: providerRegistry,
            detailsDisplayMode: DetailsDisplayMode.Closed).Build(plan);

        var renderer = new MarkdownRenderer(providerRegistry: providerRegistry);

        // Act
        var markdown = renderer.Render(model);

        // Assert – no outer resource <details> should carry the open attribute
        markdown.Should().NotContain("<details open",
            $"template for {resourceType} must use details_open_attr(change) so --details closed is respected");
    }

    [Test]
    public void ProviderTemplate_DetailsOpen_AzureDevOps_VariableGroup_RendersOpenAttribute()
    {
        // Arrange
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureDevOpsModule(largeValueFormat: LargeValueFormat.InlineDiff));

        var parser = new TerraformPlanParser();
        var json = File.ReadAllText(Path.Combine("TestData", "azuredevops-variable-groups.json"));
        var plan = parser.Parse(json);
        var model = new ReportModelBuilder(
            providerRegistry: providerRegistry,
            detailsDisplayMode: DetailsDisplayMode.Open).Build(plan);

        var renderer = new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        // Act
        var markdown = renderer.Render(model);

        // Assert
        markdown.Should().Contain("<details open",
            "template for azuredevops_variable_group must use details_open_attr(change) so --details open is respected");
    }

    [Test]
    public void ProviderTemplate_DetailsClosed_AzureDevOps_VariableGroup_RendersNoOpenAttribute()
    {
        // Arrange
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureDevOpsModule(largeValueFormat: LargeValueFormat.InlineDiff));

        var parser = new TerraformPlanParser();
        var json = File.ReadAllText(Path.Combine("TestData", "azuredevops-variable-groups.json"));
        var plan = parser.Parse(json);
        var model = new ReportModelBuilder(
            providerRegistry: providerRegistry,
            detailsDisplayMode: DetailsDisplayMode.Closed).Build(plan);

        var renderer = new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        // Act
        var markdown = renderer.Render(model);

        // Assert
        markdown.Should().NotContain("<details open",
            "template for azuredevops_variable_group must use details_open_attr(change) so --details closed is respected");
    }

    [Test]
    [Arguments("firewall-rule-changes.json", "azurerm_firewall_network_rule_collection")]
    [Arguments("nsg-rule-changes.json", "azurerm_network_security_group")]
    [Arguments("role-assignments.json", "azurerm_role_assignment")]
    public void ProviderTemplate_DetailsOpen_AzureRM_RendersOpenAttribute(string planFile, string resourceType)
    {
        // Arrange
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));

        var parser = new TerraformPlanParser();
        var json = File.ReadAllText(Path.Combine("TestData", planFile));
        var plan = parser.Parse(json);
        var model = new ReportModelBuilder(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry,
            detailsDisplayMode: DetailsDisplayMode.Open).Build(plan);

        var renderer = new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        // Act
        var markdown = renderer.Render(model);

        // Assert
        markdown.Should().Contain("<details open",
            $"template for {resourceType} must use details_open_attr(change) so --details open is respected");
    }

    [Test]
    [Arguments("firewall-rule-changes.json", "azurerm_firewall_network_rule_collection")]
    [Arguments("nsg-rule-changes.json", "azurerm_network_security_group")]
    [Arguments("role-assignments.json", "azurerm_role_assignment")]
    public void ProviderTemplate_DetailsClosed_AzureRM_RendersNoOpenAttribute(string planFile, string resourceType)
    {
        // Arrange
        var providerRegistry = new ProviderRegistry();
        providerRegistry.RegisterProvider(new AzureRMModule(
            largeValueFormat: LargeValueFormat.InlineDiff,
            principalMapper: new NullPrincipalMapper()));

        var parser = new TerraformPlanParser();
        var json = File.ReadAllText(Path.Combine("TestData", planFile));
        var plan = parser.Parse(json);
        var model = new ReportModelBuilder(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry,
            detailsDisplayMode: DetailsDisplayMode.Closed).Build(plan);

        var renderer = new MarkdownRenderer(
            principalMapper: new NullPrincipalMapper(),
            providerRegistry: providerRegistry);

        // Act
        var markdown = renderer.Render(model);

        // Assert
        markdown.Should().NotContain("<details open",
            $"template for {resourceType} must use details_open_attr(change) so --details closed is respected");
    }
}
