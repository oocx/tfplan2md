namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Associates a service instance with its match pattern and registration order.
/// </summary>
/// <typeparam name="TService">The type of service registered in the registry.</typeparam>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class ServiceRegistration<TService>
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistration{TService}"/> class.
    /// </summary>
    /// <param name="service">The registered service instance.</param>
    /// <param name="pattern">The match pattern associated with the service.</param>
    /// <param name="registrationOrder">The registration order used for stable tie-breaking.</param>
    public ServiceRegistration(TService service, MatchPattern pattern, int registrationOrder)
    {
        Service = service;
        Pattern = pattern;
        RegistrationOrder = registrationOrder;
    }

    /// <summary>
    /// Gets the registered service instance.
    /// </summary>
    /// <value>The service instance.</value>
    public TService Service { get; }

    /// <summary>
    /// Gets the match pattern associated with the service.
    /// </summary>
    /// <value>The match pattern.</value>
    public MatchPattern Pattern { get; }

    /// <summary>
    /// Gets the registration order used for stable tie-breaking.
    /// </summary>
    /// <value>The registration order.</value>
    public int RegistrationOrder { get; }
}
