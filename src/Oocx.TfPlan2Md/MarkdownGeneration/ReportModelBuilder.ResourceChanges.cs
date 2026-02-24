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

    private ResourceChangeModel BuildResourceChangeModel(ResourceChange rc)
    {
        var action = DetermineAction(rc.Change.Actions);
        var actionSymbol = GetActionSymbol(action);
        var attributeChanges = BuildAttributeChanges(rc.Change, rc.ProviderName);
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
            ReplacePaths = rc.Change.ReplacePaths,
            ImportId = importId,
            MovedFromAddress = movedFromAddress,
            IsRefactoringAlreadyApplied = isRefactoringAlreadyApplied,
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
    /// The display string used for attributes whose final value will only be known after Terraform applies the plan.
    /// Matches the format used by <c>terraform show</c> for consistency.
    /// </summary>
    /// <remarks>
    /// Related issue: docs/issues/575-azuread-group-member-empty-rendering/analysis.md.
    /// </remarks>
    private const string KnownAfterApplyDisplay = "(known after apply)";

    /// <summary>
    /// Builds attribute changes for a resource, filtering unchanged values when configured.
    /// </summary>
    /// <param name="change">The resource change containing before and after state.</param>
    /// <param name="providerName">The provider name for the resource (e.g., "azurerm", "aws").</param>
    /// <returns>Attribute changes prepared for rendering.</returns>
    /// <remarks>
    /// Compares raw values before masking to avoid dropping masked sensitive creates that would
    /// otherwise appear unchanged (e.g., "(sensitive)" versus a real value).
    /// <para>
    /// Attributes marked as "known after apply" in <c>after_unknown</c> are included using
    /// <see cref="KnownAfterApplyDisplay"/> as their display value, even when <c>after</c> is
    /// <see langword="null"/>. This covers resources like <c>azuread_group_member</c> where all
    /// attribute values are computed at apply time.
    /// </para>
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// Related issue: docs/issues/575-azuread-group-member-empty-rendering/analysis.md.
    /// </remarks>
    private List<AttributeChangeModel> BuildAttributeChanges(Change change, string providerName)
    {
        var beforeDict = ConvertToFlatDictionary(change.Before);
        var afterDict = ConvertToFlatDictionary(change.After);
        var afterUnknownDict = ConvertToFlatDictionary(change.AfterUnknown);
        var beforeSensitiveDict = ConvertToFlatDictionary(change.BeforeSensitive);
        var afterSensitiveDict = ConvertToFlatDictionary(change.AfterSensitive);

        // Include keys from after_unknown so attributes that are computed at apply time
        // are not silently omitted when `after` is null or contains null values.
        var allKeys = beforeDict.Keys
            .Union(afterDict.Keys)
            .Union(afterUnknownDict.Keys)
            .Order();

        var changes = new List<AttributeChangeModel>();

        foreach (var key in allKeys)
        {
            beforeDict.TryGetValue(key, out var beforeValue);
            afterDict.TryGetValue(key, out var afterValue);

            // An attribute is "known after apply" when after_unknown contains a "true" entry for it.
            var isUnknown = afterUnknownDict.TryGetValue(key, out var unknownFlag)
                && string.Equals(unknownFlag, "true", StringComparison.OrdinalIgnoreCase);

            var isSensitive = IsSensitiveAttribute(key, beforeSensitiveDict, afterSensitiveDict);
            var beforeDisplay = isSensitive && !_showSensitive ? "(sensitive)" : beforeValue;

            // Unknown attributes override the after display value regardless of sensitivity.
            string? afterDisplay;
            if (isUnknown)
            {
                afterDisplay = KnownAfterApplyDisplay;
            }
            else if (isSensitive && !_showSensitive)
            {
                afterDisplay = "(sensitive)";
            }
            else
            {
                afterDisplay = afterValue;
            }

            // Treat "known after apply" as a meaningful change so it is never filtered out by
            // the unchanged-values check, even when before is also null (e.g., create actions).
            var valuesEqual = !isUnknown && string.Equals(beforeValue, afterValue, StringComparison.Ordinal);

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
