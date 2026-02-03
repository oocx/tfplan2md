using System;
using System.IO;
using System.Text.Json;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Parses SARIF 2.1.0 files into code analysis models.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed class SarifParser
{
    /// <summary>
    /// Parses a SARIF 2.1.0 file into a code analysis model.
    /// </summary>
    /// <param name="filePath">The path to the SARIF file.</param>
    /// <returns>The parsed code analysis model.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the SARIF file contains invalid JSON.</exception>
    public CodeAnalysisModel ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var json = File.ReadAllText(filePath);
        return ParseJson(json);
    }

    /// <summary>
    /// Parses SARIF JSON content into a code analysis model.
    /// </summary>
    /// <param name="json">The SARIF JSON content.</param>
    /// <returns>The parsed code analysis model.</returns>
    /// <exception cref="JsonException">Thrown when the SARIF content contains invalid JSON.</exception>
    private CodeAnalysisModel ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return SarifDocumentReader.Parse(document.RootElement);
    }
}
