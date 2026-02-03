using System;
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.Tests.Azure;

public class PrincipalMapperTests
{
    [Test]
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

    [Test]
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

    [Test]
    public void GetPrincipalName_WithMalformedJson_LogsWarningAndReturnsOriginal()
    {
        // Arrange
        var mappingPath = CreateTempMapping("{ this-is-not-json }");
        try
        {
            var originalError = Console.Error;
            using var capture = new StringWriter();
            Console.SetError(capture);

            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("abc-123");

            // Assert
            result.Should().Be("abc-123");

            var stderr = capture.ToString();
            stderr.Should().Contain("Could not read principal mapping file")
                .And.Contain(mappingPath);

            Console.SetError(originalError);
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void GetPrincipalName_NestedFormatUsers_ReturnsNameAndType()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "users": {
            "user-123": "jane.doe@contoso.com"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("user-123");

            // Assert
            result.Should().Be("jane.doe@contoso.com [user-123]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void GetPrincipalName_NestedFormatGroups_ReturnsNameAndType()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "groups": {
            "group-456": "Platform Team"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("group-456");

            // Assert
            result.Should().Be("Platform Team [group-456]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void GetPrincipalName_NestedFormatServicePrincipals_ReturnsNameAndType()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "servicePrincipals": {
            "spn-789": "terraform-spn"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("spn-789");

            // Assert
            result.Should().Be("terraform-spn [spn-789]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void GetPrincipalName_NestedFormatAllSections_ReturnsCorrectNames()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "users": {
            "12345678-1234-1234-1234-123456789012": "jane.doe@contoso.com",
            "87654321-4321-4321-4321-210987654321": "john.smith@contoso.com"
          },
          "groups": {
            "abcdef12-3456-7890-abcd-ef1234567890": "Platform Team",
            "fedcba98-7654-3210-fedc-ba9876543210": "Security Team"
          },
          "servicePrincipals": {
            "11111111-2222-3333-4444-555555555555": "terraform-spn",
            "66666666-7777-8888-9999-000000000000": "github-actions-spn"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act & Assert
            mapper.GetPrincipalName("12345678-1234-1234-1234-123456789012")
                .Should().Be("jane.doe@contoso.com [12345678-1234-1234-1234-123456789012]");

            mapper.GetPrincipalName("abcdef12-3456-7890-abcd-ef1234567890")
                .Should().Be("Platform Team [abcdef12-3456-7890-abcd-ef1234567890]");

            mapper.GetPrincipalName("11111111-2222-3333-4444-555555555555")
                .Should().Be("terraform-spn [11111111-2222-3333-4444-555555555555]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void GetPrincipalName_NestedFormatCaseInsensitive_ReturnsName()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "users": {
            "ABC-123": "Jane Doe"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var result = mapper.GetPrincipalName("abc-123"); // lowercase query

            // Assert
            result.Should().Be("Jane Doe [abc-123]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void GetPrincipalName_FlatFormatBackwardCompatibility_ReturnsName()
    {
        // Arrange - Old flat format should still work
        var mappingPath = CreateTempMapping("""
        {
          "00000000-0000-0000-0000-000000000001": "Jane Doe (User)",
          "00000000-0000-0000-0000-000000000002": "DevOps Team (Group)"
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act & Assert
            mapper.GetPrincipalName("00000000-0000-0000-0000-000000000001")
                .Should().Be("Jane Doe (User) [00000000-0000-0000-0000-000000000001]");

            mapper.GetPrincipalName("00000000-0000-0000-0000-000000000002")
                .Should().Be("DevOps Team (Group) [00000000-0000-0000-0000-000000000002]");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void TryGetPrincipalType_NestedFormat_ReturnsType()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "users": {
            "user-123": "jane.doe@contoso.com"
          },
          "groups": {
            "group-456": "Platform Team"
          },
          "servicePrincipals": {
            "spn-789": "terraform-spn"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var userFound = mapper.TryGetPrincipalType("user-123", out var userType);
            var groupFound = mapper.TryGetPrincipalType("group-456", out var groupType);
            var spnFound = mapper.TryGetPrincipalType("spn-789", out var spnType);

            // Assert
            userFound.Should().BeTrue();
            userType.Should().Be("User");
            groupFound.Should().BeTrue();
            groupType.Should().Be("Group");
            spnFound.Should().BeTrue();
            spnType.Should().Be("ServicePrincipal");
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void TryGetPrincipalType_FlatFormat_ReturnsFalse()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "00000000-0000-0000-0000-000000000001": "Jane Doe (User)"
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var found = mapper.TryGetPrincipalType("00000000-0000-0000-0000-000000000001", out var principalType);

            // Assert
            found.Should().BeFalse();
            principalType.Should().BeNull();
        }
        finally
        {
            File.Delete(mappingPath);
        }
    }

    [Test]
    public void TryGetPrincipalType_EmptyId_ReturnsFalse()
    {
        // Arrange
        var mappingPath = CreateTempMapping("""
        {
          "users": {
            "user-123": "jane.doe@contoso.com"
          }
        }
        """);
        try
        {
            var mapper = new PrincipalMapper(mappingPath);

            // Act
            var found = mapper.TryGetPrincipalType(" ", out var principalType);

            // Assert
            found.Should().BeFalse();
            principalType.Should().BeNull();
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
