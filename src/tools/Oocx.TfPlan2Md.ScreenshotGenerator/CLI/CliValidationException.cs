namespace Oocx.TfPlan2Md.ScreenshotGenerator.CLI;

/// <summary>
/// Represents a validation error for parsed CLI options.
/// Related feature: docs/features/028-html-screenshot-generation/specification.md.
/// </summary>
internal sealed class CliValidationException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliValidationException"/> class.
    /// </summary>
    /// <param name="message">Description of the validation error.</param>
    public CliValidationException(string message)
        : base(message)
    {
    }
}
