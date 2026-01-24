using System.Globalization;
using System.Text.Json;
using Oocx.TfPlan2Md.CoverageEnforcer;
using TUnit.Assertions;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.Workflows;

/// <summary>
/// Exercises edge cases for the coverage history writer.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
public class CoverageHistoryWriterEdgeTests
{
    /// <summary>
    /// Verifies whitespace-only history files are treated as empty documents.
    /// </summary>
    [Test]
    public async Task History_writer_handles_whitespace_history_files()
    {
        var tempDirectory = CreateTempDirectory();
        var historyPath = Path.Combine(tempDirectory, "history.json");
        await File.WriteAllTextAsync(historyPath, "   ");
        var entry = CreateEntry("abc123");
        var writer = new CoverageHistoryWriter();

        writer.UpdateHistory(historyPath, entry);

        var json = await File.ReadAllTextAsync(historyPath);
        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("Entries");
        await Assert.That(entries.GetArrayLength()).IsEqualTo(1);
    }

    /// <summary>
    /// Verifies null documents are replaced with a fresh history document.
    /// </summary>
    [Test]
    public async Task History_writer_handles_null_json_document()
    {
        var tempDirectory = CreateTempDirectory();
        var historyPath = Path.Combine(tempDirectory, "history.json");
        await File.WriteAllTextAsync(historyPath, "null");
        var entry = CreateEntry("def456");
        var writer = new CoverageHistoryWriter();

        writer.UpdateHistory(historyPath, entry);

        var json = await File.ReadAllTextAsync(historyPath);
        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("Entries");
        await Assert.That(entries.GetArrayLength()).IsEqualTo(1);
    }

    /// <summary>
    /// Creates a coverage history entry for testing.
    /// </summary>
    /// <param name="commitSha">Commit SHA used in the entry.</param>
    /// <returns>The constructed history entry.</returns>
    private static CoverageHistoryEntry CreateEntry(string commitSha)
    {
        return new CoverageHistoryEntry(
            DateTimeOffset.Parse("2026-01-21T00:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
            commitSha,
            80m,
            70m);
    }

    /// <summary>
    /// Creates a unique temp directory under the workspace .tmp folder.
    /// </summary>
    /// <returns>Absolute path to the created directory.</returns>
    private static string CreateTempDirectory()
    {
        var root = Path.Combine(GetRepoRoot(), ".tmp", "coverage-history-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Resolves the repository root directory.
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
