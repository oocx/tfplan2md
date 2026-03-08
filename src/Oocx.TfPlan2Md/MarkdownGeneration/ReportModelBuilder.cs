using System;
using System.Collections.Generic;
using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Callback invoked after parent-child merging is complete.
/// </summary>
/// <param name="allChanges">All resource changes after parent-child merging.</param>
/// <param name="principalMapper">Optional principal mapper for resolving member types.</param>
/// <remarks>
/// This callback mechanism allows provider-specific logic to run after core merging
/// without introducing dependencies from MarkdownGeneration to Providers.
/// Related issue: docs/issues/059-parent-child-summary-member-counts/analysis.md.
/// </remarks>
internal delegate void ParentPostMergeCallback(
    List<ResourceChangeModel> allChanges,
    IPrincipalMapper? principalMapper);

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportModelBuilder"/> class using grouped option and service records.
    /// </summary>
    /// <param name="options">Scalar configuration options; defaults to <see cref="ReportModelBuilderOptions"/> with all defaults.</param>
    /// <param name="services">Injected service dependencies; defaults to <see cref="ReportModelBuilderServices"/> with all defaults.</param>
    internal ReportModelBuilder(
        ReportModelBuilderOptions? options = null,
        ReportModelBuilderServices? services = null)
    {
        var opts = options ?? new ReportModelBuilderOptions();
        var svcs = services ?? new ReportModelBuilderServices();

        // Resolve provider contributions once to avoid repeated computation across field initialisations.
        var resolvedContributions = CreateProviderContributions(svcs.ProviderContributions, svcs.ProviderRegistry);
        _resolvedProviderContributions = resolvedContributions;

        // Option fields
        _showSensitive = opts.ShowSensitive;
        _showUnchangedValues = opts.ShowUnchangedValues;
        _ignoreAzureIdCaseChanges = opts.IgnoreAzureIdCaseChanges;
        _reportTitle = opts.ReportTitle;
        _hideMetadata = opts.HideMetadata;
        _detailsDisplayMode = opts.DetailsDisplayMode;
        _renderTarget = opts.RenderTarget;
        _largeValueFormat = ConvertRenderTargetToLargeValueFormat(opts.RenderTarget);

        // Service fields
        _summaryBuilder = svcs.SummaryBuilder ?? new ResourceSummaryBuilder();
        _metadataProvider = svcs.MetadataProvider ?? new AssemblyMetadataProvider();
        _principalMapper = svcs.PrincipalMapper ?? new NullPrincipalMapper();
        _codeAnalysisInput = svcs.CodeAnalysisInput;
        _attributeChangeFilterRegistry = svcs.AttributeChangeFilterRegistry ?? new Services.AttributeChangeFilterRegistry();
        _resourceChangeStage = svcs.ResourceChangeStage;
        _attributeFilteringStage = svcs.AttributeFilteringStage;
        _summaryEnrichmentStage = svcs.SummaryEnrichmentStage;
        _displayFilteringStage = svcs.DisplayFilteringStage;
        _reportAssemblyStage = svcs.ReportAssemblyStage;

        // Computed fields derived from provider contributions
        _iconProviderRegistry = svcs.IconProviderRegistry ?? CreateIconProviderRegistry(resolvedContributions);
        _valueFormatterRegistry = CreateValueFormatterRegistry(resolvedContributions);
        _viewModelFactoryRegistry = CreateFactoryRegistry(resolvedContributions);
        _parentChildRelationshipRegistry = CreateParentChildRelationshipRegistry(resolvedContributions);

        // Lazy-initialised fields
        _configurationReferenceIndex = [];
        _postMergeCallbacks = null;
    }

    /// <summary>
    /// Indicates whether sensitive values should be rendered without masking.
    /// </summary>
    private readonly bool _showSensitive;

    /// <summary>
    /// Indicates whether unchanged attribute values should be included in output tables.
    /// </summary>
    private readonly bool _showUnchangedValues;

    /// <summary>
    /// Indicates whether attribute change rows where before/after are Azure resource IDs
    /// differing only in casing should be suppressed.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </summary>
    private readonly bool _ignoreAzureIdCaseChanges;

    /// <summary>
    /// Registry of attribute change filters consulted when <see cref="_ignoreAzureIdCaseChanges"/> is active.
    /// Defaults to an empty registry (never suppresses anything) when not supplied.
    /// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
    /// </summary>
    private readonly Services.AttributeChangeFilterRegistry _attributeChangeFilterRegistry;

    /// <summary>
    /// Strategy for building resource summaries used in the report.
    /// </summary>
    private readonly IResourceSummaryBuilder _summaryBuilder;

    /// <summary>
    /// Optional custom report title provided by the user.
    /// </summary>
    private readonly string? _reportTitle;

    /// <summary>
    /// Provider for tfplan2md build metadata used in the report header.
    /// </summary>
    private readonly IMetadataProvider _metadataProvider;

    /// <summary>
    /// Indicates whether metadata should be hidden from the rendered report.
    /// </summary>
    private readonly bool _hideMetadata;

    /// <summary>
    /// Optional code analysis inputs to integrate into the report.
    /// </summary>
    private readonly CodeAnalysisInput? _codeAnalysisInput;

    /// <summary>
    /// Registry for icon provider services.
    /// </summary>
    private readonly MarkdownGeneration.Services.IconProviderRegistry? _iconProviderRegistry;

    /// <summary>
    /// Registry for value formatter services.
    /// </summary>
    private readonly MarkdownGeneration.Services.ValueFormatterRegistry? _valueFormatterRegistry;

    /// <summary>
    /// Mapper for resolving Azure principal names.
    /// </summary>
    private readonly IPrincipalMapper _principalMapper;

    /// <summary>
    /// Render target platform (GitHub or Azure DevOps).
    /// Stored so it can be passed through to the report assembly stage.
    /// </summary>
    private readonly RenderTargets.RenderTarget _renderTarget;

    /// <summary>
    /// Format for rendering large value diffs in tables.
    /// </summary>
    private readonly LargeValueFormat _largeValueFormat;

    /// <summary>
    /// Display mode for resource details blocks.
    /// </summary>
    private readonly RenderTargets.DetailsDisplayMode _detailsDisplayMode;

    /// <summary>
    /// Registry for resource-specific view model factories.
    /// </summary>
    private readonly ResourceViewModelFactoryRegistry _viewModelFactoryRegistry;

    /// <summary>
    /// Optional override for the resource-change construction stage.
    /// </summary>
    private readonly IResourceChangeStage? _resourceChangeStage;

    /// <summary>
    /// Optional override for the attribute-filtering stage.
    /// </summary>
    private readonly IAttributeFilteringStage? _attributeFilteringStage;

    /// <summary>
    /// Optional override for the summary-enrichment stage.
    /// </summary>
    private readonly ISummaryEnrichmentStage? _summaryEnrichmentStage;

    /// <summary>
    /// Optional override for the display-filtering stage.
    /// </summary>
    private readonly IDisplayFilteringStage? _displayFilteringStage;

    /// <summary>
    /// Optional override for the report-assembly stage.
    /// </summary>
    private readonly IReportAssemblyStage? _reportAssemblyStage;

    /// <summary>
    /// Registry for parent-child resource relationships.
    /// </summary>
    private readonly ParentChildRelationshipRegistry _parentChildRelationshipRegistry;

    /// <summary>
    /// Resolved provider contribution set, computed once in the constructor and reused
    /// for lazy callback initialisation to avoid repeated derivation from the registry.
    /// </summary>
    private readonly Services.ProviderContributionSet? _resolvedProviderContributions;

    /// <summary>
    /// Cached configuration reference index for fallback parent-child matching.
    /// </summary>
    private readonly Dictionary<(string Address, string Attribute), IReadOnlyList<string>> _configurationReferenceIndex;

    /// <summary>
    /// Collection of callbacks to invoke after parent-child merging completes.
    /// Populated lazily on first use to avoid initialization order issues.
    /// </summary>
    private List<ParentPostMergeCallback>? _postMergeCallbacks;

    /// <summary>
    /// Registers a callback to be invoked after parent-child merging completes.
    /// </summary>
    /// <param name="callback">The callback to register.</param>
    /// <remarks>
    /// Used by providers to perform post-merge processing like updating summaries.
    /// Related issue: docs/issues/059-parent-child-summary-member-counts/analysis.md.
    /// </remarks>
    public void RegisterPostMergeCallback(ParentPostMergeCallback callback)
    {
        _postMergeCallbacks ??= new List<ParentPostMergeCallback>();
        _postMergeCallbacks.Add(callback);
    }

    /// <summary>
    /// Initializes post-merge callbacks from the provider registry.
    /// </summary>
    private void EnsurePostMergeCallbacksInitialized()
    {
        if (_postMergeCallbacks is not null)
        {
            return;
        }

        _postMergeCallbacks = new List<ParentPostMergeCallback>();
        _resolvedProviderContributions?.RegisterPostMergeCallbacks(this);
    }

    /// <summary>
    /// Converts RenderTarget to LargeValueFormat for backwards compatibility.
    /// This will be removed in Task 6 when LargeValueFormat enum is fully removed.
    /// </summary>
    /// <param name="target">The render target to convert.</param>
    /// <returns>The corresponding LargeValueFormat value.</returns>
    internal static LargeValueFormat ConvertRenderTargetToLargeValueFormat(RenderTargets.RenderTarget target)
    {
        return target == RenderTargets.RenderTarget.GitHub
            ? LargeValueFormat.SimpleDiff
            : LargeValueFormat.InlineDiff;
    }

    /// <summary>
    /// Creates and configures the resource view model factory registry.
    /// </summary>
    /// <param name="providerContributions">Optional centralized provider contribution set.</param>
    /// <returns>Configured factory registry.</returns>
    private static ResourceViewModelFactoryRegistry CreateFactoryRegistry(
        Services.ProviderContributionSet? providerContributions)
    {
        var registry = new ResourceViewModelFactoryRegistry();

        providerContributions?.RegisterFactories(registry);

        return registry;
    }

    /// <summary>
    /// Builds an icon provider registry from the configured providers when not supplied explicitly.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="providerContributions">The provider contribution set to pull icon providers from.</param>
    /// <returns>The populated icon provider registry, or null when no providers are registered.</returns>
    private static MarkdownGeneration.Services.IconProviderRegistry? CreateIconProviderRegistry(
        Services.ProviderContributionSet? providerContributions)
    {
        return providerContributions?.CreateIconProviderRegistry();
    }

    /// <summary>
    /// Builds a value formatter registry from the configured providers.
    /// Related feature: docs/features/061-extensible-provider-registry/specification.md.
    /// </summary>
    /// <param name="providerContributions">The provider contribution set to pull value formatters from.</param>
    /// <returns>The populated value formatter registry, or null when no providers are registered.</returns>
    private static MarkdownGeneration.Services.ValueFormatterRegistry? CreateValueFormatterRegistry(
        Services.ProviderContributionSet? providerContributions)
    {
        return providerContributions?.CreateValueFormatterRegistry();
    }

    /// <summary>
    /// Creates and populates the parent-child relationship registry.
    /// </summary>
    /// <param name="providerContributions">Optional provider contribution set to register relationships from.</param>
    /// <returns>The populated parent-child relationship registry.</returns>
    private static ParentChildRelationshipRegistry CreateParentChildRelationshipRegistry(
        Services.ProviderContributionSet? providerContributions)
    {
        return providerContributions?.CreateParentChildRelationshipRegistry() ?? new ParentChildRelationshipRegistry();
    }

    /// <summary>
    /// Resolves the effective provider contribution set.
    /// </summary>
    /// <param name="providerContributions">Explicit contribution set, when already built.</param>
    /// <param name="providerRegistry">Provider registry used as a fallback source.</param>
    /// <returns>The resolved provider contribution set, or null when no providers are configured.</returns>
    private static Services.ProviderContributionSet? CreateProviderContributions(
        Services.ProviderContributionSet? providerContributions,
        Services.ProviderRegistry? providerRegistry)
    {
        return providerContributions ?? providerRegistry?.CreateContributionSet();
    }

    /// <summary>
    /// Creates the default resource-change construction stage for this builder instance.
    /// </summary>
    /// <returns>The default resource-change stage.</returns>
    private ResourceChangeStage CreateResourceChangeStage()
    {
        return new ResourceChangeStage(
            _summaryBuilder,
            _showSensitive,
            _showUnchangedValues,
            _viewModelFactoryRegistry,
            _principalMapper,
            _iconProviderRegistry);
    }

    /// <summary>
    /// Creates the default attribute-filtering stage for this builder instance.
    /// </summary>
    /// <returns>The default attribute-filtering stage.</returns>
    private AttributeFilteringStage CreateAttributeFilteringStage()
    {
        return new AttributeFilteringStage(
            _ignoreAzureIdCaseChanges,
            _attributeChangeFilterRegistry,
            _summaryBuilder);
    }

    /// <summary>
    /// Creates the default summary-enrichment stage for this builder instance.
    /// </summary>
    /// <returns>The default summary-enrichment stage.</returns>
    private static SummaryEnrichmentStage CreateSummaryEnrichmentStage()
    {
        return new SummaryEnrichmentStage();
    }

    /// <summary>
    /// Creates the default display-filtering stage for this builder instance.
    /// </summary>
    /// <returns>The default display-filtering stage.</returns>
    private DisplayFilteringStage CreateDisplayFilteringStage()
    {
        return new DisplayFilteringStage(_ignoreAzureIdCaseChanges);
    }

    /// <summary>
    /// Creates the default report-assembly stage for this builder instance.
    /// </summary>
    /// <returns>The default report-assembly stage.</returns>
    private static ReportAssemblyStage CreateReportAssemblyStage()
    {
        return new ReportAssemblyStage();
    }
}
