using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for parsing SARIF result entries into findings.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class SarifResultReaderTests
{
    /// <summary>
    /// Verifies rule metadata fallbacks and markdown message parsing.
    /// </summary>
    [Test]
    public void ParseFinding_UsesRuleIndexAndRuleHelpUri()
    {
        using var document = JsonDocument.Parse("""
        {
          "ruleIndex": 0,
          "message": {
            "markdown": "*Finding*"
          },
          "properties": {
            "security-severity": "7.2"
          },
          "locations": [
            {
              "logicalLocations": [
                {
                  "fullyQualifiedName": "aws_s3_bucket.example"
                }
              ]
            }
          ]
        }
        """);

        var rules = new List<SarifRuleMetadata>
        {
            new("CKV_1", "https://example.com/rule")
        };
        var helpByRuleId = SarifRuleMetadata.BuildHelpIndex(rules);

        var finding = SarifResultReader.ParseFinding(document.RootElement, "Checkov", rules, helpByRuleId);

        finding.RuleId.Should().Be("CKV_1");
        finding.HelpUri.Should().Be("https://example.com/rule");
        finding.Message.Should().Be("*Finding*");
        finding.SecuritySeverity.Should().Be(7.2);
        finding.Locations.Should().ContainSingle();
        finding.Locations[0].FullyQualifiedName.Should().Be("aws_s3_bucket.example");
    }

    /// <summary>
    /// Verifies invalid locations and missing message text are handled safely.
    /// </summary>
    [Test]
    public void ParseFinding_SkipsInvalidLocationsAndHandlesMissingMessage()
    {
        using var document = JsonDocument.Parse("""
        {
          "ruleId": "CKV_2",
          "message": {},
          "locations": [
            {
              "physicalLocation": {}
            },
            {
              "logicalLocations": [
                {
                  "fullyQualifiedName": "  "
                },
                {
                  "fullyQualifiedName": "module.vpc.aws_vpc.main"
                }
              ]
            }
          ]
        }
        """);

        var finding = SarifResultReader.ParseFinding(document.RootElement, "Checkov", [], new Dictionary<string, string?>());

        finding.Message.Should().BeEmpty();
        finding.Locations.Should().ContainSingle();
        finding.Locations[0].FullyQualifiedName.Should().Be("module.vpc.aws_vpc.main");
    }

    /// <summary>
    /// Verifies logical location name values are used when fully qualified names are missing.
    /// </summary>
    [Test]
    public void ParseFinding_UsesLogicalLocationNameWhenFullyQualifiedNameMissing()
    {
        using var document = JsonDocument.Parse("""
        {
          "ruleId": "CKV_3",
          "message": {
            "text": "Finding message"
          },
          "locations": [
            {
              "logicalLocations": [
                {
                  "name": "aws_s3_bucket.example"
                }
              ]
            }
          ]
        }
        """);

        var finding = SarifResultReader.ParseFinding(document.RootElement, "Checkov", [], new Dictionary<string, string?>());

        finding.Locations.Should().ContainSingle();
        finding.Locations[0].FullyQualifiedName.Should().Be("aws_s3_bucket.example");
    }
}
