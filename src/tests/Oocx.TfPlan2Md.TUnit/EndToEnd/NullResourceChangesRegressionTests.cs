using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.EndToEnd;

/// <summary>
/// End-to-end regression tests for null/missing resource_changes in Terraform plan JSON.
/// Related issue: docs/issues/113-argument-null-source/analysis.md.
/// </summary>
public class NullResourceChangesRegressionTests
{
    /// <summary>
    /// TC-113-4: Parsing and rendering a plan without resource_changes key must not throw.
    /// This reproduces the "ArgumentNull_Generic Arg_ParamName_Name, source" crash.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void Render_PlanWithMissingResourceChanges_DoesNotThrow()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-resource-changes-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var act = () =>
        {
            var model = builder.Build(plan);
            var renderer = new MarkdownRenderer();
            return renderer.Render(model);
        };

        // Assert – must not throw ArgumentNullException("source") or any other exception
        act.Should().NotThrow();
    }

    /// <summary>
    /// TC-113-5: Parsing and rendering a plan with explicit null resource_changes must not throw.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void Render_PlanWithNullResourceChanges_DoesNotThrow()
    {
        // Arrange
        var json = File.ReadAllText("TestData/null-resource-changes-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var act = () =>
        {
            var model = builder.Build(plan);
            var renderer = new MarkdownRenderer();
            return renderer.Render(model);
        };

        // Assert – must not throw
        act.Should().NotThrow();
    }

    /// <summary>
    /// TC-113-6: Rendering a plan with only output changes should include the output section.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void Render_PlanWithMissingResourceChanges_IncludesOutputSection()
    {
        // Arrange
        var json = File.ReadAllText("TestData/no-resource-changes-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var model = builder.Build(plan);
        var renderer = new MarkdownRenderer();
        var markdown = renderer.Render(model);

        // Assert – the output change should appear in the rendered markdown
        markdown.Should().Contain("my_output");
    }

    /// <summary>
    /// TC-113-7: Regression test using the exact tfplan.json file attached to GitHub issue 113.
    /// The file has explicit null resource_changes and four output changes (create actions).
    /// Must not throw, and must render all four output names.
    /// Related issue: docs/issues/113-argument-null-source/analysis.md.
    /// </summary>
    [Test]
    public void Render_UserReportedIssue113Plan_DoesNotThrowAndRendersOutputs()
    {
        // Arrange – the exact file attached by the reporter on GitHub issue 113
        var json = File.ReadAllText("TestData/issue-113-user-reported-plan.json");
        var parser = new TerraformPlanParser();
        var plan = parser.Parse(json);
        var builder = new ReportModelBuilder();

        // Act
        var markdown = string.Empty;
        var act = () =>
        {
            var model = builder.Build(plan);
            var renderer = new MarkdownRenderer();
            markdown = renderer.Render(model);
        };

        // Assert – must not throw ArgumentNullException("source") or any other exception
        act.Should().NotThrow();

        // Assert – all four output names from the reporter's plan appear in the rendered markdown
        markdown.Should().Contain("artifact_registry_repository");
        markdown.Should().Contain("foo_direct_wif");
        markdown.Should().Contain("qix_direct_wif");
        markdown.Should().Contain("bar_direct_wif");
    }
}
