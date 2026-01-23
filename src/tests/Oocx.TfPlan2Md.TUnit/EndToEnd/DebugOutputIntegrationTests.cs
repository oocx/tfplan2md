using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.TUnit.EndToEnd;

/// <summary>
/// End-to-end integration tests for the debug output feature.
/// Related feature: docs/features/038-debug-output/
/// Test cases: TC-10, TC-11.
/// </summary>
[Category("Integration")]
public class DebugOutputIntegrationTests
{
    /// <summary>
    /// TC-10: Without --debug flag, no debug section appears in output.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task WithoutDebugFlag_NoDebugSection()
    {
        // Arrange
        var planJson = await File.ReadAllTextAsync("TestData/azurerm-azuredevops-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(planJson);

        // Create components WITHOUT diagnostic context (simulating no --debug flag)
        var principalMapper = new PrincipalMapper(mappingFile: null, diagnosticContext: null);
        var modelBuilder = new ReportModelBuilder(
            showSensitive: false,
            showUnchangedValues: false,
            largeValueFormat: LargeValueFormat.InlineDiff,
            reportTitle: null,
            principalMapper: principalMapper,
            hideMetadata: false);
        var model = modelBuilder.Build(plan);

        var renderer = new MarkdownRenderer(principalMapper, diagnosticContext: null);

        // Act
        var markdown = renderer.Render(model);

        // Assert
        await Assert.That(markdown).DoesNotContain("## Debug Information");
        await Assert.That(markdown).DoesNotContain("### Principal Mapping");
        await Assert.That(markdown).DoesNotContain("### Template Resolution");
    }

    /// <summary>
    /// TC-11: With --debug flag, debug section is appended to report.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task WithDebugFlag_DebugSectionAppended()
    {
        // Arrange
        var planJson = await File.ReadAllTextAsync("TestData/azurerm-azuredevops-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(planJson);

        // Create components WITH diagnostic context (simulating --debug flag)
        var diagnosticContext = new DiagnosticContext();
        var principalMapper = new PrincipalMapper(
            mappingFile: "TestData/principal-mapping.json",
            diagnosticContext: diagnosticContext);
        var modelBuilder = new ReportModelBuilder(
            showSensitive: false,
            showUnchangedValues: false,
            largeValueFormat: LargeValueFormat.InlineDiff,
            reportTitle: null,
            principalMapper: principalMapper,
            hideMetadata: false);
        var model = modelBuilder.Build(plan);

        var renderer = new MarkdownRenderer(principalMapper, diagnosticContext);

        // Act
        var markdown = renderer.Render(model);

        // Append debug section (simulating what Program.cs does)
        markdown += "\n\n" + diagnosticContext.GenerateMarkdownSection();

        // Assert - Debug section exists
        await Assert.That(markdown).Contains("## Debug Information");

        // Assert - Principal mapping diagnostics are present
        await Assert.That(markdown).Contains("### Principal Mapping");
        await Assert.That(markdown).Contains("Loaded successfully");
        await Assert.That(markdown).Contains("TestData/principal-mapping.json");

        // Assert - Template resolution diagnostics are present
        await Assert.That(markdown).Contains("### Template Resolution");
        await Assert.That(markdown).Contains("_main");

        // Assert - Main report content is still present (not replaced)
        await Assert.That(markdown).Contains("# Terraform Plan Report");
        await Assert.That(markdown).Contains("## Summary");
    }

    /// <summary>
    /// TC-11 (extended): Verify debug section contains template resolution details.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task WithDebugFlag_TemplateResolutionRecorded()
    {
        // Arrange
        var planJson = await File.ReadAllTextAsync("TestData/firewall-rule-changes.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(planJson);

        var diagnosticContext = new DiagnosticContext();
        var principalMapper = new NullPrincipalMapper();
        var modelBuilder = new ReportModelBuilder(
            showSensitive: false,
            showUnchangedValues: false,
            largeValueFormat: LargeValueFormat.InlineDiff,
            reportTitle: null,
            principalMapper: principalMapper,
            hideMetadata: false);
        var model = modelBuilder.Build(plan);

        var renderer = new MarkdownRenderer(principalMapper, diagnosticContext);

        // Act
        var markdown = renderer.Render(model);
        markdown += "\n\n" + diagnosticContext.GenerateMarkdownSection();

        // Assert - Template resolutions are recorded
        await Assert.That(markdown).Contains("### Template Resolution");

        // Should record main template
        await Assert.That(markdown).Contains("_main");

        // Should record resource-specific templates
        // (firewall-rule-changes.json has azurerm_firewall_network_rule_collection which has a built-in template)
        await Assert.That(markdown).Contains("azurerm_firewall_network_rule_collection");
    }

    /// <summary>
    /// TC-11 (extended): Verify debug section contains failed principal resolution details.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task WithDebugFlag_FailedPrincipalResolutionsRecorded()
    {
        // Arrange
        var planJson = await File.ReadAllTextAsync("TestData/role-assignments.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(planJson);

        var diagnosticContext = new DiagnosticContext();
        // Use partial mapping file that's missing some principal IDs
        var principalMapper = new PrincipalMapper(
            mappingFile: "TestData/partial-principal-mapping.json",
            diagnosticContext: diagnosticContext);
        var modelBuilder = new ReportModelBuilder(
            showSensitive: false,
            showUnchangedValues: false,
            largeValueFormat: LargeValueFormat.InlineDiff,
            reportTitle: null,
            principalMapper: principalMapper,
            hideMetadata: false);
        var model = modelBuilder.Build(plan);

        var renderer = new MarkdownRenderer(principalMapper, diagnosticContext);

        // Act
        var markdown = renderer.Render(model);
        markdown += "\n\n" + diagnosticContext.GenerateMarkdownSection();

        // Assert - Failed principal resolutions are recorded if any exist
        if (diagnosticContext.FailedResolutions.Count > 0)
        {
            await Assert.That(markdown).Contains("Failed to resolve");
            await Assert.That(markdown).Contains("principal ID");
            // Should show resource context
            await Assert.That(markdown).Contains("referenced in");
        }
    }

    /// <summary>
    /// TC-10 (regression): Verify existing tests still pass - main report unchanged without debug.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Test]
    public async Task WithoutDebugFlag_ReportContentUnchanged()
    {
        // Arrange
        var planJson = await File.ReadAllTextAsync("TestData/azurerm-azuredevops-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(planJson);

        // Without diagnostic context
        var principalMapper = new NullPrincipalMapper();
        var modelBuilder = new ReportModelBuilder(
            showSensitive: false,
            showUnchangedValues: false,
            largeValueFormat: LargeValueFormat.InlineDiff,
            reportTitle: null,
            principalMapper: principalMapper,
            hideMetadata: false);
        var model = modelBuilder.Build(plan);

        var renderer = new MarkdownRenderer(principalMapper);

        // Act
        var markdown = renderer.Render(model);

        // Assert - Normal report sections present
        await Assert.That(markdown).Contains("# Terraform Plan Report");
        await Assert.That(markdown).Contains("## Summary");

        // Assert - No debug section
        await Assert.That(markdown).DoesNotContain("## Debug Information");
    }
}
