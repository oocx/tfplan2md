using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Represents the data model passed to the Scriban template.
/// </summary>
public class ReportModel
{
    /// <summary>
    /// Gets the Terraform version that created the plan.
    /// </summary>
    public required string TerraformVersion { get; init; }

    /// <summary>
    /// Gets the Terraform plan format version.
    /// </summary>
    public required string FormatVersion { get; init; }

    /// <summary>
    /// Gets the tfplan2md semantic version used to generate the report.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required string TfPlan2MdVersion { get; init; }

    /// <summary>
    /// Gets the Short git commit hash (7 characters) of the tfplan2md build used for rendering.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required string CommitHash { get; init; }

    /// <summary>
    /// Gets the UTC timestamp captured when the report was generated.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether the metadata line should be hidden in the rendered report.
    /// Related feature: docs/features/029-report-presentation-enhancements/specification.md.
    /// </summary>
    public required bool HideMetadata { get; init; }

    /// <summary>
    /// Gets the timestamp string from the plan (if available).
    /// </summary>
    public string? Timestamp { get; init; }
    /// <summary>
    /// Gets the optional custom report title provided via the CLI.
    /// Related feature: docs/features/020-custom-report-title/specification.md.
    /// </summary>
    /// <value>
    /// The escaped title text used by templates; null when no custom title is provided so templates can apply defaults.
    /// </value>
    public string? ReportTitle { get; init; }

    /// <summary>
    /// Gets the list of all resource changes in the plan.
    /// </summary>
    public required IReadOnlyList<ResourceChangeModel> Changes { get; init; }

    /// <summary>
    /// Gets resource changes organized by module.
    /// </summary>
    public required IReadOnlyList<ModuleChangeGroup> ModuleChanges { get; init; }

    /// <summary>
    /// Gets the summary statistics for the plan.
    /// </summary>
    public required SummaryModel Summary { get; init; }

    /// <summary>
    /// Gets a value indicating whether unchanged attribute values are included in attribute change tables.
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// </summary>
    public required bool ShowUnchangedValues { get; init; }

    /// <summary>
    /// Gets the Rendering format to use for large attribute values.
    /// Related feature: docs/features/006-large-attribute-value-display/specification.md.
    /// </summary>
    public required LargeValueFormat LargeValueFormat { get; init; }
}

/// <summary>
/// Groups resource changes by module for hierarchical presentation.
/// </summary>
public class ModuleChangeGroup
{
    /// <summary>
    /// Gets the the module address (e.g. "module.network.module.subnet"). Empty string represents the root module.
    /// </summary>
    public required string ModuleAddress { get; init; }

    /// <summary>
    /// Gets the list of resource changes within this module.
    /// </summary>
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
    /// <summary>
    /// Gets the summary of resources to be added.
    /// </summary>
    public required ActionSummary ToAdd { get; init; }

    /// <summary>
    /// Gets the summary of resources to be changed in place.
    /// </summary>
    public required ActionSummary ToChange { get; init; }

    /// <summary>
    /// Gets the summary of resources to be destroyed.
    /// </summary>
    public required ActionSummary ToDestroy { get; init; }

    /// <summary>
    /// Gets the summary of resources to be replaced.
    /// </summary>
    public required ActionSummary ToReplace { get; init; }

    /// <summary>
    /// Gets the summary of resources with no changes.
    /// </summary>
    public required ActionSummary NoOp { get; init; }

    /// <summary>
    /// Gets the Total count of resources with changes, excluding no-op resources.
    /// Calculated as: ToAdd.Count + ToChange.Count + ToDestroy.Count + ToReplace.Count.
    /// </summary>
    public int Total { get; init; }
}

/// <summary>
/// Represents a single resource change for template rendering.
/// </summary>
public class ResourceChangeModel
{
    /// <summary>
    /// Gets the full Terraform address of the resource.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets or sets the module address containing this resource.
    /// </summary>
    public string? ModuleAddress { get; set; }

    /// <summary>
    /// Gets the resource type (e.g., "aws_s3_bucket").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the resource name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provider name (e.g., "aws", "azurerm").
    /// </summary>
    public required string ProviderName { get; init; }

    /// <summary>
    /// Gets the action being performed (e.g., "create", "update", "delete").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets the symbol representing the action (e.g., "+", "~", "-").
    /// </summary>
    public required string ActionSymbol { get; init; }

    /// <summary>
    /// Gets the list of attribute changes for this resource.
    /// </summary>
    public required IReadOnlyList<AttributeChangeModel> AttributeChanges { get; init; }

    /// <summary>
    /// Gets the raw JSON representation of the resource state before the change.
    /// Used by resource-specific templates for semantic diffing.
    /// </summary>
    public object? BeforeJson { get; init; }

    /// <summary>
    /// Gets the raw JSON representation of the resource state after the change.
    /// Used by resource-specific templates for semantic diffing.
    /// </summary>
    public object? AfterJson { get; init; }

    /// <summary>
    /// Gets or sets the paths to attributes that triggered replacement (from Terraform plan replace_paths).
    /// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object>>? ReplacePaths { get; set; }

