using AwesomeAssertions;
using Oocx.TfPlan2Md.Azure;
using Oocx.TfPlan2Md.Diagnostics;
using TUnit.Core;

namespace Oocx.TfPlan2Md.TUnit.Azure;

/// <summary>
/// Integration tests for PrincipalMapper with diagnostic context.
/// Related feature: docs/features/038-debug-output/
/// </summary>
[Category("Integration")]
public class PrincipalMapperDiagnosticsTests
{
    /// <summary>
    /// TC-05: Successful principal mapping file load records diagnostics.
    /// </summary>
    [Test]
    public void PrincipalMapper_WithValidFile_RecordsSuccessfulLoad()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/principal-mapping.json";

        // Act
        _ = new PrincipalMapper(mappingFile, context);

        // Assert
        context.PrincipalMappingFileProvided.Should().BeTrue();
        context.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
        context.PrincipalMappingFilePath.Should().Be(mappingFile);
        context.PrincipalTypeCount.Should().ContainKey("principals");
        context.PrincipalTypeCount["principals"].Should().Be(3);
    }

    /// <summary>
    /// TC-06: Failed principal mapping file load records diagnostics.
    /// </summary>
    [Test]
    public void PrincipalMapper_WithMissingFile_RecordsFailedLoad()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/nonexistent.json";

        // Act
        _ = new PrincipalMapper(mappingFile, context);

        // Assert
        context.PrincipalMappingFileProvided.Should().BeTrue();
        context.PrincipalMappingLoadedSuccessfully.Should().BeFalse();
        context.PrincipalMappingFilePath.Should().Be(mappingFile);
        context.PrincipalTypeCount.Should().BeEmpty();
    }

    /// <summary>
    /// TC-07: Type counts match actual principals in mapping file.
    /// </summary>
    [Test]
    public void PrincipalMapper_TypeCounts_MatchActualData()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/principal-mapping.json";

        // Act
        _ = new PrincipalMapper(mappingFile, context);

        // Assert
        // The mapping file has 3 principals, so count should be 3
        context.PrincipalTypeCount["principals"].Should().Be(3);
    }

    /// <summary>
    /// TC-12: Failed principal resolutions record resource context.
    /// </summary>
    [Test]
    public void PrincipalMapper_FailedResolution_RecordsResourceContext()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/partial-principal-mapping.json";
        var mapper = new PrincipalMapper(mappingFile, context);
        var missingPrincipalId = "00000000-0000-0000-0000-000000000099";
        var resourceAddress = "azurerm_role_assignment.example";

        // Act
        var name = mapper.GetName(missingPrincipalId, principalType: null, resourceAddress);

        // Assert
        name.Should().BeNull(); // Principal not found
        context.FailedResolutions.Should().HaveCount(1);
        context.FailedResolutions[0].PrincipalId.Should().Be(missingPrincipalId);
        context.FailedResolutions[0].ResourceAddress.Should().Be(resourceAddress);
    }

    /// <summary>
    /// TC-13: PrincipalMapper works normally with null context (backward compatibility).
    /// </summary>
    [Test]
    public void PrincipalMapper_WithNullContext_WorksNormally()
    {
        // Arrange
        var mappingFile = "TestData/principal-mapping.json";

        // Act
        var mapper = new PrincipalMapper(mappingFile, diagnosticContext: null);
        var name = mapper.GetName("00000000-0000-0000-0000-000000000001");

        // Assert - Should work normally without diagnostic context
        name.Should().Be("Jane Doe");
    }

    /// <summary>
    /// TC-19: Same failed ID referenced by multiple resources records all references.
    /// </summary>
    [Test]
    public void PrincipalMapper_SameFailedIdMultipleResources_RecordsAllReferences()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/partial-principal-mapping.json";
        var mapper = new PrincipalMapper(mappingFile, context);
        var missingPrincipalId = "00000000-0000-0000-0000-000000000099";

        // Act - Same principal ID referenced by multiple resources
        mapper.GetName(missingPrincipalId, null, "azurerm_role_assignment.example");
        mapper.GetName(missingPrincipalId, null, "azurerm_role_assignment.reader");
        mapper.GetName(missingPrincipalId, null, "azurerm_role_assignment.contributor");

        // Assert - Should have 3 entries for the same principal ID
        context.FailedResolutions.Should().HaveCount(3);
        context.FailedResolutions.Should().AllSatisfy(f => f.PrincipalId.Should().Be(missingPrincipalId));
        context.FailedResolutions.Select(f => f.ResourceAddress).Should().BeEquivalentTo(
            ["azurerm_role_assignment.example", "azurerm_role_assignment.reader", "azurerm_role_assignment.contributor"]);
    }

    /// <summary>
    /// Test that successful resolution does not record in FailedResolutions.
    /// </summary>
    [Test]
    public void PrincipalMapper_SuccessfulResolution_DoesNotRecordFailure()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/principal-mapping.json";
        var mapper = new PrincipalMapper(mappingFile, context);

        // Act
        var name = mapper.GetName("00000000-0000-0000-0000-000000000001", null, "azurerm_role_assignment.example");

        // Assert
        name.Should().Be("Jane Doe");
        context.FailedResolutions.Should().BeEmpty();
    }

    /// <summary>
    /// Test that resolution without resource address doesn't record failure even when principal not found.
    /// </summary>
    [Test]
    public void PrincipalMapper_FailedResolutionWithoutResourceAddress_DoesNotRecord()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/partial-principal-mapping.json";
        var mapper = new PrincipalMapper(mappingFile, context);
        var missingPrincipalId = "00000000-0000-0000-0000-000000000099";

        // Act - No resource address provided (resourceAddress parameter is null)
        var name = mapper.GetName(missingPrincipalId, principalType: null, resourceAddress: null);

        // Assert
        name.Should().BeNull();
        context.FailedResolutions.Should().BeEmpty(); // Not recorded because no resource address
    }

    /// <summary>
    /// Test that no mapping file provided doesn't set diagnostic properties.
    /// </summary>
    [Test]
    public void PrincipalMapper_NoMappingFile_DoesNotSetDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext();

        // Act
        _ = new PrincipalMapper(mappingFile: null, context);

        // Assert
        context.PrincipalMappingFileProvided.Should().BeFalse();
        context.PrincipalMappingFilePath.Should().BeNull();
    }

    /// <summary>
    /// Test that empty string mapping file is treated same as null.
    /// </summary>
    [Test]
    public void PrincipalMapper_EmptyStringMappingFile_DoesNotSetDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext();

        // Act
        _ = new PrincipalMapper(mappingFile: string.Empty, context);

        // Assert
        context.PrincipalMappingFileProvided.Should().BeFalse();
        context.PrincipalMappingFilePath.Should().BeNull();
    }

    /// <summary>
    /// Test GetPrincipalName method with resource context.
    /// </summary>
    [Test]
    public void PrincipalMapper_GetPrincipalName_WithResourceContext_RecordsFailure()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/partial-principal-mapping.json";
        var mapper = new PrincipalMapper(mappingFile, context);
        var missingPrincipalId = "00000000-0000-0000-0000-000000000099";
        var resourceAddress = "azurerm_role_assignment.example";

        // Act
        var displayName = mapper.GetPrincipalName(missingPrincipalId, principalType: null, resourceAddress);

        // Assert
        displayName.Should().Be(missingPrincipalId); // Returns ID unchanged when not found
        context.FailedResolutions.Should().HaveCount(1);
        context.FailedResolutions[0].PrincipalId.Should().Be(missingPrincipalId);
    }

    /// <summary>
    /// Test that file not found error populates enhanced diagnostics.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    [Test]
    public void PrincipalMapper_FileNotFound_RecordsEnhancedDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/nonexistent.json";

        // Act
        _ = new PrincipalMapper(mappingFile, context);

        // Assert
        context.PrincipalMappingFileProvided.Should().BeTrue();
        context.PrincipalMappingLoadedSuccessfully.Should().BeFalse();
        context.PrincipalMappingFilePath.Should().Be(mappingFile);
        context.PrincipalMappingFileExists.Should().BeFalse();
        context.PrincipalMappingDirectoryExists.Should().BeTrue(); // TestData directory exists
        context.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.FileNotFound);
        context.PrincipalMappingErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test that directory not found error populates enhanced diagnostics.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    [Test]
    public void PrincipalMapper_DirectoryNotFound_RecordsEnhancedDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "/nonexistent/directory/principals.json";

        // Act
        _ = new PrincipalMapper(mappingFile, context);

        // Assert
        context.PrincipalMappingFileProvided.Should().BeTrue();
        context.PrincipalMappingLoadedSuccessfully.Should().BeFalse();
        context.PrincipalMappingFilePath.Should().Be(mappingFile);
        context.PrincipalMappingFileExists.Should().BeFalse();
        context.PrincipalMappingDirectoryExists.Should().BeFalse();
        context.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.DirectoryNotFound);
        context.PrincipalMappingErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test that JSON parse error populates enhanced diagnostics with line/column info.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    [Test]
    public void PrincipalMapper_JsonParseError_RecordsEnhancedDiagnosticsWithLineInfo()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/invalid-json-principal-mapping.json";

        // Create a temporary invalid JSON file for testing
        var invalidJson = "{ \"key1\": \"value1\", invalid }";
        Directory.CreateDirectory("TestData");
        File.WriteAllText(mappingFile, invalidJson);

        try
        {
            // Act
            _ = new PrincipalMapper(mappingFile, context);

            // Assert
            context.PrincipalMappingFileProvided.Should().BeTrue();
            context.PrincipalMappingLoadedSuccessfully.Should().BeFalse();
            context.PrincipalMappingFilePath.Should().Be(mappingFile);
            context.PrincipalMappingFileExists.Should().BeTrue();
            context.PrincipalMappingDirectoryExists.Should().BeTrue();
            context.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.JsonParseError);
            context.PrincipalMappingErrorMessage.Should().NotBeNullOrEmpty();
            context.PrincipalMappingErrorDetails.Should().NotBeNullOrEmpty();
        }
        finally
        {
            // Cleanup
            if (File.Exists(mappingFile))
            {
                File.Delete(mappingFile);
            }
        }
    }

    /// <summary>
    /// Test that empty file (valid JSON but no principals) populates enhanced diagnostics.
    /// Related to issue 042: Enhanced principal loading debug context.
    /// </summary>
    [Test]
    public void PrincipalMapper_EmptyJsonFile_RecordsEnhancedDiagnostics()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/empty-principal-mapping.json";

        // Create a temporary empty JSON file for testing
        var emptyJson = "{}";
        Directory.CreateDirectory("TestData");
        File.WriteAllText(mappingFile, emptyJson);

        try
        {
            // Act
            _ = new PrincipalMapper(mappingFile, context);

            // Assert
            context.PrincipalMappingFileProvided.Should().BeTrue();
            context.PrincipalMappingLoadedSuccessfully.Should().BeTrue(); // Empty is technically successful
            context.PrincipalMappingFilePath.Should().Be(mappingFile);
            context.PrincipalTypeCount["principals"].Should().Be(0); // But count is 0
        }
        finally
        {
            // Cleanup
            if (File.Exists(mappingFile))
            {
                File.Delete(mappingFile);
            }
        }
    }

    /// <summary>
    /// Test that nested format records principal type counts by category.
    /// </summary>
    [Test]
    public void PrincipalMapper_NestedFormat_RecordsTypeCounts()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/nested-principal-mapping.json";

        // Create a nested format JSON file
        var nestedJson = """
        {
          "users": {
            "user-1": "Jane Doe",
            "user-2": "John Smith"
          },
          "groups": {
            "group-1": "Platform Team",
            "group-2": "Security Team",
            "group-3": "DevOps Team"
          },
          "servicePrincipals": {
            "spn-1": "terraform-spn"
          }
        }
        """;
        Directory.CreateDirectory("TestData");
        File.WriteAllText(mappingFile, nestedJson);

        try
        {
            // Act
            _ = new PrincipalMapper(mappingFile, context);

            // Assert
            context.PrincipalMappingFileProvided.Should().BeTrue();
            context.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
            context.PrincipalMappingFilePath.Should().Be(mappingFile);
            context.PrincipalTypeCount["users"].Should().Be(2);
            context.PrincipalTypeCount["groups"].Should().Be(3);
            context.PrincipalTypeCount["servicePrincipals"].Should().Be(1);
        }
        finally
        {
            // Cleanup
            if (File.Exists(mappingFile))
            {
                File.Delete(mappingFile);
            }
        }
    }

    /// <summary>
    /// Test that nested format with only users section records only users count.
    /// </summary>
    [Test]
    public void PrincipalMapper_NestedFormatUsersOnly_RecordsOnlyUserCount()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/nested-users-only.json";

        // Create a nested format JSON file with only users
        var nestedJson = """
        {
          "users": {
            "user-1": "Jane Doe",
            "user-2": "John Smith"
          }
        }
        """;
        Directory.CreateDirectory("TestData");
        File.WriteAllText(mappingFile, nestedJson);

        try
        {
            // Act
            _ = new PrincipalMapper(mappingFile, context);

            // Assert
            context.PrincipalMappingFileProvided.Should().BeTrue();
            context.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
            context.PrincipalTypeCount["users"].Should().Be(2);
            context.PrincipalTypeCount.Should().NotContainKey("groups");
            context.PrincipalTypeCount.Should().NotContainKey("servicePrincipals");
        }
        finally
        {
            // Cleanup
            if (File.Exists(mappingFile))
            {
                File.Delete(mappingFile);
            }
        }
    }

    /// <summary>
    /// Test that flat format still works and records "principals" count (backward compatibility).
    /// </summary>
    [Test]
    public void PrincipalMapper_FlatFormat_RecordsPrincipalsCount()
    {
        // Arrange
        var context = new DiagnosticContext();
        var mappingFile = "TestData/flat-principal-mapping.json";

        // Create a flat format JSON file
        var flatJson = """
        {
          "00000000-0000-0000-0000-000000000001": "Jane Doe (User)",
          "00000000-0000-0000-0000-000000000002": "DevOps Team (Group)",
          "00000000-0000-0000-0000-000000000003": "terraform-spn (Service Principal)"
        }
        """;
        Directory.CreateDirectory("TestData");
        File.WriteAllText(mappingFile, flatJson);

        try
        {
            // Act
            _ = new PrincipalMapper(mappingFile, context);

            // Assert
            context.PrincipalMappingFileProvided.Should().BeTrue();
            context.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
            context.PrincipalTypeCount["principals"].Should().Be(3);
            context.PrincipalTypeCount.Should().NotContainKey("users");
            context.PrincipalTypeCount.Should().NotContainKey("groups");
            context.PrincipalTypeCount.Should().NotContainKey("servicePrincipals");
        }
        finally
        {
            // Cleanup
            if (File.Exists(mappingFile))
            {
                File.Delete(mappingFile);
            }
        }
    }
}
