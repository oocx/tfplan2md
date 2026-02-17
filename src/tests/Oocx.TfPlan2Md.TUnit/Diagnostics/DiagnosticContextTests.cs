using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Diagnostics;

/// <summary>
/// Tests for the DiagnosticContext class.
/// Related feature: docs/features/038-debug-output/.
/// </summary>
[Category("Unit")]
public class DiagnosticContextTests
{
    private const string PrincipalMappingHeader = "### Principal Mapping";
    private const string PrincipalsFileName = "principals.json";
    private const string PrincipalsFilePath = "/data/principals.json";
    private const string VirtualNetworkResourceType = "azurerm_virtual_network";

    /// <summary>
    /// TC-03: Empty diagnostic context generates appropriate output.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_EmptyDiagnostics_ReturnsHeaderWithNoData()
    {
        // Arrange
        var context = new DiagnosticContext();

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify collapsible details block structure
        await Assert.That(markdown).Contains("<details>");
        await Assert.That(markdown).Contains("<summary>🐛\u00A0Debug Information</summary>");
        await Assert.That(markdown).Contains("<br>");
        await Assert.That(markdown).Contains("No diagnostics collected");
        await Assert.That(markdown).Contains("</details>");
        await Assert.That(markdown).DoesNotContain("<details open>");
    }

