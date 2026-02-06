using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Exception thrown when a service registration contains invalid matching rules.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
public sealed class ServiceRegistrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class.
    /// </summary>
    public ServiceRegistrationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the registration failure.</param>
    public ServiceRegistrationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the registration failure.</param>
    /// <param name="innerException">The underlying exception that triggered this failure.</param>
    public ServiceRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
