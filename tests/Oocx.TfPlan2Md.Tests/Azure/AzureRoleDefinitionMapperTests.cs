using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;

namespace Oocx.TfPlan2Md.Tests.Azure;

public class AzureRoleDefinitionMapperTests
{
    [Fact]
    public void GetRoleName_KnownGuid_ReturnsNameAndGuid()
    {
        // Arrange
        const string input = "/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/acdd72a7-3385-48ef-bd42-f606fba81ae7";

        // Act
        var result = AzureRoleDefinitionMapper.GetRoleName(input);

        // Assert
        result.Should().Be("Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)");
    }

    [Fact]
    public void GetRoleName_UnknownGuid_ReturnsOriginalId()
    {
        // Arrange
        const string input = "/subscriptions/sub-id/providers/Microsoft.Authorization/roleDefinitions/unknown-guid";

        // Act
        var result = AzureRoleDefinitionMapper.GetRoleName(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void GetRoleName_BareGuid_MapsSuccessfully()
    {
        // Arrange
        const string input = "acdd72a7-3385-48ef-bd42-f606fba81ae7";

        // Act
        var result = AzureRoleDefinitionMapper.GetRoleName(input);

        // Assert
        result.Should().Be("Reader (acdd72a7-3385-48ef-bd42-f606fba81ae7)");
    }
}
