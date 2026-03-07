using System;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Resolves and executes value formatters in specificity order.
/// </summary>
/// <remarks>
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </remarks>
internal sealed class ValueFormatterRegistry
{
    /// <summary>
    /// The underlying pattern-matching registry for value formatters.
    /// </summary>
    private readonly PatternMatchingRegistry<IValueFormatter> _registry = new();

    /// <summary>
    /// Registers a value formatter with its match pattern.
    /// </summary>
    /// <param name="pattern">The match pattern that determines applicability.</param>
    /// <param name="formatter">The formatter to register.</param>
    public void Register(MatchPattern pattern, IValueFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(formatter);

        _registry.Register(pattern, formatter);
    }

    /// <summary>
    /// Attempts to format a value by returning the first non-null result from registered formatters.
    /// </summary>
    /// <param name="context">The resolution context to evaluate.</param>
    /// <returns>A formatted string when handled; otherwise null.</returns>
    public string? TryFormat(ServiceResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _registry.TryResolveFirst(context, static (formatter, ctx) => formatter.TryFormat(ctx));
    }
}
