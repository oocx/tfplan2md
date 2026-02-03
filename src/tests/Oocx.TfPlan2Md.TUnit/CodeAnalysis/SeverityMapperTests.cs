using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class SeverityMapperTests
{
    /// <summary>
    /// Verifies rank values map to severity buckets.
    /// </summary>
    [Test]
    public void DeriveSeverity_Rank_MapsToSeverityBuckets()
    {
        var criticalFinding = CreateFinding(rank: 5);
        var highFinding = CreateFinding(rank: 25);
        var mediumFinding = CreateFinding(rank: 55);
        var lowFinding = CreateFinding(rank: 85);
        var infoFinding = CreateFinding(rank: 95);

        SeverityMapper.DeriveSeverity(criticalFinding).Should().Be(CodeAnalysisSeverity.Critical);
        SeverityMapper.DeriveSeverity(highFinding).Should().Be(CodeAnalysisSeverity.High);
        SeverityMapper.DeriveSeverity(mediumFinding).Should().Be(CodeAnalysisSeverity.Medium);
        SeverityMapper.DeriveSeverity(lowFinding).Should().Be(CodeAnalysisSeverity.Low);
        SeverityMapper.DeriveSeverity(infoFinding).Should().Be(CodeAnalysisSeverity.Informational);
    }

    /// <summary>
    /// Verifies invalid rank values default to informational severity.
    /// </summary>
    [Test]
    public void DeriveSeverity_RankNaNOrInfinity_ReturnsInformational()
    {
        var nanFinding = CreateFinding(rank: double.NaN);
        var infinityFinding = CreateFinding(rank: double.PositiveInfinity);

        SeverityMapper.DeriveSeverity(nanFinding).Should().Be(CodeAnalysisSeverity.Informational);
        SeverityMapper.DeriveSeverity(infinityFinding).Should().Be(CodeAnalysisSeverity.Informational);
    }

    [Test]
    public void DeriveSeverity_SecuritySeverity_UsesThresholds()
    {
        // Arrange
        var criticalFinding = CreateFinding(securitySeverity: 9.5);
        var highFinding = CreateFinding(securitySeverity: 7.1);
        var mediumFinding = CreateFinding(securitySeverity: 4.2);
        var lowFinding = CreateFinding(securitySeverity: 1.0);
        var infoFinding = CreateFinding(securitySeverity: 0.5);

        // Act
        var critical = SeverityMapper.DeriveSeverity(criticalFinding);
        var high = SeverityMapper.DeriveSeverity(highFinding);
        var medium = SeverityMapper.DeriveSeverity(mediumFinding);
        var low = SeverityMapper.DeriveSeverity(lowFinding);
        var info = SeverityMapper.DeriveSeverity(infoFinding);

        // Assert
        critical.Should().Be(CodeAnalysisSeverity.Critical);
        high.Should().Be(CodeAnalysisSeverity.High);
        medium.Should().Be(CodeAnalysisSeverity.Medium);
        low.Should().Be(CodeAnalysisSeverity.Low);
        info.Should().Be(CodeAnalysisSeverity.Informational);
    }

    [Test]
    public void DeriveSeverity_LevelFallback_UsesSarifLevel()
    {
        // Arrange
        var errorFinding = CreateFinding(level: "error");
        var warningFinding = CreateFinding(level: "warning");
        var noteFinding = CreateFinding(level: "note");
        var noneFinding = CreateFinding(level: "none");

        // Act
        var errorSeverity = SeverityMapper.DeriveSeverity(errorFinding);
        var warningSeverity = SeverityMapper.DeriveSeverity(warningFinding);
        var noteSeverity = SeverityMapper.DeriveSeverity(noteFinding);
        var noneSeverity = SeverityMapper.DeriveSeverity(noneFinding);

        // Assert
        errorSeverity.Should().Be(CodeAnalysisSeverity.High);
        warningSeverity.Should().Be(CodeAnalysisSeverity.Medium);
        noteSeverity.Should().Be(CodeAnalysisSeverity.Low);
        noneSeverity.Should().Be(CodeAnalysisSeverity.Informational);
    }

    private static CodeAnalysisFinding CreateFinding(double? securitySeverity = null, double? rank = null, string? level = null)
    {
        return new CodeAnalysisFinding
        {
            Message = "Finding message",
            SecuritySeverity = securitySeverity,
            Rank = rank,
            Level = level,
            Locations = []
        };
    }
}
