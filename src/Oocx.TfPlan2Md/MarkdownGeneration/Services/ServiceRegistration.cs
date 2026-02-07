namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Associates a service instance with its match pattern and registration order.
/// </summary>
/// <typeparam name="TService">The type of service registered in the registry.</typeparam>
/// <param name="Service">The registered service instance.</param>
/// <param name="Pattern">The match pattern associated with the service.</param>
/// <param name="RegistrationOrder">The registration order used for stable tie-breaking.</param>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed record ServiceRegistration<TService>(
    TService Service,
    MatchPattern Pattern,
    int RegistrationOrder)
    where TService : class;
