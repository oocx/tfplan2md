using System.IO;
using AwesomeAssertions;
using Oocx.TfPlan2Md.CodeAnalysis;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.CodeAnalysis;

/// <summary>
/// Tests for loading SARIF inputs into aggregated models.
/// Related feature: docs/features/056-static-analysis-integration/test-plan.md.
/// </summary>
public class CodeAnalysisLoaderTests
{
    /// <summary>
    /// Verifies empty pattern lists produce empty models.
    /// </summary>
    [Test]
    public void Load_NoPatterns_ReturnsEmptyModel()
    {
        var loader = new CodeAnalysisLoader(new SarifParser());

        var result = loader.Load([]);

        result.Model.Tools.Should().BeEmpty();
        result.Model.Findings.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies invalid SARIF files surface warnings instead of throwing.
    /// </summary>
    [Test]
    public void Load_InvalidSarif_AddsWarning()
    {
        var loader = new CodeAnalysisLoader(new SarifParser());
        var tempFile = GetTempPath("invalid-sarif.json");
        File.WriteAllText(tempFile, "not-json");

        try
        {
            var result = loader.Load([tempFile]);

            result.Warnings.Should().ContainSingle();
            result.Warnings[0].FilePath.Should().Be(tempFile);
            result.Model.Findings.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Gets a temporary file path under the repository .tmp directory.
    /// </summary>
    /// <param name="fileName">The file name to create.</param>
    /// <returns>The absolute file path.</returns>
    private static string GetTempPath(string fileName)
    {
        var tempRoot = Path.Combine(GetRepoRoot(), ".tmp", "code-analysis-loader-tests");
        Directory.CreateDirectory(tempRoot);
        return Path.Combine(tempRoot, fileName);
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
