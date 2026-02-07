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
