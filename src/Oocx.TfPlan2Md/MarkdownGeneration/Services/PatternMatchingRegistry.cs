using System;
using System.Collections.Generic;
using System.Linq;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Registers services with match patterns and resolves them by specificity.
/// </summary>
/// <typeparam name="TService">The type of service to register.</typeparam>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class PatternMatchingRegistry<TService>
    where TService : class
{
    /// <summary>
    /// Stores registered services in registration order for stable tie-breaking.
    /// </summary>
    private readonly List<ServiceRegistration<TService>> _registrations = [];

    /// <summary>
    /// Tracks the next registration order to ensure deterministic sorting.
    /// </summary>
    private int _nextRegistrationOrder;

    /// <summary>
    /// Registers a service with a match pattern.
    /// </summary>
    /// <param name="pattern">The match pattern that determines applicability.</param>
    /// <param name="service">The service instance to register.</param>
    public void Register(MatchPattern pattern, TService service)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(service);

        var registration = new ServiceRegistration<TService>(service, pattern, _nextRegistrationOrder++);
        _registrations.Add(registration);
    }

    /// <summary>
    /// Resolves all services that match the given context in specificity order.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>The services ordered by specificity and tie-break rules.</returns>
    public IReadOnlyList<TService> ResolveAll(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _registrations
            .Where(registration => registration.Pattern.IsMatch(context))
            .OrderByDescending(registration => registration.Pattern.Specificity)
            .ThenByDescending(registration => registration.Pattern.DimensionPriority)
            .ThenBy(registration => registration.RegistrationOrder)
            .Select(registration => registration.Service)
            .ToList();
    }
}
