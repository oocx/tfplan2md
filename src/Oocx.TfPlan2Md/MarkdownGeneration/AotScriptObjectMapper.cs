using System;
using System.Collections.Generic;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Scriban.Runtime;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Provides explicit mapping of ReportModel to ScriptObject for NativeAOT compatibility.
/// Reflection-based Scriban Import does not work reliably under AOT trimming.
/// Related feature: docs/features/037-aot-trimmed-image/specification.md.
/// </summary>
internal static class AotScriptObjectMapper
{
    /// <summary>
    /// The Scriban key used for module address fields.
    /// </summary>
    private const string ModuleAddressKey = "module_address";

    /// <summary>
    /// Maps a ReportModel to a ScriptObject without using reflection.
    /// </summary>
    /// <param name="model">The report model to map.</param>
    /// <param name="mapperRegistry">Optional registry for provider-specific resource model mappers.</param>
    /// <returns>A ScriptObject containing all report data accessible by templates.</returns>
    internal static ScriptObject MapReportModel(ReportModel model, Services.ResourceModelMapperRegistry? mapperRegistry = null)
    {
        var scriptObject = new ScriptObject();

        // Top-level scalar properties
        scriptObject["terraform_version"] = model.TerraformVersion;
        scriptObject["format_version"] = model.FormatVersion;
        scriptObject["tf_plan2_md_version"] = model.TfPlan2MdVersion;
        scriptObject["commit_hash"] = model.CommitHash;
        scriptObject["hide_metadata"] = model.HideMetadata;
        scriptObject["timestamp"] = model.Timestamp;
        scriptObject["report_title"] = model.ReportTitle;
        scriptObject["show_unchanged_values"] = model.ShowUnchangedValues;
        scriptObject["show_sensitive"] = model.ShowSensitive;
        scriptObject["large_value_format"] = model.RenderTarget == RenderTargets.RenderTarget.GitHub ? "simple-diff" : "inline-diff";

        // Generated timestamp as nested object with DateTime for Scriban date functions
        var generatedAtUtcObj = new ScriptObject();
        generatedAtUtcObj.Add("date_time", model.GeneratedAtUtc.UtcDateTime);
        scriptObject["generated_at_utc"] = generatedAtUtcObj;

        // Summary
        scriptObject["summary"] = MapSummary(model.Summary);

        // Code analysis
        scriptObject["code_analysis"] = model.CodeAnalysis is null
            ? null
            : MapCodeAnalysisReport(model.CodeAnalysis);

        // Changes and module changes
        scriptObject["changes"] = MapChanges(model.Changes, model.ShowSensitive, mapperRegistry);
        scriptObject["module_changes"] = MapModuleChanges(model.ModuleChanges, model.ShowSensitive, mapperRegistry);
        scriptObject["refactoring_operations"] = MapRefactoringOperations(model.RefactoringOperations);

        return scriptObject;
    }

    /// <summary>
    /// Maps a ResourceChangeModel to a ScriptObject for resource-specific template rendering.
    /// Includes large_value_format from the provided parameter.
    /// </summary>
    /// <param name="change">The resource change to map.</param>
    /// <param name="renderTarget">The target platform for rendering.</param>
    /// <param name="mapperRegistry">Optional registry for provider-specific resource model mappers.</param>
    /// <param name="showSensitive">Whether to reveal sensitive values. When <c>false</c>, sensitive JSON leaves are masked.</param>
    /// <returns>A ScriptObject containing the change data.</returns>
    /// <remarks>
    /// Delegates to <see cref="MapResourceChange"/> which applies sensitivity masking on
    /// <c>before_json</c>/<c>after_json</c> before the template sees them.
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </remarks>
    internal static ScriptObject MapResourceChangeWithFormat(
        ResourceChangeModel change,
        RenderTargets.RenderTarget renderTarget,
        Services.ResourceModelMapperRegistry? mapperRegistry = null,
        bool showSensitive = false)
    {
        var changeObject = MapResourceChange(change, showSensitive, mapperRegistry);

        // Add large_value_format to change context for template access
        var formatString = renderTarget == RenderTargets.RenderTarget.GitHub ? "simple-diff" : "inline-diff";
        changeObject["large_value_format"] = formatString;

        return changeObject;
    }

