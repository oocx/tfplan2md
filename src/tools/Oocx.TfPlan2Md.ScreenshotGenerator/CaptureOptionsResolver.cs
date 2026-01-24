using Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

namespace Oocx.TfPlan2Md.ScreenshotGenerator;

/// <summary>
/// Resolves capture-related options such as output path, format, and quality.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal static class CaptureOptionsResolver
{
    private const int DefaultJpegQuality = 90;

    /// <summary>
    /// Determines the output path. When <paramref name="explicitOutputPath"/> is null, derives a PNG path alongside the input.
    /// </summary>
    /// <param name="inputPath">Input HTML path.</param>
    /// <param name="explicitOutputPath">User-specified output path.</param>
    /// <returns>Resolved output path.</returns>
    public static string DetermineOutputPath(string inputPath, string? explicitOutputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        if (!string.IsNullOrWhiteSpace(explicitOutputPath))
        {
            return explicitOutputPath;
        }

        var directory = Path.GetDirectoryName(inputPath);
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var derivedFile = string.Concat(fileName, ".png");

        return string.IsNullOrWhiteSpace(directory)
            ? derivedFile
            : Path.Combine(directory, derivedFile);
    }

    /// <summary>
    /// Resolves the output format using the explicit CLI value first, then the output file extension, defaulting to PNG.
    /// </summary>
    /// <param name="formatOption">Format provided on the CLI.</param>
    /// <param name="outputPath">Resolved output path used for extension detection.</param>
    /// <returns>The resolved screenshot format.</returns>
    public static ScreenshotFormat ResolveFormat(ScreenshotFormat? formatOption, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (formatOption.HasValue)
        {
            return formatOption.Value;
        }

        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => ScreenshotFormat.Jpeg,
            _ => ScreenshotFormat.Png,
        };
    }

    /// <summary>
    /// Resolves the quality setting for the selected format, applying defaults for JPEG when not provided.
    /// </summary>
    /// <param name="format">Resolved screenshot format.</param>
    /// <param name="qualityOption">Quality provided on the CLI.</param>
    /// <returns>The quality to apply, or <see langword="null"/> when not applicable.</returns>
    public static int? ResolveQuality(ScreenshotFormat format, int? qualityOption)
    {
        if (format != ScreenshotFormat.Jpeg)
        {
            return null;
        }

        return qualityOption ?? DefaultJpegQuality;
    }
}
