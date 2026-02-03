using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.CodeAnalysis;

/// <summary>
/// Loads and aggregates SARIF files into a unified code analysis model.
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </summary>
internal sealed class CodeAnalysisLoader
{
    private readonly SarifParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeAnalysisLoader"/> class.
    /// </summary>
    /// <param name="parser">The SARIF parser to use for file loading.</param>
    internal CodeAnalysisLoader(SarifParser parser)
    {
        _parser = parser;
    }

    /// <summary>
    /// Loads SARIF inputs from the provided file patterns.
    /// </summary>
    /// <param name="patterns">The file patterns to expand and parse.</param>
    /// <returns>The aggregated load result containing findings and warnings.</returns>
    internal CodeAnalysisLoadResult Load(IReadOnlyList<string> patterns)
    {
        if (patterns.Count == 0)
        {
            return new CodeAnalysisLoadResult
            {
                Model = new CodeAnalysisModel
                {
                    Tools = [],
                    Findings = []
                },
                Warnings = []
            };
        }

        var files = WildcardExpander.Expand(patterns);
        var tools = new List<CodeAnalysisTool>();
        var findings = new List<CodeAnalysisFinding>();
        var warnings = new List<CodeAnalysisWarning>();

        foreach (var file in files)
        {
            try
            {
                var model = _parser.ParseFile(file);
                tools.AddRange(model.Tools);
                findings.AddRange(model.Findings);
            }
            catch (Exception ex)
            {
                warnings.Add(new CodeAnalysisWarning
                {
                    FilePath = file,
                    Message = ex.Message
                });
            }
        }

        return new CodeAnalysisLoadResult
        {
            Model = new CodeAnalysisModel
            {
                Tools = tools,
                Findings = findings
            },
            Warnings = warnings
        };
    }
}
