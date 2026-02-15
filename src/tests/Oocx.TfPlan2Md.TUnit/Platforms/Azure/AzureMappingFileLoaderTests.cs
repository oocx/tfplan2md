using System;
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for loading Azure mapping files with extended sections.
/// Related feature: docs/features/063-azure-display-enhancements/specification.md.
/// </summary>
public class AzureMappingFileLoaderTests
{
    /// <summary>
    /// TC-16: Verifies array-of-objects sections parse and diagnostics capture counts.
    /// </summary>
    [Test]
    public void Load_NestedFormatWithExtendedSections_ParsesMappings()
    {
        var filePath = GetTempPath($"extended-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "users": {
            "user-1": "user@example.com"
          },
          "groups": {
            "group-1": "Platform Team"
          },
          "servicePrincipals": {
            "sp-1": "terraform-spn"
          },
          "subscriptions": [
            { "id": "sub-1", "displayName": "Production" }
          ],
          "managementGroups": [
            { "id": "mg-1", "displayName": "Cloud" }
          ],
          "tenants": [
            { "id": "tenant-1", "displayName": "Contoso" }
          ],
          "roles": [
            { "id": "role-1", "displayName": "Custom Role" }
          ]
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            result.Principals.Should().ContainKey("user-1").WhoseValue.Should().Be("user@example.com");
            result.PrincipalTypes.Should().ContainKey("group-1").WhoseValue.Should().Be("Group");
            result.Subscriptions.Should().ContainSingle(entry => entry.Id == "sub-1" && entry.DisplayName == "Production");
            result.ManagementGroups.Should().ContainSingle(entry => entry.Id == "mg-1" && entry.DisplayName == "Cloud");
            result.Tenants.Should().ContainSingle(entry => entry.Id == "tenant-1" && entry.DisplayName == "Contoso");
            result.Roles.Should().ContainSingle(entry => entry.Id == "role-1" && entry.DisplayName == "Custom Role");

            diagnostics.SubscriptionCount.Should().Be(1);
            diagnostics.ManagementGroupCount.Should().Be(1);
            diagnostics.TenantCount.Should().Be(1);
            diagnostics.RoleCount.Should().Be(1);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-15: Verifies flat mapping files still load for principals without extended sections.
    /// </summary>
    [Test]
    public void Load_FlatFormat_PrincipalsOnly_LoadsSuccessfully()
    {
        var filePath = GetTempPath($"flat-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "principal-1": "flat-user"
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            result.Principals.Should().ContainKey("principal-1").WhoseValue.Should().Be("flat-user");
            result.PrincipalTypes.Should().BeEmpty();
            result.Subscriptions.Should().BeEmpty();
            result.ManagementGroups.Should().BeEmpty();
            result.Tenants.Should().BeEmpty();
            result.Roles.Should().BeEmpty();

            diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
            diagnostics.PrincipalTypeCount.Should().ContainKey("principals").WhoseValue.Should().Be(1);
            diagnostics.SubscriptionCount.Should().Be(0);
            diagnostics.ManagementGroupCount.Should().Be(0);
            diagnostics.TenantCount.Should().Be(0);
            diagnostics.RoleCount.Should().Be(0);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-03: Verifies that the parser correctly parses the azdoUsers section.
    /// </summary>
    [Test]
    public void Load_AzdoUsersSection_ParsesCorrectly()
    {
        var filePath = GetTempPath($"azdo-users-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "azdoUsers": {
            "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "John Smith",
            "7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f": "Alice Johnson"
          }
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            result.AzdoUsers.Should().NotBeNull();
            result.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")
                .WhoseValue.Should().Be("John Smith");
            result.AzdoUsers.Should().ContainKey("7f8e9d0c-1b2a-3c4d-5e6f-7a8b9c0d1e2f")
                .WhoseValue.Should().Be("Alice Johnson");
            result.AzdoUsers.Count.Should().Be(2);

            diagnostics.AzdoUserCount.Should().Be(2);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-04: Verifies that the parser preserves long group descriptors without truncation.
    /// </summary>
    [Test]
    public void Load_AzdoGroupsSection_PreservesLongDescriptors()
    {
        var filePath = GetTempPath($"azdo-groups-{Guid.NewGuid():N}.json");
        var longDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yNzY5MzQwNjk3LTExMDE5ODM1NjMtMzU0Nzk5MjM2MS0zNzAyMTIxNjI4LTEtMTIzNDU2Nzg5MC0xMjM0NTY3ODkwLTEyMzQ1Njc4OTAtMTIzNDU2Nzg5MA";
        File.WriteAllText(filePath, $$"""
        {
          "azdoGroups": {
            "{{longDescriptor}}": "Platform Team",
            "vssgp.Short": "Security Team"
          }
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            result.AzdoGroups.Should().ContainKey(longDescriptor)
                .WhoseValue.Should().Be("Platform Team");
            result.AzdoGroups.Keys.Should().Contain(key => key.Length > 100);
            diagnostics.AzdoGroupCount.Should().Be(2);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-05: Verifies that the parser correctly parses the azdoProjects section.
    /// </summary>
    [Test]
    public void Load_AzdoProjectsSection_ParsesCorrectly()
    {
        var filePath = GetTempPath($"azdo-projects-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "azdoProjects": {
            "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Infrastructure Project",
            "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d": "Application Platform"
          }
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            result.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f")
                .WhoseValue.Should().Be("Infrastructure Project");
            result.AzdoProjects.Count.Should().Be(2);
            diagnostics.AzdoProjectCount.Should().Be(2);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-06: Verifies that Azure AD and Azure DevOps mappings are kept separate.
    /// </summary>
    [Test]
    public void Load_MixedAzureAndAzdoSections_ParsesBothCorrectly()
    {
        var filePath = GetTempPath($"mixed-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "users": {
            "00000000-0000-0000-0000-000000000001": "Azure AD User"
          },
          "groups": {
            "00000000-0000-0000-0000-000000000002": "Azure AD Group"
          },
          "azdoUsers": {
            "4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b": "Azure DevOps User"
          },
          "azdoGroups": {
            "vssgp.Uy0xLTktMTU1MTM...": "Azure DevOps Group"
          },
          "azdoProjects": {
            "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f": "Azure DevOps Project"
          }
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            // Azure AD principals in Principals dictionary
            result.Principals.Should().ContainKey("00000000-0000-0000-0000-000000000001")
                .WhoseValue.Should().Be("Azure AD User");
            result.Principals.Should().ContainKey("00000000-0000-0000-0000-000000000002")
                .WhoseValue.Should().Be("Azure AD Group");

            // Azure DevOps entities in separate dictionaries
            result.AzdoUsers.Should().ContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b")
                .WhoseValue.Should().Be("Azure DevOps User");
            result.AzdoGroups.Should().ContainKey("vssgp.Uy0xLTktMTU1MTM...")
                .WhoseValue.Should().Be("Azure DevOps Group");
            result.AzdoProjects.Should().ContainKey("8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f")
                .WhoseValue.Should().Be("Azure DevOps Project");

            // Verify no cross-contamination
            result.Principals.Should().NotContainKey("4a2c5e2b-3b4f-4e6f-8a9b-1c2d3e4f5a6b");
            result.AzdoUsers.Should().NotContainKey("00000000-0000-0000-0000-000000000001");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-07: Verifies that null azdo sections are handled gracefully.
    /// </summary>
    [Test]
    public void Load_NullAzdoSections_HandlesGracefully()
    {
        var filePath = GetTempPath($"null-azdo-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "users": {
            "user-1": "Test User"
          },
          "azdoUsers": null,
          "azdoGroups": null,
          "azdoProjects": null
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            result.Principals.Should().ContainKey("user-1");
            result.AzdoUsers.Should().BeEmpty();
            result.AzdoGroups.Should().BeEmpty();
            result.AzdoProjects.Should().BeEmpty();

            diagnostics.AzdoUserCount.Should().Be(0);
            diagnostics.AzdoGroupCount.Should().Be(0);
            diagnostics.AzdoProjectCount.Should().Be(0);
            diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// TC-08: Verifies backwards compatibility - existing mapping files without azdo sections continue to work.
    /// </summary>
    [Test]
    public void Load_LegacyFileWithoutAzdoSections_WorksAsExpected()
    {
        // Create a legacy-format file without azdo sections
        var filePath = GetTempPath($"legacy-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "users": {
            "00000000-0000-0000-0000-000000000001": "Jane Doe"
          },
          "groups": {
            "00000000-0000-0000-0000-000000000002": "DevOps Team"
          }
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var result = AzureMappingFileLoader.Load(filePath, diagnostics);

            // Azure AD mappings work
            result.Principals.Should().NotBeEmpty();

            // Azdo dictionaries are empty but not null
            result.AzdoUsers.Should().NotBeNull().And.BeEmpty();
            result.AzdoGroups.Should().NotBeNull().And.BeEmpty();
            result.AzdoProjects.Should().NotBeNull().And.BeEmpty();

            diagnostics.AzdoUserCount.Should().Be(0);
            diagnostics.AzdoGroupCount.Should().Be(0);
            diagnostics.AzdoProjectCount.Should().Be(0);
            diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Gets a temporary file path under the repository .tmp directory.
    /// </summary>
    /// <param name="fileName">The file name to create.</param>
    /// <returns>The absolute file path.</returns>
    private static string GetTempPath(string fileName)
    {
        var tempRoot = GetTempRoot();
        Directory.CreateDirectory(tempRoot);
        return Path.Combine(tempRoot, fileName);
    }

    /// <summary>
    /// Gets the base temporary directory for mapping loader tests.
    /// </summary>
    /// <returns>The absolute directory path.</returns>
    private static string GetTempRoot()
    {
        var tempRoot = Path.Combine(GetRepoRoot(), ".tmp", "mapping-loader-tests");
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    /// <summary>
    /// Resolves the repository root to keep file IO inside the workspace.
    /// </summary>
    /// <returns>Absolute path to the repo root.</returns>
    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
