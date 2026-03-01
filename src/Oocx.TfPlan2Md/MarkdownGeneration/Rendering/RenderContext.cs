using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Default immutable implementation of <see cref="IRenderContext"/>.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal sealed class RenderContext : IRenderContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenderContext"/> class.
    /// </summary>
    /// <param name="showSensitive">Whether sensitive values should be rendered unmasked.</param>
    /// <param name="showUnchangedValues">Whether unchanged values should be displayed.</param>
    /// <param name="ignoreAzureIdCaseChanges">Whether Azure ID case-only changes should be ignored.</param>
    /// <param name="renderTarget">The markdown render target.</param>
    /// <param name="detailsDisplayMode">The details display mode.</param>
    /// <param name="valueFormatterRegistry">The optional value formatter registry.</param>
    /// <param name="iconProviderRegistry">The optional icon provider registry.</param>
    public RenderContext(
        bool showSensitive,
        bool showUnchangedValues,
        bool ignoreAzureIdCaseChanges,
        RenderTarget renderTarget,
        DetailsDisplayMode detailsDisplayMode,
        ValueFormatterRegistry? valueFormatterRegistry = null,
        IconProviderRegistry? iconProviderRegistry = null)
    {
        ShowSensitive = showSensitive;
        ShowUnchangedValues = showUnchangedValues;
        IgnoreAzureIdCaseChanges = ignoreAzureIdCaseChanges;
        RenderTarget = renderTarget;
        DetailsDisplayMode = detailsDisplayMode;
        ValueFormatterRegistry = valueFormatterRegistry;
        IconProviderRegistry = iconProviderRegistry;
    }

    /// <inheritdoc />
    public bool ShowSensitive { get; }

    /// <inheritdoc />
    public bool ShowUnchangedValues { get; }

    /// <inheritdoc />
    public bool IgnoreAzureIdCaseChanges { get; }

    /// <inheritdoc />
    public RenderTarget RenderTarget { get; }

    /// <inheritdoc />
    public DetailsDisplayMode DetailsDisplayMode { get; }

    /// <inheritdoc />
    public ValueFormatterRegistry? ValueFormatterRegistry { get; }

    /// <inheritdoc />
    public IconProviderRegistry? IconProviderRegistry { get; }
}
