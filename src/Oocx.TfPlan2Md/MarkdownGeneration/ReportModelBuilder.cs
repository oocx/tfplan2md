using Oocx.TfPlan2Md.MarkdownGeneration.Models;
using Oocx.TfPlan2Md.MarkdownGeneration.Summaries;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Builds a ReportModel from a TerraformPlan.
/// </summary>
/// <param name="summaryBuilder">Factory for resource summaries; defaults to <see cref="ResourceSummaryBuilder"/>.</param>
/// <param name="showSensitive">Whether to show sensitive values without masking.</param>
/// <param name="showUnchangedValues">Whether unchanged attributes should be included in tables.</param>
/// <param name="largeValueFormat">Rendering format for large values (inline-diff or simple-diff).</param>
/// <param name="reportTitle">Optional custom report title to propagate to templates.</param>
/// <param name="principalMapper">Optional mapper for resolving principal names in role assignments.</param>
/// <param name="metadataProvider">Provider for tfplan2md version, commit, and generation timestamp metadata.</param>
/// <param name="hideMetadata">Whether the metadata line should be suppressed in the rendered report.</param>
/// <remarks>
/// Related features: docs/features/020-custom-report-title/specification.md and docs/features/014-unchanged-values-cli-option/specification.md.
/// </remarks>
public partial class ReportModelBuilder(
    IResourceSummaryBuilder? summaryBuilder = null,
    bool showSensitive = false,
    bool showUnchangedValues = false,
    LargeValueFormat largeValueFormat = LargeValueFormat.InlineDiff,
    string? reportTitle = null,
    Azure.IPrincipalMapper? principalMapper = null,
    IMetadataProvider? metadataProvider = null,
    bool hideMetadata = false)
{
    /// <summary>
    /// Non-breaking space used to keep semantic icons attached to their labels in markdown output.
    /// Related feature: docs/features/024-visual-report-enhancements/specification.md.
    /// </summary>
    private const string NonBreakingSpace = ScribanHelpers.NonBreakingSpace;

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
    /// Preferred rendering format for large attribute values.
    /// </summary>
    private readonly LargeValueFormat _largeValueFormat = largeValueFormat;

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
    /// Registry for resource-specific view model factories.
    /// </summary>
    private readonly ResourceViewModelFactoryRegistry _viewModelFactoryRegistry =
        new(largeValueFormat, principalMapper ?? new Azure.NullPrincipalMapper());
}