    /// <summary>
    /// Gets or sets the human-readable summary of the resource change for quick scanning in templates.
    /// Related feature: docs/features/010-replacement-reasons-and-summaries/specification.md.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the precomputed HTML summary line content for rich summary rendering (includes action, type, name, and context values with HTML code spans).
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    public string? SummaryHtml { get; set; }

    /// <summary>
    /// Gets or sets the precomputed changed-attributes summary for update operations (e.g., "2 🔧 attr1, attr2"). Empty for non-update actions.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    public string? ChangedAttributesSummary { get; set; }

    /// <summary>
    /// Gets or sets the precomputed tags badge string for create/delete actions (e.g., "**🏷️ Tags:** `env: prod` `owner: ops`"). Null when no tags or on updates.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    public string? TagsBadges { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azurerm_network_security_group resources.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    public NetworkSecurityGroupViewModel? NetworkSecurityGroup { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azurerm_firewall_network_rule_collection resources.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    public FirewallNetworkRuleCollectionViewModel? FirewallNetworkRuleCollection { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azurerm_role_assignment resources.
    /// Related feature: docs/features/026-template-rendering-simplification/specification.md.
    /// </summary>
    public RoleAssignmentViewModel? RoleAssignment { get; set; }

    /// <summary>
    /// Gets or sets the precomputed view model for azuredevops_variable_group resources.
    /// Related feature: docs/features/039-azdo-variable-group-template/specification.md.
    /// </summary>
    public VariableGroupViewModel? VariableGroup { get; set; }
}

/// <summary>
/// Represents a single attribute change within a resource.
/// </summary>
public class AttributeChangeModel
{
    /// <summary>
    /// Gets the name of the attribute that changed.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the value before the change, or null if the attribute is being added.
    /// </summary>
    public string? Before { get; init; }

    /// <summary>
    /// Gets the value after the change, or null if the attribute is being removed.
    /// </summary>
    public string? After { get; init; }

    /// <summary>
    /// Gets a value indicating whether this attribute contains sensitive data.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the attribute value should be rendered as a large value block (collapsible section).
    /// Related feature: docs/features/019-azure-resource-id-formatting/specification.md.
    /// </summary>
    public bool IsLarge { get; init; }
}

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <param name="summaryBuilder">Factory for resource summaries; defaults to <see cref="ResourceSummaryBuilder"/>.</param>
/// <param name="showSensitive">Whether to show sensitive values without masking.</param>
/// <param name="showUnchangedValues">Whether unchanged attributes should be included in tables.</param>
/// <param name="largeValueFormat">Rendering format for large values (inline-diff or simple-diff).</param>
/// <param name="reportTitle">Optional custom report title to propagate to templates.</param>
/// <param name="principalMapper">Optional mapper for resolving principal names in role assignments.</param>
/// <param name="metadataProvider">Provider for tfplan2md version, commit, and generation timestamp metadata.</param>
/// <param name="hideMetadata">Whether the metadata line should be suppressed in the rendered report.</param>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
public class ReportModelBuilder(IResourceSummaryBuilder? summaryBuilder = null, bool showSensitive = false, bool showUnchangedValues = false, LargeValueFormat largeValueFormat = LargeValueFormat.InlineDiff, string? reportTitle = null, Azure.IPrincipalMapper? principalMapper = null, IMetadataProvider? metadataProvider = null, bool hideMetadata = false)
{
    /// <summary>
    /// Non-breaking space used to keep semantic icons attached to their labels in markdown output.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    private const string NonBreakingSpace = ScribanHelpers.NonBreakingSpace;

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
    /// Mapper for resolving principal names in role assignments.
    /// </summary>
    private readonly Azure.IPrincipalMapper _principalMapper = principalMapper ?? new Azure.NullPrincipalMapper();

    /// <summary>
    /// Provider for tfplan2md build metadata used in the report header.
    /// </summary>
    private readonly IMetadataProvider _metadataProvider = metadataProvider ?? new AssemblyMetadataProvider();

    /// <summary>
    /// Indicates whether metadata should be hidden from the rendered report.
    /// </summary>
    private readonly bool _hideMetadata = hideMetadata;

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
        var metadata = _metadataProvider.GetMetadata();

        return new ReportModel
        {
            TerraformVersion = plan.TerraformVersion,
            FormatVersion = plan.FormatVersion,
            TfPlan2MdVersion = metadata.Version,
            CommitHash = metadata.CommitHash,
            GeneratedAtUtc = metadata.GeneratedAtUtc,
            HideMetadata = _hideMetadata,
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

        if (string.Equals(rc.Type, "azurerm_network_security_group", StringComparison.OrdinalIgnoreCase))
        {
            model.NetworkSecurityGroup = NetworkSecurityGroupViewModelFactory.Build(rc, rc.ProviderName, _largeValueFormat);
        }
        else if (string.Equals(rc.Type, "azurerm_firewall_network_rule_collection", StringComparison.OrdinalIgnoreCase))
        {
            model.FirewallNetworkRuleCollection = FirewallNetworkRuleCollectionViewModelFactory.Build(rc, rc.ProviderName, _largeValueFormat);
        }
        else if (string.Equals(rc.Type, "azurerm_role_assignment", StringComparison.OrdinalIgnoreCase))
        {
            model.RoleAssignment = RoleAssignmentViewModelFactory.Build(rc, action, attributeChanges, _principalMapper);
        }
        else if (string.Equals(rc.Type, "azuredevops_variable_group", StringComparison.OrdinalIgnoreCase))
        {
            model.VariableGroup = VariableGroupViewModelFactory.Build(rc, rc.ProviderName, _largeValueFormat);
        }

        model.Summary = _summaryBuilder.BuildSummary(model);
        model.ChangedAttributesSummary = BuildChangedAttributesSummary(model.AttributeChanges, model.Action);
        model.TagsBadges = BuildTagsBadges(model.AfterJson, model.BeforeJson, model.Action);
        model.SummaryHtml = BuildSummaryHtml(model);

        return model;
    }

