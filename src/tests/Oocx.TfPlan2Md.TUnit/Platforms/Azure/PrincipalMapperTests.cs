using System;
using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.Diagnostics;
using Oocx.TfPlan2Md.Platforms.Azure;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Platforms.Azure;

/// <summary>
/// Tests for mapping Azure principals using mapping files.
/// Related feature: docs/features/006-role-assignment-readable-display/.
/// </summary>
public class PrincipalMapperTests
{
    /// <summary>
    /// Verifies missing mapping files fall back to raw IDs.
    /// </summary>
    [Test]
    public void GetPrincipalName_NoMappingFile_ReturnsRawId()
    {
        var mapper = new PrincipalMapper(null);

        var result = mapper.GetPrincipalName("00000000-0000-0000-0000-000000000000");

        result.Should().Be("00000000-0000-0000-0000-000000000000");
    }

    /// <summary>
    /// Verifies nested mapping files resolve names and types with diagnostics.
    /// </summary>
    [Test]
    public void LoadMappings_NestedFormat_ResolvesNamesAndTypes()
    {
        var filePath = GetTempPath($"nested-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "users": {
            "11111111-1111-1111-1111-111111111111": "jane.doe@contoso.com"
          },
          "groups": {
            "22222222-2222-2222-2222-222222222222": "Platform Team"
          },
          "servicePrincipals": {
            "33333333-3333-3333-3333-333333333333": "terraform-spn"
          }
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var mapper = new PrincipalMapper(filePath, diagnostics);

            mapper.GetPrincipalName("11111111-1111-1111-1111-111111111111")
                .Should().Be("jane.doe@contoso.com [11111111-1111-1111-1111-111111111111]");
            mapper.TryGetPrincipalType("22222222-2222-2222-2222-222222222222", out var principalType)
                .Should().BeTrue();
            principalType.Should().Be("Group");

            diagnostics.PrincipalMappingFileProvided.Should().BeTrue();
            diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
            diagnostics.PrincipalTypeCount.Should().ContainKey("users").WhoseValue.Should().Be(1);
            diagnostics.PrincipalTypeCount.Should().ContainKey("groups").WhoseValue.Should().Be(1);
            diagnostics.PrincipalTypeCount.Should().ContainKey("servicePrincipals").WhoseValue.Should().Be(1);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Verifies flat mapping files resolve names and record totals.
    /// </summary>
    [Test]
    public void LoadMappings_FlatFormat_ResolvesNames()
    {
        var filePath = GetTempPath($"flat-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, """
        {
          "44444444-4444-4444-4444-444444444444": "flat-user"
        }
        """);
        var diagnostics = new DiagnosticContext();

        try
        {
            var mapper = new PrincipalMapper(filePath, diagnostics);

            mapper.GetName("44444444-4444-4444-4444-444444444444").Should().Be("flat-user");
            diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeTrue();
            diagnostics.PrincipalTypeCount.Should().ContainKey("principals").WhoseValue.Should().Be(1);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Verifies diagnostics capture missing mapping files.
    /// </summary>
    [Test]
    public void LoadMappings_MissingFile_RecordsDiagnostics()
    {
        var missingPath = Path.Combine(GetTempRoot(), "missing", "principals.json");
        var diagnostics = new DiagnosticContext();

        var mapper = new PrincipalMapper(missingPath, diagnostics);

        mapper.GetName("99999999-9999-9999-9999-999999999999").Should().BeNull();
        diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeFalse();
        diagnostics.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.DirectoryNotFound);
    }

    /// <summary>
    /// Verifies missing files within existing directories are reported.
    /// </summary>
    [Test]
    public void LoadMappings_FileMissingInExistingDirectory_RecordsFileNotFound()
    {
        var directory = Path.Combine(GetTempRoot(), "existing");
        Directory.CreateDirectory(directory);
        var missingPath = Path.Combine(directory, "principals.json");
        var diagnostics = new DiagnosticContext();

        var mapper = new PrincipalMapper(missingPath, diagnostics);

        mapper.GetName("missing").Should().BeNull();
        diagnostics.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.FileNotFound);
    }

    /// <summary>
    /// Verifies invalid JSON is captured as a parsing error.
    /// </summary>
    [Test]
    public void LoadMappings_InvalidJson_RecordsParseError()
    {
        var filePath = GetTempPath($"invalid-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, "{");
        var diagnostics = new DiagnosticContext();

        try
        {
            var mapper = new PrincipalMapper(filePath, diagnostics);

            mapper.GetName("missing").Should().BeNull();
            diagnostics.PrincipalMappingLoadedSuccessfully.Should().BeFalse();
            diagnostics.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.JsonParseError);
            diagnostics.PrincipalMappingErrorMessage.Should().Be("Invalid JSON syntax");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Verifies null mapping files are treated as empty and reported.
    /// </summary>
    [Test]
    public void LoadMappings_NullPayload_RecordsEmptyFile()
    {
        var filePath = GetTempPath($"null-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, "null");
        var diagnostics = new DiagnosticContext();

        try
        {
            var mapper = new PrincipalMapper(filePath, diagnostics);

            mapper.GetName("missing").Should().BeNull();
            diagnostics.PrincipalMappingErrorType.Should().Be(PrincipalLoadError.EmptyFile);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Verifies failed resolutions are tracked when a resource address is provided.
    /// </summary>
    [Test]
    public void GetName_UnmappedId_RecordsFailedResolution()
    {
        var filePath = GetTempPath($"flat-mapping-{Guid.NewGuid():N}.json");
        File.WriteAllText(filePath, "{\"known\":\"name\"}");
        var diagnostics = new DiagnosticContext();

        try
        {
            var mapper = new PrincipalMapper(filePath, diagnostics);

            mapper.GetName("missing", "User", "azurerm_role_assignment.example").Should().BeNull();

            diagnostics.FailedResolutions.Should().ContainSingle();
            diagnostics.FailedResolutions[0].PrincipalId.Should().Be("missing");
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
    /// Gets the base temporary directory for principal mapper tests.
    /// </summary>
    /// <returns>The absolute directory path.</returns>
    private static string GetTempRoot()
    {
        var tempRoot = Path.Combine(GetRepoRoot(), ".tmp", "principal-mapper-tests");
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
