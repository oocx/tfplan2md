using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.Parsing;

namespace Oocx.TfPlan2Md.Providers.MsGraph.Models;

/// <summary>
/// Classifies Microsoft Graph provider settings independently of the primary Terraform diff.
/// Related feature: docs/features/146-generic-resource-review-clarity/specification.md.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1506:Avoid excessive class coupling",
    Justification = "The provider boundary translates raw Terraform state through shared sensitivity and computed-value helpers into the resource view model.")]
internal sealed class MsGraphResourceViewModelFactory : IResourceViewModelFactory
{
    /// <summary>
    /// Root provider and identity values that do not prove an imported Graph resource has useful prior state.
    /// </summary>
    private static readonly ImmutableHashSet<string> ImportHousekeepingRootPaths = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "id",
        "api_version",
        "url",
        "ignore_missing_property");

    /// <summary>
    /// Graph settings displayed in the provider-owned secondary section.
    /// </summary>
    private static readonly string[] SettingNames = ["api_version", "url", "ignore_missing_property"];

    /// <inheritdoc />
    public void ApplyViewModel(ApplyViewModelContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resourceChange = context.ResourceChange;
        context.Model.IsImportedWithEmptyPriorState = context.Model.ImportId is not null
            && !MeaningfulStateWalker.HasMeaningfulState(resourceChange.Change.Before, ImportHousekeepingRootPaths);

        var before = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.Before);
        var after = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.After);
        var beforeSensitive = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.BeforeSensitive);
        var afterSensitive = JsonFlattener.ConvertToFlatDictionary(resourceChange.Change.AfterSensitive);
        var currentAttributes = context.AttributeChanges.ToDictionary(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase);
        var secondaryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var secondaryAttributes = new List<AttributeChangeModel>();

        foreach (var settingName in SettingNames)
        {
            var hasBefore = before.TryGetValue(settingName, out var beforeValue);
            var hasAfter = after.TryGetValue(settingName, out var afterValue);
            var isUnknownAfter = AfterUnknownHelper.IsAttributeUnknownAfterApply(resourceChange.Change.AfterUnknown, settingName);

            if (!ShouldMoveToSecondary(
                context.Action,
                context.Model.IsImportedWithEmptyPriorState,
                hasBefore,
                beforeValue,
                hasAfter,
                afterValue,
                isUnknownAfter))
            {
                continue;
            }

            currentAttributes.TryGetValue(settingName, out var existingAttribute);
            var attribute = existingAttribute
                ?? CreateSecondaryAttribute(context, settingName, hasBefore, beforeValue, hasAfter, afterValue, beforeSensitive, afterSensitive, isUnknownAfter);

            secondaryNames.Add(settingName);
            secondaryAttributes.Add(attribute);
        }

        if (context.AttributeChanges is List<AttributeChangeModel> mutableAttributes)
        {
            mutableAttributes.RemoveAll(attribute => secondaryNames.Contains(attribute.Name));
        }

        context.Model.SecondaryAttributeChanges = secondaryAttributes;
    }

    /// <summary>
    /// Determines whether desired provider metadata belongs outside the primary diff.
    /// </summary>
    /// <param name="action">The Terraform action.</param>
    /// <param name="isEmptyImport">Whether the prior import state contains only housekeeping values.</param>
    /// <param name="hasBefore">Whether the setting exists in prior state.</param>
    /// <param name="beforeValue">The prior setting value.</param>
    /// <param name="hasAfter">Whether the setting exists in desired state.</param>
    /// <param name="afterValue">The desired setting value.</param>
    /// <param name="isUnknownAfter">Whether Terraform marks the desired setting unknown.</param>
    /// <returns><see langword="true"/> when the setting should be presented as secondary context.</returns>
    private static bool ShouldMoveToSecondary(
        string action,
        bool isEmptyImport,
        bool hasBefore,
        string? beforeValue,
        bool hasAfter,
        string? afterValue,
        bool isUnknownAfter)
    {
        if (!hasAfter)
        {
            return false;
        }

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase) || isEmptyImport)
        {
            return true;
        }

        return hasBefore
            && !isUnknownAfter
            && string.Equals(beforeValue, afterValue, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a display-ready model when core change rows omitted an unchanged value.
    /// </summary>
    /// <param name="context">The provider factory context.</param>
    /// <param name="settingName">The setting path.</param>
    /// <param name="hasBefore">Whether prior state contains the setting.</param>
    /// <param name="beforeValue">The raw prior value.</param>
    /// <param name="hasAfter">Whether desired state contains the setting.</param>
    /// <param name="afterValue">The raw desired value.</param>
    /// <param name="beforeSensitive">Flattened prior sensitivity paths.</param>
    /// <param name="afterSensitive">Flattened desired sensitivity paths.</param>
    /// <param name="isUnknownAfter">Whether Terraform marks the desired setting unknown.</param>
    /// <returns>The secondary attribute change.</returns>
    private static AttributeChangeModel CreateSecondaryAttribute(
        ApplyViewModelContext context,
        string settingName,
        bool hasBefore,
        string? beforeValue,
        bool hasAfter,
        string? afterValue,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive,
        bool isUnknownAfter)
    {
        var isSensitive = SensitivityHelper.IsSensitiveAttribute(settingName, beforeSensitive, afterSensitive);
        var beforeDisplay = hasBefore ? beforeValue : null;
        var afterDisplay = hasAfter ? afterValue : null;

        if (isSensitive && !context.ShowSensitive)
        {
            if (hasBefore)
            {
                beforeDisplay = "(sensitive)";
            }

            if (hasAfter)
            {
                afterDisplay = "(sensitive)";
            }
        }

        if (isUnknownAfter && hasAfter)
        {
            var knownAfterApply = ResolveKnownAfterApplyLabel(context.Model, settingName);
            beforeDisplay = isSensitive ? "(sensitive)" : beforeDisplay;
            afterDisplay = isSensitive ? $"🔒{knownAfterApply}" : knownAfterApply;
        }

        return new AttributeChangeModel
        {
            Name = settingName,
            Before = beforeDisplay,
            After = afterDisplay,
            IsSensitive = isSensitive,
            IsLarge = MarkdownHelpers.IsLargeValue(beforeDisplay, context.ResourceChange.ProviderName)
                || MarkdownHelpers.IsLargeValue(afterDisplay, context.ResourceChange.ProviderName)
        };
    }

    /// <summary>
    /// Resolves an existing configuration reference for a computed provider setting.
    /// </summary>
    /// <param name="model">The shared resource model containing references.</param>
    /// <param name="settingName">The root setting name.</param>
    /// <returns>A known-after-apply label with optional reference context.</returns>
    private static string ResolveKnownAfterApplyLabel(ResourceChangeModel model, string settingName)
    {
        if (model.ConfigurationReferences.TryGetValue(settingName, out var references))
        {
            var selectedReference = ReferenceSelector.SelectBestReference(references);
            if (!string.IsNullOrWhiteSpace(selectedReference))
            {
                return $"(known after apply: {selectedReference})";
            }
        }

        return "(known after apply)";
    }
}
