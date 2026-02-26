using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Services;

/// <summary>
/// Holds all registered <see cref="IAttributeChangeFilter"/> instances and dispatches
/// suppression decisions to each in turn.
/// Related feature: docs/features/103-azure-id-case-insensitive-filter/specification.md.
/// </summary>
/// <remarks>
/// Mirrors the <see cref="ValueFormatterRegistry"/> pattern. Filters use OR semantics:
/// a row is suppressed when <em>any</em> registered filter returns <c>true</c>.
/// An empty registry never suppresses anything.
/// </remarks>
internal sealed class AttributeChangeFilterRegistry
{
    private readonly List<IAttributeChangeFilter> _filters = new();

    /// <summary>
    /// Registers an attribute change filter.
    /// </summary>
    /// <param name="filter">The filter to add to the registry.</param>
    public void Register(IAttributeChangeFilter filter)
    {
        _filters.Add(filter);
    }

    /// <summary>
    /// Determines whether any registered filter wants to suppress the given attribute change row.
    /// </summary>
    /// <param name="context">The context carrying provider name, attribute name, and before/after values.</param>
    /// <returns>
    /// <c>true</c> if at least one filter returns <c>true</c>; <c>false</c> when no filters are
    /// registered or all return <c>false</c>.
    /// </returns>
    public bool ShouldSuppress(AttributeChangeFilterContext context)
        => _filters.Exists(filter => filter.ShouldSuppress(context));
}
