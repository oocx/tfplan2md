using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration;

/// <summary>
/// Exception thrown when a Scriban helper function encounters an error.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
public class ScribanHelperException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScribanHelperException"/> class.
    /// </summary>
    public ScribanHelperException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScribanHelperException"/> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ScribanHelperException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScribanHelperException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Underlying exception that caused this error.</param>
    public ScribanHelperException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
