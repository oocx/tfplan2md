using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class ResourceMapperTests
{
    [Test]
    public void TryMapLogicalLocation_StandardAndModuleAddress_ReturnsResourceAddress()
    {
        // Arrange
        const string standardLocation = "aws_s3_bucket.example";
        const string moduleLocation = "module.vpc.aws_vpc.main";

        // Act
        var standardMapped = ResourceMapper.TryMapLogicalLocation(standardLocation, out var standard);
        var moduleMapped = ResourceMapper.TryMapLogicalLocation(moduleLocation, out var module);

        // Assert
        standardMapped.Should().BeTrue();
        standard.ResourceAddress.Should().Be("aws_s3_bucket.example");
        standard.AttributePath.Should().BeNull();
        standard.ModuleAddress.Should().BeNull();

        moduleMapped.Should().BeTrue();
        module.ResourceAddress.Should().Be("module.vpc.aws_vpc.main");
        module.ModuleAddress.Should().Be("module.vpc");
    }

    [Test]
    public void TryMapLogicalLocation_AttributePath_ReturnsAttributePath()
    {
        // Arrange
        const string location = "aws_s3_bucket.example.versioning.enabled";

        // Act
        var mapped = ResourceMapper.TryMapLogicalLocation(location, out var result);

        // Assert
        mapped.Should().BeTrue();
        result.ResourceAddress.Should().Be("aws_s3_bucket.example");
        result.AttributePath.Should().Be("versioning.enabled");
    }

    [Test]
    public void TryMapLogicalLocation_ModuleOnly_ReturnsModuleAddress()
    {
        // Arrange
        const string location = "module.network.module.subnet";

        // Act
        var mapped = ResourceMapper.MapFinding(new CodeAnalysisFinding
        {
            Message = "Finding message",
            Locations = [new CodeAnalysisLocation { FullyQualifiedName = location }]
        }, CodeAnalysisSeverity.Medium);

        // Assert
        mapped.Should().ContainSingle();
        mapped[0].ResourceAddress.Should().BeNull();
        mapped[0].ModuleAddress.Should().Be(location);
    }

    [Test]
    public void MapFinding_MultipleLocations_CreatesMultipleFindings()
    {
        // Arrange
        var finding = new CodeAnalysisFinding
        {
            Message = "Finding message",
            Locations =
            [
                new CodeAnalysisLocation { FullyQualifiedName = "aws_s3_bucket.example" },
                new CodeAnalysisLocation { FullyQualifiedName = "module.vpc.aws_vpc.main" }
            ]
        };

        // Act
        var mapped = ResourceMapper.MapFinding(finding, CodeAnalysisSeverity.High);

        // Assert
        mapped.Should().HaveCount(2);
        mapped[0].ResourceAddress.Should().Be("aws_s3_bucket.example");
        mapped[1].ResourceAddress.Should().Be("module.vpc.aws_vpc.main");
    }
}
