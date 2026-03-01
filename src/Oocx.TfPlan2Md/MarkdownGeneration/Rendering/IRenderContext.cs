using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using Oocx.TfPlan2Md.RenderTargets;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Rendering;

/// <summary>
/// Describes shared rendering state passed through the rendering tree.
/// Related feature: docs/features/107-remove-scriban/specification.md.
/// </summary>
internal interface IRenderContext
{
    /// <summary>
    /// Gets a value indicating whether sensitive values should be rendered unmasked.
    /// </summary>
    bool ShowSensitive { get; }

    /// <summary>
    /// Gets a value indicating whether unchanged values should be displayed.
    /// </summary>
    bool ShowUnchangedValues { get; }

    /// <summary>
    /// Gets a value indicating whether Azure ID case-only changes should be ignored.
    /// </summary>
    bool IgnoreAzureIdCaseChanges { get; }

    /// <summary>
    /// Gets the current markdown render target.
    /// </summary>
    RenderTarget RenderTarget { get; }

    /// <summary>
    /// Gets the selected details block display mode.
    /// </summary>
    DetailsDisplayMode DetailsDisplayMode { get; }

    /// <summary>
    /// Gets the value formatter registry used during resource rendering.
    /// </summary>
    ValueFormatterRegistry? ValueFormatterRegistry { get; }

    /// <summary>
    /// Gets the icon provider registry used during resource rendering.
    /// </summary>
    IconProviderRegistry? IconProviderRegistry { get; }
}
