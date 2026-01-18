using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator.Capturing;

/// <summary>
/// Represents capture configuration for generating a screenshot from HTML.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class CaptureSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureSettings"/> class.
    /// </summary>
    /// <param name="inputPath">Full path to the input HTML file.</param>
    /// <param name="outputPath">Full path to the output image file.</param>
    /// <param name="width">Viewport width in pixels.</param>
    /// <param name="height">Viewport height in pixels.</param>
    /// <param name="fullPage">Whether to capture the full scrollable page.</param>
    /// <param name="format">The requested output format.</param>
    /// <param name="quality">Optional quality value for lossy formats.</param>
    /// <param name="deviceScaleFactor">Device scale factor for high-DPI rendering (default: 1.0).</param>
    /// <param name="targetTerraformResourceId">Optional Terraform address for partial capture.</param>
    /// <param name="targetSelector">Optional selector for partial capture.</param>
    public CaptureSettings(string inputPath, string outputPath, int width, int height, bool fullPage, ScreenshotFormat format, int? quality = null, double deviceScaleFactor = 1.0, string? targetTerraformResourceId = null, string? targetSelector = null)
    {
        InputPath = inputPath;
        OutputPath = outputPath;
        Width = width;
        Height = height;
        FullPage = fullPage;
        Format = format;
        Quality = quality;
        DeviceScaleFactor = deviceScaleFactor;
        TargetTerraformResourceId = targetTerraformResourceId;
        TargetSelector = targetSelector;
    }

    /// <summary>
    /// Gets the input HTML file path.
    /// </summary>
    public string InputPath { get; }

    /// <summary>
    /// Gets the output image file path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets the viewport width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the viewport height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets a value indicating whether full-page capture is enabled.
    /// </summary>
    public bool FullPage { get; }

    /// <summary>
    /// Gets the requested screenshot format.
    /// </summary>
    public ScreenshotFormat Format { get; }

    /// <summary>
    /// Gets the optional quality value for lossy formats.
    /// </summary>
    public int? Quality { get; }

    /// <summary>
    /// Gets the device scale factor for high-DPI rendering (e.g., 2.0 for Retina displays).
    /// </summary>
    public double DeviceScaleFactor { get; }

    /// <summary>
    /// Gets the Terraform resource address used for partial capture when specified.
    /// </summary>
    public string? TargetTerraformResourceId { get; }

    /// <summary>
    /// Gets the selector used for partial capture when specified.
    /// </summary>
    public string? TargetSelector { get; }
}
