namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Represents an error encountered while parsing CLI arguments.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class CliParseException : ApplicationException
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
