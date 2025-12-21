using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Docker-based markdownlint integration tests.
/// These tests run the actual markdownlint-cli2 tool via Docker to ensure
/// the generated markdown passes all linting rules in a production-like environment.
/// </summary>
/// <remarks>
/// Unlike the C#-based validation tests, these tests use the real markdownlint tool,
/// ensuring that any linter-specific rules or edge cases are caught.
/// Tests are skipped if Docker is not available.
/// </remarks>
[Collection(nameof(MarkdownLintCollection))]
public class MarkdownLintIntegrationTests
{
    private readonly MarkdownLintFixture _lintFixture;
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Initializes the test class with the shared markdownlint fixture.
    /// </summary>
    /// <param name="lintFixture">The Docker-based markdownlint fixture.</param>
    public MarkdownLintIntegrationTests(MarkdownLintFixture lintFixture)
    {
        _lintFixture = lintFixture;
    }

    /// <summary>
    /// Skips the test if Docker is not available.
    /// </summary>
    private void SkipIfDockerNotAvailable()
    {
        Xunit.Skip.If(!_lintFixture.IsDockerAvailable, "Docker is not available");
        Xunit.Skip.If(!_lintFixture.ImageReady, "markdownlint Docker image could not be pulled");
    }

    /// <summary>
    /// Verifies that the comprehensive demo output passes all markdownlint rules.
    /// This is the primary integration test for markdown quality.
    /// </summary>
    [SkippableFact]
    public async Task Lint_ComprehensiveDemo_PassesAllRules()
    {
        SkipIfDockerNotAvailable();

        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);
        var result = await _lintFixture.LintAsync(markdown);

        result.IsValid.Should().BeTrue(
            $"Expected no linting errors but found {result.Violations.Count}:\n" +
            string.Join("\n", result.Violations.Select(v => $"  Line {v.Line}: {v.RuleId} - {v.Message}")));
    }

    /// <summary>
    /// Verifies that all test plans in TestData produce valid markdown.
    /// </summary>
    [SkippableFact]
    public async Task Lint_AllTestPlans_PassAllRules()
    {
        SkipIfDockerNotAvailable();

        var testDataDir = Path.GetFullPath("TestData");
        var planFiles = Directory.GetFiles(testDataDir, "*.json")
            .Where(f => !f.EndsWith("demo-principals.json", StringComparison.Ordinal)); // Skip non-plan files

        var failures = new List<string>();

        foreach (var planFile in planFiles)
        {
            var json = await File.ReadAllTextAsync(planFile);

            // Skip files that aren't valid Terraform plans
            if (!json.Contains("\"resource_changes\""))
            {
                continue;
            }

            try
            {
                var plan = _parser.Parse(json);
                var model = new ReportModelBuilder().Build(plan);
                var renderer = new MarkdownRenderer();

                var markdown = renderer.Render(model);
                var result = await _lintFixture.LintAsync(markdown);

                if (!result.IsValid)
                {
                    var fileName = Path.GetFileName(planFile);
                    failures.Add($"{fileName}: {result.Violations.Count} violations\n" +
                        string.Join("\n", result.Violations.Take(5).Select(v => $"    Line {v.Line}: {v.RuleId} - {v.Message}")));
                }
            }
            catch (Exception ex)
            {
                // Some test files may not be valid plans, that's OK
                var fileName = Path.GetFileName(planFile);
                if (!ex.Message.Contains("parse") && !ex.Message.Contains("format"))
                {
                    failures.Add($"{fileName}: Exception - {ex.Message}");
                }
            }
        }

        failures.Should().BeEmpty(
            $"Expected all test plans to pass linting but found failures:\n{string.Join("\n", failures)}");
    }

    /// <summary>
    /// Verifies that the summary template produces valid markdown.
    /// </summary>
    [SkippableFact]
    public async Task Lint_SummaryTemplate_PassesAllRules()
    {
        SkipIfDockerNotAvailable();

        var plan = _parser.Parse(File.ReadAllText(DemoPaths.DemoPlanPath));
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model, "summary");
        var result = await _lintFixture.LintAsync(markdown);

        result.IsValid.Should().BeTrue(
            $"Summary template produced invalid markdown:\n" +
            string.Join("\n", result.Violations.Select(v => $"  Line {v.Line}: {v.RuleId} - {v.Message}")));
    }

    /// <summary>
    /// Verifies that markdown with special characters in resource names passes linting.
    /// </summary>
    [SkippableFact]
    public async Task Lint_BreakingPlan_PassesAllRules()
    {
        SkipIfDockerNotAvailable();

        var json = File.ReadAllText("TestData/markdown-breaking-plan.json");
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);
        var result = await _lintFixture.LintAsync(markdown);

        result.IsValid.Should().BeTrue(
            $"Breaking plan produced invalid markdown:\n" +
            string.Join("\n", result.Violations.Select(v => $"  Line {v.Line}: {v.RuleId} - {v.Message}")));
    }
}
