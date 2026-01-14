using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Diagnostics;

/// <summary>
/// Tests for the DiagnosticContext class.
/// Related feature: docs/features/038-debug-output/
/// </summary>
[Category("Unit")]
public class DiagnosticContextTests
{
    /// <summary>
    /// TC-03: Empty diagnostic context generates appropriate output.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_EmptyDiagnostics_ReturnsHeaderWithNoData()
    {
        // Arrange
        var context = new DiagnosticContext();

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("## Debug Information");
        await Assert.That(markdown).Contains("No diagnostics collected");
    }

    /// <summary>
    /// TC-04: Full diagnostic context generates correctly formatted markdown.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_WithAllDiagnostics_ReturnsFormattedMarkdown()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = "principals.json"
        };
        context.PrincipalTypeCount["users"] = 45;
        context.PrincipalTypeCount["groups"] = 12;
        context.PrincipalTypeCount["service principals"] = 8;
        context.FailedResolutions.Add(new FailedPrincipalResolution(
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example"));
        context.FailedResolutions.Add(new FailedPrincipalResolution(
            "87654321-4321-4321-4321-210987654321",
            "azurerm_role_assignment.reader"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_firewall_network_rule_collection",
            "Built-in resource-specific template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_virtual_network",
            "Default template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_custom_resource",
            "Custom template: /templates/azurerm/custom_resource.sbn"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("## Debug Information");
        await Assert.That(markdown).Contains("### Principal Mapping");
        await Assert.That(markdown).Contains("Loaded successfully from 'principals.json'");
        await Assert.That(markdown).Contains("45 users");
        await Assert.That(markdown).Contains("12 groups");
        await Assert.That(markdown).Contains("8 service principals");
        await Assert.That(markdown).Contains("Failed to resolve 2 principal IDs:");
        await Assert.That(markdown).Contains("`12345678-1234-1234-1234-123456789012`");
        await Assert.That(markdown).Contains("`azurerm_role_assignment.example`");
        await Assert.That(markdown).Contains("### Template Resolution");
        await Assert.That(markdown).Contains("`azurerm_firewall_network_rule_collection`: Built-in resource-specific template");
        await Assert.That(markdown).Contains("`azurerm_virtual_network`: Default template");
        await Assert.That(markdown).Contains("`azurerm_custom_resource`: Custom template");
    }

    /// <summary>
    /// TC-15: Failed principal resolutions are formatted correctly.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_FailedResolutions_FormatsCorrectly()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = "principals.json"
        };
        context.FailedResolutions.Add(new FailedPrincipalResolution(
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example"));
        context.FailedResolutions.Add(new FailedPrincipalResolution(
            "87654321-4321-4321-4321-210987654321",
            "azurerm_role_assignment.reader"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify formatting with backticks
        await Assert.That(markdown).Contains("- `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_role_assignment.example`)");
        await Assert.That(markdown).Contains("- `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`)");
    }

    /// <summary>
    /// TC-16: Template resolutions are formatted correctly.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_TemplateResolutions_FormatsCorrectly()
    {
        // Arrange
        var context = new DiagnosticContext();
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_firewall_network_rule_collection",
            "Built-in resource-specific template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_virtual_network",
            "Default template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_custom_resource",
            "Custom template: /templates/azurerm/custom_resource.sbn"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify list formatting with resource types in backticks
        await Assert.That(markdown).Contains("- `azurerm_firewall_network_rule_collection`: Built-in resource-specific template");
        await Assert.That(markdown).Contains("- `azurerm_virtual_network`: Default template");
        await Assert.That(markdown).Contains("- `azurerm_custom_resource`: Custom template: /templates/azurerm/custom_resource.sbn");
    }

    /// <summary>
    /// TC-20: No principal mapping file provided is handled gracefully.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_NoPrincipalMappingFile_OmitsPrincipalSection()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = false
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Principal Mapping section should not appear
        await Assert.That(markdown).DoesNotContain("### Principal Mapping");
        await Assert.That(markdown).Contains("## Debug Information");
    }

    /// <summary>
    /// Test that principal mapping load failure is reported correctly.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_PrincipalMappingLoadFailure_ShowsFailureMessage()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = "missing.json"
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("### Principal Mapping");
        await Assert.That(markdown).Contains("Failed to load from 'missing.json'");
    }

    /// <summary>
    /// Test that duplicate resource types in template resolutions are deduplicated.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_DuplicateResourceTypes_ShowsOnlyFirst()
    {
        // Arrange
        var context = new DiagnosticContext();
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_virtual_network",
            "Built-in template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_virtual_network",
            "Custom template"));  // Duplicate - should be ignored

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should only show first resolution for the resource type
        var lines = markdown.Split('\n').Where(l => l.Contains("azurerm_virtual_network")).ToList();
        lines.Should().HaveCount(1);
        await Assert.That(lines[0]).Contains("Built-in template");
    }

    /// <summary>
    /// Test that principal type counts are sorted alphabetically.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_PrincipalTypeCounts_AreSortedAlphabetically()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = "principals.json"
        };
        context.PrincipalTypeCount["users"] = 45;
        context.PrincipalTypeCount["groups"] = 12;
        context.PrincipalTypeCount["service principals"] = 8;

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should be in alphabetical order: groups, service principals, users
        var principalLine = markdown.Split('\n').First(l => l.Contains("Found"));
        var groupsIndex = principalLine.IndexOf("groups", StringComparison.Ordinal);
        var spIndex = principalLine.IndexOf("service principals", StringComparison.Ordinal);
        var usersIndex = principalLine.IndexOf("users", StringComparison.Ordinal);

        await Assert.That(groupsIndex).IsLessThan(spIndex);
        await Assert.That(spIndex).IsLessThan(usersIndex);
    }

    /// <summary>
    /// Test singular vs plural for failed principal count.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_SingleFailedResolution_UsesSingularForm()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = "principals.json"
        };
        context.FailedResolutions.Add(new FailedPrincipalResolution(
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should say "1 principal ID" not "1 principal IDs"
        await Assert.That(markdown).Contains("Failed to resolve 1 principal ID:");
        await Assert.That(markdown).DoesNotContain("1 principal IDs");
    }
}