    /// <summary>
    /// TC-04: Full diagnostic context generates correctly formatted markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_WithAllDiagnostics_ReturnsFormattedMarkdown()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName
        };
        context.PrincipalTypeCount["users"] = 45;
        context.PrincipalTypeCount["groups"] = 12;
        context.PrincipalTypeCount["service principals"] = 8;
        context.FailedResolutions.Add(new FailedResolution(
            FailedResolutionType.Principal,
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example",
            "not found in mapping file"));
        context.FailedResolutions.Add(new FailedResolution(
            FailedResolutionType.RoleDefinition,
            "87654321-4321-4321-4321-210987654321",
            "azurerm_role_assignment.reader",
            "not found in mapping file or built-in roles"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_firewall_network_rule_collection",
            "Built-in resource-specific template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            VirtualNetworkResourceType,
            "Default template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_custom_resource",
            "Custom template: /templates/azurerm/custom_resource.sbn"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify collapsible details block with all content preserved
        await Assert.That(markdown).Contains("<details>");
        await Assert.That(markdown).Contains("<summary>🐛\u00A0Debug Information</summary>");
        await Assert.That(markdown).Contains("</details>");
        await Assert.That(markdown).Contains(PrincipalMappingHeader);
        await Assert.That(markdown).Contains($"Loaded successfully from '{PrincipalsFileName}'");
        await Assert.That(markdown).Contains("45 users");
        await Assert.That(markdown).Contains("12 groups");
        await Assert.That(markdown).Contains("8 service principals");
        await Assert.That(markdown).Contains("Failed to resolve 2 mappings:");
        await Assert.That(markdown).Contains("`12345678-1234-1234-1234-123456789012`");
        await Assert.That(markdown).Contains("`azurerm_role_assignment.example`");
        await Assert.That(markdown).Contains("### Template Resolution");
        await Assert.That(markdown).Contains("`azurerm_firewall_network_rule_collection`: Built-in resource-specific template");
        await Assert.That(markdown).Contains($"`{VirtualNetworkResourceType}`: Default template");
        await Assert.That(markdown).Contains("`azurerm_custom_resource`: Custom template");
    }

    /// <summary>
    /// TC-15: Failed principal resolutions are formatted correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_FailedResolutions_FormatsCorrectly()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName
        };
        context.FailedResolutions.Add(new FailedResolution(
            FailedResolutionType.Principal,
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example",
            "not found in mapping file"));
        context.FailedResolutions.Add(new FailedResolution(
            FailedResolutionType.RoleDefinition,
            "87654321-4321-4321-4321-210987654321",
            "azurerm_role_assignment.reader",
            "not found in mapping file or built-in roles"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify formatting with backticks
        await Assert.That(markdown).Contains("- Principal `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_role_assignment.example`)");
        await Assert.That(markdown).Contains("- Role definition `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`)");
    }

    /// <summary>
    /// TC-16: Template resolutions are formatted correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_TemplateResolutions_FormatsCorrectly()
    {
        // Arrange
        var context = new DiagnosticContext();
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_firewall_network_rule_collection",
            "Built-in resource-specific template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            VirtualNetworkResourceType,
            "Default template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            "azurerm_custom_resource",
            "Custom template: /templates/azurerm/custom_resource.sbn"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify list formatting with resource types in backticks
        await Assert.That(markdown).Contains("- `azurerm_firewall_network_rule_collection`: Built-in resource-specific template");
        await Assert.That(markdown).Contains($"- `{VirtualNetworkResourceType}`: Default template");
        await Assert.That(markdown).Contains("- `azurerm_custom_resource`: Custom template: /templates/azurerm/custom_resource.sbn");
    }

    /// <summary>
    /// TC-20: No principal mapping file provided is handled gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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

        // Assert - Principal Mapping section should not appear, but details block should be present
        await Assert.That(markdown).DoesNotContain(PrincipalMappingHeader);
        await Assert.That(markdown).Contains("<details>");
        await Assert.That(markdown).Contains("<summary>🐛\u00A0Debug Information</summary>");
        await Assert.That(markdown).Contains("</details>");
    }

    /// <summary>
    /// Test that principal mapping load failure is reported correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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
        await Assert.That(markdown).Contains(PrincipalMappingHeader);
        await Assert.That(markdown).Contains("Failed to load from 'missing.json'");
    }

    /// <summary>
    /// Test that duplicate resource types in template resolutions are deduplicated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_DuplicateResourceTypes_ShowsOnlyFirst()
    {
        // Arrange
        var context = new DiagnosticContext();
        context.TemplateResolutions.Add(new TemplateResolution(
            VirtualNetworkResourceType,
            "Built-in template"));
        context.TemplateResolutions.Add(new TemplateResolution(
            VirtualNetworkResourceType,
            "Custom template"));  // Duplicate - should be ignored

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should only show first resolution for the resource type
        var lines = markdown.Split('\n').Where(l => l.Contains(VirtualNetworkResourceType)).ToList();
        lines.Should().HaveCount(1);
        await Assert.That(lines[0]).Contains("Built-in template");
    }

    /// <summary>
    /// Test that principal type counts are sorted alphabetically.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_PrincipalTypeCounts_AreSortedAlphabetically()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName
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
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_SingleFailedResolution_UsesSingularForm()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName
        };
        context.FailedResolutions.Add(new FailedResolution(
            FailedResolutionType.Principal,
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example",
            "not found in mapping file"));

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should say "1 mapping" not "1 mappings"
        await Assert.That(markdown).Contains("Failed to resolve 1 mapping:");
        await Assert.That(markdown).DoesNotContain("1 mappings");
    }

    /// <summary>
    /// Test that enhanced file system diagnostics are included when file doesn't exist.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_PrincipalMappingFileNotFound_ShowsDetailedDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = PrincipalsFilePath,
            PrincipalMappingFileExists = false,
            PrincipalMappingDirectoryExists = true,
            PrincipalMappingErrorType = PrincipalLoadError.FileNotFound,
            PrincipalMappingErrorMessage = "File not found",
            PrincipalMappingErrorDetails = $"Could not find file '{PrincipalsFilePath}'"
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should show detailed diagnostics
        await Assert.That(markdown).Contains(PrincipalMappingHeader);
        await Assert.That(markdown).Contains($"Failed to load from '{PrincipalsFilePath}'");
        await Assert.That(markdown).Contains("**Diagnostic Details:**");
        await Assert.That(markdown).Contains("File exists: ❌");
        await Assert.That(markdown).Contains("Directory exists: ✅");
        await Assert.That(markdown).Contains("Error type: FileNotFound");
        await Assert.That(markdown).Contains("File not found");
    }

    /// <summary>
    /// Test that JSON parse errors show line and column information.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_JsonParseError_ShowsLineAndColumn()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = PrincipalsFilePath,
            PrincipalMappingFileExists = true,
            PrincipalMappingDirectoryExists = true,
            PrincipalMappingErrorType = PrincipalLoadError.JsonParseError,
            PrincipalMappingErrorMessage = "Invalid JSON syntax",
            PrincipalMappingErrorDetails = "Unexpected character 'i' at line 3, column 15"
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should show parse error details
        await Assert.That(markdown).Contains("Error type: JsonParseError");
        await Assert.That(markdown).Contains("Invalid JSON syntax");
        await Assert.That(markdown).Contains("line 3, column 15");
    }

    /// <summary>
    /// Test that directory not found shows parent directory status.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_DirectoryNotFound_ShowsDirectoryDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = "/data/subdir/principals.json",
            PrincipalMappingFileExists = false,
            PrincipalMappingDirectoryExists = false,
            PrincipalMappingErrorType = PrincipalLoadError.DirectoryNotFound,
            PrincipalMappingErrorMessage = "Directory not found",
            PrincipalMappingErrorDetails = "Could not find directory '/data/subdir'"
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should show directory not found
        await Assert.That(markdown).Contains("Directory exists: ❌");
        await Assert.That(markdown).Contains("Error type: DirectoryNotFound");
        await Assert.That(markdown).Contains("Directory not found");
    }

    /// <summary>
    /// Test that Docker volume mount guidance is included in error output.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_FileNotFound_IncludesDockerGuidance()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = false,
            PrincipalMappingFilePath = PrincipalsFilePath,
            PrincipalMappingFileExists = false,
            PrincipalMappingDirectoryExists = true,
            PrincipalMappingErrorType = PrincipalLoadError.FileNotFound
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Should include Docker mount guidance
        await Assert.That(markdown).Contains("**Common Solutions:**");
        await Assert.That(markdown).Contains("docker run");
        await Assert.That(markdown).Contains("-v");
        await Assert.That(markdown).Contains("--principal-mapping");
    }

    /// <summary>
    /// TC-18: Azure DevOps entity counts are included in diagnostic output.
    /// Related feature: docs/features/085-azdo-principal-mapping/test-cases.md.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_WithAzdoEntities_IncludesCounts()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName,
            AzdoUserCount = 2,
            AzdoGroupCount = 3,
            AzdoProjectCount = 1,
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify azdo counts are included
        await Assert.That(markdown).Contains(PrincipalMappingHeader);
        await Assert.That(markdown).Contains("2 azdo users");
        await Assert.That(markdown).Contains("3 azdo groups");
        await Assert.That(markdown).Contains("1 azdo project");
    }

    /// <summary>
    /// TC-18: Azure DevOps entity counts use singular form correctly.
    /// Related feature: docs/features/085-azdo-principal-mapping/test-cases.md.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_WithSingleAzdoEntity_UsesSingularForm()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName,
            AzdoUserCount = 1,
            AzdoGroupCount = 1,
            AzdoProjectCount = 1,
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify singular forms are used
        await Assert.That(markdown).Contains("1 azdo user");
        await Assert.That(markdown).Contains("1 azdo group");
        await Assert.That(markdown).Contains("1 azdo project");
        await Assert.That(markdown).DoesNotContain("1 azdo users");
        await Assert.That(markdown).DoesNotContain("1 azdo groups");
        await Assert.That(markdown).DoesNotContain("1 azdo projects");
    }

    /// <summary>
    /// TC-18: Azure DevOps entity counts are omitted when all are zero.
    /// Related feature: docs/features/085-azdo-principal-mapping/test-cases.md.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GenerateMarkdownSection_WithZeroAzdoEntities_OmitsAzdoCounts()
    {
        // Arrange
        var context = new DiagnosticContext
        {
            PrincipalMappingFileProvided = true,
            PrincipalMappingLoadedSuccessfully = true,
            PrincipalMappingFilePath = PrincipalsFileName,
            AzdoUserCount = 0,
            AzdoGroupCount = 0,
            AzdoProjectCount = 0,
        };

        // Act
        var markdown = context.GenerateMarkdownSection();

        // Assert - Verify azdo counts are not shown when all zero
        await Assert.That(markdown).Contains(PrincipalMappingHeader);
        await Assert.That(markdown).DoesNotContain("azdo user");
        await Assert.That(markdown).DoesNotContain("azdo group");
        await Assert.That(markdown).DoesNotContain("azdo project");
    }
}
