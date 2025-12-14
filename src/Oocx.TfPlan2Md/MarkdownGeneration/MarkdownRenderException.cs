namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Exception thrown when Markdown rendering fails.
/// </summary>
public class MarkdownRenderException : ApplicationException
{
    public MarkdownRenderException(string message) : base(message)
    {
    }

    public MarkdownRenderException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
