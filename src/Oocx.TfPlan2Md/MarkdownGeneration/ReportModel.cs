using System;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents the data model passed to the Scriban template.
/// </summary>
public class ReportModel
{
    public required string TerraformVersion { get; init; }
    public required string FormatVersion { get; init; }
    public string? Timestamp { get; init; }
    /// <summary>
    /// Optional custom report title provided via the CLI.
    /// Related feature: docs/features/custom-report-title/specification.md
    /// </summary>
    /// <value>
    /// The escaped title text used by templates; null when no custom title is provided so templates can apply defaults.
    /// </value>
    public string? ReportTitle { get; init; }
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }
    public required IReadOnlyList<ModuleChangeGroup> ModuleChanges { get; init; }
    public required SummaryModel Summary { get; init; }

    /// <summary>
    /// Indicates whether unchanged attribute values are included in attribute change tables.
    /// Related feature: docs/features/unchanged-values-cli-option/specification.md
    /// </summary>
    public required bool ShowUnchangedValues { get; init; }

    /// <summary>
    /// Rendering format to use for large attribute values.
    /// Related feature: docs/features/large-attribute-value-display/specification.md
    /// </summary>
    public required LargeValueFormat LargeValueFormat { get; init; }
}

public class ModuleChangeGroup
{
    /// <summary>
    /// The module address (e.g. "module.network.module.subnet"). Empty string represents the root module.
    /// </summary>
    public required string ModuleAddress { get; init; }
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }
}

/// <summary>
/// Breakdown of resource types for a specific action.
/// </summary>
public record ResourceTypeBreakdown(string Type, int Count);

/// <summary>
/// Summary details for a specific action (e.g., add, change).
/// </summary>
public record ActionSummary(int Count, IReadOnlyList<ResourceTypeBreakdown> Breakdown);

/// <summary>
/// Summary of changes in the Terraform plan.
/// </summary>
public class SummaryModel
{
    public required ActionSummary ToAdd { get; init; }
    public required ActionSummary ToChange { get; init; }
    public required ActionSummary ToDestroy { get; init; }
    public required ActionSummary ToReplace { get; init; }
    public required ActionSummary NoOp { get; init; }

    /// <summary>
    /// Total count of resources with changes, excluding no-op resources.
    /// Calculated as: ToAdd.Count + ToChange.Count + ToDestroy.Count + ToReplace.Count.
    /// </summary>
    public int Total { get; init; }
}

/// <summary>
/// Represents a single resource change for template rendering.
/// </summary>
public class ResourceChangeModel
{
    public required string Address { get; init; }
    public string? ModuleAddress { get; set; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string ProviderName { get; init; }
    public required string Action { get; init; }
    public required string ActionSymbol { get; init; }
    public required IReadOnlyList<AttributeChangeModel> AttributeChanges { get; init; }

    /// <summary>
    /// Raw JSON representation of the resource state before the change.
    /// Used by resource-specific templates for semantic diffing.
    /// </summary>
    public object? BeforeJson { get; init; }

    /// <summary>
    /// Raw JSON representation of the resource state after the change.
    /// Used by resource-specific templates for semantic diffing.
    /// </summary>
    public object? AfterJson { get; init; }

    /// <summary>
    /// Paths to attributes that triggered replacement (from Terraform plan replace_paths).
    /// Related feature: docs/features/replacement-reasons-and-summaries/specification.md
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; set; }

    /// <summary>
    /// Human-readable summary of the resource change for quick scanning in templates.
    /// Related feature: docs/features/replacement-reasons-and-summaries/specification.md
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
/// Represents a single attribute change within a resource.
/// </summary>
public class AttributeChangeModel
{
    public required string Name { get; init; }
    public string? Before { get; init; }
    public string? After { get; init; }
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Indicates whether the attribute value should be rendered as a large value block (collapsible section).
    /// Related feature: docs/features/azure-resource-id-formatting/specification.md
    /// </summary>
    public bool IsLarge { get; init; }
}

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <param name="summaryBuilder">Factory for resource summaries; defaults to <see cref="ResourceSummaryBuilder"/>.</param>
/// <param name="showSensitive">Whether to show sensitive values without masking.</param>
/// <param name="showUnchangedValues">Whether unchanged attributes should be included in tables.</param>
/// <param name="largeValueFormat">Rendering format for large values (inline-diff or standard-diff).</param>
/// <param name="reportTitle">Optional custom report title to propagate to templates.</param>
/// <remarks>
/// Related features: docs/features/custom-report-title/specification.md and docs/features/unchanged-values-cli-option/specification.md.
/// </remarks>
public class ReportModelBuilder(IResourceSummaryBuilder? summaryBuilder = null, bool showSensitive = false, bool showUnchangedValues = false, LargeValueFormat largeValueFormat = LargeValueFormat.InlineDiff, string? reportTitle = null)
{
    /// <summary>
    /// Indicates whether sensitive values should be rendered without masking.
    /// </summary>
    private readonly bool _showSensitive = showSensitive;

