using System;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Diagnostics;

/// <summary>
/// Tests for diagnostic collection and markdown formatting.
/// Related feature: docs/features/038-debug-output/.
/// </summary>
[Category("Unit")]
public class DiagnosticContextTests
{
    private const string PrincipalMappingHeader = "### Principal Mapping";
    private const string PrincipalsFileName = "principals.json";
    private const string PrincipalsFilePath = "/data/principals.json";
    private const string VirtualNetworkResourceType = "azurerm_virtual_network";

    [Test]
    public async Task Format_EmptyDiagnostics_ReturnsHeaderWithNoData()
    {
        var markdown = Render(new DiagnosticContext());

        await Assert.That(markdown).Contains("<details>");
        await Assert.That(markdown).Contains("<summary>🐛\u00A0Debug Information</summary>");
        await Assert.That(markdown).Contains("<br>");
        await Assert.That(markdown).Contains("No diagnostics collected");
        await Assert.That(markdown).Contains("</details>");
        await Assert.That(markdown).DoesNotContain("<details open>");
    }

    [Test]
    public async Task Format_WithAllDiagnostics_ReturnsFormattedMarkdown()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordPrincipalTypeCount("users", 45);
        context.RecordPrincipalTypeCount("groups", 12);
        context.RecordPrincipalTypeCount("service principals", 8);
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.Principal,
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example",
            "not found in mapping file"));
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.RoleDefinition,
            "87654321-4321-4321-4321-210987654321",
            "azurerm_role_assignment.reader",
            "not found in mapping file or built-in roles"));
        context.RecordTemplateResolution(new TemplateResolution(
            "azurerm_firewall_network_rule_collection",
            "Built-in resource-specific template"));
        context.RecordTemplateResolution(new TemplateResolution(VirtualNetworkResourceType, "Default template"));
        context.RecordTemplateResolution(new TemplateResolution(
            "azurerm_custom_resource",
            "Custom template: /templates/azurerm/custom_resource.sbn"));

        var markdown = Render(context);

        await Assert.That(markdown).Contains("<details>");
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

    [Test]
    public async Task Format_FailedResolutions_FormatsCorrectly()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.Principal,
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example",
            "not found in mapping file"));
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.RoleDefinition,
            "87654321-4321-4321-4321-210987654321",
            "azurerm_role_assignment.reader",
            "not found in mapping file or built-in roles"));

        var markdown = Render(context);

        await Assert.That(markdown).Contains("- Principal `12345678-1234-1234-1234-123456789012` (referenced in `azurerm_role_assignment.example`)");
        await Assert.That(markdown).Contains("- Role definition `87654321-4321-4321-4321-210987654321` (referenced in `azurerm_role_assignment.reader`)");
    }

    [Test]
    public async Task Format_TemplateResolutions_FormatsCorrectly()
    {
        var context = new DiagnosticContext();
        context.RecordTemplateResolution(new TemplateResolution(
            "azurerm_firewall_network_rule_collection",
            "Built-in resource-specific template"));
        context.RecordTemplateResolution(new TemplateResolution(VirtualNetworkResourceType, "Default template"));
        context.RecordTemplateResolution(new TemplateResolution(
            "azurerm_custom_resource",
            "Custom template: /templates/azurerm/custom_resource.sbn"));

        var markdown = Render(context);

        await Assert.That(markdown).Contains("- `azurerm_firewall_network_rule_collection`: Built-in resource-specific template");
        await Assert.That(markdown).Contains($"- `{VirtualNetworkResourceType}`: Default template");
        await Assert.That(markdown).Contains("- `azurerm_custom_resource`: Custom template: /templates/azurerm/custom_resource.sbn");
    }

    [Test]
    public async Task Format_NoPrincipalMappingFile_OmitsPrincipalSection()
    {
        var markdown = Render(new DiagnosticContext());

        await Assert.That(markdown).DoesNotContain(PrincipalMappingHeader);
        await Assert.That(markdown).Contains("<details>");
        await Assert.That(markdown).Contains("</details>");
    }

    [Test]
    public async Task Format_PrincipalMappingLoadFailure_ShowsFailureMessage()
    {
        var markdown = Render(CreateFailureContext(
            path: "missing.json",
            errorType: PrincipalLoadError.FileNotFound,
            message: "File not found",
            details: "Could not find file 'missing.json'"));

        await Assert.That(markdown).Contains(PrincipalMappingHeader);
        await Assert.That(markdown).Contains("Failed to load from 'missing.json'");
    }

    [Test]
    public async Task Format_DuplicateResourceTypes_ShowsOnlyFirst()
    {
        var context = new DiagnosticContext();
        context.RecordTemplateResolution(new TemplateResolution(VirtualNetworkResourceType, "Built-in template"));
        context.RecordTemplateResolution(new TemplateResolution(VirtualNetworkResourceType, "Custom template"));

        var markdown = Render(context);
        var lines = markdown.Split('\n').Where(line => line.Contains(VirtualNetworkResourceType, StringComparison.Ordinal)).ToList();

        lines.Should().HaveCount(1);
        await Assert.That(lines[0]).Contains("Built-in template");
    }

    [Test]
    public async Task Format_PrincipalTypeCounts_AreSortedAlphabetically()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordPrincipalTypeCount("users", 45);
        context.RecordPrincipalTypeCount("groups", 12);
        context.RecordPrincipalTypeCount("service principals", 8);

        var markdown = Render(context);
        var principalLine = markdown.Split('\n').First(line => line.Contains("Found", StringComparison.Ordinal));
        var groupsIndex = principalLine.IndexOf("groups", StringComparison.Ordinal);
        var servicePrincipalIndex = principalLine.IndexOf("service principals", StringComparison.Ordinal);
        var usersIndex = principalLine.IndexOf("users", StringComparison.Ordinal);

        await Assert.That(groupsIndex).IsLessThan(servicePrincipalIndex);
        await Assert.That(servicePrincipalIndex).IsLessThan(usersIndex);
    }

    [Test]
    public async Task Format_SingleFailedResolution_UsesSingularForm()
    {
        var context = CreateSuccessfulPrincipalMappingContext();
        context.RecordFailedResolution(new FailedResolution(
            FailedResolutionType.Principal,
            "12345678-1234-1234-1234-123456789012",
            "azurerm_role_assignment.example",
            "not found in mapping file"));

        var markdown = Render(context);

        await Assert.That(markdown).Contains("Failed to resolve 1 mapping:");
        await Assert.That(markdown).DoesNotContain("1 mappings");
    }

    [Test]
    public async Task Format_PrincipalMappingFileNotFound_ShowsDetailedDiagnostics()
    {
        var markdown = Render(CreateFailureContext(
            path: PrincipalsFilePath,
            errorType: PrincipalLoadError.FileNotFound,
            message: "File not found",
            details: $"Could not find file '{PrincipalsFilePath}'",
            fileExists: false,
            directoryExists: true));

        await Assert.That(markdown).Contains($"Failed to load from '{PrincipalsFilePath}'");
        await Assert.That(markdown).Contains("**Diagnostic Details:**");
        await Assert.That(markdown).Contains("File exists: ❌");
        await Assert.That(markdown).Contains("Directory exists: ✅");
        await Assert.That(markdown).Contains("Error type: FileNotFound");
        await Assert.That(markdown).Contains("File not found");
    }

    [Test]
    public async Task Format_JsonParseError_ShowsLineAndColumn()
    {
        var markdown = Render(CreateFailureContext(
            path: PrincipalsFilePath,
            errorType: PrincipalLoadError.JsonParseError,
            message: "Invalid JSON syntax",
            details: "Unexpected character 'i' at line 3, column 15",
            fileExists: true,
            directoryExists: true));

        await Assert.That(markdown).Contains("Error type: JsonParseError");
        await Assert.That(markdown).Contains("Invalid JSON syntax");
        await Assert.That(markdown).Contains("line 3, column 15");
    }

    [Test]
    public async Task Format_DirectoryNotFound_ShowsDirectoryDiagnostics()
    {
        var markdown = Render(CreateFailureContext(
            path: "/data/subdir/principals.json",
            errorType: PrincipalLoadError.DirectoryNotFound,
            message: "Directory not found",
            details: "Could not find directory '/data/subdir'",
            fileExists: false,
            directoryExists: false));

        await Assert.That(markdown).Contains("Directory exists: ❌");
        await Assert.That(markdown).Contains("Error type: DirectoryNotFound");
        await Assert.That(markdown).Contains("Directory not found");
    }

    [Test]
    public async Task Format_FileNotFound_IncludesDockerGuidance()
    {
        var markdown = Render(CreateFailureContext(
            path: PrincipalsFilePath,
            errorType: PrincipalLoadError.FileNotFound,
            message: "File not found",
            details: $"Could not find file '{PrincipalsFilePath}'",
            fileExists: false,
            directoryExists: true));

        await Assert.That(markdown).Contains("**Common Solutions:**");
        await Assert.That(markdown).Contains("docker run");
        await Assert.That(markdown).Contains("-v");
        await Assert.That(markdown).Contains("--principal-mapping");
    }

    private static DiagnosticContext CreateSuccessfulPrincipalMappingContext()
    {
        var context = new DiagnosticContext();
        context.RecordPrincipalMappingFileProvided(PrincipalsFileName);
        context.RecordPrincipalMappingLoadedSuccessfully();
        return context;
    }

    private static DiagnosticContext CreateFailureContext(
        string path,
        PrincipalLoadError errorType,
        string message,
        string details,
        bool? fileExists = null,
        bool? directoryExists = null)
    {
        var context = new DiagnosticContext();
        context.RecordPrincipalMappingFileProvided(path);

        if (fileExists.HasValue && directoryExists.HasValue)
        {
            context.RecordPrincipalMappingPathStatus(fileExists.Value, directoryExists.Value);
        }

        context.RecordPrincipalMappingLoadFailure(errorType, message, details);
        return context;
    }

    private static string Render(DiagnosticContext context)
    {
        return DiagnosticMarkdownFormatter.Format(context.CreateSnapshot());
    }
}
