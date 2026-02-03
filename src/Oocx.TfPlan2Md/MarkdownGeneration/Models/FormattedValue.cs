using System;
using System.Collections.Generic;

namespace Oocx.TfPlan2Md.MarkdownGeneration.Models;

/// <summary>
/// Wraps a value with its pre-computed formatted representation for template consumption.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
/// <typeparam name="T">The type of the raw value.</typeparam>
public sealed record FormattedValue<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattedValue{T}"/> class.
    /// </summary>
    /// <param name="raw">The raw underlying value.</param>
    /// <param name="formatted">The pre-computed formatted representation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatted"/> is null.</exception>
    public FormattedValue(T raw, string formatted)
    {
        Raw = raw;
        Formatted = formatted ?? throw new ArgumentNullException(nameof(formatted));
    }

    /// <summary>
    /// Gets the raw underlying value, typically used for logic or fallbacks.
    /// </summary>
    public T Raw { get; }

    /// <summary>
    /// Gets the pre-computed formatted value suitable for direct template output.
    /// </summary>
    public string Formatted { get; }
}

/// <summary>
/// Wraps a list of values with a pre-computed formatted representation for template consumption.
/// Related feature: docs/features/026-template-rendering-simplification/specification.md.
/// </summary>
/// <typeparam name="T">The type of the raw list elements.</typeparam>
public sealed record FormattedList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattedList{T}"/> class.
    /// </summary>
    /// <param name="raw">The raw values.</param>
    /// <param name="formatted">The pre-computed formatted representation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="raw"/> or <paramref name="formatted"/> is null.</exception>
    public FormattedList(IReadOnlyList<T> raw, string formatted)
    {
        Raw = raw ?? throw new ArgumentNullException(nameof(raw));
        Formatted = formatted ?? throw new ArgumentNullException(nameof(formatted));
    }

    /// <summary>
    /// Gets the raw values.
    /// </summary>
    public IReadOnlyList<T> Raw { get; }

    /// <summary>
    /// Gets the pre-computed formatted representation of the list.
    /// </summary>
    public string Formatted { get; }

    /// <summary>
    /// Gets the number of items in <see cref="Raw"/>.
    /// </summary>
    public int Count => Raw.Count;

    /// <summary>
    /// Gets a value indicating whether <see cref="Raw"/> is empty.
    /// </summary>
    public bool IsEmpty => Raw.Count == 0;
}
