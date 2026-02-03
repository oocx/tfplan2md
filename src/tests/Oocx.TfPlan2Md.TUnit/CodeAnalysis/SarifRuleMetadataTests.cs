using System.Collections.Generic;
using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for SARIF rule metadata parsing.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class SarifRuleMetadataTests
{
    /// <summary>
    /// Verifies missing rules return an empty list.
    /// </summary>
    [Test]
    public void ParseRules_MissingRules_ReturnsEmpty()
    {
        using var document = JsonDocument.Parse("{\"name\":\"tool\"}");
        var rules = SarifRuleMetadata.ParseRules(document.RootElement);

        rules.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies help index ignores rules without identifiers and preserves first entries.
    /// </summary>
    [Test]
    public void BuildHelpIndex_IgnoresMissingIdsAndDuplicates()
    {
        var rules = new List<SarifRuleMetadata>
        {
            new("", "https://ignored"),
            new("CKV_1", "https://first"),
            new("CKV_1", "https://second")
        };

        var lookup = SarifRuleMetadata.BuildHelpIndex(rules);

        lookup.Should().ContainKey("CKV_1");
        lookup["CKV_1"].Should().Be("https://first");
    }
}
