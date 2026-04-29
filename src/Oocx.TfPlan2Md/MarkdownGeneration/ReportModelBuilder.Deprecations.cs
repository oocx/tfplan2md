using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builder partial that emits Terraform 1.15+ deprecation warnings through the
/// existing code-analysis warnings pipeline (per ADR-004), so reviewers see
/// deprecation notices in the same "Warnings" section as SARIF processing
/// failures with no parallel UI surface.
/// Related feature: docs/features/122-terraform-1-15-support/adr-004-deprecation-warnings-via-existing-pipeline.md.
/// </summary>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Walks the plan configuration via <see cref="ConfigurationDeprecationReader"/>
    /// and produces a warning per deprecated variable or output that is referenced
    /// by the plan. Variables are considered referenced when present in
    /// <see cref="TerraformPlan.Variables"/>; outputs when present in
    /// <see cref="TerraformPlan.OutputChanges"/>. Unreferenced deprecations are
    /// suppressed to avoid noise.
    /// </summary>
    /// <param name="plan">Parsed Terraform plan.</param>
    /// <returns>A list of deprecation warning models (empty when no referenced deprecations exist).</returns>
    private static List<CodeAnalysisWarningModel> BuildDeprecationWarnings(TerraformPlan plan)
    {
        var warnings = new List<CodeAnalysisWarningModel>();

        foreach (var (name, kind, message, _) in ConfigurationDeprecationReader.ReadDeprecations(plan.Configuration))
        {
            if (!IsDeprecationReferenced(plan, name, kind))
            {
                continue;
            }

            warnings.Add(new CodeAnalysisWarningModel
            {
                Source = CodeAnalysisWarningSource.PlanDeprecation,
                SubjectKind = kind,
                SubjectName = name,
                Message = message,
                FilePath = null
            });
        }

        return warnings;
    }

    /// <summary>
    /// Determines whether a deprecated variable or output is actually referenced
    /// by the plan. Filtering at this layer means unused module-level deprecations
    /// never reach the rendered report (per AC-8 / FR-M2.4 wording: "each deprecated
    /// item that is referenced by the plan").
    /// </summary>
    /// <param name="plan">Parsed Terraform plan.</param>
    /// <param name="name">Subject name (variable or output identifier).</param>
    /// <param name="kind">Subject kind ("variable" or "output").</param>
    /// <returns><c>true</c> when the deprecation is referenced by the plan; otherwise <c>false</c>.</returns>
    private static bool IsDeprecationReferenced(TerraformPlan plan, string name, string kind)
    {
        return kind switch
        {
            "variable" => plan.Variables?.ContainsKey(name) == true,
            "output" => plan.OutputChanges?.ContainsKey(name) == true,
            _ => false
        };
    }
}
