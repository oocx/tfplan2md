using System.Text.Json;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for PrincipalMappingFile deserialization including Azure DevOps entity mappings.
/// Related feature: docs/features/085-azdo-principal-mapping/specification.md.
/// </summary>
public class PrincipalMappingFileTests
{
    /// <summary>
    /// TC-01: Verifies that the PrincipalMappingFile class correctly deserializes the azdoUsers JSON section.
    /// </summary>
    [Test]
    public void PrincipalMappingFile_DeserializeAzdoUsers_PopulatesProperty()
    {
        var json = """
        {
          "azdoUsers": {
            "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
            "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
          }
        }
        """;

        var mappingFile = JsonSerializer.Deserialize<PrincipalMappingFile>(json);

        mappingFile.Should().NotBeNull();
        mappingFile!.AzdoUsers.Should().NotBeNull();
        mappingFile.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")
            .WhoseValue.Should().Be("John Smith");
        mappingFile.AzdoUsers.Should().ContainKey("7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f")
            .WhoseValue.Should().Be("Alice Johnson");
        mappingFile.AzdoUsers.Count.Should().Be(2);
    }

    /// <summary>
    /// TC-02: Verifies that the PrincipalMappingFile class correctly deserializes all three azdo sections simultaneously.
    /// </summary>
    [Test]
    public void PrincipalMappingFile_DeserializeAllAzdoSections_PopulatesAllProperties()
    {
        var json = """
        {
          "azdoUsers": {
            "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith"
          },
          "azdoGroups": {
            "vssgp.Uy0xLTktMTU1MTM...": "Platform Team"
          },
          "azdoProjects": {
            "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project"
          }
        }
        """;

        var mappingFile = JsonSerializer.Deserialize<PrincipalMappingFile>(json);

        mappingFile.Should().NotBeNull();
        mappingFile!.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");
        mappingFile.AzdoGroups.Should().ContainKey("vssgp.Uy0xLTktMTU1MTM...");
        mappingFile.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f");
    }

    /// <summary>
    /// TC-03: Verifies that the PrincipalMappingFile class correctly deserializes the azdoRepositories JSON section.
    /// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
    /// </summary>
    [Test]
    public void PrincipalMappingFile_DeserializeAzdoRepositories_PopulatesProperty()
    {
        var json = """
        {
          "azdoRepositories": {
            "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo",
            "f9e8d7c6-b5a4-3210-fedc-ba9876543210": "Web Application Repo"
          }
        }
        """;

        var mappingFile = JsonSerializer.Deserialize<PrincipalMappingFile>(json);

        mappingFile.Should().NotBeNull();
        mappingFile!.AzdoRepositories.Should().NotBeNull();
        mappingFile.AzdoRepositories.Should().ContainKey("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d")
            .WhoseValue.Should().Be("Infrastructure Repo");
        mappingFile.AzdoRepositories.Should().ContainKey("f9e8d7c6-b5a4-3210-fedc-ba9876543210")
            .WhoseValue.Should().Be("Web Application Repo");
        mappingFile.AzdoRepositories.Count.Should().Be(2);
    }

    /// <summary>
    /// TC-04: Verifies that the PrincipalMappingFile class correctly deserializes all four azdo sections including repositories.
    /// Related feature: docs/features/096-azdo-repo-mapping-and-icons/specification.md.
    /// </summary>
    [Test]
    public void PrincipalMappingFile_DeserializeAllAzdoSectionsIncludingRepositories_PopulatesAllProperties()
    {
        var json = """
        {
          "azdoUsers": {
            "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith"
          },
          "azdoGroups": {
            "vssgp.Uy0xLTktMTU1MTM...": "Platform Team"
          },
          "azdoProjects": {
            "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project"
          },
          "azdoRepositories": {
            "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d": "Infrastructure Repo"
          }
        }
        """;

        var mappingFile = JsonSerializer.Deserialize<PrincipalMappingFile>(json);

        mappingFile.Should().NotBeNull();
        mappingFile!.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");
        mappingFile.AzdoGroups.Should().ContainKey("vssgp.Uy0xLTktMTU1MTM...");
        mappingFile.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f");
        mappingFile.AzdoRepositories.Should().ContainKey("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d");
    }
}
