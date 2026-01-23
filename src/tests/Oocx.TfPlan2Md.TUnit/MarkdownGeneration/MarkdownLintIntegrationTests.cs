using System.IO;
using System.Linq;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Tests.TestData;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Docker-based markdownlint integration tests for TUnit.
/// These tests run the actual markdownlint-cli2 tool via Docker to ensure
/// the generated markdown passes all linting rules in a production-like environment.
/// </summary>
/// <remarks>
/// These tests have a longer timeout (2 minutes) because running Docker containers
/// takes significant time.
/// </remarks>
[Timeout(120_000)] // 2 minutes - accounts for Docker container startup
public class MarkdownLintIntegrationTests
{
    private static readonly Lazy<MarkdownLintFixture> s_tunitFixture = new(
        () =>
        {
            var fixture = new MarkdownLintFixture();
            fixture.InitializeAsync().GetAwaiter().GetResult();
            return fixture;
        });

    private static MarkdownLintFixture Fixture => s_tunitFixture.Value;
    private readonly TerraformPlanParser _parser = new();

    /// <summary>
    /// Verifies that the comprehensive demo output passes all markdownlint rules.
    /// This is the primary integration test for markdown quality.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Lint_ComprehensiveDemo_PassesAllRules(CancellationToken cancellationToken)
    {
        if (!Fixture.IsDockerAvailable || !Fixture.ImageReady)
        {
            // Skip test if Docker not available
            return;
        }

        var plan = _parser.Parse(await File.ReadAllTextAsync(DemoPaths.DemoPlanPath, cancellationToken));
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer(new PrincipalMapper(DemoPaths.DemoPrincipalsPath));

        var markdown = renderer.Render(model);
        var result = await Fixture.LintAsync(markdown);

        if (!result.IsValid)
        {
            var errorMsg = $"Expected no linting errors but found {result.Violations.Count}:\n" +
                string.Join("\n", result.Violations.Select(v => $"  Line {v.Line}: {v.RuleId} - {v.Message}"));
            throw new Exception(errorMsg);
        }

        await Assert.That(result.IsValid).IsTrue();
    }

    /// <summary>
    /// Verifies that all test plans in TestData produce valid markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Lint_AllTestPlans_PassAllRules(CancellationToken cancellationToken)
    {
        if (!Fixture.IsDockerAvailable || !Fixture.ImageReady)
        {
            return;
        }

        var testDataDir = Path.GetFullPath("TestData");
        var planFiles = Directory.GetFiles(testDataDir, "*.json")
            .Where(f => !f.EndsWith("demo-principals.json", StringComparison.Ordinal));

        var failures = new List<string>();

        foreach (var planFile in planFiles)
        {
            var json = await File.ReadAllTextAsync(planFile, cancellationToken);

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
                var result = await Fixture.LintAsync(markdown);

                if (!result.IsValid)
                {
                    var fileName = Path.GetFileName(planFile);
                    failures.Add($"{fileName}: {result.Violations.Count} violations\n" +
                        string.Join("\n", result.Violations.Take(5).Select(v => $"    Line {v.Line}: {v.RuleId} - {v.Message}")));
                }
            }
            catch (Exception ex)
            {
                var fileName = Path.GetFileName(planFile);
                if (!ex.Message.Contains("parse") && !ex.Message.Contains("format"))
                {
                    failures.Add($"{fileName}: Exception - {ex.Message}");
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new Exception($"Expected all test plans to pass linting but found failures:\n{string.Join("\n", failures)}");
        }

        await Assert.That(failures).IsEmpty();
    }

    /// <summary>
    /// Verifies that the summary template produces valid markdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Lint_SummaryTemplate_PassesAllRules(CancellationToken cancellationToken)
    {
        if (!Fixture.IsDockerAvailable || !Fixture.ImageReady)
        {
            return;
        }

        var plan = _parser.Parse(await File.ReadAllTextAsync(DemoPaths.DemoPlanPath, cancellationToken));
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model, "summary");
        var result = await Fixture.LintAsync(markdown);

        if (!result.IsValid)
        {
            var errorMsg = "Summary template produced invalid markdown:\n" +
                string.Join("\n", result.Violations.Select(v => $"  Line {v.Line}: {v.RuleId} - {v.Message}"));
            throw new Exception(errorMsg);
        }

        await Assert.That(result.IsValid).IsTrue();
    }

    /// <summary>
    /// Verifies that markdown with special characters in resource names passes linting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task Lint_BreakingPlan_PassesAllRules(CancellationToken cancellationToken)
    {
        if (!Fixture.IsDockerAvailable || !Fixture.ImageReady)
        {
            return;
        }

        var json = await File.ReadAllTextAsync("TestData/markdown-breaking-plan.json", cancellationToken);
        var plan = _parser.Parse(json);
        var model = new ReportModelBuilder().Build(plan);
        var renderer = new MarkdownRenderer();

        var markdown = renderer.Render(model);
        var result = await Fixture.LintAsync(markdown);

        if (!result.IsValid)
        {
            var errorMsg = "Breaking plan produced invalid markdown:\n" +
                string.Join("\n", result.Violations.Select(v => $"  Line {v.Line}: {v.RuleId} - {v.Message}"));
            throw new Exception(errorMsg);
        }

        await Assert.That(result.IsValid).IsTrue();
    }
}
