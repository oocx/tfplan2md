namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Represents a validation error for parsed CLI options.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
// SonarAnalyzer S3871: Exception is intentionally internal
// Justification: CLI exception used only within this tool, not exposed in public API
#pragma warning disable S3871 // Exception types should be "public"
internal sealed class CliValidationException : Exception
#pragma warning restore S3871
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliValidationException"/> class.
    /// </summary>
    public CliValidationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliValidationException"/> class.
    /// </summary>
    /// <param name="message">Description of the validation error.</param>
    public CliValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliValidationException"/> class.
    /// </summary>
    /// <param name="message">Description of the validation error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CliValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
