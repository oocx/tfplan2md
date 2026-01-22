namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Represents an error encountered while parsing CLI arguments.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
// SonarAnalyzer S3871: Exception is intentionally internal
// Justification: CLI exception used only within this tool, not exposed in public API
#pragma warning disable S3871 // Exception types should be "public"
internal sealed class CliParseException : ApplicationException
#pragma warning restore S3871
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class.
    /// </summary>
    /// <param name="message">Description of the parsing error.</param>
    public CliParseException(string message)
        : base(message)
    {
    }
}