    /// <summary>
    /// Indicates whether unchanged attribute values should be included in output tables.
    /// </summary>
    private readonly bool _showUnchangedValues = showUnchangedValues;

    /// <summary>
    /// Strategy for building resource summaries used in the report.
    /// </summary>
    private readonly IResourceSummaryBuilder _summaryBuilder = summaryBuilder ?? new ResourceSummaryBuilder();

    /// <summary>
    /// Preferred rendering format for large attribute values.
    /// </summary>
    private readonly LargeValueFormat _largeValueFormat = largeValueFormat;

    /// <summary>
    /// Optional custom report title provided by the user.
    /// </summary>
    private readonly string? _reportTitle = reportTitle;

    /// <summary>
    /// Builds a fully-populated report model from a parsed Terraform plan.
    /// </summary>
    /// <param name="plan">Terraform plan to transform into a report model.</param>
    /// <returns>A model containing change details, summaries, and optional custom title.</returns>
    public ReportModel Build(TerraformPlan plan)
    {
        // Build all resource change models first (for summary counting)
        var allChanges = plan.ResourceChanges
            .Select(BuildResourceChangeModel)
            .ToList();

        // Filter out no-op resources from the changes list passed to the template
        // No-op resources have no meaningful changes to display and including them
        // can cause the template to exceed Scriban's iteration limit of 1000
        var displayChanges = allChanges
            .Where(c => c.Action != "no-op")
            .ToList();

        // Update ResourceChangeModel.ModuleAddress to be empty string when null for consistency
        foreach (var c in displayChanges)
        {
            if (c.ModuleAddress is null)
            {
                c.ModuleAddress = string.Empty;
            }
        }

        var toAdd = BuildActionSummary(allChanges.Where(c => c.Action == "create"));
        var toChange = BuildActionSummary(allChanges.Where(c => c.Action == "update"));
        var toDestroy = BuildActionSummary(allChanges.Where(c => c.Action == "delete"));
        var toReplace = BuildActionSummary(allChanges.Where(c => c.Action == "replace"));
        var noOp = BuildActionSummary(allChanges.Where(c => c.Action == "no-op"));

        var summary = new SummaryModel
        {
            ToAdd = toAdd,
            ToChange = toChange,
            ToDestroy = toDestroy,
            ToReplace = toReplace,
            NoOp = noOp,
            Total = toAdd.Count + toChange.Count + toDestroy.Count + toReplace.Count
        };

        // Group changes by module. Use empty string for root module. Sort so root comes first,
        // then modules in lexicographic order which ensures parents precede children (flat grouping).
        // Preserve the order of modules as they appear in the plan while ensuring the root
        // module is listed first. This keeps child modules next to their parent modules
        // (flat grouping but ordered by appearance).
        var moduleGroups = displayChanges
            .GroupBy(c => c.ModuleAddress ?? string.Empty)
            .Select(g => new
            {
                Key = g.Key,
                Changes = g.ToList(),
                FirstIndex = displayChanges.FindIndex(c => (c.ModuleAddress ?? string.Empty) == g.Key)
            })
            .OrderBy(g => g.Key == string.Empty ? 0 : 1)
            .ThenBy(g => g.FirstIndex)
            .Select(g => new ModuleChangeGroup
            {
                ModuleAddress = g.Key, // empty string represents root
                Changes = g.Changes
            })
            .ToList();

        var escapedReportTitle = _reportTitle is null ? null : ScribanHelpers.EscapeMarkdownHeading(_reportTitle);

        return new ReportModel
        {
            TerraformVersion = plan.TerraformVersion,
            FormatVersion = plan.FormatVersion,
            Timestamp = plan.Timestamp,
            ReportTitle = escapedReportTitle,
            Changes = displayChanges,
            ModuleChanges = moduleGroups,
            Summary = summary,
            ShowUnchangedValues = _showUnchangedValues,
            LargeValueFormat = _largeValueFormat
        };
    }

    private static ActionSummary BuildActionSummary(IEnumerable<ResourceChangeModel> changes)
    {
        var changeList = changes.ToList();

        var breakdown = changeList
            .GroupBy(c => c.Type)
            .Select(g => new ResourceTypeBreakdown(g.Key, g.Count()))
            .OrderBy(b => b.Type, StringComparer.Ordinal)
            .ToList();

        return new ActionSummary(changeList.Count, breakdown);
    }

