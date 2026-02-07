using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Exception thrown when a service registration contains invalid matching rules.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
// SonarAnalyzer S3871: Exception is intentionally internal
// Justification: registration errors are internal to the rendering pipeline
#pragma warning disable S3871 // Exception types should be "public"
internal sealed class ServiceRegistrationException : Exception
#pragma warning restore S3871
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
