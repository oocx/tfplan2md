using AwesomeAssertions;
using Oocx.TfPlan2Md.MarkdownGeneration.Services;
using TUnit.Core;

namespace Oocx.TfPlan2Md.Tests.MarkdownGeneration;

/// <summary>
/// Tests the typed registries for value formatting and icon resolution.
/// Related feature: docs/features/061-extensible-provider-registry/specification.md.
/// </summary>
public class ValueFormatterRegistryTests
{
    /// <summary>
    /// Ensures value formatters fall back until a formatter handles the context.
    /// </summary>
    [Test]
    public void ValueFormatterRegistry_TryFormat_UsesFirstNonNullFormatter()
    {
        var registry = new ValueFormatterRegistry();
        registry.Register(new MatchPattern(null, null, null, null), new NullFormatter());
        registry.Register(new MatchPattern(null, null, null, null), new FixedFormatter("formatted"));

        var result = registry.TryFormat(new ServiceResolutionContext("provider", "resource", "attribute", "value"));

        result.Should().Be("formatted");
    }

    /// <summary>
    /// Ensures icon providers fall back until a provider handles the context.
    /// </summary>
    [Test]
    public void IconProviderRegistry_TryGetIcon_UsesFirstNonNullProvider()
    {
        var registry = new IconProviderRegistry();
        registry.Register(new MatchPattern(null, null, null, null), new NullIconProvider());
        registry.Register(new MatchPattern(null, null, null, null), new FixedIconProvider("icon"));

        var result = registry.TryGetIcon(new ServiceResolutionContext("provider", "resource", "attribute", "value"));

        result.Should().Be("icon");
    }

    /// <summary>
    /// Formatter that always declines to handle the context.
    /// </summary>
    private sealed class NullFormatter : IValueFormatter
    {
        /// <summary>
        /// Returns null to signal fallback behavior.
        /// </summary>
        /// <param name="context">The resolution context to evaluate.</param>
        /// <returns>Always null.</returns>
        public string? TryFormat(ServiceResolutionContext context)
        {
            return null;
        }
    }

    /// <summary>
    /// Formatter that returns a fixed formatted value.
    /// </summary>
    private sealed class FixedFormatter : IValueFormatter
    {
        /// <summary>
        /// The formatted value to return.
        /// </summary>
        private readonly string _formatted;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedFormatter"/> class.
        /// </summary>
        /// <param name="formatted">The formatted value to return.</param>
        public FixedFormatter(string formatted)
        {
            _formatted = formatted;
        }

        /// <summary>
        /// Returns the fixed formatted value.
        /// </summary>
        /// <param name="context">The resolution context to evaluate.</param>
        /// <returns>The formatted value.</returns>
        public string? TryFormat(ServiceResolutionContext context)
        {
            return _formatted;
        }
    }

    /// <summary>
    /// Icon provider that always declines to handle the context.
    /// </summary>
    private sealed class NullIconProvider : IIconProvider
    {
        /// <summary>
        /// Returns null to signal fallback behavior.
        /// </summary>
        /// <param name="context">The resolution context to evaluate.</param>
        /// <returns>Always null.</returns>
        public string? TryGetIcon(ServiceResolutionContext context)
        {
            return null;
        }
    }

    /// <summary>
    /// Icon provider that returns a fixed icon value.
    /// </summary>
    private sealed class FixedIconProvider : IIconProvider
    {
        /// <summary>
        /// The icon value to return.
        /// </summary>
        private readonly string _icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedIconProvider"/> class.
        /// </summary>
        /// <param name="icon">The icon value to return.</param>
        public FixedIconProvider(string icon)
        {
            _icon = icon;
        }

        /// <summary>
        /// Returns the fixed icon value.
        /// </summary>
        /// <param name="context">The resolution context to evaluate.</param>
        /// <returns>The icon value.</returns>
        public string? TryGetIcon(ServiceResolutionContext context)
        {
            return _icon;
        }
    }
}