    /// <summary>
    /// Builds a summary-safe HTML string for use inside summary elements, including action icon, type, name, location, address space, and changed attributes.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="model">Resource change model containing the source data.</param>
    /// <returns>HTML string safe for use inside a summary element.</returns>
    private static string BuildSummaryHtml(ResourceChangeModel model)
    {
        var state = model.AfterJson ?? model.BeforeJson;
        var flatState = ConvertToFlatDictionary(state);

        flatState.TryGetValue("name", out var nameValue);
        flatState.TryGetValue("resource_group_name", out var resourceGroup);
        flatState.TryGetValue("location", out var location);
        flatState.TryGetValue("address_space[0]", out var addressSpace);

        var prefix = $"{model.ActionSymbol}{NonBreakingSpace}{model.Type} <b>{ScribanHelpers.FormatCodeSummary(model.Name)}</b>";

        var detailParts = new List<string>();

        var primaryContext = !string.IsNullOrWhiteSpace(nameValue) ? ScribanHelpers.FormatAttributeValueSummary("name", nameValue!, null) : null;

        if (!string.IsNullOrWhiteSpace(resourceGroup))
        {
            var groupText = ScribanHelpers.FormatAttributeValueSummary("resource_group_name", resourceGroup!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} in {groupText}" : groupText;
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var locationText = ScribanHelpers.FormatAttributeValueSummary("location", location!, null);
            primaryContext = primaryContext != null ? $"{primaryContext} {locationText}" : locationText;
        }

        if (primaryContext != null)
        {
            detailParts.Add(primaryContext);
        }

        if (!string.IsNullOrWhiteSpace(addressSpace))
        {
            detailParts.Add(ScribanHelpers.FormatAttributeValueSummary("address_space[0]", addressSpace!, null));
        }

        if (!string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            detailParts.Add($"| {model.ChangedAttributesSummary!}");
        }

        return detailParts.Count == 0
            ? prefix
            : $"{prefix} — {string.Join(" ", detailParts)}";
    }

    /// <summary>
    /// Builds a concise changed-attributes summary for update operations (e.g., "2 🔧 attr1, attr2, +N more").
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="attributeChanges">Attribute changes for the resource.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Formatted summary or empty string when not applicable.</returns>
    private static string BuildChangedAttributesSummary(IReadOnlyList<AttributeChangeModel> attributeChanges, string action)
    {
        if (!string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (attributeChanges.Count == 0)
        {
            return string.Empty;
        }

        var names = attributeChanges.Select(a => a.Name).ToList();
        var displayedNames = names.Take(3).ToList();
        var remaining = names.Count - displayedNames.Count;

        var nameList = string.Join(", ", displayedNames);
        if (remaining > 0)
        {
            nameList += $", +{remaining} more";
        }

        return $"{names.Count}🔧{NonBreakingSpace}{nameList}";
    }

    /// <summary>
    /// Builds inline tag badges for create/delete operations, keeping templates free from tag formatting logic.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    /// <param name="after">After-state JSON for the resource.</param>
    /// <param name="before">Before-state JSON for the resource.</param>
    /// <param name="action">Terraform action derived from the plan.</param>
    /// <returns>Formatted tags badge string or null when no tags or not applicable.</returns>
    private static string? BuildTagsBadges(object? after, object? before, string action)
    {
        if (!string.Equals(action, "create", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var state = string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase) ? before : after;
        var flat = ConvertToFlatDictionary(state);

        var tags = flat.Where(kvp => kvp.Key.StartsWith("tags.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => new { Key = kvp.Key[5..], Value = kvp.Value })
            .ToList();

        if (tags.Count == 0)
        {
            return null;
        }

        var badges = tags.Select(tag => ScribanHelpers.FormatCodeTable($"{tag.Key}: {tag.Value}"));
        return $"**🏷️{NonBreakingSpace}Tags:** {string.Join(' ', badges)}";
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
    /// <param name="providerName">The provider name for the resource (e.g., "azurerm", "aws").</param>
    /// <returns>Attribute changes prepared for rendering.</returns>
    /// <remarks>
    /// Compares raw values before masking to avoid dropping masked sensitive creates that would
    /// otherwise appear unchanged (e.g., "(sensitive)" versus a real value).
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
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
