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
}
