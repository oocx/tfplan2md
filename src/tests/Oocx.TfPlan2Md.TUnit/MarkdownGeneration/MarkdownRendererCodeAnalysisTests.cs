using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class MarkdownRendererCodeAnalysisTests
{
    private const string MinimalPlanPath = "TestData/minimal-plan.json";
    private const string RuleHelpUri = "rules/rule";
    private const string CriticalHelpUri = "rules/critical";
    private const string LowHelpUri = "rules/low";
    private const string ModuleHelpUri = "rules/module";
    private const string UnmatchedHelpUri = "rules/unmatched";

    private readonly TerraformPlanParser _parser = new();
    private readonly MarkdownRenderer _renderer = new();

    [Test]
    public void Render_CodeAnalysisSummary_RendersCountsAndTools()
    {
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.8);
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
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var criticalFinding = CreateFinding("null_resource.test.triggers.endpoint", CriticalHelpUri, 9.5);
        var lowFinding = CreateFinding("null_resource.test.triggers.endpoint", LowHelpUri, 1.5);
        var codeAnalysisInput = BuildInput([lowFinding, criticalFinding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("🔒 **Security & Quality:**", "because the metadata line should appear with lock icon");
        markdown.Should().Contain("#### 🔒 Security & Quality Findings", "because the findings table heading should have lock icon");
        markdown.Should().Contain("| 🚨 Critical | `triggers.endpoint` |", "because attribute paths should render with backticks for findings");
        markdown.Should().Contain($"[Details]({CriticalHelpUri})");

        var criticalIndex = markdown.IndexOf("🚨 Critical", StringComparison.Ordinal);
        var lowIndex = markdown.IndexOf("ℹ️ Low", StringComparison.Ordinal);
        criticalIndex.Should().BeLessThan(lowIndex, "because findings should be ordered by severity");
    }

    [Test]
    public void Render_CodeAnalysisFindingsTable_DoesNotInsertBlankLines()
    {
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.5);
        var codeAnalysisInput = BuildInput([finding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        var lines = markdown.Split('\n');
        var headerIndex = Array.FindIndex(lines, line => line.StartsWith("| Severity | Attribute | Finding | Remediation |", StringComparison.Ordinal));
        headerIndex.Should().BeGreaterThan(-1, "because the findings table header should be present");
        lines.Length.Should().BeGreaterThan(headerIndex + 2, "because the findings table should have rows");
        lines[headerIndex + 1].Should().StartWith("| -------- |", "because the header separator should follow the header");
        lines[headerIndex + 2].Should().StartWith("| ", "because the first finding row should immediately follow the header");
        lines[headerIndex + 2].Should().Contain("Critical", "because the example finding should render in the first row");
    }

    [Test]
    public void Render_CodeAnalysisWarnings_RendersWarningSection()
    {
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
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
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var moduleFinding = CreateFinding("module.network", ModuleHelpUri, 7.2);
        var unmatchedFinding = new CodeAnalysisFinding
        {
            Message = "Orphaned finding",
            HelpUri = UnmatchedHelpUri,
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

    [Test]
    public void Render_UnmatchedFindingsTable_EscapesMultilineMessages()
    {
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var unmatchedFinding = new CodeAnalysisFinding
        {
            Message = "Artifact: main.tf\n| ⚠️ High | Something broke",
            HelpUri = UnmatchedHelpUri,
            Locations = []
        };

        var codeAnalysisInput = BuildInput([unmatchedFinding]);
        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);
        var lines = markdown.Split('\n');
        var headerIndex = Array.FindIndex(lines, line => line.StartsWith("| Severity | Finding | Remediation |", StringComparison.Ordinal));
        headerIndex.Should().BeGreaterThan(-1, "because the unmatched findings table header should be present");
        lines.Length.Should().BeGreaterThan(headerIndex + 2, "because the unmatched findings table should have rows");
        lines[headerIndex + 1].Should().StartWith("| -------- |", "because the header separator should follow the header");
        lines[headerIndex + 2].Should().StartWith("| ", "because the first unmatched finding row should immediately follow the header");
        markdown.Should().Contain("Artifact: main.tf<br/>&#124; ⚠️ High &#124; Something broke");
    }

    [Test]
    public void Render_UnmatchedFindingsTable_IncludesLocationHints()
    {
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var unmatchedFinding = new CodeAnalysisFinding
        {
            Message = "Orphaned finding",
            HelpUri = UnmatchedHelpUri,
            Locations =
            [
                new CodeAnalysisLocation
                {
                    FullyQualifiedName = null,
                    ArtifactUri = "main.tf",
                    StartLine = 12,
                    StartColumn = 4
                }
            ]
        };

        var codeAnalysisInput = BuildInput([unmatchedFinding]);
        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);

        var markdown = _renderer.Render(model);

        markdown.Should().Contain("Orphaned finding<br/>Artifact: main.tf (Line: 12, Column: 4)");
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