    private ResourceChangeModel BuildResourceChangeModel(ResourceChange rc)
    {
        var action = DetermineAction(rc.Change.Actions);
        var actionSymbol = GetActionSymbol(action);
        var attributeChanges = BuildAttributeChanges(rc.Change, rc.ProviderName);

        var model = new ResourceChangeModel
        {
            Address = rc.Address,
            ModuleAddress = rc.ModuleAddress,
            Type = rc.Type,
            Name = rc.Name,
            ProviderName = rc.ProviderName,
            Action = action,
            ActionSymbol = actionSymbol,
            AttributeChanges = attributeChanges,
            BeforeJson = rc.Change.Before,
            AfterJson = rc.Change.After,
            ReplacePaths = rc.Change.ReplacePaths
        };

        model.Summary = _summaryBuilder.BuildSummary(model);

        return model;
    }

    private static string DetermineAction(IReadOnlyList<string> actions)
    {
        if (actions.Contains("create") && actions.Contains("delete"))
        {
            return "replace";
        }

        if (actions.Contains("create"))
        {
            return "create";
        }

        if (actions.Contains("delete"))
        {
            return "delete";
        }

        if (actions.Contains("update"))
        {
            return "update";
        }

        return "no-op";
    }

    private static string GetActionSymbol(string action) => action switch
    {
        "create" => "➕",
        "delete" => "❌",
        "update" => "🔄",
        "replace" => "♻️",
        _ => " "
    };

    /// <summary>
    /// Builds attribute changes for a resource, filtering unchanged values when configured.
    /// </summary>
    /// <param name="change">The resource change containing before and after state.</param>
    /// <returns>Attribute changes prepared for rendering.</returns>
    /// <remarks>
    /// Compares raw values before masking to avoid dropping masked sensitive creates that would
    /// otherwise appear unchanged (e.g., "(sensitive)" versus a real value).
    /// Related feature: docs/features/unchanged-values-cli-option/specification.md
    /// </remarks>
    private List<AttributeChangeModel> BuildAttributeChanges(Change change, string providerName)
    {
        var beforeDict = ConvertToFlatDictionary(change.Before);
        var afterDict = ConvertToFlatDictionary(change.After);
        var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
        var afterSensitiveDict = ConvertToFlatDictionary(change.AfterSensitive);

        var allKeys = beforeDict.Keys.Union(afterDict.Keys).OrderBy(k => k);

        var changes = new List<AttributeChangeModel>();

        foreach (var key in allKeys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            var isSensitive = IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);
            var beforeDisplay = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue;
            var afterDisplay = isSensitive && !_showSensitive ? "(sensitive)" : afterValue;

            var valuesEqual = string.Equals(beforeValue, afterValue, StringComparison.Ordinal);

            if (!_showUnchangedValues && valuesEqual)
            {
                continue;
            }

            var isLarge = ScribanHelpers.IsLargeValue(beforeDisplay, providerName)
                || ScribanHelpers.IsLargeValue(afterDisplay, providerName);

            changes.Add(new AttributeChangeModel
            {
                Name = key,
                Before = beforeDisplay,
                After = afterDisplay,
                IsSensitive = isSensitive,
                IsLarge = isLarge
            });
        }

        return changes;
    }

    private static bool IsSensitiveAttribute(string key, Dictionary<string, string?> beforeSensitive, Dictionary<string, string?> afterSensitive)
    {
        // Check if the key is marked as sensitive in either before or after state
        return (beforeSensitive.TryGetValue(key, out var bv) && bv == "true")
            || (afterSensitive.TryGetValue(key, out var av) && av == "true");
    }

    private static Dictionary<string, string?> ConvertToFlatDictionary(object? obj, string prefix = "")
    {
        Dictionary<string, string?> result = [];
        if (obj is null)
        {
            return result;
        }

        if (obj is JsonElement element)
        {
            FlattenJsonElement(element, prefix, result);
        }

        return result;
    }

    private static void FlattenJsonElement(JsonElement element, string prefix, Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, key, result);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}[{index}]";
                    FlattenJsonElement(item, key, result);
                    index++;
                }
                break;
            case JsonValueKind.String:
                result[prefix] = element.GetString();
                break;
            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.GetBoolean().ToString().ToLowerInvariant();
                break;
            case JsonValueKind.Null:
                result[prefix] = null;
                break;
        }
    }
}
