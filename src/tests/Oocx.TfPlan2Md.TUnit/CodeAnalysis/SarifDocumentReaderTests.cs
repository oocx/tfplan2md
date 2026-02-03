using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for SARIF document parsing.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class SarifDocumentReaderTests
{
    /// <summary>
    /// Verifies documents without runs return empty models.
    /// </summary>
    [Test]
    public void Parse_NoRuns_ReturnsEmptyModel()
    {
        using var document = JsonDocument.Parse("{\"version\":\"2.1.0\"}");

        var model = SarifDocumentReader.Parse(document.RootElement);

        model.Tools.Should().BeEmpty();
        model.Findings.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies runs are parsed into tools and findings.
    /// </summary>
    [Test]
    public void Parse_WithRuns_CollectsToolsAndFindings()
    {
        using var document = JsonDocument.Parse("""
        {
          "runs": [
            {
              "tool": {
                "driver": {
                  "name": "Checkov",
                  "semanticVersion": "3.2.10",
                  "rules": [
                    {
                      "id": "CKV_1",
                      "helpUri": "https://example.com/rule"
                    }
                  ]
                }
              },
              "results": [
                {
                  "ruleId": "CKV_1",
                  "level": "error",
                  "message": {
                    "text": "Bucket is public"
                  }
                }
              ]
            }
          ]
        }
        """);

        var model = SarifDocumentReader.Parse(document.RootElement);

        model.Tools.Should().ContainSingle();
        model.Tools[0].Name.Should().Be("Checkov");
        model.Tools[0].Version.Should().Be("3.2.10");
        model.Findings.Should().ContainSingle();
        model.Findings[0].RuleId.Should().Be("CKV_1");
        model.Findings[0].Message.Should().Be("Bucket is public");
    }
}
