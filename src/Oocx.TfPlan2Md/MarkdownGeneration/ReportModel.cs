using System;
using System.Text.Json;
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
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }
    public required IReadOnlyList<ModuleChangeGroup> ModuleChanges { get; init; }
    public required SummaryModel Summary { get; init; }
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
}

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
public class ReportModelBuilder(bool showSensitive = false)
{
    private readonly bool _showSensitive = showSensitive;

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

        var summary = new SummaryModel
        {
            ToAdd = BuildActionSummary(allChanges.Where(c => c.Action == "create")),
            ToChange = BuildActionSummary(allChanges.Where(c => c.Action == "update")),
            ToDestroy = BuildActionSummary(allChanges.Where(c => c.Action == "delete")),
            ToReplace = BuildActionSummary(allChanges.Where(c => c.Action == "replace")),
            NoOp = BuildActionSummary(allChanges.Where(c => c.Action == "no-op")),
            Total = allChanges.Count
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

        return new ReportModel
        {
            TerraformVersion = plan.TerraformVersion,
            FormatVersion = plan.FormatVersion,
            Timestamp = plan.Timestamp,
            Changes = displayChanges,
            ModuleChanges = moduleGroups,
            Summary = summary
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
        var attributeChanges = BuildAttributeChanges(rc.Change);

        return new ResourceChangeModel
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
            AfterJson = rc.Change.After
        };
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

    private List<AttributeChangeModel> BuildAttributeChanges(Change change)
    {
        var beforeDict = ConvertToFlatDictionary(change.Before);
        var afterDict = ConvertToFlatDictionary(change.After);
        var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
        var afterSensitiveDict = ConvertToFlatDictionary(change.AfterSensitive);

        var allKeys = beforeDict.Keys.Union(afterDict.Keys).OrderBy(k => k);

        return allKeys.Select(key =>
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            var isSensitive = IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);

            return new AttributeChangeModel
            {
                Name = key,
                Before = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue,
                After = isSensitive && !_showSensitive ? "(sensitive)" : afterValue,
                IsSensitive = isSensitive
            };
        }).ToList();
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
