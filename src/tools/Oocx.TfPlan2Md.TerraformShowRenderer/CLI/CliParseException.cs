namespace Oocx.TfPlan2Md.TerraformShowRenderer.CLI;

/// <summary>
/// Represents CLI parsing failures for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md.
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
    /// Initializes a new instance of the <see cref="CliParseException"/> class with a message.
    /// </summary>
    /// <param name="message">Details about the parsing failure.</param>
    public CliParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">Details about the parsing failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CliParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
