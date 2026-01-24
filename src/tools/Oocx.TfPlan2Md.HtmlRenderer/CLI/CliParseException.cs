namespace Oocx.TfPlan2Md.HtmlRenderer.CLI;

/// <summary>
/// Represents an error encountered while parsing CLI arguments.
/// Related feature: docs/features/027-markdown-html-rendering/specification.md.
/// </summary>
// SonarAnalyzer S3871: Exception is intentionally internal
// Justification: CLI exception used only within this tool, not exposed in public API
#pragma warning disable S3871 // Exception types should be "public"
internal sealed class CliParseException : Exception
#pragma warning restore S3871
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class.
    /// </summary>
    public CliParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class.
    /// </summary>
    /// <param name="message">Description of the parsing error.</param>
    public CliParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class.
    /// </summary>
    /// <param name="message">Description of the parsing error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CliParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
