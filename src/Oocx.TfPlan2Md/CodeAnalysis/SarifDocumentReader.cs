using System.Collections.Generic;
using System.Text.Json;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Reads SARIF document roots into code analysis models.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal static class SarifDocumentReader
{
    /// <summary>
    /// Parses the SARIF document root into a code analysis model.
    /// </summary>
    /// <param name="root">The SARIF document root element.</param>
    /// <returns>The parsed code analysis model.</returns>
    internal static CodeAnalysisModel Parse(JsonElement root)
    {
        var tools = new List<CodeAnalysisTool>();
        var findings = new List<CodeAnalysisFinding>();

        if (!root.TryGetProperty("runs", out var runsElement) || runsElement.ValueKind != JsonValueKind.Array)
        {
            return new CodeAnalysisModel { Tools = tools, Findings = findings };
        }

        foreach (var run in runsElement.EnumerateArray())
        {
            SarifRunReader.ParseRun(run, tools, findings);
        }

        return new CodeAnalysisModel { Tools = tools, Findings = findings };
    }
}
