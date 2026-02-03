using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;
using static Oocx.TfPlan2Md.MarkdownGeneration.ScribanHelpers;

namespace Oocx.TfPlan2Md.Providers.AzureRM.Models;

/// <summary>
/// Applies API Management named value summary overrides and sensitivity corrections.
/// Related feature: docs/features/051-display-enhancements/specification.md.
/// </summary>
internal sealed class AzureRMApimNamedValueFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Attribute name for named value contents.
    /// </summary>
    private const string ValueAttributeName = "value";

    /// <summary>
    /// Attribute name for the named value secret flag.
    /// </summary>
    private const string SecretAttributeName = "secret";

    /// <inheritdoc />
    public void ApplyViewModel(
        ResourceChangeModel model,
        ResourceChange resourceChange,
        string action,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        var state = ResolveActiveState(resourceChange, action);
        model.SummaryHtml = AzureRMApimSummaryBuilder.BuildSubresourceSummaryHtml(model, state);

        OverrideSensitiveValueWhenNotSecret(resourceChange, attributeChanges);
    }

    /// <summary>
    /// Resolves the state object to use for summary generation based on the action.
    /// </summary>
    /// <param name="resourceChange">The resource change.</param>
    /// <param name="action">The normalized action string.</param>
    /// <returns>The state object to summarize.</returns>
    private static object? ResolveActiveState(ResourceChange resourceChange, string action)
    {
        var state = action == "delete" ? resourceChange.Change.Before : resourceChange.Change.After;
        return state ?? resourceChange.Change.After ?? resourceChange.Change.Before;
    }

    /// <summary>
    /// Overrides the masked named value when the resource's secret flag is false.
    /// </summary>
    /// <param name="resourceChange">The resource change data.</param>
    /// <param name="attributeChanges">The attribute changes computed by the core builder.</param>
    private static void OverrideSensitiveValueWhenNotSecret(
        ResourceChange resourceChange,
        IReadOnlyList<AttributeChangeModel> attributeChanges)
    {
        if (!TryGetNamedValueSecret(resourceChange, out var isSecret) || isSecret)
        {
            return;
        }

        if (attributeChanges is not List<AttributeChangeModel> mutableChanges)
        {
            return;
        }

        var valueIndex = mutableChanges.FindIndex(change =>
            string.Equals(change.Name, ValueAttributeName, StringComparison.OrdinalIgnoreCase));

        if (valueIndex < 0)
        {
            return;
        }

        var beforeValues = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.Before);
        var afterValues = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.After);

        beforeValues.TryGetValue(ValueAttributeName, out var beforeValue);
        afterValues.TryGetValue(ValueAttributeName, out var afterValue);

        var isLarge = IsLargeValue(beforeValue, resourceChange.ProviderName)
            || IsLargeValue(afterValue, resourceChange.ProviderName);

        mutableChanges[valueIndex] = new AttributeChangeModel
        {
            Name = ValueAttributeName,
            Before = beforeValue,
            After = afterValue,
            IsSensitive = false,
            IsLarge = isLarge
        };
    }

    /// <summary>
    /// Gets the effective secret flag for an API Management named value.
    /// </summary>
    /// <param name="resourceChange">The resource change.</param>
    /// <param name="isSecret">The resolved secret flag value.</param>
    /// <returns>True when a secret value could be resolved; otherwise false.</returns>
    private static bool TryGetNamedValueSecret(ResourceChange resourceChange, out bool isSecret)
    {
        var beforeValues = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.Before);
        var afterValues = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.After);

        if (TryGetBooleanValue(afterValues, SecretAttributeName, out isSecret))
        {
            return true;
        }

        if (TryGetBooleanValue(beforeValues, SecretAttributeName, out isSecret))
        {
            return true;
        }

        isSecret = false;
        return false;
    }

    /// <summary>
    /// Attempts to parse a boolean value from a flattened dictionary.
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
}
