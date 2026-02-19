using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.RenderTargets;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests for the --details CLI option controlling resource details block expansion.
/// Related feature: docs/features/092-details-display-mode/specification.md.
/// </summary>
public class MarkdownRendererDetailsDisplayModeTests
{
    private const string MinimalPlanPath = "TestData/minimal-plan.json";
    private const string RuleHelpUri = "rules/rule";

    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Render_DetailsOpenOnWarnings_Default_OpenWhenFindingsPresent()
    {
        // Arrange: resource with a code analysis finding, default mode
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.8);
        var codeAnalysisInput = BuildInput([finding]);
        var builder = new ReportModelBuilder(
            codeAnalysisInput: codeAnalysisInput,
            detailsDisplayMode: DetailsDisplayMode.OpenOnWarnings);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(detailsDisplayMode: DetailsDisplayMode.OpenOnWarnings);

        // Act
        var markdown = renderer.Render(model);

        // Assert: details block should be open because there are code analysis findings
        markdown.Should().Contain("<details open style=", "because resource has code analysis findings");
    }

    [Test]
    public void Render_DetailsOpenOnWarnings_Default_ClosedWhenNoFindings()
    {
        // Arrange: resource without code analysis findings, default mode
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var builder = new ReportModelBuilder(detailsDisplayMode: DetailsDisplayMode.OpenOnWarnings);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(detailsDisplayMode: DetailsDisplayMode.OpenOnWarnings);

        // Act
        var markdown = renderer.Render(model);

        // Assert: details block should NOT be open when no findings
        markdown.Should().NotContain("<details open style=", "because resource has no code analysis findings");
        markdown.Should().Contain("<details style=", "because resource details block should still be present");
    }

    [Test]
    public void Render_DetailsAlwaysOpen_OpenEvenWithoutFindings()
    {
        // Arrange: resource without code analysis findings, always-open mode
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var builder = new ReportModelBuilder(detailsDisplayMode: DetailsDisplayMode.Open);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(detailsDisplayMode: DetailsDisplayMode.Open);

        // Act
        var markdown = renderer.Render(model);

        // Assert: details block should always be open
        markdown.Should().Contain("<details open style=", "because --details open forces all blocks open");
    }

    [Test]
    public void Render_DetailsAlwaysClosed_ClosedEvenWithFindings()
    {
        // Arrange: resource with a code analysis finding, always-closed mode
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.8);
        var codeAnalysisInput = BuildInput([finding]);
        var builder = new ReportModelBuilder(
            codeAnalysisInput: codeAnalysisInput,
            detailsDisplayMode: DetailsDisplayMode.Closed);
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer(detailsDisplayMode: DetailsDisplayMode.Closed);

        // Act
        var markdown = renderer.Render(model);

        // Assert: details block should NOT be open even though findings exist
        markdown.Should().NotContain("<details open style=", "because --details closed forces all blocks closed");
        markdown.Should().Contain("<details style=", "because resource details block should still be present");
    }

    [Test]
    public void Render_DefaultMode_BehavesLikeOpenOnWarnings()
    {
        // Arrange: verify the default constructor parameter is OpenOnWarnings
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var builder = new ReportModelBuilder();
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer();

        // Act
        var markdown = renderer.Render(model);

        // Assert: without findings, the default mode should produce closed details
        markdown.Should().NotContain("<details open style=");
        markdown.Should().Contain("<details style=");
    }

    private static CodeAnalysisFinding CreateFinding(string location, string helpUri, double? securitySeverity)
    {
        return new CodeAnalysisFinding
        {
            Message = "Finding message",
            HelpUri = helpUri,
            SecuritySeverity = securitySeverity,
            Locations =
            [
                new CodeAnalysisLocation { FullyQualifiedName = location }
            ]
        };
    }

    private static CodeAnalysisInput BuildInput(IReadOnlyList<CodeAnalysisFinding> findings)
    {
        return new CodeAnalysisInput
        {
            Model = new CodeAnalysisModel
            {
                Tools =
                [
                    new CodeAnalysisTool
                    {
                        Name = "Checkov",
                        Version = "3.2.10"
                    }
                ],
                Findings = findings
            },
            Warnings = [],
            MinimumLevel = null,
            FailOnLevel = null
        };
    }
}
