using System.Text.Json;

namespace Oocx.TfPlan2Md.CoverageEnforcer;

/// <summary>
/// Updates the coverage history file with the latest measurement.
/// Related feature: docs/features/043-code-coverage-ci/specification.md.
/// </summary>
internal sealed class CoverageHistoryWriter
{
    /// <summary>
    /// JSON serializer options for persisting the history document.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Appends or updates the history file with the latest coverage entry.
    /// </summary>
    /// <param name="historyPath">Path to the history JSON file.</param>
    /// <param name="entry">Coverage history entry to store.</param>
    internal void UpdateHistory(string historyPath, CoverageHistoryEntry entry)
    {
        if (string.IsNullOrWhiteSpace(historyPath))
        {
            throw new InvalidDataException("History file path must be provided.");
        }

        var document = LoadDocument(historyPath);
        var existing = document.Entries.Find(item => string.Equals(item.CommitSha, entry.CommitSha, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            document.Entries.Remove(existing);
        }

        document.Entries.Add(entry);
        SaveDocument(historyPath, document);
    }

    /// <summary>
    /// Loads the existing history document or returns an empty document.
    /// </summary>
    /// <param name="historyPath">Path to the history JSON file.</param>
    /// <returns>Loaded coverage history document.</returns>
    private static CoverageHistoryDocument LoadDocument(string historyPath)
    {
        if (!File.Exists(historyPath))
        {
            return new CoverageHistoryDocument();
        }

        var content = File.ReadAllText(historyPath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new CoverageHistoryDocument();
        }

        return JsonSerializer.Deserialize<CoverageHistoryDocument>(content, SerializerOptions)
            ?? new CoverageHistoryDocument();
    }

    /// <summary>
    /// Persists the coverage history document to disk.
    /// </summary>
    /// <param name="historyPath">Path to the history JSON file.</param>
    /// <param name="document">Document to serialize.</param>
    private static void SaveDocument(string historyPath, CoverageHistoryDocument document)
    {
        var directory = Path.GetDirectoryName(historyPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        File.WriteAllText(historyPath, json);
    }
}
