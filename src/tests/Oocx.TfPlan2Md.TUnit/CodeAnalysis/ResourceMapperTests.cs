using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

public class ResourceMapperTests
{
    private const string FindingMessage = "Finding message";
    private const string StandardLocation = "aws_s3_bucket.example";
    private const string ModuleLocation = "module.vpc.aws_vpc.main";

    /// <summary>
    /// Verifies findings without locations are treated as unmapped.
    /// </summary>
    [Test]
    public void MapFinding_NoLocations_ReturnsUnmapped()
    {
        var finding = new CodeAnalysisFinding
        {
            Message = FindingMessage,
            Locations = []
        };

        var mapped = ResourceMapper.MapFinding(finding, CodeAnalysisSeverity.Low);

        mapped.Should().ContainSingle();
        mapped[0].ResourceAddress.Should().BeNull();
        mapped[0].ModuleAddress.Should().BeNull();
    }

    /// <summary>
    /// Verifies null location names are treated as unmapped.
    /// </summary>
    [Test]
    public void MapFinding_NullLocation_ReturnsUnmapped()
    {
        var finding = new CodeAnalysisFinding
        {
            Message = FindingMessage,
            Locations = [new CodeAnalysisLocation { FullyQualifiedName = null }]
        };

        var mapped = ResourceMapper.MapFinding(finding, CodeAnalysisSeverity.Low);

        mapped.Should().ContainSingle();
        mapped[0].ResourceAddress.Should().BeNull();
        mapped[0].ModuleAddress.Should().BeNull();
    }

    /// <summary>
    /// Verifies invalid or whitespace logical locations return false.
    /// </summary>
    [Test]
    public void TryMapLogicalLocation_InvalidValue_ReturnsFalse()
    {
        ResourceMapper.TryMapLogicalLocation(" ", out _).Should().BeFalse();
        ResourceMapper.TryMapLogicalLocation("invalid", out _).Should().BeFalse();
    }

    /// <summary>
    /// Verifies module-only tokens that lack resources do not map as resource locations.
    /// </summary>
    [Test]
    public void TryMapLogicalLocation_ModuleWithoutResource_ReturnsFalse()
    {
        ResourceMapper.TryMapLogicalLocation("module.only", out _).Should().BeFalse();
    }

    [Test]
    public void TryMapLogicalLocation_StandardAndModuleAddress_ReturnsResourceAddress()
    {
        // Arrange
        // Act
        var standardMapped = ResourceMapper.TryMapLogicalLocation(StandardLocation, out var standard);
        var moduleMapped = ResourceMapper.TryMapLogicalLocation(ModuleLocation, out var module);

        // Assert
        standardMapped.Should().BeTrue();
        standard.ResourceAddress.Should().Be(StandardLocation);
        standard.AttributePath.Should().BeNull();
        standard.ModuleAddress.Should().BeNull();

        moduleMapped.Should().BeTrue();
        module.ResourceAddress.Should().Be(ModuleLocation);
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
        result.ResourceAddress.Should().Be(StandardLocation);
        result.AttributePath.Should().Be("versioning.enabled");
    }

    /// <summary>
    /// Verifies data source addresses are mapped with the data prefix intact.
    /// </summary>
    [Test]
    public void TryMapLogicalLocation_DataSource_ReturnsResourceAddress()
    {
        const string location = "data.azurerm_subscription.current";

        var mapped = ResourceMapper.TryMapLogicalLocation(location, out var result);

        mapped.Should().BeTrue();
        result.ResourceAddress.Should().Be("data.azurerm_subscription.current");
        result.ModuleAddress.Should().BeNull();
    }

    /// <summary>
    /// Verifies module-prefixed data sources are mapped with the module address retained.
    /// </summary>
    [Test]
    public void TryMapLogicalLocation_ModuleDataSource_ReturnsResourceAddress()
    {
        const string location = "module.network.data.azurerm_subscription.current";

        var mapped = ResourceMapper.TryMapLogicalLocation(location, out var result);

        mapped.Should().BeTrue();
        result.ResourceAddress.Should().Be("module.network.data.azurerm_subscription.current");
        result.ModuleAddress.Should().Be("module.network");
    }

    [Test]
    public void TryMapLogicalLocation_ModuleOnly_ReturnsModuleAddress()
    {
        // Arrange
        const string location = "module.network.module.subnet";

        // Act
        var mapped = ResourceMapper.MapFinding(new CodeAnalysisFinding
        {
            Message = FindingMessage,
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
            Message = FindingMessage,
            Locations =
            [
                new CodeAnalysisLocation { FullyQualifiedName = StandardLocation },
                new CodeAnalysisLocation { FullyQualifiedName = ModuleLocation }
            ]
        };

        // Act
        var mapped = ResourceMapper.MapFinding(finding, CodeAnalysisSeverity.High);

        // Assert
        mapped.Should().HaveCount(2);
        mapped[0].ResourceAddress.Should().Be(StandardLocation);
        mapped[1].ResourceAddress.Should().Be(ModuleLocation);
    }
}
