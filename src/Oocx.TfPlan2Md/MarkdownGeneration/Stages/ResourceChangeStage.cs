using System;
using System.Collections.Generic;
using System.Linq;
using Oocx.TfPlan2Md.MarkdownGeneration.Helpers;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Parsing;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Stages;

/// <summary>
/// Default implementation of the resource-change construction stage.
/// Related feature: docs/features/110-refactoring-opportunities/specification.md.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Maintainability",
    "CA1506:Avoid excessive class coupling",
    Justification = "This stage is the translation boundary from parsed Terraform resources into report models during the staged pipeline extraction.")]
internal sealed partial class ResourceChangeStage : IResourceChangeStage
{
    /// <summary>
    /// Terraform action value for create operations.
    /// </summary>
    private const string CreateAction = TerraformActions.Create;

    /// <summary>
    /// Terraform action value for delete operations.
    /// </summary>
    private const string DeleteAction = TerraformActions.Delete;

    /// <summary>
    /// Terraform action value for update operations.
    /// </summary>
    private const string UpdateAction = TerraformActions.Update;

    /// <summary>
    /// Terraform action value for read operations.
    /// </summary>
    private const string ReadAction = TerraformActions.Read;

    /// <summary>
    /// Terraform action value for forget operations.
    /// </summary>
    private const string ForgetAction = TerraformActions.Forget;

    /// <summary>
    /// Terraform action value for ephemeral open operations.
    /// </summary>
    private const string OpenAction = TerraformActions.Open;

    /// <summary>
    /// Terraform action value for replace operations.
    /// </summary>
    private const string ReplaceAction = TerraformActions.Replace;

    /// <summary>
    /// Terraform action value for unknown operations.
    /// </summary>
    private const string UnknownAction = TerraformActions.Unknown;

    /// <summary>
    /// Terraform action value for no-op operations.
    /// </summary>
    private const string NoOpAction = TerraformActions.NoOp;

    /// <summary>
    /// Display value used when sensitive values are masked.
    /// </summary>
    private const string SensitiveMask = "(sensitive)";

    /// <summary>
    /// Strategy used to build resource summaries.
    /// </summary>
    private readonly IResourceSummaryBuilder _summaryBuilder;

    /// <summary>
    /// Indicates whether sensitive values should be shown in clear text.
    /// </summary>
    private readonly bool _showSensitive;

    /// <summary>
    /// Indicates whether unchanged attributes should be retained.
    /// </summary>
    private readonly bool _showUnchangedValues;

    /// <summary>
    /// Registry of provider-specific view model factories.
    /// </summary>
    private readonly IResourceViewModelFactoryRegistry _viewModelFactoryRegistry;

