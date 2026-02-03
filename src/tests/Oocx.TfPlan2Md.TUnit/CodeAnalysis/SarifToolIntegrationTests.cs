using System;
using System.Linq;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class SarifToolIntegrationTests
{
    private readonly SarifParser _parser = new();

    [Test]
    public void Parse_CheckovSarif_ReturnsToolAndFindings()
    {
        var model = _parser.ParseFile("TestData/code-analysis/checkov.sarif");

        model.Tools.Should().NotBeEmpty();
        model.Tools.Select(t => t.Name).Should().Contain(name => name.Contains("checkov", StringComparison.OrdinalIgnoreCase));
        model.Findings.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies plan-based Checkov SARIF inputs are parsed successfully.
    /// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
    /// </summary>
    [Test]
    public void Parse_CheckovPlanSarif_ReturnsToolAndFindings()
    {
        var model = _parser.ParseFile("TestData/code-analysis/checkov-plan.sarif");

        model.Tools.Should().NotBeEmpty();
        model.Tools.Select(t => t.Name).Should().Contain(name => name.Contains("checkov", StringComparison.OrdinalIgnoreCase));
        model.Findings.Should().NotBeEmpty();
    }

    [Test]
    public void Parse_TrivySarif_ReturnsToolAndFindings()
    {
        var model = _parser.ParseFile("TestData/code-analysis/trivy.sarif");

        model.Tools.Should().NotBeEmpty();
        model.Tools.Select(t => t.Name).Should().Contain(name => name.Contains("trivy", StringComparison.OrdinalIgnoreCase));
        model.Findings.Should().NotBeEmpty();
    }

    [Test]
    public void Parse_TflintSarif_ReturnsToolAndFindings()
    {
        var model = _parser.ParseFile("TestData/code-analysis/tflint.sarif");

        model.Tools.Should().NotBeEmpty();
        model.Tools.Select(t => t.Name).Should().Contain(name => name.Contains("tflint", StringComparison.OrdinalIgnoreCase));
        model.Findings.Should().NotBeEmpty();
    }

    /// <summary>
    /// Verifies aggregated loading across multiple SARIF files.
    /// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
    /// </summary>
    [Test]
    public void Load_MultipleSarifFiles_AggregatesToolsAndFindings()
    {
        var loader = new CodeAnalysisLoader(new SarifParser());

        var result = loader.Load([
            "TestData/code-analysis/checkov.sarif",
            "TestData/code-analysis/trivy.sarif",
            "TestData/code-analysis/tflint.sarif"
        ]);

        result.Model.Tools.Should().NotBeEmpty();
        result.Model.Findings.Should().NotBeEmpty();

        var toolNames = result.Model.Tools.Select(t => t.Name).ToArray();
        toolNames.Should().Contain(name => name.Contains("checkov", StringComparison.OrdinalIgnoreCase));
        toolNames.Should().Contain(name => name.Contains("trivy", StringComparison.OrdinalIgnoreCase));
        toolNames.Should().Contain(name => name.Contains("tflint", StringComparison.OrdinalIgnoreCase));
    }
}
