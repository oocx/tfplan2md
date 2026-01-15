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

    /// <summary>
    /// Test that FileNotFound error shows detailed diagnostics with Docker guidance.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_FileNotFoundError_ShowsDetailedDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = "/app/principals.json",
            PrincipalMappingErrorType = PrincipalMappingErrorType.FileNotFound,
            PrincipalMappingErrorMessage = "File not found: /app/principals.json",
            PrincipalMappingParentDirectoryExists = false
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("**Error Type:** FileNotFound");
        await Assert.That(markdown).Contains("**Error Message:** File not found: /app/principals.json");
        await Assert.That(markdown).Contains("**File Existence Check:**");
        await Assert.That(markdown).Contains("- File exists: No");
        await Assert.That(markdown).Contains("- Parent directory exists: No");
        await Assert.That(markdown).Contains("**Troubleshooting:**");
        await Assert.That(markdown).Contains("The parent directory does not exist");
        await Assert.That(markdown).Contains("If running in Docker: The volume mount may be missing or incorrect");
        await Assert.That(markdown).Contains("docker run -v /host/path");
    }

    /// <summary>
    /// Test that ParseError shows file size and JSON troubleshooting guidance.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_ParseError_ShowsFileSizeAndGuidance()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = "principals.json",
            PrincipalMappingErrorType = PrincipalMappingErrorType.ParseError,
            PrincipalMappingErrorMessage = "JSON parse error at line 12, column 5: Unexpected character '}'",
            PrincipalMappingFileSize = 2048
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("**Error Type:** ParseError");
        await Assert.That(markdown).Contains("**File Size:** 2048 bytes");
        await Assert.That(markdown).Contains("**Troubleshooting:**");
        await Assert.That(markdown).Contains("The file contains invalid JSON");
        await Assert.That(markdown).Contains("Missing or extra commas");
        await Assert.That(markdown).Contains("Check the error message above for line/column information");
    }

    /// <summary>
    /// Test that ParseError with empty file shows special guidance.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_ParseErrorEmptyFile_ShowsEmptyFileWarning()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = "principals.json",
            PrincipalMappingErrorType = PrincipalMappingErrorType.ParseError,
            PrincipalMappingErrorMessage = "JSON parse error: Unexpected end of input",
            PrincipalMappingFileSize = 0
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("**File Size:** 0 bytes");
        await Assert.That(markdown).Contains("**Note:** The file is empty. Ensure it contains valid JSON.");
    }

    /// <summary>
    /// Test that ReadError shows permissions troubleshooting guidance.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_ReadError_ShowsPermissionsGuidance()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = "principals.json",
            PrincipalMappingErrorType = PrincipalMappingErrorType.ReadError,
            PrincipalMappingErrorMessage = "Access denied: principals.json"
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("**Error Type:** ReadError");
        await Assert.That(markdown).Contains("**Troubleshooting:**");
        await Assert.That(markdown).Contains("File permissions (the process must have read access)");
        await Assert.That(markdown).Contains("The file is not locked by another process");
    }

    /// <summary>
    /// Test that template loading error shows appropriate diagnostics.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_TemplateFileNotFound_ShowsDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            TemplateErrorType = TemplateErrorType.FileNotFound,
            TemplateErrorMessage = "Template 'custom.sbn' not found",
            TemplateFileExists = false
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("### Template Loading Error");
        await Assert.That(markdown).Contains("**Error Type:** FileNotFound");
        await Assert.That(markdown).Contains("**Error Message:** Template 'custom.sbn' not found");
        await Assert.That(markdown).Contains("**File Exists:** No");
        await Assert.That(markdown).Contains("**Troubleshooting:**");
        await Assert.That(markdown).Contains("The template file was not found");
        await Assert.That(markdown).Contains("If running in Docker: The template file is mounted via volume");
    }

    /// <summary>
    /// Test that template parse error shows Scriban guidance.
    /// </summary>
    [Test]
    public async Task GenerateMarkdownSection_TemplateParseError_ShowsScribanGuidance()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            TemplateErrorType = TemplateErrorType.ParseError,
            TemplateErrorMessage = "Scriban syntax error: Unexpected token at line 42"
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert
        await Assert.That(markdown).Contains("**Error Type:** ParseError");
        await Assert.That(markdown).Contains("The template contains invalid Scriban syntax");
        await Assert.That(markdown).Contains("Check the Scriban documentation: https://github.com/scriban/scriban");
    }
}
