using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class SeverityMapperTests
{
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
