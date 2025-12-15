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
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }
    public required SummaryModel Summary { get; init; }
}

/// <summary>
/// Summary of changes in the Terraform plan.
/// </summary>
public class SummaryModel
{
    public int ToAdd { get; init; }
    public int ToChange { get; init; }
    public int ToDestroy { get; init; }
    public int ToReplace { get; init; }
    public int NoOp { get; init; }
    public int Total { get; init; }
}

/// <summary>
/// Represents a single resource change for template rendering.
/// </summary>
public class ResourceChangeModel
{
    public required string Address { get; init; }
    public string? ModuleAddress { get; init; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string ProviderName { get; init; }
    public required string Action { get; init; }
    public required string ActionSymbol { get; init; }
    public required IReadOnlyList<AttributeChangeModel> AttributeChanges { get; init; }
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
public class ReportModelBuilder
{
    private readonly bool _showSensitive;

    public ReportModelBuilder(bool showSensitive = false)
    {
        _showSensitive = showSensitive;
    }

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

        var summary = new SummaryModel
        {
            ToAdd = allChanges.Count(c => c.Action == "create"),
            ToChange = allChanges.Count(c => c.Action == "update"),
            ToDestroy = allChanges.Count(c => c.Action == "delete"),
            ToReplace = allChanges.Count(c => c.Action == "replace"),
            NoOp = allChanges.Count(c => c.Action == "no-op"),
            Total = allChanges.Count
        };

        return new ReportModel
        {
            TerraformVersion = plan.TerraformVersion,
            FormatVersion = plan.FormatVersion,
            Changes = displayChanges,
            Summary = summary
        };
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
            AttributeChanges = attributeChanges
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
        var result = new List<AttributeChangeModel>();

        var beforeDict = ConvertToFlatDictionary(change.Before);
        var afterDict = ConvertToFlatDictionary(change.After);
        var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
        var afterSensitiveDict = ConvertToFlatDictionary(change.AfterSensitive);

        var allKeys = beforeDict.Keys.Union(afterDict.Keys).OrderBy(k => k);

        foreach (var key in allKeys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            var isSensitive = IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);

            result.Add(new AttributeChangeModel
            {
                Name = key,
                Before = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue,
                After = isSensitive && !_showSensitive ? "(sensitive)" : afterValue,
                IsSensitive = isSensitive
            });
        }

        return result;
    }

    private static bool IsSensitiveAttribute(string key, Dictionary<string, string?> beforeSensitive, Dictionary<string, string?> afterSensitive)
    {
        // Check if the key or any parent key is marked as sensitive
        if (beforeSensitive.TryGetValue(key, out var bv) && bv == "true")
        {
            return true;
        }

        if (afterSensitive.TryGetValue(key, out var av) && av == "true")
        {
            return true;
        }

        return false;
    }

    private static Dictionary<string, string?> ConvertToFlatDictionary(object? obj, string prefix = "")
    {
        var result = new Dictionary<string, string?>();
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
