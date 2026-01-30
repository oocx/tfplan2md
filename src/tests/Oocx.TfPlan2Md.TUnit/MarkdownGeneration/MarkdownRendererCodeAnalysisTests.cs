using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererCodeAnalysisTests
{
    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    [Test]
    public void Render_CodeAnalysisSummary_RendersCountsAndTools()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var finding = CreateFinding("null_resource.test", "https://example.com/rule", 9.8);
        var codeAnalysisInput = BuildInput([finding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("## Code Analysis Summary");
        markdown.Should().Contain("| 🚨 Critical | 1 |");
        markdown.Should().Contain("**Tools Used:** Checkov 3.2.10");
    }

    [Test]
    public void Render_CodeAnalysisFindingsTable_RendersRemediationAndOrdering()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var criticalFinding = CreateFinding("null_resource.test.triggers.endpoint", "https://example.com/critical", 9.5);
        var lowFinding = CreateFinding("null_resource.test.triggers.endpoint", "https://example.com/low", 1.5);
        var codeAnalysisInput = BuildInput([lowFinding, criticalFinding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("**Security & Quality Findings:**");
        markdown.Should().Contain("| 🚨 Critical | triggers.endpoint |", "because attribute paths should render for findings");
        markdown.Should().Contain("[Details](https://example.com/critical)");

        var criticalIndex = markdown.IndexOf("🚨 Critical", StringComparison.Ordinal);
        var lowIndex = markdown.IndexOf("ℹ️ Low", StringComparison.Ordinal);
        criticalIndex.Should().BeLessThan(lowIndex, "because findings should be ordered by severity");
    }

    [Test]
    public void Render_CodeAnalysisWarnings_RendersWarningSection()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var codeAnalysisInput = new CodeAnalysisInput
        {
            Model = new CodeAnalysisModel
            {
                Tools = [],
                Findings = []
            },
            Warnings =
            [
                new CodeAnalysisWarning
                {
                    FilePath = "invalid.sarif",
                    Message = "Invalid JSON"
                }
            ],
            MinimumLevel = null,
            FailOnLevel = null
        };

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("### Code Analysis Warnings");
        markdown.Should().Contain("invalid.sarif");
        markdown.Should().Contain("Invalid JSON");
    }

    [Test]
    public void Render_OtherFindingsSection_RendersModuleAndUnmatched()
    {
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var moduleFinding = CreateFinding("module.network", "https://example.com/module", 7.2);
        var unmatchedFinding = new CodeAnalysisFinding
        {
            Message = "Orphaned finding",
            HelpUri = "https://example.com/unmatched",
            Locations = []
        };

        var codeAnalysisInput = BuildInput([moduleFinding, unmatchedFinding]);
        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("## Other Findings");
        markdown.Should().Contain("### Module:");
        markdown.Should().Contain("module.network");
        markdown.Should().Contain("### Unmatched Findings");
        markdown.Should().Contain("Orphaned finding");
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
}
