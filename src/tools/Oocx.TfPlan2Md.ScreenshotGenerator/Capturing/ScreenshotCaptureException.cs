namespace Oocx.TfPlan2Md.ScreenshotGenerator.Capturing;

/// <summary>
/// Represents a failure during screenshot capture.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
// SonarAnalyzer S3871: Exception is intentionally internal
// Justification: Screenshot exception used only within this tool, not exposed in public API
#pragma warning disable S3871 // Exception types should be "public"
internal sealed class ScreenshotCaptureException : Exception
#pragma warning restore S3871
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotCaptureException"/> class.
    /// </summary>
    /// <param name="message">Exception message describing the failure.</param>
    /// <param name="innerException">Original exception cause.</param>
    public ScreenshotCaptureException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
