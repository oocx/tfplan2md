using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;

namespace Oocx.TfPlan2Md.Tests.Azure;

public class PrincipalMapperTests
{
    [Fact]
    public void GetPrincipalName_MappedId_ReturnsNameAndType()
    {
        // Arrange
        var mappingPath = CreateTempMapping("{\"abc-123\": \"John Doe (User)\"}");
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("abc-123");

            // Assert
            result.Should().Be("John Doe (User) [abc-123]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Fact]
    public void GetPrincipalName_UnmappedId_ReturnsOriginalId()
    {
        // Arrange
        var mappingPath = CreateTempMapping("{\"abc-123\": \"John Doe (User)\"}");
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("unmapped-id");

            // Assert
            result.Should().Be("unmapped-id");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Fact]
    public void GetPrincipalName_WithMalformedJson_DoesNotThrowAndReturnsOriginal()
    {
        // Arrange
        var mappingPath = CreateTempMapping("{ this-is-not-json }");
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("abc-123");

            // Assert
            result.Should().Be("abc-123");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    private static string CreateTempMapping(string content)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }
}
