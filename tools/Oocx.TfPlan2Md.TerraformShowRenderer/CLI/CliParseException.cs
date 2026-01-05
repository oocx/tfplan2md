namespace Oocx.TfPlan2Md.TerraformShowRenderer.CLI;

/// <summary>
/// Represents CLI parsing failures for the Terraform show approximation tool.
/// Related feature: docs/features/030-terraform-show-approximation/specification.md
/// </summary>
internal sealed class CliParseException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliParseException"/> class with a message.
    /// </summary>
    /// <param name="message">Details about the parsing failure.</param>
    public CliParseException(string message)
        : base(message)
    {
    }
}
