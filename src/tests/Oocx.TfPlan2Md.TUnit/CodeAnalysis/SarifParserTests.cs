using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class SarifParserTests
{
    private readonly SarifParser _parser = new();

    [Test]
    public void ParseFile_ValidSarif_ReturnsModel()
    {
        // Arrange
        const string sarifPath = "TestData/valid-sarif.sarif";

        // Act
        var model = _parser.ParseFile(sarifPath);

        // Assert
        model.Tools.Should().ContainSingle();
        model.Tools[0].Name.Should().Be("Checkov");
        model.Tools[0].Version.Should().Be("3.2.10");

        model.Findings.Should().ContainSingle();
        var finding = model.Findings[0];
        finding.RuleId.Should().Be("CKV_AWS_1");
        finding.Message.Should().Be("Bucket is public");
        finding.Level.Should().Be("error");
        finding.SecuritySeverity.Should().Be(9.5);
        finding.HelpUri.Should().Be("https://example.com/rule");
        finding.ToolName.Should().Be("Checkov");
        finding.Locations.Should().ContainSingle();
        finding.Locations[0].FullyQualifiedName.Should().Be("aws_s3_bucket.example");
    }
}
