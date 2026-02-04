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
        markdown.Should().Contain("| Severity | Count | Resource Types |");
        markdown.Should().Contain("| 🚨 Critical | 1 |");
        markdown.Should().Contain("| 🚨 Critical | 1 | 1 null_resource |");
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
        var headerIndex = Array.FindIndex(lines, line => line.StartsWith("| Severity | Tool | Attribute | Finding | Remediation |", StringComparison.Ordinal));
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
        var headerIndex = Array.FindIndex(lines, line => line.StartsWith("| Severity | Tool | Finding | Remediation |", StringComparison.Ordinal));
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

    [Test]
    public void Render_SecurityFindingsTable_IncludesToolColumn()
    {
        // TC-01: Verify Tool column appears between Severity and Attribute columns
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test.triggers.endpoint", RuleHelpUri, 9.5) with { ToolName = "Checkov" };
        var codeAnalysisInput = BuildInput([finding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("| Severity | Tool | Attribute | Finding | Remediation |", "because the header should include Tool column");
        markdown.Should().Contain("| 🚨 Critical | Checkov | `triggers.endpoint` |", "because the Tool column should display the tool name between Severity and Attribute");
    }

    [Test]
    public void Render_ModuleFindingsTable_IncludesToolColumn()
    {
        // TC-02: Verify Tool column in module findings table
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var moduleFinding = CreateFinding("module.network", ModuleHelpUri, 7.2) with { ToolName = "tfsec" };
        var codeAnalysisInput = BuildInput([moduleFinding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("## Other Findings");
        markdown.Should().Contain("| Severity | Tool | Finding | Remediation |", "because the module findings table should include Tool column");
        markdown.Should().Contain("| ⚠️ High | tfsec |", "because the Tool column should display the tool name");
    }

    [Test]
    public void Render_UnmatchedFindingsTable_IncludesToolColumn()
    {
        // TC-03: Verify Tool column in unmatched findings table
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var unmatchedFinding = new CodeAnalysisFinding
        {
            Message = "Orphaned security finding",
            HelpUri = UnmatchedHelpUri,
            ToolName = "Trivy",
            Locations = []
        };
        var codeAnalysisInput = BuildInput([unmatchedFinding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("### Unmatched Findings");
        markdown.Should().Contain("| Severity | Tool | Finding | Remediation |", "because the unmatched findings table should include Tool column");
        markdown.Should().Contain("| ⚠️ Medium | Trivy | Orphaned security finding |", "because the Tool column should display the tool name");
    }

    [Test]
    public void Render_FindingsTable_HandlesNullToolName()
    {
        // TC-05: Verify null tool name displays "-"
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.5);
        var codeAnalysisInput = BuildInput([finding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        finding.ToolName.Should().BeNull("because the test is specifically for null tool names");
        markdown.Should().Contain("| 🚨 Critical | - |", "because null tool names should display as '-'");
    }

    [Test]
    public void Render_FindingsTable_HandlesEmptyToolName()
    {
        // TC-06: Verify empty string tool name displays "-"
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.5) with { ToolName = "" };
        var codeAnalysisInput = BuildInput([finding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("| 🚨 Critical | - |", "because empty string tool names should display as '-'");
    }

    [Test]
    public void Render_FindingsTable_HandlesMultipleTools()
    {
        // TC-04: Verify multiple different tools in one report
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));

        var checkovFinding = CreateFinding("null_resource.test.triggers.endpoint", RuleHelpUri, 9.5) with { ToolName = "Checkov" };

        var tfsecFinding = CreateFinding("module.network", ModuleHelpUri, 7.2) with { ToolName = "tfsec" };

        var trivyFinding = new CodeAnalysisFinding
        {
            Message = "Vulnerability found",
            HelpUri = UnmatchedHelpUri,
            ToolName = "Trivy",
            Locations = []
        };

        var codeAnalysisInput = BuildInput([checkovFinding, tfsecFinding, trivyFinding]);
        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("| 🚨 Critical | Checkov |", "because Checkov finding should show correct tool name");
        markdown.Should().Contain("| ⚠️ High | tfsec |", "because tfsec finding should show correct tool name");
        markdown.Should().Contain("| ⚠️ Medium | Trivy |", "because Trivy finding should show correct tool name");
    }

    [Test]
    public void Render_FindingsTable_HandlesSpecialCharsInToolName()
    {
        // TC-07: Verify tool names with special characters render correctly
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));

        var finding1 = CreateFinding("null_resource.test", RuleHelpUri, 9.5) with { ToolName = "tool-name" };

        var finding2 = CreateFinding("null_resource.test", RuleHelpUri + "2", 7.0) with { ToolName = "tool_name" };

        var finding3 = CreateFinding("null_resource.test", RuleHelpUri + "3", 5.0) with { ToolName = "tool.name" };

        var finding4 = CreateFinding("null_resource.test", RuleHelpUri + "4", 3.0) with { ToolName = "Tool-Name 2.0" };

        var codeAnalysisInput = BuildInput([finding1, finding2, finding3, finding4]);
        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("| tool-name |", "because hyphen should display correctly");
        markdown.Should().Contain("| tool_name |", "because underscore should display correctly");
        markdown.Should().Contain("| tool.name |", "because dot should display correctly");
        markdown.Should().Contain("| Tool-Name 2.0 |", "because mixed special characters should display correctly");
    }

    [Test]
    public void Render_FindingsTable_HandlesLongToolName()
    {
        // TC-08: Verify very long tool names don't break table structure
        var plan = _parser.Parse(File.ReadAllText(MinimalPlanPath));
        var finding = CreateFinding("null_resource.test", RuleHelpUri, 9.5) with { ToolName = "VeryLongSecurityScannerToolNameThatExceedsTypicalLength" };
        var codeAnalysisInput = BuildInput([finding]);

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);
        var model = builder.Build(plan);
        var markdown = _renderer.Render(model);

        markdown.Should().Contain("| VeryLongSecurityScannerToolNameThatExceedsTypicalLength |",
            "because long tool names should display in full without truncation");
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