    private static ScriptObject MapSummary(SummaryModel summary)
    {
        var obj = new ScriptObject();
        obj["to_add"] = MapActionSummary(summary.ToAdd);
        obj["to_change"] = MapActionSummary(summary.ToChange);
        obj["to_destroy"] = MapActionSummary(summary.ToDestroy);
        obj["to_replace"] = MapActionSummary(summary.ToReplace);
        obj["no_op"] = MapActionSummary(summary.NoOp);
        obj["total"] = summary.Total;
        return obj;
    }

    private static ScriptObject MapActionSummary(ActionSummary action)
    {
        var obj = new ScriptObject();
        obj["count"] = action.Count;

        obj["breakdown"] = MapResourceTypeBreakdown(action.Breakdown);
        return obj;
    }

    /// <summary>
    /// Maps resource type breakdown entries to a Scriban array.
    /// </summary>
    /// <param name="breakdown">The breakdown entries to map.</param>
    /// <returns>The mapped Scriban array.</returns>
    private static ScriptArray MapResourceTypeBreakdown(IReadOnlyList<ResourceTypeBreakdown> breakdown)
    {
        var arr = new ScriptArray();
        foreach (var item in breakdown)
        {
            var obj = new ScriptObject();
            obj["type"] = item.Type;
            obj["count"] = item.Count;
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptArray MapChanges(IReadOnlyList<ResourceChangeModel> changes, bool showSensitive, Services.ResourceModelMapperRegistry? mapperRegistry = null)
    {
        var arr = new ScriptArray();
        foreach (var change in changes)
        {
            arr.Add(MapResourceChange(change, showSensitive, mapperRegistry));
        }

        return arr;
    }

    private static ScriptArray MapModuleChanges(IReadOnlyList<ModuleChangeGroup> moduleChanges, bool showSensitive, Services.ResourceModelMapperRegistry? mapperRegistry = null)
    {
        var arr = new ScriptArray();
        foreach (var group in moduleChanges)
        {
            var obj = new ScriptObject();
            obj[ModuleAddressKey] = group.ModuleAddress;
            obj["changes"] = MapChanges(group.Changes, showSensitive, mapperRegistry);
            arr.Add(obj);
        }

        return arr;
    }

    /// <summary>
    /// Maps refactoring operations to a Scriban array for template rendering.
    /// Related feature: docs/features/057-terraform-import-moved-blocks/specification.md.
    /// </summary>
    /// <param name="operations">The refactoring operations to map.</param>
    /// <returns>A Scriban array of refactoring operation objects.</returns>
    private static ScriptArray MapRefactoringOperations(IReadOnlyList<RefactoringOperationModel> operations)
    {
        var arr = new ScriptArray();
        foreach (var operation in operations)
        {
            var obj = new ScriptObject();
            obj["operation"] = operation.Operation;
            obj["address"] = operation.Address;
            obj["resource_type"] = operation.ResourceType;
            obj["resource_name"] = operation.ResourceName;
            obj["details"] = operation.Details;
            obj["status"] = operation.Status;
            obj["is_already_applied"] = operation.IsAlreadyApplied;
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptObject MapResourceChange(ResourceChangeModel change, bool showSensitive = false, Services.ResourceModelMapperRegistry? mapperRegistry = null)
    {
        var obj = new ScriptObject();
        obj["address"] = change.Address;
        obj[ModuleAddressKey] = change.ModuleAddress;
        obj["type"] = change.Type;
        obj["name"] = change.Name;
        obj["provider_name"] = change.ProviderName;
        obj["action"] = change.Action;
        obj["action_symbol"] = change.ActionSymbol;
        obj["summary"] = change.Summary;
        obj["summary_html"] = change.SummaryHtml;
        obj["changed_attributes_summary"] = change.ChangedAttributesSummary;
        obj["tags_badges"] = change.TagsBadges;
        obj["import_id"] = change.ImportId;
        obj["moved_from_address"] = change.MovedFromAddress;
        obj["is_refactoring_already_applied"] = change.IsRefactoringAlreadyApplied;

        // Sensitivity maps for provider templates to mask sensitive values
        // Related issue: docs/issues/098-sensitive-info-exposure/analysis.md
        var beforeSensitiveObj = change.BeforeSensitive is JsonElement sensBefore
            ? ConvertToScriptObject(sensBefore)
            : null;
        var afterSensitiveObj = change.AfterSensitive is JsonElement sensAfter
            ? ConvertToScriptObject(sensAfter)
            : null;

        obj["before_sensitive"] = beforeSensitiveObj;
        obj["after_sensitive"] = afterSensitiveObj;

        // JSON values — mask sensitive leaves when showSensitive is false
        var beforeJsonObj = change.BeforeJson is JsonElement jsonBefore
            ? ConvertToScriptObject(jsonBefore)
            : null;
        var afterJsonObj = change.AfterJson is JsonElement jsonAfter
            ? ConvertToScriptObject(jsonAfter)
            : null;

        if (!showSensitive)
        {
            MaskSensitiveLeaves(beforeJsonObj, beforeSensitiveObj);
            MaskSensitiveLeaves(afterJsonObj, afterSensitiveObj);
        }

        obj["before_json"] = beforeJsonObj;
        obj["after_json"] = afterJsonObj;

        // Replace paths
        if (change.ReplacePaths != null)
        {
            var replacePaths = new ScriptArray();
            foreach (var path in change.ReplacePaths)
            {
                var pathArr = new ScriptArray();
                foreach (var segment in path)
                {
                    pathArr.Add(segment?.ToString());
                }

                replacePaths.Add(pathArr);
            }

            obj["replace_paths"] = replacePaths;
        }

        // Attribute changes
        var attrChanges = new ScriptArray();
        foreach (var attr in change.AttributeChanges)
        {
            attrChanges.Add(MapAttributeChange(attr));
        }

        obj["attribute_changes"] = attrChanges;

        // Child resource groups
        obj["child_resource_groups"] = MapChildResourceGroups(change.ChildResourceGroups);

        // Code analysis findings
        obj["code_analysis_findings"] = MapCodeAnalysisFindings(change.CodeAnalysisFindings);

        // Provider-specific view models (delegated to registry)
        mapperRegistry?.EnrichScriptObject(change, obj);

        return obj;
    }

    /// <summary>
    /// Maps child resource groups to a Scriban array for template rendering.
    /// </summary>
    /// <param name="groups">The child resource groups to map.</param>
    /// <returns>The mapped Scriban array.</returns>
    private static ScriptArray MapChildResourceGroups(IReadOnlyList<ChildResourceGroup> groups)
    {
        var arr = new ScriptArray();
        foreach (var group in groups)
        {
            var obj = new ScriptObject();
            obj["label"] = group.Label;
            obj["columns"] = MapChildTableColumns(group.Columns);
            obj["rows"] = MapChildResourceRows(group.Rows);
            obj["has_mixed_sources"] = group.HasMixedSources;
            obj["has_external_resources"] = group.HasExternalResources;
            arr.Add(obj);
        }

        return arr;
    }

    /// <summary>
    /// Maps child table columns to a Scriban array.
    /// </summary>
    /// <param name="columns">The column definitions to map.</param>
    /// <returns>The mapped Scriban array.</returns>
    private static ScriptArray MapChildTableColumns(IReadOnlyList<ChildTableColumn> columns)
    {
        var arr = new ScriptArray();
        foreach (var column in columns)
        {
            var obj = new ScriptObject();
            obj["header"] = column.Header;
            obj["property_name"] = column.PropertyName;
            arr.Add(obj);
        }

        return arr;
    }

    /// <summary>
    /// Maps child resource rows to a Scriban array.
    /// </summary>
    /// <param name="rows">The child rows to map.</param>
    /// <returns>The mapped Scriban array.</returns>
    private static ScriptArray MapChildResourceRows(IReadOnlyList<ChildResourceRow> rows)
    {
        var arr = new ScriptArray();
        foreach (var row in rows)
        {
            var obj = new ScriptObject();
            obj["change_indicator"] = row.ChangeIndicator;
            obj["values"] = MapChildRowValues(row.Values);
            obj["terraform_resource"] = row.TerraformResource;
            obj["original_resource_address"] = row.OriginalResourceAddress;
            arr.Add(obj);
        }

        return arr;
    }

    /// <summary>
    /// Maps child row value dictionaries to a Scriban object.
    /// </summary>
    /// <param name="values">The row values keyed by column property name.</param>
    /// <returns>The mapped Scriban object.</returns>
    private static ScriptObject MapChildRowValues(IReadOnlyDictionary<string, string> values)
    {
        var obj = new ScriptObject();
        foreach (var (key, value) in values)
        {
            obj[key] = value;
        }

        return obj;
    }

    private static ScriptObject MapCodeAnalysisReport(CodeAnalysisReportModel report)
    {
        var obj = new ScriptObject();
        obj["summary"] = MapCodeAnalysisSummary(report.Summary);
        obj["tools"] = MapCodeAnalysisTools(report.Tools);
        obj["warnings"] = MapCodeAnalysisWarnings(report.Warnings);
        obj["findings"] = MapCodeAnalysisFindings(report.Findings);
        obj["module_findings"] = MapCodeAnalysisModuleFindings(report.ModuleFindings);
        obj["unmatched_findings"] = MapCodeAnalysisFindings(report.UnmatchedFindings);
        return obj;
    }

    private static ScriptObject MapCodeAnalysisSummary(CodeAnalysisSummaryModel summary)
    {
        var obj = new ScriptObject();
        obj["critical_count"] = summary.CriticalCount;
        obj["critical_breakdown"] = MapResourceTypeBreakdown(summary.CriticalResourceTypes);
        obj["high_count"] = summary.HighCount;
        obj["high_breakdown"] = MapResourceTypeBreakdown(summary.HighResourceTypes);
        obj["medium_count"] = summary.MediumCount;
        obj["medium_breakdown"] = MapResourceTypeBreakdown(summary.MediumResourceTypes);
        obj["low_count"] = summary.LowCount;
        obj["low_breakdown"] = MapResourceTypeBreakdown(summary.LowResourceTypes);
        obj["informational_count"] = summary.InformationalCount;
        obj["informational_breakdown"] = MapResourceTypeBreakdown(summary.InformationalResourceTypes);
        obj["total_count"] = summary.TotalCount;
        return obj;
    }

    private static ScriptArray MapCodeAnalysisTools(IReadOnlyList<CodeAnalysisToolModel> tools)
    {
        var arr = new ScriptArray();
        foreach (var tool in tools)
        {
            var obj = new ScriptObject();
            obj["name"] = tool.Name;
            obj["version"] = tool.Version;
            obj["display_name"] = tool.DisplayName;
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptArray MapCodeAnalysisWarnings(IReadOnlyList<CodeAnalysisWarningModel> warnings)
    {
        var arr = new ScriptArray();
        foreach (var warning in warnings)
        {
            var obj = new ScriptObject();
            obj["file_path"] = warning.FilePath;
            obj["message"] = warning.Message;
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptArray MapCodeAnalysisFindings(IReadOnlyList<CodeAnalysisFindingModel> findings)
    {
        var arr = new ScriptArray();
        foreach (var finding in findings)
        {
            var obj = new ScriptObject();
            obj["severity"] = finding.Severity;
            obj["severity_icon"] = finding.SeverityIcon;
            obj["severity_rank"] = finding.SeverityRank;
            obj["message"] = finding.Message;
            obj["rule_id"] = finding.RuleId;
            obj["help_uri"] = finding.HelpUri;
            obj["tool_name"] = finding.ToolName;
            obj["resource_address"] = finding.ResourceAddress;
            obj[ModuleAddressKey] = finding.ModuleAddress;
            obj["attribute_path"] = finding.AttributePath;
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptArray MapCodeAnalysisModuleFindings(IReadOnlyList<CodeAnalysisModuleFindingsModel> modules)
    {
        var arr = new ScriptArray();
        foreach (var module in modules)
        {
            var obj = new ScriptObject();
            obj[ModuleAddressKey] = module.ModuleAddress;
            obj["findings"] = MapCodeAnalysisFindings(module.Findings);
            arr.Add(obj);
        }

        return arr;
    }

    private static ScriptObject MapAttributeChange(AttributeChangeModel attr)
    {
        var obj = new ScriptObject();
        obj["name"] = attr.Name;
        obj["before"] = attr.Before;
        obj["after"] = attr.After;
        obj["is_sensitive"] = attr.IsSensitive;
        obj["is_large"] = attr.IsLarge;
        return obj;
    }

    /// <summary>
    /// Replaces leaf values in a JSON-derived <see cref="ScriptObject"/> with <c>(sensitive)</c>
    /// where the corresponding node in the sensitivity map is <c>true</c>.
    /// </summary>
    /// <param name="jsonObj">The JSON tree (from <see cref="ConvertToScriptObject"/>). Modified in place.</param>
    /// <param name="sensitivityObj">The sensitivity map. A <c>true</c> at any leaf marks the
    /// corresponding value in <paramref name="jsonObj"/> as sensitive.</param>
    /// <remarks>
    /// Handles three sensitivity patterns from Terraform plan data:
    /// <list type="bullet">
    ///   <item><c>true</c> at object level — all children are sensitive</item>
    ///   <item><c>true</c> at leaf level — single property is sensitive</item>
    ///   <item>nested object — recurse into sub-trees</item>
    /// </list>
    /// Related issue: docs/issues/098-sensitive-info-exposure/analysis.md.
    /// </remarks>
    private static void MaskSensitiveLeaves(object? jsonObj, object? sensitivityObj)
    {
        if (jsonObj is not ScriptObject json)
        {
            return;
        }

        // If the entire sensitivity tree is boolean true, mask ALL leaves
        if (sensitivityObj is bool allSensitive && allSensitive)
        {
            MaskAllLeaves(json);
            return;
        }

        if (sensitivityObj is not ScriptObject sensitivity)
        {
            return;
        }

        foreach (var key in json.GetMembers())
        {
            var sensitiveValue = sensitivity.TryGetValue(key, out var sv) ? sv : null;

            if (sensitiveValue is bool isSensitive && isSensitive)
            {
                // This leaf/subtree is marked sensitive — mask it
                if (json[key] is ScriptObject childObj)
                {
                    MaskAllLeaves(childObj);
                }
                else
                {
                    json[key] = "(sensitive)";
                }
            }
            else if (sensitiveValue is ScriptObject sensitiveChild && json[key] is ScriptObject jsonChild)
            {
                // Recurse into nested structure
                MaskSensitiveLeaves(jsonChild, sensitiveChild);
            }
        }
    }

    /// <summary>
    /// Recursively replaces all leaf values in a <see cref="ScriptObject"/> with <c>(sensitive)</c>.
    /// </summary>
    /// <param name="obj">The object to mask completely.</param>
    private static void MaskAllLeaves(ScriptObject obj)
    {
        foreach (var key in obj.GetMembers())
        {
            if (obj[key] is ScriptObject child)
            {
                MaskAllLeaves(child);
            }
            else
            {
                obj[key] = "(sensitive)";
            }
        }
    }
}
