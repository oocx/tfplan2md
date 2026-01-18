namespace Oocx.TfPlan2Md.ScreenshotGenerator.Capturing;

/// <summary>
/// Represents a failure during screenshot capture.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class ScreenshotCaptureException : ApplicationException
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
