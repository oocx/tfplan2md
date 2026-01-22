namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Exception thrown when Markdown rendering fails.
/// </summary>
public class MarkdownRenderException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownRenderException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MarkdownRenderException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownRenderException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public MarkdownRenderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
