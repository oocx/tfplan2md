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
        var attributeChanges = BuildAttributeChanges(rc.Change, rc.ProviderName, rc.Address);
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
    /// <param name="resourceAddress">The full resource address used to look up configuration references.</param>
    /// <returns>Attribute changes prepared for rendering.</returns>
    /// <remarks>
    /// Compares raw values before masking to avoid dropping masked sensitive creates that would
    /// otherwise appear unchanged (e.g., "(sensitive)" versus a real value).
    /// <para>
    /// Attributes marked as "known after apply" in <c>after_unknown</c> are included using
    /// the most specific configuration reference available (e.g., <c>→ azuread_group.admins.id</c>)
    /// when the plan's configuration block contains expressions for the attribute; otherwise falls
    /// back to <see cref="KnownAfterApplyDisplay"/>. This covers resources like
    /// <c>azuread_group_member</c> where all attribute values are computed at apply time.
    /// </para>
    /// Related feature: docs/features/014-unchanged-values-cli-option/specification.md.
    /// Related issue: docs/issues/575-azuread-group-member-empty-rendering/analysis.md.
    /// </remarks>
    private List<AttributeChangeModel> BuildAttributeChanges(Change change, string providerName, string? resourceAddress = null)
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
                afterDisplay = ResolveUnknownDisplay(resourceAddress, key);
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
    /// Resolves the display string for an attribute that is "known after apply".
    /// Uses the configuration reference index to show the actual source reference (e.g.,
    /// <c>→ azuread_group.admins.id</c> or <c>→ each.value.group_object_id</c>) when available,
    /// falling back to <see cref="KnownAfterApplyDisplay"/> otherwise.
    /// </summary>
    /// <param name="resourceAddress">The full resource instance address (instance key stripped internally).</param>
    /// <param name="attribute">The attribute name to look up.</param>
    /// <returns>A reference display string or the generic known-after-apply placeholder.</returns>
    private string ResolveUnknownDisplay(string? resourceAddress, string attribute)
    {
        if (resourceAddress is not null)
        {
            var baseAddress = StripInstanceKey(resourceAddress);
            if (_configurationReferenceIndex.TryGetValue((baseAddress, attribute), out var references))
            {
                var best = SelectBestReference(references);
                if (best is not null)
                {
                    return $"→ {best}";
                }
            }
        }

        return KnownAfterApplyDisplay;
    }

    /// <summary>
    /// Strips the instance key suffix from a resource address so it matches the configuration block address.
    /// For example, <c>azuread_group_member.users["alice"]</c> becomes <c>azuread_group_member.users</c>.
    /// </summary>
    /// <param name="address">The full resource address, potentially including an instance key.</param>
    /// <returns>The base address without instance key.</returns>
    private static string StripInstanceKey(string address)
    {
        var bracketIndex = address.IndexOf('[', StringComparison.Ordinal);
        return bracketIndex >= 0 ? address[..bracketIndex] : address;
    }

    /// <summary>
    /// Selects the most useful reference from a list of Terraform expression references.
    /// Prioritises full resource references (e.g., <c>azuread_group.admins.id</c>) over
    /// <c>each.value.*</c>, then <c>var.*</c>/<c>local.*</c>, skipping bare meta-arguments
    /// such as <c>each.key</c> and <c>each.value</c>.
    /// </summary>
    /// <param name="references">The candidate references from the configuration expression.</param>
    /// <returns>The best reference string, or <see langword="null"/> when none are useful.</returns>
    private static string? SelectBestReference(IReadOnlyList<string> references)
    {
        // Skip bare meta-arguments that carry no useful information
        static bool IsUseless(string r) =>
            r is "each.key" or "each.value" or "self" or "count.index";

        var useful = references.Where(r => !IsUseless(r)).ToList();
        if (useful.Count == 0)
        {
            return null;
        }

        // Prefer full resource/data references (type.name.attribute format, not meta-arguments)
        var resourceRef = useful.Find(r =>
            !r.StartsWith("each.", StringComparison.Ordinal) &&
            !r.StartsWith("var.", StringComparison.Ordinal) &&
            !r.StartsWith("local.", StringComparison.Ordinal) &&
            !r.StartsWith("path.", StringComparison.Ordinal) &&
            r.Contains('.', StringComparison.Ordinal));
        if (resourceRef is not null)
        {
            return resourceRef;
        }

        // Second choice: each.value.attribute (conveys the attribute name)
        var eachValueRef = useful.Find(r => r.StartsWith("each.value.", StringComparison.Ordinal));
        if (eachValueRef is not null)
        {
            return eachValueRef;
        }

        // Third choice: var.something or local.something
        var varOrLocalRef = useful.Find(r =>
            r.StartsWith("var.", StringComparison.Ordinal) ||
            r.StartsWith("local.", StringComparison.Ordinal));
        if (varOrLocalRef is not null)
        {
            return varOrLocalRef;
        }

        return useful[0];
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
