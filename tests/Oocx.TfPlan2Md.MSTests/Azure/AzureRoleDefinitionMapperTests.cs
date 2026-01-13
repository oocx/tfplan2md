using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;

namespace Oocx.TfPlan2Md.Tests.Azure;

[TestClass]
public class AzureRoleDefinitionMapperTests
{
    [TestMethod]
    public void GetRoleName_KnownGuid_ReturnsNameAndGuid()
    {
        // Arrange
        const string input = "/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7";

        // Act
        var result = AzureRoleDefinitionMapper.GetRoleName(input);

        // Assert
        result.Should().Be("Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)");
    }

    [TestMethod]
    public void GetRoleName_UnknownGuid_ReturnsOriginalId()
    {
        // Arrange
        const string input = "/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/unknown-guid";

        // Act
        var result = AzureRoleDefinitionMapper.GetRoleName(input);

        // Assert
        result.Should().Be(input);
    }

    [TestMethod]
    public void GetRoleName_BareGuid_MapsSuccessfully()
    {
        // Arrange
        const string input = "acdd72a7-3385-48ef-bd42-f606fba81ae7";

        // Act
        var result = AzureRoleDefinitionMapper.GetRoleName(input);

        // Assert
        result.Should().Be("Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)");
    }

    [TestMethod]
    public void GetRoleDefinition_KnownGuid_ReturnsStructuredInfo()
    {
        const string input = "/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7";

        var result = AzureRoleDefinitionMapper.GetRoleDefinition(input, null);

        result.Name.Should().Be("Reader");
        result.Id.Should().Be("acdd72a7-3385-48ef-bd42-f606fba81ae7");
        result.FullName.Should().Be("Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)");
    }

    [TestMethod]
    public void GetRoleDefinition_UnknownGuid_UsesId()
    {
        const string input = "/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/unknown-guid";

        var result = AzureRoleDefinitionMapper.GetRoleDefinition(input, null);

        result.Name.Should().Be("unknown-guid");
        result.Id.Should().Be("unknown-guid");
        result.FullName.Should().Be(input);
    }

    [TestMethod]
    public void GetRoleDefinition_FallsBackToRoleDefinitionName_WhenIdMissing()
    {
        var result = AzureRoleDefinitionMapper.GetRoleDefinition(null, "Custom Role");

        result.Name.Should().Be("Custom Role");
        result.Id.Should().Be(string.Empty);
        result.FullName.Should().Be("Custom Role");
    }
}
