using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.Parsing;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

public class ReportModelBuilderCodeAnalysisTests
{
    private readonly TerraformPlanParser _parser = new();

    [Test]
    public void Build_WithFindingsOnlyResource_AddsResourceToChanges()
    {
        // Arrange
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var finding = CreateFinding("aws_s3_bucket.example", securitySeverity: 9.1);
        var codeAnalysisInput = new CodeAnalysisInput
        {
            Model = new CodeAnalysisModel
            {
                Tools = [],
                Findings = [finding]
            },
            Warnings = [],
            MinimumLevel = null,
            FailOnLevel = null
        };

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);

        // Act
        var model = builder.Build(plan);

        // Assert
        model.Changes.Should().Contain(change => change.Address == "aws_s3_bucket.example");
        var injected = model.Changes.First(change => change.Address == "aws_s3_bucket.example");
        injected.CodeAnalysisFindings.Should().HaveCount(1);
    }

    [Test]
    public void Build_EffectiveMinimumLevel_IncludesFailOnFindings()
    {
        // Arrange
        var plan = _parser.Parse(File.ReadAllText("TestData/minimal-plan.json"));
        var highFinding = CreateFinding("null_resource.test", securitySeverity: 7.4);
        var lowFinding = CreateFinding("null_resource.test", securitySeverity: 1.2);

        var codeAnalysisInput = new CodeAnalysisInput
        {
            Model = new CodeAnalysisModel
            {
                Tools = [],
                Findings = [highFinding, lowFinding]
            },
            Warnings = [],
            MinimumLevel = CodeAnalysisSeverity.High,
            FailOnLevel = CodeAnalysisSeverity.Low
        };

        var builder = new ReportModelBuilder(codeAnalysisInput: codeAnalysisInput);

        // Act
        var model = builder.Build(plan);

        // Assert
        var resource = model.Changes.First(change => change.Address == "null_resource.test");
        resource.CodeAnalysisFindings.Should().HaveCount(2);
    }

    private static CodeAnalysisFinding CreateFinding(string address, double? securitySeverity = null)
    {
        return new CodeAnalysisFinding
        {
            Message = "Finding message",
            SecuritySeverity = securitySeverity,
            Locations =
            [
                new CodeAnalysisLocation { FullyQualifiedName = address }
            ]
        };
    }
}
