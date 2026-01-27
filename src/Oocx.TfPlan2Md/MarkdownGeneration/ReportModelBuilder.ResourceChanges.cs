using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    private ResourceChangeModel BuildResourceChangeModel(ResourceChange rc)
    {
        var action = DetermineAction(rc.Change.Actions);
        var actionSymbol = GetActionSymbol(action);
        var attributeChanges = BuildAttributeChanges(rc.Change, rc.ProviderName, rc.Type);

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

        // Apply resource-specific view model if a factory is registered for this type
        if (_viewModelFactoryRegistry.TryGetFactory(rc.Type, out var factory) && factory is not null)
        {
            factory.ApplyViewModel(model, rc, action, attributeChanges);
        }

        model.Summary = _summaryBuilder.BuildSummary(model);
        if (string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            model.ChangedAttributesSummary = BuildChangedAttributesSummary(model.AttributeChanges, model.Action);
        }
        model.TagsBadges = BuildTagsBadges(model.AfterJson, model.BeforeJson, model.Action);
        model.SummaryHtml = BuildSummaryHtml(model);

        return model;
    }

    /// <summary>
    /// Builds attribute changes for a resource, filtering unchanged values when configured.
    /// </summary>
    /// <param name="change">The resource change containing before and after state.</param>
    /// <param name="providerName">The provider name for the resource (e.g., "azurerm", "aws").</param>
    /// <param name="resourceType">The Terraform resource type name.</param>
    /// <returns>Attribute changes prepared for rendering.</returns>
    /// <remarks>
    /// Compares raw values before masking to avoid dropping masked sensitive creates that would
    /// otherwise appear unchanged (e.g., "(sensitive)" versus a real value).
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// </remarks>
    private List<AttributeChangeModel> BuildAttributeChanges(Change change, string providerName, string resourceType)
    {
        var beforeDict = ConvertToFlatDictionary(change.Before);
        var afterDict = ConvertToFlatDictionary(change.After);
        var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
        var afterSensitiveDict = ConvertToFlatDictionary(change.AfterSensitive);

        var allKeys = beforeDict.Keys.Union(afterDict.Keys).Order();

        var changes = new List<AttributeChangeModel>();

        foreach (var key in allKeys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            var isSensitive = IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);
            if (IsNamedValueNonSecret(resourceType, key, beforeDict, afterDict))
            {
                isSensitive = false;
            }
            var beforeDisplay = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue;
            var afterDisplay = isSensitive && !_showSensitive ? "(sensitive)" : afterValue;

            var valuesEqual = string.Equals(beforeValue, afterValue, StringComparison.Ordinal);

            if (!_showUnchangedValues && valuesEqual)
            {
                continue;
            }

            var isLarge = IsLargeValue(beforeDisplay, providerName)
                || IsLargeValue(afterDisplay, providerName);

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

    private static bool IsSensitiveAttribute(
        string key,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
    {
        // Check if the key is marked as sensitive in either before or after state
        return (beforeSensitive.TryGetValue(key, out var bv) && bv == "true")
            || (afterSensitive.TryGetValue(key, out var av) && av == "true");
    }

    /// <summary>
    /// Determines whether an API Management named value should be treated as non-sensitive based on the secret flag.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="resourceType">The resource type name.</param>
    /// <param name="attributeName">The attribute name being evaluated.</param>
    /// <param name="beforeValues">Flattened values before the change.</param>
    /// <param name="afterValues">Flattened values after the change.</param>
    /// <returns>True when the named value is marked as non-secret and should not be masked.</returns>
    private static bool IsNamedValueNonSecret(
        string resourceType,
        string attributeName,
        Dictionary<string, string?> beforeValues,
        Dictionary<string, string?> afterValues)
    {
        if (!resourceType.Equals("azurerm_api_management_named_value", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!attributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryGetNamedValueSecret(beforeValues, afterValues, out var isSecret))
        {
            return false;
        }

        return !isSecret;
    }

    /// <summary>
    /// Gets the effective secret flag for an API Management named value.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="beforeValues">Flattened values before the change.</param>
    /// <param name="afterValues">Flattened values after the change.</param>
    /// <param name="isSecret">The resolved secret flag value.</param>
    /// <returns>True when a secret value could be resolved; otherwise false.</returns>
    private static bool TryGetNamedValueSecret(
        Dictionary<string, string?> beforeValues,
        Dictionary<string, string?> afterValues,
        out bool isSecret)
    {
        if (TryGetBooleanValue(afterValues, "secret", out isSecret))
        {
            return true;
        }

        if (TryGetBooleanValue(beforeValues, "secret", out isSecret))
        {
            return true;
        }

        isSecret = false;
        return false;
    }

    /// <summary>
    /// Attempts to parse a boolean value from a flattened dictionary.
    /// Related feature: docs/features/051-display-enhancements/specification.md.
    /// </summary>
    /// <param name="values">Flattened values from Terraform state.</param>
    /// <param name="key">The key to inspect.</param>
    /// <param name="value">The parsed boolean value.</param>
    /// <returns>True when the value was found and parsed successfully.</returns>
    private static bool TryGetBooleanValue(Dictionary<string, string?> values, string key, out bool value)
    {
        if (values.TryGetValue(key, out var rawValue) && bool.TryParse(rawValue, out value))
        {
            return true;
        }

        value = false;
        return false;
    }

    private static Dictionary<string, string?> ConvertToFlatDictionary(object? obj, string prefix = "") =>
        Helpers.JsonFlattener.ConvertToFlatDictionary(obj, prefix);

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
}
