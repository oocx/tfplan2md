using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for parsing SARIF run entries.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class SarifRunReaderTests
{
    /// <summary>
    /// Verifies runs without tool drivers are ignored.
    /// </summary>
    [Test]
    public void ParseRun_MissingDriver_IgnoresRun()
    {
        using var document = JsonDocument.Parse("{\"tool\":{},\"results\":[{}]}");
        var tools = new List<CodeAnalysisTool>();
        var findings = new List<CodeAnalysisFinding>();

        SarifRunReader.ParseRun(document.RootElement, tools, findings);

        tools.Should().BeEmpty();
        findings.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies tools are captured even when results are missing.
    /// </summary>
    [Test]
    public void ParseRun_NoResults_AddsToolOnly()
    {
        using var document = JsonDocument.Parse("""
        {
          "tool": {
            "driver": {
              "name": "Checkov",
              "version": "3.2.10"
            }
          }
        }
        """);
        var tools = new List<CodeAnalysisTool>();
        var findings = new List<CodeAnalysisFinding>();

        SarifRunReader.ParseRun(document.RootElement, tools, findings);

        tools.Should().ContainSingle();
        tools[0].Name.Should().Be("Checkov");
        tools[0].Version.Should().Be("3.2.10");
        findings.Should().BeEmpty();
    }
}
