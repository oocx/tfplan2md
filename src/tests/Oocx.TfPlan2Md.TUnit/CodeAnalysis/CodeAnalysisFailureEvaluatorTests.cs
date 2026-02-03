using System.Collections.Generic;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for evaluating code analysis failure thresholds.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class CodeAnalysisFailureEvaluatorTests
{
    /// <summary>
    /// Verifies findings at or above the threshold are counted.
    /// </summary>
    [Test]
    public void CountFindingsAtOrAbove_CountsSeveritiesAtOrAboveThreshold()
    {
        var model = new CodeAnalysisModel
        {
            Tools = [],
            Findings = new List<CodeAnalysisFinding>
            {
                CreateFinding(9.1),
                CreateFinding(7.0),
                CreateFinding(4.0),
                CreateFinding(0.5)
            }
        };

        var count = CodeAnalysisFailureEvaluator.CountFindingsAtOrAbove(model, CodeAnalysisSeverity.High);

        count.Should().Be(2);
    }

    /// <summary>
    /// Verifies severity labels are formatted as expected for messaging.
    /// </summary>
    [Test]
    public void FormatSeverityLabel_FormatsExpectedLabels()
    {
        CodeAnalysisFailureEvaluator.FormatSeverityLabel(CodeAnalysisSeverity.Critical).Should().Be("critical");
        CodeAnalysisFailureEvaluator.FormatSeverityLabel(CodeAnalysisSeverity.High).Should().Be("high");
        CodeAnalysisFailureEvaluator.FormatSeverityLabel(CodeAnalysisSeverity.Medium).Should().Be("medium");
        CodeAnalysisFailureEvaluator.FormatSeverityLabel(CodeAnalysisSeverity.Low).Should().Be("low");
        CodeAnalysisFailureEvaluator.FormatSeverityLabel(CodeAnalysisSeverity.Informational).Should().Be("informational");
    }

    /// <summary>
    /// Creates a minimal finding with a security severity value.
    /// </summary>
    /// <param name="securitySeverity">The security severity value to set.</param>
    /// <returns>The constructed finding.</returns>
    private static CodeAnalysisFinding CreateFinding(double securitySeverity)
    {
        return new CodeAnalysisFinding
        {
            Message = "finding",
            SecuritySeverity = securitySeverity,
            Locations = []
        };
    }
}
