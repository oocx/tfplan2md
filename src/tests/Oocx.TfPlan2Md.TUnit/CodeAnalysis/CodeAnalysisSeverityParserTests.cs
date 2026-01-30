using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for parsing CLI severity values.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class CodeAnalysisSeverityParserTests
{
    /// <summary>
    /// Verifies valid severity labels parse successfully.
    /// </summary>
    [Test]
    public void TryParse_ValidValues_ReturnsTrue()
    {
        CodeAnalysisSeverityParser.TryParse("critical", out var critical).Should().BeTrue();
        CodeAnalysisSeverityParser.TryParse("HIGH", out var high).Should().BeTrue();
        CodeAnalysisSeverityParser.TryParse("Medium", out var medium).Should().BeTrue();
        CodeAnalysisSeverityParser.TryParse("low", out var low).Should().BeTrue();
        CodeAnalysisSeverityParser.TryParse("info", out var info).Should().BeTrue();

        critical.Should().Be(CodeAnalysisSeverity.Critical);
        high.Should().Be(CodeAnalysisSeverity.High);
        medium.Should().Be(CodeAnalysisSeverity.Medium);
        low.Should().Be(CodeAnalysisSeverity.Low);
        info.Should().Be(CodeAnalysisSeverity.Informational);
    }

    /// <summary>
    /// Verifies invalid severity values fail parsing.
    /// </summary>
    [Test]
    public void TryParse_InvalidValue_ReturnsFalse()
    {
        CodeAnalysisSeverityParser.TryParse("unknown", out var severity).Should().BeFalse();
        severity.Should().Be(CodeAnalysisSeverity.Informational);
    }

    /// <summary>
    /// Verifies ParseOptional returns null for invalid values.
    /// </summary>
    [Test]
    public void ParseOptional_InvalidValue_ReturnsNull()
    {
        var result = CodeAnalysisSeverityParser.ParseOptional("bad");

        result.Should().BeNull();
    }
}
