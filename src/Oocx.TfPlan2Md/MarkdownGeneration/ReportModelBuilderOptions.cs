using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Stages;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;
using Oocx.TfPlan2Md.Platforms.Azure;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Groups the boolean, enum, and scalar configuration options for <see cref="ReportModelBuilder"/>.
/// </summary>
/// <param name="ShowSensitive">Whether to show sensitive values without masking.</param>
/// <param name="ShowUnchangedValues">Whether unchanged attributes should be included in tables.</param>
/// <param name="RenderTarget">Target platform for markdown rendering (GitHub or Azure DevOps).</param>
/// <param name="ReportTitle">Optional custom report title to propagate to templates.</param>
/// <param name="HideMetadata">Whether the metadata line should be suppressed in the rendered report.</param>
/// <param name="DetailsDisplayMode">Display mode for resource details blocks.</param>
/// <param name="IgnoreAzureIdCaseChanges">Whether attribute change rows where before/after are Azure resource IDs differing only in casing are suppressed.</param>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md,
/// docs/features/014-unchanged-values-cli-option/specification.md, and
/// docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </remarks>
internal sealed record ReportModelBuilderOptions(
    bool ShowSensitive = false,
    bool ShowUnchangedValues = false,
    RenderTargets.RenderTarget RenderTarget = RenderTargets.RenderTarget.AzureDevOps,
    string? ReportTitle = null,
    bool HideMetadata = false,
    RenderTargets.DetailsDisplayMode DetailsDisplayMode = RenderTargets.DetailsDisplayMode.Auto,
    bool IgnoreAzureIdCaseChanges = true);

/// <summary>
/// Groups the injected service dependencies for <see cref="ReportModelBuilder"/>.
/// </summary>
/// <param name="SummaryBuilder">Factory for resource summaries; defaults to <see cref="ResourceSummaryBuilder"/>.</param>
/// <param name="PrincipalMapper">Optional mapper for resolving principal names in role assignments.</param>
/// <param name="MetadataProvider">Provider for tfplan2md version, commit, and generation timestamp metadata.</param>
/// <param name="ProviderRegistry">Optional registry of provider modules for registering provider-specific factories.</param>
/// <param name="ProviderContributions">Optional centralized provider contribution set.</param>
/// <param name="CodeAnalysisInput">Optional code analysis inputs to integrate into the report.</param>
/// <param name="IconProviderRegistry">Optional registry of icon providers used during rendering.</param>
/// <param name="AttributeChangeFilterRegistry">Optional registry of attribute change filters; defaults to an empty registry.</param>
/// <param name="ResourceChangeStage">Optional override for the resource-change construction stage.</param>
/// <param name="AttributeFilteringStage">Optional override for the attribute-filtering stage.</param>
/// <param name="SummaryEnrichmentStage">Optional override for the summary-enrichment stage.</param>
/// <param name="DisplayFilteringStage">Optional override for the display-filtering stage.</param>
/// <param name="ReportAssemblyStage">Optional override for the report-assembly stage.</param>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed record ReportModelBuilderServices(
    IResourceSummaryBuilder? SummaryBuilder = null,
    IPrincipalMapper? PrincipalMapper = null,
    IMetadataProvider? MetadataProvider = null,
    Services.ProviderRegistry? ProviderRegistry = null,
    Services.ProviderContributionSet? ProviderContributions = null,
    CodeAnalysisInput? CodeAnalysisInput = null,
    Services.IconProviderRegistry? IconProviderRegistry = null,
    Services.AttributeChangeFilterRegistry? AttributeChangeFilterRegistry = null,
    IResourceChangeStage? ResourceChangeStage = null,
    IAttributeFilteringStage? AttributeFilteringStage = null,
    ISummaryEnrichmentStage? SummaryEnrichmentStage = null,
    IDisplayFilteringStage? DisplayFilteringStage = null,
    IReportAssemblyStage? ReportAssemblyStage = null);
