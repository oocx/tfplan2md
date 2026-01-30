using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related feature: docs/features/056-static-analysis-integration/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Builds the code analysis report model and attaches findings to resources.
    /// </summary>
    /// <param name="allChanges">The full list of resource changes to update.</param>
    /// <returns>The code analysis report model when inputs are provided; otherwise <c>null</c>.</returns>
    private CodeAnalysisReportModel? BuildCodeAnalysisReport(List<ResourceChangeModel> allChanges)
    {
        if (_codeAnalysisInput is null)
        {
            return null;
        }

        var tools = BuildToolModels(_codeAnalysisInput.Model.Tools);
        var warnings = BuildWarningModels(_codeAnalysisInput.Warnings);
        var effectiveMinimumLevel = GetEffectiveMinimumLevel(_codeAnalysisInput.MinimumLevel, _codeAnalysisInput.FailOnLevel);

        var findings = new List<CodeAnalysisFindingModel>();
        var resourceLookup = allChanges.ToDictionary(c => c.Address, StringComparer.Ordinal);

        foreach (var finding in _codeAnalysisInput.Model.Findings)
        {
            var severity = SeverityMapper.DeriveSeverity(finding);
            if (!MeetsMinimumLevel(severity, effectiveMinimumLevel))
            {
                continue;
            }

            var mappedFindings = ResourceMapper.MapFinding(finding, severity);
            foreach (var mappedFinding in mappedFindings)
            {
                var findingModel = BuildFindingModel(finding, mappedFinding, severity);
                findings.Add(findingModel);

                if (mappedFinding.ResourceAddress is not null)
                {
                    var resourceModel = GetOrCreateResourceChange(allChanges, resourceLookup, mappedFinding);
                    AppendFinding(resourceModel, findingModel);
                }
            }
        }

        if (tools.Count == 0 && warnings.Count == 0 && findings.Count == 0)
        {
            return null;
        }

        var summary = BuildSummaryModel(findings);
        return new CodeAnalysisReportModel
        {
            Summary = summary,
            Tools = tools,
            Warnings = warnings,
            Findings = findings
        };
    }

    /// <summary>
    /// Builds code analysis tool view models for rendering.
    /// </summary>
    /// <param name="tools">The parsed tool metadata.</param>
    /// <returns>The list of tool models.</returns>
    private static List<CodeAnalysisToolModel> BuildToolModels(IReadOnlyList<CodeAnalysisTool> tools)
    {
        return tools
            .Where(tool => !string.IsNullOrWhiteSpace(tool.Name))
            .Select(tool => new CodeAnalysisToolModel
            {
                Name = tool.Name,
                Version = tool.Version,
                DisplayName = BuildToolDisplayName(tool.Name, tool.Version)
            })
            .GroupBy(tool => tool.DisplayName, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    /// Builds code analysis warning view models for rendering.
    /// </summary>
    /// <param name="warnings">The warnings encountered during parsing.</param>
    /// <returns>The list of warning models.</returns>
    private static List<CodeAnalysisWarningModel> BuildWarningModels(IReadOnlyList<CodeAnalysisWarning> warnings)
    {
        return warnings
            .Select(warning => new CodeAnalysisWarningModel
            {
                FilePath = warning.FilePath,
                Message = warning.Message
            })
            .ToList();
    }

    /// <summary>
    /// Builds a formatted display name for a code analysis tool.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="version">The tool version, if available.</param>
    /// <returns>The formatted display name.</returns>
    private static string BuildToolDisplayName(string name, string? version)
    {
        return string.IsNullOrWhiteSpace(version)
            ? name
            : $"{name} {version}";
    }

    /// <summary>
    /// Builds a code analysis finding view model from mapped data.
    /// </summary>
    /// <param name="finding">The source SARIF finding.</param>
    /// <param name="mappedFinding">The mapped location data.</param>
    /// <param name="severity">The derived severity.</param>
    /// <returns>The finding view model.</returns>
    private static CodeAnalysisFindingModel BuildFindingModel(
        CodeAnalysisFinding finding,
        CodeAnalysisMappedFinding mappedFinding,
        CodeAnalysisSeverity severity)
    {
        return new CodeAnalysisFindingModel
        {
            Severity = GetSeverityLabel(severity),
            SeverityIcon = GetSeverityIcon(severity),
            SeverityRank = GetSeverityRank(severity),
            Message = finding.Message,
            RuleId = finding.RuleId,
            HelpUri = finding.HelpUri,
            ToolName = finding.ToolName,
            ResourceAddress = mappedFinding.ResourceAddress,
            ModuleAddress = mappedFinding.ModuleAddress,
            AttributePath = mappedFinding.AttributePath
        };
    }

    /// <summary>
    /// Builds summary counts for the provided findings.
    /// </summary>
    /// <param name="findings">The findings to summarize.</param>
    /// <returns>The summary model.</returns>
    private static CodeAnalysisSummaryModel BuildSummaryModel(List<CodeAnalysisFindingModel> findings)
    {
        var critical = findings.Count(f => f.Severity == "Critical");
        var high = findings.Count(f => f.Severity == "High");
        var medium = findings.Count(f => f.Severity == "Medium");
        var low = findings.Count(f => f.Severity == "Low");
        var informational = findings.Count(f => f.Severity == "Informational");

        return new CodeAnalysisSummaryModel
        {
            CriticalCount = critical,
            HighCount = high,
            MediumCount = medium,
            LowCount = low,
            InformationalCount = informational,
            TotalCount = findings.Count
        };
    }

    /// <summary>
    /// Determines the effective minimum severity level for filtering.
    /// </summary>
    /// <param name="minimumLevel">The minimum display level.</param>
    /// <param name="failOnLevel">The failure threshold level.</param>
    /// <returns>The effective minimum level, or <c>null</c> when not specified.</returns>
    private static CodeAnalysisSeverity? GetEffectiveMinimumLevel(
        CodeAnalysisSeverity? minimumLevel,
        CodeAnalysisSeverity? failOnLevel)
    {
        if (minimumLevel is null)
        {
            return failOnLevel;
        }

        if (failOnLevel is null)
        {
            return minimumLevel;
        }

        var minimumRank = GetSeverityRank(minimumLevel.Value);
        var failOnRank = GetSeverityRank(failOnLevel.Value);
        return minimumRank <= failOnRank ? minimumLevel : failOnLevel;
    }

    /// <summary>
    /// Determines whether a severity meets the configured minimum level.
    /// </summary>
    /// <param name="severity">The severity to evaluate.</param>
    /// <param name="minimumLevel">The minimum severity to include.</param>
    /// <returns><c>true</c> when the severity meets the minimum level; otherwise <c>false</c>.</returns>
    private static bool MeetsMinimumLevel(CodeAnalysisSeverity severity, CodeAnalysisSeverity? minimumLevel)
    {
        if (minimumLevel is null)
        {
            return true;
        }

        return GetSeverityRank(severity) >= GetSeverityRank(minimumLevel.Value);
    }

    /// <summary>
    /// Appends a code analysis finding to a resource change.
    /// </summary>
    /// <param name="resource">The resource change to update.</param>
    /// <param name="finding">The finding to append.</param>
    private static void AppendFinding(ResourceChangeModel resource, CodeAnalysisFindingModel finding)
    {
        var updated = resource.CodeAnalysisFindings.ToList();
        updated.Add(finding);
        resource.CodeAnalysisFindings = updated;
    }

    /// <summary>
    /// Retrieves an existing resource change or creates a findings-only entry when missing.
    /// </summary>
    /// <param name="allChanges">The list of existing changes.</param>
    /// <param name="lookup">Lookup dictionary by resource address.</param>
    /// <param name="mappedFinding">The mapped finding data.</param>
    /// <returns>The existing or newly created resource change.</returns>
    private ResourceChangeModel GetOrCreateResourceChange(
        List<ResourceChangeModel> allChanges,
        Dictionary<string, ResourceChangeModel> lookup,
        CodeAnalysisMappedFinding mappedFinding)
    {
        var address = mappedFinding.ResourceAddress!;
        if (lookup.TryGetValue(address, out var existing))
        {
            return existing;
        }

        var (type, name) = ParseResourceTypeAndName(address);
        var providerName = ParseProviderName(type);
        var model = new ResourceChangeModel
        {
            Address = address,
            ModuleAddress = mappedFinding.ModuleAddress,
            Type = type,
            Name = name,
            ProviderName = providerName,
            Action = "no-op",
            ActionSymbol = GetActionSymbol("no-op"),
            AttributeChanges = [],
            BeforeJson = null,
            AfterJson = null,
            ReplacePaths = null
        };

        FinalizeResourceChangeModel(model);
        allChanges.Add(model);
        lookup[address] = model;
        return model;
    }

    /// <summary>
    /// Finalizes summary-related fields for a resource change model.
    /// </summary>
    /// <param name="model">The resource change model to finalize.</param>
    private void FinalizeResourceChangeModel(ResourceChangeModel model)
    {
        model.Summary = _summaryBuilder.BuildSummary(model);
        if (string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            model.ChangedAttributesSummary = BuildChangedAttributesSummary(model.AttributeChanges, model.Action);
        }

        model.TagsBadges = BuildTagsBadges(model.AfterJson, model.BeforeJson, model.Action);
        if (string.IsNullOrWhiteSpace(model.SummaryHtml))
        {
            model.SummaryHtml = BuildSummaryHtml(model);
        }
    }

    /// <summary>
    /// Parses a Terraform resource address into its type and name components.
    /// </summary>
    /// <param name="resourceAddress">The Terraform resource address.</param>
    /// <returns>The resource type and name.</returns>
    private static (string Type, string Name) ParseResourceTypeAndName(string resourceAddress)
    {
        var tokens = resourceAddress.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2)
        {
            return (tokens[^2], tokens[^1]);
        }

        return (resourceAddress, resourceAddress);
    }

    /// <summary>
    /// Determines the provider name from a Terraform resource type.
    /// </summary>
    /// <param name="resourceType">The Terraform resource type.</param>
    /// <returns>The provider name.</returns>
    private static string ParseProviderName(string resourceType)
    {
        var underscoreIndex = resourceType.IndexOf('_', StringComparison.Ordinal);
        return underscoreIndex > 0 ? resourceType[..underscoreIndex] : resourceType;
    }

    /// <summary>
    /// Gets the display label for a normalized severity.
    /// </summary>
    /// <param name="severity">The severity value.</param>
    /// <returns>The display label.</returns>
    private static string GetSeverityLabel(CodeAnalysisSeverity severity)
    {
        return severity switch
        {
            CodeAnalysisSeverity.Critical => "Critical",
            CodeAnalysisSeverity.High => "High",
            CodeAnalysisSeverity.Medium => "Medium",
            CodeAnalysisSeverity.Low => "Low",
            _ => "Informational"
        };
    }

    /// <summary>
    /// Gets the icon for a normalized severity.
    /// </summary>
    /// <param name="severity">The severity value.</param>
    /// <returns>The severity icon.</returns>
    private static string GetSeverityIcon(CodeAnalysisSeverity severity)
    {
        return severity switch
        {
            CodeAnalysisSeverity.Critical => "🚨",
            CodeAnalysisSeverity.High => "⚠️",
            CodeAnalysisSeverity.Medium => "⚠️",
            CodeAnalysisSeverity.Low => "ℹ️",
            _ => "ℹ️"
        };
    }

    /// <summary>
    /// Gets the severity rank used for ordering (higher means more severe).
    /// </summary>
    /// <param name="severity">The severity value.</param>
    /// <returns>The severity rank.</returns>
    private static int GetSeverityRank(CodeAnalysisSeverity severity)
    {
        return severity switch
        {
            CodeAnalysisSeverity.Critical => 5,
            CodeAnalysisSeverity.High => 4,
            CodeAnalysisSeverity.Medium => 3,
            CodeAnalysisSeverity.Low => 2,
            _ => 1
        };
    }
}
