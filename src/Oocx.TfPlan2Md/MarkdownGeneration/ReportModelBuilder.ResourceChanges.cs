using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
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
    private const string CreateAction = "create";
    private const string DeleteAction = "delete";
    private const string UpdateAction = "update";
    private const string ReadAction = "read";
    private const string ForgetAction = "forget";
    private const string OpenAction = "open";
    private const string ReplaceAction = "replace";
    private const string UnknownAction = "unknown";
    private const string NoOpAction = "no-op";
    private const string SensitiveMask = "(sensitive)";

    private ResourceChangeModel BuildResourceChangeModel(ResourceChange rc)
    {
        var action = DetermineAction(rc.Change.Actions);
        var actionSymbol = GetActionSymbol(action);
        var normalizedAddress = NormalizeResourceAddressForReferenceLookup(rc.Address);
        var configurationReferences = BuildConfigurationReferencesForResource(normalizedAddress);
        var hasWholeResourceUnknownAfterApply = AfterUnknownHelper.IsWholeResourceUnknownAfterApply(rc.Change.AfterUnknown);
        var attributeChanges = BuildAttributeChanges(rc.Change, rc.ProviderName, configurationReferences);
        var importId = string.IsNullOrWhiteSpace(rc.Change.Importing?.Id) ? null : rc.Change.Importing?.Id;
        var movedFromAddress = string.IsNullOrWhiteSpace(rc.PreviousAddress) ? null : rc.PreviousAddress;
        var isRefactoringAlreadyApplied = action == NoOpAction && (importId is not null || movedFromAddress is not null);

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
            BeforeSensitive = rc.Change.BeforeSensitive,
            AfterSensitive = rc.Change.AfterSensitive,
            AfterUnknown = rc.Change.AfterUnknown,
            ReplacePaths = rc.Change.ReplacePaths,
            ImportId = importId,
            MovedFromAddress = movedFromAddress,
            IsRefactoringAlreadyApplied = isRefactoringAlreadyApplied,
            HasWholeResourceUnknownAfterApply = hasWholeResourceUnknownAfterApply,
            ConfigurationReferences = configurationReferences,
            ResourceChange = rc // Store for mapper access
        };

        // Apply resource-specific view model if a factory is registered for this type
        if (_viewModelFactoryRegistry.TryGetFactory(rc.Type, out var factory) && factory is not null)
        {
            factory.ApplyViewModel(model, rc, action, attributeChanges, _principalMapper, _iconProviderRegistry);
        }

        if (string.IsNullOrWhiteSpace(model.Summary))
        {
            model.Summary = _summaryBuilder.BuildSummary(model);
        }
        if (string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            model.ChangedAttributesSummary = BuildChangedAttributesSummary(model.AttributeChanges, model.Action);
        }
        model.TagsBadges = BuildTagsBadges(model.AfterJson, model.BeforeJson, model.Action);
        if (string.IsNullOrWhiteSpace(model.SummaryHtml))
        {
            model.SummaryHtml = BuildSummaryHtml(model);
        }

        return model;
    }

    /// <summary>
    /// Builds attribute changes for a resource, filtering unchanged values when configured.
    /// </summary>
    /// <param name="change">The resource change containing before and after state.</param>
    /// <param name="providerName">The provider name for the resource (e.g., "azurerm", "aws").</param>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <returns>Attribute changes prepared for rendering.</returns>
    /// <remarks>
    /// Compares raw values before masking to avoid dropping masked sensitive creates that would
    /// otherwise appear unchanged (e.g., "(sensitive)" versus a real value).
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// </remarks>
    private List<AttributeChangeModel> BuildAttributeChanges(
        Change change,
        string providerName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences)
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
            var beforeDisplay = isSensitive && !_showSensitive ? SensitiveMask : beforeValue;
            var afterDisplay = isSensitive && !_showSensitive ? SensitiveMask : afterValue;
            var valuesEqual = string.Equals(beforeValue, afterValue, StringComparison.Ordinal);

            ApplyComputedKnownAfterApplyOverride(
                change,
                configurationReferences,
                key,
                isSensitive,
                ref beforeDisplay,
                ref afterDisplay,
                ref valuesEqual);

            // Azure ID casing-only filter: suppress rows where the registry indicates the change
            // is purely a casing difference (e.g., azurerm resource ID capitalisation noise).
            // This guard comes BEFORE the valuesEqual check so that Azure ID casing rows remain
            // hidden even when --show-unchanged-values is active.
            // Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
            if (_ignoreAzureIdCaseChanges
                && !valuesEqual
                && _attributeChangeFilterRegistry.ShouldSuppress(
                       new Services.AttributeChangeFilterContext(providerName, key, beforeValue, afterValue)))
            {
                continue;
            }

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

    /// <summary>
    /// Applies known-after-apply display overrides for computed attributes.
    /// </summary>
    /// <param name="change">Terraform change object.</param>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <param name="key">Flattened attribute key.</param>
    /// <param name="isSensitive">Whether the attribute is sensitive.</param>
    /// <param name="beforeDisplay">The display before value to update when needed.</param>
    /// <param name="afterDisplay">The display after value to update.</param>
    /// <param name="valuesEqual">Equality flag to update when value should be forced as changed.</param>
    private static void ApplyComputedKnownAfterApplyOverride(
        Change change,
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences,
        string key,
        bool isSensitive,
        ref string? beforeDisplay,
        ref string? afterDisplay,
        ref bool valuesEqual)
    {
        var isUnknownAfterApply = afterDisplay is null || string.Equals(afterDisplay, SensitiveMask, StringComparison.Ordinal);
        isUnknownAfterApply = isUnknownAfterApply
            && AfterUnknownHelper.IsAttributeUnknownAfterApply(change.AfterUnknown, key);
        if (!isUnknownAfterApply)
        {
            return;
        }

        var displayLabel = ResolveKnownAfterApplyLabel(configurationReferences, key);
        if (isSensitive)
        {
            beforeDisplay = SensitiveMask;
            afterDisplay = $"🔒{displayLabel}";
        }
        else
        {
            afterDisplay = displayLabel;
        }

        valuesEqual = false;
    }

    /// <summary>
    /// Builds a reference map for a single resource by grouping configuration references by attribute.
    /// Uses the pre-computed secondary index for O(1) lookup instead of linear scanning.
    /// </summary>
    /// <param name="normalizedAddress">Normalized resource address without instance key.</param>
    /// <returns>Attribute-to-references map for the resource.</returns>
    private Dictionary<string, IReadOnlyList<string>> BuildConfigurationReferencesForResource(string normalizedAddress)
    {
        if (string.IsNullOrWhiteSpace(normalizedAddress) || _configurationReferencesByAddress.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        if (_configurationReferencesByAddress.TryGetValue(normalizedAddress, out var refs))
        {
            return refs;
        }

        return new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the display label for a computed known-after-apply attribute.
    /// </summary>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <param name="flattenedKey">Flattened attribute key.</param>
    /// <returns>A formatted known-after-apply label with optional reference context.</returns>
    private static string ResolveKnownAfterApplyLabel(
        IReadOnlyDictionary<string, IReadOnlyList<string>> configurationReferences,
        string flattenedKey)
    {
        var topLevelAttribute = GetTopLevelAttributeName(flattenedKey);
        if (!string.IsNullOrWhiteSpace(topLevelAttribute)
            && configurationReferences.TryGetValue(topLevelAttribute, out var references)
            && references.Count > 0)
        {
            var selectedReference = ReferenceSelector.SelectBestReference(references);
            if (!string.IsNullOrWhiteSpace(selectedReference))
            {
                return $"(known after apply: {selectedReference})";
            }
        }

        return "(known after apply)";
    }

    /// <summary>
    /// Extracts the top-level attribute name from a flattened key.
    /// </summary>
    /// <param name="flattenedKey">Flattened key such as <c>tags.env</c> or <c>rules[0].priority</c>.</param>
    /// <returns>The top-level attribute name.</returns>
    private static string GetTopLevelAttributeName(string flattenedKey)
    {
        if (string.IsNullOrWhiteSpace(flattenedKey))
        {
            return string.Empty;
        }

        var dotIndex = flattenedKey.IndexOf('.');
        var bracketIndex = flattenedKey.IndexOf('[');

        if (dotIndex < 0 && bracketIndex < 0)
        {
            return flattenedKey;
        }

        if (dotIndex < 0)
        {
            return flattenedKey[..bracketIndex];
        }

        if (bracketIndex < 0)
        {
            return flattenedKey[..dotIndex];
        }

        var endIndex = Math.Min(dotIndex, bracketIndex);
        return flattenedKey[..endIndex];
    }

    /// <summary>
    /// Normalizes resource addresses for configuration lookups by removing instance keys.
    /// </summary>
    /// <param name="address">The resource address to normalize.</param>
    /// <returns>The normalized address without instance keys.</returns>
    private static string NormalizeResourceAddressForReferenceLookup(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return string.Empty;
        }

        if (!address.EndsWith(']'))
        {
            return address;
        }

        var bracketIndex = address.LastIndexOf('[');
        return bracketIndex < 0 ? address : address[..bracketIndex];
    }

    /// <summary>
    /// Checks if an attribute is marked as sensitive by examining the attribute path and all parent paths.
    /// Delegates to <see cref="SensitivityHelper.IsSensitiveAttribute"/> for centralized logic.
    /// </summary>
    /// <param name="key">The attribute path (e.g., "variable[0].secret_value").</param>
    /// <param name="beforeSensitive">Dictionary of sensitive attributes from before state.</param>
    /// <param name="afterSensitive">Dictionary of sensitive attributes from after state.</param>
    /// <returns>True if the attribute or any parent path is marked sensitive.</returns>
    private static bool IsSensitiveAttribute(
        string key,
        Dictionary<string, string?> beforeSensitive,
        Dictionary<string, string?> afterSensitive)
        => SensitivityHelper.IsSensitiveAttribute(key, beforeSensitive, afterSensitive);

    private static Dictionary<string, string?> ConvertToFlatDictionary(object? obj, string prefix = "") =>
        Helpers.JsonFlattener.ConvertToFlatDictionary(obj, prefix);

    /// <summary>
    /// Determines the action type from Terraform's action list.
    /// </summary>
    /// <param name="actions">List of actions from Terraform plan (e.g., ["create"], ["read"], ["no-op"]).</param>
    /// <returns>A normalized action string for use in report generation.</returns>
    /// <remarks>
    /// Explicitly handles the "read" action to prevent false positives in import detection.
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md.
    /// </remarks>
    private static string DetermineAction(IReadOnlyList<string> actions)
    {
        if (actions.Count == 0)
        {
            return NoOpAction;
        }

        if (actions.Contains(CreateAction) && actions.Contains(DeleteAction))
        {
            return ReplaceAction;
        }

        if (actions.Contains(CreateAction) && actions.Contains(ForgetAction))
        {
            return ReplaceAction;
        }

        if (actions.Contains(CreateAction))
        {
            return CreateAction;
        }

        if (actions.Contains(DeleteAction))
        {
            return DeleteAction;
        }

        if (actions.Contains(UpdateAction))
        {
            return UpdateAction;
        }

        if (actions.Contains(ReadAction))
        {
            return ReadAction;
        }

        if (actions.Contains(OpenAction))
        {
            return OpenAction;
        }

        if (actions.Contains(NoOpAction))
        {
            return NoOpAction;
        }

        if (actions.Contains(ForgetAction))
        {
            return ForgetAction;
        }

        Console.Error.WriteLine(
            $"Warning: Encountered unknown Terraform action set: [{string.Join(", ", actions)}]; classifying as '{UnknownAction}'.");

        return UnknownAction;
    }

    /// <summary>
    /// Maps action type to display symbol/icon.
    /// </summary>
    /// <param name="action">The normalized action string.</param>
    /// <returns>An icon/symbol representing the action.</returns>
    /// <remarks>
    /// "read" action uses Add icon as it represents bringing a resource into state (similar to create).
    /// Related issue: docs/issues/464-already-imported-false-positive/analysis.md.
    /// </remarks>
    private static string GetActionSymbol(string action) => action switch
    {
        CreateAction => ActionIcons.Add,
        DeleteAction => ActionIcons.Delete,
        UpdateAction => ActionIcons.Update,
        ReadAction => ActionIcons.Add,
        OpenAction => ActionIcons.Add,
        ForgetAction => ActionIcons.Delete,
        ReplaceAction => ActionIcons.Replace,
        UnknownAction => "⚠️",
        _ => ActionIcons.NoOp
    };
}