    /// <summary>
    /// Principal mapper used by provider-specific factories.
    /// </summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Optional registry of icon providers used by provider-specific factories.
    /// </summary>
    private readonly IconProviderRegistry? _iconProviderRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceChangeStage"/> class.
    /// </summary>
    /// <param name="summaryBuilder">Strategy used to build summaries.</param>
    /// <param name="showSensitive">Whether sensitive values should be shown.</param>
    /// <param name="showUnchangedValues">Whether unchanged attributes should be retained.</param>
    /// <param name="viewModelFactoryRegistry">Registry of provider-specific view model factories.</param>
    /// <param name="principalMapper">Mapper for Azure principal display names.</param>
    /// <param name="iconProviderRegistry">Optional icon provider registry.</param>
    internal ResourceChangeStage(
        IResourceSummaryBuilder summaryBuilder,
        bool showSensitive,
        bool showUnchangedValues,
        IResourceViewModelFactoryRegistry viewModelFactoryRegistry,
        IPrincipalMapper principalMapper,
        IconProviderRegistry? iconProviderRegistry)
    {
        _summaryBuilder = summaryBuilder;
        _showSensitive = showSensitive;
        _showUnchangedValues = showUnchangedValues;
        _viewModelFactoryRegistry = viewModelFactoryRegistry;
        _principalMapper = principalMapper;
        _iconProviderRegistry = iconProviderRegistry;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResourceChangeModel> Build(
        TerraformPlan plan,
        IReadOnlyDictionary<(string Address, string Attribute), IReadOnlyList<string>>? preBuiltReferenceIndex = null)
    {
        var configurationReferenceIndex = preBuiltReferenceIndex
            ?? ConfigurationReferenceResolver.BuildReferenceIndex(plan.Configuration);
        var configurationReferencesByAddress = configurationReferenceIndex
            .GroupBy(e => e.Key.Address, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(e => e.Key.Attribute, e => e.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        // plan.ResourceChanges may be null when the "resource_changes" key is absent from the
        // Terraform plan JSON (e.g., a plan that only modifies output values). System.Text.Json
        // does not enforce non-nullability annotations, so treat null as an empty collection to
        // avoid ArgumentNullException("source") from LINQ.
        // Related issue: docs/issues/113-argument-null-source/analysis.md.
        return (plan.ResourceChanges ?? [])
            .Select(resourceChange => BuildResourceChangeModel(resourceChange, configurationReferencesByAddress))
            .ToList();
    }

    /// <summary>
    /// Builds a view model for a single resource change.
    /// </summary>
    /// <param name="resourceChange">The Terraform resource change.</param>
    /// <param name="configurationReferencesByAddress">Configuration references grouped by normalized resource address.</param>
    /// <returns>The resource change model for downstream report stages.</returns>
    private ResourceChangeModel BuildResourceChangeModel(
        ResourceChange resourceChange,
        IReadOnlyDictionary<string, Dictionary<string, IReadOnlyList<string>>> configurationReferencesByAddress)
    {
        var action = DetermineAction(resourceChange.Change.Actions);
        var actionSymbol = GetActionSymbol(action);
        var normalizedAddress = NormalizeResourceAddressForReferenceLookup(resourceChange.Address);
        var configurationReferences = BuildConfigurationReferencesForResource(normalizedAddress, configurationReferencesByAddress);
        var hasWholeResourceUnknownAfterApply = AfterUnknownHelper.IsWholeResourceUnknownAfterApply(resourceChange.Change.AfterUnknown);
        var attributeChanges = BuildAttributeChanges(resourceChange.Change, resourceChange.ProviderName, configurationReferences);
        var importId = string.IsNullOrWhiteSpace(resourceChange.Change.Importing?.Id) ? null : resourceChange.Change.Importing?.Id;
        var movedFromAddress = string.IsNullOrWhiteSpace(resourceChange.PreviousAddress) ? null : resourceChange.PreviousAddress;
        // Terraform can emit importing.id together with a no-op action before apply for pending
        // import blocks. Treat imports as ready unless the plan exposes stronger evidence that the
        // import is unnecessary, otherwise the report overstates "already imported" warnings.
        // Any future "already imported" detection must rely on explicit Terraform metadata that
        // distinguishes stale import blocks from pending imports without guessing from no-op.
        // Related issue: docs/issues/123-already-imported-false-positive/analysis.md.
        var isImportAlreadyApplied = false;
        var isMoveAlreadyApplied = action == NoOpAction && movedFromAddress is not null;

        var model = new ResourceChangeModel
        {
            Address = resourceChange.Address,
            ModuleAddress = resourceChange.ModuleAddress,
            Type = resourceChange.Type,
            Name = resourceChange.Name,
            ProviderName = resourceChange.ProviderName,
            Action = action,
            ActionSymbol = actionSymbol,
            AttributeChanges = attributeChanges,
            BeforeJson = resourceChange.Change.Before,
            AfterJson = resourceChange.Change.After,
            BeforeSensitive = resourceChange.Change.BeforeSensitive,
            AfterSensitive = resourceChange.Change.AfterSensitive,
            AfterUnknown = resourceChange.Change.AfterUnknown,
            ReplacePaths = resourceChange.Change.ReplacePaths,
            ImportId = importId,
            MovedFromAddress = movedFromAddress,
            IsImportAlreadyApplied = isImportAlreadyApplied,
            IsMoveAlreadyApplied = isMoveAlreadyApplied,
            HasWholeResourceUnknownAfterApply = hasWholeResourceUnknownAfterApply,
            ConfigurationReferences = configurationReferences,
            ResourceChange = resourceChange
        };

        if (_viewModelFactoryRegistry.TryGetFactory(resourceChange.Type, out var factory) && factory is not null)
        {
            factory.ApplyViewModel(new ApplyViewModelContext(model, resourceChange, action, attributeChanges, _principalMapper, _iconProviderRegistry));
        }

        if (string.IsNullOrWhiteSpace(model.Summary))
        {
            model.Summary = _summaryBuilder.BuildSummary(model);
        }

        if (string.IsNullOrWhiteSpace(model.ChangedAttributesSummary))
        {
            model.ChangedAttributesSummary = ResourceSummaryHtmlBuilder.BuildChangedAttributesSummary(model.AttributeChanges, model.Action);
        }

        model.TagsBadges = ResourceSummaryHtmlBuilder.BuildTagsBadges(model.AfterJson, model.BeforeJson, model.Action);

        if (string.IsNullOrWhiteSpace(model.SummaryHtml))
        {
            model.SummaryHtml = ResourceSummaryHtmlBuilder.BuildSummaryHtml(model);
        }

        return model;
    }

    /// <summary>
    /// Builds attribute changes for a resource.
    /// </summary>
    /// <param name="change">The Terraform change payload.</param>
    /// <param name="providerName">The Terraform provider name.</param>
    /// <param name="configurationReferences">Configuration references grouped by top-level attribute.</param>
    /// <returns>The attribute changes prepared for rendering.</returns>
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

            if (!_showUnchangedValues && valuesEqual)
            {
                continue;
            }

            var isLarge = MarkdownHelpers.IsLargeValue(beforeDisplay, providerName)
                || MarkdownHelpers.IsLargeValue(afterDisplay, providerName);

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

}
