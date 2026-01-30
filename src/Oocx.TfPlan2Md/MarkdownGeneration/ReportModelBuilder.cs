using Oocx.TfPlan2Md.CodeAnalysis;
using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <param name="summaryBuilder">Factory for resource summaries; defaults to <see cref="ResourceSummaryBuilder"/>.</param>
/// <param name="showSensitive">Whether to show sensitive values without masking.</param>
/// <param name="showUnchangedValues">Whether unchanged attributes should be included in tables.</param>
/// <param name="renderTarget">Target platform for markdown rendering (GitHub or Azure DevOps).</param>
/// <param name="reportTitle">Optional custom report title to propagate to templates.</param>
/// <param name="principalMapper">Optional mapper for resolving principal names in role assignments.</param>
/// <param name="metadataProvider">Provider for tfplan2md version, commit, and generation timestamp metadata.</param>
/// <param name="hideMetadata">Whether the metadata line should be suppressed in the rendered report.</param>
/// <param name="providerRegistry">Optional registry of provider modules for registering provider-specific factories.</param>
/// <param name="codeAnalysisInput">Optional code analysis inputs to integrate into the report.</param>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
internal partial class ReportModelBuilder(
    IResourceSummaryBuilder? summaryBuilder = null,
    bool showSensitive = false,
    bool showUnchangedValues = false,
    RenderTargets.RenderTarget renderTarget = RenderTargets.RenderTarget.AzureDevOps,
    string? reportTitle = null,
    Platforms.Azure.IPrincipalMapper? principalMapper = null,
    IMetadataProvider? metadataProvider = null,
    bool hideMetadata = false,
    Providers.ProviderRegistry? providerRegistry = null,
    CodeAnalysisInput? codeAnalysisInput = null)
{
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
    /// Optional custom report title provided by the user.
    /// </summary>
    private readonly string? _reportTitle = reportTitle;

    /// <summary>
    /// Provider for tfplan2md build metadata used in the report header.
    /// </summary>
    private readonly IMetadataProvider _metadataProvider = metadataProvider ?? new AssemblyMetadataProvider();

    /// <summary>
    /// Indicates whether metadata should be hidden from the rendered report.
    /// </summary>
    private readonly bool _hideMetadata = hideMetadata;

    /// <summary>
    /// Optional code analysis inputs to integrate into the report.
    /// </summary>
    private readonly CodeAnalysisInput? _codeAnalysisInput = codeAnalysisInput;

    /// <summary>
    /// Registry for resource-specific view model factories.
    /// </summary>
    private readonly ResourceViewModelFactoryRegistry _viewModelFactoryRegistry =
        CreateFactoryRegistry(ConvertRenderTargetToLargeValueFormat(renderTarget), principalMapper ?? new Platforms.Azure.NullPrincipalMapper(), providerRegistry);

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
    /// <param name="largeValueFormat">Preferred rendering format for large attribute values.</param>
    /// <param name="principalMapper">Mapper for resolving principal names.</param>
    /// <param name="providerRegistry">Optional registry of provider modules.</param>
    /// <returns>Configured factory registry.</returns>
    private static ResourceViewModelFactoryRegistry CreateFactoryRegistry(
        LargeValueFormat largeValueFormat,
        Platforms.Azure.IPrincipalMapper principalMapper,
        Providers.ProviderRegistry? providerRegistry)
    {
        var registry = new ResourceViewModelFactoryRegistry(largeValueFormat, principalMapper);

        // Register provider-specific factories if a provider registry is available
        providerRegistry?.RegisterAllFactories(registry);

        return registry;
    }
}
